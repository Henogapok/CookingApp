namespace Cooking.Application.Nutrition;

/// <summary>КБЖУ: ккал, белки, жиры, углеводы (г).</summary>
public record NutritionFacts(decimal Calories, decimal Protein, decimal Fat, decimal Carbs)
{
    public static readonly NutritionFacts Zero = new(0, 0, 0, 0);

    public static NutritionFacts operator +(NutritionFacts a, NutritionFacts b) =>
        new(a.Calories + b.Calories, a.Protein + b.Protein, a.Fat + b.Fat, a.Carbs + b.Carbs);

    public NutritionFacts Scale(decimal factor) =>
        new(Calories * factor, Protein * factor, Fat * factor, Carbs * factor);

    /// <summary>КБЖУ, если заданы все четыре значения (продукт с КБЖУ с упаковки), иначе null — тогда считаем по каталогу.</summary>
    public static NutritionFacts? AllOrNone(decimal? calories, decimal? protein, decimal? fat, decimal? carbs) =>
        calories is { } k && protein is { } b && fat is { } f && carbs is { } c ? new NutritionFacts(k, b, f, c) : null;
}
