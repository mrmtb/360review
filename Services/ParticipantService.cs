using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Constants;
using reviews.Models;

namespace reviews.Services;

public class ParticipantService : IParticipantService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ParticipantService> _logger;

    public ParticipantService(ApplicationDbContext db, ILogger<ParticipantService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<vwAdminParticipants>> GetParticipantsByClientAsync(Guid clientId)
    {
        return await _db.ViewAdminParticipants
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<List<AdminPersonRow>> GetAdminPeopleByClientAsync(Guid clientId)
    {
        var now = DateTime.UtcNow;
        var terminalStatusIds = new HashSet<int>
        {
            EvaluationParticipantStatusIds.Completed,
            EvaluationParticipantStatusIds.OptOut,
            EvaluationParticipantStatusIds.Unfamiliar,
            EvaluationParticipantStatusIds.Deleted
        };

        var participants = await _db.Participants
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .Select(p => new
            {
                p.ParticipantId,
                p.ParticipantNameFirst,
                p.ParticipantNameLast,
                p.ParticipantEmail,
                p.ParticipantIsActive
            })
            .ToListAsync();

        var rateeFlags = await _db.EvaluationRatees
            .AsNoTracking()
            .Where(er => er.Participants.ClientId == clientId)
            .Select(er => new
            {
                RateeId = er.ParticipantId,
                IsActiveCluster =
                    (er.Evaluations.EvaluationRateeDueDate.HasValue && er.Evaluations.EvaluationRateeDueDate.Value >= now) ||
                    (er.Evaluations.EvaluationRaterDueDate.HasValue && er.Evaluations.EvaluationRaterDueDate.Value >= now) ||
                    (er.EvaluationRateeDueDateOverride.HasValue && er.EvaluationRateeDueDateOverride.Value >= now) ||
                    (er.EvaluationRaterDueDateOverride.HasValue && er.EvaluationRaterDueDateOverride.Value >= now),
                er.EvaluationRateeDateAdded
            })
            .ToListAsync();

        var participantFlags = await _db.EvaluationParticipants
            .AsNoTracking()
            .Where(ep => ep.Participants.ClientId == clientId)
            .Select(ep => new
            {
                ep.ParticipantId,
                StatusId = ep.EvaluationParticipantStatusId ?? 0,
                ep.EvaluationParticipantDateLastEmailed,
                IsActiveCluster =
                    (ep.EvaluationRatee.Evaluations.EvaluationRateeDueDate.HasValue && ep.EvaluationRatee.Evaluations.EvaluationRateeDueDate.Value >= now) ||
                    (ep.EvaluationRatee.Evaluations.EvaluationRaterDueDate.HasValue && ep.EvaluationRatee.Evaluations.EvaluationRaterDueDate.Value >= now) ||
                    (ep.EvaluationRatee.EvaluationRateeDueDateOverride.HasValue && ep.EvaluationRatee.EvaluationRateeDueDateOverride.Value >= now) ||
                    (ep.EvaluationRatee.EvaluationRaterDueDateOverride.HasValue && ep.EvaluationRatee.EvaluationRaterDueDateOverride.Value >= now)
            })
            .ToListAsync();

        var activeRateeSet = rateeFlags
            .Where(x => x.IsActiveCluster)
            .Select(x => x.RateeId)
            .ToHashSet();

        var activeParticipantSet = participantFlags
            .Where(x => x.IsActiveCluster)
            .Select(x => x.ParticipantId)
            .ToHashSet();

        var openItemsSet = participantFlags
            .Where(x => x.IsActiveCluster && !terminalStatusIds.Contains(x.StatusId))
            .Select(x => x.ParticipantId)
            .ToHashSet();

        var lastEmailedByParticipant = participantFlags
            .Where(x => x.IsActiveCluster && x.EvaluationParticipantDateLastEmailed.HasValue)
            .GroupBy(x => x.ParticipantId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.EvaluationParticipantDateLastEmailed));

        var rateeDateAddedByParticipant = rateeFlags
            .Where(x => x.IsActiveCluster && x.EvaluationRateeDateAdded.HasValue)
            .GroupBy(x => x.RateeId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.EvaluationRateeDateAdded));

        return participants
            .Select(p => new AdminPersonRow
            {
                ParticipantId = p.ParticipantId,
                ParticipantNameFirst = p.ParticipantNameFirst,
                ParticipantNameLast = p.ParticipantNameLast,
                ParticipantEmail = p.ParticipantEmail,
                ParticipantIsActive = p.ParticipantIsActive,
                IsRatee = activeRateeSet.Contains(p.ParticipantId),
                IsParticipant = activeParticipantSet.Contains(p.ParticipantId),
                IsInActiveEvaluation = activeRateeSet.Contains(p.ParticipantId) || activeParticipantSet.Contains(p.ParticipantId),
                HasOpenItems = openItemsSet.Contains(p.ParticipantId),
                LastEmailedAt = lastEmailedByParticipant.GetValueOrDefault(p.ParticipantId),
                RateeDateAdded = rateeDateAddedByParticipant.GetValueOrDefault(p.ParticipantId)
            })
            .OrderBy(p => p.ParticipantNameLast)
            .ThenBy(p => p.ParticipantNameFirst)
            .ToList();
    }

    public async Task<AdminParticipantViewModel?> GetParticipantDetailsAsync(Guid participantId)
    {
        var participant = await _db.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

        if (participant == null)
        {
            return null;
        }

        var evaluations = await _db.ViewEvaluationsByRatees
            .AsNoTracking()
            .Where(p => p.ParticipantId == participantId)
            .ToListAsync();

        var activeEvaluations = await _db.Evaluations
            .AsNoTracking()
            .Where(e => e.ClientId == participant.ClientId && e.EvaluationActive)
            .OrderByDescending(e => e.EvaluationOpenDate ?? DateTime.MinValue)
            .ToListAsync();

        return new AdminParticipantViewModel
        {
            Participant = participant,
            Raters = new List<vwRatersByRatee>(),
            Ratees = new List<vwEvaluationsToComplete>(),
            Evaluations = evaluations,
            ActiveEvaluations = activeEvaluations
        };
    }

    public async Task<AdminParticipantViewModel?> GetParticipantDumpAsync(Guid participantId)
    {
        var participant = await _db.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

        if (participant == null)
        {
            return null;
        }

        return new AdminParticipantViewModel
        {
            Participant = participant,
            Raters = await _db.ViewRatersByRatees
                .AsNoTracking()
                .Where(p => p.Ratee == participantId)
                .ToListAsync(),
            Ratees = await _db.ViewEvaluationsToComplete
                .AsNoTracking()
                .Where(p => p.ParticipantId == participantId)
                .ToListAsync(),
            Evaluations = await _db.ViewEvaluationsByRatees
                .AsNoTracking()
                .Where(p => p.ParticipantId == participantId)
                .ToListAsync()
        };
    }

    public async Task<bool> SaveParticipantAsync(Participant participant)
    {
        try
        {
            _db.Participants.Update(participant);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save participant {ParticipantId}", participant.ParticipantId);
            return false;
        }
    }

    public async Task<bool> ToggleParticipantStatusAsync(Guid participantId)
    {
        try
        {
            var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantId == participantId);
            if (participant == null)
            {
                _logger.LogWarning("Participant {ParticipantId} not found for toggle", participantId);
                return false;
            }

            participant.ParticipantIsActive = !participant.ParticipantIsActive;
            _db.Participants.Update(participant);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle participant {ParticipantId}", participantId);
            return false;
        }
    }

    public async Task<bool> AddParticipantToEvaluationAsync(Guid participantId, Guid clientId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var activeEvaluation = await _db.Evaluations
                .Where(e => e.ClientId == clientId && e.EvaluationActive)
                .Select(e => e.EvaluationId)
                .FirstOrDefaultAsync();

            if (activeEvaluation == Guid.Empty)
            {
                _logger.LogWarning("No active evaluation found for client {ClientId}", clientId);
                return false;
            }

            var draftStatusId = await _db.EvaluationRateeStatuses
                .Where(e => e.EvaluationRateeStatusName == StatusNames.Draft)
                .Select(e => e.EvaluationRateeStatusId)
                .FirstOrDefaultAsync();

            var selfRoleId = await _db.ParticipantRoles
                .Where(r => r.ParticipantRoleName == ParticipantRoleNames.Self)
                .Select(r => r.ParticipantRoleId)
                .FirstOrDefaultAsync();

            var participantStatusId = await _db.EvaluationParticipantStatuses
                .Where(e => e.EvaluationParticipantStatusName == EvaluationParticipantStatusNames.InvitationNotSent)
                .Select(e => e.EvaluationParticipantStatusId)
                .FirstOrDefaultAsync();

            if (draftStatusId == 0)
            {
                _logger.LogError("Unable to resolve EvaluationRatee draft status id.");
                return false;
            }

            if (selfRoleId == 0)
            {
                _logger.LogError("Unable to resolve Self participant role id.");
                return false;
            }

            // Fallback to known seeded id if lookup by name returns no row.
            if (participantStatusId == 0)
            {
                participantStatusId = EvaluationParticipantStatusIds.InvitationNotSent;
            }

            var participantStatusExists = await _db.EvaluationParticipantStatuses
                .AnyAsync(s => s.EvaluationParticipantStatusId == participantStatusId);

            if (!participantStatusExists)
            {
                _logger.LogError(
                    "Unable to resolve valid EvaluationParticipant status id. Candidate id: {StatusId}",
                    participantStatusId);
                return false;
            }

            // Create EvaluationRatee
            var evaluationRatee = new EvaluationRatee
            {
                EvaluationRateeId = Guid.NewGuid(),
                EvaluationId = activeEvaluation,
                ParticipantId = participantId,
                EvaluationRateeStatusId = draftStatusId,
                EvaluationRateeDateAdded = DateTime.UtcNow
            };
            _db.EvaluationRatees.Add(evaluationRatee);
            await _db.SaveChangesAsync();

            // Create EvaluationParticipant
            var evaluationParticipant = new EvaluationParticipant
            {
                EvaluationRateeId = evaluationRatee.EvaluationRateeId,
                ParticipantId = participantId,
                ParticipantRoleId = selfRoleId,
                EvaluationParticipantStatusId = participantStatusId,
                EvaluationParticipantSurveyPassword = RandomValueGenerator.GenerateRandomValue(),
                EvaluationParticipantDateAdded = DateTime.UtcNow
            };
            _db.EvaluationParticipants.Add(evaluationParticipant);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to add participant {ParticipantId} to evaluation", participantId);
            return false;
        }
    }

    public async Task<(bool Success, string Message)> MoveRateeToActiveEvaluationAsync(
        Guid participantId,
        Guid fromEvaluationId,
        Guid toEvaluationId,
        Guid clientId)
    {
        if (participantId == Guid.Empty || fromEvaluationId == Guid.Empty || toEvaluationId == Guid.Empty)
        {
            return (false, "A valid participant and evaluation selection is required.");
        }

        if (fromEvaluationId == toEvaluationId)
        {
            return (false, "Select a different active evaluation phase.");
        }

        try
        {
            var sourceRatee = await _db.EvaluationRatees
                .Include(er => er.Evaluations)
                .FirstOrDefaultAsync(er =>
                    er.ParticipantId == participantId &&
                    er.EvaluationId == fromEvaluationId &&
                    er.Evaluations.ClientId == clientId);

            if (sourceRatee == null)
            {
                return (false, "The selected source evaluation record was not found.");
            }

            var targetEvaluation = await _db.Evaluations
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EvaluationId == toEvaluationId &&
                    e.ClientId == clientId &&
                    e.EvaluationActive);

            if (targetEvaluation == null)
            {
                return (false, "Target evaluation must be active and belong to the current client.");
            }

            var alreadyInTarget = await _db.EvaluationRatees
                .AsNoTracking()
                .AnyAsync(er => er.ParticipantId == participantId && er.EvaluationId == toEvaluationId);

            if (alreadyInTarget)
            {
                return (false, "This participant is already a ratee in the selected active evaluation.");
            }

            sourceRatee.EvaluationId = toEvaluationId;
            await _db.SaveChangesAsync();

            return (true, "Participant moved to the selected active evaluation phase.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed moving participant {ParticipantId} from evaluation {FromEvaluationId} to {ToEvaluationId}",
                participantId,
                fromEvaluationId,
                toEvaluationId);
            return (false, "Unable to move participant to the selected evaluation phase.");
        }
    }

    public async Task<List<vwRatersByRatee>> GetRatersForEvaluationAsync(Guid evaluationId, Guid participantId)
    {
        return await _db.ViewRatersByRatees
            .AsNoTracking()
            .Where(r => r.EvaluationId == evaluationId && r.Ratee == participantId)
            .ToListAsync();
    }

    public async Task<List<vwEvaluationsToComplete>> GetRateesForEvaluationAsync(Guid evaluationId, Guid participantId)
    {
        return await _db.ViewEvaluationsToComplete
            .AsNoTracking()
            .Where(r => r.EvaluationId == evaluationId && r.Rater == participantId)
            .ToListAsync();
    }

    public async Task<List<vwEvaluationsToComplete>> GetEvaluationsToCompleteAsync(
        Guid participantId, 
        Guid? evaluationId = null, 
        bool activeOnly = false)
    {
        var query = _db.ViewEvaluationsToComplete
            .AsNoTracking()
            .Where(p => p.Rater == participantId);

        if (evaluationId.HasValue)
        {
            query = query.Where(p => p.EvaluationId == evaluationId.Value);
        }

        if (activeOnly)
        {
            query = query.Where(p => p.EvaluationActive == true);
        }

        return await query.ToListAsync();
    }

    public async Task<List<vwRatersByRatee>> GetRatersByRateeAsync(
        Guid rateeId, 
        Guid? evaluationId = null, 
        bool activeOnly = false)
    {
        var query = _db.ViewRatersByRatees
            .AsNoTracking()
            .Where(p => p.Ratee == rateeId);

        if (evaluationId.HasValue)
        {
            query = query.Where(p => p.EvaluationId == evaluationId.Value);
        }

        if (activeOnly)
        {
            query = query.Where(p => p.EvaluationActive == true);
        }

        return await query.ToListAsync();
    }

    public async Task<List<ParticipantRole>> GetParticipantRolesAsync()
    {
        return await _db.ParticipantRoles
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<EligibleRaters>> GetEligibleRatersAsync(Guid evaluationRateeId)
    {
        try
        {
            var result = new List<EligibleRaters>();
            
            // Use ADO.NET directly to bypass EF Core mapping issues
            var connection = _db.Database.GetDbConnection();
            var needToClose = connection.State != System.Data.ConnectionState.Open;
            
            if (needToClose)
                await connection.OpenAsync();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM public.\"fnEligibleRaters\"(@p0)";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p0";
                parameter.Value = evaluationRateeId;
                command.Parameters.Add(parameter);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new EligibleRaters
                    {
                        EvaluationRateeId = reader.GetGuid(reader.GetOrdinal("EvaluationRateeId")),
                        ParticipantId = reader.GetGuid(reader.GetOrdinal("ParticipantId")),
                        ParticipantNameFirst = reader.GetString(reader.GetOrdinal("ParticipantNameFirst")),
                        ParticipantNameLast = reader.GetString(reader.GetOrdinal("ParticipantNameLast")),
                        ParticipantEmail = reader.GetString(reader.GetOrdinal("ParticipantEmail")),
                        ParticipantHasRegistered = reader.GetBoolean(reader.GetOrdinal("ParticipantHasRegistered"))
                    });
                }
            }
            finally
            {
                if (needToClose)
                    await connection.CloseAsync();
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get eligible raters for EvaluationRateeId {EvaluationRateeId}", evaluationRateeId);
            return new List<EligibleRaters>();
        }
    }

    public async Task<bool> AddNewRaterAsync(
        Guid evaluationRateeId, 
        string firstName, 
        string lastName, 
        string email, 
        string roleName, 
        Guid clientId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        
        try
        {
            // Create new participant
            var newRater = new Participant
            {
                ParticipantId = Guid.NewGuid(),
                ParticipantNameFirst = firstName,
                ParticipantNameLast = lastName,
                ParticipantEmail = email,
                ClientId = clientId
            };

            _db.Participants.Add(newRater);
            await _db.SaveChangesAsync();

            // Add to evaluation
            var role = await _db.ParticipantRoles
                .FirstOrDefaultAsync(r => r.ParticipantRoleName == roleName);

            if (role == null)
            {
                _logger.LogWarning("Role not found: {RoleName}", roleName);
                await transaction.RollbackAsync();
                return false;
            }

            var newEvaluationParticipant = new EvaluationParticipant
            {
                EvaluationRateeId = evaluationRateeId,
                ParticipantId = newRater.ParticipantId,
                ParticipantRoleId = role.ParticipantRoleId,
                EvaluationParticipantStatusId = EvaluationParticipantStatusIds.InvitationNotSent,
                EvaluationParticipantSurveyPassword = RandomValueGenerator.GenerateRandomValue(),
                EvaluationParticipantDateAdded = DateTime.UtcNow
            };

            _db.EvaluationParticipants.Add(newEvaluationParticipant);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Added new rater {ParticipantId} to evaluation ratee {EvaluationRateeId}", 
                newRater.ParticipantId, evaluationRateeId);
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to add new rater to evaluation ratee {EvaluationRateeId}", evaluationRateeId);
            return false;
        }
    }

    public async Task<bool> AddExistingRaterAsync(
        Guid evaluationRateeId, 
        Guid participantId, 
        string roleName)
    {
        try
        {
            var role = await _db.ParticipantRoles
                .FirstOrDefaultAsync(r => r.ParticipantRoleName == roleName);

            if (role == null)
            {
                _logger.LogWarning("Role not found: {RoleName}", roleName);
                return false;
            }

            var newEvaluationParticipant = new EvaluationParticipant
            {
                EvaluationRateeId = evaluationRateeId,
                ParticipantId = participantId,
                ParticipantRoleId = role.ParticipantRoleId,
                EvaluationParticipantStatusId = EvaluationParticipantStatusIds.InvitationNotSent,
                EvaluationParticipantSurveyPassword = RandomValueGenerator.GenerateRandomValue(),
                EvaluationParticipantDateAdded = DateTime.UtcNow
            };

            _db.EvaluationParticipants.Add(newEvaluationParticipant);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Added existing rater {ParticipantId} to evaluation ratee {EvaluationRateeId}", 
                participantId, evaluationRateeId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add existing rater {ParticipantId} to evaluation ratee {EvaluationRateeId}", 
                participantId, evaluationRateeId);
            return false;
        }
    }

    public async Task<bool> DeleteEvaluationParticipantAsync(
        Guid evaluationRateeId, 
        Guid participantId)
    {
        try
        {
            var evaluationParticipant = await _db.EvaluationParticipants
                .FirstOrDefaultAsync(ep => ep.EvaluationRateeId == evaluationRateeId 
                    && ep.ParticipantId == participantId);

            if (evaluationParticipant == null)
            {
                _logger.LogWarning("EvaluationParticipant not found for EvaluationRateeId {EvaluationRateeId} and ParticipantId {ParticipantId}", 
                    evaluationRateeId, participantId);
                return false;
            }

            _db.EvaluationParticipants.Remove(evaluationParticipant);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted evaluation participant {ParticipantId} from evaluation ratee {EvaluationRateeId}", 
                participantId, evaluationRateeId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete evaluation participant {ParticipantId} from evaluation ratee {EvaluationRateeId}", 
                participantId, evaluationRateeId);
            return false;
        }
    }
}
