namespace reviews.Services;

public interface ISurveyCompletionService
{
    Task<bool> ProcessCompletedSurveyAsync(
        string surveyPassword,
        string participantName,
        string rateeName,
        string evalFamiliar,
        string surveySession,
        string surveyTimestamp);
}
