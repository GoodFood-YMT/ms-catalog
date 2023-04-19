using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ms_catalog.Models
{
    public class ProductsIngredients
    {
        [Key, Column(Order = 0)]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Key, Column(Order = 1)]
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
    }
}
