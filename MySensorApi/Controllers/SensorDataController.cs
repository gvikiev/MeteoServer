using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers; // EntityTagHeaderValue, HeaderNames
using MySensorApi.Data;
using MySensorApi.DTO;
using MySensorApi.Models;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // -> api/sensordata/...
    public class SensorDataController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SensorDataController> _logger;

        public SensorDataController(AppDbContext context, ILogger<SensorDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ----------------------
        // Helpers
        // ----------------------
        private static string NormalizeChip(string? raw) =>
            (raw ?? string.Empty).Trim().ToUpperInvariant();

        private int? TryGetUserId()
        {
            // Підігнай під свої клейми з JWT
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(s, out var id) ? id : null;
        }

        // ----------------------
        // POST api/sensordata
        // ----------------------
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data, CancellationToken ct)
        {
            if (data == null) return BadRequest("Body is required");

            data.ChipId = NormalizeChip(data.ChipId);
            data.CreatedAt = DateTime.UtcNow;

            _context.SensorData.Add(data);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("SensorData saved: id={Id}, chip={Chip}, at={At:o}", data.Id, data.ChipId, data.CreatedAt);
            return Ok(new { message = "Дані збережено!", id = data.Id });
        }

        // ------------------------------------------------
        // GET api/sensordata/{chipId}/latest
        // roomName тягнемо з SensorOwnerships (для цього юзера, якщо є)
        // ------------------------------------------------
        [HttpGet("{chipId}/latest")]
        public async Task<ActionResult<SensorDataDto>> GetLatest(string chipId, CancellationToken ct)
        {
            var norm = NormalizeChip(chipId);
            var userId = TryGetUserId();

            var dto = await _context.SensorData
                .AsNoTracking()
                .Where(s => s.ChipId == norm)
                .OrderByDescending(s => s.CreatedAt) // підстав своє поле, якщо інше
                .Select(s => new SensorDataDto
                {
                    ChipId = s.ChipId,
                    RoomName = (userId != null
                        ? _context.SensorOwnerships
                            .Where(o => o.ChipId == s.ChipId && o.UserId == userId.Value)
                            .Select(o => o.RoomName)
                            .FirstOrDefault()
                        : _context.SensorOwnerships
                            .Where(o => o.ChipId == s.ChipId)
                            .Select(o => o.RoomName)
                            .FirstOrDefault()) ?? string.Empty,

                    TemperatureDht = s.TemperatureDht,
                    HumidityDht = s.HumidityDht,
                    GasDetected = s.GasDetected,
                    Light = s.Light,
                    Pressure = s.Pressure,
                    Altitude = s.Altitude,
                    TemperatureBme = s.TemperatureBme,
                    HumidityBme = s.HumidityBme,
                    Mq2Analog = s.Mq2Analog,
                    Mq2AnalogPercent = s.Mq2AnalogPercent,
                    LightAnalog = s.LightAnalog,
                    LightAnalogPercent = s.LightAnalogPercent
                })
                .FirstOrDefaultAsync(ct);

            return dto is null ? NotFound() : Ok(dto);
        }

        // ---------------------------------------------------------------------
        // GET api/sensordata/{chipId}/history?take=50&from=2025-08-01&to=2025-08-21
        // ---------------------------------------------------------------------
        [HttpGet("{chipId}/history")]
        public async Task<ActionResult<IEnumerable<SensorDataDto>>> GetHistory(
            string chipId,
            [FromQuery] int take = 50,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken ct = default)
        {
            var norm = NormalizeChip(chipId);
            var userId = TryGetUserId();

            var q = _context.SensorData.AsNoTracking().Where(s => s.ChipId == norm);
            if (from.HasValue) q = q.Where(s => s.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(s => s.CreatedAt <= to.Value);

            var list = await q
                .OrderByDescending(s => s.CreatedAt)
                .Take(Math.Clamp(take, 1, 500))
                .Select(s => new SensorDataDto
                {
                    ChipId = s.ChipId,
                    RoomName = (userId != null
                        ? _context.SensorOwnerships
                            .Where(o => o.ChipId == s.ChipId && o.UserId == userId.Value)
                            .Select(o => o.RoomName)
                            .FirstOrDefault()
                        : _context.SensorOwnerships
                            .Where(o => o.ChipId == s.ChipId)
                            .Select(o => o.RoomName)
                            .FirstOrDefault()) ?? string.Empty,

                    TemperatureDht = s.TemperatureDht,
                    HumidityDht = s.HumidityDht,
                    GasDetected = s.GasDetected,
                    Light = s.Light,
                    Pressure = s.Pressure,
                    Altitude = s.Altitude,
                    TemperatureBme = s.TemperatureBme,
                    HumidityBme = s.HumidityBme,
                    Mq2Analog = s.Mq2Analog,
                    Mq2AnalogPercent = s.Mq2AnalogPercent,
                    LightAnalog = s.LightAnalog,
                    LightAnalogPercent = s.LightAnalogPercent
                })
                .ToListAsync(ct);

            return list;
        }

        // ------------------------
        // GET api/sensordata/secure-test
        // ------------------------
        [Authorize]
        [HttpGet("secure-test")]
        public IActionResult SecureTest()
        {
            var username = User.Identity?.Name ?? "(unknown)";
            return Ok($"🔒 Привіт, {username}. Доступ дозволено.");
        }

        // --------------------------------------------------------------------
        // GET api/sensordata/ownership/{chipId}/latest  (для ESP/синху)
        // з ETag = "CHIPID-Version"
        // --------------------------------------------------------------------
        [HttpGet("ownership/{chipId}/latest")]
        public async Task<IActionResult> GetOwnershipForEsp([FromRoute] string chipId, CancellationToken ct)
        {
            var norm = NormalizeChip(chipId);
            if (string.IsNullOrEmpty(norm)) return BadRequest("chipId is required");

            var ownership = await _context.SensorOwnerships
                .AsNoTracking()
                .Include(o => o.User)
                .Where(o => o.ChipId == norm)
                .OrderByDescending(o => o.UpdatedAt)
                .FirstOrDefaultAsync(ct);

            if (ownership == null) return NotFound();

            var etag = new EntityTagHeaderValue($"\"{ownership.ChipId}-{ownership.Version}\"");

            var ifNoneMatch = Request.GetTypedHeaders().IfNoneMatch;
            if (ifNoneMatch != null && ifNoneMatch.Any(t => t.Tag == etag.Tag))
            {
                var h304 = Response.GetTypedHeaders();
                h304.ETag = etag;
                h304.LastModified = new DateTimeOffset(ownership.UpdatedAt.ToUniversalTime());
                h304.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
                Response.Headers[HeaderNames.Pragma] = "no-cache";
                return StatusCode(StatusCodes.Status304NotModified);
            }

            var headers = Response.GetTypedHeaders();
            headers.ETag = etag;
            headers.LastModified = new DateTimeOffset(ownership.UpdatedAt.ToUniversalTime());
            headers.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
            Response.Headers[HeaderNames.Pragma] = "no-cache";

            var dto = new OwnershipSyncDto
            {
                Username = ownership.User?.Username ?? "",
                RoomName = ownership.RoomName ?? "",
                ImageName = ownership.ImageName ?? ""
            };

            _logger.LogInformation("OWN OUT: id={Id} chip={Chip} ver={Ver} upd={Upd:o} user={User} room={Room} img={Img}",
                ownership.Id, ownership.ChipId, ownership.Version, ownership.UpdatedAt, dto.Username, dto.RoomName, dto.ImageName);

            return Ok(dto);
        }

        // -----------------------------------------
        // PUT api/sensordata/ownership  (оновити room/image)
        // If-Match: "CHIPID-Version"  → 412 при невідповідності
        // -----------------------------------------
        [HttpPut("ownership")]
        public async Task<IActionResult> UpdateOwnership([FromBody] SensorOwnershipUpdateDto dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest("Body is required");
            if (string.IsNullOrWhiteSpace(dto.ChipId)) return BadRequest("ChipId обов'язковий");
            if (dto.RoomName is null && dto.ImageName is null)
                return BadRequest("Немає полів для оновлення");

            var norm = NormalizeChip(dto.ChipId);

            var ownership = await _context.SensorOwnerships
                .FirstOrDefaultAsync(o => o.ChipId == norm, ct);

            if (ownership == null) return NotFound("Пристрій не знайдено");

            if (Request.Headers.TryGetValue("If-Match", out var ifMatch))
            {
                var currentEtag = $"\"{ownership.ChipId}-{ownership.Version}\"";
                if (!string.Equals(ifMatch.ToString(), currentEtag, StringComparison.Ordinal))
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
                Response.Headers.ETag = $"\"{ownership.ChipId}-{ownership.Version}\"";
                return NoContent();
            }

            ownership.Version++;
            ownership.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            Response.Headers.ETag = $"\"{ownership.ChipId}-{ownership.Version}\"";
            return NoContent();
        }

        // ---------------------------------------------------------
        // DELETE api/sensordata/ownership/{chipId}/user/{userId}
        // ---------------------------------------------------------
        [HttpDelete("ownership/{chipId}/user/{userId}")]
        public async Task<IActionResult> DeleteOwnership(string chipId, int userId, CancellationToken ct)
        {
            var norm = NormalizeChip(chipId);
            if (string.IsNullOrEmpty(norm)) return BadRequest("chipId required");

            var ow = await _context.SensorOwnerships
                .FirstOrDefaultAsync(o => o.ChipId == norm && o.UserId == userId, ct);

            if (ow == null) return NotFound();

            _context.SensorOwnerships.Remove(ow);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
