using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HORIZON1.Data;
using HORIZON1.Models;

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

        [HttpGet]
        public ActionResult<IEnumerable<Category>> GetCategories()
        {
            return Ok(_context.Categories.OrderBy(c => c.Name).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return BadRequest("Назва категорії обов'язкова.");

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }
    }
}