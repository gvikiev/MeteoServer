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

        [HttpGet("ownership/{chipId}/user/{userId}/latest")]
        public async Task<ActionResult<RoomWithSensorDto>> GetRoomByChipIdForUser(string chipId, int userId, CancellationToken ct)
        {
            var dto = await _ownership.GetRoomForUserAsync(chipId, userId, ct);
            return dto is null ? NotFound("Кімната не знайдена для цього chipId і userId") : Ok(dto);
        }

        [HttpGet("byUser/{userId}")]
        public async Task<ActionResult<IEnumerable<RoomWithSensorDto>>> GetRoomsByUserId(int userId, CancellationToken ct)
        {
            var items = await _ownership.GetRoomsByUserAsync(userId, ct);
            return Ok(items);
        }

        [HttpPost("ownership")]
        public async Task<IActionResult> CreateOwnership([FromBody] SensorOwnershipCreateDto dto, CancellationToken ct)
        {
            try
            {
                var room = await _ownership.CreateAsync(dto, ct);
                return CreatedAtAction(
                    nameof(GetRoomByChipIdForUser),
                    new { chipId = room.ChipId, userId = dto.UserId },
                    room
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
