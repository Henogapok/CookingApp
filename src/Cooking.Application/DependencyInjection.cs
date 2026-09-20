using Cooking.Application.Common.Behaviors;
using Cooking.Application.MeasurementUnits.Commands;
using Cooking.Application.ReferenceData.Commands;
using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // IValidator<CreateReferenceEntityCommand<TEntity>> нельзя зарегистрировать одной open-generic записью:
        // .NET DI сопоставляет открытые generic-типы 1:1 по параметрам (как IRequestHandler<,> у MediatR),
        // а тут TEntity вложен внутрь CreateReferenceEntityCommand<TEntity> — типы не совпадают по форме.
        // Поэтому регистрируем явно на каждый из 5 "чистых" справочников; сам валидатор/хендлер остаётся общим.
        services.AddReferenceEntityValidators<SourceType>();
        services.AddReferenceEntityValidators<Complexity>();
        services.AddReferenceEntityValidators<IngredientCategory>();
        services.AddReferenceEntityValidators<DataSource>();
        services.AddReferenceEntityValidators<TagType>();

        services.AddTransient<IValidator<CreateMeasurementUnitCommand>, CreateMeasurementUnitCommandValidator>();
        services.AddTransient<IValidator<UpdateMeasurementUnitCommand>, UpdateMeasurementUnitCommandValidator>();

        return services;
    }

    private static IServiceCollection AddReferenceEntityValidators<TEntity>(this IServiceCollection services)
        where TEntity : ReferenceEntity, new()
    {
        services.AddTransient<IValidator<CreateReferenceEntityCommand<TEntity>>, CreateReferenceEntityCommandValidator<TEntity>>();
        services.AddTransient<IValidator<UpdateReferenceEntityCommand<TEntity>>, UpdateReferenceEntityCommandValidator<TEntity>>();

        return services;
    }
}
