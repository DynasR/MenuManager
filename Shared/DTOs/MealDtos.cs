using MenuManager.Shared.Entities;

namespace MenuManager.Shared.DTOs;

public enum CreateMealError { DailyMenuNotFound, AlreadyExists }

public record CreateMealResult(
    MealResponse? Response,
    CreateMealError? Error
);

public class MealResponse
{
    public int Id { get; set; }
    public MealType MealType { get; set; }
    public int DailyMenuId { get; set; }
    public List<MealItemResponse> MealItems { get; set; } = [];
}

public class CreateMealRequest
{
    public MealType MealType { get; set; }
    public int DailyMenuId { get; set; }
}

public class UpdateMealRequest
{
    public MealType MealType { get; set; }
    public int DailyMenuId { get; set; }
}
