using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Categories.Any()) return;

        // --- Categories ---
        db.Categories.AddRange(
            new Category { Id = 1,  Name = "Biscuits & Goûters" },
            new Category { Id = 2,  Name = "Surgelés" },
            new Category { Id = 3,  Name = "Charcuterie" },
            new Category { Id = 4,  Name = "Viandes" },
            new Category { Id = 5,  Name = "Boissons" },
            new Category { Id = 6,  Name = "Fruits & Légumes" },
            new Category { Id = 7,  Name = "Épicerie Salée" },
            new Category { Id = 8,  Name = "Féculents", ParentCategoryId = 7 },
            new Category { Id = 9,  Name = "Produits Laitiers & Œufs" },
            new Category { Id = 10, Name = "Boulangerie & Viennoiserie" },
            new Category { Id = 11, Name = "Matières Grasses" },
            new Category { Id = 12, Name = "Entretien & Hygiène" },
            new Category { Id = 13, Name = "Sucre & Pâtisserie", ParentCategoryId = 1 }
        );
        db.SaveChanges();

        // --- Suppliers (Party TPT) ---
        db.Suppliers.AddRange(
            new Supplier { Id = 1, Name = "Carrefour City",  CompanyName = "Carrefour City",  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Supplier { Id = 2, Name = "Leclerc Drive",   CompanyName = "E.Leclerc Drive",  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();

        // --- Customer (Party TPT, Id = 3) ---
        var dummyHash = new byte[64];
        var dummySalt = new byte[128];
        db.Customers.Add(new Customer
        {
            Id = 3, Name = "Dynas", PasswordHash = dummyHash, PasswordSalt = dummySalt,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        // --- Items ---
        var now = DateTime.UtcNow;
        db.Items.AddRange(
            new Item { Id = 1,  Name = "Pims Orange",                  Unit = MeasurementUnit.Piece,       PackageSize = 1,    CategoryId = 1,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 2,  Name = "Pims Fraise",                  Unit = MeasurementUnit.Piece,       PackageSize = 1,    CategoryId = 1,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 3,  Name = "Glace Fraise",                 Unit = MeasurementUnit.Piece,       PackageSize = 6,    CategoryId = 2,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 4,  Name = "Jambon x4",                    Unit = MeasurementUnit.Piece,       PackageSize = 4,    CategoryId = 3,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 5,  Name = "Jambon x10",                   Unit = MeasurementUnit.Piece,       PackageSize = 10,   CategoryId = 3,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 6,  Name = "Lardons 100g",                 Unit = MeasurementUnit.Gram,        PackageSize = 100,  CategoryId = 3,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 7,  Name = "Lardons 200g",                 Unit = MeasurementUnit.Gram,        PackageSize = 200,  CategoryId = 3,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 8,  Name = "Curly Donuts",                 Unit = MeasurementUnit.Piece,       PackageSize = 1,    CategoryId = 1,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 9,  Name = "Jus d'orange Andros",          Unit = MeasurementUnit.Liter,       PackageSize = 1.5m, CategoryId = 5,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 10, Name = "Jus d'orange Sanguine Andros", Unit = MeasurementUnit.Liter,       PackageSize = 1.5m, CategoryId = 5,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 11, Name = "Jus de pomme Andros",          Unit = MeasurementUnit.Liter,       PackageSize = 1.5m, CategoryId = 5,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 12, Name = "Côte de porc échine 300g",     Unit = MeasurementUnit.Gram,        PackageSize = 300,  CategoryId = 4,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 13, Name = "Côte de porc échine 680g",     Unit = MeasurementUnit.Gram,        PackageSize = 680,  CategoryId = 4,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 14, Name = "Steak Haché 5%mg Férial",      Unit = MeasurementUnit.Gram,        PackageSize = 250,  CategoryId = 4,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 15, Name = "Saumon Fumé",                  Unit = MeasurementUnit.Piece,       PackageSize = 8,    CategoryId = 3,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 16, Name = "Citron vert",                  Unit = MeasurementUnit.Piece,       PackageSize = 1,    CategoryId = 6,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 17, Name = "Raviolis 4 fromages Turini",   Unit = MeasurementUnit.Gram,        PackageSize = 300,  CategoryId = 7,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 18, Name = "Sauce Tomate Heintz 420g",     Unit = MeasurementUnit.Gram,        PackageSize = 420,  CategoryId = 7,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 19, Name = "Sauce Tomate Heintz 500g",     Unit = MeasurementUnit.Gram,        PackageSize = 500,  CategoryId = 7,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 20, Name = "Smacks",                       Unit = MeasurementUnit.Gram,        PackageSize = 400,  CategoryId = 1,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 21, Name = "Brioches",                     Unit = MeasurementUnit.Piece,       PackageSize = 20,   CategoryId = 10, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 22, Name = "Chocolat Daims",               Unit = MeasurementUnit.Piece,       PackageSize = 3,    CategoryId = 1,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 23, Name = "Sucre Morceaux Daddy",         Unit = MeasurementUnit.Kilogram,    PackageSize = 1,    CategoryId = 13, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 24, Name = "Poulet (morceaux)",            Unit = MeasurementUnit.Piece,       PackageSize = 20,   CategoryId = 4,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 25, Name = "Viande hachée",                Unit = MeasurementUnit.Kilogram,    PackageSize = 1,    CategoryId = 4,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 26, Name = "Oignons",                     Unit = MeasurementUnit.Piece,       PackageSize = 6,    CategoryId = 6,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 27, Name = "Tomates",                     Unit = MeasurementUnit.Piece,       PackageSize = 3,    CategoryId = 6,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 28, Name = "Spaghetti",                   Unit = MeasurementUnit.Gram,        PackageSize = 500,  CategoryId = 8,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 29, Name = "Riz",                         Unit = MeasurementUnit.Gram,        PackageSize = 1000, CategoryId = 8,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 30, Name = "Œufs",                        Unit = MeasurementUnit.Piece,       PackageSize = 6,    CategoryId = 9,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 31, Name = "Petits pois surgelés",        Unit = MeasurementUnit.Gram,        PackageSize = 400,  CategoryId = 2,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 32, Name = "Sauce soja",                  Unit = MeasurementUnit.Milliliter,   PackageSize = 150,  CategoryId = 7,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 33, Name = "Nouilles chinoises",          Unit = MeasurementUnit.Gram,        PackageSize = 250,  CategoryId = 7,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 34, Name = "Bouillon volaille",           Unit = MeasurementUnit.Piece,       PackageSize = 8,    CategoryId = 7,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 35, Name = "Roquefort",                   Unit = MeasurementUnit.Gram,        PackageSize = 150,  CategoryId = 9,  CreatedAt = now, UpdatedAt = now },
            new Item { Id = 36, Name = "Bloc WC",                     Unit = MeasurementUnit.Piece,       PackageSize = 1,    CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 37, Name = "Papier toilette",             Unit = MeasurementUnit.Piece,       PackageSize = 6,    CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 38, Name = "Lingette WC",                 Unit = MeasurementUnit.Piece,       PackageSize = 70,   CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 39, Name = "Sopalin",                     Unit = MeasurementUnit.Piece,       PackageSize = 2,    CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 40, Name = "Liquide Vaisselle",           Unit = MeasurementUnit.Milliliter,   PackageSize = 500,  CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 41, Name = "Éponge vaisselle",            Unit = MeasurementUnit.Piece,       PackageSize = 2,    CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 42, Name = "Éponge carrée",               Unit = MeasurementUnit.Piece,       PackageSize = 2,    CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 43, Name = "Lingette entretien",          Unit = MeasurementUnit.Piece,       PackageSize = 72,   CategoryId = 12, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 44, Name = "Huile tournesol",             Unit = MeasurementUnit.Liter,       PackageSize = 1,    CategoryId = 11, IsStaple = true, MonthlyEstimate = 1,    CreatedAt = now, UpdatedAt = now },
            new Item { Id = 45, Name = "Huile d'olive",               Unit = MeasurementUnit.Liter,       PackageSize = 0.75m,CategoryId = 11, IsStaple = true, MonthlyEstimate = 0.5m, CreatedAt = now, UpdatedAt = now },
            new Item { Id = 46, Name = "Huile de friture",            Unit = MeasurementUnit.Liter,       PackageSize = 2,    CategoryId = 11, IsStaple = true, MonthlyEstimate = 1,    CreatedAt = now, UpdatedAt = now }
        );
        db.SaveChanges();

        // --- ItemSuppliers ---
        db.ItemSuppliers.AddRange(
            new ItemSupplier { ItemId = 1,  SupplierId = 1, UnitPrice = 3.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 2,  SupplierId = 2, UnitPrice = 2.95m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 3,  SupplierId = 1, UnitPrice = 4.50m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 3,  SupplierId = 2, UnitPrice = 3.80m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 4,  SupplierId = 1, UnitPrice = 2.80m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 5,  SupplierId = 2, UnitPrice = 5.50m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 6,  SupplierId = 1, UnitPrice = 1.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 7,  SupplierId = 1, UnitPrice = 1.85m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 7,  SupplierId = 2, UnitPrice = 1.55m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 8,  SupplierId = 1, UnitPrice = 1.65m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 8,  SupplierId = 2, UnitPrice = 1.35m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 9,  SupplierId = 2, UnitPrice = 2.85m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 10, SupplierId = 2, UnitPrice = 3.10m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 11, SupplierId = 2, UnitPrice = 2.65m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 12, SupplierId = 1, UnitPrice = 4.20m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 13, SupplierId = 2, UnitPrice = 7.50m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 14, SupplierId = 2, UnitPrice = 2.95m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 15, SupplierId = 2, UnitPrice = 7.80m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 16, SupplierId = 1, UnitPrice = 0.65m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 16, SupplierId = 2, UnitPrice = 0.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 17, SupplierId = 2, UnitPrice = 1.95m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 18, SupplierId = 2, UnitPrice = 1.75m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 19, SupplierId = 2, UnitPrice = 1.95m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 20, SupplierId = 1, UnitPrice = 3.80m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 20, SupplierId = 2, UnitPrice = 3.20m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 21, SupplierId = 1, UnitPrice = 3.50m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 21, SupplierId = 2, UnitPrice = 2.90m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 22, SupplierId = 2, UnitPrice = 2.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 23, SupplierId = 1, UnitPrice = 2.20m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 23, SupplierId = 2, UnitPrice = 1.85m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 24, SupplierId = 2, UnitPrice = 6.50m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 25, SupplierId = 2, UnitPrice = 8.50m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 26, SupplierId = 2, UnitPrice = 1.20m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 27, SupplierId = 2, UnitPrice = 1.85m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 28, SupplierId = 2, UnitPrice = 0.95m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 29, SupplierId = 2, UnitPrice = 1.65m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 30, SupplierId = 1, UnitPrice = 2.20m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 30, SupplierId = 2, UnitPrice = 1.85m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 31, SupplierId = 2, UnitPrice = 1.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 32, SupplierId = 2, UnitPrice = 1.95m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 33, SupplierId = 2, UnitPrice = 1.85m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 34, SupplierId = 1, UnitPrice = 1.65m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 34, SupplierId = 2, UnitPrice = 1.35m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 35, SupplierId = 1, UnitPrice = 3.80m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 35, SupplierId = 2, UnitPrice = 3.10m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 36, SupplierId = 1, UnitPrice = 2.20m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 36, SupplierId = 2, UnitPrice = 1.65m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 37, SupplierId = 1, UnitPrice = 3.80m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 37, SupplierId = 2, UnitPrice = 3.10m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 38, SupplierId = 2, UnitPrice = 2.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 39, SupplierId = 1, UnitPrice = 2.80m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 39, SupplierId = 2, UnitPrice = 2.25m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 40, SupplierId = 1, UnitPrice = 1.85m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 40, SupplierId = 2, UnitPrice = 1.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 41, SupplierId = 1, UnitPrice = 1.45m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 41, SupplierId = 2, UnitPrice = 1.15m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 42, SupplierId = 2, UnitPrice = 1.25m, IsAvailable = true, UpdatedAt = now },
            new ItemSupplier { ItemId = 43, SupplierId = 2, UnitPrice = 2.85m, IsAvailable = true, UpdatedAt = now }
        );
        db.SaveChanges();

        // --- Recipes ---
        db.Recipes.AddRange(
            new Recipe { Id = 1, Name = "Spaghetti Bolognaise",            BaseServings = 4 },
            new Recipe { Id = 2, Name = "Riz cantonnais",                  BaseServings = 4 },
            new Recipe { Id = 3, Name = "Soupe chinoise",                  BaseServings = 4 },
            new Recipe { Id = 4, Name = "Raviolis sauce tomate",           BaseServings = 2 },
            new Recipe { Id = 5, Name = "Sandwich Roquefort Jambon",       BaseServings = 1 }
        );
        db.SaveChanges();

        // --- RecipeIngredients ---
        db.RecipeIngredients.AddRange(
            // Spaghetti Bolognaise
            new RecipeIngredient { RecipeId = 1, ItemId = 28, Quantity = 400 },   // Spaghetti 400g
            new RecipeIngredient { RecipeId = 1, ItemId = 25, Quantity = 0.4m },  // Viande hachée 0.4kg
            new RecipeIngredient { RecipeId = 1, ItemId = 19, Quantity = 500 },   // Sauce Tomate 500g
            new RecipeIngredient { RecipeId = 1, ItemId = 26, Quantity = 1 },     // Oignons 1pc
            // Riz cantonnais
            new RecipeIngredient { RecipeId = 2, ItemId = 29, Quantity = 400 },   // Riz 400g
            new RecipeIngredient { RecipeId = 2, ItemId = 7,  Quantity = 200 },   // Lardons 200g
            new RecipeIngredient { RecipeId = 2, ItemId = 30, Quantity = 3 },     // Œufs 3pc
            new RecipeIngredient { RecipeId = 2, ItemId = 31, Quantity = 150 },   // Petits pois 150g
            new RecipeIngredient { RecipeId = 2, ItemId = 32, Quantity = 50 },    // Sauce soja 50ml
            // Soupe chinoise
            new RecipeIngredient { RecipeId = 3, ItemId = 33, Quantity = 250 },   // Nouilles chinoises 250g
            new RecipeIngredient { RecipeId = 3, ItemId = 34, Quantity = 2 },     // Bouillon volaille 2pc
            new RecipeIngredient { RecipeId = 3, ItemId = 32, Quantity = 30 },    // Sauce soja 30ml
            // Raviolis sauce tomate
            new RecipeIngredient { RecipeId = 4, ItemId = 17, Quantity = 300 },   // Raviolis 300g
            new RecipeIngredient { RecipeId = 4, ItemId = 18, Quantity = 200 },   // Sauce Tomate 420g → 200g
            // Sandwich Roquefort Jambon
            new RecipeIngredient { RecipeId = 5, ItemId = 21, Quantity = 2 },     // Brioches 2pc
            new RecipeIngredient { RecipeId = 5, ItemId = 35, Quantity = 25 },    // Roquefort 25g
            new RecipeIngredient { RecipeId = 5, ItemId = 4,  Quantity = 1 }      // Jambon x4 1pc
        );
        db.SaveChanges();

        // --- DailyMenus (on-demand, only days with items) ---
        db.DailyMenus.AddRange(
            new DailyMenu { Id = 1,  Date = new DateOnly(2026, 4, 7),  CustomerId = 3 },
            new DailyMenu { Id = 2,  Date = new DateOnly(2026, 4, 9),  CustomerId = 3 },
            new DailyMenu { Id = 3,  Date = new DateOnly(2026, 4, 10), CustomerId = 3 },
            new DailyMenu { Id = 4,  Date = new DateOnly(2026, 4, 12), CustomerId = 3 },
            new DailyMenu { Id = 5,  Date = new DateOnly(2026, 4, 14), CustomerId = 3 },
            new DailyMenu { Id = 6,  Date = new DateOnly(2026, 4, 16), CustomerId = 3 },
            new DailyMenu { Id = 7,  Date = new DateOnly(2026, 4, 18), CustomerId = 3 },
            new DailyMenu { Id = 8,  Date = new DateOnly(2026, 4, 21), CustomerId = 3 },
            new DailyMenu { Id = 9,  Date = new DateOnly(2026, 4, 23), CustomerId = 3 },
            new DailyMenu { Id = 10, Date = new DateOnly(2026, 4, 25), CustomerId = 3 }
        );
        db.SaveChanges();

        // --- Meals ---
        db.Meals.AddRange(
            new Meal { Id = 1,  MealType = MealType.Lunch,          DailyMenuId = 1 },  // 07/04 Lunch
            new Meal { Id = 2,  MealType = MealType.AfternoonSnack, DailyMenuId = 1 },  // 07/04 AfternoonSnack
            new Meal { Id = 3,  MealType = MealType.Lunch,          DailyMenuId = 2 },  // 09/04 Lunch
            new Meal { Id = 4,  MealType = MealType.Breakfast,      DailyMenuId = 3 },  // 10/04 Breakfast
            new Meal { Id = 5,  MealType = MealType.Lunch,          DailyMenuId = 4 },  // 12/04 Lunch
            new Meal { Id = 6,  MealType = MealType.Dinner,         DailyMenuId = 5 },  // 14/04 Dinner
            new Meal { Id = 7,  MealType = MealType.MorningSnack,   DailyMenuId = 6 },  // 16/04 MorningSnack
            new Meal { Id = 8,  MealType = MealType.Lunch,          DailyMenuId = 7 },  // 18/04 Lunch
            new Meal { Id = 9,  MealType = MealType.Dinner,         DailyMenuId = 8 },  // 21/04 Dinner
            new Meal { Id = 10, MealType = MealType.AfternoonSnack, DailyMenuId = 9 },  // 23/04 AfternoonSnack
            new Meal { Id = 11, MealType = MealType.Lunch,          DailyMenuId = 10 }  // 25/04 Lunch
        );
        db.SaveChanges();

        // --- MealItems (Recipe → RecipeId set, Item → ItemId set) ---
        db.MealItems.AddRange(
            new MealItem { Id = 1,  MealId = 1,  RecipeId = 1,  Quantity = 1, Servings = 4 },  // Spaghetti Bolognaise
            new MealItem { Id = 2,  MealId = 2,  ItemId = 3,   Quantity = 2, Servings = 1 },   // Glace Fraise x2
            new MealItem { Id = 3,  MealId = 3,  RecipeId = 4,  Quantity = 1, Servings = 2 },  // Raviolis
            new MealItem { Id = 4,  MealId = 4,  ItemId = 1,   Quantity = 3, Servings = 1 },   // Pims Orange x3
            new MealItem { Id = 5,  MealId = 4,  ItemId = 9,   Quantity = 1, Servings = 1 },   // Jus d'orange Andros x1
            new MealItem { Id = 6,  MealId = 5,  RecipeId = 2,  Quantity = 1, Servings = 4 },  // Riz cantonnais
            new MealItem { Id = 7,  MealId = 6,  RecipeId = 3,  Quantity = 1, Servings = 4 },  // Soupe chinoise
            new MealItem { Id = 8,  MealId = 7,  ItemId = 20,  Quantity = 1, Servings = 1 },   // Smacks x1
            new MealItem { Id = 9,  MealId = 8,  RecipeId = 5,  Quantity = 1, Servings = 2 },  // Sandwich Roquefort Jambon
            new MealItem { Id = 10, MealId = 9,  RecipeId = 1,  Quantity = 1, Servings = 4 },  // Spaghetti Bolognaise
            new MealItem { Id = 11, MealId = 10, ItemId = 22,  Quantity = 1, Servings = 1 },   // Chocolat Daims x1
            new MealItem { Id = 12, MealId = 11, RecipeId = 2,  Quantity = 1, Servings = 4 }   // Riz cantonnais
        );
        db.SaveChanges();

        // --- Reset PostgreSQL sequences to avoid PK conflicts on next insert ---
        var sequences = new (string Table, string Column)[]
        {
            ("Categories",  "Id"),
            ("Party",       "Id"),
            ("Items",       "Id"),
            ("Recipes",     "Id"),
            ("DailyMenus",  "Id"),
            ("Meals",       "Id"),
            ("MealItems",   "Id")
        };

        foreach (var (table, column) in sequences)
        {
            var sql = string.Format(
                "SELECT setval(pg_get_serial_sequence('\"{0}\"', '{1}'), (SELECT COALESCE(MAX(\"{1}\"), 0) FROM \"{0}\"))",
                table, column);
            db.Database.ExecuteSqlRaw(sql);
        }
    }
}
