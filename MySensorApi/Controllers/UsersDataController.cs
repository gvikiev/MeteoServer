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
        private readonly JwtTokenService _tokenService; // 👈 додали поле

        public UsersController(IUsersService users, JwtTokenService tokenService)
        {
            _users = users;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(UserRegistrationDto dto,[FromServices] JwtTokenService tokenService,CancellationToken ct)
        {
            try
            {
                var user = await _users.RegisterAsync(dto, tokenService, ct);
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

        [HttpGet("{id}/profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserById(int id, CancellationToken ct)
        {
            var profile = await _users.GetUserProfileAsync(id, ct);
            return profile is null ? NotFound("Користувача не знайдено") : Ok(profile);
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
