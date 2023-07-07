using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MsCatalog.Models
{
    public class Product
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
        public double TaxPercent { get; set; }
        public double SpecialPrice { get; set; }
        public bool Visible { get; set; }
        public int Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Category? Category { get; set; }
        public string RestaurantId { get; set; }
        public ICollection<ProductsIngredients>? ProductsIngredients { get; set; }

        public Product(string label, string description, double price, double taxPercent, double specialPrice, bool visible, int quantity, string restaurantId)
        {
            Label = label;
            Description = description;
            Price = price;
            TaxPercent = taxPercent;
            SpecialPrice = specialPrice;
            Visible = visible;
            Quantity = quantity;
            CreatedAt = DateTime.Now;
            Category = null;
            RestaurantId = restaurantId;
        }
    }

    public class ProductsDto
    {
        public string Id { get; set; }
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
        public double TaxPercent { get; set; }
        public double SpecialPrice { get; set; }
        public bool Visible { get; set; }
        public int Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CategoryId { get; set; }
        public string RestaurantId { get; set; }

        public ProductsDto(string id, string label, string description, double price, double taxPercent, double specialPrice, bool visible, int quantity, DateTime? createdAt, DateTime? updatedAt, string categoryId, string restaurantId)
        {
            Id = id;
            Label = label;
            Description = description;
            Price = price;
            TaxPercent = taxPercent;
            SpecialPrice = specialPrice;
            Visible = visible;
            Quantity = quantity;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CategoryId = categoryId;
            RestaurantId = restaurantId;
        }
    }
}
