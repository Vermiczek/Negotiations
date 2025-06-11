using System.Collections.Generic;

namespace Negotiations.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Navigation property
        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}