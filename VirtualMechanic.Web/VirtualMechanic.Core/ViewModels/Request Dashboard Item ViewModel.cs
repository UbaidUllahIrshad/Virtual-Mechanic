using System;
using System.ComponentModel.DataAnnotations;

namespace VirtualMechanic.Core.ViewModels
{
    public class RequestDashboardItemViewModel
    {
        public int RequestId { get; set; }

        // Client details
        [Display(Name = "Client Name")]
        public string ClientName { get; set; } = "";
        [Display(Name = "Client Phone")]
        public string ClientPhone { get; set; } = "";

        public string Problem { get; set; } = "";
        public string ProblemDescription { get; set; } = "";
        public string Status { get; set; } = "";

        [Display(Name = "Assigned Mechanic")]
        public string MechanicName { get; set; } = "N/A";

        public string MechanicPhone { get; set; } = "N/A";

        // Location/Time data
        public string Distance { get; set; } = "Calculating...";
        public string ETA { get; set; } = "Calculating...";
        public DateTime RequestTime { get; set; }

        public double ClientLatitude { get; set; }
        public double ClientLongitude { get; set; }
    }
}



