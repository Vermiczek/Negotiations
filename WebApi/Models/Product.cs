using System;
using System.ComponentModel.DataAnnotations;

namespace Negotiations.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Product name is required")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Product description is required")]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}