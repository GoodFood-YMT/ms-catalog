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
    [Route("catalog/{RestaurantId}/products")]
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

        [HttpGet]
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
                        p.TaxPercent,
                        p.SpecialPrice,
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

        [HttpPost]
        public async Task<IActionResult> CreateProduct(string RestaurantId, ProductRequestModel product)
        {
            if (product == null)
            {
                return BadRequest();
            }

            Product newProduct = new(
                product.Label, 
                product.Description, 
                product.Price, product.TaxPercent, 
                product.SpecialPrice, product.Visible, 
                product.Quantity, 
                RestaurantId
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
                newProduct.TaxPercent,
                newProduct.SpecialPrice,
                newProduct.Visible,
                newProduct.Quantity,
                newProduct.CreatedAt,
                newProduct.UpdatedAt,
                newProduct.Category != null ? newProduct.Category.Id.ToString() : "",
                newProduct.RestaurantId);

            await _redis.SetStringAsync($"restaurant:{RestaurantId}:product:all", "");

            return Ok(productDto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string RestaurantId, string id)
        {
            string? cachedProduct = await _redis.GetStringAsync($"restaurant:{RestaurantId}:product:{id}");
            ProductsDto? product = null;

            if (cachedProduct == null)
            {
                product = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Id.ToString() == id && p.RestaurantId == RestaurantId)
                    .Select(p => new ProductsDto(
                        p.Id.ToString(),
                        p.Label,
                        p.Description,
                        p.Price,
                        p.TaxPercent,
                        p.SpecialPrice,
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

                await _redis.SetStringAsync($"restaurant:{RestaurantId}:product:{id}", JsonConvert.SerializeObject(product));
                return Ok(product);
            }

            product = JsonConvert.DeserializeObject<ProductsDto>(cachedProduct);
            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string RestaurantId, string id, ProductRequestModel product)
        {
            Product? currentProduct = await _context.Products.Where(c => c.Id.ToString() == id && c.RestaurantId == RestaurantId).FirstOrDefaultAsync();

            if (currentProduct == null)
            {
                return NotFound();
            }

            currentProduct.Label = product.Label;
            currentProduct.Description = product.Description;
            currentProduct.Price = product.Price;
            currentProduct.TaxPercent = product.TaxPercent;
            currentProduct.SpecialPrice = product.SpecialPrice;
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
                currentProduct.TaxPercent,
                currentProduct.SpecialPrice,
                currentProduct.Visible,
                currentProduct.Quantity,
                currentProduct.CreatedAt,
                currentProduct.UpdatedAt,
                currentProduct.Category != null ? currentProduct.Category.Id.ToString() : "",
                currentProduct.RestaurantId);


            await _redis.SetStringAsync($"restaurant:{RestaurantId}:product:all", "");
            await _redis.SetStringAsync($"restaurant:{RestaurantId}:product:{id}", "");

            return Ok(productDto);
        }
    }
    public class ProductRequestModel
    {
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
        public double TaxPercent { get; set; }
        public double SpecialPrice { get; set; }
        public bool Visible { get; set; }
        public int Quantity { get; set; }
        public string? CategoryId { get; set; }
    }

    public class GetProductsRequestModel
    {
        public string RestaurantId { get; set; }
        public string? CategoryId { get; set; }
    }
}
