using Microsoft.AspNetCore.Mvc;
using MySensorApi.DTO;
using MySensorApi.Services;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisplayDataController : ControllerBase
    {
        private readonly IOwnershipService _ownership;

        public DisplayDataController(IOwnershipService ownership)
        {
            _ownership = ownership;
        }

        // Усі кімнати користувача
        [HttpGet("byUser/{userId}")]
        public async Task<ActionResult<IEnumerable<RoomWithSensorDto>>> GetRoomsByUserId(
            int userId, CancellationToken ct)
        {
            var items = await _ownership.GetRoomsByUserAsync(userId, ct);
            return Ok(items);
        }

        // Створення ownership (UserId + ChipId + RoomName + ImageName)
        [HttpPost("ownership")]
        public async Task<IActionResult> CreateOwnership(
            [FromBody] SensorOwnershipDto dto, CancellationToken ct)
        {
            try
            {
                var room = await _ownership.CreateAsync(dto, ct);
                // dto.UserId гарантовано присутній для create (валідація в сервісі)
                return CreatedAtAction(nameof(GetRoomsByUserId),
                    new { userId = dto.UserId!.Value }, room);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
