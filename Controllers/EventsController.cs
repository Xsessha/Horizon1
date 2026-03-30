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
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var results = new List<Event>();

            foreach (var ev in events)
            {
                if (ev.RecurrencePattern == RecurrencePattern.None)
                {
                    results.Add(ev);
                    continue;
                }

                var occurrenceStart = ev.StartTime;
                var occurrenceEnd = ev.EndTime;

                DateTime currentStart = occurrenceStart;
                DateTime currentEnd = occurrenceEnd;

                // починаємо від першого дня місяця, якщо подія почалась раніше
                if (currentStart < monthStart)
                {
                    switch (ev.RecurrencePattern)
                    {
                        case RecurrencePattern.Daily:
                            var daysOffset = (monthStart - currentStart).Days;
                            currentStart = currentStart.AddDays(daysOffset);
                            currentEnd = currentEnd.AddDays(daysOffset);
                            break;
                        case RecurrencePattern.Weekly:
                            while (currentStart < monthStart)
                            {
                                currentStart = currentStart.AddDays(7);
                                currentEnd = currentEnd.AddDays(7);
                            }
                            break;
                        case RecurrencePattern.Monthly:
                            while (currentStart < monthStart)
                            {
                                currentStart = currentStart.AddMonths(1);
                                currentEnd = currentEnd.AddMonths(1);
                            }
                            break;
                        case RecurrencePattern.Yearly:
                            while (currentStart < monthStart)
                            {
                                currentStart = currentStart.AddYears(1);
                                currentEnd = currentEnd.AddYears(1);
                            }
                            break;
                    }
                }

                while (currentStart <= monthEnd && currentStart < ev.EndTime.AddYears(100)) // убезпечення
                {
                    if (currentEnd >= monthStart && currentStart <= monthEnd)
                    {
                        var occurrence = new Event
                        {
                            Id = ev.Id,
                            Title = ev.Title,
                            Description = ev.Description,
                            StartTime = currentStart,
                            EndTime = currentEnd,
                            IsRecurring = ev.IsRecurring,
                            IsDeleted = ev.IsDeleted,
                            UserId = ev.UserId,
                            CategoryId = ev.CategoryId,
                            Category = ev.Category,
                            IsTemporaryCategory = ev.IsTemporaryCategory,
                            TemporaryCategoryName = ev.TemporaryCategoryName,
                            TemporaryCategoryColor = ev.TemporaryCategoryColor,
                            RecurrencePattern = ev.RecurrencePattern,
                            RecurrenceDays = ev.RecurrenceDays,
                        };

                        results.Add(occurrence);
                    }

                    currentStart = ev.RecurrencePattern switch
                    {
                        RecurrencePattern.Daily => currentStart.AddDays(1),
                        RecurrencePattern.Weekly => currentStart.AddDays(7),
                        RecurrencePattern.Monthly => currentStart.AddMonths(1),
                        RecurrencePattern.Yearly => currentStart.AddYears(1),
                        _ => currentStart.AddDays(1)
                    };
                    currentEnd = ev.RecurrencePattern switch
                    {
                        RecurrencePattern.Daily => currentEnd.AddDays(1),
                        RecurrencePattern.Weekly => currentEnd.AddDays(7),
                        RecurrencePattern.Monthly => currentEnd.AddMonths(1),
                        RecurrencePattern.Yearly => currentEnd.AddYears(1),
                        _ => currentEnd.AddDays(1)
                    };
                }
            }

            return Ok(results.OrderBy(e => e.StartTime));
        }

        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent, [FromQuery] string reminderType = "email")
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized("Користувач не авторизований.");

            newEvent.UserId = CurrentUserId;

            if (newEvent.EndTime <= newEvent.StartTime)
                return BadRequest("Час закінчення має бути пізніше за час початку.");

            if (newEvent.IsTemporaryCategory)
            {
                // Не зберігаємо нову категорію у БД
                newEvent.CategoryId = null;
                newEvent.Category = null;
            }

            newEvent.IsRecurring = newEvent.RecurrencePattern != RecurrencePattern.None;

            var createdEvent = await _repository.CreateAsync(newEvent);

            var reminderTime = createdEvent.StartTime.AddMinutes(-30);
            if (reminderTime < DateTime.UtcNow)
            {
                reminderTime = DateTime.UtcNow.AddMinutes(1);
            }

            var strategy = _reminderFactory.CreateStrategy(reminderType);
            strategy.SendReminder(createdEvent, new Reminder
            {
                Message = $"Нагадування: подія '{createdEvent.Title}' починається {createdEvent.StartTime:dd.MM.yyyy HH:mm}",
                ReminderTime = reminderTime,
                EventId = createdEvent.Id
            });

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