using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MySensorApi.Data;
using MySensorApi.Models;
using MySensorApi.DTO;

namespace MySensorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SettingsController(AppDbContext context) => _context = context;

        // ---------------------------- Helpers ----------------------------
        private static string NormalizeChip(string? raw) =>
            (raw ?? string.Empty).Trim().ToUpperInvariant();

        private int? TryGetUserId()
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(s, out var id) ? id : null;
        }

        private sealed record EffSetting(float? Low, float? High, string? LowMsg, string? HighMsg);

        private async Task<Dictionary<string, EffSetting>> GetEffectiveSettingsAsync(
            int? userId, CancellationToken ct)
        {
            var names = new[] { "temperature", "humidity", "gas" };

            var baseSettings = await _context.Settings
                .AsNoTracking()
                .Where(s => names.Contains(s.ParameterName))
                .ToListAsync(ct);

            var eff = baseSettings.ToDictionary(
                s => s.ParameterName,
                s => new EffSetting(s.LowValue, s.HighValue, s.LowValueMessage, s.HighValueMessage),
                StringComparer.OrdinalIgnoreCase);

            if (userId is null) return eff;

            var ids = baseSettings.Select(s => s.Id).ToList();

            // беремо ОСТАННЮ версію для кожного SettingId
            var lastAdj = await _context.SettingsUserAdjustments
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value && ids.Contains(a.SettingId))
                .GroupBy(a => a.SettingId)
                .Select(g => g.OrderByDescending(x => x.Version).First())
                .ToListAsync(ct);

            float? Add(float? @base, float delta) =>
                @base.HasValue ? @base.Value + delta : (float?)null;

            foreach (var adj in lastAdj)
            {
                var setting = baseSettings.First(x => x.Id == adj.SettingId);
                var cur = eff[setting.ParameterName];
                eff[setting.ParameterName] = new EffSetting(
                    Add(cur.Low, adj.LowValueAdjustment),
                    Add(cur.High, adj.HighValueAdjustment),
                    cur.LowMsg,
                    cur.HighMsg
                );
            }

            return eff;
        }

        private static List<string> BuildAdvice(SensorData s, IDictionary<string, EffSetting> eff)
        {
            var msgs = new List<string>();

            float? t = s.TemperatureDht ?? s.TemperatureBme;
            float? h = s.HumidityDht ?? s.HumidityBme;

            if (eff.TryGetValue("temperature", out var T) && t.HasValue)
            {
                if (T.Low.HasValue && t < T.Low && !string.IsNullOrWhiteSpace(T.LowMsg)) msgs.Add(T.LowMsg!);
                if (T.High.HasValue && t > T.High && !string.IsNullOrWhiteSpace(T.HighMsg)) msgs.Add(T.HighMsg!);
            }
            if (eff.TryGetValue("humidity", out var H) && h.HasValue)
            {
                if (H.Low.HasValue && h < H.Low && !string.IsNullOrWhiteSpace(H.LowMsg)) msgs.Add(H.LowMsg!);
                if (H.High.HasValue && h > H.High && !string.IsNullOrWhiteSpace(H.HighMsg)) msgs.Add(H.HighMsg!);
            }
            if (eff.TryGetValue("gas", out var G))
            {
                if (s.GasDetected == true && !string.IsNullOrWhiteSpace(G.HighMsg))
                {
                    msgs.Add(G.HighMsg!);
                }
                else if (s.Mq2AnalogPercent.HasValue && G.High.HasValue &&
                         s.Mq2AnalogPercent > G.High && !string.IsNullOrWhiteSpace(G.HighMsg))
                {
                    msgs.Add(G.HighMsg!);
                }
            }
            return msgs;
        }

        // ---------------------------- Settings (base) ----------------------------

        // GET: /api/settings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Setting>>> GetSettings(CancellationToken ct)
            => Ok(await _context.Settings.AsNoTracking().ToListAsync(ct));

        // GET: /api/settings/{name}
        [HttpGet("{name}")]
        public async Task<ActionResult<Setting>> GetSettingByName(string name, CancellationToken ct)
        {
            var s = await _context.Settings.AsNoTracking()
                       .FirstOrDefaultAsync(x => x.ParameterName == name, ct);
            return s is null ? NotFound($"Налаштування '{name}' не знайдено") : Ok(s);
        }

        // PUT: /api/settings  (upsert by ParameterName)
        // [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> UpsertSetting([FromBody] SettingUpsertDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.ParameterName))
                return BadRequest("ParameterName is required");

            var name = dto.ParameterName.Trim();
            var s = await _context.Settings.FirstOrDefaultAsync(x => x.ParameterName == name, ct);

            if (s is null)
            {
                s = new Setting
                {
                    ParameterName = name,
                    LowValue = dto.LowValue,
                    HighValue = dto.HighValue,
                    LowValueMessage = dto.LowValueMessage,
                    HighValueMessage = dto.HighValueMessage,
                };
                _context.Settings.Add(s);
            }
            else
            {
                s.LowValue = dto.LowValue;
                s.HighValue = dto.HighValue;
                s.LowValueMessage = dto.LowValueMessage;
                s.HighValueMessage = dto.HighValueMessage;
            }

            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        // ---------------------------- Adjustments via ETag (GET + PUT) ----------------------------

        // GET: /api/settings/adjustments/{parameterName}
        // Поточний стан дельти + ETag (останній Version)
        [HttpGet("adjustments/{parameterName}")]
        public async Task<ActionResult<object>> GetAdjustment(string parameterName, CancellationToken ct)
        {
            var userId = TryGetUserId(); if (userId is null) return Unauthorized();

            var setting = await _context.Settings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ParameterName == parameterName, ct);
            if (setting is null) return NotFound("Setting not found");

            var last = await _context.SettingsUserAdjustments.AsNoTracking()
                .Where(a => a.UserId == userId && a.SettingId == setting.Id)
                .OrderByDescending(a => a.Version)   // ⚠️ потрібне поле Version
                .FirstOrDefaultAsync(ct);

            var ver = last?.Version ?? 0;
            var etag = new EntityTagHeaderValue($"\"{userId}-{setting.Id}-{ver}\"");

            var headers = Response.GetTypedHeaders();
            headers.ETag = etag;
            headers.CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };

            return Ok(new
            {
                parameterName,
                lowValueAdjustment = last?.LowValueAdjustment ?? 0f,
                highValueAdjustment = last?.HighValueAdjustment ?? 0f,
                version = ver
            });
        }

        // PUT: /api/settings/adjustments/{parameterName}
        // Оновлення через If-Match: створюємо НОВУ версію (append-only історія)
        [HttpPut("adjustments/{parameterName}")]
        public async Task<IActionResult> PutAdjustment(string parameterName, [FromBody] AdjustmentCreateDto dto, CancellationToken ct)
        {
            var userId = TryGetUserId(); if (userId is null) return Unauthorized();

            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.ParameterName == parameterName, ct);
            if (setting is null) return NotFound("Setting not found");

            var ifMatch = Request.GetTypedHeaders().IfMatch;
            if (ifMatch == null || ifMatch.Count == 0)
                return StatusCode(StatusCodes.Status428PreconditionRequired, "Missing If-Match");

            var last = await _context.SettingsUserAdjustments.AsNoTracking()
                .Where(a => a.UserId == userId && a.SettingId == setting.Id)
                .OrderByDescending(a => a.Version)
                .FirstOrDefaultAsync(ct);

            var currentVer = last?.Version ?? 0;
            var currentEtag = new EntityTagHeaderValue($"\"{userId}-{setting.Id}-{currentVer}\"");
            if (!ifMatch.Any(t => t.Tag == currentEtag.Tag))
                return StatusCode(StatusCodes.Status412PreconditionFailed);

            var nextVer = currentVer + 1;

            _context.SettingsUserAdjustments.Add(new SettingsUserAdjustment
            {
                UserId = userId.Value,
                SettingId = setting.Id,
                LowValueAdjustment = dto.LowValueAdjustment,
                HighValueAdjustment = dto.HighValueAdjustment,
                Version = nextVer,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(ct);

            var newEtag = new EntityTagHeaderValue($"\"{userId}-{setting.Id}-{nextVer}\"");
            Response.GetTypedHeaders().ETag = newEtag;
            Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue { NoStore = true, NoCache = true, MustRevalidate = true };
            return NoContent();
        }

        // ---------------------------- Effective + Advice ----------------------------

        // GET: /api/settings/effective
        [HttpGet("effective")]
        public async Task<ActionResult<object>> GetEffective(CancellationToken ct)
        {
            var userId = TryGetUserId();
            var eff = await GetEffectiveSettingsAsync(userId, ct);

            var payload = eff.Select(kv => new
            {
                parameterName = kv.Key,
                lowValue = kv.Value.Low,
                highValue = kv.Value.High,
                lowValueMessage = kv.Value.LowMsg,
                highValueMessage = kv.Value.HighMsg
            });

            return Ok(payload);
        }

        // GET: /api/settings/advice/{chipId}/latest
        [HttpGet("advice/{chipId}/latest")]
        public async Task<ActionResult<object>> ComputeLatestAdvice(string chipId, CancellationToken ct)
        {
            var norm = NormalizeChip(chipId);

            var latest = await _context.SensorData
                .AsNoTracking()
                .Where(s => s.ChipId == norm)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (latest is null) return NotFound("Немає сенсорних даних");

            var ownership = await _context.SensorOwnerships
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.ChipId == norm, ct);

            var eff = await GetEffectiveSettingsAsync(ownership?.UserId, ct);
            var advice = BuildAdvice(latest, eff);

            var dataDto = new
            {
                chipId = latest.ChipId,
                roomName = ownership?.RoomName ?? "",
                latest.TemperatureDht,
                latest.HumidityDht,
                latest.GasDetected,
                latest.Pressure,
                latest.Altitude,
                latest.TemperatureBme,
                latest.HumidityBme,
                latest.Mq2Analog,
                latest.Mq2AnalogPercent,
                latest.LightAnalog,
                latest.LightAnalogPercent,
                latest.Light
            };

            return Ok(new { data = dataDto, advice });
        }

        // POST: /api/settings/advice/{chipId}/save-latest
        [HttpPost("advice/{chipId}/save-latest")]
        public async Task<IActionResult> SaveLatestAdvice(string chipId, CancellationToken ct)
        {
            var norm = NormalizeChip(chipId);

            var latest = await _context.SensorData
                .Where(s => s.ChipId == norm)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (latest is null) return NotFound("Немає сенсорних даних");

            var ownership = await _context.SensorOwnerships
                .FirstOrDefaultAsync(o => o.ChipId == norm, ct);

            if (ownership is null) return NotFound("Власника/кімнату не знайдено");

            var eff = await GetEffectiveSettingsAsync(ownership.UserId, ct);
            var advice = BuildAdvice(latest, eff);

            if (advice.Count == 0) return NoContent();

            _context.ComfortRecommendations.Add(new ComfortRecommendation
            {
                SensorOwnershipId = ownership.Id,
                Recommendation = string.Join("\n", advice),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(ct);

            return Ok(new { saved = true, count = advice.Count });
        }

        // GET: /api/settings/advice/{chipId}/history?take=20
        [HttpGet("advice/{chipId}/history")]
        public async Task<ActionResult<IEnumerable<object>>> GetAdviceHistory(
            string chipId, [FromQuery] int take = 20, CancellationToken ct = default)
        {
            var norm = NormalizeChip(chipId);

            var list = await _context.ComfortRecommendations
                .AsNoTracking()
                .Where(r => r.SensorOwnership.ChipId == norm)
                .OrderByDescending(r => r.CreatedAt)
                .Take(Math.Clamp(take, 1, 200))
                .Select(r => new
                {
                    createdAt = r.CreatedAt,
                    roomName = r.SensorOwnership.RoomName,
                    recommendation = r.Recommendation
                })
                .ToListAsync(ct);

            return Ok(list);
        }
    }
}
