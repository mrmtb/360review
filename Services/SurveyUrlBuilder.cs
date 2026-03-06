using Microsoft.AspNetCore.Http;
using reviews.Models;

namespace reviews.Services;

public class SurveyUrlBuilder : ISurveyUrlBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SurveyUrlBuilder(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string BuildSurveyUrl(vwEvaluationsToComplete evaluation)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        
        if (request == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        var siteUrl = $"{request.Scheme}://{request.Host}";
        
        if (!siteUrl.EndsWith("/"))
        {
            siteUrl += "/";
        }

        // New SurveyJS route format: /survey/{password}
        // The password identifies the participant assignment; all context is resolved server-side
        var surveyUrl = $"{siteUrl}survey/{evaluation.EvaluationParticipantSurveyPassword}";

        return surveyUrl;
    }

    public string GetSiteBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        
        if (request == null)
        {
            return string.Empty;
        }

        var siteUrl = $"{request.Scheme}://{request.Host}";
        
        if (!siteUrl.EndsWith("/"))
        {
            siteUrl += "/";
        }

        return siteUrl;
    }
}
