using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace reviews.Services;

/// <summary>
/// Simple survey template resolver that loads templates from the Surveys/ folder.
/// Per Issue #9 spec - stores templates in codebase initially.
/// </summary>
public interface ISurveyTemplateResolver
{
    Task<SurveyTemplate?> ResolveAsync(string templateKey, int templateVersion, CancellationToken ct = default);
}

public record SurveyTemplate(string Key, int Version, string Json);

public class SurveyTemplateResolver : ISurveyTemplateResolver
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SurveyTemplateResolver> _logger;

    public SurveyTemplateResolver(
        IConfiguration configuration,
        ILogger<SurveyTemplateResolver> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SurveyTemplate?> ResolveAsync(string templateKey, int templateVersion, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resolving survey template: {TemplateKey} v{Version}",
            templateKey,
            templateVersion);

        // For now, we only have one template: leadership-competency-v1
        // Future: Add template resolution logic for multiple templates

        var basePath = _configuration["Survey:TemplatePath"] 
            ?? Path.Combine(Directory.GetCurrentDirectory(), "Surveys", "leadership-competency-v1.json");

        if (!File.Exists(basePath))
        {
            _logger.LogError("Survey template file not found: {Path}", basePath);
            return null;
        }

        var json = await File.ReadAllTextAsync(basePath, ct);

        return new SurveyTemplate(
            Key: templateKey,
            Version: templateVersion,
            Json: json);
    }
}
