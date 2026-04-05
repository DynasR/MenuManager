using MenuManager.Shared.Entities;

namespace MenuManager.Client.Helpers;

public static class MealTypeHelper
{
    public static string ToFrenchLabel(this MealType mt) => mt switch
    {
        MealType.Breakfast      => "Petit-déjeuner",
        MealType.MorningSnack   => "Collation matin",
        MealType.Lunch          => "Déjeuner",
        MealType.AfternoonSnack => "Collation après-midi",
        MealType.Dinner         => "Dîner",
        _                       => mt.ToString()
    };
}
