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
        public async Task<IActionResult> GetIngredients([FromQuery] PaginationFilter filter)
        {
            string RestaurantId = Request.Headers["RestaurantID"].ToString();
            if (string.IsNullOrEmpty(RestaurantId))
            {
                return BadRequest();
            }
            string? cachedIngredients = await _redis.GetStringAsync($"restaurant:{RestaurantId}:ingredient:all");
            List<IngredientDto>? ingredients = new List<IngredientDto>();

            PagedResponse<List<IngredientDto>> pagedReponse;
            int totalRecords = 0;
            string route = Request.Path.Value!;
            PaginationFilter validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

            if (string.IsNullOrEmpty(cachedIngredients))
            {
                ingredients = await _context.Ingredients
                    .Select(i => new IngredientDto { 
                        Id = i.Id.ToString(),
                        Name = i.Name, 
                        Quantity = i.Quantity, 
                        RestaurantId = i.RestaurantId 
                    }).ToListAsync();

                await _redis.SetStringAsync($"restaurant:{RestaurantId}:ingredient:all", ingredients.Count > 0 ? JsonConvert.SerializeObject(ingredients) : "");

                ingredients = ingredients.Where(i => i.RestaurantId == RestaurantId).ToList();

                totalRecords = ingredients.Count();

                ingredients
                    .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();

                pagedReponse = PaginationHelper.CreatePagedReponse(ingredients, validFilter, totalRecords, _uriService, route);
                return Ok(pagedReponse);
            }
            ingredients = JsonConvert.DeserializeObject<List<IngredientDto>>(cachedIngredients)
                .Where(i => i.RestaurantId == RestaurantId)
                .ToList();

            totalRecords = ingredients!.Count();

            ingredients = ingredients
                .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                .Take(validFilter.PageSize)
                .ToList();
            
            pagedReponse = PaginationHelper.CreatePagedReponse(ingredients!, validFilter, totalRecords, _uriService, route);

            return Ok(pagedReponse);
        }

        [HttpPost]
        public async Task<IActionResult> CreateIngredient(IngredientModel request)
        {
            string RestaurantId = Request.Headers["RestaurantID"].ToString();
            if (string.IsNullOrEmpty(RestaurantId) || !request.ValidFields())
            {
                return BadRequest();
            }

            Ingredient newIngredient = new(request.Name, request.Quantity.Value, RestaurantId);
            _context.Ingredients.Add(newIngredient);
            await _context.SaveChangesAsync();

            IngredientDto ingredient = new IngredientDto { 
                Id = newIngredient.Id.ToString(), 
                Name = newIngredient.Name, 
                Quantity = newIngredient.Quantity, 
                RestaurantId = newIngredient.RestaurantId 
            };

            await _redis.SetStringAsync($"restaurant:{RestaurantId}:ingredient:all", "");

            return Ok(ingredient);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIngredientById(string id)
        {
            string RestaurantId = Request.Headers["RestaurantID"].ToString();
            if (string.IsNullOrEmpty(RestaurantId))
            {
                return BadRequest();
            }

            string? cachedIngredient = await _redis.GetStringAsync($"restaurant:{RestaurantId}:ingredient:{id}");
            IngredientDto? ingredient = null;

            if (string.IsNullOrEmpty(cachedIngredient))
            {
                ingredient = await _context.Ingredients
                    .Where(i => i.Id.ToString() == id && i.RestaurantId == RestaurantId)
                    .Select(i => new IngredientDto { 
                        Id = i.Id.ToString(), 
                        Name = i.Name, 
                        Quantity = i.Quantity, 
                        RestaurantId = i.RestaurantId 
                    }).FirstOrDefaultAsync();

                if (ingredient == null)
                {
                    return NotFound();
                }

                await _redis.SetStringAsync($"restaurant:{RestaurantId}:ingredient:{id}", JsonConvert.SerializeObject(ingredient));
                return Ok(ingredient);
            }

            ingredient = JsonConvert.DeserializeObject<IngredientDto>(cachedIngredient);
            return Ok(ingredient);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIngredient(string id, [FromBody] IngredientModel request)
        {
            string RestaurantId = Request.Headers["RestaurantID" ].ToString();

            if(string.IsNullOrEmpty(RestaurantId))
            {
                return BadRequest();
            }

            Ingredient? currentIgredient = await _context.Ingredients.Where(i => i.Id.ToString() == id && i.RestaurantId == RestaurantId).FirstOrDefaultAsync();

            if (currentIgredient == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(request.Name)){ currentIgredient.Name = request.Name; }
            if (request.Quantity.HasValue){ currentIgredient.Quantity = request.Quantity.Value; }
            await _context.SaveChangesAsync();

            IngredientDto ingredientDto = new() { Id = currentIgredient.Id.ToString(), Name = currentIgredient.Name, Quantity = currentIgredient.Quantity, RestaurantId = currentIgredient.RestaurantId };

            await _redis.SetStringAsync($"restaurant:{RestaurantId}:ingredient:all", "");
            await _redis.SetStringAsync($"restaurant:{RestaurantId}:ingredient:{id}", "");

            return Ok(ingredientDto);
        }
    }

    public class IngredientModel {

        public string Name { get; set; } = "";
        public int? Quantity { get; set; }

        public bool ValidFields()
        {
            return 
                !string.IsNullOrEmpty(Name) &&
                Quantity.HasValue;
        }
    }
}
