using Microsoft.EntityFrameworkCore;
using MsCatalog.Data;
using MsCatalog.Models;
using Newtonsoft.Json;

namespace MsCatalog.Listeners.RabbitMQ;

public class IngredientsStockListener : RabbitMQListener
{
    private readonly ApiDbContext _context;

    public IngredientsStockListener(ApiDbContext context) : base("catalog.ingredients.stock")
    {
        this._context = context;
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
        }
    }
}

public class RequestStockModel
{
    public string restaurantId { get; set; }
    public string ingredientId { get; set; }
    public int add { get; set; }
}