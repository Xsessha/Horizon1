using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    // DTO (Data Transfer Object) для реєстрації.
    // Використовується для передачі даних від форми реєстрації на фронтенді до контролера.
    public class RegisterDto
    {
        // [Required] — поле не може бути порожнім.
        // [EmailAddress] — автоматична перевірка, чи введений текст схожий на реальну пошту (наявність @ та крапки).
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Невірний формат Email")]
        public string Email { get; set; } = string.Empty;

        // [Required] — пароль обов'язковий.
        // [MinLength] — важливе правило безпеки: пароль не може бути коротшим за 6 символів.
        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [MinLength(6, ErrorMessage = "Пароль має бути не менше 6 символів")]
        public string Password { get; set; } = string.Empty;

        // Повне ім'я користувача (наприклад, для відображення у профілі чи календарі).
        [Required(ErrorMessage = "Повне ім'я є обов'язковим")]
        public string FullName { get; set; } = string.Empty;
    }
}