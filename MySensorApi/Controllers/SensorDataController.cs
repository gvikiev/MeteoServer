using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.DTO;
using MySensorApi.Models;
using System.Security.Claims;
using Microsoft.Net.Http.Headers;           // EntityTagHeaderValue, HeaderNames
using Microsoft.Extensions.Logging;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SensorDataController(AppDbContext context, ILogger<SensorDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private readonly ILogger<SensorDataController> _logger;

        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data)
        {
            data.CreatedAt = DateTime.UtcNow;

            _context.SensorData.Add(data);
            await _context.SaveChangesAsync();

            Console.WriteLine("Дані збережено без виклику процедури.");

            return Ok(new { message = "Дані збережено!", id = data.Id });
        }


        //[Authorize]
        [HttpGet]
        public async Task<IEnumerable<SensorData>> GetSensorData()
        {
            return await _context.SensorData.ToListAsync();
        }

        [Authorize]
        [HttpGet("secure-test")]
        public IActionResult SecureTest()
        {
            var username = User.Identity?.Name;
            return Ok($"🔒 Привіт, {username}. Доступ дозволено.");
        }


        [HttpGet("ownership/{chipId}/latest")]
        public async Task<IActionResult> GetOwnershipForEsp([FromRoute] string chipId)
        {
            if (string.IsNullOrWhiteSpace(chipId))
                return BadRequest("chipId is required");

            // Нормалізуємо, щоб збігалося з тим, як ти зберігаєш у БД
            var normalized = chipId.Trim().ToUpperInvariant();

            // Беремо ОСТАННІЙ запис для цього ChipId
            var ownership = await _context.SensorOwnerships
                .AsNoTracking()
                .Include(o => o.User)
                .Where(o => o.ChipId == normalized)
                .OrderByDescending(o => o.UpdatedAt)
                .FirstOrDefaultAsync();

            if (ownership == null)
                return NotFound();

            // УНІКАЛЬНИЙ ETag НА РЕСУРС: ChipId-Version (щоб різні чипи не колізували)
            var etag = new EntityTagHeaderValue($"\"{ownership.ChipId}-{ownership.Version}\"");

            // If-None-Match → 304, якщо збігається саме з цим ресурсом
            var ifNoneMatch = Request.GetTypedHeaders().IfNoneMatch;
            if (ifNoneMatch != null && ifNoneMatch.Any(t => t.Tag == etag.Tag))
            {
                // Повертаємо 304 і дублюємо валідні заголовки
                var h304 = Response.GetTypedHeaders();
                h304.ETag = etag;
                h304.LastModified = new DateTimeOffset(ownership.UpdatedAt.ToUniversalTime());
                h304.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
                Response.Headers[HeaderNames.Pragma] = "no-cache";
                return StatusCode(StatusCodes.Status304NotModified);
            }

            // Заголовки відповіді 200
            var headers = Response.GetTypedHeaders();
            headers.ETag = etag;
            headers.LastModified = new DateTimeOffset(ownership.UpdatedAt.ToUniversalTime());
            headers.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
            Response.Headers[HeaderNames.Pragma] = "no-cache";

            // DTO (camelCase забезпечується налаштуваннями в Program.cs)
            var dto = new OwnershipSyncDto
            {
                Username = ownership.User?.Username ?? "",
                RoomName = ownership.RoomName ?? "",
                ImageName = ownership.ImageName ?? ""
            };

            _logger.LogInformation(
                "OWN OUT: id={Id} chip={Chip} ver={Ver} upd={Upd:o} user={User} room={Room} img={Img}",
                ownership.Id, ownership.ChipId, ownership.Version, ownership.UpdatedAt,
                dto.Username, dto.RoomName, dto.ImageName);

            return Ok(dto);
        }

        [HttpPut("ownership")]
        public async Task<IActionResult> UpdateOwnership([FromBody] SensorOwnershipUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ChipId))
                return BadRequest("ChipId обов'язковий");
            if (dto.RoomName is null && dto.ImageName is null)
                return BadRequest("Немає полів для оновлення");

            var normalized = dto.ChipId.Trim().ToUpperInvariant();

            var ownership = await _context.SensorOwnerships
                .FirstOrDefaultAsync(o => o.ChipId == normalized);

            if (ownership == null)
                return NotFound("Пристрій не знайдено");

            // (Опційно) оптимістична конкуренція: клієнт шле If-Match: "<currentEtag>"
            if (Request.Headers.TryGetValue("If-Match", out var ifMatch))
            {
                // 🔽 ТУТ: перевіряємо по ChipId-Version
                var currentEtag = $"\"{ownership.ChipId}-{ownership.Version}\""; // важливо: в лапках
                if (ifMatch.ToString() != currentEtag)
                    return StatusCode(StatusCodes.Status412PreconditionFailed);
            }

            bool changed = false;

            if (dto.RoomName != null)
            {
                var v = dto.RoomName.Trim();
                if (v.Length == 0) return BadRequest("RoomName не може бути порожнім");
                if (!string.Equals(ownership.RoomName, v, StringComparison.Ordinal))
                { ownership.RoomName = v; changed = true; }
            }

            if (dto.ImageName != null)
            {
                var v = dto.ImageName.Trim();
                if (v.Length == 0) return BadRequest("ImageName не може бути порожнім");
                if (!string.Equals(ownership.ImageName, v, StringComparison.Ordinal))
                { ownership.ImageName = v; changed = true; }
            }

            if (!changed)
            {
                // 🔽 ТУТ: нічого не змінилось — повертаємо поточний ChipId-Version
                Response.Headers.ETag = $"\"{ownership.ChipId}-{ownership.Version}\"";
                return NoContent(); // 204 без тіла
            }

            ownership.Version++;
            ownership.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 🔽 ТУТ: після збереження повертаємо новий ChipId-Version
            Response.Headers.ETag = $"\"{ownership.ChipId}-{ownership.Version}\"";
            return NoContent();
        }

        [HttpDelete("ownership/{chipId}/user/{userId}")]
        public async Task<IActionResult> DeleteOwnership(string chipId, int userId)
        {
            if (string.IsNullOrWhiteSpace(chipId)) return BadRequest("chipId required");
            var norm = chipId.Trim().ToUpperInvariant();

            var ow = await _context.SensorOwnerships
                .FirstOrDefaultAsync(o => o.ChipId == norm && o.UserId == userId);
            if (ow == null) return NotFound();

            _context.SensorOwnerships.Remove(ow);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
