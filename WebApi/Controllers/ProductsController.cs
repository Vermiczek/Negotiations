using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negotiations.Data;
using Negotiations.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Negotiations.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all available products
        /// </summary>
        /// <remarks>
        /// Retrieves a list of all products in the system. Publicly accessible.
        /// </remarks>
        /// <returns>A list of all products</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        /// <summary>
        /// Get a specific product by ID
        /// </summary>
        /// <remarks>
        /// Retrieves detailed information for a specific product.
        /// </remarks>
        /// <param name="id">The ID of the product to retrieve</param>
        /// <returns>The product details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        /// <remarks>
        /// Allows admins and sellers to create a new product in the system.
        /// </remarks>
        /// <param name="product">The product details to create</param>
        /// <returns>The created product with assigned ID</returns>
        [HttpPost]
        [Authorize(Policy = "RequireAdminOrSellerRole")]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            product.CreatedDate = DateTime.UtcNow;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        /// <remarks>
        /// Allows admins and sellers to update product details (name, description, and price).
        /// The creation date is preserved.
        /// </remarks>
        /// <param name="id">ID of the product to update</param>
        /// <param name="product">Updated product details</param>
        /// <returns>No content on success</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdminOrSellerRole")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        /// <remarks>
        /// Allows admins to delete a product. Only products without active negotiations can be deleted.
        /// </remarks>
        /// <param name="id">ID of the product to delete</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var hasNegotiations = await _context.Negotiations
                .AnyAsync(n => n.ProductId == id && 
                              (n.Status == NegotiationStatus.Pending || n.Status == NegotiationStatus.Accepted));
            
            if (hasNegotiations)
            {
                return BadRequest("Cannot delete product with active negotiations.");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}