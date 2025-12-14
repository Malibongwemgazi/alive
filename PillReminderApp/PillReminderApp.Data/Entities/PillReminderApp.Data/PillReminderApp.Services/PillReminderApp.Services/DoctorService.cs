using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PillReminderApp.Services
{
    public interface IDoctorService
    {
        Task<List<Doctor>> FindDoctorsNearbyAsync(double latitude, double longitude, string specialty = null);
        Task<List<Doctor>> FindDoctorsBySpecialtyAsync(string specialty, double latitude, double longitude);
        Task<List<EmergencyContact>> GetEmergencyContactsAsync(string location);
    }
    
    public class DoctorService : IDoctorService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _googlePlacesApiKey;
        
        public DoctorService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _googlePlacesApiKey = configuration["GooglePlaces:ApiKey"];
        }
        
        public async Task<List<Doctor>> FindDoctorsNearbyAsync(double latitude, double longitude, string specialty = null)
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            string type = "doctor";
            if (!string.IsNullOrEmpty(specialty))
            {
                type = GetGooglePlacesTypeForSpecialty(specialty);
            }
            
            var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json" +
                     $"?location={latitude},{longitude}" +
                     $"&radius=5000" +
                     $"&type={type}" +
                     $"&key={_googlePlacesApiKey}";
            
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<GooglePlacesResponse>(content);
                
                var doctors = new List<Doctor>();
                foreach (var place in result.Results.Take(10))
                {
                    var doctor = new Doctor
                    {
                        Name = place.Name,
                        Address = place.Vicinity,
                        Rating = place.Rating,
                        Types = place.Types,
                        PlaceId = place.PlaceId,
                        Latitude = place.Geometry.Location.Lat,
                        Longitude = place.Geometry.Location.Lng
                    };
                    
                    // Get phone number and other details
                    var details = await GetPlaceDetailsAsync(place.PlaceId);
                    if (details != null)
                    {
                        doctor.PhoneNumber = details.FormattedPhoneNumber;
                        doctor.Website = details.Website;
                        doctor.OpeningHours = details.OpeningHours?.WeekdayText;
                    }
                    
                    doctors.Add(doctor);
                }
                
                return doctors;
            }
            
            return new List<Doctor>();
        }
        
        private async Task<PlaceDetails> GetPlaceDetailsAsync(string placeId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://maps.googleapis.com/maps/api/place/details/json" +
                     $"?place_id={placeId}" +
                     $"&fields=formatted_phone_number,website,opening_hours" +
                     $"&key={_googlePlacesApiKey}";
            
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PlaceDetailsResponse>(content);
                return result.Result;
            }
            
            return null;
        }
        
        private string GetGooglePlacesTypeForSpecialty(string specialty)
        {
            var specialtyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"cardiologist", "doctor"},
                {"dentist", "dentist"},
                {"dermatologist", "doctor"},
                {"pediatrician", "doctor"},
                {"ophthalmologist", "doctor"},
                {"psychiatrist", "doctor"},
                {"hospital", "hospital"},
                {"clinic", "health"}
            };
            
            return specialtyMap.TryGetValue(specialty.ToLower(), out var type) ? type : "doctor";
        }
        
        public async Task<List<Doctor>> FindDoctorsBySpecialtyAsync(string specialty, double latitude, double longitude)
        {
            return await FindDoctorsNearbyAsync(latitude, longitude, specialty);
        }
        
        public async Task<List<EmergencyContact>> GetEmergencyContactsAsync(string location)
        {
            var contacts = new List<EmergencyContact>
            {
                new EmergencyContact { Name = "Emergency", Number = "911", Type = "Emergency" },
                new EmergencyContact { Name = "Poison Control", Number = "1-800-222-1222", Type = "Emergency" },
                new EmergencyContact { Name = "National Suicide Prevention", Number = "988", Type = "Emergency" }
            };
            
            return await Task.FromResult(contacts);
        }
    }
    
    public class Doctor
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Website { get; set; }
        public double Rating { get; set; }
        public List<string> Types { get; set; }
        public string PlaceId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> OpeningHours { get; set; }
        public double Distance { get; set; }
    }
    
    public class EmergencyContact
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string Type { get; set; }
    }
    
    public class GooglePlacesResponse
    {
        public List<GooglePlace> Results { get; set; }
    }
    
    public class GooglePlace
    {
        public string Name { get; set; }
        public string Vicinity { get; set; }
        public double Rating { get; set; }
        public List<string> Types { get; set; }
        public string PlaceId { get; set; }
        public Geometry Geometry { get; set; }
    }
    
    public class Geometry
    {
        public Location Location { get; set; }
    }
    
    public class Location
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
    
    public class PlaceDetailsResponse
    {
        public PlaceDetails Result { get; set; }
    }
    
    public class PlaceDetails
    {
        public string FormattedPhoneNumber { get; set; }
        public string Website { get; set; }
        public OpeningHours OpeningHours { get; set; }
    }
    
    public class OpeningHours
    {
        public List<string> WeekdayText { get; set; }
    }
}
