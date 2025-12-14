using Microsoft.EntityFrameworkCore;
using PillReminderApp.Data.Entities;

namespace PillReminderApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<DoctorContact> DoctorContacts { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
