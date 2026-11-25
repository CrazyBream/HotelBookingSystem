using Dapper;
using HotelBooking.DAL.Infrastructure;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;

namespace HotelBooking.DAL.Repositories;

public sealed class HotelRepository : BaseRepository, IHotelRepository
{
    public HotelRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<IEnumerable<Hotel>> GetAllHotelsAsync(bool onlyActive = false) =>
        ExecuteAsync(conn => conn.QueryAsync<Hotel>(
            """
            SELECT id, hotel_code, name, star_rating, country, city, address,
                   phone, email, description, image_url, is_active, created_at
            FROM hotel.hotels
            WHERE (@OnlyActive = false OR is_active = true)
            ORDER BY star_rating DESC, name ASC
            """,
            new { OnlyActive = onlyActive }));

    public Task<Hotel?> GetHotelByIdAsync(int id) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<Hotel>(
            """
            SELECT id, hotel_code, name, star_rating, country, city, address,
                   phone, email, description, image_url, is_active, created_at
            FROM hotel.hotels
            WHERE id = @Id
            """,
            new { Id = id }));

    public Task<IEnumerable<RoomCatalogItem>> GetRoomCatalogAsync(int? hotelId = null, string? city = null, bool onlyActiveHotels = true) =>
        ExecuteAsync(conn => conn.QueryAsync<RoomCatalogItem>(
            """
            SELECT *
            FROM hotel.v_room_catalog
            WHERE (@HotelId IS NULL OR hotel_id = @HotelId)
              AND (@City IS NULL OR btrim(@City) = '' OR lower(city) = lower(btrim(@City)))
              AND (@OnlyActiveHotels = false OR hotel_is_active = true)
            ORDER BY star_rating DESC, base_price ASC, room_number ASC
            """,
            new { HotelId = hotelId, City = city, OnlyActiveHotels = onlyActiveHotels }));

    public Task<RoomCatalogItem?> GetRoomCatalogItemAsync(int roomId) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<RoomCatalogItem>(
            """
            SELECT *
            FROM hotel.v_room_catalog
            WHERE room_id = @RoomId
            """,
            new { RoomId = roomId }));

    public Task<IEnumerable<AvailableRoomItem>> FindAvailableRoomsAsync(string? city, DateOnly checkIn, DateOnly checkOut, short guests) =>
        ExecuteAsync(conn => conn.QueryAsync<AvailableRoomItem>(
            """
            SELECT *
            FROM hotel.fn_find_available_rooms(@City, @CheckIn, @CheckOut, @Guests)
            """,
            new { City = city, CheckIn = checkIn, CheckOut = checkOut, Guests = guests }));

    public Task<decimal?> CalculateStayPriceAsync(int roomId, DateOnly checkIn, DateOnly checkOut) =>
        ExecuteAsync(conn => conn.QuerySingleOrDefaultAsync<decimal?>(
            """
            SELECT hotel.fn_calculate_stay_price(@RoomId, @CheckIn, @CheckOut)
            """,
            new { RoomId = roomId, CheckIn = checkIn, CheckOut = checkOut }));

    public Task<IEnumerable<RoomCategory>> GetCategoriesAsync() =>
        ExecuteAsync(conn => conn.QueryAsync<RoomCategory>(
            """
            SELECT id, code, name, description, max_guests, area_sqm, base_price
            FROM hotel.room_categories
            ORDER BY base_price ASC
            """));

    public Task<IEnumerable<Amenity>> GetAmenitiesAsync() =>
        ExecuteAsync(conn => conn.QueryAsync<Amenity>(
            """
            SELECT id, name, icon_code
            FROM hotel.amenities
            ORDER BY name ASC
            """));

    public Task<IEnumerable<string>> GetActiveCitiesAsync() =>
        ExecuteAsync(conn => conn.QueryAsync<string>(
            """
            SELECT DISTINCT city
            FROM hotel.hotels
            WHERE is_active = true
            ORDER BY city ASC
            """));

    public Task<int> CreateHotelAsync(Hotel hotel) =>
        ExecuteAsync(conn => conn.QuerySingleAsync<int>(
            """
            INSERT INTO hotel.hotels (
                hotel_code, name, star_rating, country, city, address,
                phone, email, description, image_url, is_active
            )
            VALUES (
                @HotelCode, @Name, @StarRating, @Country, @City, @Address,
                @Phone, @Email, @Description, @ImageUrl, @IsActive
            )
            RETURNING id
            """,
            hotel));

    public Task<bool> UpdateHotelAsync(Hotel hotel) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                """
                UPDATE hotel.hotels
                SET name = @Name,
                    star_rating = @StarRating,
                    country = @Country,
                    city = @City,
                    address = @Address,
                    phone = @Phone,
                    email = @Email,
                    description = @Description,
                    image_url = @ImageUrl,
                    is_active = @IsActive
                WHERE id = @Id
                """,
                hotel);
            return affected > 0;
        });

    public Task<bool> SetHotelActiveAsync(int id, bool isActive) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                "UPDATE hotel.hotels SET is_active = @IsActive WHERE id = @Id",
                new { Id = id, IsActive = isActive });
            return affected > 0;
        });

    public Task<int> CreateRoomAsync(Room room, IEnumerable<int> amenityIds) =>
        ExecuteAsync(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();
            var roomId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO hotel.rooms (
                    hotel_id, category_id, room_number, floor, status, description, image_url
                )
                VALUES (
                    @HotelId, @CategoryId, @RoomNumber, @Floor, @Status, @Description, @ImageUrl
                )
                RETURNING id
                """,
                room, tx);

            var uniqueAmenities = amenityIds.Distinct().ToArray();
            if (uniqueAmenities.Length > 0)
            {
                await conn.ExecuteAsync(
                    """
                    INSERT INTO hotel.room_amenities (room_id, amenity_id)
                    VALUES (@RoomId, @AmenityId)
                    ON CONFLICT DO NOTHING
                    """,
                    uniqueAmenities.Select(a => new { RoomId = roomId, AmenityId = a }),
                    tx);
            }

            await tx.CommitAsync();
            return roomId;
        });

    public Task<bool> UpdateRoomStatusAsync(int roomId, string status) =>
        ExecuteAsync(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                "UPDATE hotel.rooms SET status = @Status WHERE id = @RoomId",
                new { RoomId = roomId, Status = status });
            return affected > 0;
        });
}