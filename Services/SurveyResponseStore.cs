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
    Task<SurveyResponseDto> GetOrCreateAsync(Guid evaluationRateeId, Guid raterParticipantId, string surveyPassword, string templateKey, int templateVersion, CancellationToken ct = default);
    Task SaveDraftAsync(string surveyPassword, string draftJson, CancellationToken ct = default);
    Task SubmitAsync(string surveyPassword, string finalJson, CancellationToken ct = default);
}

public record SurveyResponseDto(
    Guid EvaluationParticipantResponseId,
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

    public async Task<SurveyResponseDto> GetOrCreateAsync(
        Guid evaluationRateeId,
        Guid raterParticipantId,
        string surveyPassword,
        string templateKey,
        int templateVersion,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting or creating response for password: {Password}", surveyPassword);

        var existing = await _db.EvaluationParticipantResponses
            .Where(r => r.EvaluationParticipantSurveyPassword == surveyPassword)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            return new SurveyResponseDto(
                EvaluationParticipantResponseId: existing.EvaluationParticipantResponseId,
                SurveyTemplateKey: existing.SurveyTemplateKey,
                SurveyTemplateVersion: existing.SurveyTemplateVersion,
                DraftJson: existing.DraftJson,
                FinalJson: existing.FinalJson,
                IsSubmitted: existing.IsSubmitted,
                LastSavedAt: existing.LastSavedAt,
                SubmittedAt: existing.SubmittedAt);
        }

        // Create new response record
        var newResponse = new EvaluationParticipantResponse
        {
            EvaluationParticipantResponseId = Guid.NewGuid(),
            EvaluationRateeId = evaluationRateeId,
            RaterParticipantId = raterParticipantId,
            EvaluationParticipantSurveyPassword = surveyPassword,
            SurveyTemplateKey = templateKey,
            SurveyTemplateVersion = templateVersion,
            IsSubmitted = false,
            DraftJson = null,
            FinalJson = null,
            LastSavedAt = null,
            SubmittedAt = null
        };

        _db.EvaluationParticipantResponses.Add(newResponse);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created new response: {ResponseId}", newResponse.EvaluationParticipantResponseId);

        return new SurveyResponseDto(
            EvaluationParticipantResponseId: newResponse.EvaluationParticipantResponseId,
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
