using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestApi.Data;
using RestApi.Models;
using System.Linq;
using System.Diagnostics;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Product")]
    public class ProductController : ControllerBase
    {
        // Servis için bir ActivitySource tanımla
        private static readonly ActivitySource ActivitySource = new("RestApi.ProductController");

        private readonly ApplicationDbContext _context;

        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Create(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
            _logger.LogInformation("Product created: {@Product}", product);
            return Ok(product);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Product product)
        {
            var existingProduct = _context.Products.Find(id);
            if (existingProduct == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", id);
                return NotFound();
            }
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            _context.SaveChanges();
            _logger.LogInformation("Product updated: {@Product}", existingProduct);
            return Ok(existingProduct);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", id);
                return NotFound();
            }
            _context.Products.Remove(product);
            _context.SaveChanges();
            _logger.LogInformation("Product deleted: {ProductId}", id);
            return Ok();
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            // Özel span başlat
            using var activity = ActivitySource.StartActivity("GetAll");
            // Attribute ekle
            activity?.SetTag("product.query.type", "all");

            // Veritabanı sorgusu için alt span
            using var dbActivity = ActivitySource.StartActivity("DatabaseQuery");
            dbActivity?.SetTag("db.system", "sqlite");
            dbActivity?.SetTag("db.statement", "SELECT * FROM Products");

            var products = _context.Products.ToList();
            Util.Util.ls.Add(products);
            _logger.LogInformation("Retrieved {ProductCount} products", products.Count);

            dbActivity?.SetTag("db.result.count", products.Count);
            activity?.SetTag("product.result.count", products.Count);
            return Ok(products);
        }
    }
}