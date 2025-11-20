using BookShopMVC.Constants;
using Microsoft.AspNetCore.Identity;

namespace BookShopMVC.Models
{
    public class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider service)
        {
            var userMgr = service.GetService<UserManager<IdentityUser>>();
            var roleMgr = service.GetService<RoleManager<IdentityRole>>();

            //Adding roles to database
            //Admin
            await roleMgr.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
            //User
            await roleMgr.CreateAsync(new IdentityRole(Roles.User.ToString()));

            //creating Amin
            var admin = new IdentityUser
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                EmailConfirmed = true
            };

            var userInDb = await userMgr.FindByEmailAsync(admin.Email);
            if (userInDb is null)
            {
                await userMgr.CreateAsync(admin, "Admin@123");
                await userMgr.AddToRoleAsync(admin, Roles.Admin.ToString());
            }
        }
    }
}
