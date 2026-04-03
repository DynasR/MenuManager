using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetAllAsync();
    Task<List<CategoryResponse>> GetTreeAsync();
    Task<CategoryResponse?> GetByIdAsync(int id);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);
    Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request);
    Task<bool> DeleteAsync(int id);
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryResponse>> GetAllAsync()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .ToListAsync();

        return categories.Select(MapToResponse).ToList();
    }

    public async Task<List<CategoryResponse>> GetTreeAsync()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .ToListAsync();

        return BuildTree(categories, null);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return category is null ? null : MapToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return null;

        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;

        await _db.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _db.Categories
            .Include(c => c.SubCategories)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null) return false;
        if (category.SubCategories.Count > 0) return false;
        if (category.Items.Count > 0) return false;

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return true;
    }

    private static CategoryResponse MapToResponse(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        ParentCategoryId = c.ParentCategoryId
    };

    private static List<CategoryResponse> BuildTree(List<Category> all, int? parentId)
    {
        return all
            .Where(c => c.ParentCategoryId == parentId)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                SubCategories = BuildTree(all, c.Id)
            })
            .ToList();
    }
}
