﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MsCatalog.Data;
using MsCatalog.Helpers;
using MsCatalog.Models;
using MsCatalog.Models.Filters;
using MsCatalog.Models.Wrappers;
using MsCatalog.Services;
using MsCatalog.Services.UriService;
using Newtonsoft.Json;

namespace MsCatalog.Controllers
{
    [Route("catalog/products/{productId}/ingredients")]
    [ApiController]
    public class ProductIngredientsController : Controller
    {

        private readonly ApiDbContext _context;
        private readonly ILogger _logger;
        private readonly IDistributedCache _redis;
        private readonly IUriService _uriService;
        private readonly StockService _stockService;

        public ProductIngredientsController(ILogger<Category> logger, ApiDbContext context, IDistributedCache redis, IUriService uriService, StockService stockService)
        {
            _logger = logger;
            _context = context;
            _redis = redis;
            _uriService = uriService;
            _stockService = stockService;
        }

        [HttpGet]
        public async Task<IActionResult> GetIngredientsByProduct(string productId)
        {
            string RestaurantID = Request.Headers["RestaurantID"].ToString();
            if (string.IsNullOrEmpty(RestaurantID))
            {
                return BadRequest();
            }
            string cachedProductIngredients = await _redis.GetStringAsync($"product:{productId}:ingredient:all");
            List<ProductsIngredientsDto>? productIngredients = new List<ProductsIngredientsDto>();

            if (string.IsNullOrEmpty(cachedProductIngredients))
            {
                productIngredients = await _context.ProductsIngredients
                    .Include(p => p.Product)
                    .Include(p => p.Ingredient)
                    .Where(p => p.ProductId == productId && p.Product.RestaurantId == RestaurantID)                
                    .Select(p => new ProductsIngredientsDto { 
                        ProductId = p.ProductId, 
                        IngredientId = p.IngredientId, 
                        Quantity = p.Quantity,
                        Name = p.Ingredient.Name,
                    }).ToListAsync();

                await _redis.SetStringAsync($"product:{productId}:ingredient:all", JsonConvert.SerializeObject(productIngredients));

                return Ok(new
                {
                    data = productIngredients
                });
            }

            productIngredients = JsonConvert.DeserializeObject<List<ProductsIngredientsDto>>(cachedProductIngredients)
                    .Select(p => new ProductsIngredientsDto { ProductId = p.ProductId, IngredientId = p.IngredientId, Quantity = p.Quantity, Name = p.Name })
                    .ToList();

            return Ok(new
            {
                data = productIngredients
            });
        }

        [HttpGet("{ingredientId}")]
        public async Task<IActionResult> GetIngredientByProduct(string productId, string ingredientId)
        {
            string key = $"product:{productId}:ingredient:{ingredientId}";
            string? cachedProductIngredient = await _redis.GetStringAsync(key);
            ProductsIngredientsDto? productIngredient = null;

            if (string.IsNullOrEmpty(cachedProductIngredient))
            {
                productIngredient = await _context.ProductsIngredients
                    .Where((p) => p.ProductId == productId && p.IngredientId == ingredientId)
                    .Include(p => p.Ingredient)
                    .Select(p => new ProductsIngredientsDto { ProductId = p.ProductId, IngredientId = p.IngredientId, Quantity = p.Quantity, Name = p.Ingredient.Name })
                    .FirstOrDefaultAsync();

                await _redis.SetStringAsync(key, JsonConvert.SerializeObject(productIngredient));
                return Ok(productIngredient);
            }

            productIngredient = JsonConvert.DeserializeObject<ProductsIngredientsDto>(cachedProductIngredient);
            return Ok(productIngredient);
        }

        [HttpPost("{ingredientId}")]
        public async Task<IActionResult> AddIngredientByProduct(string productId, string ingredientId, [FromBody] QuantityRequiredModel request)
        {

            Product? currentProduct = await _context.Products.Where(p => p.Id.ToString() == productId).FirstOrDefaultAsync();
            Ingredient? currentIngredient = await _context.Ingredients.Where(i => i.Id.ToString() == ingredientId).FirstOrDefaultAsync();
            if (currentProduct != null &&  currentIngredient != null)
            {

                if(await _context.ProductsIngredients.AnyAsync(p => p.ProductId == productId &&  p.IngredientId == ingredientId) == false)
                {
                    ProductsIngredients productIngredient = new ProductsIngredients();
                    productIngredient.ProductId = productId;
                    productIngredient.Product = currentProduct;
                    productIngredient.Ingredient = currentIngredient;
                    productIngredient.IngredientId = ingredientId;
                    productIngredient.Quantity = request.Quantity;

                    await _context.ProductsIngredients.AddAsync(productIngredient);
                    await _context.SaveChangesAsync();

                    ProductsIngredientsDto result = new ProductsIngredientsDto { 
                        IngredientId= ingredientId, 
                        ProductId = productId, 
                        Quantity = request.Quantity, 
                        Name = currentIngredient.Name 
                    };

                    await _redis.SetStringAsync($"product:{productId}:ingredient:all", "");

                    await _stockService.UpdateProductStock(productId, _context);

                    return Ok(result);
                }
            }
            return NotFound();
        }

        [HttpPut("{ingredientId}")]
        public async Task<IActionResult> UpdateIngredientProduct(string productId, string ingredientId, [FromBody] QuantityRequiredModel request)
        {
            
            if(await _context.ProductsIngredients.AnyAsync((p) => p.ProductId == productId && p.IngredientId == ingredientId))
            {
                ProductsIngredients? currentProductIngredients = await _context.ProductsIngredients
                    .Where((p) => p.ProductId == productId && p.IngredientId == ingredientId)
                    .Include(p => p.Ingredient)
                    .FirstOrDefaultAsync();
                currentProductIngredients!.Quantity = request.Quantity;
                await _context.SaveChangesAsync();

                ProductsIngredientsDto result = new ProductsIngredientsDto { 
                    IngredientId = ingredientId, 
                    ProductId = productId, 
                    Quantity = request.Quantity, 
                    Name = currentProductIngredients.Ingredient.Name
                };

                await _redis.SetStringAsync($"product:{productId}:ingredient:all", "");
                await _redis.SetStringAsync($"product:{productId}:ingredient:{ingredientId}", "");
                await _stockService.UpdateProductStock(productId, _context);         

                return Ok(result);
            }

            return NotFound();
        }

        [HttpDelete("{ingredientId}")]
        public async Task<IActionResult> Delete(string productId, string ingredientId)
        {
            ProductsIngredients? currentProductIngredients = await _context.ProductsIngredients
                    .Where((p) => p.ProductId == productId && p.IngredientId == ingredientId)
                    .Include(p => p.Ingredient)
                    .FirstOrDefaultAsync();

            if (currentProductIngredients == null)
            {
                return NotFound();
            }
            _context.ProductsIngredients.Remove(currentProductIngredients!);
            await _context.SaveChangesAsync();

            await _redis.SetStringAsync($"product:{productId}:ingredient:all", "");
            await _redis.SetStringAsync($"product:{productId}:ingredient:{ingredientId}", "");
            await _stockService.UpdateProductStock(productId, _context);

            ProductsIngredientsDto result = new ProductsIngredientsDto
            {
                IngredientId = ingredientId,
                ProductId = productId,
                Name = currentProductIngredients.Ingredient.Name
            };
            return Ok(result);
        }
    }

    public class QuantityRequiredModel
    {
        public int Quantity { get; set; }
    }
}
