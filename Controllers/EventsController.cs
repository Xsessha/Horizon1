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

            // БЕРЕМО ВСІ ПОДІЇ: щоб точно захопити повторювані події, які почалися в минулих місяцях/роках
            var events = await _repository.GetAllAsync(CurrentUserId); 
            
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var results = new List<Event>();

            foreach (var ev in events)
            {
                // 1. Якщо подія НЕ повторюється - просто перевіряємо чи вона в цьому місяці
                if (ev.RecurrencePattern == RecurrencePattern.None)
                {
                    if (ev.StartTime <= monthEnd && ev.EndTime >= monthStart)
                    {
                        results.Add(ev);
                    }
                    continue;
                }

                // 2. Якщо подія ПОВТОРЮЄТЬСЯ
                var currentStart = ev.StartTime;
                var currentEnd = ev.EndTime;

                // Якщо подія має кінцеву дату повторення, і ця дата БУЛА ДО початку поточного місяця - ігноруємо
                if (ev.RecurrenceEndDate.HasValue && ev.RecurrenceEndDate.Value < monthStart)
                {
                    continue;
                }

                // "Перемотуємо" дату вперед, поки вона не дійде до поточного місяця (щоб не генерувати роки даремно)
                while (currentStart < monthStart)
                {
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

                // ГЕНЕРУЄМО події для поточного місяця
                while (currentStart <= monthEnd)
                {
                    // НАЙГОЛОВНІША ПЕРЕВІРКА: чи не вийшли ми за межі кінцевої дати повторення?
                    if (ev.RecurrenceEndDate.HasValue && currentStart.Date > ev.RecurrenceEndDate.Value.Date)
                    {
                        break; // Зупиняємо генерацію для цієї події!
                    }

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
                            RecurrenceEndDate = ev.RecurrenceEndDate // Передаємо нашу нову дату
                        };

                        results.Add(occurrence);
                    }

                    // Крок до наступної дати
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
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent)
        {
            // Перевірка авторизації
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();
            
            // Прив'язуємо подію до поточного користувача
            newEvent.UserId = CurrentUserId;

            // 1. Зберігаємо подію в базу через репозиторій
            var createdEvent = await _repository.CreateAsync(newEvent);

            // 2. Логіка нагадування (за 15 хвилин до початку)
            // Вираховуємо час, коли має спрацювати фонова служба
            var reminderTime = createdEvent.StartTime.AddMinutes(-15);
            
            // Якщо до події залишилось менше 15 хв, ставимо нагадування на "зараз + 1 хвилина"
            // Це щоб користувач отримав сповіщення майже миттєво для термінових справ
            if (reminderTime < DateTime.Now) 
            {
                reminderTime = DateTime.Now.AddMinutes(1);
            }

            // Створюємо об'єкт нагадування
            var reminder = new Reminder
            {
                EventId = createdEvent.Id,
                ReminderTime = reminderTime,
                Message = $"Нагадування від HORIZON: Подія '{createdEvent.Title}' розпочнеться о {createdEvent.StartTime:HH:mm}!"
            };

            // 3. ЗБЕРІГАЄМО НАГАДУВАННЯ В БАЗУ (використовуємо твій метод у репозиторії)
            await _repository.AddReminderAsync(reminder);

            // Повертаємо створену подію на фронтенд
            return Ok(createdEvent);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event updatedEvent)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            // Перевіряємо, чи існує подія і чи належить вона поточному користувачу
            var existingEvent = await _repository.GetByIdAsync(id, CurrentUserId);
            if (existingEvent == null)
                return NotFound("Подію не знайдено або у вас немає прав на її редагування.");

            // Оновлюємо поля
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartTime = updatedEvent.StartTime;
            existingEvent.EndTime = updatedEvent.EndTime;
            existingEvent.RecurrencePattern = updatedEvent.RecurrencePattern;
            existingEvent.IsRecurring = updatedEvent.RecurrencePattern != RecurrencePattern.None;
            
            // Категорії
            existingEvent.IsTemporaryCategory = updatedEvent.IsTemporaryCategory;
            existingEvent.TemporaryCategoryName = updatedEvent.TemporaryCategoryName;
            existingEvent.TemporaryCategoryColor = updatedEvent.TemporaryCategoryColor;
            existingEvent.CategoryId = updatedEvent.IsTemporaryCategory ? null : updatedEvent.CategoryId;

            await _repository.UpdateAsync(existingEvent);

            return Ok(existingEvent);
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