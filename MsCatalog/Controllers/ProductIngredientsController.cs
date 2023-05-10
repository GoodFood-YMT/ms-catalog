using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MsCatalog.Data;
using MsCatalog.Helpers;
using MsCatalog.Models;
using MsCatalog.Models.Filters;
using MsCatalog.Models.Wrappers;
using MsCatalog.Services.UriService;
using Newtonsoft.Json;

namespace MsCatalog.Controllers
{
    [Route("api/product/{productId}/ingredient")]
    [ApiController]
    public class ProductIngredientsController : Controller
    {

        private readonly ApiDbContext _context;
        private readonly ILogger _logger;
        private readonly IDistributedCache _redis;
        private readonly IUriService _uriService;

        public ProductIngredientsController(ILogger<Category> logger, ApiDbContext context, IDistributedCache redis, IUriService uriService)
        {
            _logger = logger;
            _context = context;
            _redis = redis;
            _uriService = uriService;
        }

        [HttpGet]
        public async Task<IActionResult> GetIngredientsByProduct(int productId, [FromQuery] PaginationFilter filter)
        {
            string key = $"product-{productId}-ingredients";
            string? cachedProductIngredients = await _redis.GetStringAsync(key);
            List<ProductsIngredientsDto>? productIngredients = new List<ProductsIngredientsDto>();

            PagedResponse<List<ProductsIngredientsDto>> pagedReponse;
            int totalRecords = 0;
            string route = Request.Path.Value!;
            PaginationFilter validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

            if (string.IsNullOrEmpty(cachedProductIngredients))
            {
                productIngredients = await _context.ProductsIngredients
                    .Where((p) => p.ProductId == productId)
                    .Select(p => new ProductsIngredientsDto { ProductId = p.ProductId, IngredientId = p.IngredientId, Quantity = p.Quantity })
                    .ToListAsync();

                totalRecords = productIngredients.Count;

                productIngredients
                    .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();

                await _redis.SetStringAsync(key, JsonConvert.SerializeObject(productIngredients));

                pagedReponse = PaginationHelper.CreatePagedReponse(productIngredients, validFilter, totalRecords, _uriService, route);
                return Ok(productIngredients);
            }

            productIngredients = JsonConvert.DeserializeObject<List<ProductsIngredientsDto>>(cachedProductIngredients)
                    .Where((p) => p.ProductId == productId)
                    .Select(p => new ProductsIngredientsDto { ProductId = p.ProductId, IngredientId = p.IngredientId, Quantity = p.Quantity })
                    .ToList();

            totalRecords = productIngredients.Count;

            productIngredients
                .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                .Take(validFilter.PageSize)
                .ToList();

            pagedReponse = PaginationHelper.CreatePagedReponse(productIngredients, validFilter, totalRecords, _uriService, route);

            return Ok(pagedReponse);
        }

        [HttpGet("{ingredientId}")]
        public async Task<IActionResult> GetIngredientByProduct(int productId, int ingredientId)
        {
            string key = $"product-{productId}-ingredient-{ingredientId}";
            string? cachedProductIngredient = await _redis.GetStringAsync(key);
            ProductsIngredientsDto? productIngredient = null;

            if (string.IsNullOrEmpty(cachedProductIngredient))
            {
                productIngredient = await _context.ProductsIngredients
                    .Where((p) => p.ProductId == productId && p.IngredientId == ingredientId)
                    .Select(p => new ProductsIngredientsDto { ProductId = p.ProductId, IngredientId = p.IngredientId, Quantity = p.Quantity })
                    .FirstOrDefaultAsync();

                await _redis.SetStringAsync(key, JsonConvert.SerializeObject(productIngredient));
                return Ok(productIngredient);
            }

            productIngredient = JsonConvert.DeserializeObject<ProductsIngredientsDto>(cachedProductIngredient);
            return Ok(productIngredient);
        }

        [HttpPost("{ingredientId}")]
        public async Task<IActionResult> AddIngredientByProduct(int productId, int ingredientId, int quantity)
        {
            Product? currentProduct = await _context.Products.FindAsync(productId);
            Ingredient? currentIngredient = await _context.Ingredients.FindAsync(ingredientId);
            if (currentProduct != null &&  currentIngredient != null)
            {

                if(await _context.ProductsIngredients.Where((p) => p.ProductId == productId &&  p.IngredientId == ingredientId).FirstOrDefaultAsync() == null)
                {
                    ProductsIngredients productIngredient = new ProductsIngredients();
                    productIngredient.ProductId = productId;
                    productIngredient.Product = currentProduct;
                    productIngredient.Ingredient = currentIngredient;
                    productIngredient.IngredientId = ingredientId;
                    productIngredient.Quantity = quantity;

                    await _context.ProductsIngredients.AddAsync(productIngredient);
                    await _context.SaveChangesAsync();

                    ProductsIngredientsDto result = new ProductsIngredientsDto { IngredientId= ingredientId, ProductId = productId, Quantity = quantity };

                    await _redis.SetStringAsync($"product-{productId}-ingredients", "");
                    await _redis.SetStringAsync($"product-{productId}-ingredient-{ingredientId}", "");

                    return Ok(result);
                }
            }
            return BadRequest();
        }

        [HttpPut("{ingredientId}")]
        public async Task<IActionResult> SetIngredientProductQuantity(int productId, int ingredientId, [FromBody] QuantityRequiredModel request)
        {
            ProductsIngredients? currentProductIngredients = await _context.ProductsIngredients.Where((p) => p.ProductId == productId && p.IngredientId == ingredientId).FirstOrDefaultAsync();
            if(currentProductIngredients != null)
            {
                currentProductIngredients.Quantity = request.Quantity;
                await _context.SaveChangesAsync();

                ProductsIngredientsDto result = new ProductsIngredientsDto { IngredientId = ingredientId, ProductId = productId, Quantity = request.Quantity };

                await _redis.SetStringAsync($"product-{productId}-ingredients", "");
                await _redis.SetStringAsync($"product-{productId}-ingredient-{ingredientId}", "");

                return Ok(result);
            }

            return BadRequest();
        }

        [HttpDelete("{ingredientId}")]
        public async Task<IActionResult> Delete(int productId, int ingredientId)
        {
            ProductsIngredients? currentProductIngredients = await _context.ProductsIngredients.Where((p) => p.ProductId == productId && p.IngredientId == ingredientId).FirstOrDefaultAsync();
            if (currentProductIngredients != null)
            {
                _context.ProductsIngredients.Remove(currentProductIngredients);
                await _context.SaveChangesAsync();

                await _redis.SetStringAsync($"product-{productId}-ingredients", "");
                await _redis.SetStringAsync($"product-{productId}-ingredient-{ingredientId}", "");

                ProductsIngredientsDto result = new ProductsIngredientsDto { IngredientId = ingredientId, ProductId = productId };

                return Ok(result);
            }

            return BadRequest();
        }
    }

    public class QuantityRequiredModel
    {
        public int Quantity { get; set; }
    }
}
