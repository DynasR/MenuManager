namespace MenuManager.Client.Helpers;

public static class FrenchHolidays
{
    public static HashSet<DateOnly> GetHolidays(int year) =>
        new(GetNamedHolidays(year).Keys);

    public static Dictionary<DateOnly, string> GetNamedHolidays(int year)
    {
        var easter = ComputeEaster(year);

        return new Dictionary<DateOnly, string>
        {
            [new DateOnly(year, 1, 1)]   = "Jour de l'An",
            [new DateOnly(year, 5, 1)]   = "Fête du Travail",
            [new DateOnly(year, 5, 8)]   = "Victoire 1945",
            [new DateOnly(year, 7, 14)]  = "Fête nationale",
            [new DateOnly(year, 8, 15)]  = "Assomption",
            [new DateOnly(year, 11, 1)]  = "Toussaint",
            [new DateOnly(year, 11, 11)] = "Armistice",
            [new DateOnly(year, 12, 25)] = "Noël",
            [easter]                     = "Pâques",
            [easter.AddDays(1)]          = "Lundi de Pâques",
            [easter.AddDays(39)]         = "Ascension",
            [easter.AddDays(50)]         = "Lundi de Pentecôte"
        };
    }

    private static DateOnly ComputeEaster(int year)
    {
        // Butcher–Meeus algorithm
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }
}
