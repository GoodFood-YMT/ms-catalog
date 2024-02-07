using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MsCatalog.Data;
using MsCatalog.Models;

namespace MsCatalog.Services
{
    public class StockService
    {
        private IDistributedCache _redis;

        public StockService(IDistributedCache redis)
        {
            _redis = redis;
        }

        public async Task UpdateProductStock(string productId, ApiDbContext context)
        {
            try
            {
                Product? product = await context.Products.Where(p => p.Id.ToString() == productId).FirstOrDefaultAsync();
                Console.WriteLine(product);
                if (product == null) return;
                Console.WriteLine(product);

                List<ProductsIngredients> productsIngredients = await context.ProductsIngredients.Where(pi => pi.ProductId == productId).ToListAsync();
                Console.WriteLine(productsIngredients.Count);
                List<string> ids = productsIngredients.Select(i => i.IngredientId).ToList();
                Console.WriteLine(ids.Count);
                List<Ingredient> ingredients = await context.Ingredients.Where(i => ids.Contains(i.Id.ToString())).ToListAsync();
                Console.WriteLine(ingredients.Count);
                int stock = int.MaxValue;
                foreach (ProductsIngredients pi in productsIngredients)
                {
                    Ingredient? ingredient = ingredients.Where(i => i.Id.ToString() == pi.IngredientId.ToString()).FirstOrDefault();
                    if (ingredient == null) continue;
                    Console.WriteLine($"ingredientQuantity: {ingredient.Quantity} & productIngredientQuantity: {pi.Quantity}");
                    int tempStock = (int)Math.Ceiling((decimal)(ingredient.Quantity / pi.Quantity));
                    Console.WriteLine("tempStock: " + tempStock);
                    if (tempStock < stock) stock = tempStock;

                }
                Console.WriteLine(stock);
                product.Quantity = stock == int.MaxValue ? 0 : stock;
                await context.SaveChangesAsync();

                await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product:all", string.Empty);
                await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product-visible:all", string.Empty);
                await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product-inStock:all", string.Empty);
                await _redis.SetStringAsync($"product-inStock:all", string.Empty);
                await _redis.SetStringAsync($"product-visible:all", string.Empty);
                await _redis.SetStringAsync($"product:{product.Id}", string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }         
        }

        public async Task UpdateStockProductsByIngredient(string ingredientId, ApiDbContext context)
        {
            List<ProductsIngredients> productsIngredients = await context.ProductsIngredients.Where(pi => pi.Ingredient.Id.ToString() == ingredientId).ToListAsync();
            
            foreach (ProductsIngredients pi in productsIngredients)
            {
                await UpdateProductStock(pi.ProductId, context);
            }
        }
    }
}
