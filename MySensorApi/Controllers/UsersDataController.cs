using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.DTO;
using MySensorApi.Helpers;
using MySensorApi.Models;

namespace MySensorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<User>> Register(UserRegistrationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Login) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest("All fields are required.");
            }

            // Перевірка унікальності логіну
            if (_context.Users.Any(u => u.Login == dto.Login))
            {
                return Conflict("User with this login already exists.");
            }

            var user = new User
            {
                Login = dto.Login,
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                Email = dto.Email
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto, [FromServices] JwtTokenService tokenService)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);
            if (user == null) return Unauthorized("Invalid login");

            var parts = user.PasswordHash.Split(':');
            if (parts.Length != 2) return Unauthorized("Invalid hash format");

            var salt = Convert.FromBase64String(parts[0]);
            var hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: dto.Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            if (hashedInput != parts[1]) return Unauthorized("Invalid password");

            var accessToken = tokenService.GenerateToken(user.Login);
            var refreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [Authorize] // захищено токеном
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetUsernameById(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound("Користувача не знайдено");

            return Ok(user.Login);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto, [FromServices] JwtTokenService tokenService)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == dto.RefreshToken &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow);

            if (user == null)
                return Unauthorized("Invalid or expired refresh token");

            var newAccessToken = tokenService.GenerateToken(user.Login);
            var newRefreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
    }
}
