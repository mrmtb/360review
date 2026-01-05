using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using reviews.Constants;
using reviews.Models;
using reviews.Services;

namespace reviews.Controllers.ClientController;

[Authorize(Roles = RoleNames.AdminOrClient)]
public class ClientController : Controller
{
    private readonly IClientService _clientService;
    private readonly IReviewSessionService _sessionService;
    private readonly ILogger<ClientController> _logger;

    private Guid LocalClientId { get; set; } = Guid.Empty;
    private string LocalRole { get; set; } = "";

    public ClientController(
        IClientService clientService,
        IReviewSessionService sessionService,
        ILogger<ClientController> logger)
    {
        _clientService = clientService;
        _sessionService = sessionService;
        _logger = logger;

        var sessionData = _sessionService.GetSessionData();
        if (sessionData != null)
        {
            LocalClientId = sessionData.ClientId;
            LocalRole = sessionData.Role;
        }
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var clients = await _clientService.GetAllClientsAsync();
            return View(clients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading clients list");
            return View(new List<Client>());
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var client = await _clientService.GetClientDetailsAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading client details for {ClientId}", id);
            return NotFound();
        }
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult Create()
    {
        return View(_clientService.CreateNewClient());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        try
        {
            var success = await _clientService.SaveClientAsync(client);
            if (!success)
            {
                ModelState.AddModelError("", ErrorMessages.FailedToCreateClient);
                return View(client);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client");
            ModelState.AddModelError("", "An error occurred while creating the client.");
            return View(client);
        }
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var client = await _clientService.GetClientDetailsAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading client for edit {ClientId}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(Client client)
    {
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        try
        {
            var success = await _clientService.SaveClientAsync(client);
            if (!success)
            {
                ModelState.AddModelError("", ErrorMessages.FailedToUpdateClient);
                return View(client);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client {ClientId}", client.ClientId);
            ModelState.AddModelError("", "An error occurred while updating the client.");
            return View(client);
        }
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var client = await _clientService.GetClientDetailsAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading client for delete {ClientId}", id);
            return NotFound();
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            var success = await _clientService.DeleteClientAsync(id);
            if (!success)
            {
                _logger.LogWarning("Client not found for deletion: {ClientId}", id);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting client {ClientId}", id);
            return RedirectToAction(nameof(Index));
        }
    }
}
