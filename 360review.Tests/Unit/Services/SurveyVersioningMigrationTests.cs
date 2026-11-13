using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using reviews.Migrations;
using reviews.Models;

namespace reviews.Tests.Unit.Services;

public class SurveyVersioningMigrationTests
{
    [PostgresFact]
    public async Task Publication_RejectsStalePublisherAndDraftWriter()
    {
        var connectionString = Environment.GetEnvironmentVariable("SURVEY_MIGRATION_TEST_CONNECTION")!;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.StartsWith("360review_pr12_", connection.Database);
        await using var transaction = await connection.BeginTransactionAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
        await using var setup = new ApplicationDbContext(options, null!);
        await setup.Database.UseTransactionAsync(transaction);
        await using (var emptyCheck = new NpgsqlCommand("SELECT count(*) FROM pg_tables WHERE schemaname = 'public'", connection, transaction))
        {
            Assert.Equal(0L, await emptyCheck.ExecuteScalarAsync());
        }

        await setup.Database.ExecuteSqlRawAsync(setup.Database.GenerateCreateScript());
        var client = reviews.Tests.Helpers.TestDataBuilder.CreateClient();
        var version = new SurveyDefinitionVersion
        {
            SurveyDefinition = new SurveyDefinition
            {
                ClientId = client.ClientId, SurveyDefinitionKey = "concurrent", SurveyDefinitionName = "Concurrent"
            },
            VersionNumber = 1, TemplateKey = "concurrent-v1", TemplatePath = "unused.json",
            DraftJson = "{\"title\":\"Original draft\"}", IsPublished = false
        };
        setup.Clients.Add(client);
        setup.SurveyDefinitionVersions.Add(version);
        await setup.SaveChangesAsync();

        await using var first = new ApplicationDbContext(options, null!);
        await using var second = new ApplicationDbContext(options, null!);
        await using var editor = new ApplicationDbContext(options, null!);
        foreach (var context in new[] { first, second, editor })
        {
            await context.Database.UseTransactionAsync(transaction);
            await context.SurveyDefinitionVersions.Include(item => item.SurveyDefinition).SingleAsync();
        }

        reviews.Services.EvaluationService Service(ApplicationDbContext context) => new(context,
            Moq.Mock.Of<Microsoft.Extensions.Logging.ILogger<reviews.Services.EvaluationService>>());
        var winner = await Service(first).PublishSurveyVersionAsync(
            version.SurveyDefinitionVersionId, client.ClientId, "first@test.com", "{\"title\":\"Winner\"}");
        var loser = await Service(second).PublishSurveyVersionAsync(
            version.SurveyDefinitionVersionId, client.ClientId, "second@test.com", "{\"title\":\"Loser\"}");
        var staleDraft = await Service(editor).SaveSurveyVersionDraftAsync(
            version.SurveyDefinitionVersionId, client.ClientId, "{\"title\":\"Late edit\"}", "editor@test.com");

        Assert.True(winner.Success);
        Assert.False(loser.Success);
        Assert.False(staleDraft.Success);
        await setup.Entry(version).ReloadAsync();
        Assert.Equal("first@test.com", version.PublishedBy);
        Assert.Contains("Winner", version.PublishedJson);
        Assert.Equal(version.DraftJson, version.PublishedJson);
        await transaction.RollbackAsync();
    }

    [PostgresFact]
    public async Task UpgradeAndRollback_PreserveLegacyDataAndBackfillVersionLinks()
    {
        var connectionString = Environment.GetEnvironmentVariable("SURVEY_MIGRATION_TEST_CONNECTION")!;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.StartsWith("360review_pr12_", connection.Database);
        await using var transaction = await connection.BeginTransactionAsync();

        async Task ExecuteAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await command.ExecuteNonQueryAsync();
        }

