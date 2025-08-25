using MySensorApi.DTO;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;
using MySensorApi.Services.Utils;

namespace MySensorApi.Services
{
    public interface ISensorDataService
    {
        Task<int> SaveAsync(SensorData data, CancellationToken ct = default);
        Task<SensorDataDto?> GetLatestAsync(string chipId, int? userId, CancellationToken ct = default);
        Task<List<SensorDataDto>> GetHistoryAsync(string chipId, int? userId, DateTime? from, DateTime? to, int take, CancellationToken ct = default);
    }

    public sealed class SensorDataService : ISensorDataService
    {
        private readonly ISensorDataRepository _dataRepo;
        private readonly IOwnershipRepository _ownRepo;

        public SensorDataService(ISensorDataRepository dataRepo, IOwnershipRepository ownRepo)
        {
            _dataRepo = dataRepo; _ownRepo = ownRepo;
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

        public async Task<List<SensorDataDto>> GetHistoryAsync(
            string chipId, int? userId, DateTime? from, DateTime? to, int take, CancellationToken ct = default)
        {
            var norm = ChipId.Normalize(chipId);
            var list = await _dataRepo.GetHistoryAsync(norm, from, to, take, ct);

            var roomName = userId.HasValue
                ? (await _ownRepo.GetByChipAndUserAsync(norm, userId.Value, ct))?.RoomName ?? string.Empty
                : (await _ownRepo.GetByChipAsync(norm, ct))?.RoomName ?? string.Empty;

            return list.Select(s => Map(s, roomName)).ToList();
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
