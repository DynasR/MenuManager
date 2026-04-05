using Microsoft.EntityFrameworkCore;
using MenuManager.Shared.Entities;

namespace MenuManager.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemSupplier> ItemSuppliers => Set<ItemSupplier>();
    public DbSet<DailyMenu> DailyMenus => Set<DailyMenu>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPT — Table Per Type inheritance
        modelBuilder.Entity<Party>().UseTptMappingStrategy();

        // ItemSupplier — composite key
        modelBuilder.Entity<ItemSupplier>()
            .HasKey(isp => new { isp.ItemId, isp.SupplierId });

        // Category — self-referencing hierarchy
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_Category_Name_NotEmpty", "trim(\"Name\") <> ''"));
        });

        // Item -> Category
        modelBuilder.Entity<Item>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // ItemSupplier -> Item
        modelBuilder.Entity<ItemSupplier>()
            .HasOne(isp => isp.Item)
            .WithMany(i => i.ItemSuppliers)
            .HasForeignKey(isp => isp.ItemId);

        // ItemSupplier -> Supplier
        modelBuilder.Entity<ItemSupplier>()
            .HasOne(isp => isp.Supplier)
            .WithMany(s => s.ItemSuppliers)
            .HasForeignKey(isp => isp.SupplierId);

        // DailyMenu -> Customer
        modelBuilder.Entity<DailyMenu>()
            .HasOne(dm => dm.Customer)
            .WithMany(c => c.DailyMenus)
            .HasForeignKey(dm => dm.CustomerId);

        // Decimal precision
        modelBuilder.Entity<ItemSupplier>()
            .Property(isp => isp.UnitPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<MealItem>()
            .Property(mi => mi.Quantity)
            .HasPrecision(10, 3);

        // RecipeIngredient — composite key
        modelBuilder.Entity<RecipeIngredient>()
            .HasKey(ri => new { ri.RecipeId, ri.ItemId });

        // RecipeIngredient — Quantity precision
        modelBuilder.Entity<RecipeIngredient>()
            .Property(ri => ri.Quantity)
            .HasPrecision(10, 3);

        // Meal — unique per type/day
        modelBuilder.Entity<Meal>()
            .HasIndex(m => new { m.DailyMenuId, m.MealType })
            .IsUnique();
    }
}
