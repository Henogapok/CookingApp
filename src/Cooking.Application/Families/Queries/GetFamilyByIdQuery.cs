using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Queries;

public record GetFamilyByIdQuery(Guid Id) : IRequest<Result<FamilyDto>>;
