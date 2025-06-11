using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negotiations.Data;
using Negotiations.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Negotiations.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userCount = await _context.Users.CountAsync();
            var activeUserCount = await _context.Users.Where(u => u.IsActive).CountAsync();
            var roleStats = await _context.Roles
                .Select(r => new
                {
                    roleName = r.Name,
                    userCount = r.UserRoles.Count
                })
                .ToListAsync();

            return Ok(new
            {
                systemStats = new
                {
                    totalUsers = userCount,
                    activeUsers = activeUserCount,
                    inactiveUsers = userCount - activeUserCount,
                    roles = roleStats
                },
                serverInfo = new
                {
                    version = "1.0.0",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    startTime = DateTime.UtcNow.AddHours(-24) 
                }
            });
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllUsersWithRoles()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.CreatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User {user.Username} is now {(user.IsActive ? "active" : "inactive")}" });
        }

        [HttpPost("roles")]
        public async Task<ActionResult<Role>> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (await _context.Roles.AnyAsync(r => r.Name.ToLower() == request.Name.ToLower()))
            {
                return BadRequest($"Role with name '{request.Name}' already exists");
            }

            var role = new Role { Name = request.Name };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }

        [HttpGet("roles/{id}")]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return role;
        }
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}