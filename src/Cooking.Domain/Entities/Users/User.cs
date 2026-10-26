using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Recipes;

namespace Cooking.Domain.Entities.Users;

public class User : BaseEntity
{
    public long TelegramId { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>null — пользователь пока не состоит ни в одной семье.</summary>
    public Guid? FamilyId { get; set; }
    public Family? Family { get; set; }

    /// <summary>
    /// Тема PWA (id темы или «auto»), чтобы она совпадала на телефоне и компьютере. null — пользователь ещё ни разу
    /// не выбирал: тогда PWA отправляет свой локальный выбор, а не сбрасывает его. Список тем знает только PWA.
    /// </summary>
    public string? Theme { get; set; }

    public ICollection<Recipe> CreatedRecipes { get; set; } = new List<Recipe>();
}
