using reviews.Models;

namespace reviews.Services;

public interface ISurveyUrlBuilder
{
    string BuildSurveyUrl(vwEvaluationsToComplete evaluation);
    string GetSiteBaseUrl();
}
