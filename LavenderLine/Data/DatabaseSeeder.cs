using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LavenderLine.Data
{
    public class DatabaseSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _Iconfig;

        public DatabaseSeeder(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IConfiguration iconfig)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _Iconfig = iconfig;
        }

        public async Task SeedAsync()
        {
            string[] roleNames = { Roles.Admin, Roles.Customer, Roles.Manager };

            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminEmail = _Iconfig["EmailSettings:AdminEmail"];
            var adminPassword = _Iconfig["EmailSettings:AdminPassword"];
            var adminPhoneNumber = _Iconfig["EmailSettings:AdminPhoneNumber"];
            var FullName = "AvenderLine";
            var Area = "Doha";
            var AddressLine = "Building no.x, st.xxx ALKRTYAIT";


            var fullAddress = $"Area: {Area}, Address: {AddressLine}" ;

            var adminUser = await _userManager.FindByNameAsync(adminEmail);


            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Email = adminEmail,
                    UserName = adminEmail,
                    FullName = FullName,
                    Area = Area,
                    StreetAddress = AddressLine,
                    PhoneNumber = adminPhoneNumber,
                    IsSubscribed = false,
                    EmailConfirmed = true,
                    Role = Roles.Admin,
                    CreatedAt = QatarDateTime.Now
                };
                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, Roles.Admin);

                    await _userManager.AddClaimAsync(adminUser, new Claim("FullName", FullName));
                    await _userManager.AddClaimAsync(adminUser, new Claim("Address", fullAddress));
                }
            }

        }
    }
}
