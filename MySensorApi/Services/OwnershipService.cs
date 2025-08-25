using MySensorApi.DTO;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;
using MySensorApi.Services.Utils;

namespace MySensorApi.Services
{
    public interface IOwnershipService
    {
        Task<RoomWithSensorDto?> GetRoomForUserAsync(string chipId, int userId, CancellationToken ct = default);
        Task<List<RoomWithSensorDto>> GetRoomsByUserAsync(int userId, CancellationToken ct = default);
        Task<RoomWithSensorDto> CreateAsync(SensorOwnershipCreateDto dto, CancellationToken ct = default);
        Task<(OwnershipSyncDto? dto, string? etag, DateTime? lastModified)> GetSyncForEspAsync(
            string chipId, CancellationToken ct = default);
        Task<(bool updated, string newEtag)> UpdateAsync(SensorOwnershipUpdateDto dto, string? ifMatch, CancellationToken ct = default);
        Task<bool> DeleteAsync(string chipId, int userId, CancellationToken ct = default);
    }

    public sealed class OwnershipService : IOwnershipService
    {
        private readonly IOwnershipRepository _repo;
        private readonly ISensorDataRepository _data;

        public OwnershipService(IOwnershipRepository repo, ISensorDataRepository data)
        {
            _repo = repo; _data = data;
        }

        public async Task<RoomWithSensorDto?> GetRoomForUserAsync(string chipId, int userId, CancellationToken ct = default)
        {
            var norm = ChipId.Normalize(chipId);
            var o = await _repo.GetByChipAndUserAsync(norm, userId, ct);
            if (o is null) return null;
            var latest = await _data.GetLatestByChipIdAsync(norm, ct);
            return Map(o, latest);
        }

        public async Task<List<RoomWithSensorDto>> GetRoomsByUserAsync(int userId, CancellationToken ct = default)
        {
            var own = await _repo.GetByUserAsync(userId, ct);
            var result = new List<RoomWithSensorDto>(own.Count);
            foreach (var o in own)
            {
                var latest = await _data.GetLatestByChipIdAsync(o.ChipId, ct);
                result.Add(Map(o, latest));
            }
            return result;
        }

        public async Task<RoomWithSensorDto> CreateAsync(SensorOwnershipCreateDto dto, CancellationToken ct = default)
        {
            if (dto.UserId <= 0 || string.IsNullOrWhiteSpace(dto.ChipId) ||
                string.IsNullOrWhiteSpace(dto.RoomName) || string.IsNullOrWhiteSpace(dto.ImageName))
                throw new InvalidOperationException("Потрібні поля: UserId, ChipId, RoomName, ImageName");

            var norm = ChipId.Normalize(dto.ChipId);

            if (await _repo.ChipExistsAsync(norm, ct))
                throw new InvalidOperationException("Цей пристрій уже зареєстрований");

            var o = new SensorOwnership
            {
                UserId = dto.UserId,
                ChipId = norm,
                RoomName = dto.RoomName.Trim(),
                ImageName = dto.ImageName.Trim(),
                Version = 1,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(o, ct);
            await _repo.SaveChangesAsync(ct);

            var latest = await _data.GetLatestByChipIdAsync(norm, ct);
            return Map(o, latest);
        }

        public async Task<(OwnershipSyncDto? dto, string? etag, DateTime? lastModified)>
            GetSyncForEspAsync(string chipId, CancellationToken ct = default)
        {
            var o = await _repo.GetByChipAsync(ChipId.Normalize(chipId), ct);
            if (o is null) return (null, null, null);

            var etag = $"\"{o.ChipId}-{o.Version}\"";
            var lastModified = o.UpdatedAt.ToUniversalTime();

            var dto = new OwnershipSyncDto
            {
                Username = o.User?.Username ?? "",
                RoomName = o.RoomName ?? "",
                ImageName = o.ImageName ?? ""
            };
            return (dto, etag, lastModified);
        }


        public async Task<(bool updated, string newEtag)> UpdateAsync(
            SensorOwnershipUpdateDto dto, string? ifMatch, CancellationToken ct = default)
        {
            var o = await _repo.GetByChipAsync(ChipId.Normalize(dto.ChipId), ct);
            if (o is null) throw new KeyNotFoundException("Пристрій не знайдено");

            var currentEtag = $"\"{o.ChipId}-{o.Version}\"";

            // If-Match обов'язковий
            //if (string.IsNullOrWhiteSpace(ifMatch))
            //    throw new InvalidOperationException("428");
            //if (!string.Equals(ifMatch, currentEtag, StringComparison.Ordinal))
            //    throw new InvalidOperationException("412");

            var changed = false;

            if (dto.RoomName is not null)
            {
                var v = dto.RoomName.Trim();
                if (v.Length == 0) throw new InvalidOperationException("RoomName не може бути порожнім");
                if (!string.Equals(o.RoomName, v, StringComparison.Ordinal)) { o.RoomName = v; changed = true; }
            }

            if (dto.ImageName is not null)
            {
                var v = dto.ImageName.Trim();
                if (v.Length == 0) throw new InvalidOperationException("ImageName не може бути порожнім");
                if (!string.Equals(o.ImageName, v, StringComparison.Ordinal)) { o.ImageName = v; changed = true; }
            }

            if (!changed) return (false, currentEtag);

            o.Version++;
            o.UpdatedAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync(ct);

            return (true, $"\"{o.ChipId}-{o.Version}\"");
        }

        public async Task<bool> DeleteAsync(string chipId, int userId, CancellationToken ct = default)
        {
            var o = await _repo.GetByChipAndUserAsync(ChipId.Normalize(chipId), userId, ct);
            if (o is null) return false;
            _repo.Remove(o);
            await _repo.SaveChangesAsync(ct);
            return true;
        }

        private static RoomWithSensorDto Map(SensorOwnership o, SensorData? s) => new RoomWithSensorDto
        {
            Id = o.Id,
            ChipId = o.ChipId,
            RoomName = o.RoomName,
            ImageName = o.ImageName,
            Temperature = s?.TemperatureDht,
            Humidity = s?.HumidityDht
        };
    }
}
