using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using HORIZON1.Models;
using HORIZON1.Repository;
using HORIZON1.Factory;

namespace HORIZON1.Controllers
{
    [Authorize] // Тільки авторизовані користувачі мають доступ до своїх подій
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _repository; // Репозиторій для роботи з БД
        private readonly ReminderFactory _reminderFactory; // Фабрика для створення об'єктів нагадувань
        

        public EventsController(IEventRepository repository, ReminderFactory reminderFactory)
        {
            _repository = repository;
            _reminderFactory = reminderFactory;
        }

        // Допоміжна властивість для швидкого отримання ID поточного користувача з токена
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // МЕТОД: Отримати абсолютно всі події користувача
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetAllEvents()
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            var events = await _repository.GetAllAsync(CurrentUserId);
            return Ok(events);
        }

        // МЕТОД: Отримати події для конкретного місяця (найскладніша логіка)
        [HttpGet("month/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEventsByMonth(int year, int month)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            // 1. Завантажуємо всі події, бо повторювана подія могла початися рік тому, але діяти зараз
            var events = await _repository.GetAllAsync(CurrentUserId); 
            
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var results = new List<Event>();

            foreach (var ev in events)
            {
                // ЛОГІКА 1: Подія без повторення (одиночна)
                if (ev.RecurrencePattern == RecurrencePattern.None)
                {
                    // Додаємо, якщо вона потрапляє в межі обраного місяця
                    if (ev.StartTime <= monthEnd && ev.EndTime >= monthStart)
                    {
                        results.Add(ev);
                    }
                    continue;
                }

                // ЛОГІКА 2: Повторювана подія (Daily, Weekly, Monthly, Yearly)
                var currentStart = ev.StartTime;
                var currentEnd = ev.EndTime;

                // Якщо цикл повторень вже закінчився до початку цього місяця — ігноруємо
                if (ev.RecurrenceEndDate.HasValue && ev.RecurrenceEndDate.Value < monthStart)
                {
                    continue;
                }

                // АЛГОРИТМ "ПЕРЕМОТКИ": Пропускаємо повторення, які були в минулих місяцях
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

                // ГЕНЕРУЄМО КЛОНИ: Створюємо копії події для кожної дати повторення всередині місяця
                while (currentStart <= monthEnd)
                {
                    // Якщо вказана дата закінчення повторень — перевіряємо її
                    if (ev.RecurrenceEndDate.HasValue && currentStart.Date > ev.RecurrenceEndDate.Value.Date)
                    {
                        break;
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
                            RecurrenceEndDate = ev.RecurrenceEndDate 
                        };

                        results.Add(occurrence);
                    }

                    // Переходимо до наступної дати згідно з патерном (день/тиждень/місяць/рік)
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

            // Повертаємо відсортовані за часом події
            return Ok(results.OrderBy(e => e.StartTime));
        }

        // POST: api/Events — Створює нову подію та планує нагадування
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent)
        {

            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();
            
            // Прив'язуємо подію до автора
            newEvent.UserId = CurrentUserId;

            // 1. Зберігаємо подію в базі
            var createdEvent = await _repository.CreateAsync(newEvent);

            // 2. ЛОГІКА НАГАДУВАННЯ: Ставимо за 15 хвилин до початку
            var reminderTime = createdEvent.StartTime.AddMinutes(-15);
            
            // Якщо до події вже менше 15 хв, ставимо сповіщення на "зараз + 1 хвилина"
            if (reminderTime < DateTime.Now) 
            {
                reminderTime = DateTime.Now.AddMinutes(1);
            }

            // 3. Формуємо об'єкт сповіщення
            var reminder = new Reminder
            {
                EventId = createdEvent.Id,
                ReminderTime = reminderTime,
                Message = $"Нагадування від HORIZON: Подія '{createdEvent.Title}' розпочнеться о {createdEvent.StartTime:HH:mm}!"
            };

            // 4. Додаємо в чергу нагадувань (їх обробить фоновий сервіс)
            await _repository.AddReminderAsync(reminder);

            
            return Ok(createdEvent);
        }

        // PUT: api/Events/{id} — Редагує існуючу подію
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event updatedEvent)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return Unauthorized();

            // Перевіряємо власність: чи ця подія належить тому, хто її хоче змінити
            var existingEvent = await _repository.GetByIdAsync(id, CurrentUserId);
            if (existingEvent == null)
                return NotFound("Подію не знайдено або у вас немає прав на її редагування.");

            // Оновлюємо основну інформацію
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartTime = updatedEvent.StartTime;
            existingEvent.EndTime = updatedEvent.EndTime;
            existingEvent.RecurrencePattern = updatedEvent.RecurrencePattern;
            existingEvent.IsRecurring = updatedEvent.RecurrencePattern != RecurrencePattern.None;
            
            // Логіка категорій (підтримка як постійних, так і тимчасових)
            existingEvent.IsTemporaryCategory = updatedEvent.IsTemporaryCategory;
            existingEvent.TemporaryCategoryName = updatedEvent.TemporaryCategoryName;
            existingEvent.TemporaryCategoryColor = updatedEvent.TemporaryCategoryColor;
            existingEvent.CategoryId = updatedEvent.IsTemporaryCategory ? null : updatedEvent.CategoryId;

            await _repository.UpdateAsync(existingEvent);

            return Ok(existingEvent);
        }

        // DELETE: api/Events/{id} — Видаляє подію
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