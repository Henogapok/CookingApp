using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Commands;

public record UpdateReferenceEntityCommand<TEntity>(Guid Id, string Name) : IRequest<Result>
    where TEntity : ReferenceEntity;
