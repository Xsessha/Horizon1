using Microsoft.AspNetCore.Identity;

namespace HORIZON1.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}