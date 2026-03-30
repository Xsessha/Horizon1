using HORIZON1.Models;

namespace HORIZON1.Repository
{
    public interface IEventRepository
    {
        Task<IEnumerable<Event>> GetAllAsync(string userId);
        Task<Event?> GetByIdAsync(int id, string userId);
        Task<Event> CreateAsync(Event newEvent);
        Task<Event> UpdateAsync(Event updatedEvent);
        Task<bool> DeleteAsync(int id, string userId);

        Task<IEnumerable<Event>> GetEventsByMonthAsync(int year, int month, string userId);
        Task<IEnumerable<Event>> GetEventsByCategoryAsync(int categoryId, string userId);
    }
}