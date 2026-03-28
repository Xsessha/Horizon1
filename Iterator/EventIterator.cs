using HORIZON1.Models;

namespace HORIZON1.Iterator
{
    public class EventIterator : IEventIterator
    {
        private readonly List<Event> _events;
        private int _position = 0;

        public EventIterator(IEnumerable<Event> events)
        {
            _events = events.ToList();
        }

        public bool HasNext() => _position < _events.Count;

        public Event Next() => _events[_position++];
    }
}