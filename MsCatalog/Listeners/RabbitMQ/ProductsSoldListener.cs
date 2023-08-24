using Microsoft.EntityFrameworkCore;
using MsCatalog.Data;
using MsCatalog.Models;
using Newtonsoft.Json;

namespace MsCatalog.Listeners.RabbitMQ;

public class ProductsSoldListener : RabbitMQListener
{
    private readonly ApiDbContext _context;

    public ProductsSoldListener(ApiDbContext context) : base("catalog.products.sold")
    {
        this._context = context;
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
        }
    }
}

public class RequestSoldModel
{
    public string restaurantId { get; set; }
    public string productId { get; set; }
    public int quantity { get; set; }
}