﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MsCatalog.Models
{
    public class ProductsIngredients
    {
        [Key, Column(Order = 0)]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Key, Column(Order = 1)]
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }

        public int Quantity { get; set; }
    }

    public class ProductsIngredientsDto
    {
        public int ProductId { get; set; }
        public int IngredientId { get; set; }
        public int Quantity { get; set; }
    }

}
