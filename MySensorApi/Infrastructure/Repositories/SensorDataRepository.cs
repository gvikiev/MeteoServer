using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;
using MySensorApi.DTO;

namespace MySensorApi.Infrastructure.Repositories
{
    public sealed class SensorDataRepository : ISensorDataRepository
    {
        private readonly AppDbContext _db;
        public SensorDataRepository(AppDbContext db) => _db = db;

        public Task<SensorData?> GetLatestByChipIdAsync(string chipId, CancellationToken ct = default) =>
            _db.SensorData
               .AsNoTracking()
               .Where(x => x.ChipId == chipId)
               .OrderByDescending(x => x.CreatedAt)
               .FirstOrDefaultAsync(ct);

        public async Task<List<SensorData>> GetHistoryAsync(
            string chipId, DateTime? from, DateTime? to, int take, CancellationToken ct = default)
        {
            var q = _db.SensorData.AsNoTracking().Where(x => x.ChipId == chipId);
            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);

            return await q.OrderByDescending(x => x.CreatedAt)
                          .Take(Math.Clamp(take, 1, 500))
                          .ToListAsync(ct);
        }

        // =======================
        //  СЕРІЇ ДЛЯ ГРАФІКІВ
        // =======================

        // RAW: повертаємо кожен запис, округлюючи до int (округлення робимо вже після ToListAsync)
        public async Task<List<SensorPointDto>> GetSeriesRawAsync(
            string chipId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            var rows = await _db.SensorData.AsNoTracking()
                .Where(x => x.ChipId == chipId && x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    Ts = x.CreatedAt,
                    Temp = (double?)(x.TemperatureBme ?? x.TemperatureDht),
                    Hum = (double?)(x.HumidityBme ?? x.HumidityDht)
                })
                .ToListAsync(ct);

            return rows.Select(r => new SensorPointDto(
                r.Ts,
                (int)Math.Round(r.Temp ?? 0.0),
                (int)Math.Round(r.Hum ?? 0.0)
            )).ToList();
        }

        // HOURLY: групування по годинах через DateDiffHour від UnixEpoch
        public async Task<List<SensorPointDto>> GetSeriesHourlyAsync(
            string chipId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            var rows = await _db.SensorData.AsNoTracking()
                .Where(x => x.ChipId == chipId && x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
                .GroupBy(x => EF.Functions.DateDiffHour(DateTime.UnixEpoch, x.CreatedAt))
                .Select(g => new
                {
                    Bucket = g.Key, // int
                    AvgTemp = g.Average(s => (double?)(s.TemperatureBme ?? s.TemperatureDht)),
                    AvgHum = g.Average(s => (double?)(s.HumidityBme ?? s.HumidityDht))
                })
                .OrderBy(x => x.Bucket)
                .ToListAsync(ct);

            return rows.Select(x => new SensorPointDto(
                DateTime.SpecifyKind(DateTime.UnixEpoch.AddHours(x.Bucket), DateTimeKind.Utc),
                (int)Math.Round(x.AvgTemp ?? 0.0),
                (int)Math.Round(x.AvgHum ?? 0.0)
            )).ToList();
        }

        // DAILY: групування по днях через DateDiffDay від UnixEpoch
        public async Task<List<SensorPointDto>> GetSeriesDailyAsync(
            string chipId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            var rows = await _db.SensorData.AsNoTracking()
                .Where(x => x.ChipId == chipId && x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
                .GroupBy(x => EF.Functions.DateDiffDay(DateTime.UnixEpoch, x.CreatedAt))
                .Select(g => new
                {
                    Bucket = g.Key, // int
                    AvgTemp = g.Average(s => (double?)(s.TemperatureBme ?? s.TemperatureDht)),
                    AvgHum = g.Average(s => (double?)(s.HumidityBme ?? s.HumidityDht))
                })
                .OrderBy(x => x.Bucket)
                .ToListAsync(ct);

            return rows.Select(x => new SensorPointDto(
                DateTime.SpecifyKind(DateTime.UnixEpoch.AddDays(x.Bucket), DateTimeKind.Utc),
                (int)Math.Round(x.AvgTemp ?? 0.0),
                (int)Math.Round(x.AvgHum ?? 0.0)
            )).ToList();
        }

        // Універсальний роутер
        public async Task<List<SensorPointDto>> GetSeriesAsync(
            string chipId, DateTime fromUtc, DateTime toUtc, TimeBucket bucket, CancellationToken ct = default)
        {
            var norm = chipId; // нормалізація робиться у сервісі
            return bucket switch
            {
                TimeBucket.Hour => await GetSeriesHourlyAsync(norm, fromUtc, toUtc, ct),
                TimeBucket.Day => await GetSeriesDailyAsync(norm, fromUtc, toUtc, ct),
                _ => await GetSeriesRawAsync(norm, fromUtc, toUtc, ct)
            };
        }

        // =======================
        //  CRUD
        // =======================
        public Task AddAsync(SensorData data, CancellationToken ct = default) =>
            _db.SensorData.AddAsync(data, ct).AsTask();

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);
    }
}
