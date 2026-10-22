namespace CookingDirections.Application.Domain;

public sealed class CookingDirection
{
    public int Id { get; set; }

    public int FoodProductId { get; set; }

    public int StepNumber { get; set; }

    public required string Instruction { get; set; }

    public int? DurationMinutes { get; set; }

    public int? TemperatureFahrenheit { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
