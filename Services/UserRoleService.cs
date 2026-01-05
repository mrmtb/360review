using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

public class UserRoleService : IUserRoleService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<UserRoleService> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<string> GetUserRoleAsync(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return "Admin";
        if (await _userManager.IsInRoleAsync(user, "Client"))
            return "Client";
        if (await _userManager.IsInRoleAsync(user, "Participant"))
            return "Participant";
        
        return string.Empty;
    }

    public async Task<bool> AssignParticipantRoleAsync(ApplicationUser user, string email)
    {
        try
        {
            var participant = await _db.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParticipantEmail == email);

            if (participant == null)
            {
                return false;
            }

            var role = await _roleManager.FindByNameAsync("Participant");
            if (role?.Name == null)
            {
                _logger.LogError("Participant role not found in system");
                return false;
            }

            if (!await _userManager.IsInRoleAsync(user, role.Name))
            {
                var result = await _userManager.AddToRoleAsync(user, role.Name);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Assigned Participant role to user {Email}", email);
                    await _signInManager.RefreshSignInAsync(user);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to assign Participant role to user {Email}: {Errors}", 
                        email, string.Join(", ", result.Errors));
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning participant role to user {Email}", email);
            return false;
        }
    }

    public async Task<bool> UpdateUserParticipantIdAsync(ApplicationUser user, Guid participantId)
    {
        try
        {
            if (user.ParticipantId == participantId)
            {
                return true; // Already set
            }

            user.ParticipantId = participantId;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Updated ParticipantId for user {Email} to {ParticipantId}", 
                    user.Email, participantId);
                await _signInManager.RefreshSignInAsync(user);
                return true;
            }
            else
            {
                _logger.LogError("Failed to update ParticipantId for user {Email}: {Errors}", 
                    user.Email, string.Join(", ", result.Errors));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ParticipantId for user {Email}", user.Email);
            return false;
        }
    }

    public async Task<bool> MarkParticipantAsRegisteredAsync(Guid participantId)
    {
        try
        {
            var participant = await _db.Participants
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

            if (participant == null)
            {
                _logger.LogWarning("Participant not found: {ParticipantId}", participantId);
                return false;
            }

            if (participant.ParticipantHasRegistered)
            {
                return true; // Already registered
            }

            participant.ParticipantHasRegistered = true;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Marked participant {ParticipantId} as registered", participantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking participant {ParticipantId} as registered", participantId);
            return false;
        }
    }
}
