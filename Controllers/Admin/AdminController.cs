using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using reviews.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using reviews.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

public class AdminController : Controller
{

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ReviewSessionService _sessionService;
    private readonly EmailQueueHelper _emailQueueHelper;

    private Guid localClientId = new Guid();
    //private string localClientName = "";
    //private Guid localUserId;
    private string localEmail = "";
    private string localRole = "";
    //private string localClientLogoLocation = "";
    //private int localDefaultEvaluationId = -1;
    //private int localOpenRatings = -1;
    //private string EvaluationsText = "";
    //private string RatersText = "";

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ReviewSessionService sessionService, EmailQueueHelper emailQueueHelper)
    {
        _db = db;
        _userManager = userManager;
        _sessionService = sessionService;
        _emailQueueHelper = emailQueueHelper;

        var sessionData = _sessionService.GetSessionData();

        if (sessionData != null)
        {
            //localUserId = ViewBag.UserId;
            localEmail = sessionData.Email;
            localRole = sessionData.Role;
            localClientId = sessionData.ClientId;
            //localClientName = ViewBag.ClientName;
            //localClientLogoLocation = ViewBag.ClientLogoLocation;
            //localDefaultEvaluationId = ViewBag.DefaultEvaluationId;
            //localOpenRatings = ViewBag.OpenRatingCount;
        }

    }

    [Authorize(Roles = "Admin")]
    public IActionResult ViewParticipants()
    {
        var participants = _db.ViewAdminParticipants.Where(p => p.ClientId == localClientId).ToList();
        return View(participants);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ViewRatees()
    {
        var ratees = _db.ViewAdminRatees.Where(p => p.ClientId == localClientId).ToList();
        return View(ratees);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ViewReminders()
    {
        var redminders = _db.ViewReminders.Where(p => p.ClientId == localClientId).ToList();
        return View(redminders);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult RemindRatee(Guid participantId)
    {
        var participantIds = new List<Guid> { participantId };
        return RedirectToAction("ProcessReminders", new { participantIds });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult RemindSelectedRatees(List<Guid> participantIds)
    {
        if (participantIds == null || !participantIds.Any())
        {
            ModelState.AddModelError("", "No participants selected for reminders.");
            return RedirectToAction("ViewReminders");
        }

        return RedirectToAction("ProcessReminders", new { participantIds });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessReminders(List<Guid> participantIds)
    {
        if (participantIds == null || !participantIds.Any())
        {
            ModelState.AddModelError("", "No participants selected for reminders.");
            return RedirectToAction("ViewReminders");
        }

        var lastUniqueEmail = "";

        var reminders = await _db.ViewReminders
                                 .Where(p => p.ClientId == localClientId && participantIds.Contains(p.ParticipantId))
                                 .OrderBy(o => o.ParticipantEmail)
                                 .ToListAsync();

        if (!reminders.Any())
        {
            ModelState.AddModelError("", "No reminders found for the selected participants.");
            return RedirectToAction("ViewReminders");
        }

        var EmailTemplates = _db.EmailTemplates
            .Where(p => p.ClientId == localClientId && p.EmailTemplateName == "Reminder - General")
            .ToList();

        foreach (var reminder in reminders)
        {
            if (lastUniqueEmail == "")
            {
                lastUniqueEmail = reminder.ParticipantEmail;
            }
            else if (lastUniqueEmail == reminder.ParticipantEmail)
            {
                continue; // Skip if the email is the same as the last one processed
            }
            else
            {
                lastUniqueEmail = reminder.ParticipantEmail;
            }

            await _emailQueueHelper.ProcessParticipantsForEmailQueueAsync( MapRemindersToInvitations(new List<vwReminders> { reminder }) , EmailTemplates, false, localEmail);
        }

        return RedirectToAction("ViewReminders");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult DumpParticipant(Guid participantId, string returnUrl)
    {
        var AdminDumpParticipantViewModel = new AdminParticipantViewModel();
        AdminDumpParticipantViewModel.Participant = _db.Participants.FirstOrDefault(p => p.ParticipantId == participantId);
        AdminDumpParticipantViewModel.Raters = _db.ViewRatersByRatees.Where(p => p.Ratee == participantId).ToList();
        AdminDumpParticipantViewModel.Ratees = _db.ViewEvaluationsToComplete.Where(p => p.ParticipantId == participantId).ToList();
        AdminDumpParticipantViewModel.Evaluations = _db.ViewEvaluationsByRatees.Where(p => p.ParticipantId == participantId).ToList();

        if (AdminDumpParticipantViewModel.Participant == null)
        {
            return NotFound();
        }
        ViewBag.ReturnUrl = returnUrl;
        return View(AdminDumpParticipantViewModel);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Client")]
    public IActionResult GetRatersForEvaluation(Guid evaluationId, Guid participantId)
    {
        var raters = _db.ViewRatersByRatees
            .Where(r => r.EvaluationId == evaluationId && r.Ratee == participantId)
            .ToList();
        return PartialView("_RatersTablePartial", raters);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Client")]
    public IActionResult GetRateesForEvaluation(Guid evaluationId, Guid participantId)
    {
        var ratees = _db.ViewEvaluationsToComplete
            .Where(r => r.EvaluationId == evaluationId && r.Rater == participantId)
            .ToList();
        return PartialView("_RateesTablePartial", ratees);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetEvaluationsForManage()
    {
        var evaluations = _db.Evaluations
            .Include(e => e.Client)
            .Where(r => r.ClientId == localClientId)
            .ToList();
        return PartialView("_ManageEvaluationsPartial", evaluations);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetClientsForManage()
    {
        var clients = _db.Clients
            .ToList();

        // pass in the localClientId to the ViewData to highlight the active client in the UI
        ViewData["ActiveClient"] = localClientId;
        return PartialView("_ManageClientsPartial", clients);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetEmailTemplatesForManage()
    {
        Console.WriteLine($"Local Client ID: {localClientId}");
        var emailTemplates = _db.EmailTemplates
            .Include(et => et.Client)
            .Where(r => r.ClientId == localClientId)
            .ToList();
        return PartialView("_ManageEmailTemplatesPartial", emailTemplates);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult EditParticipant(Guid participantId, string returnUrl)
    {
        var AdminParticipantViewModel = new AdminParticipantViewModel();
        AdminParticipantViewModel.Participant = _db.Participants.FirstOrDefault(p => p.ParticipantId == participantId);
        AdminParticipantViewModel.Raters = new List<vwRatersByRatee>(); // no need to pass it in, the JS will populate on start _db.ViewRatersByRatees.Where(p => p.Ratee == participantId).ToList();
        AdminParticipantViewModel.Ratees = new List<vwEvaluationsToComplete>(); // no need to pass it in, the JS will populate on start _db.ViewEvaluationsToComplete.Where(p => p.Rater == participantId).ToList();
        AdminParticipantViewModel.Evaluations = _db.ViewEvaluationsByRatees.Where(p => p.ParticipantId == participantId).ToList();

        if (AdminParticipantViewModel.Participant == null)
        {
            return NotFound();
        }
        ViewBag.ReturnUrl = returnUrl;
        return View(AdminParticipantViewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult SaveParticipant(Participant participant, string returnUrl)
    {
        if (ModelState.IsValid)
        {
            _db.Participants.Update(participant);
            _db.SaveChanges();
            return RedirectToAction(returnUrl);
        }

        return RedirectToAction(returnUrl);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult AddParticipantToEvaluation(Guid participantId, string returnUrl)
    {
        if (participantId == Guid.Empty)
        {
            ModelState.AddModelError("", "Invalid participantId.");
            return RedirectToAction(returnUrl);
        }
        using (var transaction = _db.Database.BeginTransaction())
        {
            try
            {
                // Create the EvaluationRatee
                var EvaluationRatee = new EvaluationRatee
                {
                    EvaluationRateeId = Guid.NewGuid(),
                    EvaluationId = _db.Evaluations
                        .Where(e => e.ClientId == localClientId && e.EvaluationActive)
                        .Select(e => e.EvaluationId)
                        .FirstOrDefault(), // Get the first active evaluation for the client
                    ParticipantId = participantId,
                    EvaluationRateeStatusId = _db.EvaluationRateeStatuses
                        .Where(e => e.EvaluationRateeStatusName == "Draft")
                        .Select(e => e.EvaluationRateeStatusId)
                        .FirstOrDefault(),
                    EvaluationRateeDateAdded = DateTime.UtcNow
                };
                _db.EvaluationRatees.Add(EvaluationRatee);
                _db.SaveChanges();

                // Create the EvaluationParticipant
                var EvaluationParticipant = new EvaluationParticipant
                {
                    EvaluationRateeId = EvaluationRatee.EvaluationRateeId,
                    ParticipantId = participantId,
                    ParticipantRoleId = _db.ParticipantRoles
                        .Where(r => r.ParticipantRoleName == "Self")
                        .Select(r => r.ParticipantRoleId)
                        .FirstOrDefault(),
                    EvaluationParticipantStatusId = _db.EvaluationParticipantStatuses
                        .Where(e => e.EvaluationParticipantStatusName == "Draft")
                        .Select(e => e.EvaluationParticipantStatusId)
                        .FirstOrDefault(),
                    EvaluationParticipantSurveyPassword = RandomValueGenerator.GenerateRandomValue(),
                    EvaluationParticipantDateAdded = DateTime.UtcNow
                };
                _db.EvaluationParticipants.Add(EvaluationParticipant);
                _db.SaveChanges();

                // Commit the transaction
                transaction.Commit();

                return returnUrl != null ? RedirectToAction(returnUrl) : RedirectToAction("ViewParticipants");
            }
            catch (Exception ex)
            {
                // Rollback the transaction if an error occurs
                transaction.Rollback();
                ModelState.AddModelError("", "An error occurred while adding the participant to the evaluation.");
                Debug.WriteLine($"Transaction failed: {ex.Message}");
                return RedirectToAction("Error"); // Redirect to an error page or handle appropriately
            }
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult ToggleParticipant(Guid participantId, string returnUrl)
    {
        var participant = _db.Participants.FirstOrDefault(p => p.ParticipantId == participantId);
        if (participant == null)
        {
            return NotFound();
        }
        else
        {
            participant.ParticipantIsActive = !participant.ParticipantIsActive;
            _db.Participants.Update(participant);
            _db.SaveChanges();
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
    [Authorize(Roles = "Admin")]
    public IActionResult GetEvaluationDetails(Guid id)
    {
        var evaluation = _db.Evaluations
            .Include(e => e.Client) // Include related data if necessary
            .FirstOrDefault(e => e.EvaluationId == id);

        if (evaluation == null)
        {
            return NotFound();
        }

        ViewData["SelectedModel"] = "Evaluations";
        return PartialView("_ManageEvaluationsDetailPartial", evaluation);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvaluation(Evaluation model, string selectedModel)
    {
        if (model == null || model.ClientId == Guid.Empty)
        {
            return BadRequest("Invalid Evaluation data.");
        }

        // Retrieve the existing Evaluation from the database
        var evaluation = await _db.Evaluations.FirstOrDefaultAsync(e => e.EvaluationId == model.EvaluationId && e.ClientId == localClientId);
        if (evaluation == null)
        {
            evaluation = new Evaluation
            {
                EvaluationId = model.EvaluationId,
                ClientId = model.ClientId
            };
        }

        var properties = typeof(Evaluation).GetProperties();
        foreach (var prop in properties)
        {
            // Skip EvaluationId and ClientId (or any other keys you don't want to update)
            if (prop.Name == "EvaluationId" || prop.Name == "ClientId") continue;

            var newValue = prop.GetValue(model);
            var oldValue = prop.GetValue(evaluation);

            // Special handling for nullable DateTime properties to ensure UTC conversion
            if (prop.PropertyType == typeof(DateTime?) && newValue != null)
            {
                var dateValue = (DateTime?)newValue;
                if (dateValue.HasValue)
                {
                    var utcValue = DateTime.SpecifyKind(dateValue.Value, DateTimeKind.Local).ToUniversalTime();
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

    if (evaluation.EvaluationId == Guid.Empty)
        {
            _db.Evaluations.Add(evaluation);
        }
        else
        {
            _db.Evaluations.Update(evaluation);
        }
        
        await _db.SaveChangesAsync();
        return RedirectToAction("ManageIndex", new { selectedModel });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult SetActiveClient(Guid clientId)
    {
        var client = _db.Clients
            .FirstOrDefault(c => c.ClientId == clientId);

        if (client == null)
        {
            return NotFound();
        }

        var sessionData = _sessionService.GetSessionData();

        if (sessionData != null)
        {
            sessionData.ClientId = client.ClientId;
            sessionData.ClientName = client.ClientName;
            sessionData.ClientLogoLocation = client.ClientLogoLocation;
            _sessionService.SetSessionData(sessionData);
        }

        //ViewData["SelectedModel"] = "Clients";
        return RedirectToAction("ManageIndex", new { selectedModel = "Clients" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetClientDetails(Guid id)
    {
        var client = _db.Clients
            .FirstOrDefault(c => c.ClientId == id);

        if (client == null)
        {
            return NotFound();
        }

        ViewData["SelectedModel"] = "Clients";
        return PartialView("_ManageClientsDetailPartial", client);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveClient(Client model, string selectedModel)
    {
        if (model == null)
        {
            return BadRequest("Invalid Client data.");
        }

        // Retrieve the existing Client from the database
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == model.ClientId);
        if (client == null)
        {
            client = new Client
            {
                ClientId = model.ClientId
            };
        }

        // Update existing client

        var properties = typeof(Client).GetProperties();
        foreach (var prop in properties)
        {
            // Skip ClientId (or any other keys you don't want to update)
            if (prop.Name == "ClientId") continue;

            var newValue = prop.GetValue(model);
            var oldValue = prop.GetValue(client);

            // Special handling for nullable DateTime properties to ensure UTC conversion
            if (prop.PropertyType == typeof(DateTime?) && newValue != null)
            {
                var dateValue = (DateTime?)newValue;
                if (dateValue.HasValue)
                {
                    var utcValue = DateTime.SpecifyKind(dateValue.Value, DateTimeKind.Local).ToUniversalTime();
                    if (!oldValue?.Equals(utcValue) ?? true)
                    {
                        prop.SetValue(client, utcValue);
                    }
                }
            }
            else if ((oldValue == null && newValue != null) || (oldValue != null && !oldValue.Equals(newValue)))
            {
                prop.SetValue(client, newValue);
            }
        }

        if (client.ClientId == Guid.Empty)
        {
            client.ClientId = Guid.NewGuid();
            _db.Clients.Add(client);
        }
        else
        {
            _db.Clients.Update(client);
        }
        
        await _db.SaveChangesAsync();
        return RedirectToAction("ManageIndex", new { selectedModel });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetEmailTemplateDetails(Guid id)
    {
        var emailTemplate = _db.EmailTemplates
            .FirstOrDefault(c => c.EmailTemplateId == id && c.ClientId == localClientId);

        if (emailTemplate == null)
        {
            return NotFound("Could not find template with ID: " + id + " for Client ID: " + localClientId);
        }

        ViewData["SelectedModel"] = "EmailTemplates";
        return PartialView("_ManageEmailTemplatesDetailPartial", emailTemplate);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEmailTemplate(EmailTemplate model, string selectedModel)
    {
        if (model == null || model.ClientId == Guid.Empty)
        {
            return BadRequest("Invalid Email Template data.");
        }

        // Retrieve the existing Client from the database
        var emailTemplate = await _db.EmailTemplates.FirstOrDefaultAsync(c => c.EmailTemplateId == model.EmailTemplateId);
        if (emailTemplate == null)
        {
            emailTemplate = new EmailTemplate
            {
                ClientId = model.ClientId
            };
        }

        // Update existing email template

        var properties = typeof(EmailTemplate).GetProperties();
        foreach (var prop in properties)
        {
            // Skip ClientId (or any other keys you don't want to update)
            if (prop.Name == "ClientId" || prop.Name == "EmailTemplateId") continue;

            var newValue = prop.GetValue(model);
            var oldValue = prop.GetValue(emailTemplate);

            // Special handling for nullable DateTime properties to ensure UTC conversion
            if (prop.PropertyType == typeof(DateTime?) && newValue != null)
            {
                var dateValue = (DateTime?)newValue;
                if (dateValue.HasValue)
                {
                    var utcValue = DateTime.SpecifyKind(dateValue.Value, DateTimeKind.Local).ToUniversalTime();
                    if (!oldValue?.Equals(utcValue) ?? true)
                    {
                        prop.SetValue(emailTemplate, utcValue);
                    }
                }
            }
            else if ((oldValue == null && newValue != null) || (oldValue != null && !oldValue.Equals(newValue)))
            {
                prop.SetValue(emailTemplate, newValue);
            }
        }

        if (emailTemplate.EmailTemplateId == Guid.Empty)
        {
            emailTemplate.EmailTemplateId = Guid.NewGuid();
            _db.EmailTemplates.Add(emailTemplate);
        }
        else
        {
            _db.EmailTemplates.Update(emailTemplate);
        }
        
        await _db.SaveChangesAsync();
        return RedirectToAction("ManageIndex", new { selectedModel });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult ViewInvitations()
    {
        var raterInvitations = new List<vwInvitations>(_db.ViewRaterInvitations
            .Where(p => p.ClientId == localClientId));

        var rateeInvitations = new List<vwInvitations>(_db.ViewRateeInvitations
            .Where(p => p.ClientId == localClientId));

        var invitations = raterInvitations
            .Union(rateeInvitations)
            .OrderBy(p => p.RaterLastName)
            .ThenBy(p => p.RaterFirstName)
            .ToList();

        return View(invitations);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult InviteParticipant(Guid evaluationRateeId, Guid participantId)
    {
        var participantIds = new List<Guid> { participantId };
        return RedirectToAction("ProcessInvitations", new { participantIds, evaluationRateeId });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessInvitations(List<(Guid EvaluationRateeId, Guid ParticipantId)>? participantTuples)
    {

        var raterInvitations = new List<vwInvitations>(_db.ViewRaterInvitations
            .Where(p => p.ClientId == localClientId));

        var rateeInvitations = new List<vwInvitations>(_db.ViewRateeInvitations
            .Where(p => p.ClientId == localClientId));

        var invitations = raterInvitations
            .Union(rateeInvitations)
            .OrderBy(p => p.RaterLastName)
            .ThenBy(p => p.RaterFirstName)
            .ToList();

        if (participantTuples != null && participantTuples.Any())
        {
            // If participantTuples are provided, use them to filter invitations
            invitations = invitations.Where(i => participantTuples.Any(t => t.ParticipantId == i.RaterParticipantId && t.EvaluationRateeId == i.EvaluationRateeId)).ToList();
        }

        var messages = new List<EmailTemplate>(_db.EmailTemplates
            .Where(t => t.ClientId == localClientId));

        await _emailQueueHelper.ProcessParticipantsForEmailQueueAsync(invitations, messages, false, localEmail);
        return RedirectToAction("ViewInvitations");
    }

    private bool EvaluationExists(Guid id)
    {
        return _db.Evaluations.Any(e => e.EvaluationId == id);
    }
    
    public List<vwInvitations> MapRemindersToInvitations(List<vwReminders> reminders)
    {
        return reminders.Select(reminder => new vwInvitations
        {
            RateeFirstName = reminder.RateeNameFirst,
            RateeLastName = reminder.RateeNameLast,
            RaterRole = reminder.ParticipantRoleName,
            RaterFirstName = reminder.ParticipantNameFirst,
            RaterLastName = reminder.ParticipantNameLast,
            RaterEmail = reminder.ParticipantEmail,
            RaterParticipantId = reminder.ParticipantId,
            EvaluationRateeId = reminder.EvaluationRateeId,
            EvaluationTestEmails = reminder.EvaluationTestEmails,
            DueDate = reminder.DueDate,
            ClientName = reminder.ClientName,
            ClientId = reminder.ClientId,
            ClientHelpContact = reminder.ClientHelpContact,
            MessageToSend = reminder.MessageToSend
        }).ToList();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult EmailQueueDashboard()
    {
        var emailQueue = _db.EmailQueue.OrderByDescending(e => e.EmailQueuedDateTime).ToList();
        return View(emailQueue);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult PreviewEmail(int id)
    {
    var email = _db.EmailQueue.FirstOrDefault(e => e.EmailQueueId == id);
        if (email == null)
        {
            return NotFound();
        }

        return PartialView("_EmailPreview", email);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult RetryEmail(int id)
    {
    var email = _db.EmailQueue.FirstOrDefault(e => e.EmailQueueId == id);
        if (email == null || email.EmailSent)
        {
            return BadRequest("Email not found or already sent.");
        }

        email.EmailSent = false; // Reset the sent status
        _db.SaveChanges();

        return RedirectToAction("EmailQueueDashboard");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult CancelEmail(int id)
    {
    var email = _db.EmailQueue.FirstOrDefault(e => e.EmailQueueId == id);
        if (email == null || email.EmailSent)
        {
            return BadRequest("Email not found or already sent.");
        }

        _db.EmailQueue.Remove(email);
        _db.SaveChanges();

        return RedirectToAction("EmailQueueDashboard");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult CreateItem(string selectedModel)
    {
        switch (selectedModel)
        {
            case "GetEvaluationsForManage":
                // Return the edit partial view for Evaluation (empty model for create)
                ViewData["SelectedModel"] = "Evaluations";
                return PartialView("_ManageEvaluationsDetailPartial", new Evaluation
                {
                    EvaluationId = Guid.Empty, //indicates the new item interface
                    ClientId = localClientId
                });
            case "GetClientsForManage":
                // Return the edit partial view for Client (empty model for create)
                ViewData["SelectedModel"] = "Clients";
                return PartialView("_ManageClientsDetailPartial", new Client());
            case "GetEmailTemplatesForManage":
                // Return the edit partial view for Email Template (empty model for create)
                ViewData["SelectedModel"] = "EmailTemplates";
                return PartialView("_ManageEmailTemplatesDetailPartial", new EmailTemplate
                {
                    ClientId = localClientId
                });
            default:
                // Optionally, return a default partial or error
                return Content("Unknown model type: " + selectedModel);
        }
    }
}