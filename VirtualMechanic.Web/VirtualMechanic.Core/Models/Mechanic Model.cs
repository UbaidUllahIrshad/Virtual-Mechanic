using System.Collections.Generic;

namespace VirtualMechanic.Core.Models
{
    public class Mechanic
    {
        public int MechanicId { get; set; } 
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";

        // Status: Available, Busy, Offline
        public string Status { get; set; } = "Offline";

        public string Specialty { get; set; } = "General";

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public ICollection<ServiceRequest> AssignedRequests { get; set; } = new List<ServiceRequest>();
    }
}