using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using reviews.Models;
using reviews.Services;
using Microsoft.EntityFrameworkCore;

namespace reviews.Controllers.ParticipantController;
public class ParticipantController : Controller
{

    private readonly ApplicationDBContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ReviewSessionService _sessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ParticipantController> _logger;
    private readonly IConfiguration _configuration;
    private readonly EmailQueueHelper _emailQueueHelper;

    private Guid localClientId = new Guid();
    private string localClientName = "";
    private Guid localUserId;
    private Guid localParticipantId;
    private string localEmail = "";
    private string localRole = "";
    private string localClientLogoLocation = "";
    private int localDefaultEvaluationId = -1;
    private int localOpenRatings = -1;
    public ParticipantController(ApplicationDBContext db, UserManager<ApplicationUser> userManager, ReviewSessionService sessionService,
                                 IHttpContextAccessor httpContextAccessor, ILogger<ParticipantController> logger, IConfiguration configuration,
                                 EmailQueueHelper emailQueueHelper)
    {
        _db = db;
        _userManager = userManager;
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger;
        _configuration = configuration;
        _emailQueueHelper = emailQueueHelper ?? throw new ArgumentNullException(nameof(emailQueueHelper));
        _configuration = configuration;

        var sessionData = _sessionService.GetSessionData();

        if (sessionData != null)
        {
            localUserId = sessionData.UserId;
            localParticipantId = sessionData.ParticipantId;
            localEmail = sessionData.Email;
            localRole = sessionData.Role;
            localClientId = sessionData.ClientId;
            localClientName = sessionData.ClientName;
            localClientLogoLocation = sessionData.ClientLogoLocation;
            localDefaultEvaluationId = sessionData.DefaultEvaluationId;
            localOpenRatings = sessionData.OpenRatingCount;
        }
    }

    [Authorize(Roles = "Admin, Participant")]
    public IActionResult ParticipantIndex(int? EvaluationId, Guid? ParticipantId)
    {

        var participant = new List<vwEvaluationsToComplete>();
        // if EvaluationId was not passed through, get the most-recent EvaluationId for the active ParticipantId
        // Admin should always pass through an Evaluation Id to this page
        if (localRole == "Participant" && !EvaluationId.HasValue)
        {
            participant = _db.ViewEvaluationsToComplete
                             .Where(p => p.Rater == localParticipantId
                                      && p.EvaluationActive == true)
                             .ToList();
        }
        else if (localRole == "Admin" && EvaluationId != null && ParticipantId != null)
        {
            // If Admin, we can filter by EvaluationId and ParticipantId
            participant = _db.ViewEvaluationsToComplete
                .Where(p => p.Rater == localParticipantId && p.EvaluationId == EvaluationId && p.ParticipantId == ParticipantId)
                .ToList();
        }
        else if (EvaluationId != null)  // If EvaluationId is provided, filter by it
        {
            participant = _db.ViewEvaluationsToComplete.Where(p => p.Rater == ParticipantId && p.EvaluationId == EvaluationId).ToList(); // or filter as needed
        }
        else //confirm the logic here
        {
            // Not certain about the circumstance that would lead to this else block, but if EvaluationId is not provided,
            // we can just get all evaluations for the participant
            participant = _db.ViewEvaluationsToComplete.Where(p => p.Rater == localParticipantId).ToList(); // or filter as needed
        }
        return View(participant);
    }

