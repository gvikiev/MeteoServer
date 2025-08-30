using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySensorApi.DTO;
using MySensorApi.DTO.User;
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

        // POST: /api/Users/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserProfileDto>> Register([FromBody] UserAuthRequestDto dto, CancellationToken ct)
        {
            try
            {
                var user = await _users.RegisterAsync(dto, ct);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: /api/Users/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserAuthRequestDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _users.LoginAsync(dto, ct);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // POST: /api/Users/refresh
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto, CancellationToken ct)
        {
            try
            {
                var profileWithNewTokens = await _users.RefreshAsync(dto.RefreshToken, ct);
                return Ok(profileWithNewTokens);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // GET: /api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetUsernameById(int id, CancellationToken ct)
        {
            var name = await _users.GetUsernameByIdAsync(id, ct);
            return name is null ? NotFound("Користувача не знайдено") : Ok(name);
        }

        // GET: /api/Users/{id}/profile
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserById(int id, CancellationToken ct)
        {
            var profile = await _users.GetUserProfileAsync(id, ct);
            return profile is null ? NotFound("Користувача не знайдено") : Ok(profile);
        }
    }
}
