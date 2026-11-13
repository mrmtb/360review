using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using reviews.Models;
using reviews.Services;
using reviews.Tests.Helpers;

namespace reviews.Tests.Unit.Services;

public class SurveyVersionBootstrapperTests
{
    [Fact]
    public async Task SaveClientAsync_NewClient_ProvisionsSelectablePublishedSurvey()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "{\"title\":\"Configured survey\",\"elements\":[]}");
            await using var db = CreateDbContext();
            var configuration = Configuration(path);
            var service = new ClientService(db, Mock.Of<ILogger<ClientService>>(), configuration);
            var client = TestDataBuilder.CreateClient(clientId: Guid.Empty);

            Assert.True(await service.SaveClientAsync(client));
            var saved = Assert.Single(await db.Clients.ToListAsync());
            var evaluationService = new EvaluationService(db, Mock.Of<ILogger<EvaluationService>>());
            var option = Assert.Single(await evaluationService.GetSurveyVersionOptionsAsync(saved.ClientId));
            var version = await db.SurveyDefinitionVersions.SingleAsync();
            Assert.Equal(version.SurveyDefinitionVersionId, option.SurveyDefinitionVersionId);
            Assert.True(version.IsPublished);
            Assert.Equal("Configured survey", JsonDocument.Parse(version.PublishedJson!).RootElement.GetProperty("title").GetString());
            Assert.Equal(version.SurveyDefinitionVersionId, evaluationService.CreateNewEvaluation(saved.ClientId).CurrentSurveyDefinitionVersionId);
            saved.ClientName = "Renamed client";
            Assert.True(await service.SaveClientAsync(saved));
            Assert.Single(await db.SurveyDefinitionVersions.ToListAsync());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ImportLegacyVersionsAsync_UsesConfiguredTemplateAndDoesNotOverwriteSnapshot()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "{\"title\":\"Legacy custom survey\"}");
            await using var db = CreateDbContext();
            var client = TestDataBuilder.CreateClient();
            db.Clients.Add(client);
            var version = new SurveyDefinitionVersion
            {
                SurveyDefinition = new SurveyDefinition
                {
                    ClientId = client.ClientId,
                    SurveyDefinitionKey = "leadership-competency",
                    SurveyDefinitionName = "Legacy"
                },
                VersionNumber = 1,
                TemplateKey = "leadership-competency-v1",
                TemplatePath = "Surveys/leadership-competency-v1.json",
                IsPublished = true
            };
            db.SurveyDefinitionVersions.Add(version);
            await db.SaveChangesAsync();

            await SurveyVersionBootstrapper.ImportLegacyVersionsAsync(db, Configuration(path));
            var snapshot = version.PublishedJson;
            Assert.Equal(path, version.TemplatePath);
            Assert.Equal("Legacy custom survey", JsonDocument.Parse(snapshot!).RootElement.GetProperty("title").GetString());
            await File.WriteAllTextAsync(path, "{\"title\":\"Changed file\"}");
            await SurveyVersionBootstrapper.ImportLegacyVersionsAsync(db, Configuration(path));
            Assert.Equal(snapshot, version.PublishedJson);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static IConfiguration Configuration(string path) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Survey:TemplatePath"] = path }).Build();

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, null!);
}