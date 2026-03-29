using HORIZON1.Models;

namespace HORIZON1.Iterator
{
    public interface IEventIterator
    {
        bool HasNext();
        Event Next();
    }
}