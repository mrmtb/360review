using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Constants;
using reviews.Models;

namespace reviews.Services;

public class SurveyCompletionService : ISurveyCompletionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SurveyCompletionService> _logger;

    public SurveyCompletionService(ApplicationDbContext db, ILogger<SurveyCompletionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> ProcessCompletedSurveyAsync(
        string surveyPassword,
        string participantName,
        string rateeName,
        string evalFamiliar,
        string surveySession,
        string surveyTimestamp)
    {
        try
        {
            var evaluationParticipants = await _db.EvaluationParticipants
                .Where(ep => ep.EvaluationParticipantSurveyPassword == surveyPassword)
                .ToListAsync();

            // If multiple participants found, try to narrow down
            if (evaluationParticipants.Count > 1)
            {
                var nameParts = participantName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (nameParts.Length >= 2)
                {
                    var firstName = nameParts[0];
                    var lastName = nameParts[^1]; // Last element

                    var specificParticipantIds = await _db.Participants
                        .Where(p => p.ParticipantNameFirst == firstName && p.ParticipantNameLast == lastName)
                        .Select(p => p.ParticipantId)
                        .ToListAsync();

                    if (specificParticipantIds.Any())
                    {
                        evaluationParticipants = evaluationParticipants
                            .Where(ep => specificParticipantIds.Contains(ep.ParticipantId))
                            .ToList();
                    }
                }

                if (evaluationParticipants.Count > 1)
                {
                    _logger.LogWarning(
                        "Multiple evaluation participants found with password {Password}. Participant: {Participant}, Ratee: {Ratee}, Session: {Session}",
                        surveyPassword, participantName, rateeName, surveySession);
                    return false;
                }
            }

            if (!evaluationParticipants.Any())
            {
                _logger.LogWarning(
                    "No evaluation participant found with password {Password}. Participant: {Participant}, Ratee: {Ratee}, Session: {Session}",
                    surveyPassword, participantName, rateeName, surveySession);
                return false;
            }

            // Process the single participant
            var participant = evaluationParticipants.First();
            
            // Determine status based on eval_familiar value
            // If eval_familiar == "2", they completed the survey
            // Otherwise, they indicated they are unfamiliar
            var statusName = evalFamiliar == "2" 
                ? StatusNames.Completed 
                : StatusNames.Unfamiliar;

            var status = await _db.EvaluationParticipantStatuses
                .FirstOrDefaultAsync(s => s.EvaluationParticipantStatusName == statusName);

            if (status != null)
            {
                participant.EvaluationParticipantStatusId = status.EvaluationParticipantStatusId;
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Survey completed. ParticipantId: {ParticipantId}, Status: {Status}, Session: {Session}",
                    participant.ParticipantId, statusName, surveySession);
                
                return true;
            }
            else
            {
                _logger.LogError("Status not found: {StatusName}", statusName);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing completed survey. Password: {Password}, Participant: {Participant}",
                surveyPassword, participantName);
            return false;
        }
    }
}
