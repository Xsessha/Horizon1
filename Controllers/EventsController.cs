using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using HORIZON1.Models;
using HORIZON1.Repository;
using HORIZON1.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HORIZON1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _repository;
        private readonly AppDbContext _context;

        public EventsController(IEventRepository repository, AppDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();
            
            newEvent.UserId = CurrentUserId;
            newEvent.IsRecurring = newEvent.RecurrencePattern != RecurrencePattern.None;

            // 1. Створюємо подію
            var createdEvent = await _repository.CreateAsync(newEvent);

            // 2. Логіка нагадування за 15 хв
            var reminderTime = createdEvent.StartTime.AddMinutes(-15);
            if (reminderTime < DateTime.Now) 
            {
                reminderTime = DateTime.Now.AddMinutes(1);
            }

            // 3. Додаємо нагадування в базу
            var reminder = new Reminder
            {
                EventId = createdEvent.Id,
                ReminderTime = reminderTime,
                Message = $"🔔 Нагадування: '{createdEvent.Title}' почнеться о {createdEvent.StartTime:HH:mm}!"
            };

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();

            return Ok(createdEvent);
        }

        [HttpGet("month/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEventsByMonth(int year, int month)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var events = await _repository.GetAllAsync(CurrentUserId); 
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var results = new List<Event>();

            foreach (var ev in events)
            {
                if (ev.RecurrencePattern == RecurrencePattern.None)
                {
                    if (ev.StartTime <= monthEnd && ev.EndTime >= monthStart) results.Add(ev);
                    continue;
                }

                var currentStart = ev.StartTime;
                var currentEnd = ev.EndTime;

                if (ev.RecurrenceEndDate.HasValue && ev.RecurrenceEndDate.Value < monthStart) continue;

                while (currentStart < monthStart)
                {
                    currentStart = MoveNext(currentStart, ev.RecurrencePattern);
                    currentEnd = MoveNext(currentEnd, ev.RecurrencePattern);
                }

                while (currentStart <= monthEnd)
                {
                    if (ev.RecurrenceEndDate.HasValue && currentStart.Date > ev.RecurrenceEndDate.Value.Date) break;

                    results.Add(new Event {
                        Id = ev.Id, Title = ev.Title, StartTime = currentStart, EndTime = currentEnd,
                        UserId = ev.UserId, RecurrencePattern = ev.RecurrencePattern
                    });

                    currentStart = MoveNext(currentStart, ev.RecurrencePattern);
                    currentEnd = MoveNext(currentEnd, ev.RecurrencePattern);
                }
            }
            return Ok(results.OrderBy(e => e.StartTime));
        }

        // --- НОВІ МЕТОДИ: ВИДАЛЕННЯ ТА РЕДАГУВАННЯ ---

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            // Знаходимо подію саме цього користувача
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

            if (ev == null) return NotFound("Подію не знайдено");

            // Видаляємо нагадування, щоб не було помилок Foreign Key в SQLite
            var reminders = _context.Reminders.Where(r => r.EventId == id);
            _context.Reminders.RemoveRange(reminders);

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, Event updatedEvent)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

            if (existingEvent == null) return NotFound("Подію не знайдено");

            // Оновлюємо основні поля
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartTime = updatedEvent.StartTime;
            existingEvent.EndTime = updatedEvent.EndTime;
            existingEvent.CategoryId = updatedEvent.CategoryId;
            existingEvent.RecurrencePattern = updatedEvent.RecurrencePattern;
            existingEvent.RecurrenceEndDate = updatedEvent.RecurrenceEndDate;
            existingEvent.IsRecurring = updatedEvent.RecurrencePattern != RecurrencePattern.None;

            // Оновлюємо нагадування, якщо воно існує
            var reminder = await _context.Reminders.FirstOrDefaultAsync(r => r.EventId == id);
            if (reminder != null)
            {
                reminder.ReminderTime = existingEvent.StartTime.AddMinutes(-15);
                reminder.Message = $"🔔 Оновлене нагадування: '{existingEvent.Title}' почнеться о {existingEvent.StartTime:HH:mm}!";
            }

            await _context.SaveChangesAsync();
            return Ok(existingEvent);
        }

        private DateTime MoveNext(DateTime date, RecurrencePattern pattern) => pattern switch
        {
            RecurrencePattern.Daily => date.AddDays(1),
            RecurrencePattern.Weekly => date.AddDays(7),
            RecurrencePattern.Monthly => date.AddMonths(1),
            RecurrencePattern.Yearly => date.AddYears(1),
            _ => date.AddDays(1)
        };
    }
}