using Cooking.Application.Common.Behaviors;
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
            // Логирование снаружи — в лог попадают и ошибки валидации.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Все Command/Query и их валидаторы теперь конкретные (не generic), поэтому MediatR
        // и FluentValidation сами находят их сканированием сборки — без ручной регистрации
        // на каждый тип, как было нужно для generic ReferenceEntity<TEntity>-хендлеров.
        services.AddValidatorsFromAssembly(AssemblyReference.Assembly);

        return services;
    }
}
