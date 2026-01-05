using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using reviews.Models;
using reviews.Services;

namespace reviews.Controllers.HomeController;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReviewSessionService _sessionService;
    private readonly ISessionInitializationService _sessionInitService;
    private readonly IUserRoleService _userRoleService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        UserManager<ApplicationUser> userManager,
        IReviewSessionService sessionService,
        ISessionInitializationService sessionInitService,
        IUserRoleService userRoleService,
        SignInManager<ApplicationUser> signInManager,
        ILogger<HomeController> logger)
    {
        _userManager = userManager;
        _sessionService = sessionService;
        _sessionInitService = sessionInitService;
        _userRoleService = userRoleService;
        _signInManager = signInManager;
        _logger = logger;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found in Index");
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var sessionData = _sessionService.GetSessionData();

            // Initialize session if not already set
            if (sessionData.ClientId == Guid.Empty)
            {
                var newSessionData = await InitializeUserSessionAsync(user);
                if (newSessionData == null)
                {
                    _logger.LogError("Failed to initialize session for user {Email}", user.Email);
                    return View(CreateErrorViewModel(user.Email ?? ""));
                }

                _sessionService.SetSessionData(newSessionData);
                sessionData = newSessionData;
            }

            // Retrieve fresh session data
            sessionData = _sessionService.GetSessionData();

            // Determine redirect for participants with active work
            var redirectResult = await DetermineParticipantRedirectAsync(sessionData);
            if (redirectResult != null)
            {
                return redirectResult;
            }

            // Build view model
            var model = await BuildHomeViewModelAsync(sessionData);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Home Index");
            return View(CreateErrorViewModel(""));
        }
    }

    private async Task<SessionData?> InitializeUserSessionAsync(ApplicationUser user)
    {
        try
        {
            var email = user.Email ?? "";
            var role = await _userRoleService.GetUserRoleAsync(user);

            // Check if user should be assigned Participant role
            if (string.IsNullOrEmpty(role))
            {
                var participant = await _sessionInitService.GetParticipantByEmailAsync(email);
                if (participant != null)
                {
                    var roleAssigned = await _userRoleService.AssignParticipantRoleAsync(user, email);
                    if (roleAssigned)
                    {
                        // Reload user after role assignment
                        return null; // Signal to redirect and reinitialize
                    }
                }
            }

            // Initialize session based on role
            if (role == "Participant")
            {
                var participant = await _sessionInitService.GetParticipantByEmailAsync(email);
                if (participant != null)
                {
                    // Mark participant as registered
                    await _userRoleService.MarkParticipantAsRegisteredAsync(participant.ParticipantId);

                    // Update user's ParticipantId if not set
                    if (user.ParticipantId == Guid.Empty)
                    {
                        await _userRoleService.UpdateUserParticipantIdAsync(user, participant.ParticipantId);
                        return null; // Signal to redirect and reinitialize
                    }

                    return await _sessionInitService.InitializeParticipantSessionAsync(email);
                }
            }
            else if (role == "Client" || role == "Admin")
            {
                return await _sessionInitService.InitializeClientSessionAsync(email, role);
            }

            _logger.LogWarning("Unable to initialize session for user {Email} with role {Role}", email, role);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing user session");
            return null;
        }
    }

    private Task<IActionResult?> DetermineParticipantRedirectAsync(SessionData sessionData)
    {
        if (sessionData.Role != "Participant")
        {
            return Task.FromResult<IActionResult?>(null);
        }

        // Redirect to ViewRaters if they have an active evaluation
        if (sessionData.DefaultEvaluationId != Guid.Empty)
        {
            return Task.FromResult<IActionResult?>(RedirectToAction("ViewRaters", "Participant"));
        }

        // Redirect to evaluations if they have open ratings
        if (sessionData.OpenRatingCount > 0)
        {
            return Task.FromResult<IActionResult?>(RedirectToAction("ParticipantIndex", "Participant"));
        }

        return Task.FromResult<IActionResult?>(null);
    }

    private async Task<HomeViewModel> BuildHomeViewModelAsync(SessionData sessionData)
    {
        var (evaluationsText, ratersText) = await _sessionInitService.GetParticipantMessagesAsync(
            sessionData.ParticipantId,
            sessionData.DefaultEvaluationId,
            sessionData.OpenRatingCount);

        return new HomeViewModel
        {
            Role = sessionData.Role,
            Email = sessionData.Email,
            LocalClientId = sessionData.ClientId,
            LocalClientName = sessionData.ClientName,
            LocalClientLogoLocation = sessionData.ClientLogoLocation,
            LocalDefaultEvaluationId = sessionData.DefaultEvaluationId,
            LocalOpenRatings = sessionData.OpenRatingCount,
            EvaluationsText = evaluationsText,
            RatersText = ratersText,
            ParticipantId = sessionData.ParticipantId,
            ParticipantName = "", // Can be added if needed
            Clients = new List<Client>() // Can be populated for Admin/Client if needed
        };
    }

    private HomeViewModel CreateErrorViewModel(string email)
    {
        return new HomeViewModel
        {
            Role = "",
            Email = email,
            LocalClientId = Guid.Empty,
            LocalClientName = "",
            LocalClientLogoLocation = "/images/Transparent-Logo-Horiz-1.png",
            LocalDefaultEvaluationId = Guid.Empty,
            LocalOpenRatings = 0,
            EvaluationsText = "An error occurred loading your session. Please try logging out and back in.",
            RatersText = "",
            ParticipantId = Guid.Empty,
            ParticipantName = "",
            Clients = new List<Client>()
        };
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _sessionService.ClearSessionData();
            _logger.LogInformation("User logged out successfully");
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return RedirectToAction("Index", "Home");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
