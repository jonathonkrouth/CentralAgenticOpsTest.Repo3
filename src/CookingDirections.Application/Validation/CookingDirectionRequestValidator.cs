using CookingDirections.Application.Contracts;
using CookingDirections.Application.Exceptions;

namespace CookingDirections.Application.Validation;

internal static class CookingDirectionRequestValidator
{
    private const int MaxStepNumber = 100;
    private const int InstructionMaxLength = 2000;
    private const int MaxDurationMinutes = 1440;
    private const int MaxTemperatureFahrenheit = 1000;

    public static void ValidateAndThrow(CookingDirectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();

        if (request.FoodProductId <= 0)
        {
            errors[nameof(request.FoodProductId)] = ["Food product id must be a positive number."];
        }

        if (request.StepNumber is < 1 or > MaxStepNumber)
        {
            errors[nameof(request.StepNumber)] = [$"Step number must be between 1 and {MaxStepNumber}."];
        }

        if (string.IsNullOrWhiteSpace(request.Instruction))
        {
            errors[nameof(request.Instruction)] = ["Instruction is required."];
        }
        else if (request.Instruction.Trim().Length > InstructionMaxLength)
        {
            errors[nameof(request.Instruction)] = [$"Instruction must be {InstructionMaxLength} characters or fewer."];
        }

        if (request.DurationMinutes is < 1 or > MaxDurationMinutes)
        {
            errors[nameof(request.DurationMinutes)] = [$"Duration must be between 1 and {MaxDurationMinutes} minutes."];
        }

        if (request.TemperatureFahrenheit is < 0 or > MaxTemperatureFahrenheit)
        {
            errors[nameof(request.TemperatureFahrenheit)] =
                [$"Temperature must be between 0 and {MaxTemperatureFahrenheit} degrees Fahrenheit."];
        }
        else if (request.TemperatureFahrenheit is not null && request.DurationMinutes is null)
        {
            errors[nameof(request.DurationMinutes)] = ["Duration is required when a temperature is specified."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
