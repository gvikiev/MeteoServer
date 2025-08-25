using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MySensorApi.DTO;
using MySensorApi.Models;
using MySensorApi.Services;
using System;
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

        // POST api/sensordata
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data, CancellationToken ct)
        {
            if (data == null) return BadRequest("Body is required");
            await _sensorData.SaveAsync(data, ct);
            _logger.LogInformation("SensorData saved: chip={Chip}", data.ChipId);
            return Ok(new { message = "Дані збережено!", id = data.Id });
        }

        // GET api/sensordata/{chipId}/latest
        [HttpGet("{chipId}/latest")]
        public async Task<ActionResult<SensorDataDto>> GetLatest(string chipId, CancellationToken ct)
        {
            var dto = await _sensorData.GetLatestAsync(chipId, TryGetUserId(), ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        // GET api/sensordata/{chipId}/history?take=50&from=...&to=...
        [HttpGet("{chipId}/history")]
        public async Task<ActionResult<IEnumerable<SensorDataDto>>> GetHistory(
            string chipId,
            [FromQuery] int take = 50,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken ct = default)
        {
            var list = await _sensorData.GetHistoryAsync(chipId, TryGetUserId(), from, to, take, ct);
            return Ok(list);
        }

        // GET api/sensordata/secure-test
        [Authorize]
        [HttpGet("secure-test")]
        public IActionResult SecureTest()
        {
            var username = User.Identity?.Name ?? "(unknown)";
            return Ok($"🔒 Привіт, {username}. Доступ дозволено.");
        }

        // GET api/sensordata/ownership/{chipId}/latest  (для ESP/синху) з підтримкою ETag/304
        [HttpGet("ownership/{chipId}/latest")]
        public async Task<IActionResult> GetOwnershipForEsp([FromRoute] string chipId, CancellationToken ct)
        {
            var (dto, etag, lastModified) = await _ownership.GetSyncForEspAsync(chipId, ct);
            if (dto is null) return NotFound();

            var reqEtags = Request.GetTypedHeaders().IfNoneMatch;
            if (!string.IsNullOrEmpty(etag) && reqEtags != null &&
                reqEtags.Any(t => string.Equals(t.Tag.ToString(), etag, StringComparison.Ordinal)))
            {
                var h304 = Response.GetTypedHeaders();
                h304.ETag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue(etag);
                if (lastModified.HasValue) h304.LastModified = new DateTimeOffset(lastModified.Value);
                h304.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
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

        // PUT api/sensordata/ownership  (If-Match → 412)
        [HttpPut("ownership")]
        public async Task<IActionResult> UpdateOwnership([FromBody] SensorOwnershipUpdateDto dto, CancellationToken ct)
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

        // DELETE api/sensordata/ownership/{chipId}/user/{userId}
        [HttpDelete("ownership/{chipId}/user/{userId}")]
        public async Task<IActionResult> DeleteOwnership(string chipId, int userId, CancellationToken ct)
        {
            var ok = await _ownership.DeleteAsync(chipId, userId, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
