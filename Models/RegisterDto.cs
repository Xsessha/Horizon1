using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Невірний формат Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [MinLength(6, ErrorMessage = "Пароль має бути не менше 6 символів")]
        public string? Password { get; set; }

        public string? FullName { get; set; }
    }
}