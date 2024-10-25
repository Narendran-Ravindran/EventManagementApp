using EventManagement.Controllers;
using EventManagement.Data;
using EventManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static EventManagement.Controllers.EventController;

namespace EventManagement.Tests
{
    public class EventControllerTests
    {
        private readonly Mock<ILogger<EventController>> _mockLogger;
        private readonly ApplicationDbContext _context;
        private readonly EventController _controller;

        public EventControllerTests()
        {
            _mockLogger = new Mock<ILogger<EventController>>();

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "EventManagementDb")
                .Options;
            _context = new ApplicationDbContext(options);

            // Seed some data for tests
            SeedDatabase();

            // Initialize the controller with the in-memory context and mock logger
            _controller = new EventController(_context, _mockLogger.Object);
        }

        private void SeedDatabase()
        {
            // Clear existing data
            _context.Events.RemoveRange(_context.Events);
            _context.Users.RemoveRange(_context.Users);
            _context.EventAttendees.RemoveRange(_context.EventAttendees);
            _context.SaveChanges();

            // Add some sample data
            _context.Events.Add(new Event { EventId = 1, EventName = "Sample Event 1", StartDateTime = DateTime.Now.AddDays(1), EndDateTime = DateTime.Now.AddDays(2) });
            _context.Events.Add(new Event { EventId = 2, EventName = "Sample Event 2", StartDateTime = DateTime.Now.AddDays(3), EndDateTime = DateTime.Now.AddDays(4) });
            _context.Events.Add(new Event { EventId = 3, EventName = "Sample Event 3", StartDateTime = DateTime.Now.AddDays(1), EndDateTime = DateTime.Now.AddDays(2) });
            _context.Users.Add(new User { UserId = 1, UserName = "Lokesh" });
            _context.Users.Add(new User { UserId = 2, UserName = "Naren" });
            _context.Users.Add(new User { UserId = 3, UserName = "Sarath Kumar" });
            _context.SaveChanges();
        }

        [Fact]
        public void CreateEvent_ShouldReturnOk_WhenEventIsCreatedSuccessfully()
        {
            // Arrange
            string eventName = "New Event";
            DateTime startDateTime = DateTime.Now.AddDays(5);
            DateTime endDateTime = DateTime.Now.AddDays(6);

            // Act
            var result = _controller.CreateEvent(eventName, startDateTime, endDateTime);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdEvent = Assert.IsType<Event>(okResult.Value);
            Assert.Equal(eventName, createdEvent.EventName);
        }

        
        [Fact]
        public void CreateEvent_ShouldAssignDefaultStartAndEndDateTime_WhenBothAreNull()
        {
            // Arrange
            string eventName = "Event With Default Dates";
            DateTime? startDateTime = null; // Passing null for both startDateTime and endDateTime
            DateTime? endDateTime = null;

            // Act
            var result = _controller.CreateEvent(eventName, startDateTime, endDateTime);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdEvent = Assert.IsType<Event>(okResult.Value);
            Assert.Equal(eventName, createdEvent.EventName);
            Assert.Equal(DateTime.Now.Date, createdEvent.StartDateTime.Date); // Verifying default value for startDateTime
            Assert.Equal(DateTime.Now.AddDays(1).Date, createdEvent.EndDateTime.Date); // Verifying default value for endDateTime
        }


        [Fact]
        public void CreateEvent_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
        {
            // Arrange
            string eventName = "Invalid Event";
            DateTime startDateTime = DateTime.Now.AddDays(5);
            DateTime endDateTime = DateTime.Now.AddDays(4); // Invalid range

            // Act
            var result = _controller.CreateEvent(eventName, startDateTime, endDateTime);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("End date must be after start date.", badRequestResult.Value);
        }

        [Fact]
        public void CreateEvent_ShouldReturnBadRequest_WhenEventAlreadyExistsInTheSameDateRange()
        {
            // Arrange
            string eventName = "Sample Event 1"; // This event already exists in the seeded data
            DateTime startDateTime = DateTime.Now.AddDays(1);
            DateTime endDateTime = DateTime.Now.AddDays(2);

            // Act
            var result = _controller.CreateEvent(eventName, startDateTime, endDateTime);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("An event already exists within the specified date range.", badRequestResult.Value);
        }

        [Fact]
        public void EventAttendee_ShouldHaveCorrectUserAndEvent_WhenInitialized()
        {
            // Arrange
            var user = new User { UserId = 1, UserName = "Lokesh" };
            var eventEntity = new Event { EventId = 1, EventName = "Sample Event 1", StartDateTime = DateTime.Now.AddDays(1), EndDateTime = DateTime.Now.AddDays(2) };
            var eventAttendee = new EventAttendee { UserId = user.UserId, User = user, EventId = eventEntity.EventId, Event = eventEntity };

            // Act
            var actualUser = eventAttendee.User;
            var actualEvent = eventAttendee.Event;

            // Assert
            Assert.NotNull(actualUser);
            Assert.Equal(user.UserId, actualUser.UserId);
            Assert.Equal(user.UserName, actualUser.UserName);

            Assert.NotNull(actualEvent);
            Assert.Equal(eventEntity.EventId, actualEvent.EventId);
            Assert.Equal(eventEntity.EventName, actualEvent.EventName);
        }

        [Fact]
        public void CreateEvent_ShouldReturnInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            string eventName = "Event causing exception";
            DateTime startDateTime = DateTime.Now.AddDays(5);
            DateTime endDateTime = DateTime.Now.AddDays(6);

