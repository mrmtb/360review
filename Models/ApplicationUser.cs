using Microsoft.AspNetCore.Identity;

namespace reviews.Models;

// Add profile data for application users by adding properties to the IdentityUser class
public class ApplicationUser : IdentityUser<string>
{
    [PersonalData]
    public Guid ParticipantId { get; set; }
    public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; }
    public ApplicationUser()
    {
        UserRoles = new HashSet<IdentityUserRole<string>>();
    }
}

