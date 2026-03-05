using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using reviews.Constants;
using reviews.Models;
using reviews.Services;

namespace reviews.Controllers.ParticipantController;

public class ParticipantController : Controller
{
    private readonly IParticipantService _participantService;
    private readonly IInvitationService _invitationService;
    private readonly ISurveyUrlBuilder _surveyUrlBuilder;
    private readonly ISurveyCompletionService _surveyCompletionService;
    private readonly IReviewSessionService _sessionService;
    private readonly ILogger<ParticipantController> _logger;

    private SessionData SessionData => _sessionService.GetSessionData();
    private Guid LocalClientId => SessionData.ClientId;
    private Guid LocalParticipantId => SessionData.ParticipantId;
    private string LocalEmail => SessionData.Email ?? string.Empty;
    private string LocalRole => SessionData.Role ?? string.Empty;

    public ParticipantController(
        IParticipantService participantService,
        IInvitationService invitationService,
        ISurveyUrlBuilder surveyUrlBuilder,
        ISurveyCompletionService surveyCompletionService,
        IReviewSessionService sessionService,
        ILogger<ParticipantController> logger)
    {
        _participantService = participantService;
        _invitationService = invitationService;
        _surveyUrlBuilder = surveyUrlBuilder;
        _surveyCompletionService = surveyCompletionService;
        _sessionService = sessionService;
        _logger = logger;
    }

    [Authorize(Roles = RoleNames.AdminOrParticipant)]
    public async Task<IActionResult> ParticipantIndex(Guid? EvaluationId, Guid? ParticipantId)
    {
        try
        {
            if (LocalClientId == Guid.Empty)
            {
                _logger.LogWarning("Session unavailable in ParticipantIndex.");
                return View(new List<vwEvaluationsToComplete>());
            }

            Guid targetParticipantId = LocalRole == RoleNames.Admin && ParticipantId.HasValue 
                ? ParticipantId.Value 
                : LocalParticipantId;

            bool activeOnly = LocalRole == RoleNames.Participant && !EvaluationId.HasValue;

            var evaluations = await _participantService.GetEvaluationsToCompleteAsync(
                targetParticipantId, 
                EvaluationId, 
                activeOnly);

            return View(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading participant index");
            return View(new List<vwEvaluationsToComplete>());
        }
    }

    [HttpGet("Participant/ViewRaters/{evaluationId?}/{participantID?}")]
    [Authorize(Roles = RoleNames.AdminOrParticipant)]
    public async Task<IActionResult> ViewRaters(Guid? EvaluationID, Guid? participantID)
    {
        try
        {
            // Determine target participant
            Guid targetRateeId = LocalRole == RoleNames.Admin && participantID.HasValue 
                ? participantID.Value 
                : LocalParticipantId;

            if (targetRateeId == Guid.Empty)
            {
                return BadRequest("Invalid request. Please provide a valid Participant ID.");
            }

            // Get raters with appropriate filters
            bool activeOnly = LocalRole == RoleNames.Participant && !EvaluationID.HasValue;
            var raters = await _participantService.GetRatersByRateeAsync(
                targetRateeId, 
                EvaluationID, 
                activeOnly);

            // Get participant roles for the view
            var participantRoles = await _participantService.GetParticipantRolesAsync();

            var viewModel = new ViewRatersViewModel
            {
                Raters = raters,
                ParticipantRoles = participantRoles,
                EvaluationRateeId = raters.FirstOrDefault()?.EvaluationRateeId ?? Guid.Empty
            };

            ViewBag.MaxRaters = raters.FirstOrDefault()?.EvaluationMaxRaters ?? 0;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading raters view");
            return View(new ViewRatersViewModel
            {
                Raters = new List<vwRatersByRatee>(),
                ParticipantRoles = new List<ParticipantRole>(),
                EvaluationRateeId = Guid.Empty
            });
        }
    }

    [HttpGet("Participant/GetEligibleRaters/{EvaluationRateeId}")]
    [Authorize(Roles = RoleNames.AdminOrParticipant)]
    public async Task<IActionResult> GetEligibleRaters(string EvaluationRateeId)
    {
        if (!Guid.TryParse(EvaluationRateeId, out Guid evaluationRateeGuid) || evaluationRateeGuid == Guid.Empty)
        {
            _logger.LogWarning("GetEligibleRaters called with invalid EvaluationRateeId: {EvaluationRateeId}", EvaluationRateeId);
            return Json(new List<EligibleRaters>
            {
                new EligibleRaters
                {
                    EvaluationRateeId = Guid.Empty,
                    ParticipantId = Guid.Empty,
                    ParticipantNameFirst = "",
                    ParticipantNameLast = "Invalid Evaluation Ratee ID",
                    ParticipantEmail = "",
                    ParticipantHasRegistered = false
                }
            });
        }

        var eligibleRaters = await _participantService.GetEligibleRatersAsync(evaluationRateeGuid);
        return Json(eligibleRaters);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrParticipant)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNewRater(
        Guid evaluationRateeId, 
        string firstName, 
        string lastName, 
        string email, 
        string rolename)
    {
        _logger.LogInformation(
            "AddNewRater called: EvaluationRateeId={EvaluationRateeId}, FirstName={FirstName}, LastName={LastName}, Email={Email}, Role={Role}",
            evaluationRateeId, firstName, lastName, email, rolename);

        var success = await _participantService.AddNewRaterAsync(
            evaluationRateeId, 
            firstName, 
            lastName, 
            email, 
            rolename, 
            LocalClientId);

        _logger.LogInformation("AddNewRaterAsync result: {Success}", success);

        // Check if this is an AJAX request
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (success)
            {
                return Json(new { success = true, message = "Rater added successfully" });
            }
            else
            {
                return Json(new { success = false, message = ErrorMessages.FailedToAddNewRater });
            }
        }

        // Non-AJAX request: use traditional redirect
        if (!success)
        {
            ModelState.AddModelError("", ErrorMessages.FailedToAddNewRater);
        }

        return RedirectToAction(ActionNames.ViewRaters);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrParticipant)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRater(Guid evaluationRateeId, Guid participantId, string rolename)
    {
        _logger.LogInformation(
            "AddRater called: EvaluationRateeId={EvaluationRateeId}, ParticipantId={ParticipantId}, Role={Role}",
            evaluationRateeId, participantId, rolename);

        var success = await _participantService.AddExistingRaterAsync(
            evaluationRateeId, 
            participantId, 
            rolename);

        _logger.LogInformation("AddExistingRaterAsync result: {Success}", success);

        // Check if this is an AJAX request
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            if (success)
            {
                return Json(new { success = true, message = "Rater added successfully" });
            }
            else
            {
                return Json(new { success = false, message = ErrorMessages.FailedToAddRater });
            }
        }

