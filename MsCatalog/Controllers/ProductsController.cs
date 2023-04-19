using Microsoft.AspNetCore.Mvc;
using MsCatalog.Data;
using MsCatalog.Models;

namespace MsCatalog.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        
        private readonly ApiDbContext _context;
        private readonly ILogger _logger;

        public ProductsController(ILogger<ProductsController> logger, ApiDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var products = _context.Products.ToList();
                return Ok(products);
            } 
            catch(Exception e)
            {
                return StatusCode(500, e.Message);
            }
            
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {       
            try
            {
                if (product == null)
                {
                    return BadRequest();
                }
                product.CreatedAt = DateTime.Now;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(product);
            }
            catch(Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public ActionResult GetProductById(int id)
        {
            Product? currentProduct = _context.Products.ToList().Where(c => c.Id.Equals(id)).FirstOrDefault();

            if (currentProduct == null)
            {
                return NotFound();
            }
            return Ok(currentProduct);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(int id, Product product)
        {
            try
            {
                if (id == 0)
                {
                    return BadRequest();
                }

                Product? currentProduct = _context.Products.ToList().Where(c => c.Id.Equals(id)).FirstOrDefault();

                if (currentProduct == null)
                {
                    return BadRequest();
                }

                currentProduct.Description = product.Description;
                currentProduct.Price = product.Price;
                currentProduct.TaxPercent = product.TaxPercent;
                currentProduct.SpecialPrice = product.SpecialPrice;
                currentProduct.Visible = product.Visible;

                currentProduct.UpdatedAt = DateTime.Now;

                _context.Products.Update(currentProduct);
                await _context.SaveChangesAsync();

                return Ok(currentProduct);       
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
