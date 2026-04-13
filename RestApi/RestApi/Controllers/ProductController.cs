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

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Create(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
            return Ok(product);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Product product)
        {
            var existingProduct = _context.Products.Find(id);
            if (existingProduct == null)
            {
                return NotFound();
            }
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            _context.SaveChanges();
            return Ok(existingProduct);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            _context.Products.Remove(product);
            _context.SaveChanges();
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

            dbActivity?.SetTag("db.result.count", products.Count);
            activity?.SetTag("product.result.count", products.Count);
            return Ok(products);
        }
    }
}