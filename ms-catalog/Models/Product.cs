using System.Runtime.CompilerServices;

namespace ms_catalog.Models
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
        public Categorie Categorie { get; set; }
        public ICollection<ProductsIngredients>? ProductsIngredients { get; set; }
    }
}
