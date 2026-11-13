using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

/// <summary>
/// Handles CRUD operations for survey responses
/// </summary>
public interface ISurveyResponseStore
{
    Task<SurveyResponseDto?> GetByPasswordAsync(string surveyPassword, CancellationToken ct = default);
    Task<SurveyResponseDto> CreateAsync(Guid evaluationRateeId, Guid raterParticipantId, string surveyPassword, Guid surveyDefinitionVersionId, string templateKey, int templateVersion, CancellationToken ct = default);
    Task SaveDraftAsync(string surveyPassword, string draftJson, CancellationToken ct = default);
    Task SubmitAsync(string surveyPassword, string finalJson, CancellationToken ct = default);
}

public record SurveyResponseDto(
    Guid EvaluationParticipantResponseId,
    Guid? SurveyDefinitionVersionId,
    string SurveyTemplateKey,
    int SurveyTemplateVersion,
    string? DraftJson,
    string? FinalJson,
    bool IsSubmitted,
    DateTimeOffset? LastSavedAt,
    DateTimeOffset? SubmittedAt);

public class SurveyResponseStore : ISurveyResponseStore
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SurveyResponseStore> _logger;

    public SurveyResponseStore(
        ApplicationDbContext db,
        ILogger<SurveyResponseStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SurveyResponseDto?> GetByPasswordAsync(string surveyPassword, CancellationToken ct = default)
    {
        var existing = await _db.EvaluationParticipantResponses
            .AsNoTracking()
            .Where(r => r.EvaluationParticipantSurveyPassword == surveyPassword)
            .FirstOrDefaultAsync(ct);

        if (existing == null)
        {
            return null;
        }

        return new SurveyResponseDto(
            EvaluationParticipantResponseId: existing.EvaluationParticipantResponseId,
            SurveyDefinitionVersionId: existing.SurveyDefinitionVersionId,
            SurveyTemplateKey: existing.SurveyTemplateKey,
            SurveyTemplateVersion: existing.SurveyTemplateVersion,
            DraftJson: existing.DraftJson,
            FinalJson: existing.FinalJson,
            IsSubmitted: existing.IsSubmitted,
            LastSavedAt: existing.LastSavedAt,
            SubmittedAt: existing.SubmittedAt);
    }

    public async Task<SurveyResponseDto> CreateAsync(
        Guid evaluationRateeId,
        Guid raterParticipantId,
        string surveyPassword,
        Guid surveyDefinitionVersionId,
        string templateKey,
        int templateVersion,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating response for password: {Password}", surveyPassword);

        var newResponse = new EvaluationParticipantResponse
        {
            EvaluationParticipantResponseId = Guid.NewGuid(),
            EvaluationRateeId = evaluationRateeId,
            RaterParticipantId = raterParticipantId,
            EvaluationParticipantSurveyPassword = surveyPassword,
            SurveyDefinitionVersionId = surveyDefinitionVersionId,
            SurveyTemplateKey = templateKey,
            SurveyTemplateVersion = templateVersion,
            IsSubmitted = false,
            DraftJson = null,
            FinalJson = null,
            LastSavedAt = null,
            SubmittedAt = null
        };

        var evaluationParticipant = await _db.EvaluationParticipants
            .FirstOrDefaultAsync(
                ep => ep.EvaluationRateeId == evaluationRateeId && ep.ParticipantId == raterParticipantId,
                ct);

        if (evaluationParticipant != null)
        {
            evaluationParticipant.AssignedSurveyDefinitionVersionId ??= surveyDefinitionVersionId;
            evaluationParticipant.SurveyTemplateKey = templateKey;
            evaluationParticipant.SurveyTemplateVersion = templateVersion;
        }

        _db.EvaluationParticipantResponses.Add(newResponse);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created new response: {ResponseId}", newResponse.EvaluationParticipantResponseId);

        return new SurveyResponseDto(
            EvaluationParticipantResponseId: newResponse.EvaluationParticipantResponseId,
            SurveyDefinitionVersionId: newResponse.SurveyDefinitionVersionId,
            SurveyTemplateKey: newResponse.SurveyTemplateKey,
            SurveyTemplateVersion: newResponse.SurveyTemplateVersion,
            DraftJson: null,
            FinalJson: null,
            IsSubmitted: false,
            LastSavedAt: null,
            SubmittedAt: null);
    }

    public async Task SaveDraftAsync(string surveyPassword, string draftJson, CancellationToken ct = default)
    {
        _logger.LogInformation("Saving draft for password: {Password}", surveyPassword);

        var response = await _db.EvaluationParticipantResponses
            .Where(r => r.EvaluationParticipantSurveyPassword == surveyPassword)
            .FirstOrDefaultAsync(ct);

        if (response == null)
        {
            throw new InvalidOperationException($"No response found for password: {surveyPassword}");
        }

        if (response.IsSubmitted)
        {
            throw new InvalidOperationException("Cannot save draft for submitted survey");
        }

        response.DraftJson = draftJson;
        response.LastSavedAt = DateTime.UtcNow;

        var participant = await _db.EvaluationParticipants
            .FirstOrDefaultAsync(
                ep => ep.EvaluationRateeId == response.EvaluationRateeId && ep.ParticipantId == response.RaterParticipantId,
                ct);

        if (participant != null)
        {
            participant.SurveyLastSavedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Draft saved for response: {ResponseId}", response.EvaluationParticipantResponseId);
    }

    public async Task SubmitAsync(string surveyPassword, string finalJson, CancellationToken ct = default)
    {
        _logger.LogInformation("Submitting survey for password: {Password}", surveyPassword);

        var response = await _db.EvaluationParticipantResponses
            .Where(r => r.EvaluationParticipantSurveyPassword == surveyPassword)
            .FirstOrDefaultAsync(ct);

        if (response == null)
        {
            throw new InvalidOperationException($"No response found for password: {surveyPassword}");
        }

        if (response.IsSubmitted)
        {
            throw new InvalidOperationException("Survey already submitted");
        }

        response.FinalJson = finalJson;
        response.IsSubmitted = true;
        response.SubmittedAt = DateTime.UtcNow;
        response.LastSavedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Survey submitted for response: {ResponseId}", response.EvaluationParticipantResponseId);
    }
}
