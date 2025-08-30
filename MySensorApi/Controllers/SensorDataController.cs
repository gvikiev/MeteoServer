using Microsoft.AspNetCore.Mvc;
using MySensorApi.DTO;                // <-- додано: тут живе SensorOwnershipDto
using MySensorApi.DTO.Charts;
using MySensorApi.DTO.SensorData;
using MySensorApi.Models;
using MySensorApi.Services;
using System.Security.Claims;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // -> api/sensordata/...
    public class SensorDataController : ControllerBase
    {
        private readonly ISensorDataService _sensorData;
        private readonly IOwnershipService _ownership;
        private readonly ILogger<SensorDataController> _logger;

        public SensorDataController(
            ISensorDataService sensorData,
            IOwnershipService ownership,
            ILogger<SensorDataController> logger)
        {
            _sensorData = sensorData;
            _ownership = ownership;
            _logger = logger;
        }

        private int? TryGetUserId()
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(s, out var id) ? id : null;
        }

        // --------- POST: прийом телеметрії від ESP32 ---------
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data, CancellationToken ct)
        {
            if (data == null) return BadRequest("Body is required");
            await _sensorData.SaveAsync(data, ct);
            _logger.LogInformation("SensorData saved: chip={Chip}", data.ChipId);
            return Ok(new { message = "Дані збережено!", id = data.Id });
        }

        // --------- LATEST: останні дані по chipId ---------
        [HttpGet("{chipId}/latest")]
        public async Task<ActionResult<SensorDataDto>> GetLatest(string chipId, CancellationToken ct)
        {
            var dto = await _sensorData.GetLatestAsync(chipId, TryGetUserId(), ct);
            return dto is null ? NotFound() : dto;
        }

        // --------- ESP32 SYNC (ETag) — НЕ ЧІПАЄМО ---------
        [HttpGet("ownership/{chipId}/latest")]
        public async Task<IActionResult> GetOwnershipForEsp([FromRoute] string chipId, CancellationToken ct)
        {
            var (dto, etag, lastModified) = await _ownership.GetSyncForEspAsync(chipId, ct);
            if (dto is null) return NotFound();

            var reqEtags = Request.GetTypedHeaders().IfNoneMatch;
            if (!string.IsNullOrEmpty(etag) && reqEtags != null &&
                reqEtags.Any(t => string.Equals(t.Tag.ToString(), etag, StringComparison.Ordinal)))
            {
                Response.GetTypedHeaders().ETag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue(etag);
                if (lastModified.HasValue) Response.GetTypedHeaders().LastModified = new DateTimeOffset(lastModified.Value);
                Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                {
                    NoStore = true,
                    NoCache = true,
                    MustRevalidate = true
                };
                Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Pragma] = "no-cache";
                return StatusCode(StatusCodes.Status304NotModified);
            }

            var headers = Response.GetTypedHeaders();
            if (!string.IsNullOrEmpty(etag)) headers.ETag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue(etag!);
            if (lastModified.HasValue) headers.LastModified = new DateTimeOffset(lastModified.Value);
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
            Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Pragma] = "no-cache";

            return Ok(dto);
        }

        // --------- OWNERSHIP UPDATE (PUT, If-Match) ---------
        [HttpPut("ownership")]
        public async Task<IActionResult> UpdateOwnership([FromBody] SensorOwnershipDto dto, CancellationToken ct)
        {
            try
            {
                var ifMatch = Request.Headers["If-Match"].ToString();
                var (updated, newEtag) = await _ownership.UpdateAsync(dto, ifMatch, ct);
                Response.Headers.ETag = newEtag;
                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "412")
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Пристрій не знайдено");
            }
            catch (InvalidOperationException ex) when (ex.Message == "428")
            {
                return StatusCode(StatusCodes.Status428PreconditionRequired, "Missing If-Match");
            }
        }

        // --------- OWNERSHIP DELETE ---------
        [HttpDelete("ownership/{chipId}/user/{userId}")]
        public async Task<IActionResult> DeleteOwnership(string chipId, int userId, CancellationToken ct)
        {
            var ok = await _ownership.DeleteAsync(chipId, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        // --------- SHORTCUTS для графіків (клієнт викликає day/week) ---------
        [HttpGet("ownership/{chipId}/day")]
        public async Task<ActionResult<IEnumerable<SensorPointDto>>> GetDaySeries(string chipId, CancellationToken ct)
        {
            var data = await _sensorData.GetSeriesAsync(chipId, null, null, TimeBucket.Hour, ct);
            return Ok(data);
        }

        [HttpGet("ownership/{chipId}/week")]
        public async Task<ActionResult<IEnumerable<SensorPointDto>>> GetWeekSeries(string chipId, CancellationToken ct)
        {
            var data = await _sensorData.GetSeriesAsync(chipId, null, null, TimeBucket.Day, ct);
            return Ok(data);
        }
    }
}
