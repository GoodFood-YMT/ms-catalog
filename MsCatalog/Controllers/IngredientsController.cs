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
    [Route("catalog/ingredients")]
    [ApiController]
    public class IngredientsController : Controller
    {

        private readonly ApiDbContext _context;
        private readonly ILogger _logger;
        private readonly IDistributedCache _redis;
        private readonly IUriService _uriService;

        public IngredientsController(ILogger<Ingredient> logger, ApiDbContext context, IDistributedCache redis, IUriService uriService)
        {
            _logger = logger;
            _context = context;
            _redis = redis;
            _uriService = uriService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] PaginationFilter filter)
        {
            string key = "ingredients";
            string? cachedIngredients = await _redis.GetStringAsync(key);
            List<IngredientDto>? ingredients = new List<IngredientDto>();

            PagedResponse<List<IngredientDto>> pagedReponse;
            int totalRecords = 0;
            string route = Request.Path.Value!;
            PaginationFilter validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

            if (string.IsNullOrEmpty(cachedIngredients))
            {
                ingredients = await _context.Ingredients
                    .Select(i => new IngredientDto { Id = i.Id, Name = i.Name })
                    .ToListAsync();

                totalRecords = ingredients.Count();

                ingredients
                    .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();

                await _redis.SetStringAsync(key, ingredients.Count > 0 ? JsonConvert.SerializeObject(ingredients) : "");

                pagedReponse = PaginationHelper.CreatePagedReponse(ingredients, validFilter, totalRecords, _uriService, route);
                return Ok(pagedReponse);
            }
            ingredients = JsonConvert.DeserializeObject<List<IngredientDto>>(cachedIngredients)
                .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                .Take(validFilter.PageSize)
                .ToList();
            totalRecords = ingredients!.Count();
            pagedReponse = PaginationHelper.CreatePagedReponse(ingredients!, validFilter, totalRecords, _uriService, route);

            return Ok(pagedReponse);
        }

        [HttpPost]
        public async Task<IActionResult> Create(IngredientModel request)
        {
            Ingredient newIngredient = new(request.Name);
            _context.Ingredients.Add(newIngredient);
            await _context.SaveChangesAsync();

            IngredientDto ingredient = new IngredientDto { Id = newIngredient.Id, Name = request.Name };
            await _redis.SetStringAsync("ingredients", "");

            return Ok(ingredient);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            string key = $"ingredient-{id}";
            string? cachedIngredient = await _redis.GetStringAsync(key);
            IngredientDto? ingredient = null;

            if (string.IsNullOrEmpty(cachedIngredient))
            {
                ingredient = await _context.Ingredients
                    .Where(i => i.Id == id)
                    .Select(i => new IngredientDto { Id = i.Id, Name = i.Name })
                    .FirstOrDefaultAsync();

                if (ingredient == null)
                {
                    return NotFound();
                }

                await _redis.SetStringAsync(key, JsonConvert.SerializeObject(ingredient));
                return Ok(ingredient);
            }

            ingredient = JsonConvert.DeserializeObject<IngredientDto>(cachedIngredient);
            return Ok(ingredient);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] IngredientModel request)
        {
            Ingredient? currentIgredient = await _context.Ingredients.Where(i => i.Id == id).FirstOrDefaultAsync();

            if (currentIgredient == null)
            {
                return NotFound();
            }

            currentIgredient.Name = request.Name;
            await _context.SaveChangesAsync();

            IngredientDto ingredientDto = new() { Id = currentIgredient.Id, Name = currentIgredient.Name };

            await _redis.SetStringAsync("ingredients", "");
            await _redis.SetStringAsync($"ingredient-{id}", "");

            return Ok(ingredientDto);
        }
    }

    public class IngredientModel {
        public string Name { get; set; } = "";
    }

}
