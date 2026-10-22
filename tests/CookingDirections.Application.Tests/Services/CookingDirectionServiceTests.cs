using CookingDirections.Application.Abstractions;
using CookingDirections.Application.Contracts;
using CookingDirections.Application.Domain;
using CookingDirections.Application.Exceptions;
using CookingDirections.Application.Services;
using NSubstitute;

namespace CookingDirections.Application.Tests.Services;

public sealed class CookingDirectionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 17, 45, 0, TimeSpan.Zero);

    private readonly ICookingDirectionRepository _repository = Substitute.For<ICookingDirectionRepository>();
    private readonly CookingDirectionService _service;

    public CookingDirectionServiceTests()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);
        _service = new CookingDirectionService(_repository, timeProvider);
    }

    public static TheoryData<CookingDirectionRequest, string> InvalidRequests => new()
    {
        { ValidRequest() with { FoodProductId = 0 }, nameof(CookingDirectionRequest.FoodProductId) },
        { ValidRequest() with { StepNumber = 0 }, nameof(CookingDirectionRequest.StepNumber) },
        { ValidRequest() with { StepNumber = 101 }, nameof(CookingDirectionRequest.StepNumber) },
        { ValidRequest() with { Instruction = " " }, nameof(CookingDirectionRequest.Instruction) },
        { ValidRequest() with { Instruction = new string('a', 2001) }, nameof(CookingDirectionRequest.Instruction) },
        { ValidRequest() with { DurationMinutes = 0 }, nameof(CookingDirectionRequest.DurationMinutes) },
        { ValidRequest() with { DurationMinutes = 1441 }, nameof(CookingDirectionRequest.DurationMinutes) },
        { ValidRequest() with { TemperatureFahrenheit = -1 }, nameof(CookingDirectionRequest.TemperatureFahrenheit) },
        { ValidRequest() with { TemperatureFahrenheit = 1001 }, nameof(CookingDirectionRequest.TemperatureFahrenheit) },
        { ValidRequest() with { DurationMinutes = null }, nameof(CookingDirectionRequest.DurationMinutes) },
    };

    [Fact]
    public async Task GetAllAsync_ReturnsMappedCookingDirections()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([CreateCookingDirection(1, stepNumber: 1), CreateCookingDirection(2, stepNumber: 2)]);

        var cookingDirections = await _service.GetAllAsync();

        Assert.Collection(
            cookingDirections,
            first => Assert.Equal(1, first.StepNumber),
            second => Assert.Equal(2, second.StepNumber));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCookingDirectionExists_ReturnsDto()
    {
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(CreateCookingDirection(3, stepNumber: 4));

        var cookingDirection = await _service.GetByIdAsync(3);

        Assert.NotNull(cookingDirection);
        Assert.Equal(3, cookingDirection.Id);
        Assert.Equal(4, cookingDirection.StepNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCookingDirectionDoesNotExist_ReturnsNull()
    {
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((CookingDirection?)null);

        var cookingDirection = await _service.GetByIdAsync(3);

        Assert.Null(cookingDirection);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsTrimmedInstruction()
    {
        _repository.CreateAsync(Arg.Any<CookingDirection>(), Arg.Any<CancellationToken>()).Returns(10);

        var cookingDirection = await _service.CreateAsync(ValidRequest() with { Instruction = "  Stir well.  " });

        Assert.Equal(10, cookingDirection.Id);
        Assert.Equal("Stir well.", cookingDirection.Instruction);
        Assert.Equal(Now, cookingDirection.CreatedAt);
        Assert.Equal(Now, cookingDirection.UpdatedAt);
        await _repository.Received(1).CreateAsync(
            Arg.Is<CookingDirection>(d => d.Instruction == "Stir well." && d.FoodProductId == 11 && d.CreatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutTemperatureOrDuration_IsValid()
    {
        _repository.CreateAsync(Arg.Any<CookingDirection>(), Arg.Any<CancellationToken>()).Returns(10);

        var cookingDirection = await _service.CreateAsync(
            ValidRequest() with { DurationMinutes = null, TemperatureFahrenheit = null });

        Assert.Null(cookingDirection.DurationMinutes);
        Assert.Null(cookingDirection.TemperatureFahrenheit);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task CreateAsync_WithInvalidRequest_ThrowsValidationException(
        CookingDirectionRequest request,
        string expectedErrorKey)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        Assert.Contains(expectedErrorKey, exception.Errors.Keys);
        await _repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WhenCookingDirectionExists_UpdatesFieldsAndPreservesCreatedAt()
    {
        var createdAt = Now.AddDays(-7);
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateCookingDirection(5, stepNumber: 1, createdAt));
        _repository.UpdateAsync(Arg.Any<CookingDirection>(), Arg.Any<CancellationToken>()).Returns(true);

        var cookingDirection = await _service.UpdateAsync(
            5,
            ValidRequest() with { StepNumber = 2, DurationMinutes = 25, TemperatureFahrenheit = 400 });

        Assert.NotNull(cookingDirection);
        Assert.Equal(2, cookingDirection.StepNumber);
        Assert.Equal(25, cookingDirection.DurationMinutes);
        Assert.Equal(400, cookingDirection.TemperatureFahrenheit);
        Assert.Equal(createdAt, cookingDirection.CreatedAt);
        Assert.Equal(Now, cookingDirection.UpdatedAt);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<CookingDirection>(d => d.Id == 5 && d.StepNumber == 2 && d.UpdatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenCookingDirectionDoesNotExist_ReturnsNullWithoutUpdating()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((CookingDirection?)null);

        var cookingDirection = await _service.UpdateAsync(5, ValidRequest());

        Assert.Null(cookingDirection);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ThrowsBeforeLoadingCookingDirection()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(5, ValidRequest() with { StepNumber = -1 }));

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReturnsRepositoryResult(bool deleted)
    {
        _repository.DeleteAsync(8, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _service.DeleteAsync(8);

        Assert.Equal(deleted, result);
    }

    private static CookingDirectionRequest ValidRequest() => new(11, 1, "Preheat the oven.", 10, 425);

    private static CookingDirection CreateCookingDirection(int id, int stepNumber, DateTimeOffset? createdAt = null) => new()
    {
        Id = id,
        FoodProductId = 11,
        StepNumber = stepNumber,
        Instruction = "Preheat the oven.",
        DurationMinutes = 10,
        TemperatureFahrenheit = 425,
        CreatedAt = createdAt ?? Now,
        UpdatedAt = createdAt ?? Now,
    };
}
