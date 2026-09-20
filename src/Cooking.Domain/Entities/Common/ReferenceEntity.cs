namespace Cooking.Domain.Entities.Common;

/// <summary>
/// Базовый класс для справочников (seed-таблиц): только Id + Name, без CreatedAt/UpdatedAt.
/// </summary>
public abstract class ReferenceEntity
{
    public Guid Id { get; set; }

    // Не `required`: справочники создаются через generic `new TEntity { Name = ... }`
    // (см. Cooking.Application.ReferenceData), а `new()`-ограничение в C# 11+ несовместимо
    // с required-членами, даже когда они всегда заполняются через object initializer.
    // Обязательность на уровне БД — через EF-конфигурацию (ReferenceEntityConfiguration.IsRequired()).
    public string Name { get; set; } = string.Empty;
}
