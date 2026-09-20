using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Commands;

public record CreateReferenceEntityCommand<TEntity>(string Name) : IRequest<Result<Guid>>
    where TEntity : ReferenceEntity, new();
