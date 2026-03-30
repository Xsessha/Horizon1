using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using HORIZON1.Models;
using HORIZON1.Repository;
using HORIZON1.Factory;

namespace HORIZON1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _repository;
        private readonly ReminderFactory _reminderFactory;
        

        public EventsController(IEventRepository repository, ReminderFactory reminderFactory)
        {
            _repository = repository;
            _reminderFactory = reminderFactory;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetAllEvents()
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var events = await _repository.GetAllAsync(CurrentUserId);
            return Ok(events);
        }

        [HttpGet("month/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEventsByMonth(int year, int month)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var events = await _repository.GetEventsByMonthAsync(year, month, CurrentUserId);
            return Ok(events);
        }

        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent, [FromQuery] string reminderType = "email")
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized("Користувач не авторизований.");

            newEvent.UserId = CurrentUserId;

            if (newEvent.EndTime <= newEvent.StartTime)
                return BadRequest("Час закінчення має бути пізніше за час початку.");

            var createdEvent = await _repository.CreateAsync(newEvent);

            var strategy = _reminderFactory.CreateStrategy(reminderType);
            strategy.SendReminder(createdEvent, new Reminder { Message = "Нагадування активовано!" });

            return Ok(createdEvent);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var result = await _repository.DeleteAsync(id, CurrentUserId);
            if (!result)
                return NotFound("Подію не знайдено або у вас немає прав на її видалення.");

            return Ok("Подію успішно видалено.");
        }
    }
}