using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using reviews.Models;
using reviews.Services;
using reviews.Tests.Helpers;

namespace reviews.Tests.Unit.Services;

public class EvaluationServiceTests
{
    [Fact]
    public void FoundationMigration_BackfillUsesSeededDefinitionsReturningRows()
    {
        var migration = new reviews.Migrations.AddSurveyVersioningFoundation();
        var sql = migration.UpOperations
            .OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>()
            .First(operation => operation.Sql.Contains("WITH seeded_definitions"))
            .Sql;

        Assert.Contains("INNER JOIN seeded_definitions sd", sql);
        Assert.DoesNotContain("INNER JOIN public.\"SurveyDefinition\" sd", sql);
    }

    [Fact]
    public async Task GetSurveyManagementAsync_PopulatesUsageCountsForVersions()
    {
        var clientId = Guid.NewGuid();
        var evaluationId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var version1Id = Guid.NewGuid();
        var version2Id = Guid.NewGuid();
        var rateeId = Guid.NewGuid();
        var pinnedParticipantId = Guid.NewGuid();
        var submittedParticipantId = Guid.NewGuid();

        await using var db = CreateDbContext();

        db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
        db.SurveyDefinitions.Add(new SurveyDefinition
        {
            SurveyDefinitionId = surveyDefinitionId,
            ClientId = clientId,
            SurveyDefinitionKey = "leadership-competency",
            SurveyDefinitionName = "Leadership Competency",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SurveyDefinitionVersions.AddRange(
            new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = version1Id,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 1,
                TemplateKey = "leadership-competency-v1",
                TemplatePath = "/tmp/survey-v1.json",
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow,
                PublishedAt = DateTimeOffset.UtcNow
            },
            new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = version2Id,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 2,
                TemplateKey = "leadership-competency-v2",
                TemplatePath = "/tmp/survey-v2.json",
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow,
                PublishedAt = DateTimeOffset.UtcNow
            });

        db.Evaluations.Add(new Evaluation
        {
            EvaluationId = evaluationId,
            ClientId = clientId,
            EvaluationName = "FY26 Review",
            EvaluationFolder = "unused",
            EvaluationActive = true,
            CurrentSurveyDefinitionVersionId = version2Id
        });

        db.EvaluationRatees.Add(new EvaluationRatee
        {
            EvaluationRateeId = rateeId,
            EvaluationId = evaluationId,
            ParticipantId = Guid.NewGuid(),
            EvaluationRateeStatusId = 1,
            EvaluationRateeDateAdded = DateTime.UtcNow
        });

        db.EvaluationParticipants.AddRange(
            new EvaluationParticipant
            {
                EvaluationRateeId = rateeId,
                ParticipantId = pinnedParticipantId,
                EvaluationParticipantSurveyPassword = "pin-v1",
                EvaluationParticipantDateAdded = DateTime.UtcNow,
                AssignedSurveyDefinitionVersionId = version1Id
            },
            new EvaluationParticipant
            {
                EvaluationRateeId = rateeId,
                ParticipantId = submittedParticipantId,
                EvaluationParticipantSurveyPassword = "submit-v2",
                EvaluationParticipantDateAdded = DateTime.UtcNow,
                AssignedSurveyDefinitionVersionId = version2Id
            });

        db.EvaluationParticipantResponses.AddRange(
            new EvaluationParticipantResponse
            {
                EvaluationParticipantResponseId = Guid.NewGuid(),
                EvaluationRateeId = rateeId,
                RaterParticipantId = pinnedParticipantId,
                EvaluationParticipantSurveyPassword = "pin-v1",
                SurveyDefinitionVersionId = version1Id,
                SurveyTemplateKey = "leadership-competency-v1",
                SurveyTemplateVersion = 1,
                IsSubmitted = false
            },
            new EvaluationParticipantResponse
            {
                EvaluationParticipantResponseId = Guid.NewGuid(),
                EvaluationRateeId = rateeId,
                RaterParticipantId = submittedParticipantId,
                EvaluationParticipantSurveyPassword = "submit-v2",
                SurveyDefinitionVersionId = version2Id,
                SurveyTemplateKey = "leadership-competency-v2",
                SurveyTemplateVersion = 2,
                IsSubmitted = true
            });

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetSurveyManagementAsync(evaluationId, clientId);

        Assert.NotNull(result);
        var version1 = Assert.Single(result!.Versions, v => v.SurveyDefinitionVersionId == version1Id);
        var version2 = Assert.Single(result.Versions, v => v.SurveyDefinitionVersionId == version2Id);

        Assert.Equal(1, version1.PinnedParticipantCount);
        Assert.Equal(1, version1.StartedResponseCount);
        Assert.Equal(0, version1.SubmittedResponseCount);

        Assert.Equal(1, version2.PinnedParticipantCount);
        Assert.Equal(1, version2.StartedResponseCount);
        Assert.Equal(1, version2.SubmittedResponseCount);
        Assert.True(version2.IsCurrentForEvaluation);
        Assert.Equal(1, result.CurrentPinnedParticipantCount);
    }

