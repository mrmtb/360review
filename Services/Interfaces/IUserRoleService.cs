using reviews.Models;

namespace reviews.Services;

public interface IUserRoleService
{
    Task<string> GetUserRoleAsync(ApplicationUser user);
    Task<bool> AssignParticipantRoleAsync(ApplicationUser user, string email);
    Task<bool> UpdateUserParticipantIdAsync(ApplicationUser user, Guid participantId);
    Task<bool> MarkParticipantAsRegisteredAsync(Guid participantId);
}
