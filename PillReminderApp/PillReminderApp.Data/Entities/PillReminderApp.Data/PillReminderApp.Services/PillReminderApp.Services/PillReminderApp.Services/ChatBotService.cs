using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PillReminderApp.Data;
using PillReminderApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace PillReminderApp.Services
{
    public interface IChatBotService
    {
        Task<ChatResponse> ProcessMessageAsync(string userId, string message, string context = null);
        Task<List<ChatHistory>> GetChatHistoryAsync(string userId);
    }
    
    public class ChatBotService : IChatBotService
    {
        private readonly AppDbContext _context;
        private readonly IDoctorService _doctorService;
        
        public ChatBotService(AppDbContext context, IDoctorService doctorService)
        {
            _context = context;
            _doctorService = doctorService;
        }
        
        public async Task<ChatResponse> ProcessMessageAsync(string userId, string message, string context = null)
        {
            var response = new ChatResponse();
            message = message.ToLower();
            
            // Save chat history
            var chatEntry = new ChatHistory
            {
                UserId = userId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsUserMessage = true
            };
            _context.ChatHistories.Add(chatEntry);
            
            // Process message
            if (message.Contains("how are you") || message.Contains("how do you feel"))
            {
                response.Message = "I'm here to help you with your health needs. How are you feeling today?";
                response.Type = "greeting";
            }
            else if (message.Contains("not feeling well") || message.Contains("feeling sick"))
            {
                response = await HandleHealthConcern(message, context);
            }
            else if (message.Contains("remind") || message.Contains("pill") || message.Contains("medicine"))
            {
                response.Message = "I can help you set up pill reminders. Please tell me: " +
                                 "1. Medicine name\n2. Sickness type\n3. Doctor's instructions\n4. When to take it (e.g., morning, afternoon, night)";
                response.Type = "reminder_setup";
            }
            else if (message.Contains("doctor") || message.Contains("hospital"))
            {
                response.Message = "I can help you find doctors nearby. Please share your location or describe your symptoms so I can recommend the right specialist.";
                response.Type = "doctor_finder";
            }
            else if (message.Contains("emergency") || message.Contains("urgent"))
            {
                response.Message = "⚠️ **EMERGENCY ALERT** ⚠️\n" +
                                 "If this is a medical emergency, please call 911 immediately.\n" +
                                 "Otherwise, I can connect you with emergency services or nearby hospitals.";
                response.Type = "emergency";
                response.EmergencyContacts = await _doctorService.GetEmergencyContactsAsync("current");
            }
            else
            {
                response.Message = GetGeneralHealthAdvice(message);
                response.Type = "general_advice";
            }
            
            // Save bot response
            var botEntry = new ChatHistory
            {
                UserId = userId,
                Message = response.Message,
                Timestamp = DateTime.UtcNow,
                IsUserMessage = false,
                ResponseType = response.Type
            };
            _context.ChatHistories.Add(botEntry);
            
            await _context.SaveChangesAsync();
            
            return response;
        }
        
        private async Task<ChatResponse> HandleHealthConcern(string message, string context)
        {
            var response = new ChatResponse { Type = "health_concern" };
            
            // Simple symptom analysis
            if (message.Contains("fever") || message.Contains("temperature"))
            {
                response.Message = "For fever:\n" +
                                 "• Rest and stay hydrated\n" +
                                 "• Take acetaminophen or ibuprofen (if not allergic)\n" +
                                 "• Use cool compresses\n" +
                                 "• Monitor temperature every 4 hours\n\n" +
                                 "**Seek medical attention if:**\n" +
                                 "• Fever above 103°F (39.4°C)\n" +
                                 "• Fever lasts more than 3 days\n" +
                                 "• Difficulty breathing or chest pain";
                response.HomeRemedies = new List<string>
                {
                    "Drink plenty of fluids",
                    "Take a lukewarm bath",
                    "Use lightweight clothing",
                    "Get plenty of rest"
                };
            }
            else if (message.Contains("headache") || message.Contains("migraine"))
            {
                response.Message = "For headaches:\n" +
                                 "• Rest in a quiet, dark room\n" +
                                 "• Apply cold or warm compress to forehead\n" +
                                 "• Stay hydrated\n" +
                                 "• Consider over-the-counter pain relievers\n\n" +
                                 "**Seek medical attention if:**\n" +
                                 "• Sudden, severe headache\n" +
                                 "• Headache with fever, stiff neck, or confusion\n" +
                                 "• Headache after head injury";
            }
            else if (message.Contains("cough") || message.Contains("cold"))
            {
                response.Message = "For cough/cold:\n" +
                                 "• Drink warm liquids (tea with honey)\n" +
                                 "• Use a humidifier\n" +
                                 "• Gargle with salt water\n" +
                                 "• Get plenty of rest\n\n" +
                                 "**Seek medical attention if:**\n" +
                                 "• Difficulty breathing\n" +
                                 "• Chest pain\n" +
                                 "• Coughing up blood\n" +
                                 "• Symptoms last more than 10 days";
            }
            else
            {
                response.Message = "I understand you're not feeling well. Here are general tips:\n" +
                                 "• Stay hydrated\n" +
                                 "• Get plenty of rest\n" +
                                 "• Monitor your symptoms\n" +
                                 "• Take prescribed medications as directed\n\n" +
                                 "Would you like me to help you find a doctor nearby or provide more specific advice?";
            }
            
            return response;
        }
        
        private string GetGeneralHealthAdvice(string message)
        {
            var tips = new List<string>
            {
                "Remember to take your prescribed medications on time.",
                "Stay hydrated by drinking at least 8 glasses of water daily.",
                "Regular exercise can improve your overall health.",
                "Maintain a balanced diet with plenty of fruits and vegetables.",
                "Get 7-9 hours of sleep each night for optimal health.",
                "Manage stress through meditation or deep breathing exercises.",
                "Don't skip medical appointments and follow up with your doctor.",
                "Wash your hands regularly to prevent infections."
            };
            
            var random = new Random();
            return tips[random.Next(tips.Count)];
        }
        
        public async Task<List<ChatHistory>> GetChatHistoryAsync(string userId)
        {
            return await _context.ChatHistories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();
        }
    }
    
    public class ChatResponse
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public List<string> HomeRemedies { get; set; }
        public List<Doctor> RecommendedDoctors { get; set; }
        public List<EmergencyContact> EmergencyContacts { get; set; }
        public bool RequiresImmediateAttention { get; set; }
    }
}
