using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HORIZON1.Data;
using HORIZON1.Models;

namespace HORIZON1.Controllers
{
    // [Authorize] — цей атрибут означає, що доступ до категорій мають лише залогінені користувачі
    [Authorize]
    [Route("api/[controller]")] // Шлях до контролера: /api/categories
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        // Контекст бази даних для взаємодії з таблицями
        private readonly AppDbContext _context;

        // Конструктор: отримуємо доступ до бази через Dependency Injection
        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // МЕТОД ОТРИМАННЯ СПИСКУ КАТЕГОРІЙ (GET)
        [HttpGet]
        public ActionResult<IEnumerable<Category>> GetCategories()
        {
            // Звертаємось до таблиці Categories, сортуємо за назвою (OrderBy) 
            // і перетворюємо в список (ToList), щоб відправити на фронтенд
            return Ok(_context.Categories.OrderBy(c => c.Name).ToList());
        }

        // МЕТОД СТВОРЕННЯ НОВОЇ КАТЕГОРІЇ (POST)
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            // 1. Валідація: перевіряємо, чи назва категорії не порожня
            if (string.IsNullOrWhiteSpace(category.Name))
                return BadRequest("Назва категорії обов'язкова.");

            // 2. Додаємо нову категорію в контекст (у пам'ять)
            _context.Categories.Add(category);

            // 3. Зберігаємо зміни фізично в базу даних (асинхронно)
            await _context.SaveChangesAsync();

            // 4. Повертаємо створену категорію назад клієнту з статусом 200 (OK)
            return Ok(category);
        }
    }
}
