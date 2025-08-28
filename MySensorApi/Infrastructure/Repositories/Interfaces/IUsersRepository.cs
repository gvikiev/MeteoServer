using MySensorApi.Models;

namespace MySensorApi.Infrastructure.Repositories.Interfaces
{
    public interface IUsersRepository
    {
        Task<bool> ExistsAsync(int userId, CancellationToken ct = default);               // опційно, для перевірки існування
        Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);  // унікальність логіна

        Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);

        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);  // для логіну
        Task<User?> FindByIdAsync(int id, CancellationToken ct = default);               // для GET /users/{id}
        Task<User?> FindByRefreshTokenAsync(string refreshToken, CancellationToken ct = default); // для refresh

        Task AddAsync(User user, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);

    }
}
