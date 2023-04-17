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
    }
}
