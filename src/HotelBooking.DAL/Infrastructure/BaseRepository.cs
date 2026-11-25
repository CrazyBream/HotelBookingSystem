using HotelBooking.Domain.Exceptions;
using Npgsql;

namespace HotelBooking.DAL.Infrastructure;

public abstract class BaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    protected BaseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    protected async Task<T> ExecuteAsync<T>(Func<NpgsqlConnection, Task<T>> operation)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        try
        {
            return await operation(connection);
        }
        catch (PostgresException ex)
        {
            throw MapPostgresException(ex);
        }
    }

    protected async Task ExecuteAsync(Func<NpgsqlConnection, Task> operation)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        try
        {
            await operation(connection);
        }
        catch (PostgresException ex)
        {
            throw MapPostgresException(ex);
        }
    }

    private static DomainException MapPostgresException(PostgresException ex) => ex.SqlState switch
    {
        PostgresErrorCodes.ExclusionViolation =>
            new BookingConflictException("Обраний номер уже заброньований на пересічні дати."),
        PostgresErrorCodes.UniqueViolation =>
            new BookingConflictException("Запис із такими унікальними даними вже існує."),
        PostgresErrorCodes.CheckViolation =>
            new ValidationException(string.IsNullOrWhiteSpace(ex.MessageText) ? "Порушено правило перевірки даних." : ex.MessageText),
        PostgresErrorCodes.ForeignKeyViolation =>
            new ValidationException("Операція порушує цілісність пов’язаних даних (об’єкт не знайдено або має історію)."),
        PostgresErrorCodes.NoDataFound =>
            new NotFoundException(string.IsNullOrWhiteSpace(ex.MessageText) ? "Об’єкт не знайдено." : ex.MessageText),
        _ => new ValidationException(ex.MessageText)
    };
}