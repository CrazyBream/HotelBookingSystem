using HotelBooking.BLL.DTOs.Admin;
using HotelBooking.BLL.Interfaces;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;

namespace HotelBooking.BLL.Services;

public sealed class AdminService : IAdminService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly IStaffRepository _staffRepository;

    public AdminService(
        IAnalyticsRepository analyticsRepository,
        IGuestRepository guestRepository,
        IStaffRepository staffRepository)
    {
        _analyticsRepository = analyticsRepository;
        _guestRepository = guestRepository;
        _staffRepository = staffRepository;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var stats = (await _analyticsRepository.GetHotelOccupancyStatsAsync()).ToList();
        var audits = (await _analyticsRepository.GetRecentAuditEntriesAsync(30)).ToList();

        return new AdminDashboardDto
        {
            HotelStats = stats,
            RecentAudits = audits,
            TotalCollectedRevenue = stats.Sum(s => s.CollectedRevenue),
            TotalExpectedRevenue = stats.Sum(s => s.ExpectedRevenue),
            TotalBookings = stats.Sum(s => s.TotalBookings),
            TotalOperationalRooms = stats.Sum(s => s.OperationalRooms)
        };
    }

    public async Task<IReadOnlyList<Guest>> GetAllGuestsAsync() =>
        (await _guestRepository.GetAllAsync()).ToList();

    public async Task SetGuestBannedAsync(int guestId, bool isBanned)
    {
        if (!await _guestRepository.SetBannedAsync(guestId, isBanned))
            throw new NotFoundException($"Гостя з ID={guestId} не знайдено.");
    }

    public async Task<IReadOnlyList<Staff>> GetAllStaffAsync() =>
        (await _staffRepository.GetAllAsync()).ToList();
}