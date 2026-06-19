using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualMechanic.Core.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; } 

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? MechanicId { get; set; }
        public Mechanic? Mechanic { get; set; }

        public string ProblemDescription { get; set; } = "";

        public string ServiceType { get; set; } = "General";

        public string Status { get; set; } = "Pending";

        public DateTime RequestTime { get; set; } = DateTime.UtcNow;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string Distance { get; set; } = "";
        public string ETA { get; set; } = ""; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCost { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal TravelCost { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; } 
    }
}