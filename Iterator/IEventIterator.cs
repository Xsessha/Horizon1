using HORIZON1.Models;

namespace HORIZON1.Iterator
{
    // ІНТЕРФЕЙС ІТЕРАТОРА: Визначає стандартні методи для перебору колекції подій
    public interface IEventIterator
    {
        // Метод має повертати true, якщо в списку ще залишилися події, які ми не переглянули
        bool HasNext();
        
        // Метод має повертати наступний об'єкт Event зі списку
        Event Next();
    }
}