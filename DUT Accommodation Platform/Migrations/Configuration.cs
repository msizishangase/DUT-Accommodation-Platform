namespace DUT_Accommodation_Platform.Migrations
{
    using DUT_Accommodation_Platform.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<DUT_Accommodation_Platform.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        // In Migrations/Configuration.cs
        protected override void Seed(ApplicationDbContext context)
        {
            // Initialize role manager and user manager
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            // Create roles if they don't exist
            if (!roleManager.RoleExists("Admin"))
            {
                roleManager.Create(new IdentityRole("Admin"));
            }

            if (!roleManager.RoleExists("Landlord"))
            {
                roleManager.Create(new IdentityRole("Landlord"));
            }

            if (!roleManager.RoleExists("Student"))
            {
                roleManager.Create(new IdentityRole("Student"));
            }

            // Create admin user if it doesn't exist
            const string adminEmail = "shangasemsizi469@gmail.com";
            const string adminPassword = "@Admin123";

            if (userManager.FindByEmail(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    AccountType = "Admin",
                    EmailConfirmed = true
                };

                var createResult = userManager.Create(adminUser, adminPassword);

                if (createResult.Succeeded)
                {
                    userManager.AddToRole(adminUser.Id, "Admin");
                }
                else
                {
                    // Log errors if creation fails
                    var errors = string.Join(", ", createResult.Errors);
                    throw new Exception($"Admin user creation failed: {errors}");
                }
            }

            base.Seed(context);
        }
    }
}
