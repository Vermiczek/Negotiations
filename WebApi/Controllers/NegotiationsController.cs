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

        /// <summary>
        /// Get all negotiations in the system
        /// </summary>
        /// <remarks>
        /// Requires admin or seller role. Returns full list of all negotiations with product and responder details.
        /// </remarks>
        /// <returns>A list of all negotiations</returns>
        [HttpGet]
        [Authorize(Policy = "RequireAdminOrSellerRole")]
        public async Task<ActionResult<IEnumerable<Negotiation>>> GetNegotiations()
        {
            return await _context.Negotiations
                .Include(n => n.Product)
                .Include(n => n.RespondedByUser)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific negotiation by ID
        /// </summary>
        /// <remarks>
        /// Authenticated users can access any negotiation. Unauthenticated users must provide client identifier or email to access their own negotiations.
        /// </remarks>
        /// <param name="id">The ID of the negotiation to retrieve</param>
        /// <returns>The negotiation details</returns>
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
        
        /// <summary>
        /// Get all negotiations for a specific client
        /// </summary>
        /// <remarks>
        /// Retrieves negotiations based on client identifier in header or email in query parameter.
        /// Either client identifier or email must be provided.
        /// </remarks>
        /// <returns>List of negotiations for the specified client</returns>
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

        /// <summary>
        /// Create a new price negotiation request
        /// </summary>
        /// <remarks>
        /// Creates a new negotiation for a product. Client identifier is taken from the request header.
        /// Client can only have one active negotiation per product.
        /// </remarks>
        /// <param name="request">The negotiation request details</param>
        /// <returns>The created negotiation</returns>
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

        /// <summary>
        /// Respond to a negotiation request
        /// </summary>
        /// <remarks>
        /// Allows admins and sellers to accept or reject a negotiation. 
        /// If rejected, the client has 7 days to propose a new price.
        /// </remarks>
        /// <param name="id">The negotiation ID</param>
        /// <param name="response">The response with accept/reject decision and optional comment</param>
        /// <returns>Status information about the negotiation response</returns>
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

        /// <summary>
        /// Propose a new price for a rejected negotiation
        /// </summary>
        /// <remarks>
        /// Allows client to propose a new price after their negotiation was rejected.
        /// Limited to 3 attempts and must be done within the deadline (7 days from rejection).
        /// Requires client identifier or email to match the negotiation.
        /// </remarks>
        /// <param name="id">The negotiation ID</param>
        /// <param name="request">The new price proposal</param>
        /// <returns>Status information about the new proposal</returns>
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

        /// <summary>
        /// Get all negotiations for a specific product
        /// </summary>
        /// <remarks>
        /// Available only to admins and sellers. Returns all negotiations for a given product ID.
        /// </remarks>
        /// <param name="productId">The ID of the product</param>
        /// <returns>List of negotiations for the product</returns>
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

    /// <summary>
    /// Request model for creating a new negotiation
    /// </summary>
    public class NegotiationCreateRequest
    {
        /// <summary>
        /// ID of the product to negotiate on
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Client's proposed price for the product
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than 0")]
        public decimal ProposedPrice { get; set; }
        
        /// <summary>
        /// Client's email for notifications and identification
        /// </summary>
        [Required]
        [EmailAddress]
        public string ClientEmail { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional client's name
        /// </summary>
        public string? ClientName { get; set; }
    }

    /// <summary>
    /// Response model for responding to a negotiation
    /// </summary>
    public class NegotiationResponse
    {
        /// <summary>
        /// Whether the seller accepts the proposed price
        /// </summary>
        public bool IsAccepted { get; set; }
        
        /// <summary>
        /// Optional comment from the seller
        /// </summary>
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Request model for proposing a new price after rejection
    /// </summary>
    public class NewPriceRequest
    {
        /// <summary>
        /// Client's new proposed price
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Proposed price must be greater than 0")]
        public decimal ProposedPrice { get; set; }
    }
}