using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negotiations.Data;
using Negotiations.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Negotiations.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NegotiationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NegotiationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdminOrSellerRole")]
        public async Task<ActionResult<IEnumerable<Negotiation>>> GetNegotiations()
        {
            return await _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.RespondedByUser)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Negotiation>> GetNegotiation(int id)
        {
            var negotiation = await _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.RespondedByUser)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (negotiation == null)
            {
                return NotFound();
            }

            // If user is not authenticated, they must be the client
            if (User.Identity?.IsAuthenticated != true)
            {
                string clientIdentifier = Request.Headers["Client-Identifier"].ToString();
                string? clientEmail = Request.Query["email"].ToString();
                
                bool isClientIdentifierMatch = !string.IsNullOrEmpty(negotiation.ClientIdentifier) && 
                                              negotiation.ClientIdentifier == clientIdentifier;
                bool isClientEmailMatch = !string.IsNullOrEmpty(clientEmail) && 
                                         clientEmail == negotiation.ClientEmail;
                
                if (!isClientIdentifierMatch && !isClientEmailMatch)
                {
                    return Forbid();
                }
            }

            return negotiation;
        }
        
        [HttpGet("client")]
        public async Task<ActionResult<IEnumerable<Negotiation>>> GetClientNegotiations()
        {
            string clientIdentifier = Request.Headers["Client-Identifier"].ToString();
            string? clientEmail = Request.Query["email"].ToString();
            
            if (string.IsNullOrEmpty(clientIdentifier) && string.IsNullOrEmpty(clientEmail))
            {
                return BadRequest("Either client identifier or email is required");
            }

            var query = _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.RespondedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(clientIdentifier))
            {
                query = query.Where(n => n.ClientIdentifier == clientIdentifier);
            }
            
            if (!string.IsNullOrEmpty(clientEmail))
            {
                query = query.Where(n => n.ClientEmail == clientEmail);
            }

            return await query.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Negotiation>> CreateNegotiation(NegotiationCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return BadRequest("Product not found");
            }
            
            if (request.ProposedPrice <= 0)
            {
                return BadRequest("Proposed price must be greater than 0");
            }

            string clientIdentifier = Request.Headers["Client-Identifier"].ToString();
            
            string clientEmail = request.ClientEmail;

            var existingNegotiationQuery = _context.Negotiations
                .Where(n => n.ProductId == request.ProductId &&
                           n.Status == NegotiationStatus.Pending);
                
            if (!string.IsNullOrEmpty(clientIdentifier))
            {
                existingNegotiationQuery = existingNegotiationQuery.Where(n => 
                    n.ClientIdentifier == clientIdentifier || n.ClientEmail == clientEmail);
            }
            else
            {
                existingNegotiationQuery = existingNegotiationQuery.Where(n => n.ClientEmail == clientEmail);
            }

            var existingNegotiation = await existingNegotiationQuery.FirstOrDefaultAsync();

            if (existingNegotiation != null)
            {
                return BadRequest("You already have an active negotiation for this product");
            }

            var negotiation = new Negotiation
            {
                ProductId = request.ProductId,
                ProposedPrice = request.ProposedPrice,
                ClientIdentifier = clientIdentifier,
                ClientEmail = request.ClientEmail,  
                ClientName = request.ClientName,
                Status = NegotiationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                AttemptCount = 1
            };

            _context.Negotiations.Add(negotiation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNegotiation), new { id = negotiation.Id }, negotiation);
        }

        [HttpPost("{id}/respond")]
        [Authorize(Policy = "RequireAdminOrSellerRole")]
        public async Task<IActionResult> RespondToNegotiation(int id, NegotiationResponse response)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var negotiation = await _context.Negotiations.FindAsync(id);
            if (negotiation == null)
            {
                return NotFound();
            }

            if (negotiation.Status != NegotiationStatus.Pending)
            {
                return BadRequest("This negotiation is no longer pending");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int respondedByUserId))
            {
                return BadRequest("User ID not found in token claims");
            }

            negotiation.Status = response.IsAccepted ? NegotiationStatus.Accepted : NegotiationStatus.Rejected;
            negotiation.ResponseDate = DateTime.UtcNow;
            negotiation.RespondedByUserId = respondedByUserId;
            negotiation.ResponseComment = response.Comment;

            if (!response.IsAccepted)
            {
                negotiation.NextAttemptDeadline = DateTime.UtcNow.AddDays(7);
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                status = negotiation.Status.ToString(),
                message = response.IsAccepted ? "Negotiation accepted" : "Negotiation rejected. Client has 7 days to propose a new price."
            });
        }

        [HttpPost("{id}/propose-new-price")]
        public async Task<IActionResult> ProposeNewPrice(int id, NewPriceRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var negotiation = await _context.Negotiations.FindAsync(id);
            if (negotiation == null)
            {
                return NotFound();
            }

            string clientIdentifier = Request.Headers["Client-Identifier"].ToString();
            string? clientEmail = Request.Query["email"].ToString();
                
            bool isClientIdentifierMatch = !string.IsNullOrEmpty(clientIdentifier) && 
                                          !string.IsNullOrEmpty(negotiation.ClientIdentifier) && 
                                          negotiation.ClientIdentifier == clientIdentifier;
            bool isClientEmailMatch = !string.IsNullOrEmpty(clientEmail) && 
                                     clientEmail == negotiation.ClientEmail;
                
            if (!isClientIdentifierMatch && !isClientEmailMatch)
            {
                return Forbid("You are not authorized to access this negotiation");
            }

            if (negotiation.Status != NegotiationStatus.Rejected)
            {
                return BadRequest("Can only propose a new price for rejected negotiations");
            }

            if (negotiation.NextAttemptDeadline.HasValue && negotiation.NextAttemptDeadline < DateTime.UtcNow)
            {
                negotiation.Status = NegotiationStatus.Cancelled;
                await _context.SaveChangesAsync();
                return BadRequest("The deadline for this negotiation has passed");
            }

            if (negotiation.AttemptCount >= 3)
            {
                negotiation.Status = NegotiationStatus.Cancelled;
                await _context.SaveChangesAsync();
                return BadRequest("Maximum number of negotiation attempts (3) has been reached");
            }

            if (request.ProposedPrice <= 0)
            {
                return BadRequest("Proposed price must be greater than 0");
            }

            negotiation.ProposedPrice = request.ProposedPrice;
            negotiation.Status = NegotiationStatus.Pending;
            negotiation.ResponseDate = null;
            negotiation.NextAttemptDeadline = null;
            negotiation.AttemptCount++;
            negotiation.RespondedByUserId = null;
            negotiation.ResponseComment = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "New price proposed successfully", attemptCount = negotiation.AttemptCount });
        }

        [HttpGet("product/{productId}")]
        [Authorize(Policy = "RequireAdminOrSellerRole")]
        public async Task<ActionResult<IEnumerable<Negotiation>>> GetNegotiationsForProduct(int productId)
        {
            return await _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.RespondedByUser)
                .Where(n => n.ProductId == productId)
                .ToListAsync();
        }
    }

    public class NegotiationCreateRequest
    {
        public int ProductId { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than 0")]
        public decimal ProposedPrice { get; set; }
        
        [Required]
        [EmailAddress]
        public string ClientEmail { get; set; } = string.Empty;
        
        public string? ClientName { get; set; }
    }

    public class NegotiationResponse
    {
        public bool IsAccepted { get; set; }
        public string? Comment { get; set; }
    }

    public class NewPriceRequest
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than 0")]
        public decimal ProposedPrice { get; set; }
    }
}