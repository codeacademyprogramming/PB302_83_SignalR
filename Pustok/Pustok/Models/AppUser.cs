using Microsoft.AspNetCore.Identity;

namespace Pustok.Models
{
    public class AppUser:IdentityUser
    {
        public string FullName { get; set; }
        public string? ConnectionId { get; set; }
        public DateTime? LastConnectedAt { get; set; }
    }
}
