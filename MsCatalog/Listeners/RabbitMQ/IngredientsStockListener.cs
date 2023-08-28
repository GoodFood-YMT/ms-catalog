using Microsoft.EntityFrameworkCore;
using MsCatalog.Data;
using MsCatalog.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using MsCatalog.Services;

namespace MsCatalog.Listeners.RabbitMQ;

public class IngredientsStockListener : RabbitMQListener
{
    private readonly ApiDbContext _context;
    private readonly IDistributedCache _redis;
    private StockService _stockService;

    public IngredientsStockListener(ApiDbContext context, IDistributedCache redis, StockService stockService) : base("catalog.ingredients.stock")
    {
        this._context = context;
        _redis = redis;
        _stockService = stockService;
    }

    protected override async Task Handle(string message)
    {
        RequestStockModel? result = JsonConvert.DeserializeObject<RequestStockModel>(message);
        if (result == null)
        {
            return;
        }

        Ingredient? ingredient = await _context.Ingredients
            .Where(i => i.RestaurantId == result.restaurantId && i.Id.ToString() == result.ingredientId)
            .FirstOrDefaultAsync();
        if (ingredient != null)
        {
            ingredient.Quantity += result.add;
            _context.SaveChanges();
            await _redis.SetStringAsync($"restaurant:{result.restaurantId}:ingredient:all", "");
            await _redis.SetStringAsync($"restaurant:{result.restaurantId}:ingredient:{result.ingredientId}", "");
            await _stockService.UpdateStockProductsByIngredient(ingredient.Id.ToString());
        }
    }
}

public class RequestStockModel
{
    public string restaurantId { get; set; }
    public string ingredientId { get; set; }
    public int add { get; set; }
}