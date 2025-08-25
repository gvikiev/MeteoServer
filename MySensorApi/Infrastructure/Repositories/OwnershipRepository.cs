using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories
{
    public sealed class OwnershipRepository : IOwnershipRepository
    {
        private readonly AppDbContext _db;
        public OwnershipRepository(AppDbContext db) => _db = db;

        public Task<SensorOwnership?> GetByChipAndUserAsync(string chipId, int userId, CancellationToken ct = default) =>
            _db.SensorOwnerships
               .Include(o => o.User)
               .FirstOrDefaultAsync(o => o.ChipId == chipId && o.UserId == userId, ct);

        public Task<List<SensorOwnership>> GetByUserAsync(int userId, CancellationToken ct = default) =>
            _db.SensorOwnerships
               .Where(o => o.UserId == userId)
               .ToListAsync(ct);

        public Task<SensorOwnership?> GetByChipAsync(string chipId, CancellationToken ct = default) =>
            _db.SensorOwnerships
               .Include(o => o.User)
               .FirstOrDefaultAsync(o => o.ChipId == chipId, ct);

        public Task<bool> ChipExistsAsync(string chipId, CancellationToken ct = default) =>
            _db.SensorOwnerships.AnyAsync(o => o.ChipId == chipId, ct);

        public Task AddAsync(SensorOwnership entity, CancellationToken ct = default) =>
            _db.SensorOwnerships.AddAsync(entity, ct).AsTask();

        public void Remove(SensorOwnership entity) => _db.SensorOwnerships.Remove(entity);

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
