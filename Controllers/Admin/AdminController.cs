using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public async Task<IActionResult> ViewParticipants()
    {
        var participants = await _participantService.GetParticipantsByClientAsync(LocalClientId);
        return View(participants);
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ViewRatees()
    {
        var ratees = await _evaluationService.GetRateesByClientAsync(LocalClientId);
        return View(ratees);
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
    public async Task<IActionResult> DumpParticipant(Guid participantId, string returnUrl)
    {
        var viewModel = await _participantService.GetParticipantDumpAsync(participantId);

        if (viewModel == null)
        {
            return NotFound();
        }
        
        ViewBag.ReturnUrl = returnUrl;
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
    public async Task<IActionResult> EditParticipant(Guid participantId, string returnUrl)
    {
        var viewModel = await _participantService.GetParticipantDetailsAsync(participantId);

        if (viewModel == null)
        {
            return NotFound();
        }
        
        ViewBag.ReturnUrl = returnUrl;
        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> SaveParticipant(Participant participant, string returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(returnUrl);
        }

        var success = await _participantService.SaveParticipantAsync(participant);
        if (!success)
        {
            ModelState.AddModelError("", ErrorMessages.FailedToSaveParticipant);
        }

        return RedirectToAction(returnUrl);
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> AddParticipantToEvaluation(Guid participantId, string returnUrl)
    {
        if (participantId == Guid.Empty)
        {
            ModelState.AddModelError("", ErrorMessages.InvalidParticipantId);
            return RedirectToAction(returnUrl);
        }

        var success = await _participantService.AddParticipantToEvaluationAsync(participantId, LocalClientId);
        
        if (!success)
        {
            ModelState.AddModelError("", "An error occurred while adding the participant to the evaluation.");
            return RedirectToAction(ActionNames.Error);
        }

        return returnUrl != null ? RedirectToAction(returnUrl) : RedirectToAction("ViewParticipants");
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ToggleParticipant(Guid participantId, string returnUrl)
    {
        var success = await _participantService.ToggleParticipantStatusAsync(participantId);
        
        if (!success)
        {
            ModelState.AddModelError("", "An error occurred while toggling the participant.");
        }

        return RedirectToAction(returnUrl);
    }

    // GET: ManageIndex
    public IActionResult ManageIndex(string selectedModel = "Evaluations")
    {
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