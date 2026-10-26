using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Queries;

/// <summary>Семья с участниками — только для её участника; остальным NotFound.</summary>
public record GetFamilyByIdQuery(Guid Id, Guid UserId) : IRequest<Result<FamilyDto>>;
