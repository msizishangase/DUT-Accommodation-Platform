using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DUT_Accommodation_Platform.Models
{
    public class RegistrationSuccessViewModel
    {
        public string UserType { get; set; } // "Student" or "Landlord"
        public string Email { get; set; }
        public string RedirectUrl { get; set; }
        public int CountdownSeconds { get; set; } = 3;
    }
}