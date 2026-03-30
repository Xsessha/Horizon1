using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HORIZON1.Models;
using HORIZON1.Data;
using Microsoft.EntityFrameworkCore;

namespace HORIZON1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var categories = await _context.Categories
                .Where(c => c.UserId == null || c.UserId == CurrentUserId)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(category.Name))
                return BadRequest("Назва категорії обов'язкова");

            if (string.IsNullOrWhiteSpace(category.ColorHex) || !category.ColorHex.StartsWith("#") || category.ColorHex.Length != 7)
                return BadRequest("Колір має бути у форматі #RRGGBB");

            category.UserId = CurrentUserId;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);

            if (category == null)
                return NotFound("Категорію не знайдено або у вас немає прав на її видалення");

            // Check if category is used by events
            var eventsCount = await _context.Events.CountAsync(e => e.CategoryId == id);
            if (eventsCount > 0)
                return BadRequest("Неможливо видалити категорію, яка використовується подіями");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok("Категорію успішно видалено");
        }
    }
}