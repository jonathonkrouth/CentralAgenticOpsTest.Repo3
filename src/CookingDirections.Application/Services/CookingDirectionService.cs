using CookingDirections.Application.Abstractions;
using CookingDirections.Application.Contracts;
using CookingDirections.Application.Domain;
using CookingDirections.Application.Validation;

namespace CookingDirections.Application.Services;

public sealed class CookingDirectionService(ICookingDirectionRepository repository, TimeProvider timeProvider)
    : ICookingDirectionService
{
    public async Task<IReadOnlyList<CookingDirectionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cookingDirections = await repository.GetAllAsync(cancellationToken);
        return cookingDirections.Select(CookingDirectionDto.FromEntity).ToList();
    }

    public async Task<CookingDirectionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cookingDirection = await repository.GetByIdAsync(id, cancellationToken);
        return cookingDirection is null ? null : CookingDirectionDto.FromEntity(cookingDirection);
    }

    public async Task<CookingDirectionDto> CreateAsync(CookingDirectionRequest request, CancellationToken cancellationToken = default)
    {
        CookingDirectionRequestValidator.ValidateAndThrow(request);

        var now = timeProvider.GetUtcNow();
        var cookingDirection = new CookingDirection
        {
            FoodProductId = request.FoodProductId,
            StepNumber = request.StepNumber,
            Instruction = request.Instruction.Trim(),
            DurationMinutes = request.DurationMinutes,
            TemperatureFahrenheit = request.TemperatureFahrenheit,
            CreatedAt = now,
            UpdatedAt = now,
        };

        cookingDirection.Id = await repository.CreateAsync(cookingDirection, cancellationToken);
        return CookingDirectionDto.FromEntity(cookingDirection);
    }

    public async Task<CookingDirectionDto?> UpdateAsync(int id, CookingDirectionRequest request, CancellationToken cancellationToken = default)
    {
        CookingDirectionRequestValidator.ValidateAndThrow(request);

        var cookingDirection = await repository.GetByIdAsync(id, cancellationToken);
        if (cookingDirection is null)
        {
            return null;
        }

        cookingDirection.FoodProductId = request.FoodProductId;
        cookingDirection.StepNumber = request.StepNumber;
        cookingDirection.Instruction = request.Instruction.Trim();
        cookingDirection.DurationMinutes = request.DurationMinutes;
        cookingDirection.TemperatureFahrenheit = request.TemperatureFahrenheit;
        cookingDirection.UpdatedAt = timeProvider.GetUtcNow();

        var updated = await repository.UpdateAsync(cookingDirection, cancellationToken);
        return updated ? CookingDirectionDto.FromEntity(cookingDirection) : null;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);
}
