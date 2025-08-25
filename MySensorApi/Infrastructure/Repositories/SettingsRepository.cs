using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories
{
    public sealed class SettingsRepository : ISettingsRepository
    {
        private readonly AppDbContext _db;
        public SettingsRepository(AppDbContext db) => _db = db;

        public Task<List<Setting>> GetAllAsync(CancellationToken ct = default) =>
            _db.Settings.AsNoTracking().ToListAsync(ct);

        public Task<Setting?> GetByNameAsync(string name, CancellationToken ct = default) =>
            _db.Settings.FirstOrDefaultAsync(s => s.ParameterName == name, ct);

        public async Task UpsertAsync(Setting s, CancellationToken ct = default)
        {
            if (s.Id == 0)
                await _db.Settings.AddAsync(s, ct);
            else
                _db.Settings.Update(s);
        }

        public Task<List<SettingsUserAdjustment>> GetLastAdjustmentsAsync(int userId, IEnumerable<int> settingIds, CancellationToken ct = default) =>
            _db.SettingsUserAdjustments
               .AsNoTracking()
               .Where(a => a.UserId == userId && settingIds.Contains(a.SettingId))
               .ToListAsync(ct);

        public Task AddAdjustmentAsync(SettingsUserAdjustment adj, CancellationToken ct = default) =>
            _db.SettingsUserAdjustments.AddAsync(adj, ct).AsTask();

        public Task<List<ComfortRecommendation>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct = default) =>
            _db.ComfortRecommendations
               .AsNoTracking()
               .Where(r => r.SensorOwnership.ChipId == chipId)
               .OrderByDescending(r => r.CreatedAt)
               .Take(Math.Clamp(take, 1, 200))
               .ToListAsync(ct);

        public Task AddAdviceAsync(ComfortRecommendation rec, CancellationToken ct = default) =>
            _db.ComfortRecommendations.AddAsync(rec, ct).AsTask();

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
