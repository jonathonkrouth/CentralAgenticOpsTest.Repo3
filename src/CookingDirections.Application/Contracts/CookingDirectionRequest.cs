namespace CookingDirections.Application.Contracts;

public sealed record CookingDirectionRequest(
    int FoodProductId,
    int StepNumber,
    string Instruction,
    int? DurationMinutes,
    int? TemperatureFahrenheit);
