using reviews.Models;

namespace reviews.Services;

public interface IEvaluationService
{
    Task<List<Evaluation>> GetEvaluationsByClientAsync(Guid clientId);
    Task<Evaluation?> GetEvaluationDetailsAsync(Guid evaluationId);
    Task<bool> SaveEvaluationAsync(Evaluation model, Guid clientId);
    Task<List<vwAdminRatees>> GetRateesByClientAsync(Guid clientId);
    Evaluation CreateNewEvaluation(Guid clientId);
}
