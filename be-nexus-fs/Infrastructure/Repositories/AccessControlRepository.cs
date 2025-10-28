using Microsoft.EntityFrameworkCore;
using Domain.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for access control persistence.
    /// </summary>
    public class AccessControlRepository : IAccessControlRepository
    {
        private readonly NexusFSDbContext _context;

        public AccessControlRepository(NexusFSDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> AddPermissionAsync(string username, string permission)
        {
            try
            {
                var existing = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Username == username && p.Permission == permission);

                if (existing != null)
                    return true; // Already exists

                _context.Permissions.Add(new PermissionEntity
                {
                    Username = username,
                    Permission = permission,
                    GrantedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemovePermissionAsync(string username, string permission)
        {
            try
            {
                var entity = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Username == username && p.Permission == permission);

                if (entity == null)
                    return false;

                _context.Permissions.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveAllPermissionsAsync(string username)
        {
            try
            {
                var permissions = await _context.Permissions
                    .Where(p => p.Username == username)
                    .ToListAsync();

                _context.Permissions.RemoveRange(permissions);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(string username, string permission)
        {
            return await _context.Permissions
                .AnyAsync(p => p.Username == username && p.Permission == permission);
        }

        public async Task<IEnumerable<string>> GetUserPermissionsAsync(string username)
        {
            return await _context.Permissions
                .Where(p => p.Username == username)
                .Select(p => p.Permission)
                .ToListAsync();
        }

        public async Task<Dictionary<string, List<string>>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .GroupBy(p => p.Username)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(p => p.Permission).ToList()
                );
        }
    }
}
