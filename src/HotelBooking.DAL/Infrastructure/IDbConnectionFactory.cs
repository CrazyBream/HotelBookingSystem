using Npgsql;

namespace HotelBooking.DAL.Infrastructure;

public interface IDbConnectionFactory
{
    Task<NpgsqlConnection> CreateOpenConnectionAsync();
}