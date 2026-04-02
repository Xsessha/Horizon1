using HORIZON1.Models;

namespace HORIZON1.Repository
{
    // ІНТЕРФЕЙС РЕПОЗИТОРІЮ: Це "контракт", який описує, які методи 
    // повинні бути реалізовані для роботи з подіями в базі даних.
    public interface IEventRepository
    {
        // Task<IEnumerable<Event>> — означає, що метод асинхронний і поверне список подій.
        Task<IEnumerable<Event>> GetAllAsync(string userId);

        // Task<Event?> — знак питання означає, що якщо подію не знайдено, метод поверне null.
        Task<Event?> GetByIdAsync(int id, string userId);

        // Методи для стандартних операцій (CRUD): Створення та Оновлення.
        Task<Event> CreateAsync(Event newEvent);
        Task<Event> UpdateAsync(Event updatedEvent);

        // Task<bool> — повертає true, якщо видалення пройшло успішно, і false, якщо ні.
        Task<bool> DeleteAsync(int id, string userId);

        // Метод для планування нагадувань.
        Task AddReminderAsync(Reminder reminder);
        
        // Спеціальні фільтри: пошук подій за конкретний місяць або за категорією.
        Task<IEnumerable<Event>> GetEventsByMonthAsync(int year, int month, string userId);
        Task<IEnumerable<Event>> GetEventsByCategoryAsync(int categoryId, string userId);
    }
}