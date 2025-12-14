using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PillReminderApp.Data;
using PillReminderApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace PillReminderApp.Services
{
    public interface IPillReminderService
    {
        Task<Reminder> CreateReminderAsync(Reminder reminder);
        Task<List<Reminder>> GetActiveRemindersAsync(string patientId);
        Task CheckAndSendRemindersAsync();
        Task<List<Reminder>> GetTodayRemindersAsync(string patientId);
    }
    
    public class PillReminderService : IPillReminderService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        
        public PillReminderService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }
        
        public async Task<Reminder> CreateReminderAsync(Reminder reminder)
        {
            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();
            return reminder;
        }
        
        public async Task<List<Reminder>> GetActiveRemindersAsync(string patientId)
        {
            return await _context.Reminders
                .Where(r => r.PatientId == patientId && r.IsActive && r.EndDate >= DateTime.UtcNow)
                .ToListAsync();
        }
        
        public async Task CheckAndSendRemindersAsync()
        {
            var now = DateTime.Now;
            var activeReminders = await _context.Reminders
                .Where(r => r.IsActive && r.EndDate >= DateTime.UtcNow)
                .ToListAsync();
            
            foreach (var reminder in activeReminders)
            {
                var schedule = System.Text.Json.JsonSerializer.Deserialize<Schedule>(reminder.Schedule);
                if (schedule != null && schedule.ShouldNotify(now))
                {
                    await _notificationService.SendNotificationAsync(
                        reminder.PatientId,
                        $"Time to take {reminder.MedicineName}",
                        $"Doctor's instructions: {reminder.DoctorInstructions}"
                    );
                }
            }
        }
        
        public async Task<List<Reminder>> GetTodayRemindersAsync(string patientId)
        {
            var today = DateTime.Today;
            return await _context.Reminders
                .Where(r => r.PatientId == patientId && 
                           r.IsActive && 
                           r.StartDate.Date <= today && 
                           r.EndDate.Date >= today)
                .ToListAsync();
        }
    }
    
    public class Schedule
    {
        public List<TimeSpan> Times { get; set; } = new List<TimeSpan>();
        public List<DayOfWeek> Days { get; set; } = new List<DayOfWeek>();
        
        public bool ShouldNotify(DateTime currentTime)
        {
            if (!Days.Contains(currentTime.DayOfWeek))
                return false;
                
            var currentTimeOfDay = currentTime.TimeOfDay;
            foreach (var time in Times)
            {
                var timeWindowStart = time.Subtract(TimeSpan.FromMinutes(5));
                var timeWindowEnd = time.Add(TimeSpan.FromMinutes(5));
                
                if (currentTimeOfDay >= timeWindowStart && currentTimeOfDay <= timeWindowEnd)
                    return true;
            }
            
            return false;
        }
    }
}
