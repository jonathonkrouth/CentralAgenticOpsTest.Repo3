using CookingDirections.Application.Contracts;

namespace CookingDirections.Application.Services;

public interface ICookingDirectionService
{
    Task<IReadOnlyList<CookingDirectionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CookingDirectionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CookingDirectionDto> CreateAsync(CookingDirectionRequest request, CancellationToken cancellationToken = default);

    Task<CookingDirectionDto?> UpdateAsync(int id, CookingDirectionRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
