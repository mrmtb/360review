using reviews.Models;

namespace reviews.Services;

public interface ISessionInitializationService
{
    Task<SessionData?> InitializeParticipantSessionAsync(string email);
    Task<SessionData?> InitializeClientSessionAsync(string email, string role);
    Task<Participant?> GetParticipantByEmailAsync(string email);
    Task<(string evaluationsText, string ratersText)> GetParticipantMessagesAsync(Guid participantId, Guid defaultEvaluationId, int openRatings);
}
