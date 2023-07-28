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
    [Route("catalog")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        
        private readonly ApiDbContext _context;
        private readonly ILogger _logger;
        private readonly IDistributedCache _redis;
        private readonly IUriService _uriService;

        public ProductsController(ILogger<ProductsController> logger, ApiDbContext context, IDistributedCache redis, IUriService uriService)
        {
            _logger = logger;
            _context = context;
            _redis = redis;
            _uriService = uriService;
        }

        [HttpGet("{RestaurantId}/products")]
        public async Task<IActionResult> GetProducts(string RestaurantId, [FromQuery] PaginationFilter filter, string? CategoryId)
        {

            List<ProductsDto> products = new();
            PagedResponse<List<ProductsDto>> pagedReponse;
            int totalRecords = 0;

            string route = Request.Path.Value!;
            PaginationFilter validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

            string key = $"restaurant:{RestaurantId}:product:all";
            string? cachedProducts = await _redis.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedProducts))
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Visible)
                    .Select(p => new ProductsDto(
                        p.Id.ToString(),
                        p.Label,
                        p.Description,
                        p.Price,
                        p.Visible,
                        p.Quantity,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.Category != null ? p.Category.Id.ToString() : null,
                        p.RestaurantId)
                    ).ToListAsync();

                await _redis.SetStringAsync(key, products.Count > 0 ? JsonConvert.SerializeObject(products) : "");

                products = products.Where(p => p.RestaurantId == RestaurantId && (CategoryId == null || p.CategoryId == CategoryId)).ToList();

                totalRecords = products.Count();

                products
                    .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();


                pagedReponse = PaginationHelper.CreatePagedReponse(products, validFilter, totalRecords, _uriService, route);

                
                return Ok(pagedReponse);
            }

            products = JsonConvert.DeserializeObject<List<ProductsDto>>(cachedProducts)
                .Where(p => p.RestaurantId == RestaurantId && (CategoryId == null || p.CategoryId == CategoryId))
                .ToList();

            totalRecords = products!.Count();

            products = products
                .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                .Take(validFilter.PageSize)
                .ToList();
            
            pagedReponse = PaginationHelper.CreatePagedReponse(products!, validFilter, totalRecords, _uriService, route);

            return Ok(pagedReponse);
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct(ProductRequestModel product)
        {
            if (product == null)
            {
                return BadRequest();
            }

            Product newProduct = new(
                product.Label, 
                product.Description,
                product.Price,
                product.Visible, 
                product.Quantity, 
                product.RestaurantId
            );

            if (product.CategoryId != null)
            {
                Category? categ = await _context.Categories.Where(c => c.Id.ToString() == product.CategoryId).FirstOrDefaultAsync();
                newProduct.Category = categ;
            }

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            ProductsDto productDto = new(
                newProduct.Id.ToString(),
                newProduct.Label,
                newProduct.Description,
                newProduct.Price,
                newProduct.Visible,
                newProduct.Quantity,
                newProduct.CreatedAt,
                newProduct.UpdatedAt,
                newProduct.Category != null ? newProduct.Category.Id.ToString() : "",
                newProduct.RestaurantId);

            await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product:all", string.Empty);

            return Ok(productDto);
        }

        [HttpGet("product/{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            string? cachedProduct = await _redis.GetStringAsync($"product:{id}");
            ProductsDto? product = null;

            if (cachedProduct == null)
            {
                product = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Id.ToString() == id)
                    .Select(p => new ProductsDto(
                        p.Id.ToString(),
                        p.Label,
                        p.Description,
                        p.Price,
                        p.Visible,
                        p.Quantity,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.Category != null ? p.Category.Id.ToString() :"",
                        p.RestaurantId)
                    ).FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound();
                }

                await _redis.SetStringAsync($"product:{id}", JsonConvert.SerializeObject(product));
                return Ok(product);
            }

            product = JsonConvert.DeserializeObject<ProductsDto>(cachedProduct);
            return Ok(product);
        }

        [HttpPut("product/{id}")]
        public async Task<IActionResult> UpdateProduct(string id, ProductRequestModelForUpdate product)
        {
            Product? currentProduct = await _context.Products.Where(c => c.Id.ToString() == id).FirstOrDefaultAsync();

            if (currentProduct == null)
            {
                return NotFound();
            }

            currentProduct.Label = product.Label;
            currentProduct.Description = product.Description;
            currentProduct.Price = product.Price;
            currentProduct.Visible = product.Visible;
            currentProduct.Quantity = product.Quantity;

            if (product.CategoryId != null)
            {
                Category? categ = await _context.Categories.Where(c => c.Id.ToString() == product.CategoryId).FirstOrDefaultAsync();
                currentProduct.Category = categ;
            }

            currentProduct.UpdatedAt = DateTime.Now;
            _context.Products.Update(currentProduct);
            await _context.SaveChangesAsync();

            ProductsDto productDto = new(
                currentProduct.Id.ToString(),
                currentProduct.Label,
                currentProduct.Description,
                currentProduct.Price,
                currentProduct.Visible,
                currentProduct.Quantity,
                currentProduct.CreatedAt,
                currentProduct.UpdatedAt,
                currentProduct.Category != null ? currentProduct.Category.Id.ToString() : "",
                currentProduct.RestaurantId);


            await _redis.SetStringAsync($"restaurant:{currentProduct.RestaurantId}:product:all", string.Empty);
            await _redis.SetStringAsync($"product:{id}", string.Empty);

            return Ok(productDto);
        }
    }
    public class ProductRequestModel
    {
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public double TaxPercent { get; set; }
        public double SpecialPrice { get; set; }
        public bool Visible { get; set; }
        public int Quantity { get; set; }
        public string? CategoryId { get; set; }
        public string RestaurantId { get; set; } = "";
    }

    public class ProductRequestModelForUpdate
    {
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public double TaxPercent { get; set; }
        public double SpecialPrice { get; set; }
        public bool Visible { get; set; }
        public int Quantity { get; set; }
        public string? CategoryId { get; set; }
    }
}
