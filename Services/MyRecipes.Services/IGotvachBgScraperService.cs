namespace MyRecipes.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public interface IGotvachBgScraperService
    {
        Task PopulateDbWithRecipesAsync(int recipesCount);
    }
}
