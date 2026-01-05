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

        var surveyUrl = $"{siteUrl}{evaluation.EvaluationFolder}?P1=EL0&" +
                       $"Q0001={evaluation.EvaluationParticipantSurveyPassword}&" +
                       $"Q0002={evaluation.RaterNameFirst}+{evaluation.RaterNameLast}&" +
                       $"Q0003={evaluation.ParticipantNameFirst}+{evaluation.ParticipantNameLast}&" +
                       $"Q0004={evaluation.ParticipantRoleId}&" +
                       $"Q0005={evaluation.EvaluationName}";

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
