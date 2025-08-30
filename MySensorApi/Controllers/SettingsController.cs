using Microsoft.AspNetCore.Mvc;
using MySensorApi.DTO.Recommendations;
using MySensorApi.DTO.Settings;
using MySensorApi.Services;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settings;

        public SettingsController(ISettingsService settings) => _settings = settings;

        // ---- Advice ----

        // GET: /api/settings/advice/{chipId}/latest
        [HttpGet("advice/{chipId}/latest")]
        public async Task<ActionResult<RecommendationsDto>> GetLatestAdvice(string chipId, CancellationToken ct)
        {
            var dto = await _settings.ComputeLatestAdviceAsync(chipId, ct);
            return Ok(dto);
        }

        // POST: /api/settings/advice/{chipId}/save-latest
        [HttpPost("advice/{chipId}/save-latest")]
        public async Task<ActionResult<SaveLatestRecommendationDto>> SaveLatestAdvice(string chipId, CancellationToken ct)
        {
            var dto = await _settings.SaveLatestAdviceAsync(chipId, ct);
            return Ok(dto); // завжди 200 з тілом DTO, без деструктуризації
        }

        // GET: /api/settings/advice/{chipId}/history?take=20
        [HttpGet("advice/{chipId}/history")]
        public async Task<ActionResult<IEnumerable<RecommendationHistoryDto>>> GetAdviceHistory(
            string chipId, [FromQuery] int take = 20, CancellationToken ct = default)
        {
            var list = await _settings.GetAdviceHistoryAsync(chipId, take, ct);
            return Ok(list);
        }

        // ---- Абсолютні пороги для конкретної плати ----

        // POST: /api/settings/adjustments/{chipId}
        [HttpPost("adjustments/{chipId}")]
        public async Task<ActionResult<AdjustmentAbsoluteResponseDto>> PostAdjustmentsForChip(
            string chipId,
            [FromBody] AdjustmentAbsoluteRequestDto req,
            CancellationToken ct)
        {
            if (req?.Items == null || req.Items.Count == 0)
                return BadRequest("Items are required");

            var res = await _settings.SaveAdjustmentsForChipFromAbsoluteAsync(chipId, req.Items, ct);
            return Ok(res);
        }

        // ---- Ефективні пороги ----

        // GET: /api/settings/effective/{chipId}
        [HttpGet("effective/{chipId}")]
        public async Task<ActionResult<IEnumerable<EffectiveSettingDto>>> GetEffectiveByChip(string chipId, CancellationToken ct)
        {
            var payload = await _settings.GetEffectiveByChipAsync(chipId, ct);
            return Ok(payload);
        }
    }
}
