using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MsCatalog.Models
{
    public class Category
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public ICollection<Product>? Products { get; set; }

        public Category(string name)
        {
            Name = name;
        }
    }

    public class CategoryDto
    {
        public string Id { get; set; }
        public string Name { get; set; } = "";
    }
}
