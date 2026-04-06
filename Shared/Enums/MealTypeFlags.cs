namespace MenuManager.Shared.Enums;

[Flags]
public enum MealTypeFlags
{
    None      = 0,
    Breakfast = 1,
    Lunch     = 2,
    Dinner    = 4,
    Snack     = 8,
}
