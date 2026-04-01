using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HORIZON1.Models;

namespace HORIZON1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new HORIZON1.Models.User // Явно вказуємо твій клас
            { 
                UserName = model.Email, 
                Email = model.Email, 
                FullName = model.FullName 
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { 
                    Message = "Користувача успішно створено!", 
                    UserId = user.Id 
                });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized("Користувача з таким Email не знайдено.");

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, 
                model.Password, 
                isPersistent: false, 
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                return Ok(new { 
                    Message = "Вхід успішний!", 
                    UserId = user.Id,
                    UserName = user.FullName
                });
            }

            return Unauthorized("Невірний пароль.");
        }
    }
}