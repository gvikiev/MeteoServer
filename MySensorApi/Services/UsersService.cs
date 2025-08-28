using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MySensorApi.DTO;
using MySensorApi.Infrastructure.Auth;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;

namespace MySensorApi.Services
{
    public interface IUsersService
    {
        Task<UserDto> RegisterAsync(UserRegistrationDto dto, JwtTokenService tokenService, CancellationToken ct);
        Task<UserDto> LoginAsync(UserLoginDto dto, JwtTokenService tokenService, CancellationToken ct);
        Task<TokenResponseDto> RefreshAsync(string refreshToken, JwtTokenService tokenService, CancellationToken ct);
        Task<UserProfileDto?> GetUserProfileAsync(int id, CancellationToken ct);
        Task<string?> GetUsernameByIdAsync(int id, CancellationToken ct);
    }

    public sealed class UsersService : IUsersService
    {
        private readonly IUsersRepository _usersRepo;

        public UsersService(IUsersRepository usersRepo) => _usersRepo = usersRepo;

        public async Task<UserDto> RegisterAsync(UserRegistrationDto dto, JwtTokenService tokenService, CancellationToken ct)
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

            // ⬇️ Генеруємо токени одразу
            var accessToken = tokenService.GenerateToken(user);
            var refreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _usersRepo.SaveChangesAsync(ct);

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = role.RoleName,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<UserDto> LoginAsync(UserLoginDto dto, JwtTokenService tokenService, CancellationToken ct)
        {
            var user = await _usersRepo.GetByUsernameAsync(dto.Username, ct)
                       ?? throw new UnauthorizedAccessException("Invalid login");

            if (!VerifyPassword(user.PasswordHash, dto.Password))
                throw new UnauthorizedAccessException("Invalid password");

            var accessToken = tokenService.GenerateToken(user);
            var refreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _usersRepo.SaveChangesAsync(ct);

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "User",
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<TokenResponseDto> RefreshAsync(string refreshToken, JwtTokenService tokenService, CancellationToken ct)
        {
            var user = await _usersRepo.FindByRefreshTokenAsync(refreshToken, ct);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            var newAccessToken = tokenService.GenerateToken(user); // 👈 тепер передаємо user
            var newRefreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _usersRepo.SaveChangesAsync(ct);

            return new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<string?> GetUsernameByIdAsync(int id, CancellationToken ct)
            => (await _usersRepo.FindByIdAsync(id, ct))?.Username;

        // ---- helpers ----
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

        public async Task<UserProfileDto?> GetUserProfileAsync(int id, CancellationToken ct)
        {
            var user = await _usersRepo.FindByIdAsync(id, ct);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };
        }
    }
}
