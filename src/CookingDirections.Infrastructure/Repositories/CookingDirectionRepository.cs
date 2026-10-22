using System.Data;
using System.Globalization;
using Dapper;
using CookingDirections.Application.Abstractions;
using CookingDirections.Application.Domain;
using CookingDirections.Infrastructure.Data;

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

        return rows.Select(Map).ToList();
    }

    public async Task<CookingDirection?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Int32);

        var row = await connection.QueryFirstOrDefaultAsync<CookingDirectionRow>(new CommandDefinition(
            SelectSql + " WHERE Id = @Id;",
            parameters,
            cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<int> CreateAsync(CookingDirection cookingDirection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cookingDirection);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = CreateWritableParameters(cookingDirection);
        parameters.Add("@CreatedAt", cookingDirection.CreatedAt, DbType.DateTimeOffset);

        var newId = await connection.ExecuteScalarAsync<object?>(new CommandDefinition(
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
        parameters.Add("@Id", cookingDirection.Id, DbType.Int32);
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
        parameters.Add("@Id", id, DbType.Int32);

        return await connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            parameters,
            cancellationToken: cancellationToken)) > 0;
    }

    private static DynamicParameters CreateWritableParameters(CookingDirection cookingDirection)
    {
        var parameters = new DynamicParameters();
        AddWritableParameters(parameters, cookingDirection);
        return parameters;
    }

    private static void AddWritableParameters(DynamicParameters parameters, CookingDirection cookingDirection)
    {
        parameters.Add("@FoodProductId", cookingDirection.FoodProductId, DbType.Int32);
        parameters.Add("@StepNumber", cookingDirection.StepNumber, DbType.Int32);
        parameters.Add("@Instruction", cookingDirection.Instruction, DbType.String);
        parameters.Add("@DurationMinutes", cookingDirection.DurationMinutes, DbType.Int32);
        parameters.Add("@TemperatureFahrenheit", cookingDirection.TemperatureFahrenheit, DbType.Int32);
        parameters.Add("@UpdatedAt", cookingDirection.UpdatedAt, DbType.DateTimeOffset);
    }

    private static CookingDirection Map(CookingDirectionRow row) => new()
    {
        Id = row.Id,
        FoodProductId = row.FoodProductId,
        StepNumber = row.StepNumber,
        Instruction = row.Instruction,
        DurationMinutes = row.DurationMinutes,
        TemperatureFahrenheit = row.TemperatureFahrenheit,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
    };

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
    }
}
