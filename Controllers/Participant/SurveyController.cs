using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using reviews.Models;
using reviews.Services;

namespace reviews.Controllers.Participant;

/// <summary>
/// Handles survey response collection for evaluation participants using SurveyJS
/// Issue #9: Simple SurveyJS architecture with PostgreSQL jsonb storage
/// </summary>
public class SurveyController : Controller
{
    private readonly ISurveyTemplateResolver _templateResolver;
    private readonly ISurveyAssignmentResolver _assignmentResolver;
    private readonly ISurveyResponseStore _responseStore;
    private readonly IEvaluationParticipantStatusService _statusService;
    private readonly IReviewSessionService _sessionService;
    private readonly ISessionInitializationService _sessionInitializationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SurveyController> _logger;

    public SurveyController(
        ISurveyTemplateResolver templateResolver,
        ISurveyAssignmentResolver assignmentResolver,
        ISurveyResponseStore responseStore,
        IEvaluationParticipantStatusService statusService,
        IReviewSessionService sessionService,
        ISessionInitializationService sessionInitializationService,
        UserManager<ApplicationUser> userManager,
        ILogger<SurveyController> logger)
    {
        _templateResolver = templateResolver;
        _assignmentResolver = assignmentResolver;
        _responseStore = responseStore;
        _statusService = statusService;
        _sessionService = sessionService;
        _sessionInitializationService = sessionInitializationService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// GET /survey/{password}
    /// Displays the survey page for a participant
    /// </summary>
    [HttpGet("/survey/{password}")]
    public async Task<IActionResult> Respond(string password, CancellationToken ct)
    {
        _logger.LogInformation("Survey page requested for password: {Password}", password);

        var assignment = await _assignmentResolver.ResolveAsync(password, ct);
        if (assignment == null)
        {
            _logger.LogWarning("Invalid survey password: {Password}", password);
            return NotFound("Survey not found. Please check your invitation link.");
        }

        _logger.LogInformation(
            "Assignment resolved - Password: {Password}, RateeDisplay: {Ratee}, Role: {Role}, IsComplete: {IsComplete}, CompletedAt: {CompletedAt}, EvaluationActive: {Active}",
            password, assignment.RateeDisplayName, assignment.RoleName, assignment.IsComplete, assignment.CompletedAt, assignment.EvaluationActive);

        ViewBag.Password = password;
        ViewBag.RateeDisplayName = assignment.RateeDisplayName;
        ViewBag.RoleName = assignment.RoleName;
        ViewBag.IsComplete = assignment.IsComplete;
        ViewBag.CompletedAt = assignment.CompletedAt;
        ViewBag.EvaluationActive = assignment.EvaluationActive;
        ViewBag.EvaluationDueDate = assignment.EvaluationDueDate;

        return View();
    }

    /// <summary>
    /// GET /api/survey/{password}/runtime
    /// Returns the survey template and existing response data for SurveyJS initialization
    /// </summary>
    [HttpGet("/api/survey/{password}/runtime")]
    public async Task<IActionResult> GetRuntime(string password, CancellationToken ct)
    {
        _logger.LogInformation("Runtime data requested for password: {Password}", password);

        // Resolve assignment
        var assignment = await _assignmentResolver.ResolveAsync(password, ct);
        if (assignment == null)
        {
            _logger.LogWarning("Runtime: No assignment found for password: {Password}", password);
            return NotFound(new { error = "Survey not found" });
        }

        _logger.LogInformation(
            "Runtime resolved - Password: {Password}, Ratee: {Ratee}, Role: {Role}, IsComplete: {IsComplete}, CompletedAt: {CompletedAt}, RaterParticipantId: {RaterId}",
            password, assignment.RateeDisplayName, assignment.RoleName, assignment.IsComplete, assignment.CompletedAt, assignment.RaterParticipantId);

        // Get survey template (hardcoded for now, can be made dynamic later)
        const string templateKey = "leadership-competency-v1";
        const int templateVersion = 1;

        var template = await _templateResolver.ResolveAsync(templateKey, templateVersion, ct);
        if (template == null)
        {
            _logger.LogError("Template not found: {TemplateKey} v{Version}", templateKey, templateVersion);
            return StatusCode(500, new { error = "Survey template not found" });
        }

        // Get or create response record
        var response = await _responseStore.GetOrCreateAsync(
            assignment.EvaluationRateeId,
            assignment.RaterParticipantId,
            password,
            templateKey,
            templateVersion,
            ct);

        // Return runtime data
        return Json(new
        {
            surveyDefinition = template.Json,
            existingData = response.DraftJson ?? response.FinalJson,
            isReadOnly = response.IsSubmitted || assignment.IsComplete,
            isSubmitted = response.IsSubmitted,
            rateeDisplayName = assignment.RateeDisplayName,
            roleName = assignment.RoleName,
            templateKey = template.Key,
            templateVersion = template.Version
        });
    }

    /// <summary>
    /// POST /api/survey/{password}/draft
    /// Saves a draft of the survey response (autosave)
    /// </summary>
    [HttpPost("/api/survey/{password}/draft")]
    public async Task<IActionResult> SaveDraft(string password, [FromBody] SaveDraftRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Draft save requested for password: {Password}", password);

        if (string.IsNullOrWhiteSpace(request.DraftJson))
        {
            return BadRequest(new { error = "Draft data is required" });
        }

        // Resolve assignment to verify it exists
        var assignment = await _assignmentResolver.ResolveAsync(password, ct);
        if (assignment == null)
        {
            return NotFound(new { error = "Survey not found" });
        }

        if (assignment.IsComplete)
        {
            var completedMessage = assignment.CompletedAt.HasValue 
                ? $"This survey was completed on {assignment.CompletedAt.Value:MMMM d, yyyy}. Thank you for your participation!"
                : "This survey has already been completed. Thank you for your participation!";
            return BadRequest(new { error = completedMessage });
        }

        // Check if evaluation window is still open
        if (!assignment.EvaluationActive)
        {
            return BadRequest(new { error = "This evaluation is no longer active and cannot accept responses." });
        }

        if (assignment.EvaluationDueDate.HasValue && assignment.EvaluationDueDate.Value < DateTime.UtcNow)
        {
            return BadRequest(new { error = $"The evaluation period ended on {assignment.EvaluationDueDate.Value:MMMM d, yyyy}. This survey can no longer be completed." });
        }

        try
        {
            await _responseStore.SaveDraftAsync(password, request.DraftJson, ct);
            return Ok(new { success = true, message = "Draft saved" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to save draft for password: {Password}", password);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/survey/{password}/submit
    /// Submits the final survey response and updates participant status
    /// </summary>
    [HttpPost("/api/survey/{password}/submit")]
    public async Task<IActionResult> Submit(string password, [FromBody] SubmitRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Survey submission requested for password: {Password}", password);

        if (string.IsNullOrWhiteSpace(request.FinalJson))
        {
            return BadRequest(new { error = "Survey data is required" });
        }

        // Resolve assignment to verify it exists
        var assignment = await _assignmentResolver.ResolveAsync(password, ct);
        if (assignment == null)
        {
            return NotFound(new { error = "Survey not found" });
        }

        if (assignment.IsComplete)
        {
            var completedMessage = assignment.CompletedAt.HasValue 
                ? $"This survey was completed on {assignment.CompletedAt.Value:MMMM d, yyyy}. Thank you for your participation!"
                : "This survey has already been completed. Thank you for your participation!";
            return BadRequest(new { error = completedMessage });
        }

        // Check if evaluation window is still open
        if (!assignment.EvaluationActive)
        {
            return BadRequest(new { error = "This evaluation is no longer active and cannot accept responses." });
        }

        if (assignment.EvaluationDueDate.HasValue && assignment.EvaluationDueDate.Value < DateTime.UtcNow)
        {
            return BadRequest(new { error = $"The evaluation period ended on {assignment.EvaluationDueDate.Value:MMMM d, yyyy}. This survey can no longer be completed." });
        }

        try
        {
            // Submit response
            await _responseStore.SubmitAsync(password, request.FinalJson, ct);

            // Update participant status
            await _statusService.MarkCompletedAsync(password, ct);
            await RefreshParticipantSessionAsync();

            _logger.LogInformation("Survey completed for password: {Password}", password);

            return Ok(new { success = true, message = "Survey submitted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to submit survey for password: {Password}", password);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/survey/{password}/debug
    /// Diagnostic endpoint to test what data is being resolved
    /// </summary>
    [HttpGet("/api/survey/{password}/debug")]
    public async Task<IActionResult> Debug(string password, CancellationToken ct)
    {
        var assignment = await _assignmentResolver.ResolveAsync(password, ct);
        if (assignment == null)
        {
            return NotFound(new { error = "No assignment found" });
        }

        return Json(new
        {
            password,
            assignment.EvaluationRateeId,
            assignment.RaterParticipantId,
            assignment.ParticipantRoleId,
            assignment.RoleName,
            assignment.IsComplete,
            assignment.CompletedAt,
            assignment.RateeDisplayName,
            assignment.EvaluationActive,
            assignment.EvaluationDueDate
        });
    }

    private async Task RefreshParticipantSessionAsync()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await _userManager.GetUserAsync(User);
        var email = user?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var refreshedSession = await _sessionInitializationService.InitializeParticipantSessionAsync(email);
        if (refreshedSession != null)
        {
            _sessionService.SetSessionData(refreshedSession);
        }
    }
}

/// <summary>
/// Request model for saving draft responses
/// </summary>
public record SaveDraftRequest(string DraftJson);

/// <summary>
/// Request model for submitting final responses
/// </summary>
public record SubmitRequest(string FinalJson);
