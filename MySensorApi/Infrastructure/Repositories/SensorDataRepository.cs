using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;
using MySensorApi.DTO.Charts;

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

        public Task AddAsync(SensorData data, CancellationToken ct = default) =>
            _db.SensorData.AddAsync(data, ct).AsTask();

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);

        // ------- Серії: Hour / Day -------
        public async Task<List<SensorPointDto>> GetSeriesAsync(
            string chipId, DateTime fromUtc, DateTime toUtc, TimeBucket bucket, CancellationToken ct = default)
        {
            fromUtc = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
            toUtc = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);
            if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

            if (bucket == TimeBucket.Day)
            {
                var rows = await _db.SensorData.AsNoTracking()
                    .Where(x => x.ChipId == chipId && x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
                    .GroupBy(x => EF.Functions.DateDiffDay(DateTime.UnixEpoch, x.CreatedAt))
                    .Select(g => new
                    {
                        Bucket = g.Key,
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
            else // Hour (дефолт)
            {
                var rows = await _db.SensorData.AsNoTracking()
                    .Where(x => x.ChipId == chipId && x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
                    .GroupBy(x => EF.Functions.DateDiffHour(DateTime.UnixEpoch, x.CreatedAt))
                    .Select(g => new
                    {
                        Bucket = g.Key,
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
        }
    }
}
