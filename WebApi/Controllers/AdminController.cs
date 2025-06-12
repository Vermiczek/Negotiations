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

        /// <summary>
        /// Get system dashboard statistics
        /// </summary>
        /// <remarks>
        /// Provides overview statistics about users, roles, and system information.
        /// Only accessible to administrators.
        /// </remarks>
        /// <returns>Dashboard statistics including user counts, role distribution, and system info</returns>
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

        /// <summary>
        /// Get all users with their assigned roles
        /// </summary>
        /// <remarks>
        /// Retrieves a complete list of users in the system along with their role assignments.
        /// Only accessible to administrators.
        /// </remarks>
        /// <returns>List of users with their profile information and roles</returns>
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

        /// <summary>
        /// Toggle a user's active status
        /// </summary>
        /// <remarks>
        /// Allows administrators to activate or deactivate user accounts.
        /// If the user is currently active, they will be set to inactive and vice versa.
        /// </remarks>
        /// <param name="id">The ID of the user whose status will be toggled</param>
        /// <returns>Confirmation message with the user's new status</returns>
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

        /// <summary>
        /// Create a new role
        /// </summary>
        /// <remarks>
        /// Allows administrators to create new roles in the system.
        /// Role names must be unique (case-insensitive).
        /// </remarks>
        /// <param name="request">Request containing the name for the new role</param>
        /// <returns>The created role with its assigned ID</returns>
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

        /// <summary>
        /// Get a specific role by ID
        /// </summary>
        /// <remarks>
        /// Retrieves detailed information for a specific role.
        /// </remarks>
        /// <param name="id">The ID of the role to retrieve</param>
        /// <returns>The role details</returns>
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

    /// <summary>
    /// Request model for creating a new role
    /// </summary>
    public class CreateRoleRequest
    {
        /// <summary>
        /// Name of the role to create
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}