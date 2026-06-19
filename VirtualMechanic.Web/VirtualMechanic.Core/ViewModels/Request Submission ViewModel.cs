using System.ComponentModel.DataAnnotations;

namespace VirtualMechanic.Core.ViewModels
{
    public class RequestSubmissionViewModel
    {
        [Required(ErrorMessage = "Please select the type of service required.")]
        [Display(Name = "Select Service")]
        public string ServiceType { get; set; } = "";

        [Display(Name = "Additional Notes (Optional)")]
        public string ProblemDescription { get; set; } = "";

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }
}