            // Set up the in-memory database with an exception-throwing context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            var mockLogger = new Mock<ILogger<EventController>>();

            // Use a real ApplicationDbContext with an in-memory database
            var context = new ApplicationDbContext(options);

            // Mocking SaveChanges to throw an exception
            var mockContext = new Mock<ApplicationDbContext>(options);
            mockContext.Setup(c => c.SaveChanges()).Throws(new Exception("Test exception"));

            // Create the controller with the mocked context
            var controller = new EventController(mockContext.Object, mockLogger.Object);

            // Act
            var result = controller.CreateEvent(eventName, startDateTime, endDateTime);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while creating the event.", statusCodeResult.Value);
        }


        [Fact]
        public void GetEvents_ShouldReturnUpcomingEventsByDefault()
        {
            // Act
            var result = _controller.GetEvents();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var events = Assert.IsType<List<Event>>(okResult.Value);
            Assert.True(events.All(e => e.StartDateTime >= DateTime.Now));
        }

        [Fact]
        public void GetEvents_ShouldReturnPastEvents_WhenFilterIsPast()
        {
            // Arrange
            var filter = EventFilter.past;

            // Act
            var result = _controller.GetEvents(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var events = Assert.IsType<List<Event>>(okResult.Value);

            // Verify that all returned events have ended before the current date
            Assert.All(events, e => Assert.True(e.EndDateTime < DateTime.Now));
        }

        [Fact]
        public void GetEvents_ShouldReturnAllEvents_WhenFilterIsAll()
        {
            // Arrange
            var filter = EventFilter.all;

            // Act
            var result = _controller.GetEvents(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var events = Assert.IsType<List<Event>>(okResult.Value);

            // Verify that all events are returned
            Assert.Equal(_context.Events.Count(), events.Count);
        }

        [Fact]
        public void ImportAttendees_ShouldReturnBadRequest_WhenEventDoesNotExist()
        {
            // Arrange
            int nonExistentEventId = 999;
            var attendeeIds = new List<int> { 1 };

            // Act
            var result = _controller.ImportAttendees(nonExistentEventId, attendeeIds);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Event not found. Please try to provide a valid EventId.", badRequestResult.Value);
        }

        [Fact]
        public void ImportAttendees_ShouldReturnOk_WhenAttendeesImportedSuccessfully()
        {
            // Arrange
            int eventId = 1;
            var attendeeIds = new List<int> { 1,2 };

            // Act
            var result = _controller.ImportAttendees(eventId, attendeeIds);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseObject = okResult.Value;
            // Check if the returned object has the expected structure
            Assert.NotNull(responseObject);

            // Access properties using reflection
            var messageProperty = responseObject.GetType().GetProperty("Message")?.GetValue(responseObject, null)?.ToString();
            int importedAttendeesCount = (int)(responseObject.GetType().GetProperty("ImportedAttendeesCount")?.GetValue(responseObject, null) ?? 0);

            // Verify the properties
            Assert.Equal("All attendees were imported successfully without conflicts.", messageProperty);
            Assert.Equal(2, importedAttendeesCount);
            Assert.NotEqual(1, importedAttendeesCount);



        }


        [Fact]
        public void ImportAttendees_ShouldReturnOkWithConflicts_WhenSomeAttendeesHaveConflicts()
        {
            // Arrange
            int eventId = 3;
            var attendeeIds = new List<int> { 1,2};

            // Act - First import should be successful
            _controller.ImportAttendees(eventId, attendeeIds);

            // Act - Second import should find conflicts
            var result = _controller.ImportAttendees(eventId, attendeeIds);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseObject = okResult.Value;
            Assert.NotNull(responseObject);

            // Access properties using reflection
            var messageProperty = responseObject.GetType().GetProperty("Message")?.GetValue(responseObject, null)?.ToString();
            int importedAttendeesCountProperty = (int)(responseObject.GetType().GetProperty("ImportedAttendeesCount")?.GetValue(responseObject, null) ?? 0);
            int ConflictingAttendeesCountProperty = (int)(responseObject.GetType().GetProperty("ConflictingAttendeesCount")?.GetValue(responseObject, null) ?? 0);
            var ConflictingAttendeesProperty = responseObject.GetType().GetProperty("ConflictingAttendees")?.GetValue(responseObject, null)?.ToString();
            Console.WriteLine(ConflictingAttendeesProperty);

            // Additional assertions to cover the message format
            Assert.NotNull(messageProperty);
            Assert.Contains("attendees imported successfully", messageProperty);
            Assert.Contains("attendees have conflicts", messageProperty);

            // Check if the numbers in the message are correct
            string expectedMessage = $"{importedAttendeesCountProperty} attendees imported successfully. {ConflictingAttendeesCountProperty} attendees have conflicts.";
            Assert.Equal(expectedMessage, messageProperty);

            // Verify the counts
            Assert.Equal(2, ConflictingAttendeesCountProperty);
            Assert.NotEqual(1, ConflictingAttendeesCountProperty);
            Assert.Equal(0, importedAttendeesCountProperty);
            Assert.NotEqual(100, importedAttendeesCountProperty);

            //var response = Assert.IsType<dynamic>(okResult.Value);
            //Assert.Contains("have conflicts", response.Message.ToString());
            //Assert.Equal(1, (int)response.ConflictingAttendeesCount);
        }
    }
}
