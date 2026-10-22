using System.Data;
using System.Globalization;
using CookingDirections.Application.Abstractions;
using CookingDirections.Application.Domain;
using CookingDirections.Infrastructure.Data;
using Dapper;

namespace CookingDirections.Infrastructure.Repositories;

public sealed class CookingDirectionRepository(IDbConnectionFactory connectionFactory) : ICookingDirectionRepository
{
    private const string SelectSql = """
        SELECT Id, FoodProductId, StepNumber, Instruction, DurationMinutes, TemperatureFahrenheit, CreatedAt, UpdatedAt
        FROM dbo.CookingDirections
        """;

    private const string InsertSql = """
        INSERT INTO dbo.CookingDirections (FoodProductId, StepNumber, Instruction, DurationMinutes, TemperatureFahrenheit, CreatedAt, UpdatedAt)
        OUTPUT INSERTED.Id
        VALUES (@FoodProductId, @StepNumber, @Instruction, @DurationMinutes, @TemperatureFahrenheit, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateSql = """
        UPDATE dbo.CookingDirections
        SET FoodProductId = @FoodProductId,
            StepNumber = @StepNumber,
            Instruction = @Instruction,
            DurationMinutes = @DurationMinutes,
            TemperatureFahrenheit = @TemperatureFahrenheit,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    private const string DeleteSql = "DELETE FROM dbo.CookingDirections WHERE Id = @Id;";

    public async Task<IReadOnlyList<CookingDirection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<CookingDirectionRow>(new CommandDefinition(
            SelectSql + " ORDER BY FoodProductId, StepNumber;",
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<CookingDirection?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddParameter(parameters, "@Id", DbType.Int32, id);

        var row = await connection.QueryFirstOrDefaultAsync<CookingDirectionRow>(new CommandDefinition(
            SelectSql + " WHERE Id = @Id;",
            parameters,
            cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<int> CreateAsync(CookingDirection cookingDirection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cookingDirection);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddWritableParameters(parameters, cookingDirection);
        AddParameter(parameters, "@CreatedAt", DbType.DateTimeOffset, cookingDirection.CreatedAt);

        var newId = await connection.ExecuteScalarAsync<object>(new CommandDefinition(
            InsertSql,
            parameters,
            cancellationToken: cancellationToken));
        return Convert.ToInt32(newId, CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateAsync(CookingDirection cookingDirection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cookingDirection);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddParameter(parameters, "@Id", DbType.Int32, cookingDirection.Id);
        AddWritableParameters(parameters, cookingDirection);

        return await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            parameters,
            cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddParameter(parameters, "@Id", DbType.Int32, id);

        return await connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            parameters,
            cancellationToken: cancellationToken)) > 0;
    }

    private static void AddWritableParameters(DynamicParameters parameters, CookingDirection cookingDirection)
    {
        AddParameter(parameters, "@FoodProductId", DbType.Int32, cookingDirection.FoodProductId);
        AddParameter(parameters, "@StepNumber", DbType.Int32, cookingDirection.StepNumber);
        AddParameter(parameters, "@Instruction", DbType.String, cookingDirection.Instruction);
        AddParameter(parameters, "@DurationMinutes", DbType.Int32, cookingDirection.DurationMinutes);
        AddParameter(parameters, "@TemperatureFahrenheit", DbType.Int32, cookingDirection.TemperatureFahrenheit);
        AddParameter(parameters, "@UpdatedAt", DbType.DateTimeOffset, cookingDirection.UpdatedAt);
    }

    private static void AddParameter(DynamicParameters parameters, string name, DbType dbType, object? value) =>
        parameters.Add(name, value ?? DBNull.Value, dbType);

    private sealed class CookingDirectionRow
    {
        public int Id { get; set; }

        public int FoodProductId { get; set; }

        public int StepNumber { get; set; }

        public string Instruction { get; set; } = string.Empty;

        public int? DurationMinutes { get; set; }

        public int? TemperatureFahrenheit { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public CookingDirection ToDomain() => new()
        {
            Id = Id,
            FoodProductId = FoodProductId,
            StepNumber = StepNumber,
            Instruction = Instruction,
            DurationMinutes = DurationMinutes,
            TemperatureFahrenheit = TemperatureFahrenheit,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }
}
