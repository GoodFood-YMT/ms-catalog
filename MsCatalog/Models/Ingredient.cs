namespace MsCatalog.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public ICollection<ProductsIngredients>? ProductsIngredients { get; set; }
    }
}
