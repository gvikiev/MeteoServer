using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySensorApi.DTO;
using MySensorApi.Infrastructure.Auth;
using MySensorApi.Services;

namespace MySensorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _users;

        public UsersController(IUsersService users)
        {
            _users = users;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(UserRegistrationDto dto, CancellationToken ct)
        {
            try
            {
                var user = await _users.RegisterAsync(dto, ct);
                return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto, [FromServices] JwtTokenService tokenService, CancellationToken ct)
        {
            try
            {
                var result = await _users.LoginAsync(dto, tokenService, ct);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetUsernameById(int id, CancellationToken ct)
        {
            var name = await _users.GetUsernameByIdAsync(id, ct);
            return name is null ? NotFound("Користувача не знайдено") : Ok(name);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto, [FromServices] JwtTokenService tokenService, CancellationToken ct)
        {
            try
            {
                var tokens = await _users.RefreshAsync(dto.RefreshToken, tokenService, ct);
                return Ok(tokens);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
