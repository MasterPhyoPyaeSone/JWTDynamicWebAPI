using JWTDynamicRBACWebAPI.Database.AppDbContextModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JWTDynamicRBACWebAPI.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // DTO for create/update
        public class ProductDto
        {
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string? Description { get; set; }
        }

        // GET: api/Product — View_Product permission required
        [HttpGet]
        [Authorize(Policy = "View_Product")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Select(p => new { p.Id, p.Name, p.Price, p.Description })
                .ToListAsync();
            return Ok(products);
        }

        // GET: api/Product/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "View_Product")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { Message = "Product not found." });
            return Ok(product);
        }

        // POST: api/Product — Create_Product permission required
        [HttpPost]
        [Authorize(Policy = "Create_Product")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // PUT: api/Product/{id} — Update_Product permission required
        [HttpPut("{id}")]
        [Authorize(Policy = "Edit_Product")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { Message = "Product not found." });

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        // DELETE: api/Product/{id} — Delete_Product permission required
        [HttpDelete("{id}")]
        [Authorize(Policy = "Delete_Product")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { Message = "Product not found." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Product '{product.Name}' deleted successfully." });
        }
    }
}