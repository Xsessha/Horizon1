using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using HORIZON1.Models;
using HORIZON1.Repository;
using HORIZON1.Data;
using Microsoft.EntityFrameworkCore;

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

            // 2. Логіка нагадування за 15 хв (з використанням DateTime.Now)
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

                // Перевірка наявності RecurrenceEndDate після оновлення моделі
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