using MySensorApi.DTO.Recommendations;
using MySensorApi.DTO.Settings;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;
using MySensorApi.Services.Utils;

namespace MySensorApi.Services
{
    public interface ISettingsService
    {
        Task<RecommendationsDto> ComputeLatestAdviceAsync(string chipId, CancellationToken ct);
        Task<SaveLatestRecommendationDto> SaveLatestAdviceAsync(string chipId, CancellationToken ct);
        Task<IEnumerable<RecommendationHistoryDto>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct);
        Task<AdjustmentAbsoluteResponseDto> SaveAdjustmentsForChipFromAbsoluteAsync(string chipId, IEnumerable<AdjustmentAbsoluteItemDto> items, CancellationToken ct);
        Task<IEnumerable<EffectiveSettingDto>> GetEffectiveByChipAsync(string chipId, CancellationToken ct);
    }

    internal sealed record EffSetting(float? Low, float? High, string? LowMsg, string? HighMsg);

    public sealed class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository _settingsRepo;
        private readonly ISensorDataRepository _sensorRepo;
        private readonly IOwnershipRepository _ownRepo;

        public SettingsService(ISettingsRepository settingsRepo, ISensorDataRepository sensorRepo, IOwnershipRepository ownRepo)
        {
            _settingsRepo = settingsRepo;
            _sensorRepo = sensorRepo;
            _ownRepo = ownRepo;
        }

        public async Task<RecommendationsDto> ComputeLatestAdviceAsync(string chipId, CancellationToken ct)
        {
            var norm = ChipId.Normalize(chipId);
            var latest = await _sensorRepo.GetLatestByChipIdAsync(norm, ct)
                ?? throw new KeyNotFoundException("Немає сенсорних даних");

            var ownership = await _ownRepo.GetByChipAsync(norm, ct)
                ?? throw new KeyNotFoundException("Chip not found");

            var effDict = await BuildEffectiveSettingsAsync(ownership.UserId, ownership.Id, ct);
            var advice = BuildAdvice(latest, effDict);

            return new RecommendationsDto
            {
                ChipId = norm,
                RoomName = ownership.RoomName,
                Advice = advice
            };
        }

        public async Task<SaveLatestRecommendationDto> SaveLatestAdviceAsync(string chipId, CancellationToken ct)
        {
            var norm = ChipId.Normalize(chipId);
            var ownership = await _ownRepo.GetByChipAsync(norm, ct)
                ?? throw new KeyNotFoundException("Chip not found");

            var eff = await BuildEffectiveSettingsAsync(ownership.UserId, ownership.Id, ct);
            var latest = await _sensorRepo.GetLatestByChipIdAsync(norm, ct)
                ?? throw new KeyNotFoundException("Немає сенсорних даних");

            var advice = BuildAdvice(latest, eff);
            if (advice.Count == 0) return new SaveLatestRecommendationDto { Saved = false, Count = 0 };

            await _settingsRepo.AddAdviceAsync(new ComfortRecommendation
            {
                SensorOwnershipId = ownership.Id,
                Recommendation = string.Join("\n", advice),
                CreatedAt = DateTime.UtcNow
            }, ct);

            await _settingsRepo.SaveChangesAsync(ct);
            return new SaveLatestRecommendationDto { Saved = true, Count = advice.Count };
        }

        public async Task<IEnumerable<RecommendationHistoryDto>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct)
        {
            var list = await _settingsRepo.GetAdviceHistoryAsync(ChipId.Normalize(chipId), take, ct);
            return list.Select(r => new RecommendationHistoryDto
            {
                CreatedAt = r.CreatedAt,
                RoomName = r.SensorOwnership?.RoomName,
                Recommendation = r.Recommendation
            });
        }

        public async Task<AdjustmentAbsoluteResponseDto> SaveAdjustmentsForChipFromAbsoluteAsync(string chipId, IEnumerable<AdjustmentAbsoluteItemDto> items, CancellationToken ct)
        {
            if (items == null || !items.Any())
                throw new ArgumentException("Items are required");

            var norm = ChipId.Normalize(chipId);

            // шукаємо кімнату
            var ownership = await _ownRepo.GetByChipAsync(norm, ct)
                ?? throw new KeyNotFoundException("Chip not found");

            // отримуємо базові налаштування
            var names = items.Select(i => i.ParameterName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            var all = await _settingsRepo.GetAllAsync(ct);
            var baseMap = all.Where(s => names.Contains(s.ParameterName, StringComparer.OrdinalIgnoreCase))
                             .ToDictionary(s => s.ParameterName, s => s, StringComparer.OrdinalIgnoreCase);

            var response = new AdjustmentAbsoluteResponseDto { UserId = ownership.UserId };

            foreach (var item in items)
            {
                if (!baseMap.TryGetValue(item.ParameterName, out var s)) continue;

                float lowDelta = 0f, highDelta = 0f;
                if (s.LowValue.HasValue && item.Low.HasValue)
                    lowDelta = item.Low.Value - s.LowValue.Value;
                if (s.HighValue.HasValue && item.High.HasValue)
                    highDelta = item.High.Value - s.HighValue.Value;

                // остання версія
                var last = (await _settingsRepo.GetLastAdjustmentsAsync(ownership.UserId, ownership.Id, new[] { s.Id }, ct))
                    .OrderByDescending(a => a.Version)
                    .FirstOrDefault();

                var nextVer = (last?.Version ?? 0) + 1;

                await _settingsRepo.UpsertAdjustmentAsync(new SettingsUserAdjustment
                {
                    UserId = ownership.UserId,
                    SettingId = s.Id,
                    SensorOwnershipId = ownership.Id,
                    LowValueAdjustment = lowDelta,
                    HighValueAdjustment = highDelta,
                    Version = nextVer,
                    CreatedAt = last?.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);

                response.Items.Add(new AdjustmentAppliedDto
                {
                    ParameterName = s.ParameterName,
                    BaseLow = s.LowValue,
                    BaseHigh = s.HighValue,
                    LowDelta = lowDelta,
                    HighDelta = highDelta,
                    Version = nextVer,
                    EffectiveLow = s.LowValue + lowDelta,
                    EffectiveHigh = s.HighValue + highDelta
                });
            }

            await _settingsRepo.SaveChangesAsync(ct);
            return response;
        }

        public async Task<IEnumerable<EffectiveSettingDto>> GetEffectiveByChipAsync(string chipId, CancellationToken ct)
        {
            var norm = ChipId.Normalize(chipId);

            // знайти власника плати
            var ownership = await _ownRepo.GetByChipAsync(norm, ct)
                ?? throw new KeyNotFoundException("Chip not found");

            // побудувати ефективні пороги з урахуванням базових Setting + останніх поправок користувача по цій платі
            var effDict = await BuildEffectiveSettingsAsync(ownership.UserId, ownership.Id, ct);

            // змапити у DTO для відповіді
            return effDict.Select(kv => new EffectiveSettingDto
            {
                ParameterName = kv.Key,
                LowValue = kv.Value.Low,
                HighValue = kv.Value.High,
                LowValueMessage = kv.Value.LowMsg,
                HighValueMessage = kv.Value.HighMsg
            });
        }


        // helpers BuildEffectiveSettingsAsync, BuildAdvice лишаємо як були
        // =======================
        // 🔹 Helpers
        // =======================

        private async Task<Dictionary<string, EffSetting>> BuildEffectiveSettingsAsync(
            int? userId, int? ownershipId, CancellationToken ct)
        {
            var names = new[] { "temperature", "humidity", "gas" };

            var baseSettings = (await _settingsRepo.GetAllAsync(ct))
                .Where(s => names.Contains(s.ParameterName, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var eff = baseSettings.ToDictionary(
                s => s.ParameterName,
                s => new EffSetting(s.LowValue, s.HighValue, s.LowValueMessage, s.HighValueMessage),
                StringComparer.OrdinalIgnoreCase);

            if (userId is null || ownershipId is null)
                return eff;

            var ids = baseSettings.Select(s => s.Id).ToList();
            var lastAdj = await _settingsRepo.GetLastAdjustmentsAsync(userId.Value, ownershipId.Value, ids, ct);

            float? Add(float? @base, float delta) => @base.HasValue ? @base.Value + delta : (float?)null;

            foreach (var adj in lastAdj
                .GroupBy(a => a.SettingId)
                .Select(g => g.OrderByDescending(x => x.Version).First()))
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

            // Температура
            if (eff.TryGetValue("temperature", out var T) && t.HasValue)
            {
                if (T.Low.HasValue && t < T.Low && !string.IsNullOrWhiteSpace(T.LowMsg))
                    msgs.Add(T.LowMsg!);
                if (T.High.HasValue && t > T.High && !string.IsNullOrWhiteSpace(T.HighMsg))
                    msgs.Add(T.HighMsg!);
            }

            // Вологість
            if (eff.TryGetValue("humidity", out var H) && h.HasValue)
            {
                if (H.Low.HasValue && h < H.Low && !string.IsNullOrWhiteSpace(H.LowMsg))
                    msgs.Add(H.LowMsg!);
                if (H.High.HasValue && h > H.High && !string.IsNullOrWhiteSpace(H.HighMsg))
                    msgs.Add(H.HighMsg!);
            }

            // Газ
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
    }
}
