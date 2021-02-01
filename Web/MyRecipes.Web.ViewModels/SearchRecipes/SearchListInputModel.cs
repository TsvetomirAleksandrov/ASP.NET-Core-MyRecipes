namespace MyRecipes.Web.ViewModels.SearchRecipes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SearchListInputModel
    {
       public IEnumerable<int> Ingredients { get; set; }
    }
}
