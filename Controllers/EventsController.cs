using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using HORIZON1.Models;
using HORIZON1.Repository;
using HORIZON1.Factory;
using HORIZON1.Data;

namespace HORIZON1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _repository;
        private readonly ReminderFactory _reminderFactory;
        private readonly AppDbContext _context;
        

        public EventsController(IEventRepository repository, ReminderFactory reminderFactory, AppDbContext context)
        {
            _repository = repository;
            _reminderFactory = reminderFactory;
            _context = context;
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
            var expandedEvents = ExpandRecurringEvents(events, year, month);
            return Ok(expandedEvents);
        }

        [HttpGet("daterange")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEventsByDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var events = await _repository.GetEventsByDateRangeAsync(start, end, CurrentUserId);
            var expandedEvents = ExpandRecurringEvents(events, start, end);
            return Ok(expandedEvents);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEventById(int id)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var eventItem = await _repository.GetByIdAsync(id, CurrentUserId);
            if (eventItem == null)
                return NotFound("Подію не знайдено або у вас немає прав на її перегляд.");

            return Ok(eventItem);
        }

        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent, [FromQuery] string reminderType = "email", [FromQuery] int reminderMinutes = 15)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized("Користувач не авторизований.");

            newEvent.UserId = CurrentUserId;

            if (newEvent.EndTime <= newEvent.StartTime)
                return BadRequest("Час закінчення має бути пізніше за час початку.");

            var createdEvent = await _repository.CreateAsync(newEvent);

            // Create reminder if requested
            if (reminderMinutes > 0)
            {
                var reminder = new Reminder
                {
                    Message = $"Нагадування: {createdEvent.Title}",
                    ReminderTime = createdEvent.StartTime.AddMinutes(-reminderMinutes),
                    Type = Enum.Parse<ReminderType>(reminderType, true),
                    EventId = createdEvent.Id
                };

                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();
            }

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

        private IEnumerable<Event> ExpandRecurringEvents(IEnumerable<Event> events, int year, int month)
        {
            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            return ExpandRecurringEvents(events, startOfMonth, endOfMonth);
        }

        private IEnumerable<Event> ExpandRecurringEvents(IEnumerable<Event> events, DateTime start, DateTime end)
        {
            var expandedEvents = new List<Event>();

            foreach (var ev in events)
            {
                if (!ev.IsRecurring || ev.RecurrenceType == RecurrenceType.None)
                {
                    // Check if event spans multiple days
                    if (ev.StartTime.Date < ev.EndTime.Date)
                    {
                        // Multi-day event - create instances for each day
                        var currentDate = ev.StartTime.Date;
                        while (currentDate <= ev.EndTime.Date && currentDate <= end)
                        {
                            if (currentDate >= start)
                            {
                                var dayEvent = new Event
                                {
                                    Id = ev.Id,
                                    Title = ev.Title,
                                    Description = ev.Description,
                                    StartTime = currentDate == ev.StartTime.Date ? ev.StartTime : currentDate,
                                    EndTime = currentDate == ev.EndTime.Date ? ev.EndTime : currentDate.AddDays(1).AddTicks(-1),
                                    IsRecurring = false,
                                    CategoryId = ev.CategoryId,
                                    Category = ev.Category,
                                    UserId = ev.UserId
                                };
                                expandedEvents.Add(dayEvent);
                            }
                            currentDate = currentDate.AddDays(1);
                        }
                    }
                    else
                    {
                        expandedEvents.Add(ev);
                    }
                }
                else
                {
                    // Handle recurring events
                    var occurrences = GenerateRecurringOccurrences(ev, start, end);
                    expandedEvents.AddRange(occurrences);
                }
            }

            return expandedEvents.OrderBy(e => e.StartTime);
        }

        private IEnumerable<Event> GenerateRecurringOccurrences(Event baseEvent, DateTime start, DateTime end)
        {
            var occurrences = new List<Event>();
            var currentDate = baseEvent.StartTime;

            while (currentDate <= end && (!baseEvent.RecurrenceEndDate.HasValue || currentDate <= baseEvent.RecurrenceEndDate.Value))
            {
                if (currentDate >= start)
                {
                    var occurrence = new Event
                    {
                        Id = baseEvent.Id,
                        Title = baseEvent.Title,
                        Description = baseEvent.Description,
                        StartTime = currentDate,
                        EndTime = currentDate.Add(baseEvent.EndTime - baseEvent.StartTime),
                        IsRecurring = false,
                        CategoryId = baseEvent.CategoryId,
                        Category = baseEvent.Category,
                        UserId = baseEvent.UserId
                    };
                    occurrences.Add(occurrence);
                }

                // Calculate next occurrence
                switch (baseEvent.RecurrenceType)
                {
                    case RecurrenceType.Daily:
                        currentDate = currentDate.AddDays(1);
                        break;
                    case RecurrenceType.Weekly:
                        currentDate = currentDate.AddDays(7);
                        break;
                    case RecurrenceType.Monthly:
                        currentDate = currentDate.AddMonths(1);
                        break;
                    case RecurrenceType.Yearly:
                        currentDate = currentDate.AddYears(1);
                        break;
                    case RecurrenceType.Custom:
                        if (baseEvent.RecurrenceInterval.HasValue)
                        {
                            currentDate = currentDate.AddDays(baseEvent.RecurrenceInterval.Value);
                        }
                        else
                        {
                            break; // Stop if no interval
                        }
                        break;
                }
            }

            return occurrences;
        }
    }
}