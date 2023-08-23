using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MsCatalog.Models
{
    public class ProductsIngredients
    {
        [Key, Column(Order = 0)]
        public string ProductId { get; set; }
        public Product Product { get; set; }

        [Key, Column(Order = 1)]
        public string IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }

        public int Quantity { get; set; }
    }

    public class ProductsIngredientsDto
    {
        public string ProductId { get; set; }
        public string IngredientId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }

}
