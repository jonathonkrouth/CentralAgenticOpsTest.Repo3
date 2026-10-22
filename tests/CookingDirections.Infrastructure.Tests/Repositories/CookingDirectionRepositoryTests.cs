using System.Data;
using CookingDirections.Application.Domain;
using CookingDirections.Infrastructure.Data;
using CookingDirections.Infrastructure.Repositories;
using CookingDirections.Infrastructure.Tests.Fakes;
using NSubstitute;

namespace CookingDirections.Infrastructure.Tests.Repositories;

public sealed class CookingDirectionRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 6, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 5, 7, 18, 20, 0, TimeSpan.Zero);

    private readonly FakeDbCommand _command = new();
    private readonly CookingDirectionRepository _repository;

    public CookingDirectionRepositoryTests()
    {
        var connectionFactory = Substitute.For<IDbConnectionFactory>();
        connectionFactory.CreateConnection().Returns(_ => new FakeDbConnection(_command));
        _repository = new CookingDirectionRepository(connectionFactory);
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryRowIncludingNullableColumns()
    {
        _command.ReaderResult = CreateResultTable(
            CreateCookingDirection(1, stepNumber: 1, durationMinutes: null, temperatureFahrenheit: null),
            CreateCookingDirection(2, stepNumber: 2, durationMinutes: 14, temperatureFahrenheit: 425));

        var cookingDirections = await _repository.GetAllAsync();

        Assert.Contains("FROM dbo.CookingDirections", _command.CommandText);
        Assert.Contains("ORDER BY FoodProductId, StepNumber", _command.CommandText);
        Assert.Collection(
            cookingDirections,
            first =>
            {
                Assert.Equal(1, first.Id);
                Assert.Equal(1, first.StepNumber);
                Assert.Null(first.DurationMinutes);
                Assert.Null(first.TemperatureFahrenheit);
            },
            second =>
            {
                Assert.Equal(2, second.Id);
                Assert.Equal(2, second.StepNumber);
                Assert.Equal(14, second.DurationMinutes);
                Assert.Equal(425, second.TemperatureFahrenheit);
            });
    }

    [Fact]
    public async Task GetByIdAsync_WhenRowExists_ReturnsMappedCookingDirection()
    {
        _command.ReaderResult = CreateResultTable(
            CreateCookingDirection(7, stepNumber: 3, durationMinutes: 5, temperatureFahrenheit: 350));

        var cookingDirection = await _repository.GetByIdAsync(7);

        Assert.NotNull(cookingDirection);
        Assert.Equal(7, cookingDirection.Id);
        Assert.Equal(11, cookingDirection.FoodProductId);
        Assert.Equal(3, cookingDirection.StepNumber);
        Assert.Equal("Bake until golden brown.", cookingDirection.Instruction);
        Assert.Equal(5, cookingDirection.DurationMinutes);
        Assert.Equal(350, cookingDirection.TemperatureFahrenheit);
        Assert.Equal(CreatedAt, cookingDirection.CreatedAt);
        Assert.Equal(UpdatedAt, cookingDirection.UpdatedAt);
        Assert.Contains("WHERE Id = @Id", _command.CommandText);
        Assert.Equal(7, _command.Parameters["@Id"].Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoRowExists_ReturnsNull()
    {
        _command.ReaderResult = CreateResultTable();

        var cookingDirection = await _repository.GetByIdAsync(99);

        Assert.Null(cookingDirection);
    }

    [Fact]
    public async Task CreateAsync_SendsParametersAndReturnsNewId()
    {
        _command.ScalarResult = 42;

        var id = await _repository.CreateAsync(
            CreateCookingDirection(0, stepNumber: 1, durationMinutes: null, temperatureFahrenheit: null));

        Assert.Equal(42, id);
        Assert.Contains("INSERT INTO dbo.CookingDirections", _command.CommandText);
        Assert.Equal(11, _command.Parameters["@FoodProductId"].Value);
        Assert.Equal(1, _command.Parameters["@StepNumber"].Value);
        Assert.Equal("Bake until golden brown.", _command.Parameters["@Instruction"].Value);
        Assert.Equal(DBNull.Value, _command.Parameters["@DurationMinutes"].Value);
        Assert.Equal(DBNull.Value, _command.Parameters["@TemperatureFahrenheit"].Value);
        Assert.Equal(CreatedAt, _command.Parameters["@CreatedAt"].Value);
        Assert.Equal(UpdatedAt, _command.Parameters["@UpdatedAt"].Value);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task UpdateAsync_ReturnsWhetherARowWasUpdated(int rowsAffected, bool expected)
    {
        _command.NonQueryResult = rowsAffected;

        var updated = await _repository.UpdateAsync(
            CreateCookingDirection(5, stepNumber: 2, durationMinutes: 20, temperatureFahrenheit: 375));

        Assert.Equal(expected, updated);
        Assert.Contains("UPDATE dbo.CookingDirections", _command.CommandText);
        Assert.Equal(5, _command.Parameters["@Id"].Value);
        Assert.Equal(20, _command.Parameters["@DurationMinutes"].Value);
        Assert.Equal(375, _command.Parameters["@TemperatureFahrenheit"].Value);
        Assert.False(_command.Parameters.Contains("@CreatedAt"));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task DeleteAsync_ReturnsWhetherARowWasDeleted(int rowsAffected, bool expected)
    {
        _command.NonQueryResult = rowsAffected;

        var deleted = await _repository.DeleteAsync(3);

        Assert.Equal(expected, deleted);
        Assert.Contains("DELETE FROM dbo.CookingDirections", _command.CommandText);
        Assert.Equal(3, _command.Parameters["@Id"].Value);
    }

    private static CookingDirection CreateCookingDirection(
        int id,
        int stepNumber,
        int? durationMinutes,
        int? temperatureFahrenheit) => new()
    {
        Id = id,
        FoodProductId = 11,
        StepNumber = stepNumber,
        Instruction = "Bake until golden brown.",
        DurationMinutes = durationMinutes,
        TemperatureFahrenheit = temperatureFahrenheit,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };

    private static DataTable CreateResultTable(params CookingDirection[] cookingDirections)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("FoodProductId", typeof(int));
        table.Columns.Add("StepNumber", typeof(int));
        table.Columns.Add("Instruction", typeof(string));
        table.Columns.Add("DurationMinutes", typeof(int));
        table.Columns.Add("TemperatureFahrenheit", typeof(int));
        table.Columns.Add("CreatedAt", typeof(DateTimeOffset));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));

        foreach (var cookingDirection in cookingDirections)
        {
            table.Rows.Add(
                cookingDirection.Id,
                cookingDirection.FoodProductId,
                cookingDirection.StepNumber,
                cookingDirection.Instruction,
                (object?)cookingDirection.DurationMinutes ?? DBNull.Value,
                (object?)cookingDirection.TemperatureFahrenheit ?? DBNull.Value,
                cookingDirection.CreatedAt,
                cookingDirection.UpdatedAt);
        }

        return table;
    }
}
