using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Negotiations.Data;
using Negotiations.Models;
using Negotiations.Models.DTOs;
using BC = BCrypt.Net.BCrypt;

namespace Negotiations.Services
{
    public interface IUserService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, string role);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<User?> GetByIdAsync(int id);
        Task<List<User>> GetAllAsync();
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public UserService(ApplicationDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string role)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                throw new Exception("Username is already taken");
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new Exception("Email is already registered");
            }

            var passwordHash = BC.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role) ?? 
                              await _context.Roles.FirstOrDefaultAsync(r => r.Name == "seller");

            if (roleEntity != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleEntity.Id
                });
                await _context.SaveChangesAsync();
            }

            var roles = new List<string> { roleEntity?.Name ?? "seller" };
            var token = _tokenService.GenerateToken(user, roles);

            return new AuthResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = token,
                Roles = roles
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return null;
            }

            if (!BC.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var token = _tokenService.GenerateToken(user, roles);

            return new AuthResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = token,
                Roles = roles
            };
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }
    }
}