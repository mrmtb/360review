using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

/// <summary>
/// Resolves survey assignment context from a password token
/// </summary>
public interface ISurveyAssignmentResolver
{
    Task<SurveyAssignmentContext?> ResolveAsync(string password, CancellationToken ct = default);
}

public record SurveyAssignmentContext(
    Guid EvaluationParticipantId,
    Guid EvaluationRateeId,
    Guid RaterParticipantId,
    int? ParticipantRoleId,
    Guid EvaluationId,
    Guid ClientId,
    bool IsComplete,
    DateTime? CompletedAt,
    string RateeDisplayName,
    string RoleName,
    bool EvaluationActive,
    DateTime? EvaluationDueDate);

public class SurveyAssignmentResolver : ISurveyAssignmentResolver
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SurveyAssignmentResolver> _logger;

    public SurveyAssignmentResolver(
        ApplicationDbContext db,
        ILogger<SurveyAssignmentResolver> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SurveyAssignmentContext?> ResolveAsync(string password, CancellationToken ct = default)
    {
        _logger.LogInformation("Resolving assignment for password: {Password}", password);

        // Use the existing view which already has all participant data correctly mapped
        // View has: Rater (person completing), ParticipantId (person being rated), etc.
        var assignment = await (from v in _db.ViewEvaluationsToComplete
                                join ep in _db.EvaluationParticipants 
                                    on new { v.EvaluationRateeId, v.Rater } equals new { ep.EvaluationRateeId, Rater = ep.ParticipantId }
                                join e in _db.Evaluations on v.EvaluationId equals e.EvaluationId
                                where v.EvaluationParticipantSurveyPassword == password
                                select new
                                {
                                    EvaluationParticipantId = v.EvaluationRateeId,
                                    v.EvaluationRateeId,
                                    RaterParticipantId = v.Rater,  // RATER (person completing survey)
                                    RateeParticipantId = v.ParticipantId,  // RATEE (person being rated)
                                    v.ParticipantRoleId,
                                    v.EvaluationId,
                                    ClientId = e.ClientId,
                                    IsComplete = v.EvaluationParticipantStatusIsComplete == 1,
                                    CompletedAt = ep.EvaluationParticipantDateCompleted,
                                    RateeDisplayName = v.ParticipantNameFirst + " " + v.ParticipantNameLast,  // RATEE name
                                    RaterDisplayName = v.RaterNameFirst + " " + v.RaterNameLast,  // RATER name
                                    RoleName = v.ParticipantRoleName,
                                    v.EvaluationActive,
                                    EvaluationDueDate = v.DueDate
                                })
                                .FirstOrDefaultAsync(ct);

        if (assignment == null)
        {
            _logger.LogWarning("No assignment found for password: {Password}", password);
            return null;
        }

        _logger.LogInformation(
            "Assignment resolved - EvaluationRateeId: {RateeId}, RaterParticipantId: {RaterParticipantId}, RateeParticipantId: {RateeParticipantId}, RoleId: {RoleId}, RoleName: {RoleName}, IsComplete: {IsComplete}, CompletedAt: {CompletedAt}, Rater: {RaterName}, Ratee: {RateeName}",
            assignment.EvaluationRateeId, assignment.RaterParticipantId, assignment.RateeParticipantId, assignment.ParticipantRoleId, assignment.RoleName, assignment.IsComplete, assignment.CompletedAt, assignment.RaterDisplayName, assignment.RateeDisplayName);

        return new SurveyAssignmentContext(
            EvaluationParticipantId: assignment.EvaluationParticipantId,
            EvaluationRateeId: assignment.EvaluationRateeId,
            RaterParticipantId: assignment.RaterParticipantId,
            ParticipantRoleId: assignment.ParticipantRoleId,
            EvaluationId: assignment.EvaluationId,
            ClientId: assignment.ClientId,
            IsComplete: assignment.IsComplete,
            CompletedAt: assignment.CompletedAt,
            RateeDisplayName: assignment.RateeDisplayName,
            RoleName: assignment.RoleName,
            EvaluationActive: assignment.EvaluationActive,
            EvaluationDueDate: assignment.EvaluationDueDate);
    }
}

