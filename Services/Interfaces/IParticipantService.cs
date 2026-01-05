using reviews.Models;

namespace reviews.Services;

public interface IParticipantService
{
    Task<List<vwAdminParticipants>> GetParticipantsByClientAsync(Guid clientId);
    Task<AdminParticipantViewModel?> GetParticipantDetailsAsync(Guid participantId);
    Task<AdminParticipantViewModel?> GetParticipantDumpAsync(Guid participantId);
    Task<bool> SaveParticipantAsync(Participant participant);
    Task<bool> ToggleParticipantStatusAsync(Guid participantId);
    Task<bool> AddParticipantToEvaluationAsync(Guid participantId, Guid clientId);
    Task<List<vwRatersByRatee>> GetRatersForEvaluationAsync(Guid evaluationId, Guid participantId);
    Task<List<vwEvaluationsToComplete>> GetRateesForEvaluationAsync(Guid evaluationId, Guid participantId);
    Task<List<vwEvaluationsToComplete>> GetEvaluationsToCompleteAsync(Guid participantId, Guid? evaluationId = null, bool activeOnly = false);
    Task<List<vwRatersByRatee>> GetRatersByRateeAsync(Guid participantId, Guid? evaluationId = null, bool activeOnly = false);
    Task<List<ParticipantRole>> GetParticipantRolesAsync();
    Task<List<EligibleRaters>> GetEligibleRatersAsync(Guid evaluationRateeId);
    Task<bool> AddNewRaterAsync(Guid evaluationRateeId, string firstName, string lastName, string email, string roleName, Guid clientId);
    Task<bool> AddExistingRaterAsync(Guid evaluationRateeId, Guid participantId, string roleName);
    Task<bool> DeleteEvaluationParticipantAsync(Guid evaluationParticipantId, Guid rateeId);
}
