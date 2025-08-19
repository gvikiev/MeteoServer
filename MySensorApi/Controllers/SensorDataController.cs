using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.DTO;
using MySensorApi.Models;
using System.Security.Claims;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SensorDataController(AppDbContext context)
        {
            _context = context;
        }

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
        public async Task<IActionResult> GetOwnershipForEsp(string chipId) // _ts = cache buster
        {
            var normalized = chipId.Trim().ToUpperInvariant();

            var ownership = await _context.SensorOwnerships
                .Where(o => o.ChipId == normalized)
                .OrderByDescending(o => o.UpdatedAt) // ← беремо останній
                .Include(o => o.User)
                .AsNoTracking()                       // ← без кешу EF
                .FirstOrDefaultAsync();

            if (ownership == null) return NotFound();

            var etag = $"\"{ownership.Version}\"";    // у лапках
            if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.ToString() == etag)
                return StatusCode(StatusCodes.Status304NotModified);

            Response.Headers.ETag = etag;
            Response.Headers.LastModified = ownership.UpdatedAt.ToUniversalTime().ToString("R");

            // ⬇️ тимчасово, щоб Swagger/браузер НЕ кешували під час дебагу
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";

            return Ok(new OwnershipSyncDto
            {
                Username = ownership.User?.Username ?? "",
                RoomName = ownership.RoomName,
            });
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
                var currentEtag = $"\"{ownership.Version}\""; // важливо: в лапках
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
                // Нічого не змінилось — просто повертаємо поточний ETag
                Response.Headers.ETag = $"\"{ownership.Version}\"";
                return NoContent(); // 204 без тіла — ОК для PUT
            }

            ownership.Version++;
            ownership.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Дай клієнту новий ETag (юзно для подальших If-Match / If-None-Match)
            Response.Headers.ETag = $"\"{ownership.Version}\"";
            return NoContent();
        }


    }
}
