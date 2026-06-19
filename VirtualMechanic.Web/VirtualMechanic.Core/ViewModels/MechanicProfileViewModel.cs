using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VirtualMechanic.Core.ViewModels
{
    public class MechanicProfileViewModel
    {
        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        public string Email { get; set; } = "";

        [Display(Name = "Update Specialties")]
        public List<string> SelectedSpecialties { get; set; } = new List<string>();

        public static List<string> AvailableOptionList = new List<string>
        {
            "Flat Tyre", "Dead Battery", "Engine Heat", "Towing", "Other"
        };
    }
}