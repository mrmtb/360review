using reviews.Models;

namespace reviews.Services;

public interface IInvitationService
{
    Task<List<vwReminders>> GetRemindersByClientAsync(Guid clientId);
    Task<bool> ProcessRemindersAsync(List<Guid> participantIds, Guid clientId, string userEmail);
    Task<List<vwInvitations>> GetInvitationsByClientAsync(Guid clientId);
    Task<bool> ProcessInvitationsAsync(List<(Guid EvaluationRateeId, Guid ParticipantId)>? participantTuples, Guid clientId, string userEmail);
}
