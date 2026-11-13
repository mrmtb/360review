using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace reviews.Services;

public class EvaluationService : IEvaluationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(ApplicationDbContext db, ILogger<EvaluationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Evaluation>> GetEvaluationsByClientAsync(Guid clientId)
    {
        return await _db.Evaluations
            .Include(e => e.Client)
            .Include(e => e.CurrentSurveyDefinitionVersion)
                .ThenInclude(v => v!.SurveyDefinition)
            .AsNoTracking()
            .Where(r => r.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<Evaluation?> GetEvaluationDetailsAsync(Guid evaluationId)
    {
        return await _db.Evaluations
            .Include(e => e.Client)
            .Include(e => e.CurrentSurveyDefinitionVersion)
                .ThenInclude(v => v!.SurveyDefinition)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId);
    }

    public async Task<bool> SaveEvaluationAsync(Evaluation model, Guid clientId, string? surveyTemplatePath = null)
    {
        try
        {
            // Authorization check
            if (model.ClientId != clientId)
            {
                _logger.LogWarning("Unauthorized attempt to modify evaluation for different client");
                return false;
            }

            if (model.CurrentSurveyDefinitionVersionId.HasValue &&
                !await _db.SurveyDefinitionVersions.AnyAsync(version =>
                    version.SurveyDefinitionVersionId == model.CurrentSurveyDefinitionVersionId &&
                    version.SurveyDefinition!.ClientId == clientId &&
                    version.SurveyDefinition.IsActive && version.IsPublished))
            {
                return false;
            }

            var evaluation = await _db.Evaluations
                .Include(e => e.CurrentSurveyDefinitionVersion)
                .FirstOrDefaultAsync(e => e.EvaluationId == model.EvaluationId && e.ClientId == clientId);

            if (evaluation == null)
            {
                evaluation = new Evaluation
                {
                    EvaluationId = model.EvaluationId,
                    ClientId = model.ClientId
                };
            }

            // Update properties
            var properties = typeof(Evaluation).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name == "EvaluationId" || prop.Name == "ClientId") continue;
                if (!IsPersistedScalar(prop.PropertyType)) continue;

                var newValue = prop.GetValue(model);
                var oldValue = prop.GetValue(evaluation);

                if (prop.PropertyType == typeof(DateTime?) && newValue != null)
                {
                    var dateValue = (DateTime?)newValue;
                    if (dateValue.HasValue)
                    {
                        // Convert to UTC for PostgreSQL timestamp with time zone
                        var utcValue = dateValue.Value.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(dateValue.Value, DateTimeKind.Utc)
                            : dateValue.Value.ToUniversalTime();

                        if (!oldValue?.Equals(utcValue) ?? true)
                        {
                            prop.SetValue(evaluation, utcValue);
                        }
                    }
                }
                else if ((oldValue == null && newValue != null) || (oldValue != null && !oldValue.Equals(newValue)))
                {
                    prop.SetValue(evaluation, newValue);
                }
            }

            if (!string.IsNullOrWhiteSpace(surveyTemplatePath) &&
                evaluation.CurrentSurveyDefinitionVersion != null &&
                !string.Equals(evaluation.CurrentSurveyDefinitionVersion.TemplatePath, surveyTemplatePath, StringComparison.Ordinal))
            {
                if (evaluation.CurrentSurveyDefinitionVersion.IsPublished)
                {
                    return false;
                }

                if (!TryNormalizeTemplatePath(surveyTemplatePath, out var normalizedTemplatePath, out _))
                {
                    _logger.LogWarning("Rejected invalid survey template path for evaluation {EvaluationId}: {TemplatePath}", model.EvaluationId, surveyTemplatePath);
                    return false;
                }

                evaluation.CurrentSurveyDefinitionVersion.TemplatePath = normalizedTemplatePath;
            }

            if (evaluation.EvaluationId == Guid.Empty)
            {
                _db.Evaluations.Add(evaluation);
            }
            else
            {
                _db.Evaluations.Update(evaluation);
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save evaluation {EvaluationId}", model.EvaluationId);
            return false;
        }
    }

    public async Task<List<vwAdminRatees>> GetRateesByClientAsync(Guid clientId)
    {
        return await _db.ViewAdminRatees
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .ToListAsync();
    }

    public Evaluation CreateNewEvaluation(Guid clientId)
    {
        var currentSurveyVersionId = _db.SurveyDefinitionVersions
            .AsNoTracking()
            .Where(v => v.SurveyDefinition!.ClientId == clientId && v.IsPublished)
            .OrderBy(v => v.SurveyDefinition!.SurveyDefinitionName)
            .ThenByDescending(v => v.VersionNumber)
            .Select(v => (Guid?)v.SurveyDefinitionVersionId)
            .FirstOrDefault();

        return new Evaluation
        {
            EvaluationId = Guid.Empty,
            ClientId = clientId,
            CurrentSurveyDefinitionVersionId = currentSurveyVersionId
        };
    }

    public async Task<List<SurveyVersionOption>> GetSurveyVersionOptionsAsync(Guid clientId)
    {
        return await _db.SurveyDefinitionVersions
            .AsNoTracking()
            .Include(v => v.SurveyDefinition)
            .Where(v => v.SurveyDefinition!.ClientId == clientId && v.IsPublished && v.SurveyDefinition.IsActive)
            .OrderBy(v => v.SurveyDefinition!.SurveyDefinitionName)
            .ThenByDescending(v => v.VersionNumber)
            .Select(v => new SurveyVersionOption(
                v.SurveyDefinitionVersionId,
                $"{v.SurveyDefinition!.SurveyDefinitionName} v{v.VersionNumber}",
                v.TemplatePath))
            .ToListAsync();
    }

    public async Task<SurveyManagementViewModel?> GetSurveyManagementAsync(Guid evaluationId, Guid clientId)
    {
        var evaluation = await _db.Evaluations
            .AsNoTracking()
            .Include(e => e.CurrentSurveyDefinitionVersion)
                .ThenInclude(v => v!.SurveyDefinition)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId && e.ClientId == clientId);

        if (evaluation == null)
        {
            return null;
        }

        var versionRows = await _db.SurveyDefinitionVersions
            .AsNoTracking()
            .Include(v => v.SurveyDefinition)
            .Where(v => v.SurveyDefinition!.ClientId == clientId && v.SurveyDefinition.IsActive)
            .OrderBy(v => v.SurveyDefinition!.SurveyDefinitionName)
            .ThenByDescending(v => v.VersionNumber)
            .Select(v => new SurveyManagementVersionRow
            {
                SurveyDefinitionVersionId = v.SurveyDefinitionVersionId,
                SurveyDefinitionId = v.SurveyDefinitionId,
                SurveyDefinitionName = v.SurveyDefinition!.SurveyDefinitionName,
                VersionNumber = v.VersionNumber,
                TemplateKey = v.TemplateKey,
                TemplatePath = v.TemplatePath,
                IsPublished = v.IsPublished,
                IsCurrentForEvaluation = v.SurveyDefinitionVersionId == evaluation.CurrentSurveyDefinitionVersionId,
                CreatedAt = v.CreatedAt,
                PublishedAt = v.PublishedAt
            })
            .ToListAsync();

        var evaluationRateeIds = await _db.EvaluationRatees
            .AsNoTracking()
            .Where(er => er.EvaluationId == evaluationId)
            .Select(er => er.EvaluationRateeId)
            .ToListAsync();

        var pinnedCounts = await _db.EvaluationParticipants
            .AsNoTracking()
            .Where(ep => evaluationRateeIds.Contains(ep.EvaluationRateeId) && ep.AssignedSurveyDefinitionVersionId != null)
            .GroupBy(ep => ep.AssignedSurveyDefinitionVersionId!.Value)
            .Select(g => new { SurveyDefinitionVersionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SurveyDefinitionVersionId, x => x.Count);

        var startedCounts = await _db.EvaluationParticipantResponses
            .AsNoTracking()
            .Where(r => evaluationRateeIds.Contains(r.EvaluationRateeId) && r.SurveyDefinitionVersionId != null)
            .GroupBy(r => r.SurveyDefinitionVersionId!.Value)
            .Select(g => new { SurveyDefinitionVersionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SurveyDefinitionVersionId, x => x.Count);

        var submittedCounts = await _db.EvaluationParticipantResponses
            .AsNoTracking()
            .Where(r => evaluationRateeIds.Contains(r.EvaluationRateeId) && r.SurveyDefinitionVersionId != null && r.IsSubmitted)
            .GroupBy(r => r.SurveyDefinitionVersionId!.Value)
            .Select(g => new { SurveyDefinitionVersionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SurveyDefinitionVersionId, x => x.Count);

        foreach (var version in versionRows)
        {
            version.PinnedParticipantCount = pinnedCounts.GetValueOrDefault(version.SurveyDefinitionVersionId);
            version.StartedResponseCount = startedCounts.GetValueOrDefault(version.SurveyDefinitionVersionId);
            version.SubmittedResponseCount = submittedCounts.GetValueOrDefault(version.SurveyDefinitionVersionId);
        }

        return new SurveyManagementViewModel
        {
            EvaluationId = evaluation.EvaluationId,
            EvaluationName = evaluation.EvaluationName,
            ClientId = evaluation.ClientId,
            CurrentSurveyDefinitionVersionId = evaluation.CurrentSurveyDefinitionVersionId,
            CurrentSurveyDisplayName = evaluation.CurrentSurveyDefinitionVersion?.SurveyDefinition is null
                ? null
                : $"{evaluation.CurrentSurveyDefinitionVersion.SurveyDefinition.SurveyDefinitionName} v{evaluation.CurrentSurveyDefinitionVersion.VersionNumber}",
            CurrentPinnedParticipantCount = evaluation.CurrentSurveyDefinitionVersionId.HasValue
                ? pinnedCounts.GetValueOrDefault(evaluation.CurrentSurveyDefinitionVersionId.Value)
                : 0,
            Versions = versionRows
        };
    }

    public async Task<SurveyVersionUsageViewModel?> GetSurveyVersionUsageAsync(Guid evaluationId, Guid surveyDefinitionVersionId, Guid clientId, string usageType)
    {
        var evaluation = await _db.Evaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId && e.ClientId == clientId);

        var version = await _db.SurveyDefinitionVersions
            .AsNoTracking()
            .Include(v => v.SurveyDefinition)
            .FirstOrDefaultAsync(v => v.SurveyDefinitionVersionId == surveyDefinitionVersionId && v.SurveyDefinition!.ClientId == clientId);

        if (evaluation == null || version?.SurveyDefinition == null)
        {
            return null;
        }

        var normalizedUsageType = usageType.Trim().ToLowerInvariant();

        var rawRows =
            from ep in _db.EvaluationParticipants.AsNoTracking()
            join er in _db.EvaluationRatees.AsNoTracking() on ep.EvaluationRateeId equals er.EvaluationRateeId
            join rater in _db.Participants.AsNoTracking() on ep.ParticipantId equals rater.ParticipantId
            join ratee in _db.Participants.AsNoTracking() on er.ParticipantId equals ratee.ParticipantId
            join role in _db.ParticipantRoles.AsNoTracking() on ep.ParticipantRoleId equals role.ParticipantRoleId into roleGroup
            from role in roleGroup.DefaultIfEmpty()
            join status in _db.EvaluationParticipantStatuses.AsNoTracking() on ep.EvaluationParticipantStatusId equals status.EvaluationParticipantStatusId into statusGroup
            from status in statusGroup.DefaultIfEmpty()
            join response in _db.EvaluationParticipantResponses.AsNoTracking()
                on new { ep.EvaluationRateeId, RaterParticipantId = ep.ParticipantId }
                equals new { response.EvaluationRateeId, response.RaterParticipantId } into responseGroup
            from response in responseGroup.DefaultIfEmpty()
            where er.EvaluationId == evaluationId
            select new
            {
                ep.AssignedSurveyDefinitionVersionId,
                ResponseSurveyDefinitionVersionId = response != null ? response.SurveyDefinitionVersionId : null,
                ResponseIsSubmitted = response != null && response.IsSubmitted,
                Row = new SurveyVersionUsageRow
                {
                    EvaluationRateeId = ep.EvaluationRateeId,
                    RaterParticipantId = ep.ParticipantId,
                    RaterName = rater.ParticipantNameFirst + " " + rater.ParticipantNameLast,
                    RaterEmail = rater.ParticipantEmail,
                    RateeName = ratee.ParticipantNameFirst + " " + ratee.ParticipantNameLast,
                    RoleName = role != null ? role.ParticipantRoleName : string.Empty,
                    ParticipantStatusName = status != null ? status.EvaluationParticipantStatusName : string.Empty,
                    LastSavedAt = response != null && response.LastSavedAt.HasValue ? response.LastSavedAt.Value.UtcDateTime : null,
                    SubmittedAt = response != null && response.SubmittedAt.HasValue ? response.SubmittedAt.Value.UtcDateTime : null,
                    IsSubmitted = response != null && response.IsSubmitted
                }
            };

        rawRows = normalizedUsageType switch
        {
            "pinned" => rawRows.Where(x => x.AssignedSurveyDefinitionVersionId == surveyDefinitionVersionId),
            "started" => rawRows.Where(x => x.ResponseSurveyDefinitionVersionId == surveyDefinitionVersionId),
            "submitted" => rawRows.Where(x => x.ResponseSurveyDefinitionVersionId == surveyDefinitionVersionId && x.ResponseIsSubmitted),
            _ => rawRows.Where(_ => false)
        };

        var rows = await rawRows
            .Select(x => x.Row)
            .OrderBy(x => x.RateeName)
            .ThenBy(x => x.RoleName)
            .ThenBy(x => x.RaterName)
            .ToListAsync();

        return new SurveyVersionUsageViewModel
        {
            EvaluationId = evaluationId,
            SurveyDefinitionVersionId = surveyDefinitionVersionId,
            EvaluationName = evaluation.EvaluationName,
            SurveyDisplayName = $"{version.SurveyDefinition.SurveyDefinitionName} v{version.VersionNumber}",
            UsageType = normalizedUsageType,
            Count = rows.Count,
            Rows = rows
        };
    }

    public async Task<SurveyVersionDetailViewModel?> GetSurveyVersionDetailAsync(Guid evaluationId, Guid surveyDefinitionVersionId, Guid clientId)
    {
        var evaluation = await _db.Evaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId && e.ClientId == clientId);

        var version = await _db.SurveyDefinitionVersions
            .AsNoTracking()
            .Include(v => v.SurveyDefinition)
            .FirstOrDefaultAsync(v => v.SurveyDefinitionVersionId == surveyDefinitionVersionId && v.SurveyDefinition!.ClientId == clientId);

        if (evaluation == null || version?.SurveyDefinition == null)
        {
            return null;
        }

        var management = await GetSurveyManagementAsync(evaluationId, clientId);
        var usageRow = management?.Versions.FirstOrDefault(v => v.SurveyDefinitionVersionId == surveyDefinitionVersionId);
        var fallbackJson = await LoadTemplateJsonFromPathAsync(version.TemplatePath);

        return new SurveyVersionDetailViewModel
        {
            EvaluationId = evaluationId,
            ClientId = clientId,
            SurveyDefinitionVersionId = version.SurveyDefinitionVersionId,
            SurveyDefinitionId = version.SurveyDefinitionId,
            EvaluationName = evaluation.EvaluationName,
            SurveyDefinitionName = version.SurveyDefinition.SurveyDefinitionName,
            VersionNumber = version.VersionNumber,
            TemplateKey = version.TemplateKey,
            TemplatePath = version.TemplatePath,
            DraftJson = version.DraftJson ?? version.PublishedJson ?? fallbackJson ?? "{}",
            PublishedJson = version.PublishedJson ?? fallbackJson ?? "{}",
            IsPublished = version.IsPublished,
            IsCurrentForEvaluation = evaluation.CurrentSurveyDefinitionVersionId == version.SurveyDefinitionVersionId,
            PinnedParticipantCount = usageRow?.PinnedParticipantCount ?? 0,
            StartedResponseCount = usageRow?.StartedResponseCount ?? 0,
            SubmittedResponseCount = usageRow?.SubmittedResponseCount ?? 0,
            LastEditedBy = version.LastEditedBy,
            LastEditedAt = version.LastEditedAt,
            PublishedBy = version.PublishedBy,
            PublishedAt = version.PublishedAt
        };
    }

    public async Task<SurveyActionResult> SaveSurveyVersionDraftAsync(Guid surveyDefinitionVersionId, Guid clientId, string draftJson, string editedBy)
    {
        try
        {
            var version = await _db.SurveyDefinitionVersions
                .Include(v => v.SurveyDefinition)
                .FirstOrDefaultAsync(v => v.SurveyDefinitionVersionId == surveyDefinitionVersionId && v.SurveyDefinition!.ClientId == clientId);

            if (version == null)
            {
                return new SurveyActionResult(false, "Survey version not found.", "danger");
            }

            if (version.IsPublished)
            {
                return new SurveyActionResult(false, "Published survey versions cannot be changed. Create a new version instead.", "warning");
            }

            if (!TryNormalizeSurveyJson(draftJson, out var normalizedJson, out var validationMessage))
            {
                return new SurveyActionResult(false, validationMessage, "danger");
            }

            version.DraftJson = normalizedJson;
            version.LastEditedAt = DateTimeOffset.UtcNow;
            version.LastEditedBy = editedBy;

            await _db.SaveChangesAsync();
            return new SurveyActionResult(true, $"Saved draft for {version.SurveyDefinition!.SurveyDefinitionName} v{version.VersionNumber}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save draft for survey version {SurveyDefinitionVersionId}", surveyDefinitionVersionId);
            return new SurveyActionResult(false, "Unable to save the survey draft.", "danger");
        }
    }

    public async Task<SurveyActionResult> PublishSurveyVersionAsync(Guid surveyDefinitionVersionId, Guid clientId, string publishedBy)
    {
        try
        {
            var version = await _db.SurveyDefinitionVersions
                .Include(v => v.SurveyDefinition)
                .FirstOrDefaultAsync(v => v.SurveyDefinitionVersionId == surveyDefinitionVersionId && v.SurveyDefinition!.ClientId == clientId);

            if (version == null)
            {
                return new SurveyActionResult(false, "Survey version not found.", "danger");
            }

            if (version.IsPublished)
            {
                return new SurveyActionResult(false, "Published survey versions cannot be changed. Create a new version instead.", "warning");
            }

            var sourceJson = version.DraftJson;
            if (!TryNormalizeSurveyJson(sourceJson, out var normalizedJson, out var validationMessage))
            {
                return new SurveyActionResult(false, validationMessage, "danger");
            }

            version.PublishedJson = normalizedJson;
            version.IsPublished = true;
            version.PublishedAt = DateTimeOffset.UtcNow;
            version.PublishedBy = publishedBy;
            version.LastEditedAt = DateTimeOffset.UtcNow;
            version.LastEditedBy = publishedBy;

            await _db.SaveChangesAsync();
            return new SurveyActionResult(true, $"Published {version.SurveyDefinition!.SurveyDefinitionName} v{version.VersionNumber}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish survey version {SurveyDefinitionVersionId}", surveyDefinitionVersionId);
            return new SurveyActionResult(false, "Unable to publish this survey version.", "danger");
        }
    }

    public async Task<SurveyActionResult> SetCurrentSurveyVersionAsync(Guid evaluationId, Guid surveyDefinitionVersionId, Guid clientId, bool forceSwitch = false)
    {
        try
        {
            var evaluation = await _db.Evaluations
                .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId && e.ClientId == clientId);

            if (evaluation == null)
            {
                return new SurveyActionResult(false, "Evaluation not found.", "danger");
            }

            var version = await _db.SurveyDefinitionVersions
                .Include(v => v.SurveyDefinition)
                .FirstOrDefaultAsync(v => v.SurveyDefinitionVersionId == surveyDefinitionVersionId && v.SurveyDefinition!.ClientId == clientId);

            if (version == null)
            {
                return new SurveyActionResult(false, "Survey version not found.", "danger");
            }

            if (!version.IsPublished || !version.SurveyDefinition!.IsActive)
            {
                return new SurveyActionResult(false, "Publish an active survey version before using it for new starters.", "warning");
            }

            if (evaluation.CurrentSurveyDefinitionVersionId == surveyDefinitionVersionId)
            {
                return new SurveyActionResult(true, "This evaluation is already using that survey version.");
            }

            var currentPinnedCount = 0;
            if (evaluation.CurrentSurveyDefinitionVersionId.HasValue)
            {
                currentPinnedCount = await _db.EvaluationParticipants
                    .Where(ep => ep.AssignedSurveyDefinitionVersionId == evaluation.CurrentSurveyDefinitionVersionId)
                    .Join(
                        _db.EvaluationRatees.Where(er => er.EvaluationId == evaluationId),
                        ep => ep.EvaluationRateeId,
                        er => er.EvaluationRateeId,
                        (ep, _) => ep)
                    .CountAsync();
            }

            if (currentPinnedCount > 0 && !forceSwitch)
            {
                return new SurveyActionResult(
                    false,
                    $"{currentPinnedCount} participants are pinned to the current version. Confirm the switch to use this version for new starters only.",
                    "warning");
            }

            evaluation.CurrentSurveyDefinitionVersionId = surveyDefinitionVersionId;
            await _db.SaveChangesAsync();
            return new SurveyActionResult(true, $"This evaluation will now use {version.SurveyDefinition!.SurveyDefinitionName} v{version.VersionNumber} for new starters.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set survey version {SurveyDefinitionVersionId} for evaluation {EvaluationId}", surveyDefinitionVersionId, evaluationId);
            return new SurveyActionResult(false, "Unable to switch the current survey version.", "danger");
        }
    }

    public async Task<SurveyActionResult> UpdateSurveyVersionTemplatePathAsync(Guid surveyDefinitionVersionId, Guid clientId, string templatePath)
    {
        try
        {
            var version = await _db.SurveyDefinitionVersions
                .Include(v => v.SurveyDefinition)
                .FirstOrDefaultAsync(v => v.SurveyDefinitionVersionId == surveyDefinitionVersionId && v.SurveyDefinition!.ClientId == clientId);

            if (version == null)
            {
                return new SurveyActionResult(false, "Survey version not found.", "danger");
            }

            if (version.IsPublished)
            {
                return new SurveyActionResult(false, "Published survey versions cannot be changed. Create a new version instead.", "warning");
            }

            if (!TryNormalizeTemplatePath(templatePath, out var normalizedTemplatePath, out var validationMessage))
            {
                _logger.LogWarning("Rejected invalid survey template path for survey version {SurveyDefinitionVersionId}: {TemplatePath}", surveyDefinitionVersionId, templatePath);
                return new SurveyActionResult(false, validationMessage, "danger");
            }

            version.TemplatePath = normalizedTemplatePath;
            await _db.SaveChangesAsync();
            return new SurveyActionResult(true, $"Updated the template path for {version.SurveyDefinition!.SurveyDefinitionName} v{version.VersionNumber}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template path for survey version {SurveyDefinitionVersionId}", surveyDefinitionVersionId);
            return new SurveyActionResult(false, "Unable to update the template path.", "danger");
        }
    }

    public async Task<SurveyActionResult> CreateNextSurveyVersionAsync(Guid evaluationId, Guid clientId)
    {
        try
        {
            var evaluation = await _db.Evaluations
                .Include(e => e.CurrentSurveyDefinitionVersion)
                    .ThenInclude(v => v!.SurveyDefinition)
                .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId && e.ClientId == clientId);

            if (evaluation?.CurrentSurveyDefinitionVersion?.SurveyDefinition == null)
            {
                return new SurveyActionResult(false, "The evaluation does not have a current survey to clone.", "danger");
            }

            var current = evaluation.CurrentSurveyDefinitionVersion;
            var sourceJson = current.IsPublished
                ? current.PublishedJson ?? await LoadTemplateJsonFromPathAsync(current.TemplatePath)
                : current.DraftJson;
            if (!TryNormalizeSurveyJson(sourceJson, out _, out var validationMessage))
            {
                return new SurveyActionResult(false, validationMessage, "danger");
            }

            var nextVersionNumber = await _db.SurveyDefinitionVersions
                .Where(v => v.SurveyDefinitionId == current.SurveyDefinitionId)
                .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

            var newVersion = new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = Guid.NewGuid(),
                SurveyDefinitionId = current.SurveyDefinitionId,
                VersionNumber = nextVersionNumber + 1,
                TemplateKey = Regex.Replace(current.TemplateKey, "-v\\d+$", string.Empty) + $"-v{nextVersionNumber + 1}",
                TemplatePath = current.TemplatePath,
                DraftJson = sourceJson,
                IsPublished = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.SurveyDefinitionVersions.Add(newVersion);

            await _db.SaveChangesAsync();
            return new SurveyActionResult(true, $"Created draft {current.SurveyDefinition.SurveyDefinitionName} v{newVersion.VersionNumber}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create next survey version for evaluation {EvaluationId}", evaluationId);
            return new SurveyActionResult(false, "Unable to create the next survey version.", "danger");
        }
    }

    public async Task<SurveyActionResult> CreateNewSurveyAsync(Guid evaluationId, Guid clientId)
    {
        try
        {
            var evaluation = await _db.Evaluations
                .Include(e => e.CurrentSurveyDefinitionVersion)
                    .ThenInclude(v => v!.SurveyDefinition)
                .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId && e.ClientId == clientId);

            if (evaluation?.CurrentSurveyDefinitionVersion == null)
            {
                return new SurveyActionResult(false, "The evaluation does not have a current survey to copy from.", "danger");
            }

            var current = evaluation.CurrentSurveyDefinitionVersion;
            var sourceJson = current.IsPublished
                ? current.PublishedJson ?? await LoadTemplateJsonFromPathAsync(current.TemplatePath)
                : current.DraftJson;
            if (!TryNormalizeSurveyJson(sourceJson, out _, out var validationMessage))
            {
                return new SurveyActionResult(false, validationMessage, "danger");
            }

            var baseName = string.IsNullOrWhiteSpace(evaluation.EvaluationName)
                ? "Survey"
                : $"{evaluation.EvaluationName} Survey";

            var surveyDefinition = new SurveyDefinition
            {
                SurveyDefinitionId = Guid.NewGuid(),
                ClientId = clientId,
                SurveyDefinitionName = baseName,
                SurveyDefinitionKey = await BuildUniqueSurveyKeyAsync(clientId, baseName),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var version = new SurveyDefinitionVersion
            {
                SurveyDefinitionVersionId = Guid.NewGuid(),
                SurveyDefinitionId = surveyDefinition.SurveyDefinitionId,
                VersionNumber = 1,
                TemplateKey = surveyDefinition.SurveyDefinitionKey + "-v1",
                TemplatePath = current.TemplatePath,
                DraftJson = sourceJson,
                IsPublished = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.SurveyDefinitions.Add(surveyDefinition);
            _db.SurveyDefinitionVersions.Add(version);

            await _db.SaveChangesAsync();
            return new SurveyActionResult(true, $"Created draft {surveyDefinition.SurveyDefinitionName} v1 in a new survey family.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new survey for evaluation {EvaluationId}", evaluationId);
            return new SurveyActionResult(false, "Unable to create a new survey.", "danger");
        }
    }

    private static bool IsPersistedScalar(Type propertyType)
    {
        var actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return actualType.IsPrimitive
            || actualType.IsEnum
            || actualType == typeof(string)
            || actualType == typeof(Guid)
            || actualType == typeof(DateTime)
            || actualType == typeof(DateTimeOffset)
            || actualType == typeof(decimal);
    }

    private async Task<string> BuildUniqueSurveyKeyAsync(Guid clientId, string baseName)
    {
        var slug = Slugify(baseName);
        var candidate = slug;
        var counter = 2;

        while (await _db.SurveyDefinitions.AnyAsync(d => d.ClientId == clientId && d.SurveyDefinitionKey == candidate))
        {
            candidate = $"{slug}-{counter}";
            counter++;
        }

        return candidate;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '-');
        }

        var collapsed = Regex.Replace(builder.ToString(), "-{2,}", "-").Trim('-');
        return string.IsNullOrWhiteSpace(collapsed) ? "survey" : collapsed;
    }

    private static bool TryNormalizeTemplatePath(string templatePath, out string normalizedPath, out string validationMessage)
    {
        normalizedPath = string.Empty;
        validationMessage = "Template path is required.";
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            return false;
        }

        var trimmedPath = templatePath.Trim();
        var absolutePath = Path.IsPathRooted(trimmedPath)
            ? trimmedPath
            : Path.Combine(Directory.GetCurrentDirectory(), trimmedPath);

        if (!File.Exists(absolutePath))
        {
            validationMessage = $"Could not find a survey JSON file at '{trimmedPath}'.";
            return false;
        }

        try
        {
            using var stream = File.OpenRead(absolutePath);
            using var _ = JsonDocument.Parse(stream);
        }
        catch (JsonException)
        {
            validationMessage = $"The file at '{trimmedPath}' is not valid JSON.";
            return false;
        }

        normalizedPath = trimmedPath;
        validationMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeSurveyJson(string? json, out string normalizedJson, out string validationMessage)
    {
        normalizedJson = string.Empty;
        validationMessage = "Survey JSON is required.";

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            normalizedJson = JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            validationMessage = string.Empty;
            return true;
        }
        catch (JsonException)
        {
            validationMessage = "Survey JSON is not valid JSON.";
            return false;
        }
    }

    private static async Task<string?> LoadTemplateJsonFromPathAsync(string templatePath)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            return null;
        }

        var absolutePath = Path.IsPathRooted(templatePath)
            ? templatePath
            : Path.Combine(Directory.GetCurrentDirectory(), templatePath);

        if (!File.Exists(absolutePath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(absolutePath);
    }
}
