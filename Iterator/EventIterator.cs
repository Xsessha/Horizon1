using HORIZON1.Models;

namespace HORIZON1.Iterator
{
    // КЛАС-ІТЕРАТОР: Відповідає за послідовний перебір списку подій
    public class EventIterator : IEventIterator
    {
        // Внутрішній список подій, який ми будемо перебирати
        private readonly List<Event> _events;

        // Поточна позиція (курсор) у списку
        private int _position = 0;

        // Конструктор: отримує будь-яку колекцію подій і перетворює її на список для зручної роботи
        public EventIterator(IEnumerable<Event> events)
        {
            _events = events.ToList();
        }

        // МЕТОД HasNext: Перевіряє, чи є ще події попереду.
        // Повертає true, якщо ми ще не дійшли до кінця списку.
        public bool HasNext() => _position < _events.Count;

        // МЕТОД Next: Повертає поточну подію і пересуває курсор на один крок вперед (_position++)
        public Event Next() => _events[_position++];
    }
}