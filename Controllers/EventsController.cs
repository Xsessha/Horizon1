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

            var createdEvent = await _repository.CreateAsync(newEvent);

            var reminderTime = createdEvent.StartTime.AddMinutes(-15);
            if (reminderTime < DateTime.Now) reminderTime = DateTime.Now.AddMinutes(1);

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

            // ДОДАНО .Include(e => e.Category) ДЛЯ КОЛЬОРІВ
            var events = await _context.Events
                .Include(e => e.Category)
                .Where(e => e.UserId == CurrentUserId && !e.IsDeleted)
                .ToListAsync();

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

                while (currentStart <= monthEnd)
                {
                    if (ev.RecurrenceEndDate.HasValue && currentStart.Date > ev.RecurrenceEndDate.Value.Date) break;

                    if (currentStart <= monthEnd && currentEnd >= monthStart)
                    {
                        results.Add(new Event {
                            Id = ev.Id, Title = ev.Title, StartTime = currentStart, EndTime = currentEnd,
                            UserId = ev.UserId, CategoryId = ev.CategoryId, Category = ev.Category,
                            IsTemporaryCategory = ev.IsTemporaryCategory,
                            TemporaryCategoryName = ev.TemporaryCategoryName,
                            TemporaryCategoryColor = ev.TemporaryCategoryColor,
                            RecurrencePattern = ev.RecurrencePattern
                        });
                    }
                    currentStart = MoveNext(currentStart, ev.RecurrencePattern);
                    currentEnd = MoveNext(currentEnd, ev.RecurrencePattern);
                }
            }
            return Ok(results.OrderBy(e => e.StartTime));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (ev == null) return NotFound();

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
            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (existingEvent == null) return NotFound();

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartTime = updatedEvent.StartTime;
            existingEvent.EndTime = updatedEvent.EndTime;
            existingEvent.CategoryId = updatedEvent.CategoryId;
            existingEvent.RecurrencePattern = updatedEvent.RecurrencePattern;
            existingEvent.IsTemporaryCategory = updatedEvent.IsTemporaryCategory;
            existingEvent.TemporaryCategoryName = updatedEvent.TemporaryCategoryName;
            existingEvent.TemporaryCategoryColor = updatedEvent.TemporaryCategoryColor;

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