using Microsoft.EntityFrameworkCore;
using HORIZON1.Data;
using HORIZON1.Models;

namespace HORIZON1.Repository
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetAllAsync(string userId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .ToListAsync();
        }

        public async Task<Event?> GetByIdAsync(int id, string userId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        }

        public async Task<Event> CreateAsync(Event newEvent)
        {
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        public async Task<Event> UpdateAsync(Event updatedEvent)
        {
            _context.Events.Update(updatedEvent);
            await _context.SaveChangesAsync();
            return updatedEvent;
        }

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

        public async Task<IEnumerable<Event>> GetEventsByMonthAsync(int year, int month, string userId)
        {
            DateTime monthStart = new DateTime(year, month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.UserId == userId
                         && e.StartTime <= monthEnd
                         && e.EndTime >= monthStart)
                .OrderBy(e => e.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEventsByCategoryAsync(int categoryId, string userId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && e.CategoryId == categoryId)
                .ToListAsync();
        }
    }
}