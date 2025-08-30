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

        public Task<List<ComfortRecommendation>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct = default) =>
            _db.ComfortRecommendations
               .AsNoTracking()
               .Where(r => r.SensorOwnership.ChipId == chipId)
               .OrderByDescending(r => r.CreatedAt)
               .Take(Math.Clamp(take, 1, 200))
               .ToListAsync(ct);

        public Task AddAdviceAsync(ComfortRecommendation rec, CancellationToken ct = default) =>
            _db.ComfortRecommendations.AddAsync(rec, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);

        public async Task UpsertAdjustmentAsync(SettingsUserAdjustment adj, CancellationToken ct = default)
        {
            var existing = await _db.SettingsUserAdjustments
                .FirstOrDefaultAsync(a =>
                    a.UserId == adj.UserId &&
                    a.SensorOwnershipId == adj.SensorOwnershipId &&
                    a.SettingId == adj.SettingId, ct);

            if (existing != null)
            {
                existing.LowValueAdjustment = adj.LowValueAdjustment;
                existing.HighValueAdjustment = adj.HighValueAdjustment;
                existing.Version += 1;
                existing.UpdatedAt = DateTime.UtcNow;
                _db.SettingsUserAdjustments.Update(existing);
            }
            else
            {
                adj.Version = 1;
                adj.CreatedAt = DateTime.UtcNow;
                adj.UpdatedAt = DateTime.UtcNow;
                await _db.SettingsUserAdjustments.AddAsync(adj, ct);
            }
        }

        public Task<List<SettingsUserAdjustment>> GetLastAdjustmentsAsync(
            int userId, int ownershipId, IEnumerable<int> settingIds, CancellationToken ct = default) =>
            _db.SettingsUserAdjustments
               .AsNoTracking()
               .Where(a => a.UserId == userId &&
                           a.SensorOwnershipId == ownershipId &&
                           settingIds.Contains(a.SettingId))
               .ToListAsync(ct);

    }
}
