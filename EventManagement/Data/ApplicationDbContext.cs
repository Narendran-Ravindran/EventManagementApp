using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using Microsoft.Extensions.Logging;

namespace EventManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventAttendee> EventAttendees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite key for EventAttendee
            modelBuilder.Entity<EventAttendee>()
                .HasKey(ea => new { ea.UserId, ea.EventId });
        }
    }
}
