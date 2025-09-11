using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using reviews.Models;
using reviews.Services;

namespace reviews.Controllers;


public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ReviewSessionService _sessionService;
    private readonly SignInManager<ApplicationUser> _signInManager;

    private Task<AuthenticationState> authenticationState { get; set; } = null!;

    private Guid localClientId = new Guid();
    private string localClientName = "";
    private Guid localUserId;
    private string localEmail = "";
    private string localRole = "";
    private string localClientLogoLocation = "/images/Transparent-Logo-Horiz-1.png";
    private Guid localDefaultEvaluationId = Guid.Empty;
    private int localOpenRatings = -1;
    private string EvaluationsText = "";
    private string RatersText = "";

    List<Client> ClientList = new List<Client>();

    private Guid participantId;
    private string participantName = null!;

    public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ReviewSessionService sessionService, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _sessionService = sessionService;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        // Check to see if the session data has been set; if not, set it
        var user = await _userManager.GetUserAsync(User);
        var sessionData = _sessionService.GetSessionData();

        if (sessionData.ClientId == Guid.Empty) // || sessionData.UserId == Guid.Empty)
        {
            localEmail = user?.Email ?? "";
            localRole = User.IsInRole("Admin") ? "Admin" :
                         User.IsInRole("Client") ? "Client" :
                         User.IsInRole("Participant") ? "Participant" : "";

            var participant = _db.Participants.Include(p => p.Client).FirstOrDefault(v => v.ParticipantEmail == localEmail);

            // If the logged in user does not have a role and they appear in the participant table, assign them the Participant role in the Identity system
            if (localRole == "" && participant != null)
            {
                //localRole = "Participant";
                var role = await _roleManager.FindByNameAsync("Participant");
                if (role != null && role.Name != null && user != null)
                {
                    if (role != null && !await _userManager.IsInRoleAsync(user, role.Name))
                    {
                        //Console.WriteLine($"Assigning role {role.Name} to user {user.UserName}");
                        await _userManager.AddToRoleAsync(user, role.Name);
                    }
                }

                user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    return RedirectToAction("Index", "Home");
                }
            }

            if (localRole == "Participant" && participant != null)
            {
                participantId = participant.ParticipantId;
                localUserId = participant.ParticipantId;
                participantName = participant.ParticipantNameLast + ", " + participant.ParticipantNameFirst;
                localClientId = participant.ClientId;
                localClientName = participant.Client.ClientName;
                localClientLogoLocation = participant.Client.ClientLogoLocation ?? "/images/Transparent-Logo-Horiz-1.png";

                if (!participant.ParticipantHasRegistered && user != null)
                {
                    //Console.WriteLine($"Setting ParticipantHasRegistered for {user.UserName}");
                    participant.ParticipantHasRegistered = true;
                    _db.SaveChanges();
                }
                else
                {
                    //Console.WriteLine($"Skipping set ParticipantHasRegistered for {user?.UserName}");
                }

                if (user != null && user.ParticipantId == Guid.Empty)
                {
                    //Console.WriteLine($"Setting ParticipantId for {user.UserName} to {participantId}");
                    user.ParticipantId = participantId;
                    var result = await _userManager.UpdateAsync(user);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error: {error.Description}");
                        }
                    }

                    user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _signInManager.RefreshSignInAsync(user);
                        return RedirectToAction("Index", "Home");
                    }

                }

                var defaultEvaluation = _db.ViewDefaultEvaluationByParticipant
                    .SingleOrDefault(v => v.ParticipantId == participantId);

                if (defaultEvaluation != null && defaultEvaluation.EvaluationActive)
                {
                    localDefaultEvaluationId = defaultEvaluation.EvaluationId;
                }
                else
                {
                    localDefaultEvaluationId = Guid.Empty;
                }

                var defaultReviews = _db.ViewEvaluationsToComplete
                    .Where(v => v.Rater == participantId);

                localOpenRatings = defaultReviews.Count(r => r.EvaluationParticipantStatusIsActive == 1 && r.EvaluationParticipantStatusIsComplete == 0);

            }
            else if (localRole == "Client")
            {
                ClientList = _db.Clients.ToList();
                localClientId = ClientList.FirstOrDefault()?.ClientId ?? Guid.Empty;
                localClientName = ClientList.FirstOrDefault()?.ClientName ?? "Default Client";
                localClientLogoLocation = ClientList.FirstOrDefault()?.ClientLogoLocation ?? localClientLogoLocation;
            }
            else if (localRole == "Admin" && sessionData?.ClientId == Guid.Empty)
            {
                ClientList = _db.Clients.ToList();
                localClientId = ClientList.FirstOrDefault()?.ClientId ?? Guid.Empty;
                localClientName = ClientList.FirstOrDefault()?.ClientName ?? "Default Client";
                localClientLogoLocation = ClientList.FirstOrDefault()?.ClientLogoLocation ?? localClientLogoLocation;
            }

            var NewSessionData = new SessionData
            {
                UserId = localUserId,
                ParticipantId = participantId,
                Email = localEmail,
                Role = localRole,
                ClientId = localClientId,
                ClientName = localClientName,
                ClientLogoLocation = localClientLogoLocation,
                DefaultEvaluationId = localDefaultEvaluationId,
                OpenRatingCount = localOpenRatings
            };
            _sessionService.SetSessionData(NewSessionData);
        }

        sessionData = _sessionService.GetSessionData();

        localUserId = sessionData.UserId;
        localEmail = sessionData.Email;
        localRole = sessionData.Role;
        localClientId = sessionData.ClientId;
        localClientName = sessionData.ClientName;
        localClientLogoLocation = sessionData.ClientLogoLocation;
        localDefaultEvaluationId = sessionData.DefaultEvaluationId;
        localOpenRatings = sessionData.OpenRatingCount;

    if (localDefaultEvaluationId != Guid.Empty && User.IsInRole("Participant"))
        {
            RatersText = "You have an active 360 review. Select <i>Your Raters</i> on the left to identify your raters.";
            // In this instance, we will send the participant to the Raters page
            return RedirectToAction("ViewRaters", "Participant");
        }

        if (localOpenRatings == 1 && User.IsInRole("Participant"))
        {
            EvaluationsText = "You have 1 evaluation to complete. Use the navigation on the left to access it.";
            return RedirectToAction("ViewEvaluations", "Participant");
        }
        else if (localOpenRatings > 1 && User.IsInRole("Participant"))
        {
            EvaluationsText = $"You have {localOpenRatings} evaluations to complete. Use the navigation on the left to access them.";
            return RedirectToAction("ViewEvaluations", "Participant");
        }
        else
            EvaluationsText = "You have no evaluations to complete at this time. Thanks for your attention to this important process.";

        var model = new HomeViewModel
        {
            Role = localRole,
            Email = localEmail,
            LocalClientId = localClientId,
            LocalClientName = localClientName,
            LocalClientLogoLocation = localClientLogoLocation,
            LocalDefaultEvaluationId = localDefaultEvaluationId,
            LocalOpenRatings = localOpenRatings,
            EvaluationsText = EvaluationsText,
            RatersText = RatersText,
            ParticipantId = participantId,
            ParticipantName = participantName,
            Clients = ClientList
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _sessionService.ClearSessionData();
        return RedirectToAction("Index", "Home");
    }
    
}