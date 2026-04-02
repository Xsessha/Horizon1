using Microsoft.AspNetCore.Identity;

namespace HORIZON1.Models
{
    // КЛАС КОРИСТУВАЧА: Наслідується від IdentityUser.
    // Це означає, що цей клас автоматично отримує поля: Id, Email, PasswordHash, 
    // PhoneNumber та інші, які необхідні для безпечної авторизації.
    public class User : IdentityUser
    {
        // Додаткове поле: Повне ім'я користувача (напр. "Іван Іванов").
        // Ми додали його самі, бо в стандартному IdentityUser є тільки UserName.
        public string FullName { get; set; } = string.Empty;

        // Додаткове поле: Дата реєстрації акаунта.
        // За замовчуванням ставимо поточний час у форматі UTC для точності.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}