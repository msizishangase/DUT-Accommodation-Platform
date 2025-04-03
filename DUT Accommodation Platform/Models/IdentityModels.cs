using System;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace DUT_Accommodation_Platform.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        // Common fields
        public string FullName { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        // Student-specific fields
        public string StudentNumber { get; set; }
        public bool IsStudentVerified { get; set; }

        // Landlord-specific fields
        public string CompanyName { get; set; }
        public string ContactNumber { get; set; }
        public bool IsLandlordApproved { get; set; }

        // Role discriminator
        public string AccountType { get; set; } // "Student", "Landlord", or "Admin"

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DUTCRIBSDB", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}