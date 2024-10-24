//using EventManagement.Models;
namespace EventManagement.Models;

public class EventAttendee
{
    public int UserId { get; set; }
    public User User { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; }
}
