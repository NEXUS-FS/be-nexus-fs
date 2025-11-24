using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities; // Contains FileShareEntity and PermissionEntity
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AccessControlRepository : IAccessControlRepository
    {
        private readonly NexusFSDbContext _context;

        public AccessControlRepository(NexusFSDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ==================================================================
        // PART 1: FILE ACCESS for sharing
        // ==================================================================

        public async Task<bool> HasAccessAsync(string userId, string path, string operation)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(path))
                return false;

            // 1. Normalize Path
            var normalizedPath = path.Replace("\\", "/");

            // 2. Find the specific share record
            var share = await _context.FileShares
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ResourcePath == normalizedPath);

            // 3. If no share exists, fallback to checking if they are a global admin
            if (share == null)
            { //admin or read?
                if (await HasPermissionAsync(userId, "read")) return true;
                return false;
            }


            var userPerm = share.Permission.ToLowerInvariant();
            var op = operation.ToLowerInvariant();

            return op switch
            {
                // READ: Anyone with a share can read
                "read" or "list" =>
            userPerm == SharePermission.Viewer ||
            userPerm == SharePermission.Editor ||
            userPerm == SharePermission.Owner,


                "write" or "create" or "move" or "copy" =>
              userPerm == SharePermission.Editor ||
              userPerm == SharePermission.Owner,

                // ADMIN: Only Owner
                "delete" or "share" =>
              userPerm == SharePermission.Owner,

                _ => false // Unknown operation denied
            };
        }

        public async Task ShareFileAsync(string path, string userId, string permission)
        {
            var normalizedPath = path.Replace("\\", "/");
            var permissionLower = permission.ToLowerInvariant();

            var share = await _context.FileShares
                .FirstOrDefaultAsync(s => s.ResourcePath == normalizedPath && s.UserId == userId);

            if (share == null)
            {
                share = new FileShareEntity
                {
                    ResourcePath = normalizedPath,
                    UserId = userId,
                    Permission = permissionLower, // Stored as string
                    SharedAt = DateTime.UtcNow,
                    SharedByUserId = "system" //TODO: fix this to be the actual sharing user.
                };
                await _context.FileShares.AddAsync(share);
            }
            else
            {
                share.Permission = permissionLower;
                share.SharedAt = DateTime.UtcNow;
                _context.FileShares.Update(share);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> AddPermissionAsync(string username, string permission)
        {
            try
            {
                var existing = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Username == username && p.Permission == permission);

                if (existing != null) return true;

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

                if (entity == null) return false;

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

                if (!permissions.Any()) return true;

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