using Cooking.Application.Common.Behaviors;
using Cooking.Application.MeasurementUnits.Commands;
using Cooking.Application.ReferenceData;
using Cooking.Application.ReferenceData.Commands;
using Cooking.Application.ReferenceData.Queries;
using Cooking.Domain.Entities.Common;
using Cooking.Domain.Entities.Ingredients;
using Cooking.Domain.Entities.Recipes;
using Cooking.Domain.Entities.Tags;
using FluentResults;
using FluentValidation;
using MediatR;
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

        // Аналогично для IRequestHandler<,>: TEntity вложен внутрь CreateReferenceEntityCommand<TEntity>
        // и т.п., поэтому MediatR не находит эти хендлеры при сканировании сборки как open generic.
        // Регистрируем явно на каждый из 5 "чистых" справочников; сам валидатор/хендлер остаётся общим.
        services.AddReferenceEntityValidators<SourceType>();
        services.AddReferenceEntityValidators<Complexity>();
        services.AddReferenceEntityValidators<IngredientCategory>();
        services.AddReferenceEntityValidators<DataSource>();
        services.AddReferenceEntityValidators<TagType>();

        services.AddReferenceEntityHandlers<SourceType>();
        services.AddReferenceEntityHandlers<Complexity>();
        services.AddReferenceEntityHandlers<IngredientCategory>();
        services.AddReferenceEntityHandlers<DataSource>();
        services.AddReferenceEntityHandlers<TagType>();

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

    private static IServiceCollection AddReferenceEntityHandlers<TEntity>(this IServiceCollection services)
        where TEntity : ReferenceEntity, new()
    {
        services.AddTransient<IRequestHandler<CreateReferenceEntityCommand<TEntity>, Result<Guid>>, CreateReferenceEntityCommandHandler<TEntity>>();
        services.AddTransient<IRequestHandler<UpdateReferenceEntityCommand<TEntity>, Result>, UpdateReferenceEntityCommandHandler<TEntity>>();
        services.AddTransient<IRequestHandler<DeleteReferenceEntityCommand<TEntity>, Result>, DeleteReferenceEntityCommandHandler<TEntity>>();
        services.AddTransient<IRequestHandler<GetReferenceEntitiesQuery<TEntity>, Result<List<ReferenceEntityDto>>>, GetReferenceEntitiesQueryHandler<TEntity>>();
        services.AddTransient<IRequestHandler<GetReferenceEntityByIdQuery<TEntity>, Result<ReferenceEntityDto>>, GetReferenceEntityByIdQueryHandler<TEntity>>();

        return services;
    }
}
