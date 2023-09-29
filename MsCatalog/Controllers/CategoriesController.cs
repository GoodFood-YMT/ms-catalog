using Microsoft.AspNetCore.Cors;
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
    [Route("catalog/categories")]
    [ApiController]
    public class CategoriesController : Controller
    {

        private readonly ApiDbContext _context;
        private readonly ILogger _logger;
        private readonly IDistributedCache _redis;
        private readonly IUriService _uriService;

        public CategoriesController(ILogger<Category> logger, ApiDbContext context, IDistributedCache redis, IUriService uriService)
        {
            _logger = logger;
            _context = context;
            _redis = redis;
            _uriService = uriService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] PaginationFilter filter)
        {
            List<CategoryDto> categories = new List<CategoryDto>();
            List<CategoryDto> response = new List<CategoryDto>();
            PagedResponse<List<CategoryDto>> pagedReponse;
            int totalRecords = 0;

            string route = Request.Path.Value!;
            PaginationFilter validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

            string? cachedCategories = await _redis.GetStringAsync("categories");

            if (cachedCategories != null)
            {
                categories = JsonConvert.DeserializeObject<List<CategoryDto>>(cachedCategories);

                totalRecords = categories!.Count();

                response = categories!.Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();
                
                pagedReponse = PaginationHelper.CreatePagedReponse(response, validFilter, totalRecords, _uriService, route);

                return Ok(pagedReponse);
            }

            categories = await _context.Categories.Select(c => new CategoryDto { Id = c.Id.ToString(), Name = c.Name}).ToListAsync();

            response = categories
                .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                .Take(validFilter.PageSize)
                .ToList();

            totalRecords = categories.Count();

            pagedReponse = PaginationHelper.CreatePagedReponse(response, validFilter, totalRecords, _uriService, route);

            await _redis.SetStringAsync("categories", JsonConvert.SerializeObject(categories));

            return Ok(pagedReponse);      
        }

        [HttpPost()]
        public async Task<IActionResult> Create(CategoryRequestModel request)
        {
            if(request == null || string.IsNullOrEmpty(request.Name))
            {
                return BadRequest();
            }
            Category newCategory = new(request.Name);          
            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            CategoryDto newCategoryDto = new CategoryDto() { Id = newCategory.Id.ToString(), Name = newCategory.Name };
            await _redis.SetStringAsync("categories", "");

            return Ok(newCategoryDto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(string id)
        {
            string key = $"category-{id}";
            string? cacheCategory = await _redis.GetStringAsync(key);

            CategoryDto? category = null;

            if (string.IsNullOrEmpty(cacheCategory))
            {
                category = _context.Categories.Where(c => c.Id.ToString() == id).Select(c => new CategoryDto { Id = c.Id.ToString(), Name = c.Name }).FirstOrDefault();

                if (category == null)
                {
                    return NotFound();
                }

                await _redis.SetStringAsync(key, "");
                await _redis.SetStringAsync("categories", "");

                return Ok(category);
            }

            category = JsonConvert.DeserializeObject<CategoryDto>(cacheCategory);
            return Ok(category);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Category>> Edit(string id, [FromBody] CategoryRequestModel request)
        {

            if(request == null || string.IsNullOrEmpty(request.Name)){
                return BadRequest();
            }

            Category? currentCategory = _context.Categories.Where(c => c.Id.ToString() == id).FirstOrDefault();
            if (currentCategory != null)
            {
                currentCategory.Name = request.Name;
                _context.Categories.Update(currentCategory);
                await _context.SaveChangesAsync();

                CategoryDto categoryDto = new CategoryDto() { Id = currentCategory.Id.ToString(), Name = currentCategory.Name };

                await _redis.SetStringAsync($"category-{id}", "");
                await _redis.SetStringAsync("categories", "");

                return Ok(categoryDto);
            }
            else
            {
                return NotFound();
            }
        }
    }

    public class CategoryRequestModel
    {
        public string Name { get; set; } = "";
    }
}
