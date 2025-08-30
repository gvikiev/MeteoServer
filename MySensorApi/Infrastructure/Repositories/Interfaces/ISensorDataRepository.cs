using MySensorApi.Models;
using MySensorApi.DTO.Charts;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface ISensorDataRepository
    {
        // Базові
        Task<SensorData?> GetLatestByChipIdAsync(string chipId, CancellationToken ct = default);
        Task AddAsync(SensorData data, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        // Серії для графіків (універсально)
        Task<List<SensorPointDto>> GetSeriesAsync(
            string chipId,
            DateTime fromUtc,
            DateTime toUtc,
            TimeBucket bucket,
            CancellationToken ct = default
        );
    }
}