    [HttpGet("Participant/ViewRaters/{evaluationId?}/{participantID?}")]
    [Authorize(Roles = "Admin,Participant")]
    public IActionResult ViewRaters(int? EvaluationID, Guid? participantID)
    {
        var Raters = new List<vwRatersByRatee>();

        if (localRole == "Participant")
        {
            Raters = _db.ViewRatersByRatees
                .Where(p => p.Ratee == localParticipantId && p.EvaluationActive == true)
                .ToList();
        }
        else if (localRole == "Admin" && participantID.HasValue)
        {
            if (EvaluationID.HasValue)
            {
                // Admin can filter by EvaluationId and ParticipantId
                Raters = _db.ViewRatersByRatees
                    .Where(p => p.EvaluationId == EvaluationID && p.Ratee == participantID)
                    .ToList();
            }
            else
            {
                // If only ParticipantId is provided, get all raters for that participant
                Raters = _db.ViewRatersByRatees
                    .Where(p => p.Ratee == participantID && p.EvaluationActive == true)
                    .ToList();
            }
        }
        else
        {
            return Ok("Invalid request. Please provide a valid Participant ID.");
        }

        var maxRaters = 0;
        var ParticipantRoles = _db.ParticipantRoles.ToList();
        Guid? EvaluationRateeId = new Guid();

        if (Raters.Any())
        {
            EvaluationRateeId = Raters.FirstOrDefault()?.EvaluationRateeId;
            maxRaters = Raters.FirstOrDefault()?.EvaluationMaxRaters ?? 0;
        }

        ViewBag.MaxRaters = maxRaters;

        return View(new ViewRatersViewModel
        {
            Raters = Raters,
            ParticipantRoles = ParticipantRoles,
            EvaluationRateeId = EvaluationRateeId ?? Guid.Empty
        });
    }

