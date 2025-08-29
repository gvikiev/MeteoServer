using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MySensorApi.DTO;
using MySensorApi.Models;
using MySensorApi.Services;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settings;

        public SettingsController(ISettingsService settings) => _settings = settings;

        private int? TryGetUserId()
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(s, out var id) ? id : null;
        }

        // ---- Settings (base) ----
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Setting>>> GetSettings(CancellationToken ct)
            => Ok(await _settings.GetAllAsync(ct));

        [HttpGet("{name}")]
        public async Task<ActionResult<Setting>> GetSettingByName(string name, CancellationToken ct)
        {
            var s = await _settings.GetByNameAsync(name, ct);
            return s is null ? NotFound($"Налаштування '{name}' не знайдено") : Ok(s);
        }

        // [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> UpsertSetting([FromBody] SettingUpsertDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.ParameterName))
                return BadRequest("ParameterName is required");

            await _settings.UpsertAsync(dto, ct);
            return NoContent();
        }

        // ==== User-level Adjustments (сумісність) ====
        [HttpGet("adjustments/{parameterName}")]
        public async Task<ActionResult<object>> GetAdjustment(string parameterName, CancellationToken ct)
        {
            var userId = TryGetUserId(); if (userId is null) return Unauthorized();

            var (payload, etag) = await _settings.GetAdjustmentAsync(parameterName, userId.Value, ct);

            var headers = Response.GetTypedHeaders();
            headers.ETag = new EntityTagHeaderValue(etag);
            headers.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };

            return Ok(payload);
        }

        [HttpPut("adjustments/{parameterName}")]
        public async Task<IActionResult> PutAdjustment(string parameterName, [FromBody] AdjustmentCreateDto dto, CancellationToken ct)
        {
            var userId = TryGetUserId(); if (userId is null) return Unauthorized();

            var ifMatch = Request.GetTypedHeaders().IfMatch;
            var tag = ifMatch?.FirstOrDefault()?.Tag.ToString() ?? string.Empty;

            try
            {
                var (_, newEtag) = await _settings.PutAdjustmentAsync(parameterName, userId.Value, dto, tag, ct);

                var headers = Response.GetTypedHeaders();
                headers.ETag = new EntityTagHeaderValue(newEtag);
                headers.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "428")
            {
                return StatusCode(StatusCodes.Status428PreconditionRequired, "Missing If-Match");
            }
            catch (InvalidOperationException ex) when (ex.Message == "412")
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Setting not found");
            }
        }

        // ==== Chip-level Adjustments (НОВЕ) ====

        // GET: /api/settings/adjustments/{chipId}/{parameterName}
        [HttpGet("adjustments/{chipId}/{parameterName}")]
        public async Task<ActionResult<object>> GetAdjustmentForChip(string chipId, string parameterName, CancellationToken ct)
        {
            var (payload, etag) = await _settings.GetAdjustmentForChipAsync(parameterName, chipId, ct);

            var headers = Response.GetTypedHeaders();
            headers.ETag = new EntityTagHeaderValue(etag);
            headers.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };

            return Ok(payload);
        }

        // PUT: /api/settings/adjustments/{chipId}/{parameterName}
        [HttpPut("adjustments/{chipId}/{parameterName}")]
        public async Task<IActionResult> PutAdjustmentForChip(string chipId, string parameterName, [FromBody] AdjustmentCreateDto dto, CancellationToken ct)
        {
            var ifMatch = Request.GetTypedHeaders().IfMatch;
            var tag = ifMatch?.FirstOrDefault()?.Tag.ToString() ?? string.Empty;

            try
            {
                var (_, newEtag) = await _settings.PutAdjustmentForChipAsync(parameterName, chipId, dto, tag, ct);

                var headers = Response.GetTypedHeaders();
                headers.ETag = new EntityTagHeaderValue(newEtag);
                headers.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "428")
            {
                return StatusCode(StatusCodes.Status428PreconditionRequired, "Missing If-Match");
            }
            catch (InvalidOperationException ex) when (ex.Message == "412")
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // POST: /api/settings/adjustments/{chipId}  (bulk із абсолютними значеннями для конкретної плати)
        [HttpPost("adjustments/{chipId}")]
        public async Task<ActionResult<AdjustmentAbsoluteResponseDto>> PostAdjustmentsForChip(
            string chipId,
            [FromBody] AdjustmentAbsoluteRequestDto req,
            CancellationToken ct)
        {
            if (req?.Items == null || req.Items.Count == 0)
                return BadRequest("Items are required");

            try
            {
                var res = await _settings.SaveAdjustmentsForChipFromAbsoluteAsync(chipId, req.Items, ct);
                return Ok(res);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // ---- Effective + Advice ----

        [HttpGet("effective")]
        public async Task<ActionResult<object>> GetEffective(CancellationToken ct)
        {
            var userId = TryGetUserId();
            var payload = await _settings.GetEffectiveAsync(userId, ct);
            return Ok(payload);
        }

        [HttpGet("effective/{chipId}")]
        public async Task<ActionResult<IEnumerable<EffectiveSettingDto>>> GetEffectiveByChip(string chipId, CancellationToken ct)
        {
            try
            {
                var payload = await _settings.GetEffectiveByChipAsync(chipId, ct);
                return Ok(payload);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("advice/{chipId}/latest")]
        public async Task<ActionResult<object>> ComputeLatestAdvice(string chipId, CancellationToken ct)
        {
            try
            {
                var (dataDto, advice) = await _settings.ComputeLatestAdviceAsync(chipId, ct);
                return Ok(new { data = dataDto, advice });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("advice/{chipId}/save-latest")]
        public async Task<IActionResult> SaveLatestAdvice(string chipId, CancellationToken ct)
        {
            try
            {
                var (saved, count) = await _settings.SaveLatestAdviceAsync(chipId, ct);
                if (!saved) return NoContent();
                return Ok(new { saved, count });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("advice/{chipId}/history")]
        public async Task<ActionResult<IEnumerable<object>>> GetAdviceHistory(string chipId, [FromQuery] int take = 20, CancellationToken ct = default)
        {
            var list = await _settings.GetAdviceHistoryAsync(chipId, take, ct);
            return Ok(list);
        }
    }
}