        async Task<long> CountAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            return (long)(await command.ExecuteScalarAsync())!;
        }

        Assert.Equal(0, await CountAsync("SELECT count(*) FROM pg_tables WHERE schemaname = 'public'"));
        await ExecuteAsync(
            """
            CREATE TABLE public."Client" ("ClientId" uuid PRIMARY KEY);
            CREATE TABLE public."Evaluation" ("EvaluationId" uuid PRIMARY KEY, "ClientId" uuid NOT NULL);
            CREATE TABLE public."EvaluationRatee" ("EvaluationRateeId" uuid PRIMARY KEY, "EvaluationId" uuid NOT NULL);
            CREATE TABLE public."EvaluationParticipant" (
                "EvaluationRateeId" uuid NOT NULL, "ParticipantId" uuid NOT NULL,
                "EvaluationParticipantSurveyPassword" text NOT NULL,
                "SurveyTemplateKey" text, "SurveyTemplateVersion" integer);
            CREATE TABLE public."EvaluationParticipantResponse" (
                "EvaluationParticipantResponseId" uuid PRIMARY KEY,
                "EvaluationRateeId" uuid NOT NULL, "RaterParticipantId" uuid NOT NULL,
                "EvaluationParticipantSurveyPassword" text NOT NULL,
                "SurveyTemplateKey" text, "SurveyTemplateVersion" integer,
                "DraftJson" jsonb, "FinalJson" jsonb, "IsSubmitted" boolean NOT NULL);
            INSERT INTO public."Client" VALUES
                ('00000000-0000-0000-0000-000000000001'),
                ('00000000-0000-0000-0000-000000000002');
            INSERT INTO public."Evaluation" SELECT "ClientId", "ClientId" FROM public."Client";
            INSERT INTO public."EvaluationRatee" SELECT "ClientId", "ClientId" FROM public."Client";
            INSERT INTO public."EvaluationParticipant"
                SELECT "ClientId", "ClientId", 'started-' || "ClientId", NULL, NULL FROM public."Client";
            INSERT INTO public."EvaluationParticipant" VALUES (
                '00000000-0000-0000-0000-000000000001', gen_random_uuid(), 'not-started', NULL, NULL);
            INSERT INTO public."EvaluationParticipantResponse"
                SELECT gen_random_uuid(), "ClientId", "ClientId", 'started-' || "ClientId", '', 0,
                    '{"answer":"draft"}', '{"answer":"final"}',
                    "ClientId" = '00000000-0000-0000-0000-000000000002'::uuid
                FROM public."Client";
            """);

        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options, null!);
        var generator = db.GetService<IMigrationsSqlGenerator>();
        var foundation = new AddSurveyVersioningFoundation();
        var authoring = new AddSurveyVersionAuthoring();

        foreach (var migration in new Migration[] { foundation, authoring })
        {
            foreach (var command in generator.Generate(migration.UpOperations, db.Model))
            {
                await ExecuteAsync(command.CommandText);
            }
        }

        Assert.Equal(2, await CountAsync(
            """
            SELECT count(*) FROM public."Evaluation" evaluation
            JOIN public."SurveyDefinitionVersion" version
                ON version."SurveyDefinitionVersionId" = evaluation."CurrentSurveyDefinitionVersionId"
            JOIN public."SurveyDefinition" definition
                ON definition."SurveyDefinitionId" = version."SurveyDefinitionId"
                AND definition."ClientId" = evaluation."ClientId"
            """));
        Assert.Equal(2, await CountAsync(
            """
            SELECT count(*) FROM public."EvaluationParticipantResponse" response
            JOIN public."EvaluationRatee" ratee ON ratee."EvaluationRateeId" = response."EvaluationRateeId"
            JOIN public."Evaluation" evaluation ON evaluation."EvaluationId" = ratee."EvaluationId"
            WHERE response."SurveyDefinitionVersionId" = evaluation."CurrentSurveyDefinitionVersionId"
                AND response."SurveyTemplateKey" = 'leadership-competency-v1'
                AND response."SurveyTemplateVersion" = 1
                AND response."DraftJson" = '{"answer":"draft"}'::jsonb
                AND response."FinalJson" = '{"answer":"final"}'::jsonb
            """));
        Assert.Equal(2, await CountAsync(
            """
            SELECT count(*) FROM public."EvaluationParticipant" participant
            JOIN public."EvaluationParticipantResponse" response USING ("EvaluationParticipantSurveyPassword")
            WHERE participant."AssignedSurveyDefinitionVersionId" = response."SurveyDefinitionVersionId"
            """));
        Assert.Equal(1, await CountAsync(
            """
            SELECT count(*) FROM public."EvaluationParticipant"
            WHERE "EvaluationParticipantSurveyPassword" = 'not-started'
                AND "AssignedSurveyDefinitionVersionId" IS NULL
            """));

        foreach (var migration in new Migration[] { authoring, foundation })
        {
            foreach (var command in generator.Generate(migration.DownOperations, db.Model))
            {
                await ExecuteAsync(command.CommandText);
            }
        }

        Assert.Equal(0, await CountAsync(
            "SELECT count(*) FROM pg_tables WHERE schemaname = 'public' AND tablename IN ('SurveyDefinition', 'SurveyDefinitionVersion')"));
        Assert.Equal(2, await CountAsync("SELECT count(*) FROM public.\"Evaluation\""));
        Assert.Equal(3, await CountAsync("SELECT count(*) FROM public.\"EvaluationParticipant\""));
        Assert.Equal(2, await CountAsync(
            """
            SELECT count(*) FROM public."EvaluationParticipantResponse"
            WHERE "DraftJson" = '{"answer":"draft"}'::jsonb AND "FinalJson" = '{"answer":"final"}'::jsonb
            """));
        await transaction.RollbackAsync();
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SURVEY_MIGRATION_TEST_CONNECTION")))
        {
            Skip = "Set SURVEY_MIGRATION_TEST_CONNECTION to an empty disposable 360review_pr12_* PostgreSQL database.";
        }
    }
}