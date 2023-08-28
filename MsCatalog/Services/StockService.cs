using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MsCatalog.Data;
using MsCatalog.Models;

namespace MsCatalog.Services
{
    public class StockService
    {
        private ApiDbContext _context;
        private IDistributedCache _redis;

        public StockService(ApiDbContext context, IDistributedCache redis)
        {
            _context = context;
            _redis = redis;
        }

        public async Task UpdateProductStock(string productId)
        {
            Product? product = await _context.Products.Where(p => p.Id.ToString() == productId).FirstOrDefaultAsync();
            if (product == null) return;
            
            List<ProductsIngredients> productsIngredients = await _context.ProductsIngredients.Where(pi => pi.ProductId == productId).ToListAsync();
            List<string> ids = productsIngredients.Select(i => i.IngredientId).ToList();
            List<Ingredient> ingredients = await _context.Ingredients.Where(i => ids.Contains(i.Id.ToString())).ToListAsync();
            int stock = 0;
            foreach (ProductsIngredients pi in productsIngredients)
            {
                Ingredient? ingredient = ingredients.Where(i => i.Id.ToString() == pi.IngredientId.ToString()).FirstOrDefault();
                if (ingredient == null) continue;
                int tempStock = (int)Math.Ceiling((decimal)(ingredient.Quantity / pi.Quantity));
                if(tempStock < stock) stock = tempStock;

            }
            product.Quantity = stock;
            _context.SaveChanges();

            await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product:all", string.Empty);
            await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product-visible:all", string.Empty);
            await _redis.SetStringAsync($"restaurant:{product.RestaurantId}:product-inStock:all", string.Empty);
            await _redis.SetStringAsync($"product-inStock:all", string.Empty);
            await _redis.SetStringAsync($"product-visible:all", string.Empty);
            await _redis.SetStringAsync($"product:{product.Id}", string.Empty);
        }

        public async Task UpdateStockProductsByIngredient(string ingredientId)
        {
            List<ProductsIngredients> productsIngredients = await _context.ProductsIngredients.Where(pi => pi.Ingredient.Id.ToString() == ingredientId).ToListAsync();
            
            foreach (ProductsIngredients pi in productsIngredients)
            {
                await UpdateProductStock(pi.ProductId);
            }
        }
    }
}
