namespace VirtualMechanic.Core.Models
{
   
    public class User
    {
        public int UserId { get; set; } 
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "Client"; 

        public required ICollection<ServiceRequest> ServiceRequests { get; set; }
    }
}