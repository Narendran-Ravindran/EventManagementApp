using EventManagement.Data;
using EventManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventController> _logger; // Add this

        public EventController(ApplicationDbContext context, ILogger<EventController> logger) // Inject logger
        {
            _context = context;
            _logger = logger; // Assign logger
        }

        // API to create events
        [HttpPost("create-event")]
        public IActionResult CreateEvent([Required] string eventName, DateTime? startDateTime = null, DateTime? endDateTime = null)
        {
            _logger.LogInformation("Creating event: {EventName}", eventName); // Log event creation
            try
            {
                if (startDateTime == null)
                {
                    startDateTime = DateTime.Now;
                }
                if (endDateTime == null)
                {
                    endDateTime = DateTime.Now.AddDays(1);
                }


                if (startDateTime >= endDateTime)
                {
                    _logger.LogError("End date must be after start date.");
                    return BadRequest("End date must be after start date.");
                }

                var existingEvent = _context.Events
                    .Where(e => (e.StartDateTime < endDateTime && e.EndDateTime > startDateTime) && e.EventName == eventName)
                    .FirstOrDefault();

                if (existingEvent != null)
                {
                    _logger.LogError("An event already exists within the specified date range");
                    return BadRequest("An event already exists within the specified date range.");
                }

                Event newEvent = new Event
                {
                    EventName = eventName,
                    StartDateTime = startDateTime.Value,
                    EndDateTime = endDateTime.Value
                };

                _context.Events.Add(newEvent);
                _context.SaveChanges();

                _logger.LogInformation("Event created successfully: {EventId}", newEvent.EventId); // Log success
                return Ok(newEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating event"); // Log error
                return StatusCode(500, "An error occurred while creating the event.");
            }
        }

        // Remaining methods (GetEvents, ImportAttendees) can also be updated similarly...

        // Enum to define filter options && JsonConverter is used for converting 0,1,2 into text datatype
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum EventFilter
        {
            upcoming,
            past,
            all
        }

        [HttpGet]
        public IActionResult GetEvents([FromQuery] EventFilter filter = EventFilter.upcoming)
        {
            DateTime currentDate = DateTime.Now;
            _logger.LogInformation("Fetching events with filter: {Filter}", filter); // Log filter

            IQueryable<Event> events;

            switch (filter)
            {
                case EventFilter.past:
                    events = _context.Events.Where(e => e.EndDateTime < currentDate);
                    break;
                case EventFilter.all:
                    events = _context.Events;
                    break;
                default:
                    events = _context.Events.Where(e => e.StartDateTime >= currentDate);
                    break;
            }

            _logger.LogInformation("Fetched {EventCount} events", events.Count()); // Log the number of events fetched
            return Ok(events.ToList());
        }

        // API to import attendees to an event and check for conflicts
        [HttpPost("import-attendees")]
        public IActionResult ImportAttendees(int eventId, List<int> attendeeIds)
        {
            _logger.LogInformation("Importing attendees for event ID: {EventId}", eventId); // Log the import attempt

            var eventToAttend = _context.Events.Find(eventId);
            if (eventToAttend == null)
            {
                _logger.LogError("Event not found for ID: {EventId}", eventId); // Log warning for not found event
                return BadRequest("Event not found. Please try to provide a valid EventId.");
            }

            // Lists to hold conflicting and valid attendees
            var conflictingAttendees = new List<int>();
            var validAttendees = new List<EventAttendee>();

            foreach (var attendeeId in attendeeIds)
            {
                _logger.LogInformation("Checking for conflicts for attendee ID: {AttendeeId}", attendeeId); // Log conflict check

                // Check if the attendee is already booked for another event that overlaps with the current event
                var conflictExists = _context.EventAttendees
                    .Include(ea => ea.Event)
                    .Any(ea => ea.UserId == attendeeId &&
                               ea.Event.StartDateTime < eventToAttend.EndDateTime &&
                               ea.Event.EndDateTime > eventToAttend.StartDateTime);

                if (conflictExists)
                {
                    // Add to conflicting attendees list
                    conflictingAttendees.Add(attendeeId);
                    _logger.LogWarning("Conflict exists for attendee ID: {AttendeeId}", attendeeId); // Log conflict
                }
                else
                {
                    // Add to valid attendees list
                    validAttendees.Add(new EventAttendee
                    {
                        UserId = attendeeId,
                        EventId = eventId
                    });
                }
            }

            // Save valid attendees to the EventAttendees table if there are any
            if (validAttendees.Any())
            {
                _logger.LogInformation("Saving {ValidAttendeesCount} valid attendees", validAttendees.Count); // Log the number of valid attendees
                _context.EventAttendees.AddRange(validAttendees);
                _context.SaveChanges();
            }

            // Get the names of the conflicting attendees
            var conflictingAttendeeNames = _context.Users
                .Where(u => conflictingAttendees.Contains(u.UserId))
                .Select(u => u.UserName)
                .ToList();

            // Prepare response
            if (conflictingAttendeeNames.Any())
            {
                _logger.LogInformation("{ValidCount} attendees imported successfully. {ConflictingCount} attendees have conflicts.",
                    validAttendees.Count, conflictingAttendeeNames.Count); // Log import success with conflict details

                return Ok(new
                {
                    Message = $"{validAttendees.Count} attendees imported successfully. {conflictingAttendeeNames.Count} attendees have conflicts.",
                    ImportedAttendeesCount = validAttendees.Count,
                    ConflictingAttendeesCount = conflictingAttendeeNames.Count,
                    ConflictingAttendees = conflictingAttendeeNames
                });
            }
            else
            {
                _logger.LogInformation("All attendees were imported successfully without conflicts."); // Log successful import with no conflicts

                return Ok(new
                {
                    Message = "All attendees were imported successfully without conflicts.",
                    ImportedAttendeesCount = validAttendees.Count
                });
            }
        }

    }
}
