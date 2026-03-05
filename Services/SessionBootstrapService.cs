using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using reviews.Constants;
using reviews.Models;

namespace reviews.Services;

public class SessionBootstrapService : ISessionBootstrapService
{
    private readonly IReviewSessionService _sessionService;
    private readonly ISessionInitializationService _sessionInitService;
    private readonly IUserRoleService _userRoleService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SessionBootstrapService> _logger;

    public SessionBootstrapService(
        IReviewSessionService sessionService,
        ISessionInitializationService sessionInitService,
        IUserRoleService userRoleService,
        UserManager<ApplicationUser> userManager,
        ILogger<SessionBootstrapService> logger)
    {
        _sessionService = sessionService;
        _sessionInitService = sessionInitService;
        _userRoleService = userRoleService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<SessionBootstrapResult> EnsureInitializedAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return new SessionBootstrapResult(false, false, SessionBootstrapReason.NotAuthenticated);
        }

        var user = await _userManager.GetUserAsync(principal);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Cannot bootstrap session because current user is missing or has no email.");
            return new SessionBootstrapResult(false, false, SessionBootstrapReason.UserNotFound);
        }

        var email = user.Email;
        var role = ResolveRole(principal);
        var existingSession = _sessionService.GetSessionData();

        if (IsSessionUsable(existingSession, email, role))
        {
            return new SessionBootstrapResult(true, false, SessionBootstrapReason.ExistingSessionValid, email, role);
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            var participant = await _sessionInitService.GetParticipantByEmailAsync(email);
            if (participant != null && await _userRoleService.AssignParticipantRoleAsync(user, email))
            {
                role = RoleNames.Participant;
                _logger.LogInformation("Session bootstrap assigned Participant role for {Email}.", email);
            }
        }

        SessionData? newSession = null;

        if (role == RoleNames.Participant)
        {
            var participant = await _sessionInitService.GetParticipantByEmailAsync(email);
            if (participant == null)
            {
                _logger.LogWarning("Cannot bootstrap participant session for {Email}: participant record not found.", email);
                return new SessionBootstrapResult(false, false, SessionBootstrapReason.ParticipantRecordMissing, email, role);
            }

            await _userRoleService.MarkParticipantAsRegisteredAsync(participant.ParticipantId);

            if (user.ParticipantId == Guid.Empty || user.ParticipantId != participant.ParticipantId)
            {
                await _userRoleService.UpdateUserParticipantIdAsync(user, participant.ParticipantId);
            }

            newSession = await _sessionInitService.InitializeParticipantSessionAsync(email);
        }
        else if (role == RoleNames.Admin || role == RoleNames.Client)
        {
            newSession = await _sessionInitService.InitializeClientSessionAsync(email, role);
        }
        else
        {
            return new SessionBootstrapResult(false, false, SessionBootstrapReason.UnsupportedRole, email, role);
        }

        if (newSession == null)
        {
            _logger.LogWarning("Session bootstrap failed for {Email} with role {Role}.", email, role);
            return new SessionBootstrapResult(false, false, SessionBootstrapReason.SessionInitializationFailed, email, role);
        }

        _sessionService.SetSessionData(newSession);
        return new SessionBootstrapResult(true, true, SessionBootstrapReason.SessionRebuilt, email, role);
    }

    private static string ResolveRole(ClaimsPrincipal principal)
    {
        if (principal.IsInRole(RoleNames.Admin))
        {
            return RoleNames.Admin;
        }

        if (principal.IsInRole(RoleNames.Client))
        {
            return RoleNames.Client;
        }

        if (principal.IsInRole(RoleNames.Participant))
        {
            return RoleNames.Participant;
        }

        return string.Empty;
    }

    private static bool IsSessionUsable(SessionData? sessionData, string email, string role)
    {
        if (sessionData == null || sessionData.ClientId == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sessionData.Email) ||
            !string.Equals(sessionData.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(role) &&
            !string.Equals(sessionData.Role, role, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(role, RoleNames.Participant, StringComparison.OrdinalIgnoreCase) &&
            sessionData.ParticipantId == Guid.Empty)
        {
            return false;
        }

        return true;
    }
}
