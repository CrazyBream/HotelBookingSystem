using Dapper;
using HotelBooking.DAL.Infrastructure;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.DAL.Repositories;

public sealed class GuestRepository : BaseRepository, IGuestRepository
{
    public GuestRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<Guest?> GetByIdAsync(int id) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<Guest>(
            """
            SELECT id, first_name, last_name, email, phone,
                   document_type, document_number, password_hash, is_banned, created_at
            FROM hotel.guests
            WHERE id = @Id
            """,
            new { Id = id }));

    public Task<Guest?> GetByEmailAsync(string email) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<Guest>(
            """
            SELECT id, first_name, last_name, email, phone,
                   document_type, document_number, password_hash, is_banned, created_at
            FROM hotel.guests
            WHERE lower(email) = lower(btrim(@Email))
            """,
            new { Email = email }));

    public Task<IEnumerable<Guest>> GetAllAsync() =>
        ExecuteAsync(conn => conn.QueryAsync<Guest>(
            """
            SELECT id, first_name, last_name, email, phone,
                   document_type, document_number, password_hash, is_banned, created_at
            FROM hotel.guests
            ORDER BY id ASC
            """));

    public Task<int> CreateAsync(Guest guest) =>
        ExecuteAsync(conn => conn.QuerySingleAsync<int>(
            """
            INSERT INTO hotel.guests (
                first_name, last_name, email, phone,
                document_type, document_number, password_hash, is_banned
            )
            VALUES (
                @FirstName, @LastName, @Email, @Phone,
                @DocumentType, @DocumentNumber, @PasswordHash, @IsBanned
            )
            RETURNING id
            """,
            guest));

    public Task<bool> UpdateProfileAsync(int id, string firstName, string lastName, string phone, string? documentType, string? documentNumber) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                """
                UPDATE hotel.guests
                SET first_name = @FirstName,
                    last_name = @LastName,
                    phone = @Phone,
                    document_type = @DocumentType,
                    document_number = @DocumentNumber
                WHERE id = @Id
                """,
                new
                {
                    Id = id,
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = phone,
                    DocumentType = string.IsNullOrWhiteSpace(documentType) ? null : documentType,
                    DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber
                });
            return affected > 0;
        });

    public Task<bool> UpdatePasswordHashAsync(int id, string passwordHash) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                "UPDATE hotel.guests SET password_hash = @PasswordHash WHERE id = @Id",
                new { Id = id, PasswordHash = passwordHash });
            return affected > 0;
        });

    public Task<bool> SetBannedAsync(int id, bool isBanned) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                "UPDATE hotel.guests SET is_banned = @IsBanned WHERE id = @Id",
                new { Id = id, IsBanned = isBanned });
            return affected > 0;
        });
}