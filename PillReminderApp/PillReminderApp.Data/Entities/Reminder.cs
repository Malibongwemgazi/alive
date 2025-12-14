using System;
using System.ComponentModel.DataAnnotations;

namespace PillReminderApp.Data.Entities
{
    public class Reminder
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string PatientId { get; set; }
        
        [Required]
        public string MedicineName { get; set; }
        
        [Required]
        public string SicknessType { get; set; }
        
        [Required]
        public string DoctorInstructions { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public string Dosage { get; set; }
        
        [Required]
        public string Schedule { get; set; } // JSON serialized schedule
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
