using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VirtualMechanic.Core.ViewModels
{
    public class RegisterRoleViewModel
    {
        [Required(ErrorMessage = "Please select an account type.")]
        [Display(Name = "Account Type")]
        public string SelectedRole { get; set; } = "";
        public List<string> AvailableRoles { get; set; } = new List<string> { "Client", "Mechanic" };
    }
}