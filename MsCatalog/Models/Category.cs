using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MsCatalog.Models
{
    public class Category
    {
        [BindNever]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ICollection<Product>? Products { get; set; }

        public Category(string name)
        {
            Name = name;
        }
    }
}
