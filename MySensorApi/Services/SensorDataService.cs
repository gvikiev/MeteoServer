using MySensorApi.DTO.Charts;
using MySensorApi.DTO.SensorData;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;
using MySensorApi.Services.Utils;

namespace MySensorApi.Services
{
    public interface ISensorDataService
    {
        Task<int> SaveAsync(SensorData data, CancellationToken ct = default);
        Task<SensorDataDto?> GetLatestAsync(string chipId, int? userId, CancellationToken ct = default);

        // Серії для графіків (з опціональними датами — контролер викликає day/week)
        Task<List<SensorPointDto>> GetSeriesAsync(
            string chipId,
            DateTime? from,
            DateTime? to,
            TimeBucket bucket,
            CancellationToken ct = default);
    }

    public sealed class SensorDataService : ISensorDataService
    {
        private readonly ISensorDataRepository _dataRepo;
        private readonly IOwnershipRepository _ownRepo;

        public SensorDataService(ISensorDataRepository dataRepo, IOwnershipRepository ownRepo)
        {
            _dataRepo = dataRepo;
            _ownRepo = ownRepo;
        }

        public async Task<int> SaveAsync(SensorData data, CancellationToken ct = default)
        {
            data.ChipId = ChipId.Normalize(data.ChipId);
            data.CreatedAt = DateTime.UtcNow;
            await _dataRepo.AddAsync(data, ct);
            return await _dataRepo.SaveChangesAsync(ct);
        }

        public async Task<SensorDataDto?> GetLatestAsync(string chipId, int? userId, CancellationToken ct = default)
        {
            var norm = ChipId.Normalize(chipId);
            var s = await _dataRepo.GetLatestByChipIdAsync(norm, ct);
            if (s is null) return null;

            var roomName = userId.HasValue
                ? (await _ownRepo.GetByChipAndUserAsync(norm, userId.Value, ct))?.RoomName
                : (await _ownRepo.GetByChipAsync(norm, ct))?.RoomName;

            return Map(s, roomName ?? string.Empty);
        }

        public Task<List<SensorPointDto>> GetSeriesAsync(
            string chipId,
            DateTime? from,
            DateTime? to,
            TimeBucket bucket,
            CancellationToken ct = default)
        {
            var norm = ChipId.Normalize(chipId);
            var toUtc = (to?.ToUniversalTime()) ?? DateTime.UtcNow;
            var fromUtc = (from?.ToUniversalTime()) ?? (bucket == TimeBucket.Day ? toUtc.AddDays(-7) : toUtc.AddDays(-1));
            if (fromUtc > toUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

            return _dataRepo.GetSeriesAsync(norm, fromUtc, toUtc, bucket, ct);
        }

        private static SensorDataDto Map(SensorData s, string room) => new SensorDataDto
        {
            ChipId = s.ChipId,
            RoomName = room,
            TemperatureDht = s.TemperatureDht,
            HumidityDht = s.HumidityDht,
            GasDetected = s.GasDetected,
            Light = s.Light,
            Pressure = s.Pressure,
            Altitude = s.Altitude,
            TemperatureBme = s.TemperatureBme,
            HumidityBme = s.HumidityBme,
            Mq2Analog = s.Mq2Analog,
            Mq2AnalogPercent = s.Mq2AnalogPercent,
            LightAnalog = s.LightAnalog,
            LightAnalogPercent = s.LightAnalogPercent
        };
    }
}