    [HttpGet("Participant/GetEligibleRaters/{EvaluationRateeId}")]
    [Authorize(Roles = "Admin,Participant")]
    public IActionResult GetEligibleRaters(String EvaluationRateeId)
    {
        var eligibleRaters = new List<EligibleRaters>();

        if (!Guid.TryParse(EvaluationRateeId, out Guid outGuid) || outGuid == Guid.Empty)
        {
            LogWarningMessage($"GetEligibleRaters called with bad EvaluationRateeId: {EvaluationRateeId}");

            eligibleRaters = new List<EligibleRaters>
            {
                new EligibleRaters
                {
                    EvaluationRateeId = Guid.Empty,
                    ParticipantId = Guid.Empty,
                    ParticipantNameFirst = "",
                    ParticipantNameLast = "Error returning Eligible Raters",
                    ParticipantEmail = "",
                    ParticipantHasRegistered = false
                }
            };
        }
        else
        {
            LogWarningMessage($"GetEligibleRaters called with EvaluationRateeId: {outGuid}");

            eligibleRaters = _db.fnEligibleRaters(outGuid)
                .Select(x => new EligibleRaters
                {
                    EvaluationRateeId = x.EvaluationRateeId,
                    ParticipantId = x.ParticipantId,
                    ParticipantNameFirst = x.ParticipantNameFirst,
                    ParticipantNameLast = x.ParticipantNameLast,
                    ParticipantEmail = x.ParticipantEmail,
                    ParticipantHasRegistered = x.ParticipantHasRegistered
                })
                .ToList();
        }

        return Json(eligibleRaters);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Participant")]
    public IActionResult AddNewRater(Guid evaluationRateeId, string firstName, string lastName, string email, string rolename)
    {
        // Add the new rater to the database
        var newRater = new Participant
        {
            ParticipantId = Guid.NewGuid(),
            ParticipantNameFirst = firstName,
            ParticipantNameLast = lastName,
            ParticipantEmail = email,
            ClientId = localClientId
        };

        _db.Participants.Add(newRater);
        _db.SaveChanges();

        var newEvaluationParticipant = new EvaluationParticipant
        {
            EvaluationRateeId = evaluationRateeId,
            ParticipantId = newRater.ParticipantId,
            ParticipantRoleId = _db.ParticipantRoles.FirstOrDefault(r => r.ParticipantRoleName == rolename)?.ParticipantRoleId ?? 0,
            EvaluationParticipantStatusId = 1,
            EvaluationParticipantSurveyPassword = "",
            EvaluationParticipantDateAdded = DateTime.UtcNow
        };

        _db.EvaluationParticipants.Add(newEvaluationParticipant);
        _db.SaveChanges();

        return RedirectToAction("ViewRaters");
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Participant")]
    public IActionResult AddRater(Guid evaluationRateeId, Guid participantId, string rolename)
    {
        var newEvaluationParticipant = new EvaluationParticipant
        {
            EvaluationRateeId = evaluationRateeId,
            ParticipantId = participantId,
            ParticipantRoleId = _db.ParticipantRoles.FirstOrDefault(r => r.ParticipantRoleName == rolename)?.ParticipantRoleId ?? 0,
            EvaluationParticipantStatusId = 1,
            EvaluationParticipantSurveyPassword = "",
            EvaluationParticipantDateAdded = DateTime.UtcNow
        };

        _db.EvaluationParticipants.Add(newEvaluationParticipant);
        _db.SaveChanges();

        return RedirectToAction("ViewRaters");
    }

    [HttpGet]
    public IActionResult GetRatersForEvaluation(int evaluationId, Guid participantId)
    {
        var raters = _db.ViewRatersByRatees
            .Where(r => r.EvaluationId == evaluationId && r.Ratee == participantId)
            .ToList();
        return PartialView("_RatersTablePartial", raters);
    }


    [HttpGet]
    public IActionResult LaunchEvaluation(Guid evaluationRateeId, Guid participantId)
    {

        if (evaluationRateeId == Guid.Empty || participantId == Guid.Empty)
        {
            return BadRequest("Invalid evaluation ratee or participant ID.");
        }

        var evaluationToComplete = _db.ViewEvaluationsToComplete
            .FirstOrDefault(e => e.EvaluationRateeId == evaluationRateeId && e.Rater == participantId);

        if (evaluationToComplete == null)
        {
            return NotFound("Evaluation not found.");
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        var siteUrl = request != null ? $"{request.Scheme}://{request.Host}" : "";

        if (siteUrl == "")
        {
            return BadRequest("Site URL could not be determined.");
        }
        else
        {
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }
        }

        var surveyUrl = siteUrl +
                        evaluationToComplete.EvaluationFolder +
                        "?P1=EL0&" +
                        "Q0001=" + evaluationToComplete.EvaluationParticipantSurveyPassword + "&" +
                        "Q0002=" + evaluationToComplete.RaterNameFirst + "+" + evaluationToComplete.RaterNameLast + "&" +
                        "Q0003=" + evaluationToComplete.ParticipantNameFirst + "+" + evaluationToComplete.ParticipantNameLast + "&" +
                        "Q0004=" + evaluationToComplete.ParticipantRoleId + "&" +
                        "Q0005=" + evaluationToComplete.EvaluationName;

        LogWarningMessage($"Launching evaluation at URL: {surveyUrl}");

        return Redirect(surveyUrl);
    }


    [HttpGet("thanks.php")]
    public IActionResult ProcessEvaluationCompleted()
    {
        // Logic to process the completed evaluation
        var svy_password = HttpContext.Request.Query["Q0001"].ToString();
        var participant_name = HttpContext.Request.Query["Q0002"].ToString();
        var ratee_name = HttpContext.Request.Query["Q0003"].ToString();
        var eval_familiar = HttpContext.Request.Query["Q0006"];
        var svy_session = HttpContext.Request.Query["FID"].ToString();
        var svy_fdt = HttpContext.Request.Query["FDT"].ToString();

        var evaluationParticipant = _db.EvaluationParticipants
            .Where(ep => ep.EvaluationParticipantSurveyPassword == svy_password);

        // test this condition first in the event we need to filter down evaluationParticipant
        if (evaluationParticipant.Count() > 1)
        {

            // Let's try to isolate the specific participant for this evaluation
            var specificParticipant = _db.Participants
                .Where(p => p.ParticipantNameLast == participant_name.Split(new[] { ' ' }).Last() &&
                            p.ParticipantNameFirst == participant_name.Split(new[] { ' ' }).First())
                .Select(p => p.ParticipantId);

            if (specificParticipant.Any())
            {
                evaluationParticipant = evaluationParticipant
                    .Where(ep => specificParticipant.Contains(ep.ParticipantId));
            }

            if (evaluationParticipant.Count() > 1)
            {
                // If we still have multiple participants, log the issue and return an error
                LogWarningMessage($"Multiple evaluation participant rows found with survey password: {svy_password}. Additional info: {participant_name}, {ratee_name}, {eval_familiar}, {svy_session}, {svy_fdt}");
                return BadRequest("Multiple survey password rows found.");
            }

        }

        if (!evaluationParticipant.Any())
        {
            LogWarningMessage($"No evaluation participant found with survey password: {svy_password}, {participant_name}, {ratee_name}, {eval_familiar}, {svy_session}, {svy_fdt}");
            return BadRequest("Invalid survey password.");
        }

        if (evaluationParticipant.Count() == 1)
        {
            var participant = evaluationParticipant.First();
            // if (eval_familiar == 2) then they are complete, else they are not familiar
            participant.EvaluationParticipantStatusId = _db.EvaluationParticipantStatuses
                                                           .FirstOrDefault(s => s.EvaluationParticipantStatusName == (eval_familiar == 2 ? "Complete" : "Not Familiar"))?.EvaluationParticipantStatusId;
            _db.SaveChanges();
        }
        else
        {
            LogWarningMessage($"We should not have gotten to this point with the following information: {svy_password}. Additional info: {participant_name}, {ratee_name}, {eval_familiar}, {svy_session}, {svy_fdt}");
        }

        return RedirectToAction("Participant", "ParticipantIndex");
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteEvaluationParticipant(Guid evaluationRateeId, Guid participantId, Guid rateeId)
    {
        if (localRole == "Participant" && rateeId != localParticipantId)
        {
            return BadRequest("You can only delete your own raters.");
        }
        
        try
        {
            var evaluationParticipant = _db.EvaluationParticipants
                .FirstOrDefault(ep => ep.EvaluationRateeId == evaluationRateeId && ep.ParticipantId == participantId);

            if (evaluationParticipant == null)
            {
                return NotFound("EvaluationParticipant not found.");
            }

            _db.EvaluationParticipants.Remove(evaluationParticipant);
            _db.SaveChanges();

            return Ok("Rater successfully removed from review.");
        }
        catch (Exception ex)
        {
            LogWarningMessage($"Error deleting EvaluationParticipant where EvaluationRateeId = {evaluationRateeId}, ParticipantId = {participantId}, RateeId = {rateeId}, Role = {localRole}, Email = {localEmail}. Exception message is : {ex.Message}");
            return StatusCode(500, "An error occurred while deleting the EvaluationParticipant.");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Participant")]
    public async Task<IActionResult> ProcessInvitationsForParticipant(bool blnTest, List<(Guid EvaluationRateeId, Guid ParticipantId)> participantTuples)
    {
        var raterInvitations = new List<vwInvitations>(_db.ViewRaterInvitations
            .Where(p => p.ClientId == localClientId));

        var invitations = raterInvitations
            .OrderBy(p => p.RaterLastName)
            .ThenBy(p => p.RaterFirstName)
            .ToList();

        if (participantTuples.Any())
        {
            // If participantTuples are provided, use them to filter invitations
            invitations = invitations.Where(i => participantTuples.Any(t => t.ParticipantId == i.RaterParticipantId && t.EvaluationRateeId == i.EvaluationRateeId)).ToList();
        }

        var messages = new List<EmailTemplate>(_db.EmailTemplates
            .Where(t => t.ClientId == localClientId));

        await _emailQueueHelper.ProcessParticipantsForEmailQueueAsync(invitations, messages, blnTest, localEmail);

        return Ok("Invitations sent.");
    }

    private void LogWarningMessage(string logMessage)
    {
        if (bool.TryParse(_configuration["EnableLogging"], out var enableLogging) && enableLogging)
        {
            _logger.LogWarning(logMessage);
        }
    }
}