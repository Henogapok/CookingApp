namespace Cooking.Domain.Entities.Common;

/// <summary>
/// Базовый класс для справочников (seed-таблиц): только Id + Name, без CreatedAt/UpdatedAt.
/// </summary>
public abstract class ReferenceEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}
