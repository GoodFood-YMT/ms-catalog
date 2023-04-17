using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ms_catalog.Data;
using ms_catalog.Models;
using System.Text.Json;

namespace ms_catalog.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class Products : ControllerBase
    {
        
        private readonly ApiDBContext _context;
        private readonly ILogger _logger;

        //Task<List<Product>> _products;

        public Products(ILogger<Products> logger, ApiDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var products = _context.Product.ToList();
                return Ok(products);
            } 
            catch(Exception e)
            {
                return StatusCode(500, e.Message);
            }
            
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetProductById(int id)
        {
            Product? currentProduct = _context.Product.ToList().Where(c => c.Id.Equals(id)).FirstOrDefault();

            if (currentProduct == null)
            {
                return NotFound();
            }
            return Ok(currentProduct);
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
                _context.Product.Add(product);
                await _context.SaveChangesAsync();

                return Ok(product);
                //return CreatedAtAction(nameof(GetProductById), new {Id = product.Id}, product);
            }
            catch(Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<Product>> DeleteProduct(int id)
        {
            try
            {
                if(id == 0)
                {
                    return BadRequest();
                }

                Product? currentProduct = _context.Product.ToList().Where(c => c.Id.Equals(id)).FirstOrDefault();

                if (currentProduct == null)
                {
                    return BadRequest();
                }

                _context.Product.Remove(currentProduct);
                await _context.SaveChangesAsync();

                return Ok(currentProduct);

            }
            catch(Exception e)
            {
                return StatusCode(500, e.Message);
            }
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

                Product? currentProduct = _context.Product.ToList().Where(c => c.Id.Equals(id)).FirstOrDefault();

                if (currentProduct == null)
                {
                    return BadRequest();
                }

                currentProduct.Description = product.Description;
                currentProduct.Price = product.Price;
                currentProduct.TaxPercent = product.TaxPercent;
                currentProduct.SpecialPrice = product.SpecialPrice;
                currentProduct.Visible = product.Visible;
                currentProduct.Stock = product.Stock;

                currentProduct.UpdatedAt = DateTime.Now;

                _context.Product.Update(currentProduct);
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
