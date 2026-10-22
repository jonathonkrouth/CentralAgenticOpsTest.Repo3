using System.Data;
using System.Data.Common;
using System.Globalization;
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
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " ORDER BY FoodProductId, StepNumber;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var cookingDirections = new List<CookingDirection>();
        while (await reader.ReadAsync(cancellationToken))
        {
            cookingDirections.Add(Map(reader));
        }

        return cookingDirections;
    }

    public async Task<CookingDirection?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " WHERE Id = @Id;";
        AddParameter(command, "@Id", DbType.Int32, id);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(CookingDirection cookingDirection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cookingDirection);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;
        AddWritableParameters(command, cookingDirection);
        AddParameter(command, "@CreatedAt", DbType.DateTimeOffset, cookingDirection.CreatedAt);

        var newId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(newId, CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateAsync(CookingDirection cookingDirection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cookingDirection);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = UpdateSql;
        AddParameter(command, "@Id", DbType.Int32, cookingDirection.Id);
        AddWritableParameters(command, cookingDirection);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        AddParameter(command, "@Id", DbType.Int32, id);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddWritableParameters(DbCommand command, CookingDirection cookingDirection)
    {
        AddParameter(command, "@FoodProductId", DbType.Int32, cookingDirection.FoodProductId);
        AddParameter(command, "@StepNumber", DbType.Int32, cookingDirection.StepNumber);
        AddParameter(command, "@Instruction", DbType.String, cookingDirection.Instruction);
        AddParameter(command, "@DurationMinutes", DbType.Int32, cookingDirection.DurationMinutes);
        AddParameter(command, "@TemperatureFahrenheit", DbType.Int32, cookingDirection.TemperatureFahrenheit);
        AddParameter(command, "@UpdatedAt", DbType.DateTimeOffset, cookingDirection.UpdatedAt);
    }

    private static void AddParameter(DbCommand command, string name, DbType dbType, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static CookingDirection Map(DbDataReader reader)
    {
        var durationOrdinal = reader.GetOrdinal("DurationMinutes");
        var temperatureOrdinal = reader.GetOrdinal("TemperatureFahrenheit");

        return new CookingDirection
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            FoodProductId = reader.GetInt32(reader.GetOrdinal("FoodProductId")),
            StepNumber = reader.GetInt32(reader.GetOrdinal("StepNumber")),
            Instruction = reader.GetString(reader.GetOrdinal("Instruction")),
            DurationMinutes = reader.IsDBNull(durationOrdinal) ? null : reader.GetInt32(durationOrdinal),
            TemperatureFahrenheit = reader.IsDBNull(temperatureOrdinal) ? null : reader.GetInt32(temperatureOrdinal),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedAt")),
        };
    }
}
