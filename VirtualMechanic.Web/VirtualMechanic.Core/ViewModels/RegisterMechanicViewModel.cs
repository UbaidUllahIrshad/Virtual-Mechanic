using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VirtualMechanic.Core.ViewModels
{
    public class RegisterMechanicViewModel : RegisterBaseViewModel
    {

        [Display(Name = "Select Your Specialties")]
        public List<string> SelectedSpecialties { get; set; } = new List<string>();

        public static List<string> AvailableOptionList = new List<string>
        {
            "Flat Tyre",
            "Dead Battery",
            "Engine Heat",
            "Towing",
            "Other"
        };
    }
}