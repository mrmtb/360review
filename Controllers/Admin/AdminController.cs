using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Constants;
using reviews.Models;
using reviews.Services;

namespace reviews.Controllers.Admin;

public class AdminController : Controller
{
    private readonly IParticipantService _participantService;
    private readonly IEvaluationService _evaluationService;
    private readonly IInvitationService _invitationService;
    private readonly IClientService _clientService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IReviewSessionService _sessionService;
    private readonly ILogger<AdminController> _logger;

    private Guid LocalClientId => GetSessionData()?.ClientId ?? Guid.Empty;
    private string LocalEmail => GetSessionData()?.Email ?? string.Empty;
    private string LocalRole => GetSessionData()?.Role ?? string.Empty;

    public AdminController(
        IParticipantService participantService,
        IEvaluationService evaluationService,
        IInvitationService invitationService,
        IClientService clientService,
        IEmailTemplateService emailTemplateService,
        IReviewSessionService sessionService,
        ILogger<AdminController> logger)
    {
        _participantService = participantService;
        _evaluationService = evaluationService;
        _invitationService = invitationService;
        _clientService = clientService;
        _emailTemplateService = emailTemplateService;
        _sessionService = sessionService;
        _logger = logger;
    }

    private SessionData? GetSessionData()
    {
        try
        {
            return _sessionService.GetSessionData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve session data");
            return null;
        }
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> People(string? tab = null)
    {
        var model = await BuildPeopleViewModelAsync();
        return View("ViewPeople", model);
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult ViewParticipants(string? tab = null)
    {
        return RedirectToAction(nameof(People));
    }

    // Backward-compatible route for legacy links/bookmarks.
    [HttpGet("/admin/addparticipant")]
    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult AddParticipant()
    {
        return RedirectToAction(nameof(People), new { tab = "participants" });
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult ViewRatees(string? tab = null)
    {
        return RedirectToAction(nameof(People));
    }

    private async Task<AdminPeopleViewModel> BuildPeopleViewModelAsync()
    {
        var people = await _participantService.GetAdminPeopleByClientAsync(LocalClientId);

        return new AdminPeopleViewModel
        {
            People = people
        };
    }

    private static string? NormalizePeopleTab(string? tab)
    {
        if (string.Equals(tab, "participants", StringComparison.OrdinalIgnoreCase))
        {
            return "participants";
        }

        if (string.Equals(tab, "ratees", StringComparison.OrdinalIgnoreCase))
        {
            return "ratees";
        }

        return null;
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ViewReminders()
    {
        var reminders = await _invitationService.GetRemindersByClientAsync(LocalClientId);
        return View(reminders);
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult RemindRatee(Guid participantId)
    {
        var participantIds = new List<Guid> { participantId };
        return RedirectToAction("ProcessReminders", new { participantIds });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> RemindPersonFromPeople(Guid participantId, string returnUrl = "People")
    {
        if (participantId == Guid.Empty)
        {
            TempData["PeopleError"] = ErrorMessages.InvalidParticipantId;
            return RedirectToListRoute(returnUrl, "participants");
        }

        var success = await _invitationService.ProcessRemindersAsync(
            new List<Guid> { participantId },
            LocalClientId,
            LocalEmail);

        TempData[success ? "PeopleSuccess" : "PeopleError"] =
            success ? "Reminder sent." : "Failed to process reminder.";

        return RedirectToListRoute(returnUrl, "participants");
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemindRateeAjax(Guid participantId)
    {
        if (participantId == Guid.Empty)
        {
            return BadRequest(ErrorMessages.InvalidParticipantId);
        }

        var participantIds = new List<Guid> { participantId };
        var success = await _invitationService.ProcessRemindersAsync(participantIds, LocalClientId, LocalEmail);
        if (!success)
        {
            return BadRequest("Failed to process reminder.");
        }

        return Ok(new { success = true, message = "Reminder sent." });
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult RemindSelectedRatees(List<Guid> participantIds)
    {
        if (participantIds == null || !participantIds.Any())
        {
            ModelState.AddModelError("", ErrorMessages.NoParticipantsSelected);
            return RedirectToAction("ViewReminders");
        }

        return RedirectToAction("ProcessReminders", new { participantIds });
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ProcessReminders(List<Guid> participantIds)
    {
        var success = await _invitationService.ProcessRemindersAsync(participantIds, LocalClientId, LocalEmail);
        
        if (!success)
        {
            ModelState.AddModelError("", "Failed to process reminders.");
        }

        return RedirectToAction("ViewReminders");
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> DumpParticipant(Guid participantId, string returnUrl, string? returnTab = null)
    {
        var viewModel = await _participantService.GetParticipantDumpAsync(participantId);

        if (viewModel == null)
        {
            return NotFound();
        }
        
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.ReturnTab = NormalizePeopleTab(returnTab) ?? "participants";
        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.AdminOrClient)]
    public async Task<IActionResult> GetRatersForEvaluation(Guid evaluationId, Guid participantId)
    {
        var raters = await _participantService.GetRatersForEvaluationAsync(evaluationId, participantId);
        return PartialView("_RatersTablePartial", raters);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.AdminOrClient)]
    public async Task<IActionResult> GetRateesForEvaluation(Guid evaluationId, Guid participantId)
    {
        var ratees = await _participantService.GetRateesForEvaluationAsync(evaluationId, participantId);
        return PartialView("_RateesTablePartial", ratees);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetEvaluationsForManage()
    {
        var evaluations = await _evaluationService.GetEvaluationsByClientAsync(LocalClientId);
        return PartialView("_ManageEvaluationsPartial", evaluations);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetClientsForManage()
    {
        var clients = await _clientService.GetAllClientsAsync();
        ViewData[ViewDataKeys.ActiveClient] = LocalClientId;
        return PartialView(PartialViewNames.ManageClientsPartial, clients);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetEmailTemplatesForManage()
    {
        _logger.LogInformation("Loading email templates for client {ClientId}", LocalClientId);
        var emailTemplates = await _emailTemplateService.GetEmailTemplatesByClientAsync(LocalClientId);
        return PartialView("_ManageEmailTemplatesPartial", emailTemplates);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> EditParticipant(Guid participantId, string returnUrl, string? returnTab = null)
    {
        var viewModel = await _participantService.GetParticipantDetailsAsync(participantId);

        if (viewModel == null)
        {
            return NotFound();
        }
        
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.ReturnTab = NormalizePeopleTab(returnTab) ?? "participants";
        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveParticipantToEvaluationPhase(
        Guid participantId,
        Guid fromEvaluationId,
        Guid toEvaluationId,
        string returnUrl,
        string? returnTab = null)
    {
        var (success, message) = await _participantService.MoveRateeToActiveEvaluationAsync(
            participantId,
            fromEvaluationId,
            toEvaluationId,
            LocalClientId);

        TempData[success ? "EditParticipantSuccess" : "EditParticipantError"] = message;
        if (success)
        {
            var fromEval = await _evaluationService.GetEvaluationDetailsAsync(fromEvaluationId);
            var toEval = await _evaluationService.GetEvaluationDetailsAsync(toEvaluationId);
            var fromName = fromEval?.EvaluationName ?? fromEvaluationId.ToString();
            var toName = toEval?.EvaluationName ?? toEvaluationId.ToString();
            TempData["EditParticipantAudit"] =
                $"Moved ratee from \"{fromName}\" to \"{toName}\" by {LocalEmail} on {DateTime.Now:MMMM d, yyyy h:mm tt}.";
        }

        return RedirectToAction(
            nameof(EditParticipant),
            new { participantId, returnUrl, returnTab });
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> SaveParticipant(Models.Participant participant, string returnUrl, string? returnTab = null)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToListRoute(returnUrl, returnTab);
        }

        var success = await _participantService.SaveParticipantAsync(participant);
        if (!success)
        {
            ModelState.AddModelError("", ErrorMessages.FailedToSaveParticipant);
        }

        return RedirectToListRoute(returnUrl, returnTab);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> AddParticipantToEvaluation(Guid participantId, string returnUrl, string? returnTab = null)
    {
        if (LocalClientId == Guid.Empty)
        {
            _logger.LogWarning("Session unavailable in AddParticipantToEvaluation.");
            return RedirectToAction(nameof(People), new { tab = "participants" });
        }

        if (participantId == Guid.Empty)
        {
            ModelState.AddModelError("", ErrorMessages.InvalidParticipantId);
            return RedirectToAction(nameof(People), new { tab = "participants" });
        }

        var success = await _participantService.AddParticipantToEvaluationAsync(participantId, LocalClientId);
        
        if (!success)
        {
            ModelState.AddModelError("", "An error occurred while adding the participant to the evaluation.");
            return RedirectToAction(nameof(People), new { tab = "participants" });
        }

        var activeEvaluationId = (await _evaluationService.GetEvaluationsByClientAsync(LocalClientId))
            .Where(e => e.EvaluationActive)
            .OrderByDescending(e => e.EvaluationOpenDate ?? DateTime.MinValue)
            .Select(e => e.EvaluationId)
            .FirstOrDefault();

        if (activeEvaluationId != Guid.Empty)
        {
            return RedirectToAction(
                "ViewRaters",
                "Participant",
                new { evaluationId = activeEvaluationId, participantID = participantId });
        }

        return RedirectToListRoute(returnUrl, returnTab);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ToggleParticipant(Guid participantId, string returnUrl, string? returnTab = null)
    {
        var success = await _participantService.ToggleParticipantStatusAsync(participantId);
        
        if (!success)
        {
            ModelState.AddModelError("", "An error occurred while toggling the participant.");
        }

        return RedirectToListRoute(returnUrl, returnTab);
    }

    private IActionResult RedirectToListRoute(string? returnUrl, string? returnTab)
    {
        var tab = NormalizePeopleTab(returnTab) ?? "participants";
        if (string.Equals(returnUrl, nameof(People), StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(People));
        }

        if (string.Equals(returnUrl, nameof(ViewRatees), StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(People));
        }

        if (string.Equals(returnUrl, nameof(ViewParticipants), StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(People));
        }

        return RedirectToAction(nameof(People));
    }

    // GET: ManageIndex
    public IActionResult ManageIndex(string selectedModel = "Evaluations")
    {
        if (LocalClientId == Guid.Empty)
        {
            _logger.LogWarning("Session unavailable in ManageIndex.");
        }

        ViewData["SelectedModel"] = selectedModel;
        return View();
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetEvaluationDetails(Guid id)
    {
        var evaluation = await _evaluationService.GetEvaluationDetailsAsync(id);

        if (evaluation == null)
        {
            return NotFound();
        }

        ViewData["SelectedModel"] = "Evaluations";
        return PartialView("_ManageEvaluationsDetailPartial", evaluation);
    }



    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvaluation(Evaluation model, string selectedModel)
    {
        if (model == null || model.ClientId == Guid.Empty)
        {
            return BadRequest(ErrorMessages.InvalidEvaluationData);
        }

        var success = await _evaluationService.SaveEvaluationAsync(model, LocalClientId);
        
        if (!success)
        {
            return Forbid();
        }

        return RedirectToAction("ManageIndex", new { selectedModel });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> SetActiveClient(Guid clientId)
    {
        var client = await _clientService.GetClientDetailsAsync(clientId);

        if (client == null)
        {
            return NotFound();
        }

        var sessionData = GetSessionData();
        if (sessionData != null)
        {
            sessionData.ClientId = client.ClientId;
            sessionData.ClientName = client.ClientName;
            sessionData.ClientLogoLocation = client.ClientLogoLocation;
            _sessionService.SetSessionData(sessionData);
        }

        return RedirectToAction("ManageIndex", new { selectedModel = "Clients" });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetClientDetails(Guid id)
    {
        var client = await _clientService.GetClientDetailsAsync(id);

        if (client == null)
        {
            return NotFound();
        }

        ViewData["SelectedModel"] = "Clients";
        return PartialView("_ManageClientsDetailPartial", client);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveClient(Client model, string selectedModel)
    {
        if (model == null)
        {
            return BadRequest(ErrorMessages.InvalidClientData);
        }

        var success = await _clientService.SaveClientAsync(model);
        
        if (!success)
        {
            ModelState.AddModelError("", ErrorMessages.FailedToSaveClient);
        }

        return RedirectToAction("ManageIndex", new { selectedModel });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetEmailTemplateDetails(Guid id)
    {
        var emailTemplate = await _emailTemplateService.GetEmailTemplateDetailsAsync(id, LocalClientId);

        if (emailTemplate == null)
        {
            _logger.LogWarning("Email template {TemplateId} not found for client {ClientId}", id, LocalClientId);
            return NotFound($"Could not find template with ID: {id} for Client ID: {LocalClientId}");
        }

        ViewData["SelectedModel"] = "EmailTemplates";
        return PartialView("_ManageEmailTemplatesDetailPartial", emailTemplate);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEmailTemplate(EmailTemplate model, string selectedModel)
    {
        if (model == null || model.ClientId == Guid.Empty)
        {
            return BadRequest(ErrorMessages.InvalidEmailTemplateData);
        }

        var success = await _emailTemplateService.SaveEmailTemplateAsync(model);
        
        if (!success)
        {
            ModelState.AddModelError("", ErrorMessages.FailedToSaveEmailTemplate);
        }

        return RedirectToAction("ManageIndex", new { selectedModel });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ViewInvitations()
    {
        var invitations = await _invitationService.GetInvitationsByClientAsync(LocalClientId);
        return View(invitations);
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult InviteParticipant(Guid evaluationRateeId, Guid participantId)
    {
        var participantIds = new List<Guid> { participantId };
        return RedirectToAction("ProcessInvitations", new { participantIds, evaluationRateeId });
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InviteParticipantAjax(Guid evaluationRateeId, Guid participantId)
    {
        if (evaluationRateeId == Guid.Empty || participantId == Guid.Empty)
        {
            return BadRequest(ErrorMessages.InvalidParticipantId);
        }

        var participantTuples = new List<(Guid EvaluationRateeId, Guid ParticipantId)>
        {
            (evaluationRateeId, participantId)
        };

        var success = await _invitationService.ProcessInvitationsAsync(participantTuples, LocalClientId, LocalEmail);
        if (!success)
        {
            return BadRequest(ErrorMessages.FailedToProcessInvitations);
        }

        return Ok(new { success = true, message = "Invitation sent." });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ProcessInvitations(List<(Guid EvaluationRateeId, Guid ParticipantId)>? participantTuples)
    {
        var success = await _invitationService.ProcessInvitationsAsync(participantTuples, LocalClientId, LocalEmail);
        
        if (!success)
        {
            ModelState.AddModelError("", ErrorMessages.FailedToProcessInvitations);
        }

        return RedirectToAction("ViewInvitations");
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> EmailQueueDashboard()
    {
        var emailQueue = await _emailTemplateService.GetEmailQueueAsync();
        return View(emailQueue);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> PreviewEmail(int id)
    {
        var email = await _emailTemplateService.GetEmailDetailsAsync(id);
        if (email == null)
        {
            return NotFound();
        }

        return PartialView("_EmailPreview", email);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> RetryEmail(int id)
    {
        var success = await _emailTemplateService.RetryEmailAsync(id);
        if (!success)
        {
            return BadRequest(ErrorMessages.EmailNotFoundOrSent);
        }

        return RedirectToAction(ActionNames.EmailQueueDashboard);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryEmailAjax(int id)
    {
        var success = await _emailTemplateService.RetryEmailAsync(id);
        if (!success)
        {
            return BadRequest(ErrorMessages.EmailNotFoundOrSent);
        }

        return Ok(new { success = true, message = "Email retry queued." });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> CancelEmail(int id)
    {
        var success = await _emailTemplateService.CancelEmailAsync(id);
        if (!success)
        {
            return BadRequest(ErrorMessages.EmailNotFoundOrSent);
        }

        return RedirectToAction(ActionNames.EmailQueueDashboard);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEmailAjax(int id)
    {
        var success = await _emailTemplateService.CancelEmailAsync(id);
        if (!success)
        {
            return BadRequest(ErrorMessages.EmailNotFoundOrSent);
        }

        return Ok(new { success = true, message = "Email cancelled." });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult CreateItem(string selectedModel)
    {
        switch (selectedModel)
        {
            case "GetEvaluationsForManage":
                ViewData["SelectedModel"] = "Evaluations";
                return PartialView("_ManageEvaluationsDetailPartial", 
                    _evaluationService.CreateNewEvaluation(LocalClientId));
            
            case "GetClientsForManage":
                ViewData["SelectedModel"] = "Clients";
                return PartialView("_ManageClientsDetailPartial", 
                    _clientService.CreateNewClient());
            
            case "GetEmailTemplatesForManage":
                ViewData["SelectedModel"] = "EmailTemplates";
                return PartialView("_ManageEmailTemplatesDetailPartial", 
                    _emailTemplateService.CreateNewEmailTemplate(LocalClientId));
            
            default:
                return Content("Unknown model type: " + selectedModel);
        }
    }
}
