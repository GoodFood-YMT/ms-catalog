using System.Text;
using Microsoft.EntityFrameworkCore;
using MsCatalog.Data;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using MsCatalog.Models;
using RabbitMQ.Client;
using System.Threading.Channels;
using System;

namespace MsCatalog.Services.RabbitMqService
{
    public class RabbitMqService : IRabbitMqService
    {
        private readonly ApiDbContext _context;

        public RabbitMqService(ApiDbContext context)
        {
            _context = context;
        }
        public async void ListenToStockIngredients(BasicDeliverEventArgs eventArgs)
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine("Meesage received in catalog.ingredients.stock" + message);
            RequestStockModel result = JsonConvert.DeserializeObject<RequestStockModel>(message);
            if (result == null)
            {
                return;
            }
            Ingredient? ingredient = await _context.Ingredients.Where(i => i.RestaurantId == result.restaurantId && i.Id.ToString() == result.ingredientId).FirstOrDefaultAsync();
            if (ingredient != null)
            {
                ingredient.Quantity += result.add;
                _context.SaveChanges();
            }
            Console.WriteLine($"Stock Ingredients: {message}");

        }
        public async void ListenToSoldProducts(BasicDeliverEventArgs eventArgs)
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine("Meesage received in catalog.products.sold" + message);
            RequestSoldModel result = JsonConvert.DeserializeObject<RequestSoldModel>(message);
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
            Console.WriteLine($"Sold Ingredients: {message}");
        }
    }

    public class RequestStockModel
    {
        public string restaurantId { get; set; }
        public string ingredientId { get; set; }
        public int add { get; set; }
    }
    public class RequestSoldModel
    {
        public string restaurantId { get; set; }
        public string productId { get; set; }
        public int quantity { get; set; }
    }
}
