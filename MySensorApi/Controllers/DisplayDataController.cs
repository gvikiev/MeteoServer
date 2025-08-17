using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.DTO;
using MySensorApi.Models;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisplayDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DisplayDataController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("ownership/{chipId}/user/{userId}/latest")]
        public async Task<ActionResult<RoomWithSensorDto>> GetRoomByChipIdForUser(string chipId, int userId)
        {
            var normalizedChipId = chipId.Trim().ToUpperInvariant();

            var ownership = await _context.SensorOwnerships
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.ChipId == normalizedChipId && o.UserId == userId);

            if (ownership == null)
                return NotFound("Кімната не знайдена для цього chipId і userId");

            var latestData = await _context.SensorData
                .Where(d => d.ChipId == normalizedChipId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            return Ok(new RoomWithSensorDto
            {
                Id = ownership.Id,
                RoomName = ownership.RoomName,
                ImageName = ownership.ImageName,
                Temperature = latestData?.TemperatureDht,
                Humidity = latestData?.HumidityDht
            });
        }


        [HttpGet("byUser/{userId}")]
        public async Task<ActionResult<IEnumerable<RoomWithSensorDto>>> GetRoomsByUserId(int userId)
        {

            var ownerships = await _context.SensorOwnerships
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var result = new List<RoomWithSensorDto>();

            foreach (var ownership in ownerships)
            {
                var latestData = await _context.SensorData
                    .Where(d => d.ChipId == ownership.ChipId)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();

                result.Add(new RoomWithSensorDto
                {
                    Id = ownership.Id,
                    RoomName = ownership.RoomName,
                    ImageName = ownership.ImageName,
                    Temperature = latestData?.TemperatureDht,
                    Humidity = latestData?.HumidityDht
                });
            }

            return Ok(result);
        }


        [HttpPost("ownership")]
        public async Task<IActionResult> CreateOwnership([FromBody] SensorOwnershipRequestDTO dto)
        {
            // Перевірка обов'язкових полів
            if (string.IsNullOrEmpty(dto.ChipId) ||
                string.IsNullOrEmpty(dto.RoomName) ||
                string.IsNullOrEmpty(dto.Username))
            {
                return BadRequest("Обов'язкові поля відсутні");
            }

            // Пошук користувача по імені
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null)
            {
                return NotFound("Користувач не знайдений");
            }

            var normalizedChipId = dto.ChipId.Trim().ToUpperInvariant();

            var ownership = new SensorOwnership
            {
                UserId = user.Id,
                ChipId = normalizedChipId,
                RoomName = dto.RoomName,
                ImageName = dto.ImageName
            };

            _context.SensorOwnerships.Add(ownership);
            await _context.SaveChangesAsync();

            return Ok(ownership);
        }


    }
}
