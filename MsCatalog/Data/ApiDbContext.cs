using Microsoft.EntityFrameworkCore;
using MsCatalog.Models;

namespace MsCatalog.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
        {
            
        }

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<ProductsIngredients> ProductsIngredients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductsIngredients>().HasKey(pi => new { pi.IngredientId, pi.ProductId });
        }
    }   
}
