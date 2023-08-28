using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MsCatalog.Models
{
    public class Ingredient
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public string RestaurantId { get; set; }
        public ICollection<ProductsIngredients>? ProductsIngredients { get; set; }

        public Ingredient(string name, int quantity, string restaurantId)
        {
            Name = name;
            Quantity = quantity; 
            RestaurantId = restaurantId;
        }
    }

    public class IngredientDto
    {
        public string Id { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public string RestaurantId { get; set; }
    }
}
