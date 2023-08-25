using Microsoft.EntityFrameworkCore;
using MsCatalog.Data;
using MsCatalog.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace MsCatalog.Listeners.RabbitMQ;

public class ProductsSoldListener : RabbitMQListener
{
    private readonly ApiDbContext _context;
    private readonly IDistributedCache _redis;

    public ProductsSoldListener(ApiDbContext context, IDistributedCache redis) : base("catalog.products.sold")
    {
        this._context = context;
        _redis = redis;
    }

    protected override async Task Handle(string message)
    {
        RequestSoldModel? result = JsonConvert.DeserializeObject<RequestSoldModel>(message);
        if (result == null)
        {
            return;
        }
        Product? product = await _context.Products.Where(p => p.RestaurantId == result.restaurantId && p.Id.ToString() == result.productId).FirstOrDefaultAsync();
        if (product != null)
        {
            product.Quantity -= result.quantity;
            _context.SaveChanges();

            await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product:all", string.Empty);
            await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product-visible:all", string.Empty);
            await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product-inStock:all", string.Empty);
            await _redis.SetStringAsync($"product-inStock:all", string.Empty);
            await _redis.SetStringAsync($"product-visible:all", string.Empty);
            await _redis.SetStringAsync($"product:{product.Id}", string.Empty);
        }
    }
}

public class RequestSoldModel
{
    public string restaurantId { get; set; }
    public string productId { get; set; }
    public int quantity { get; set; }
}