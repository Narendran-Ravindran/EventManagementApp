namespace EventManagement.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public string EventName { get; set; }

        public DateTime StartDateTime { get; set; } 
        public DateTime EndDateTime { get; set; }   
    }
}
