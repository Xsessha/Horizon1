namespace HORIZON1.Models
{
    public class Reminder
    {
        public int Id { get; set; }
        
        public int EventId { get; set; }
        public Event? Event { get; set; } 
        
        public DateTime RemindAt { get; set; }
        public bool IsSent { get; set; }
    }
}