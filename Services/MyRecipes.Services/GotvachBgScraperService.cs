﻿namespace MyRecipes.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using AngleSharp;
    using MyRecipes.Data.Common.Repositories;
    using MyRecipes.Data.Models;
    using MyRecipes.Services.Models;

    public class GotvachBgScraperService : IGotvachBgScraperService
    {
        private readonly IConfiguration config;
        private readonly IBrowsingContext context;

        private readonly IDeletableEntityRepository<Category> categoriesRepository;
        private readonly IDeletableEntityRepository<Ingredient> ingredientsRepository;
        private readonly IDeletableEntityRepository<Recipe> recipesRepository;
        private readonly IRepository<RecipeIngredient> recipeIngredientRepository;
        private readonly IRepository<Image> imagesRepository;

        public GotvachBgScraperService(
            IDeletableEntityRepository<Category> categoriesRepository,
            IDeletableEntityRepository<Ingredient> ingredientsRepository,
            IDeletableEntityRepository<Recipe> recipesRepository,
            IRepository<RecipeIngredient> recipeIngredientRepository,
            IRepository<Image> imagesRepository)
        {
            this.categoriesRepository = categoriesRepository;
            this.ingredientsRepository = ingredientsRepository;
            this.recipesRepository = recipesRepository;
            this.recipeIngredientRepository = recipeIngredientRepository;
            this.imagesRepository = imagesRepository;

            this.config = Configuration.Default.WithDefaultLoader();
            this.context = BrowsingContext.New(this.config);
        }

        public async Task PopulateDbWithRecipesAsync(int recipesCount)
        {
            var concurrentBag = new ConcurrentBag<RecipeDto>();

            Parallel.For(1, recipesCount, (i) =>
            {
                try
                {
                    var recipe = this.GetRecipe(i);
                    concurrentBag.Add(recipe);
                }
                catch
                {
                }
            });

            foreach (var recipe in concurrentBag)
            {
                var categoryId = await this.GetOrCreateCategoryAsync(recipe.CategoryName);

                var recipeExists = this.recipesRepository
                    .AllAsNoTracking()
                    .Any(x => x.Name == recipe.RecipeName);

                if (recipeExists)
                {
                    continue;
                }

                var newRecipe = new Recipe()
                {
                    Name = recipe.RecipeName,
                    Instructions = recipe.Instructions,
                    PreparationTime = recipe.PreparationTime,
                    CookingTime = recipe.CookingTime,
                    PortionsCount = recipe.PortionsCount,
                    OriginalUrl = recipe.OriginalUrl,
                    CategoryId = categoryId,
                };

                await this.recipesRepository.AddAsync(newRecipe);
                await this.recipesRepository.SaveChangesAsync();

                foreach (var item in recipe.Ingredients)
                {
                    var ingr = item.Split(" - ", 2);

                    if (ingr.Length < 2)
                    {
                        continue;
                    }

                    var ingredientId = await this.GetOrCreateIngredientAsync(ingr[0].Trim());
                    var qty = ingr[1].Trim();

                    var recipeIngredient = new RecipeIngredient
                    {
                        IngredientId = ingredientId,
                        RecipeId = newRecipe.Id,
                        Quantity = qty,
                    };

                    await this.recipeIngredientRepository.AddAsync(recipeIngredient);
                    await this.recipeIngredientRepository.SaveChangesAsync();
                }

                var image = new Image
                {
                    Extension = recipe.OriginalUrl,
                    RecipeId = newRecipe.Id,
                };

                await this.imagesRepository.AddAsync(image);
                await this.imagesRepository.SaveChangesAsync();
            }
        }

        private async Task<int> GetOrCreateIngredientAsync(string name)
        {
                var ingredient = this.ingredientsRepository
                    .AllAsNoTracking()
                    .FirstOrDefault(x => x.Name == name);

                if (ingredient == null)
                {
                    ingredient = new Ingredient
                    {
                        Name = name,
                    };

                    await this.ingredientsRepository.AddAsync(ingredient);
                    await this.ingredientsRepository.SaveChangesAsync();
            }

                return ingredient.Id;
        }

        private async Task<int> GetOrCreateCategoryAsync(string categoryName)
        {
            var category = this.categoriesRepository
                    .AllAsNoTracking()
                    .FirstOrDefault(x => x.Name == categoryName);

            if (category == null)
            {
                category = new Category()
                {
                    Name = categoryName,
                };

                await this.categoriesRepository.AddAsync(category);
                await this.categoriesRepository.SaveChangesAsync();
            }

            return category.Id;
        }

        private RecipeDto GetRecipe(int id)
        {
            var document = this.context
                .OpenAsync($"https://recepti.gotvach.bg/r-{id}")
                .GetAwaiter()
                .GetResult();

            if (document.StatusCode == HttpStatusCode.NotFound ||
                document.DocumentElement.OuterHtml.Contains("Тази страница не е намерена"))
            {
                throw new InvalidOperationException();
            }

            var recipe = new RecipeDto();

            var recipeNameAndCategory = document
                .QuerySelectorAll("#recEntity > div.breadcrumb")
                .Select(x => x.TextContent)
                .FirstOrDefault()
                .Split(" »", StringSplitOptions.RemoveEmptyEntries)
                .Reverse()
                .ToArray();

            recipe.CategoryName = recipeNameAndCategory[1];
            recipe.RecipeName = recipeNameAndCategory[0].TrimStart();

            var instructions = document.QuerySelectorAll(".text > p")
                .Select(x => x.TextContent)
                .ToList();

            var sb = new StringBuilder();
            foreach (var item in instructions)
            {
                sb.AppendLine(item);
            }

            recipe.Instructions = sb.ToString().TrimEnd();

            var timing = document.QuerySelectorAll(".mbox > .feat.small");

            if (timing.Length > 0)
            {
                var preparationTime = timing[0]
                    .TextContent
                    .Replace("Приготвяне", string.Empty)
                    .Replace(" мин.", string.Empty);

                var totalMintues = int.Parse(preparationTime);

                recipe.PreparationTime = TimeSpan.FromMinutes(totalMintues);
            }

            if (timing.Length > 1)
            {
                var cookingTime = timing[1]
                    .TextContent
                    .Replace("Готвене", string.Empty)
                    .Replace(" мин.", string.Empty);

                var totalMintues = int.Parse(cookingTime);

                recipe.CookingTime = TimeSpan.FromMinutes(totalMintues);
            }

            var portionsCount = document
                .QuerySelectorAll(".mbox > .feat > span")
                .LastOrDefault()
                .TextContent;

            recipe.PortionsCount = int.Parse(portionsCount);

            recipe.OriginalUrl = document.QuerySelector("#newsGal > div.image > img").GetAttribute("src");

            var ingredients = document.QuerySelectorAll(".products > ul > li");

            foreach (var item in ingredients)
            {
                recipe.Ingredients.Add(item.TextContent);
            }

            return recipe;
        }
    }
}
