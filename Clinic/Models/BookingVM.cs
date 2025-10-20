using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Models
{
    public class BookingVM
    {
        [Required] public int DoctorId { get; set; }
        [Required] public int ServiceId { get; set; }

        public string DoctorName { get; set; }
        public List<DateTime> AvailableSlotsLocal { get; set; } = new List<DateTime>();

        [Required] public DateTime SelectedStartLocal { get; set; }
        public string Notes { get; set; }
    }
}
