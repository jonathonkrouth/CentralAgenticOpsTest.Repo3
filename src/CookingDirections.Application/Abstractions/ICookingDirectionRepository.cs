using CookingDirections.Application.Domain;

namespace CookingDirections.Application.Abstractions;

public interface ICookingDirectionRepository
{
    Task<IReadOnlyList<CookingDirection>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CookingDirection?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CookingDirection cookingDirection, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(CookingDirection cookingDirection, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
