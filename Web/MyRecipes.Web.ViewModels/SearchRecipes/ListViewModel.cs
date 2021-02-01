namespace MyRecipes.Web.ViewModels.SearchRecipes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MyRecipes.Web.ViewModels.Recipes;

    public class ListViewModel
    {
        public IEnumerable<RecipeInListViewModel> Recipes { get; set; }
    }
}
