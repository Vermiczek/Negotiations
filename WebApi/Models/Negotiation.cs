using System;
using System.ComponentModel.DataAnnotations;

namespace Negotiations.Models
{
    public enum NegotiationStatus
    {
        Pending,
        Accepted,
        Rejected,
        Cancelled
    }

    public class Negotiation
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than 0")]
        public decimal ProposedPrice { get; set; }
        
        public string? ClientIdentifier { get; set; }
        
        [Required]
        [EmailAddress]
        public string ClientEmail { get; set; } = string.Empty;
        
        public string? ClientName { get; set; }
        
        public int? RespondedByUserId { get; set; }
        public User? RespondedByUser { get; set; } 
        
        public NegotiationStatus Status { get; set; } = NegotiationStatus.Pending;
        
        public int AttemptCount { get; set; } = 1; 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ResponseDate { get; set; }
        
        public DateTime? NextAttemptDeadline { get; set; }
        
        public string? ResponseComment { get; set; }
    }
}