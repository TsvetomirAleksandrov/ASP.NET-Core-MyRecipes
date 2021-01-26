namespace MyRecipes.Services.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class CountsDto
    {
        public int RecipesCount { get; set; }

        public int CategoriesCount { get; set; }

        public int IngredientsCount { get; set; }

        public int ImagesCount { get; set; }
    }
}
