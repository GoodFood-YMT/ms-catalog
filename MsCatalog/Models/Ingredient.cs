using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MsCatalog.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ICollection<ProductsIngredients>? ProductsIngredients { get; set; }

        public Ingredient(string name)
        {
            Name = name;
        }
    }

    public class IngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