    [Fact]
    public async Task UpdateSurveyVersionTemplatePathAsync_InvalidPath_ReturnsFailureResult()
    {
        var clientId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
        db.SurveyDefinitions.Add(new SurveyDefinition
        {
            SurveyDefinitionId = surveyDefinitionId,
            ClientId = clientId,
            SurveyDefinitionKey = "leadership-competency",
            SurveyDefinitionName = "Leadership Competency",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SurveyDefinitionVersions.Add(new SurveyDefinitionVersion
        {
            SurveyDefinitionVersionId = versionId,
            SurveyDefinitionId = surveyDefinitionId,
            VersionNumber = 1,
            TemplateKey = "leadership-competency-v1",
            TemplatePath = "/tmp/original.json",
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.UpdateSurveyVersionTemplatePathAsync(versionId, clientId, "/tmp/does-not-exist.json");

        Assert.False(result.Success);
        Assert.Equal("danger", result.Severity);
        Assert.Equal("/tmp/original.json", (await db.SurveyDefinitionVersions.FindAsync(versionId))!.TemplatePath);
    }

    [Fact]
    public async Task UpdateSurveyVersionTemplatePathAsync_ValidPath_UpdatesVersion()
    {
        var clientId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        await File.WriteAllTextAsync(tempFilePath, "{}");

        try
        {
            await using var db = CreateDbContext();
            db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
            db.SurveyDefinitions.Add(new SurveyDefinition
            {
                SurveyDefinitionId = surveyDefinitionId,
                ClientId = clientId,
                SurveyDefinitionKey = "leadership-competency",
                SurveyDefinitionName = "Leadership Competency",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.SurveyDefinitionVersions.Add(new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = versionId,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 1,
                TemplateKey = "leadership-competency-v1",
                TemplatePath = "/tmp/original.json",
                IsPublished = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var result = await service.UpdateSurveyVersionTemplatePathAsync(versionId, clientId, tempFilePath);

            Assert.True(result.Success);
            Assert.Equal(tempFilePath, (await db.SurveyDefinitionVersions.FindAsync(versionId))!.TemplatePath);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task UpdateSurveyVersionTemplatePathAsync_InvalidJson_ReturnsFailureResult()
    {
        var clientId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        await File.WriteAllTextAsync(tempFilePath, "{ not-json }");

        try
        {
            await using var db = CreateDbContext();
            db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
            db.SurveyDefinitions.Add(new SurveyDefinition
            {
                SurveyDefinitionId = surveyDefinitionId,
                ClientId = clientId,
                SurveyDefinitionKey = "leadership-competency",
                SurveyDefinitionName = "Leadership Competency",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.SurveyDefinitionVersions.Add(new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = versionId,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 1,
                TemplateKey = "leadership-competency-v1",
                TemplatePath = "/tmp/original.json",
                IsPublished = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var result = await service.UpdateSurveyVersionTemplatePathAsync(versionId, clientId, tempFilePath);

            Assert.False(result.Success);
            Assert.Contains("not valid JSON", result.Message);
            Assert.Equal("/tmp/original.json", (await db.SurveyDefinitionVersions.FindAsync(versionId))!.TemplatePath);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task SetCurrentSurveyVersionAsync_WithPinnedParticipantsAndNoForce_ReturnsWarning()
    {
        var clientId = Guid.NewGuid();
        var evaluationId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var currentVersionId = Guid.NewGuid();
        var nextVersionId = Guid.NewGuid();
        var rateeId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
        db.SurveyDefinitions.Add(new SurveyDefinition
        {
            SurveyDefinitionId = surveyDefinitionId,
            ClientId = clientId,
            SurveyDefinitionKey = "leadership-competency",
            SurveyDefinitionName = "Leadership Competency",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SurveyDefinitionVersions.AddRange(
            new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = currentVersionId,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 1,
                TemplateKey = "leadership-competency-v1",
                TemplatePath = "/tmp/v1.json",
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = nextVersionId,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 2,
                TemplateKey = "leadership-competency-v2",
                TemplatePath = "/tmp/v2.json",
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        db.Evaluations.Add(new Evaluation
        {
            EvaluationId = evaluationId,
            ClientId = clientId,
            EvaluationName = "FY26 Review",
            EvaluationFolder = "unused",
            EvaluationActive = true,
            CurrentSurveyDefinitionVersionId = currentVersionId
        });
        db.EvaluationRatees.Add(new EvaluationRatee
        {
            EvaluationRateeId = rateeId,
            EvaluationId = evaluationId,
            ParticipantId = Guid.NewGuid(),
            EvaluationRateeStatusId = 1,
            EvaluationRateeDateAdded = DateTime.UtcNow
        });
        db.EvaluationParticipants.Add(new EvaluationParticipant
        {
            EvaluationRateeId = rateeId,
            ParticipantId = Guid.NewGuid(),
            EvaluationParticipantSurveyPassword = "pin-v1",
            EvaluationParticipantDateAdded = DateTime.UtcNow,
            AssignedSurveyDefinitionVersionId = currentVersionId
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SetCurrentSurveyVersionAsync(evaluationId, nextVersionId, clientId);

        Assert.False(result.Success);
        Assert.Equal("warning", result.Severity);
        Assert.Equal(currentVersionId, (await db.Evaluations.FindAsync(evaluationId))!.CurrentSurveyDefinitionVersionId);
    }

    [Fact]
    public async Task SetCurrentSurveyVersionAsync_WithPinnedParticipantsAndForce_Succeeds()
    {
        var clientId = Guid.NewGuid();
        var evaluationId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var currentVersionId = Guid.NewGuid();
        var nextVersionId = Guid.NewGuid();
        var rateeId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
        db.SurveyDefinitions.Add(new SurveyDefinition
        {
            SurveyDefinitionId = surveyDefinitionId,
            ClientId = clientId,
            SurveyDefinitionKey = "leadership-competency",
            SurveyDefinitionName = "Leadership Competency",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SurveyDefinitionVersions.AddRange(
            new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = currentVersionId,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 1,
                TemplateKey = "leadership-competency-v1",
                TemplatePath = "/tmp/v1.json",
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = nextVersionId,
                SurveyDefinitionId = surveyDefinitionId,
                VersionNumber = 2,
                TemplateKey = "leadership-competency-v2",
                TemplatePath = "/tmp/v2.json",
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        db.Evaluations.Add(new Evaluation
        {
            EvaluationId = evaluationId,
            ClientId = clientId,
            EvaluationName = "FY26 Review",
            EvaluationFolder = "unused",
            EvaluationActive = true,
            CurrentSurveyDefinitionVersionId = currentVersionId
        });
        db.EvaluationRatees.Add(new EvaluationRatee
        {
            EvaluationRateeId = rateeId,
            EvaluationId = evaluationId,
            ParticipantId = Guid.NewGuid(),
            EvaluationRateeStatusId = 1,
            EvaluationRateeDateAdded = DateTime.UtcNow
        });
        db.EvaluationParticipants.Add(new EvaluationParticipant
        {
            EvaluationRateeId = rateeId,
            ParticipantId = Guid.NewGuid(),
            EvaluationParticipantSurveyPassword = "pin-v1",
            EvaluationParticipantDateAdded = DateTime.UtcNow,
            AssignedSurveyDefinitionVersionId = currentVersionId
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SetCurrentSurveyVersionAsync(evaluationId, nextVersionId, clientId, forceSwitch: true);

        Assert.True(result.Success);
        Assert.Equal(nextVersionId, (await db.Evaluations.FindAsync(evaluationId))!.CurrentSurveyDefinitionVersionId);
    }

    [Fact]
    public async Task GetSurveyVersionUsageAsync_ReturnsRowsForRequestedUsageType()
    {
        var clientId = Guid.NewGuid();
        var evaluationId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var rateeId = Guid.NewGuid();
        var raterId = Guid.NewGuid();
        var rateeParticipantId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
        db.ParticipantRoles.Add(new ParticipantRole { ParticipantRoleId = 3, ParticipantRoleName = "Peer" });
        db.EvaluationParticipantStatuses.Add(new EvaluationParticipantStatus
        {
            EvaluationParticipantStatusId = 2,
            EvaluationParticipantStatusName = "In Progress",
            EvaluationParticipantStatusIsActive = 1,
            EvaluationParticipantStatusIsComplete = 0,
            EvaluationParticipantStatusDecoration = "status"
        });
        db.Participants.AddRange(
            TestDataBuilder.CreateParticipant(participantId: raterId, clientId: clientId, firstName: "Rater", lastName: "One", email: "rater@example.com"),
            TestDataBuilder.CreateParticipant(participantId: rateeParticipantId, clientId: clientId, firstName: "Ratee", lastName: "One", email: "ratee@example.com"));
        db.SurveyDefinitions.Add(new SurveyDefinition
        {
            SurveyDefinitionId = surveyDefinitionId,
            ClientId = clientId,
            SurveyDefinitionKey = "leadership-competency",
            SurveyDefinitionName = "Leadership Competency",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SurveyDefinitionVersions.Add(new SurveyDefinitionVersion
        {
            SurveyDefinitionVersionId = versionId,
            SurveyDefinitionId = surveyDefinitionId,
            VersionNumber = 1,
            TemplateKey = "leadership-competency-v1",
            TemplatePath = "/tmp/v1.json",
            IsPublished = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Evaluations.Add(new Evaluation
        {
            EvaluationId = evaluationId,
            ClientId = clientId,
            EvaluationName = "FY26 Review",
            EvaluationFolder = "unused",
            EvaluationActive = true,
            CurrentSurveyDefinitionVersionId = versionId
        });
        db.EvaluationRatees.Add(new EvaluationRatee
        {
            EvaluationRateeId = rateeId,
            EvaluationId = evaluationId,
            ParticipantId = rateeParticipantId,
            EvaluationRateeStatusId = 1,
            EvaluationRateeDateAdded = DateTime.UtcNow
        });
        db.EvaluationParticipants.Add(new EvaluationParticipant
        {
            EvaluationRateeId = rateeId,
            ParticipantId = raterId,
            ParticipantRoleId = 3,
            EvaluationParticipantStatusId = 2,
            EvaluationParticipantSurveyPassword = "pin-v1",
            EvaluationParticipantDateAdded = DateTime.UtcNow,
            AssignedSurveyDefinitionVersionId = versionId
        });
        db.EvaluationParticipantResponses.Add(new EvaluationParticipantResponse
        {
            EvaluationParticipantResponseId = Guid.NewGuid(),
            EvaluationRateeId = rateeId,
            RaterParticipantId = raterId,
            EvaluationParticipantSurveyPassword = "pin-v1",
            SurveyDefinitionVersionId = versionId,
            SurveyTemplateKey = "leadership-competency-v1",
            SurveyTemplateVersion = 1,
            IsSubmitted = true,
            LastSavedAt = DateTimeOffset.UtcNow,
            SubmittedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetSurveyVersionUsageAsync(evaluationId, versionId, clientId, "submitted");

        Assert.NotNull(result);
        Assert.Equal("submitted", result!.UsageType);
        Assert.Single(result.Rows);
        Assert.Equal("Ratee One", result.Rows[0].RateeName);
        Assert.Equal("Rater One", result.Rows[0].RaterName);
        Assert.Equal("Peer", result.Rows[0].RoleName);
        Assert.True(result.Rows[0].IsSubmitted);
    }

    [Fact]
    public async Task SaveSurveyVersionDraftAsync_ValidJson_SavesDraftAndEditorMetadata()
    {
        var clientId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
        db.SurveyDefinitions.Add(new SurveyDefinition
        {
            SurveyDefinitionId = surveyDefinitionId,
            ClientId = clientId,
            SurveyDefinitionKey = "leadership-competency",
            SurveyDefinitionName = "Leadership Competency",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SurveyDefinitionVersions.Add(new SurveyDefinitionVersion
        {
            SurveyDefinitionVersionId = versionId,
            SurveyDefinitionId = surveyDefinitionId,
            VersionNumber = 1,
            TemplateKey = "leadership-competency-v1",
            TemplatePath = "/tmp/v1.json",
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SaveSurveyVersionDraftAsync(versionId, clientId, "{\"pages\":[]}", "admin@test.com");

        Assert.True(result.Success);
        var version = await db.SurveyDefinitionVersions.FindAsync(versionId);
        Assert.NotNull(version!.DraftJson);
        Assert.Equal("admin@test.com", version.LastEditedBy);
        Assert.NotNull(version.LastEditedAt);
    }

    [Fact]
    public async Task PublishSurveyVersionAsync_UsesDraftJsonAsPublishedJson()
    {
        var clientId = Guid.NewGuid();
        var surveyDefinitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        await using var db = CreateDbContext();
        db.Clients.Add(TestDataBuilder.CreateClient(clientId: clientId));
        db.SurveyDefinitions.Add(new SurveyDefinition
        {
            SurveyDefinitionId = surveyDefinitionId,
            ClientId = clientId,
            SurveyDefinitionKey = "leadership-competency",
            SurveyDefinitionName = "Leadership Competency",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SurveyDefinitionVersions.Add(new SurveyDefinitionVersion
        {
            SurveyDefinitionVersionId = versionId,
            SurveyDefinitionId = surveyDefinitionId,
            VersionNumber = 1,
            TemplateKey = "leadership-competency-v1",
            TemplatePath = "/tmp/v1.json",
            DraftJson = "{\"pages\":[{\"name\":\"page1\"}]}",
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.PublishSurveyVersionAsync(versionId, clientId, "publisher@test.com");

        Assert.True(result.Success);
        var version = await db.SurveyDefinitionVersions.FindAsync(versionId);
        Assert.NotNull(version!.PublishedJson);
        Assert.Equal("publisher@test.com", version.PublishedBy);
        Assert.NotNull(version.PublishedAt);
        Assert.True(version.IsPublished);
    }

    [Fact]
    public async Task PublishSurveyVersionAsync_AlreadyPublished_PreservesSnapshot()
    {
        await using var db = CreateDbContext();
        var evaluation = await SeedAuthoredSurveyAsync(db);
        var version = evaluation.CurrentSurveyDefinitionVersion!;
        var originalJson = version.PublishedJson;
        var publishedAt = version.PublishedAt;
        version.DraftJson = "{\"pages\":[{\"name\":\"changed\"}]}";
        await db.SaveChangesAsync();

        var result = await CreateService(db).PublishSurveyVersionAsync(
            version.SurveyDefinitionVersionId, evaluation.ClientId, "new-publisher@test.com");

        Assert.False(result.Success);
        Assert.Equal(originalJson, version.PublishedJson);
        Assert.Equal(publishedAt, version.PublishedAt);
        Assert.Equal("original@test.com", version.PublishedBy);
    }

    [Fact]
    public async Task PublishedVersion_RejectsDraftAndPathChanges()
    {
        await using var db = CreateDbContext();
        var evaluation = await SeedAuthoredSurveyAsync(db);
        var version = evaluation.CurrentSurveyDefinitionVersion!;
        var service = CreateService(db);
        var originalDraft = version.DraftJson;
        var originalPath = version.TemplatePath;

        var draftResult = await service.SaveSurveyVersionDraftAsync(
            version.SurveyDefinitionVersionId, evaluation.ClientId, "{}", "editor@test.com");
        var pathResult = await service.UpdateSurveyVersionTemplatePathAsync(
            version.SurveyDefinitionVersionId, evaluation.ClientId, "different.json");

        Assert.False(draftResult.Success);
        Assert.False(pathResult.Success);
        Assert.Equal(originalDraft, version.DraftJson);
        Assert.Equal(originalPath, version.TemplatePath);
    }

    [Fact]
    public async Task SetCurrentSurveyVersionAsync_UnpublishedVersion_RejectsSwitch()
    {
        await using var db = CreateDbContext();
        var evaluation = await SeedAuthoredSurveyAsync(db);
        var service = CreateService(db);
        await service.CreateNextSurveyVersionAsync(evaluation.EvaluationId, evaluation.ClientId);
        var draft = await db.SurveyDefinitionVersions.SingleAsync(version => !version.IsPublished);

        var switchResult = await service.SetCurrentSurveyVersionAsync(
            evaluation.EvaluationId, draft.SurveyDefinitionVersionId, evaluation.ClientId, true);
        var model = new Evaluation
        {
            EvaluationId = evaluation.EvaluationId,
            ClientId = evaluation.ClientId,
            CurrentSurveyDefinitionVersionId = draft.SurveyDefinitionVersionId
        };
        var saveResult = await service.SaveEvaluationAsync(model, evaluation.ClientId);

        Assert.False(switchResult.Success);
        Assert.False(saveResult);
        Assert.NotEqual(draft.SurveyDefinitionVersionId, evaluation.CurrentSurveyDefinitionVersionId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloneSurvey_CopiesAuthoredJsonAsDraft_WithoutSwitchingEvaluation(bool newFamily)
    {
        await using var db = CreateDbContext();
        var evaluation = await SeedAuthoredSurveyAsync(db);
        var original = evaluation.CurrentSurveyDefinitionVersion!;
        var service = CreateService(db);

        var result = newFamily
            ? await service.CreateNewSurveyAsync(evaluation.EvaluationId, evaluation.ClientId)
            : await service.CreateNextSurveyVersionAsync(evaluation.EvaluationId, evaluation.ClientId);

        Assert.True(result.Success);
        var clone = Assert.Single(await db.SurveyDefinitionVersions
            .Where(version => version.SurveyDefinitionVersionId != original.SurveyDefinitionVersionId)
            .ToListAsync());
        Assert.Equal(original.PublishedJson, clone.DraftJson);
        Assert.Null(clone.PublishedJson);
        Assert.False(clone.IsPublished);
        Assert.Null(clone.PublishedAt);
        Assert.Equal(original.SurveyDefinitionVersionId, evaluation.CurrentSurveyDefinitionVersionId);
        Assert.Equal(newFamily ? 1 : 2, clone.VersionNumber);
        Assert.Equal(newFamily, clone.SurveyDefinitionId != original.SurveyDefinitionId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VersionSwitch_PreservesPinnedResponseAndTemplate(bool submitted)
    {
        await using var db = CreateDbContext();
        var evaluation = await SeedAuthoredSurveyAsync(db);
        var original = evaluation.CurrentSurveyDefinitionVersion!;
        var ratee = TestDataBuilder.CreateEvaluationRatee(evaluationId: evaluation.EvaluationId);
        var participant = TestDataBuilder.CreateEvaluationParticipant(evaluationRateeId: ratee.EvaluationRateeId);
        db.EvaluationRatees.Add(ratee);
        db.EvaluationParticipants.Add(participant);
        await db.SaveChangesAsync();
        var store = new SurveyResponseStore(db, new Mock<ILogger<SurveyResponseStore>>().Object);
        var resolver = new SurveyTemplateResolver(
            new ConfigurationBuilder().Build(), db, new Mock<ILogger<SurveyTemplateResolver>>().Object);
        var service = CreateService(db);
        var password = participant.EvaluationParticipantSurveyPassword;
        await store.CreateAsync(ratee.EvaluationRateeId, participant.ParticipantId, password,
            original.SurveyDefinitionVersionId, original.TemplateKey, original.VersionNumber);
        await store.SaveDraftAsync(password, "{\"answer\":\"saved\"}");
        if (submitted)
        {
            await store.SubmitAsync(password, "{\"answer\":\"final\"}");
        }

        await service.CreateNextSurveyVersionAsync(evaluation.EvaluationId, evaluation.ClientId);
        var next = await db.SurveyDefinitionVersions.SingleAsync(version => !version.IsPublished);
        Assert.Null(await resolver.ResolveAsync(next.SurveyDefinitionVersionId));
        await service.SaveSurveyVersionDraftAsync(next.SurveyDefinitionVersionId, evaluation.ClientId,
            "{\"pages\":[{\"name\":\"new-survey\"}]}", "editor@test.com");
        Assert.True((await service.PublishSurveyVersionAsync(
            next.SurveyDefinitionVersionId, evaluation.ClientId, "publisher@test.com")).Success);
        Assert.True((await service.SetCurrentSurveyVersionAsync(
            evaluation.EvaluationId, next.SurveyDefinitionVersionId, evaluation.ClientId, true)).Success);

        var response = await store.GetByPasswordAsync(password);
        Assert.Equal(original.SurveyDefinitionVersionId, response!.SurveyDefinitionVersionId);
        Assert.Equal(original.SurveyDefinitionVersionId, participant.AssignedSurveyDefinitionVersionId);
        Assert.Equal("{\"answer\":\"saved\"}", response.DraftJson);
        Assert.Equal(submitted, response.IsSubmitted);
        Assert.Equal(original.PublishedJson, (await resolver.ResolveAsync(response.SurveyDefinitionVersionId!.Value))!.Json);
        Assert.Equal(next.PublishedJson, (await resolver.ResolveAsync(evaluation.CurrentSurveyDefinitionVersionId!.Value))!.Json);

        if (submitted)
        {
            Assert.Equal("{\"answer\":\"final\"}", response.FinalJson);
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveDraftAsync(password, "{}"));
        }
        else
        {
            await store.SubmitAsync(password, "{\"answer\":\"final\"}");
            Assert.True((await store.GetByPasswordAsync(password))!.IsSubmitted);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloneSurvey_LegacyFileSource_CapturesContentAsDraft(bool newFamily)
    {
        var templatePath = Path.GetTempFileName();
        try
        {
            const string originalJson = "{\"pages\":[{\"name\":\"legacy\"}]}";
            await File.WriteAllTextAsync(templatePath, originalJson);
            await using var db = CreateDbContext();
            var evaluation = await SeedAuthoredSurveyAsync(db);
            evaluation.CurrentSurveyDefinitionVersion!.PublishedJson = null;
            evaluation.CurrentSurveyDefinitionVersion.TemplatePath = templatePath;
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = newFamily
                ? await service.CreateNewSurveyAsync(evaluation.EvaluationId, evaluation.ClientId)
                : await service.CreateNextSurveyVersionAsync(evaluation.EvaluationId, evaluation.ClientId);
            await File.WriteAllTextAsync(templatePath, "{}");

            Assert.True(result.Success);
            var clone = await db.SurveyDefinitionVersions.SingleAsync(version => !version.IsPublished);
            Assert.Equal(originalJson, clone.DraftJson);
            Assert.False(clone.IsPublished);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    private static async Task<Evaluation> SeedAuthoredSurveyAsync(ApplicationDbContext db)
    {
        var client = TestDataBuilder.CreateClient();
        var definition = new SurveyDefinition
        {
            ClientId = client.ClientId,
            SurveyDefinitionKey = "authored-survey",
            SurveyDefinitionName = "Authored Survey"
        };
        var version = new SurveyDefinitionVersion
        {
            SurveyDefinition = definition,
            SurveyDefinitionId = definition.SurveyDefinitionId,
            VersionNumber = 1,
            TemplateKey = "authored-survey-v1",
            TemplatePath = "missing-file.json",
            PublishedJson = "{\"pages\":[{\"name\":\"authored\"}]}",
            DraftJson = "{\"pages\":[{\"name\":\"unpublished-edit\"}]}",
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow,
            PublishedBy = "original@test.com"
        };
        var evaluation = new Evaluation
        {
            ClientId = client.ClientId,
            EvaluationName = "Review",
            EvaluationFolder = "unused",
            CurrentSurveyDefinitionVersion = version,
            CurrentSurveyDefinitionVersionId = version.SurveyDefinitionVersionId
        };
        db.Clients.Add(client);
        db.Evaluations.Add(evaluation);
        await db.SaveChangesAsync();
        return evaluation;
    }

    private static EvaluationService CreateService(ApplicationDbContext db)
    {
        return new EvaluationService(
            db,
            new Mock<ILogger<EvaluationService>>().Object);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, null!);
    }
}
