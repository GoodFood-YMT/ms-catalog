using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MsCatalog.Models
{
    public class Ingredient
    {
        [BindNever]
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public ICollection<ProductsIngredients>? ProductsIngredients { get; set; }
    }
}
