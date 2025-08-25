using MySensorApi.DTO;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;
using MySensorApi.Services.Utils;

namespace MySensorApi.Services
{
    public interface ISettingsService
    {
        Task<IEnumerable<Setting>> GetAllAsync(CancellationToken ct);
        Task<Setting?> GetByNameAsync(string name, CancellationToken ct);
        Task UpsertAsync(SettingUpsertDto dto, CancellationToken ct);

        Task<(object payload, string etag)> GetAdjustmentAsync(string parameterName, int userId, CancellationToken ct);
        Task<(bool updated, string newEtag)> PutAdjustmentAsync(string parameterName, int userId, AdjustmentCreateDto dto, string ifMatch, CancellationToken ct);

        Task<IEnumerable<object>> GetEffectiveAsync(int? userId, CancellationToken ct);

        Task<(object dataPayload, List<string> advice)> ComputeLatestAdviceAsync(string chipId, CancellationToken ct);
        Task<(bool saved, int count)> SaveLatestAdviceAsync(string chipId, CancellationToken ct);
        Task<IEnumerable<object>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct);
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

        public async Task<IEnumerable<Setting>> GetAllAsync(CancellationToken ct) =>
            await _settingsRepo.GetAllAsync(ct);

        public Task<Setting?> GetByNameAsync(string name, CancellationToken ct) =>
            _settingsRepo.GetByNameAsync(name, ct);

        public async Task UpsertAsync(SettingUpsertDto dto, CancellationToken ct)
        {
            var name = dto.ParameterName.Trim();
            var s = await _settingsRepo.GetByNameAsync(name, ct);
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
            }
            else
            {
                s.LowValue = dto.LowValue;
                s.HighValue = dto.HighValue;
                s.LowValueMessage = dto.LowValueMessage;
                s.HighValueMessage = dto.HighValueMessage;
            }

