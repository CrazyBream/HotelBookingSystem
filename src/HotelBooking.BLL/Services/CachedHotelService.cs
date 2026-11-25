using System.Text.Json;
using HotelBooking.BLL.DTOs.Hotel;
using HotelBooking.BLL.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Views;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HotelBooking.BLL.Services;

public sealed class CachedHotelService : IHotelService
{
    private const string VersionKey = "catalog:version";
    private static readonly TimeSpan CatalogTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReferenceTtl = TimeSpan.FromMinutes(30);

    private readonly IHotelService _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedHotelService> _logger;

    public CachedHotelService(
        IHotelService inner,
        IDistributedCache cache,
        ILogger<CachedHotelService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Hotel>> GetAllHotelsAsync(bool onlyActive = false)
    {
        var ver = await GetVersionAsync();
        return await GetOrSetAsync(
            $"v{ver}:hotels:active={onlyActive}",
            () => _inner.GetAllHotelsAsync(onlyActive),
            CatalogTtl);
    }

    public async Task<Hotel> GetHotelByIdAsync(int id)
    {
        var ver = await GetVersionAsync();
        return await GetOrSetAsync(
            $"v{ver}:hotel:{id}",
            () => _inner.GetHotelByIdAsync(id),
            CatalogTtl);
    }

    public async Task<IReadOnlyList<RoomCatalogItem>> GetRoomCatalogAsync(
        int? hotelId = null, string? city = null, bool onlyActiveHotels = true)
    {
        var ver = await GetVersionAsync();
        var normalizedCity = string.IsNullOrWhiteSpace(city) ? "all" : city.Trim().ToLowerInvariant();
        return await GetOrSetAsync(
            $"v{ver}:catalog:h={hotelId?.ToString() ?? "all"}:c={normalizedCity}:a={onlyActiveHotels}",
            () => _inner.GetRoomCatalogAsync(hotelId, city, onlyActiveHotels),
            CatalogTtl);
    }

    public async Task<RoomCatalogItem> GetRoomDetailsAsync(int roomId)
    {
        var ver = await GetVersionAsync();
        return await GetOrSetAsync(
            $"v{ver}:room:{roomId}",
            () => _inner.GetRoomDetailsAsync(roomId),
            CatalogTtl);
    }

    public Task<IReadOnlyList<AvailableRoomItem>> SearchAvailableRoomsAsync(
        string? city, DateOnly checkIn, DateOnly checkOut, short guests) =>
        _inner.SearchAvailableRoomsAsync(city, checkIn, checkOut, guests);

    public Task<decimal> CalculateStayPriceAsync(int roomId, DateOnly checkIn, DateOnly checkOut) =>
        _inner.CalculateStayPriceAsync(roomId, checkIn, checkOut);

    public Task<IReadOnlyList<RoomCategory>> GetCategoriesAsync() =>
        GetOrSetAsync("ref:categories", () => _inner.GetCategoriesAsync(), ReferenceTtl);

    public Task<IReadOnlyList<Amenity>> GetAmenitiesAsync() =>
        GetOrSetAsync("ref:amenities", () => _inner.GetAmenitiesAsync(), ReferenceTtl);

    public async Task<IReadOnlyList<string>> GetActiveCitiesAsync()
    {
        var ver = await GetVersionAsync();
        return await GetOrSetAsync(
            $"v{ver}:cities",
            () => _inner.GetActiveCitiesAsync(),
            CatalogTtl);
    }

    public async Task<int> CreateHotelAsync(CreateHotelDto dto)
    {
        var id = await _inner.CreateHotelAsync(dto);
        await InvalidateCatalogAsync();
        return id;
    }

    public async Task UpdateHotelAsync(UpdateHotelDto dto)
    {
        await _inner.UpdateHotelAsync(dto);
        await InvalidateCatalogAsync();
    }

    public async Task SetHotelActiveAsync(int id, bool isActive)
    {
        await _inner.SetHotelActiveAsync(id, isActive);
        await InvalidateCatalogAsync();
    }

    public async Task<int> CreateRoomAsync(CreateRoomDto dto)
    {
        var id = await _inner.CreateRoomAsync(dto);
        await InvalidateCatalogAsync();
        return id;
    }

    public async Task UpdateRoomStatusAsync(int roomId, string status)
    {
        await _inner.UpdateRoomStatusAsync(roomId, status);
        await InvalidateCatalogAsync();
    }

    private async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cached))
            {
                var deserialized = JsonSerializer.Deserialize<T>(cached);
                if (deserialized is not null)
                {
                    _logger.LogInformation("Redis CACHE HIT: {Key}", key);
                    return deserialized;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable on GET {Key}, fallback to PostgreSQL", key);
            return await factory();
        }

        _logger.LogInformation("Redis CACHE MISS: {Key} -> PostgreSQL", key);
        var data = await factory();

        try
        {
            var json = JsonSerializer.Serialize(data);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable on SET {Key}", key);
        }

        return data;
    }

    private async Task<string> GetVersionAsync()
    {
        try
        {
            var ver = await _cache.GetStringAsync(VersionKey);
            if (!string.IsNullOrEmpty(ver))
                return ver;

            const string initial = "1";
            await _cache.SetStringAsync(VersionKey, initial);
            return initial;
        }
        catch
        {
            return "1";
        }
    }

    private async Task InvalidateCatalogAsync()
    {
        try
        {
            var nextVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            await _cache.SetStringAsync(VersionKey, nextVersion);
            _logger.LogInformation("Redis CACHE INVALIDATED: new catalog version = {Version}", nextVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable on invalidation");
        }
    }
}