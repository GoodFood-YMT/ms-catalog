using Microsoft.EntityFrameworkCore;
using ms_catalog.Models;

namespace ms_catalog.Data
{
    public class ApiDBContext : DbContext
    {
        public ApiDBContext(DbContextOptions<ApiDBContext> options) : base(options)
        {
            
        }

        public DbSet<Product> Product { get; set; }

        public DbSet<Categorie> Categorie { get; set; }
        public DbSet<Ingredient> Ingredient { get; set; }
        public DbSet<ProductsIngredients> ProductsIngredients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductsIngredients>().HasKey(pi => new { pi.IngredientId, pi.ProductId });
        }
    }   
}
