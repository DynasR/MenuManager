namespace MenuManager.Shared.DTOs;

public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public List<CategoryResponse> SubCategories { get; set; } = [];
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
}
