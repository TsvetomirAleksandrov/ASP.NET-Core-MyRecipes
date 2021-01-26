namespace MyRecipes.Services.Data
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using MyRecipes.Services.Data.Models;
    using MyRecipes.Web.ViewModels.Home;

    public interface IGetCountsService
    {
        CountsDto GetCounts();
    }
}
