using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;

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

        public async Task<List<SensorData>> GetHistoryAsync(string chipId, DateTime? from, DateTime? to, int take, CancellationToken ct = default)
        {
            var q = _db.SensorData.AsNoTracking().Where(x => x.ChipId == chipId);
            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);

            return await q.OrderByDescending(x => x.CreatedAt)
                          .Take(Math.Clamp(take, 1, 500))
                          .ToListAsync(ct);
        }

        public Task AddAsync(SensorData data, CancellationToken ct = default) =>
            _db.SensorData.AddAsync(data, ct).AsTask();

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
