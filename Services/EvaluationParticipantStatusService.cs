using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

/// <summary>
/// Manages evaluation participant status transitions
/// </summary>
public interface IEvaluationParticipantStatusService
{
    Task MarkCompletedAsync(string surveyPassword, CancellationToken ct = default);
    Task MarkOptOutAsync(string surveyPassword, CancellationToken ct = default);
}

public class EvaluationParticipantStatusService : IEvaluationParticipantStatusService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EvaluationParticipantStatusService> _logger;

    public EvaluationParticipantStatusService(
        ApplicationDbContext db,
        ILogger<EvaluationParticipantStatusService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task MarkCompletedAsync(string surveyPassword, CancellationToken ct = default)
    {
        _logger.LogInformation("Marking survey completed for password: {Password}", surveyPassword);

        var participant = await _db.EvaluationParticipants
            .Include(ep => ep.EvaluationParticipantStatus)
            .Where(ep => ep.EvaluationParticipantSurveyPassword == surveyPassword)
            .FirstOrDefaultAsync(ct);

        if (participant == null)
        {
            throw new InvalidOperationException($"No participant found for password: {surveyPassword}");
        }

        if (participant.EvaluationParticipantDateCompleted != null)
        {
            _logger.LogWarning("Participant already marked as completed: {ParticipantId}", participant.ParticipantId);
            return;
        }

        // Get the new status from the state machine
        var currentStatus = participant.EvaluationParticipantStatus;
        if (currentStatus == null)
        {
            throw new InvalidOperationException($"No status found for participant: {participant.ParticipantId}");
        }

        var newStatusId = currentStatus.OnCompletionNewStatus;

        // Update participant
        participant.EvaluationParticipantStatusId = newStatusId;
        participant.EvaluationParticipantDateCompleted = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Status updated from {OldStatus} to {NewStatus} for participant {ParticipantId}",
            currentStatus.EvaluationParticipantStatusName,
            newStatusId,
            participant.ParticipantId);
    }

    public async Task MarkOptOutAsync(string surveyPassword, CancellationToken ct = default)
    {
        _logger.LogInformation("Marking survey opted-out for password: {Password}", surveyPassword);

        var participant = await _db.EvaluationParticipants
            .Include(ep => ep.EvaluationParticipantStatus)
            .Where(ep => ep.EvaluationParticipantSurveyPassword == surveyPassword)
            .FirstOrDefaultAsync(ct);

        if (participant == null)
        {
            throw new InvalidOperationException($"No participant found for password: {surveyPassword}");
        }

        // Get the new status from the state machine
        var currentStatus = participant.EvaluationParticipantStatus;
        if (currentStatus == null)
        {
            throw new InvalidOperationException($"No status found for participant: {participant.ParticipantId}");
        }

        var newStatusId = currentStatus.OnOptOutNewStatus;

        // Update participant
        participant.EvaluationParticipantStatusId = newStatusId;
        // Note: DateCompleted is typically NOT set for opt-outs

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Status updated from {OldStatus} to {NewStatus} for opt-out participant {ParticipantId}",
            currentStatus.EvaluationParticipantStatusName,
            newStatusId,
            participant.ParticipantId);
    }
}