        // Non-AJAX request: use traditional redirect
        if (!success)
        {
            ModelState.AddModelError("", ErrorMessages.FailedToAddRater);
        }

        return RedirectToAction(ActionNames.ViewRaters);
    }

    [HttpGet]
    public async Task<IActionResult> GetRatersForEvaluation(Guid evaluationId, Guid participantId)
    {
        var raters = await _participantService.GetRatersForEvaluationAsync(evaluationId, participantId);
        return PartialView("_RatersTablePartial", raters);
    }


    [HttpGet]
    public async Task<IActionResult> LaunchEvaluation(Guid evaluationRateeId, Guid participantId)
    {
        try
        {
            if (evaluationRateeId == Guid.Empty || participantId == Guid.Empty)
            {
                return BadRequest(ErrorMessages.InvalidEvaluationRateeId);
            }

            var evaluations = await _participantService.GetEvaluationsToCompleteAsync(participantId);
            var evaluationToComplete = evaluations
                .FirstOrDefault(e => e.EvaluationRateeId == evaluationRateeId);

            if (evaluationToComplete == null)
            {
                return NotFound("Evaluation not found.");
            }

            var surveyUrl = _surveyUrlBuilder.BuildSurveyUrl(evaluationToComplete);
            
            _logger.LogInformation("Launching evaluation at URL: {SurveyUrl}", surveyUrl);

            return Redirect(surveyUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching evaluation");
            return BadRequest(ErrorMessages.UnableToLaunchEvaluation);
        }
    }


    [HttpGet("{folder}/thanks.php")]
    public async Task<IActionResult> ProcessEvaluationCompleted()
    {
        try
        {
            var surveyPassword = HttpContext.Request.Query["Q0001"].ToString();
            var participantName = HttpContext.Request.Query["Q0002"].ToString();
            var rateeName = HttpContext.Request.Query["Q0003"].ToString();
            var evalFamiliar = HttpContext.Request.Query["Q0006"].ToString();
            var surveySession = HttpContext.Request.Query["FID"].ToString();
            var surveyTimestamp = HttpContext.Request.Query["FDT"].ToString();

            var success = await _surveyCompletionService.ProcessCompletedSurveyAsync(
                surveyPassword,
                participantName,
                rateeName,
                evalFamiliar,
                surveySession,
                surveyTimestamp);

            if (!success)
            {
                return BadRequest(ErrorMessages.UnableToProcessSurvey);
            }

            return RedirectToAction("ParticipantIndex", "Participant");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing completed evaluation");
            return BadRequest("An error occurred processing the survey.");
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvaluationParticipant(
        Guid evaluationRateeId, 
        Guid participantId, 
        Guid rateeId)
    {
        if (LocalRole == RoleNames.Participant && rateeId != LocalParticipantId)
        {
            return BadRequest("You can only delete your own raters.");
        }
        
        var success = await _participantService.DeleteEvaluationParticipantAsync(
            evaluationRateeId, 
            participantId);

        if (!success)
        {
            return NotFound("EvaluationParticipant not found.");
        }

        return Ok("Rater successfully removed from review.");
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrParticipant)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessInvitationsForParticipant(
        bool blnTest, 
        List<(Guid EvaluationRateeId, Guid ParticipantId)> participantTuples)
    {
        var success = await _invitationService.ProcessInvitationsAsync(
            participantTuples, 
            LocalClientId, 
            LocalEmail);

        if (!success)
        {
            return BadRequest("Failed to process invitations.");
        }

        return Ok("Invitations sent.");
    }
}
