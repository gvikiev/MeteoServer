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
        public async Task<IActionResult> CreateOwnership([FromBody] SensorOwnershipCreateDto dto)
        {
            // 🔐 Валідація
            if (dto.UserId <= 0 ||
                string.IsNullOrWhiteSpace(dto.ChipId) ||
                string.IsNullOrWhiteSpace(dto.RoomName) ||
                string.IsNullOrWhiteSpace(dto.ImageName))
            {
                return BadRequest("Потрібні поля: UserId, ChipId, RoomName, ImageName");
            }

            var normalizedChipId = dto.ChipId.Trim().ToUpperInvariant();

            // 🔍 Перевірка, що користувач існує
            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
            {
                return NotFound("Користувача не знайдено");
            }

            // 🚫 Перевірка дублювання ChipId (пристрій уже прив’язаний до будь-кого)
            var chipTaken = await _context.SensorOwnerships
                .AnyAsync(o => o.ChipId == normalizedChipId);
            if (chipTaken)
            {
                return Conflict("Цей пристрій уже зареєстрований");
            }

            // ✅ Створення
            var ownership = new SensorOwnership
            {
                UserId = dto.UserId,
                ChipId = normalizedChipId,
                RoomName = dto.RoomName.Trim(),
                ImageName = dto.ImageName.Trim(),
                Version = 1,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SensorOwnerships.Add(ownership);
            await _context.SaveChangesAsync();

            // Готуємо DTO-відповідь (сенсори можуть бути ще відсутні)
            var latestData = await _context.SensorData
                .Where(d => d.ChipId == normalizedChipId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            var roomDto = MapToRoomDto(ownership, latestData);

            // RESTful: 201 Created + Location на GET
            return CreatedAtAction(
                nameof(GetRoomByChipIdForUser),
                new { chipId = ownership.ChipId, userId = ownership.UserId },
                roomDto
            );
        }

        // ====== ХЕЛПЕР МАПІНГУ ======
        private static RoomWithSensorDto MapToRoomDto(SensorOwnership ownership, SensorData? latestData)
        {
            return new RoomWithSensorDto
            {
                Id = ownership.Id,
                ChipId = ownership.ChipId,        // ✅ додаємо ChipId у DTO
                RoomName = ownership.RoomName,
                ImageName = ownership.ImageName,
                Temperature = latestData?.TemperatureDht,
                Humidity = latestData?.HumidityDht
            };
        }
    }
}
