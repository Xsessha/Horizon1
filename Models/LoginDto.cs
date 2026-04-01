using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    public class LoginDto
    {
        [Required]
     public string Email { get; set; } = string.Empty;
    [Required]
      public string Password { get; set; } = string.Empty;
     }
}