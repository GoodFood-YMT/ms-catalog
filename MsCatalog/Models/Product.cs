using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MsCatalog.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
        public double TaxPercent { get; set; }
        public double SpecialPrice { get; set; }
        public bool Visible { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Category? Category { get; set; }
        public ICollection<ProductsIngredients>? ProductsIngredients { get; set; }

        public Product(string label, string description, double price, double taxPercent, double specialPrice, bool visible)
        {
            Label = label;
            Description = description;
            Price = price;
            TaxPercent = taxPercent;
            SpecialPrice = specialPrice;
            Visible = visible;
            CreatedAt = DateTime.Now;
            Category = null;
        }
    }

    public class ProductsDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
        public double TaxPercent { get; set; }
        public double SpecialPrice { get; set; }
        public bool Visible { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CategoryId { get; set; }

        public ProductsDto(int id, string label, string description, double price, double taxPercent, double specialPrice, bool visible, DateTime? createdAt, DateTime? updatedAt, int categoryId)
        {
            Id = id;
            Label = label;
            Description = description;
            Price = price;
            TaxPercent = taxPercent;
            SpecialPrice = specialPrice;
            Visible = visible;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CategoryId = categoryId;
        }
    }
}