            await _settingsRepo.UpsertAsync(s, ct);
            await _settingsRepo.SaveChangesAsync(ct);
        }

        // ---------- Adjustments (ETag) ----------

        public async Task<(object payload, string etag)> GetAdjustmentAsync(string parameterName, int userId, CancellationToken ct)
        {
            var setting = await _settingsRepo.GetByNameAsync(parameterName, ct);
            if (setting is null) throw new KeyNotFoundException("Setting not found");

            var last = (await _settingsRepo.GetLastAdjustmentsAsync(userId, new[] { setting.Id }, ct))
                        .OrderByDescending(a => a.Version)
                        .FirstOrDefault();

            var ver = last?.Version ?? 0;
            var etag = $"\"{userId}-{setting.Id}-{ver}\"";

            var payload = new
            {
                parameterName,
                lowValueAdjustment = last?.LowValueAdjustment ?? 0f,
                highValueAdjustment = last?.HighValueAdjustment ?? 0f,
                version = ver
            };

            return (payload, etag);
        }

        public async Task<(bool updated, string newEtag)> PutAdjustmentAsync(
            string parameterName, int userId, AdjustmentCreateDto dto, string ifMatch, CancellationToken ct)
        {
            var setting = await _settingsRepo.GetByNameAsync(parameterName, ct);
            if (setting is null) throw new KeyNotFoundException("Setting not found");

            var last = (await _settingsRepo.GetLastAdjustmentsAsync(userId, new[] { setting.Id }, ct))
                        .OrderByDescending(a => a.Version)
                        .FirstOrDefault();

            var currentVer = last?.Version ?? 0;
            var currentEtag = $"\"{userId}-{setting.Id}-{currentVer}\"";

            //if (string.IsNullOrEmpty(ifMatch))
            //    throw new InvalidOperationException("428");
            //if (!string.Equals(ifMatch, currentEtag, StringComparison.Ordinal))
            //    throw new InvalidOperationException("412");

            var nextVer = currentVer + 1;

            await _settingsRepo.AddAdjustmentAsync(new SettingsUserAdjustment
            {
                UserId = userId,
                SettingId = setting.Id,
                LowValueAdjustment = dto.LowValueAdjustment,
                HighValueAdjustment = dto.HighValueAdjustment,
                Version = nextVer,
                CreatedAt = DateTime.UtcNow
            }, ct);

            await _settingsRepo.SaveChangesAsync(ct);

            var newEtag = $"\"{userId}-{setting.Id}-{nextVer}\"";
            return (true, newEtag);
        }

        // ---------- Effective ----------

        public async Task<IEnumerable<object>> GetEffectiveAsync(int? userId, CancellationToken ct)
        {
            var names = new[] { "temperature", "humidity", "gas" };

            var baseSettings = (await _settingsRepo.GetAllAsync(ct))
                .Where(s => names.Contains(s.ParameterName, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var eff = baseSettings.ToDictionary(
                s => s.ParameterName,
                s => new EffSetting(s.LowValue, s.HighValue, s.LowValueMessage, s.HighValueMessage),
                StringComparer.OrdinalIgnoreCase);

            if (userId is not null)
            {
                var ids = baseSettings.Select(s => s.Id).ToList();
                var lastAdj = await _settingsRepo.GetLastAdjustmentsAsync(userId.Value, ids, ct);

                float? Add(float? @base, float delta) => @base.HasValue ? @base.Value + delta : (float?)null;

                foreach (var adj in lastAdj.GroupBy(a => a.SettingId).Select(g => g.OrderByDescending(x => x.Version).First()))
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
            }

            return eff.Select(kv => new
            {
                parameterName = kv.Key,
                lowValue = kv.Value.Low,
                highValue = kv.Value.High,
                lowValueMessage = kv.Value.LowMsg,
                highValueMessage = kv.Value.HighMsg
            });
        }

        // ---------- Advice ----------

        public async Task<(object dataPayload, List<string> advice)> ComputeLatestAdviceAsync(string chipId, CancellationToken ct)
        {
            var norm = ChipId.Normalize(chipId);
            var latest = await _sensorRepo.GetLatestByChipIdAsync(norm, ct);
            if (latest is null) throw new KeyNotFoundException("Немає сенсорних даних");

            var ownership = await _ownRepo.GetByChipAsync(norm, ct);

            var effDict = await BuildEffectiveSettingsAsync(ownership?.UserId, ct);
            var advice = BuildAdvice(latest, effDict);

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

            return (dataDto, advice);
        }

        public async Task<(bool saved, int count)> SaveLatestAdviceAsync(string chipId, CancellationToken ct)
        {
            var norm = ChipId.Normalize(chipId);
            var latest = await _sensorRepo.GetLatestByChipIdAsync(norm, ct);
            if (latest is null) throw new KeyNotFoundException("Немає сенсорних даних");

            var ownership = await _ownRepo.GetByChipAsync(norm, ct);
            if (ownership is null) throw new KeyNotFoundException("Власника/кімнату не знайдено");

            var eff = await BuildEffectiveSettingsAsync(ownership.UserId, ct);
            var advice = BuildAdvice(latest, eff);
            if (advice.Count == 0) return (false, 0);

            await _settingsRepo.AddAdviceAsync(new ComfortRecommendation
            {
                SensorOwnershipId = ownership.Id,
                Recommendation = string.Join("\n", advice),
                CreatedAt = DateTime.UtcNow
            }, ct);

            await _settingsRepo.SaveChangesAsync(ct);
            return (true, advice.Count);
        }

        public async Task<IEnumerable<object>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct)
        {
            var list = await _settingsRepo.GetAdviceHistoryAsync(ChipId.Normalize(chipId), Math.Clamp(take, 1, 200), ct);

            return list.Select(r => (object)new
            {
                createdAt = r.CreatedAt,
                roomName = r.SensorOwnership?.RoomName,
                recommendation = r.Recommendation
            });
        }

        // ---- helpers ----

        private async Task<Dictionary<string, EffSetting>> BuildEffectiveSettingsAsync(int? userId, CancellationToken ct)
        {
            var names = new[] { "temperature", "humidity", "gas" };
            var baseSettings = (await _settingsRepo.GetAllAsync(ct))
                .Where(s => names.Contains(s.ParameterName, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var eff = baseSettings.ToDictionary(
                s => s.ParameterName,
                s => new EffSetting(s.LowValue, s.HighValue, s.LowValueMessage, s.HighValueMessage),
                StringComparer.OrdinalIgnoreCase);

            if (userId is null) return eff;

            var ids = baseSettings.Select(s => s.Id).ToList();
            var lastAdj = await _settingsRepo.GetLastAdjustmentsAsync(userId.Value, ids, ct);

            float? Add(float? @base, float delta) => @base.HasValue ? @base.Value + delta : (float?)null;

            foreach (var adj in lastAdj.GroupBy(a => a.SettingId).Select(g => g.OrderByDescending(x => x.Version).First()))
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
                    msgs.Add(G.HighMsg!);
                else if (s.Mq2AnalogPercent.HasValue && G.High.HasValue &&
                         s.Mq2AnalogPercent > G.High && !string.IsNullOrWhiteSpace(G.HighMsg))
                    msgs.Add(G.HighMsg!);
            }
            return msgs;
        }
    }
}
