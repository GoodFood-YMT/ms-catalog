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
    [Route("catalog/products")]
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
        public async Task<IActionResult> Get([FromQuery] PaginationFilter filter)
        {
            List<ProductsDto> products = new();
            PagedResponse<List<ProductsDto>> pagedReponse;
            int totalRecords = 0;

            string route = Request.Path.Value!;
            PaginationFilter validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

            string key = "products";
            string? cachedProducts = await _redis.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedProducts))
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Visible)
                    .Select(p => new ProductsDto(
                        p.Id,
                        p.Label,
                        p.Description,
                        p.Price,
                        p.TaxPercent,
                        p.SpecialPrice,
                        p.Visible,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.Category != null ? p.Category.Id : 0)
                    ).ToListAsync();

                totalRecords = products.Count();

                products
                    .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();


                pagedReponse = PaginationHelper.CreatePagedReponse(products, validFilter, totalRecords, _uriService, route);

                await _redis.SetStringAsync(key, products.Count > 0 ? JsonConvert.SerializeObject(products) : "");
                return Ok(products);
            }

            products = JsonConvert.DeserializeObject<List<ProductsDto>>(cachedProducts);
            totalRecords = products!.Count();
            pagedReponse = PaginationHelper.CreatePagedReponse(products!, validFilter, totalRecords, _uriService, route);

            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductRequestModel product)
        {
            string key = "products";

            if (product == null)
            {
                return BadRequest();
            }

            Product newProduct = new(product.Label, product.Description, product.Price, product.TaxPercent, product.SpecialPrice, product.Visible);
            if (product.Idcategory != null)
            {
                Category? categ = _context.Categories.FirstOrDefault(c => c.Id == product.Idcategory);
                newProduct.Category = categ;
            }
            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            ProductsDto productDto = new(
                newProduct.Id,
                newProduct.Label,
                newProduct.Description,
                newProduct.Price,
                newProduct.TaxPercent,
                newProduct.SpecialPrice,
                newProduct.Visible,
                newProduct.CreatedAt,
                newProduct.UpdatedAt,
                newProduct.Category != null ? newProduct.Category.Id : 0);

            await _redis.SetStringAsync(key, "");

            return Ok(productDto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            string key = $"product-{id}";
            string? cachedProduct = await _redis.GetStringAsync(key);
            ProductsDto? product = null;

            if (cachedProduct == null)
            {
                product = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Id == id)
                    .Select(p => new ProductsDto(
                        p.Id,
                        p.Label,
                        p.Description,
                        p.Price,
                        p.TaxPercent,
                        p.SpecialPrice,
                        p.Visible,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.Category != null ? p.Category.Id : 0)
                    ).FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound();
                }

                await _redis.SetStringAsync(key, JsonConvert.SerializeObject(product));
                return Ok(product);
            }

            product = JsonConvert.DeserializeObject<ProductsDto>(cachedProduct);
            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductRequestModel product)
        {
            Product? currentProduct = _context.Products.ToList().Where(c => c.Id.Equals(id)).FirstOrDefault();
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

            if (product.Idcategory != null)
            {
                Category? categ = _context.Categories.FirstOrDefault(c => c.Id == product.Idcategory);
                currentProduct.Category = categ;
            }

            currentProduct.UpdatedAt = DateTime.Now;
            _context.Products.Update(currentProduct);
            await _context.SaveChangesAsync();

            ProductsDto productDto = new(
                currentProduct.Id,
                currentProduct.Label,
                currentProduct.Description,
                currentProduct.Price,
                currentProduct.TaxPercent,
                currentProduct.SpecialPrice,
                currentProduct.Visible,
                currentProduct.CreatedAt,
                currentProduct.UpdatedAt,
                currentProduct.Category != null ? currentProduct.Category.Id : 0);


            await _redis.SetStringAsync("products", "");
            await _redis.SetStringAsync($"product-{id}", "");

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
        public int? Idcategory { get; set; }
    }
}
