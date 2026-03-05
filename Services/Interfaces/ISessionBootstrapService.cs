using System.Security.Claims;

namespace reviews.Services;

public enum SessionBootstrapReason
{
    None = 0,
    NotAuthenticated,
    UserNotFound,
    ExistingSessionValid,
    RoleAssignedFromParticipantRecord,
    UnsupportedRole,
    ParticipantRecordMissing,
    SessionInitializationFailed,
    SessionRebuilt
}

public sealed record SessionBootstrapResult(
    bool Success,
    bool SessionRebuilt,
    SessionBootstrapReason Reason,
    string? Email = null,
    string? Role = null);

public interface ISessionBootstrapService
{
    Task<SessionBootstrapResult> EnsureInitializedAsync(ClaimsPrincipal principal);
}
