using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface ISettingsRepository
    {
        // base settings
        Task<List<Setting>> GetAllAsync(CancellationToken ct = default);
        Task<Setting?> GetByNameAsync(string name, CancellationToken ct = default);
        Task UpsertAsync(Setting s, CancellationToken ct = default);

        // user adjustments history (append-only)
        Task<List<SettingsUserAdjustment>> GetLastAdjustmentsAsync(
            int userId, IEnumerable<int> settingIds, CancellationToken ct = default);

        // 🔹 НОВЕ: по конкретній кімнаті/платі
        Task<List<SettingsUserAdjustment>> GetLastAdjustmentsAsync(
            int userId, int ownershipId, IEnumerable<int> settingIds, CancellationToken ct = default);

        Task AddAdjustmentAsync(SettingsUserAdjustment adj, CancellationToken ct = default);

        // advice
        Task<List<ComfortRecommendation>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct = default);
        Task AddAdviceAsync(ComfortRecommendation rec, CancellationToken ct = default);

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task UpsertAdjustmentAsync(SettingsUserAdjustment adj, CancellationToken ct = default);
    }
}
