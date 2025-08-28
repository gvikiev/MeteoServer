using Microsoft.EntityFrameworkCore;
using MySensorApi.Data;
using MySensorApi.Infrastructure.Repositories.Interfaces;
using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories
{
    public sealed class UsersRepository : IUsersRepository
    {
        private readonly AppDbContext _db;
        public UsersRepository(AppDbContext db) => _db = db;

        public Task<bool> ExistsAsync(int userId, CancellationToken ct = default) =>
            _db.Users.AnyAsync(u => u.Id == userId, ct);

        public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
            _db.Users.AnyAsync(u => u.Username == username, ct);

        public Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default) =>
            _db.Roles.FirstOrDefaultAsync(r => r.RoleName == name, ct);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
            _db.Users.Include(u => u.Role)
                     .FirstOrDefaultAsync(u => u.Username == username, ct);

        public Task<User?> FindByIdAsync(int id, CancellationToken ct = default) =>
            _db.Users.Include(u => u.Role)
                     .FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<User?> FindByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
            _db.Users.Include(u => u.Role)
                     .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);

        public Task AddAsync(User user, CancellationToken ct = default) =>
            _db.Users.AddAsync(user, ct).AsTask();

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);


    }
}
