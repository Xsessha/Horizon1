using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    // DTO (Data Transfer Object) — об'єкт для передачі даних.
    // Він не зберігається в базі, а потрібен лише для отримання логіна/пароля з форми входу.
    public class LoginDto
    {
        // [Required] — Атрибут валідації. 
        // Якщо фронтенд пришле пустий Email, сервер відразу поверне помилку 400 (Bad Request).
        [Required]
        public string Email { get; set; } = string.Empty;

        // [Required] — Пароль також є обов'язковим.
        // Зверни увагу: тут ми не ставимо обмеження по довжині, 
        // щоб не підказувати зловмисникам правила наших паролів.
        [Required]
         public string Password { get; set; } = string.Empty;
     }
}