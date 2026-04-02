using Microsoft.EntityFrameworkCore;
using HORIZON1.Data;
using HORIZON1.Models;

namespace HORIZON1.Repository
{
    // КЛАС-РЕПОЗИТОРІЙ: Ізолює логіку доступу до даних від решти програми.
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context; // Контекст бази даних

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        // Отримати всі події користувача (за винятком видалених)
        public async Task<IEnumerable<Event>> GetAllAsync(string userId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && !e.IsDeleted)
                .ToListAsync();
        }

        // Отримати конкретну подію за ID
        public async Task<Event?> GetByIdAsync(int id, string userId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted);
        }

        // Створити нову подію
        public async Task<Event> CreateAsync(Event newEvent)
        {
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        // Оновити існуючу подію
        public async Task<Event> UpdateAsync(Event updatedEvent)
        {
            _context.Events.Update(updatedEvent);
            await _context.SaveChangesAsync();
            return updatedEvent;
        }

        // М'яке видалення (Soft Delete)
        public async Task<bool> DeleteAsync(int id, string userId)
        {
            var eventToDelete = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventToDelete == null)
                return false;

            eventToDelete.IsDeleted = true;
            _context.Events.Update(eventToDelete);
            await _context.SaveChangesAsync();
            
            return true;
        }

        // Отримати події для конкретного місяця (з урахуванням тих, що перекриваються)
        public async Task<IEnumerable<Event>> GetEventsByMonthAsync(int year, int month, string userId)
        {
            DateTime monthStart = new DateTime(year, month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.UserId == userId
                         && !e.IsDeleted
                         && e.StartTime <= monthEnd
                         && e.EndTime >= monthStart)
                .OrderBy(e => e.StartTime)
                .ToListAsync();
        }

        // Отримати події за категорією
        public async Task<IEnumerable<Event>> GetEventsByCategoryAsync(int categoryId, string userId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && e.CategoryId == categoryId && !e.IsDeleted)
                .ToListAsync();
        }

        // Додати нагадування в базу даних
        public async Task AddReminderAsync(Reminder reminder)
        {
            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();
        }
    }
}