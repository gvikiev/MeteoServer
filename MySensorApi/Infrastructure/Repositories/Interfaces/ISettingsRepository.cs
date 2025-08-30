using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface ISettingsRepository
    {
        // comfort advice
        Task<List<ComfortRecommendation>> GetAdviceHistoryAsync(string chipId, int take, CancellationToken ct = default);
        Task AddAdviceAsync(ComfortRecommendation rec, CancellationToken ct = default);
        Task<ComfortRecommendation?> FindAdviceBySensorDataIdAsync(int sensorDataId, CancellationToken ct = default);
        Task<ComfortRecommendation?> GetLastAdviceForOwnershipAsync(int ownershipId, CancellationToken ct = default);

        // absolute adjustments
        Task<List<Setting>> GetAllAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task UpsertAdjustmentAsync(SettingsUserAdjustment adj, CancellationToken ct = default);

        Task<List<SettingsUserAdjustment>> GetLastAdjustmentsAsync(
            int userId,
            int ownershipId,
            IEnumerable<int> settingIds,
            CancellationToken ct = default
        );
    }
}
