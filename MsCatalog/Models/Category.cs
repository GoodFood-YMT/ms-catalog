using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace MsCatalog.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ICollection<Product>? Products { get; set; }

        public Category(string name)
        {
            Name = name;
        }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
