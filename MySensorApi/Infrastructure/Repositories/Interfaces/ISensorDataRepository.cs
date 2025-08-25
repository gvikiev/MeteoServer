using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface ISensorDataRepository
    {
        Task<SensorData?> GetLatestByChipIdAsync(string chipId, CancellationToken ct = default);
        Task<List<SensorData>> GetHistoryAsync(string chipId, DateTime? from, DateTime? to, int take, CancellationToken ct = default);

        Task AddAsync(SensorData data, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
