using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negotiations.Data;
using Negotiations.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Negotiations.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            return role;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] UserRoleRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return NotFound($"User with ID {request.UserId} not found");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower());
            if (role == null)
            {
                return NotFound($"Role {request.RoleName} not found");
            }

            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id);

            if (existingUserRole != null)
            {
                return BadRequest($"User already has role {request.RoleName}");
            }

            _context.UserRoles.Add(new UserRole
            {
                UserId = request.UserId,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Role {request.RoleName} assigned to user {request.UserId}" });
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveRole([FromBody] UserRoleRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return NotFound($"User with ID {request.UserId} not found");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower());
            if (role == null)
            {
                return NotFound($"Role {request.RoleName} not found");
            }

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id);

            if (userRole == null)
            {
                return BadRequest($"User does not have role {request.RoleName}");
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Role {request.RoleName} removed from user {request.UserId}" });
        }
        
        [HttpGet("{roleName}/users")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersWithRole(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
            if (role == null)
            {
                return NotFound($"Role {roleName} not found");
            }

            var usersWithRole = await _context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.UserRoles.Any(ur => ur.RoleId == role.Id))
                .ToListAsync();

            return Ok(usersWithRole);
        }
    }

    public class UserRoleRequest
    {
        public int UserId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}