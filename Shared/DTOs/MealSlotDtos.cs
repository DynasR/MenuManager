using MenuManager.Shared.Entities;

namespace MenuManager.Shared.DTOs;

public enum CreateMealSlotError { DayPlanNotFound, AlreadyExists }

public record CreateMealSlotResult(
    MealSlotResponse? Response,
    CreateMealSlotError? Error
);

public class CreateMealSlotRequest
{
    public MealType MealType { get; set; }
    public int DayPlanId { get; set; }
}

public class UpdateMealSlotRequest
{
    public MealType MealType { get; set; }
    public int DayPlanId { get; set; }
}
