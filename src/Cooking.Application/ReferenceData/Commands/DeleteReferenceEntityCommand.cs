using Cooking.Domain.Entities.Common;
using FluentResults;
using MediatR;

namespace Cooking.Application.ReferenceData.Commands;

public record DeleteReferenceEntityCommand<TEntity>(Guid Id) : IRequest<Result>
    where TEntity : ReferenceEntity;
