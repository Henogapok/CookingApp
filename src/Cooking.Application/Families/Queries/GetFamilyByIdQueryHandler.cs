using FluentResults;
using MediatR;

namespace Cooking.Application.Families.Queries;

public class GetFamilyByIdQueryHandler(IFamilyRepositoryService repository)
    : IRequestHandler<GetFamilyByIdQuery, Result<FamilyDto>>
{
    public Task<Result<FamilyDto>> Handle(GetFamilyByIdQuery request, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(request.Id, cancellationToken);
}
