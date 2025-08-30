using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MySensorApi.DTO;
using MySensorApi.DTO.User;
using MySensorApi.Infrastructure.Auth;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;

namespace MySensorApi.Services
{
    public interface IUsersService
    {
        Task<UserProfileDto> RegisterAsync(UserAuthRequestDto dto, CancellationToken ct);
        Task<UserProfileDto> LoginAsync(UserAuthRequestDto dto, CancellationToken ct);
        Task<UserProfileDto> RefreshAsync(string refreshToken, CancellationToken ct);

        Task<UserProfileDto?> GetUserProfileAsync(int id, CancellationToken ct);
        Task<string?> GetUsernameByIdAsync(int id, CancellationToken ct);
        Task<UserProfileDto?> GetUsernameInfoAsync(int id, CancellationToken ct);
        Task<UserProfileDto> ChangeUsernameAsync(int id, string newUsername, int expectedVersion, CancellationToken ct);
    }

    public sealed class UsersService : IUsersService
    {
        private readonly IUsersRepository _usersRepo;
        private readonly JwtTokenService _tokenService;

        public UsersService(IUsersRepository usersRepo, JwtTokenService tokenService)
        {
            _usersRepo = usersRepo;
            _tokenService = tokenService;
        }

        public async Task<UserProfileDto> RegisterAsync(UserAuthRequestDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.Email))
                throw new InvalidOperationException("All fields are required.");

            if (await _usersRepo.UsernameExistsAsync(dto.Username, ct))
                throw new InvalidOperationException("User with this login already exists.");

            var role = await _usersRepo.GetRoleByNameAsync("User", ct)
                       ?? throw new InvalidOperationException("Роль 'User' не знайдена в базі.");

            var user = new User
            {
                Username = dto.Username.Trim(),
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                Email = dto.Email.Trim(),
                RoleId = role.Id,
                Role = role
            };

            await _usersRepo.AddAsync(user, ct);
            await _usersRepo.SaveChangesAsync(ct);

            var accessToken = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _usersRepo.SaveChangesAsync(ct);

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = role.RoleName,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<UserProfileDto> LoginAsync(UserAuthRequestDto dto, CancellationToken ct)
        {
            var user = await _usersRepo.GetByUsernameAsync(dto.Username, ct)
                       ?? throw new UnauthorizedAccessException("Invalid login");

            if (!VerifyPassword(user.PasswordHash, dto.Password))
                throw new UnauthorizedAccessException("Invalid password");

            var accessToken = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _usersRepo.SaveChangesAsync(ct);

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "User",
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<UserProfileDto> RefreshAsync(string refreshToken, CancellationToken ct)
        {
            var user = await _usersRepo.FindByRefreshTokenAsync(refreshToken, ct);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _usersRepo.SaveChangesAsync(ct);

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "User",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<string?> GetUsernameByIdAsync(int id, CancellationToken ct)
            => (await _usersRepo.FindByIdAsync(id, ct))?.Username;

        public async Task<UserProfileDto?> GetUserProfileAsync(int id, CancellationToken ct)
        {
            var user = await _usersRepo.FindByIdAsync(id, ct);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "User",
                Version = user.Version,  // Переконайтесь, що тут передається актуальна версія
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
                // токени тут не повертаємо
            };
        }

        public async Task<UserProfileDto?> GetUsernameInfoAsync(int id, CancellationToken ct)
        {
            var user = await _usersRepo.FindByIdAsync(id, ct);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "User",
                Version = user.Version,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<UserProfileDto> ChangeUsernameAsync(int id, string newUsername, int expectedVersion, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(newUsername))
                throw new InvalidOperationException("Username is required.");

            var user = await _usersRepo.FindByIdAsync(id, ct)
                       ?? throw new InvalidOperationException("User not found.");

            if (expectedVersion != user.Version)
                throw new InvalidOperationException("VersionConflict");

            if (await _usersRepo.UsernameExistsForOtherAsync(newUsername, user.Id, ct))
                throw new InvalidOperationException("UsernameTaken");

            user.Username = newUsername.Trim();
            user.Version += 1;
            user.UpdatedAt = DateTime.UtcNow;

            await _usersRepo.SaveChangesAsync(ct);

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "User",
                Version = user.Version,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        // ===== helpers =====
        private static bool VerifyPassword(string storedHash, string plain)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: plain,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashedInput == parts[1];
        }
    }
}
