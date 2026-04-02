using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HORIZON1.Models;

namespace HORIZON1.Controllers
{
    // Атрибути: вказують, що це API-контролер і доступ до нього через /api/auth
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Вбудовані сервіси ASP.NET Core Identity для роботи з базою користувачів
        private readonly UserManager<User> _userManager; // Керує користувачами (створення, пошук)
        private readonly SignInManager<User> _signInManager;// Керує процесом входу (перевірка пароля)

        // Конструктор: впроваджуємо залежності (Dependency Injection)
        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        // МЕТОД РЕЄСТРАЦІЇ
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            // 1. Перевіряємо, чи заповнені всі поля згідно з правилами в RegisterDto
            if (!ModelState.IsValid) return BadRequest(ModelState);
            // 2. Створюємо об'єкт нового користувача на основі отриманих даних
            var user = new HORIZON1.Models.User 
            { 
                UserName = model.Email, 
                Email = model.Email, 
                FullName = model.FullName 
            };
            // 3. Зберігаємо користувача в базі. Identity автоматично захешує пароль!
            var result = await _userManager.CreateAsync(user, model.Password);
            // 4. Якщо збереження успішне — повертаємо статус 200 (OK)
            if (result.Succeeded)
            {
                return Ok(new { 
                    Message = "Користувача успішно створено!", 
                    UserId = user.Id 
                });
            }
            // 5. Якщо виникли помилки (наприклад, такий Email вже є) — повертаємо 400 (Bad Request)
            return BadRequest(result.Errors);
        }

        // МЕТОД ВХОДУ (ЛОГІН)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            // 1. Валідація вхідних даних (чи не пусті поля)
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 2. Шукаємо користувача в базі по його Email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized("Користувача з таким Email не знайдено.");

            // 3. Перевіряємо пароль за допомогою SignInManager
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, 
                model.Password, 
                isPersistent: false, // Не зберігати вхід після закриття браузера
                lockoutOnFailure: false // Не блокувати акаунт після невдалих спроб
            );

            // 4. Якщо пароль вірний — повертаємо дані користувача для фронтенду
            if (result.Succeeded)
            {
                return Ok(new { 
                    Message = "Вхід успішний!", 
                    UserId = user.Id,
                    UserName = user.FullName
                });
            }
            
            // 5. Якщо пароль не збігається — повертаємо 401 (Unauthorized)
            return Unauthorized("Невірний пароль.");
        }
    }
}