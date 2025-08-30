using MySensorApi.Models;
using MySensorApi.DTO.Charts;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface ISensorDataRepository
    {
        // Базові
        Task<SensorData?> GetLatestByChipIdAsync(string chipId, CancellationToken ct = default);
        Task<SensorData?> FindByIdAsync(int id, CancellationToken ct = default);

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

        // --- 🔹 Робота з рекомендаціями ---
        // Отримати рекомендацію по конкретному виміру (1-до-1)
        Task<ComfortRecommendation?> GetRecommendationForDataAsync(int sensorDataId, CancellationToken ct = default);

        // Додати рекомендацію (але тільки якщо ще нема)
        Task AddRecommendationAsync(ComfortRecommendation rec, CancellationToken ct = default);
    }
}
