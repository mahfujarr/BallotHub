using Microsoft.AspNetCore.Identity;

namespace BallotHub.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string NID { get; set; } = string.Empty;
    }
}