using HotelBooking.BLL.DTOs.Auth;
using HotelBooking.BLL.Interfaces;
using HotelBooking.DAL.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;

namespace HotelBooking.BLL.Services;

public sealed class AuthService : IAuthService
{
    private readonly IGuestRepository _guestRepository;
    private readonly IStaffRepository _staffRepository;

    public AuthService(IGuestRepository guestRepository, IStaffRepository staffRepository)
    {
        _guestRepository = guestRepository;
        _staffRepository = staffRepository;
    }

    public async Task<AuthenticatedUserDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim();

        var staff = await _staffRepository.GetByEmailAsync(email);
        if (staff is not null)
        {
            if (!staff.IsActive)
                throw new ValidationException("Обліковий запис працівника деактивовано.");

            if (string.IsNullOrEmpty(staff.PasswordHash) || !BCrypt.Net.BCrypt.Verify(dto.Password, staff.PasswordHash))
                throw new ValidationException("Невірний email або пароль.");

            return new AuthenticatedUserDto
            {
                Id = staff.Id,
                FullName = staff.FullName,
                Email = staff.Email,
                Role = staff.RoleName,
                IsStaff = true,
                HotelId = staff.HotelId
            };
        }

        var guest = await _guestRepository.GetByEmailAsync(email);
        if (guest is null || string.IsNullOrEmpty(guest.PasswordHash) || !BCrypt.Net.BCrypt.Verify(dto.Password, guest.PasswordHash))
            throw new ValidationException("Невірний email або пароль.");

        if (guest.IsBanned)
            throw new ValidationException("Ваш обліковий запис заблоковано адміністратором.");

        return new AuthenticatedUserDto
        {
            Id = guest.Id,
            FullName = guest.FullName,
            Email = guest.Email,
            Role = "guest",
            IsStaff = false
        };
    }

    public async Task<AuthenticatedUserDto> RegisterGuestAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim();

        if (await _guestRepository.GetByEmailAsync(email) is not null ||
            await _staffRepository.GetByEmailAsync(email) is not null)
        {
            throw new BookingConflictException("Користувач із таким email уже зареєстрований.");
        }

        ValidateDocumentPair(dto.DocumentType, dto.DocumentNumber);

        var guest = new Guest
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),
            DocumentType = string.IsNullOrWhiteSpace(dto.DocumentType) ? null : dto.DocumentType.Trim(),
            DocumentNumber = string.IsNullOrWhiteSpace(dto.DocumentNumber) ? null : dto.DocumentNumber.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsBanned = false
        };

        var id = await _guestRepository.CreateAsync(guest);

        return new AuthenticatedUserDto
        {
            Id = id,
            FullName = guest.FullName,
            Email = guest.Email,
            Role = "guest",
            IsStaff = false
        };
    }

    public async Task<Guest> GetGuestProfileAsync(int guestId)
    {
        var guest = await _guestRepository.GetByIdAsync(guestId);
        return guest ?? throw new NotFoundException("Профіль гостя не знайдено.");
    }

    public async Task UpdateGuestProfileAsync(int guestId, UpdateProfileDto dto)
    {
        ValidateDocumentPair(dto.DocumentType, dto.DocumentNumber);

        var updated = await _guestRepository.UpdateProfileAsync(
            guestId,
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            dto.Phone.Trim(),
            dto.DocumentType?.Trim(),
            dto.DocumentNumber?.Trim());

        if (!updated)
            throw new NotFoundException("Профіль гостя не знайдено.");
    }

    public async Task EnsureSeedPasswordsAsync(string defaultPassword)
    {
        var defaultHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

        var staffList = await _staffRepository.GetAllAsync();
        foreach (var staff in staffList.Where(s => string.IsNullOrEmpty(s.PasswordHash)))
            await _staffRepository.UpdatePasswordHashAsync(staff.Id, defaultHash);

        var guests = await _guestRepository.GetAllAsync();
        foreach (var guest in guests.Where(g => string.IsNullOrEmpty(g.PasswordHash)))
            await _guestRepository.UpdatePasswordHashAsync(guest.Id, defaultHash);
    }

    private static void ValidateDocumentPair(string? docType, string? docNumber)
    {
        var hasType = !string.IsNullOrWhiteSpace(docType);
        var hasNumber = !string.IsNullOrWhiteSpace(docNumber);

        if (hasType != hasNumber)
            throw new ValidationException("Тип і номер документа мають бути заповнені разом або обидва порожні.");
    }
}