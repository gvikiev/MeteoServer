using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface IOwnershipRepository
    {
        Task<SensorOwnership?> GetByChipAndUserAsync(string chipId, int userId, CancellationToken ct = default);
        Task<List<SensorOwnership>> GetByUserAsync(int userId, CancellationToken ct = default);
        Task<SensorOwnership?> GetByChipAsync(string chipId, CancellationToken ct = default);
        Task<bool> ChipExistsAsync(string chipId, CancellationToken ct = default);

        Task AddAsync(SensorOwnership entity, CancellationToken ct = default);
        void Remove(SensorOwnership entity);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
