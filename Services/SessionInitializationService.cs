using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

public class SessionInitializationService : ISessionInitializationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SessionInitializationService> _logger;

    public SessionInitializationService(
        ApplicationDbContext db,
        ILogger<SessionInitializationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SessionData?> InitializeParticipantSessionAsync(string email)
    {
        try
        {
            var participant = await _db.Participants
                .Include(p => p.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParticipantEmail == email);

            if (participant == null)
            {
                _logger.LogWarning("No participant found for email {Email}", email);
                return null;
            }

            // Get default evaluation
            var defaultEvaluation = await _db.ViewDefaultEvaluationByParticipant
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId);

            var defaultEvaluationId = defaultEvaluation?.EvaluationActive == true
                ? defaultEvaluation.EvaluationId
                : Guid.Empty;

            // Get open ratings count
            var openRatings = await _db.ViewEvaluationsToComplete
                .AsNoTracking()
                .Where(v => v.Rater == participant.ParticipantId 
                    && v.EvaluationParticipantStatusIsActive == 1 
                    && v.EvaluationParticipantStatusIsComplete == 0)
                .CountAsync();

            var sessionData = new SessionData
            {
                UserId = participant.ParticipantId,
                ParticipantId = participant.ParticipantId,
                Email = email,
                Role = "Participant",
                ClientId = participant.ClientId,
                ClientName = participant.Client.ClientName,
                ClientLogoLocation = participant.Client.ClientLogoLocation ?? "/images/Transparent-Logo-Horiz-1.png",
                DefaultEvaluationId = defaultEvaluationId,
                OpenRatingCount = openRatings
            };

            _logger.LogInformation("Initialized participant session for {Email}", email);
            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing participant session for {Email}", email);
            return null;
        }
    }

    public async Task<SessionData?> InitializeClientSessionAsync(string email, string role)
    {
        try
        {
            var clients = await _db.Clients
                .AsNoTracking()
                .ToListAsync();

            var firstClient = clients.FirstOrDefault();

            if (firstClient == null)
            {
                _logger.LogWarning("No clients found in system");
                return null;
            }

            var sessionData = new SessionData
            {
                UserId = Guid.Empty,
                ParticipantId = Guid.Empty,
                Email = email,
                Role = role,
                ClientId = firstClient.ClientId,
                ClientName = firstClient.ClientName,
                ClientLogoLocation = firstClient.ClientLogoLocation ?? "/images/Transparent-Logo-Horiz-1.png",
                DefaultEvaluationId = Guid.Empty,
                OpenRatingCount = -1
            };

            _logger.LogInformation("Initialized {Role} session for {Email}", role, email);
            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing {Role} session for {Email}", role, email);
            return null;
        }
    }

    public async Task<Participant?> GetParticipantByEmailAsync(string email)
    {
        try
        {
            return await _db.Participants
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.ParticipantEmail == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participant by email {Email}", email);
            return null;
        }
    }

    public Task<(string evaluationsText, string ratersText)> GetParticipantMessagesAsync(
        Guid participantId,
        Guid defaultEvaluationId,
        int openRatings)
    {
        var evaluationsText = "";
        var ratersText = "";

        try
        {
            if (defaultEvaluationId != Guid.Empty)
            {
                ratersText = "You have an active 360 review. Select <i>Your Raters</i> on the left to identify your raters.";
            }

            if (openRatings == 1)
            {
                evaluationsText = "You have 1 evaluation to complete. Use the navigation on the left to access it.";
            }
            else if (openRatings > 1)
            {
                evaluationsText = $"You have {openRatings} evaluations to complete. Use the navigation on the left to access them.";
            }
            else
            {
                evaluationsText = "You have no evaluations to complete at this time. Thanks for your attention to this important process.";
            }

            return Task.FromResult((evaluationsText, ratersText));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participant messages for {ParticipantId}", participantId);
            return Task.FromResult(("", ""));
        }
    }
}
