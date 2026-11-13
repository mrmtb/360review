using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using reviews.Models;

namespace reviews.Services;

public static class SurveyVersionBootstrapper
{
    public static async Task<SurveyDefinitionVersion> CreateInitialVersionAsync(Guid clientId, IConfiguration configuration)
    {
        var path = configuration["Survey:TemplatePath"] ?? "Surveys/leadership-competency-v1.json";
        var source = await File.ReadAllTextAsync(Path.GetFullPath(path));
        using var document = JsonDocument.Parse(source);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("The configured legacy survey template must be a JSON object.");
        }

        return new SurveyDefinitionVersion
        {
            SurveyDefinition = new SurveyDefinition
            {
                ClientId = clientId,
                SurveyDefinitionKey = "leadership-competency",
                SurveyDefinitionName = "Leadership Competency",
                IsActive = true
            },
            VersionNumber = 1,
            TemplateKey = "leadership-competency-v1",
            TemplatePath = path,
            PublishedJson = document.RootElement.GetRawText(),
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow
        };
    }

    public static async Task ImportLegacyVersionsAsync(ApplicationDbContext db, IConfiguration configuration)
    {
        var versions = await db.SurveyDefinitionVersions
            .Where(version => version.VersionNumber == 1 && version.TemplateKey == "leadership-competency-v1" &&
                version.IsPublished && version.PublishedJson == null && version.DraftJson == null)
            .ToListAsync();
        if (versions.Count == 0)
        {
            return;
        }

        var source = await CreateInitialVersionAsync(Guid.Empty, configuration);
        foreach (var version in versions)
        {
            version.TemplatePath = source.TemplatePath;
            version.PublishedJson = source.PublishedJson;
        }

        await db.SaveChangesAsync();
    }
}