using MySensorApi.Models;
using MySensorApi.DTO;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface ISensorDataRepository
    {
        // ---- Базові методи ----
        Task<SensorData?> GetLatestByChipIdAsync(string chipId, CancellationToken ct = default);
        Task<List<SensorData>> GetHistoryAsync(string chipId, DateTime? from, DateTime? to, int take, CancellationToken ct = default);

        Task AddAsync(SensorData data, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        // ---- Серії для графіків ----
        Task<List<SensorPointDto>> GetSeriesRawAsync(
            string chipId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct = default
        );

        Task<List<SensorPointDto>> GetSeriesHourlyAsync(
            string chipId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct = default
        );

        Task<List<SensorPointDto>> GetSeriesDailyAsync(
            string chipId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct = default
        );

        Task<List<SensorPointDto>> GetSeriesAsync(
            string chipId,
            DateTime fromUtc,
            DateTime toUtc,
            TimeBucket bucket,
            CancellationToken ct = default
        );
    }
}
