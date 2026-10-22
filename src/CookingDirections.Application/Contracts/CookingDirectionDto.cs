using CookingDirections.Application.Domain;

namespace CookingDirections.Application.Contracts;

public sealed record CookingDirectionDto(
    int Id,
    int FoodProductId,
    int StepNumber,
    string Instruction,
    int? DurationMinutes,
    int? TemperatureFahrenheit,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static CookingDirectionDto FromEntity(CookingDirection cookingDirection) => new(
        cookingDirection.Id,
        cookingDirection.FoodProductId,
        cookingDirection.StepNumber,
        cookingDirection.Instruction,
        cookingDirection.DurationMinutes,
        cookingDirection.TemperatureFahrenheit,
        cookingDirection.CreatedAt,
        cookingDirection.UpdatedAt);
}
