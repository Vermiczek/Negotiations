using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Negotiations.Models;

namespace Negotiations.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context)
        {
            await InitializeRolesAsync(context);
            await CreateDefaultAdminIfNotExistsAsync(context);
        }
        
        private static async Task InitializeRolesAsync(ApplicationDbContext context)
        {
            await CreateDefaultSellerIfNotExistsAsync(context);
        }
        
        private static async Task CreateDefaultAdminIfNotExistsAsync(ApplicationDbContext context)
        {
            if (!await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
                if (adminRole == null)
                {
                    Console.WriteLine("Admin role not found. Cannot create default admin user.");
                    return;
                }
                
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
                
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = passwordHash,
                    FirstName = "Admin",
                    LastName = "User",
                    IsActive = true
                };
                
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                
                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();
                
                Console.WriteLine("Default admin user created.");
            }
        }
        
        private static async Task CreateDefaultSellerIfNotExistsAsync(ApplicationDbContext context)
        {
            if (!await context.Users.AnyAsync(u => u.Username == "seller"))
            {
                var sellerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "seller");
                if (sellerRole == null)
                {
                    Console.WriteLine("Seller role not found. Cannot create default seller user.");
                    return;
                }
                
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Seller123!");
                
                var sellerUser = new User
                {
                    Username = "seller",
                    Email = "seller@example.com",
                    PasswordHash = passwordHash,
                    FirstName = "Store",
                    LastName = "Employee",
                    IsActive = true
                };
                
                context.Users.Add(sellerUser);
                await context.SaveChangesAsync();
                
                context.UserRoles.Add(new UserRole
                {
                    UserId = sellerUser.Id,
                    RoleId = sellerRole.Id
                });
                await context.SaveChangesAsync();
                
                Console.WriteLine("Default seller user created.");
            }
        }
    }
}