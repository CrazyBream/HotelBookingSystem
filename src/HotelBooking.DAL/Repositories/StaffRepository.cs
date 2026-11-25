using Dapper;
using HotelBooking.DAL.Infrastructure;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.DAL.Repositories;

public sealed class StaffRepository : BaseRepository, IStaffRepository
{
    public StaffRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<Staff?> GetByIdAsync(int id) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<Staff>(
            """
            SELECT id, hotel_id, first_name, last_name, email,
                   role_name, password_hash, is_active, created_at
            FROM hotel.staff
            WHERE id = @Id
            """,
            new { Id = id }));

    public Task<Staff?> GetByEmailAsync(string email) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<Staff>(
            """
            SELECT id, hotel_id, first_name, last_name, email,
                   role_name, password_hash, is_active, created_at
            FROM hotel.staff
            WHERE lower(email) = lower(btrim(@Email))
            """,
            new { Email = email }));

    public Task<IEnumerable<Staff>> GetAllAsync() =>
        ExecuteAsync(conn => conn.QueryAsync<Staff>(
            """
            SELECT id, hotel_id, first_name, last_name, email,
                   role_name, password_hash, is_active, created_at
            FROM hotel.staff
            ORDER BY id ASC
            """));

    public Task<bool> UpdatePasswordHashAsync(int id, string passwordHash) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                "UPDATE hotel.staff SET password_hash = @PasswordHash WHERE id = @Id",
                new { Id = id, PasswordHash = passwordHash });
            return affected > 0;
        });
}