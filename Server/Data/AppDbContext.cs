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
    public DbSet<MenuPlan> MenuPlans => Set<MenuPlan>();
    public DbSet<DayPlan> DayPlans => Set<DayPlan>();
    public DbSet<MealSlot> MealSlots => Set<MealSlot>();
    public DbSet<MealSlotItem> MealSlotItems => Set<MealSlotItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPT — Table Per Type inheritance
        modelBuilder.Entity<Party>().UseTptMappingStrategy();

        // ItemSupplier — composite key
        modelBuilder.Entity<ItemSupplier>()
            .HasKey(isp => new { isp.ItemId, isp.SupplierId });

        // Category — self-referencing hierarchy
        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Decimal precision
        modelBuilder.Entity<ItemSupplier>()
            .Property(isp => isp.UnitPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<MealSlotItem>()
            .Property(msi => msi.Quantity)
            .HasPrecision(10, 3);

        // MealSlot — unique per type/day
        modelBuilder.Entity<MealSlot>()
            .HasIndex(ms => new { ms.DayPlanId, ms.MealType })
            .IsUnique();
    }
}