using reviews.Models;

namespace reviews.Services;

public interface IEvaluationService
{
    Task<List<Evaluation>> GetEvaluationsByClientAsync(Guid clientId);
    Task<Evaluation?> GetEvaluationDetailsAsync(Guid evaluationId);
    Task<bool> SaveEvaluationAsync(Evaluation model, Guid clientId, string? surveyTemplatePath = null);
    Task<List<vwAdminRatees>> GetRateesByClientAsync(Guid clientId);
    Task<List<SurveyVersionOption>> GetSurveyVersionOptionsAsync(Guid clientId);
    Task<SurveyManagementViewModel?> GetSurveyManagementAsync(Guid evaluationId, Guid clientId);
    Task<SurveyVersionUsageViewModel?> GetSurveyVersionUsageAsync(Guid evaluationId, Guid surveyDefinitionVersionId, Guid clientId, string usageType);
    Task<SurveyVersionDetailViewModel?> GetSurveyVersionDetailAsync(Guid evaluationId, Guid surveyDefinitionVersionId, Guid clientId);
    Task<SurveyActionResult> SaveSurveyVersionDraftAsync(Guid surveyDefinitionVersionId, Guid clientId, string draftJson, string editedBy);
    Task<SurveyActionResult> PublishSurveyVersionAsync(Guid surveyDefinitionVersionId, Guid clientId, string publishedBy);
    Task<SurveyActionResult> SetCurrentSurveyVersionAsync(Guid evaluationId, Guid surveyDefinitionVersionId, Guid clientId, bool forceSwitch = false);
    Task<SurveyActionResult> UpdateSurveyVersionTemplatePathAsync(Guid surveyDefinitionVersionId, Guid clientId, string templatePath);
    Task<SurveyActionResult> CreateNextSurveyVersionAsync(Guid evaluationId, Guid clientId);
    Task<SurveyActionResult> CreateNewSurveyAsync(Guid evaluationId, Guid clientId);
    Evaluation CreateNewEvaluation(Guid clientId);
}

public record SurveyVersionOption(
    Guid SurveyDefinitionVersionId,
    string DisplayName,
    string TemplatePath);

public record SurveyActionResult(
    bool Success,
    string Message,
    string Severity = "success");
