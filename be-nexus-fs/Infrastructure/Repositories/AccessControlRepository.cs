using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
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
        // PART 1: FILE ACCESS (The "Google Drive" Logic)
        // This validates access to specific files based on SharePermission
        // ==================================================================

        public async Task<bool> HasAccessAsync(string userId, string path, string operation)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(path)) return false;

            // 1. Normalize Path (e.g. "C:\Data" -> "C:/Data")
            var normalizedPath = path.Replace("\\", "/");

            // 2. Find the share record for this specific file & user
            var share = await _context.FileShares
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ResourcePath == normalizedPath);

            // 3. If no specific share exists, fallback to checking global admin permissions
            if (share == null)
            {
                // Optional: Check if they are a global admin (using the generic permission system)
                // If you don't want admins to see everything by default, remove this line.
                if (await HasPermissionAsync(userId, "admin")) return true;
                
                return false; 
            }

            // 4. Map operation string to required Permission Enum
            return operation.ToLowerInvariant() switch
            {
                // Viewers can Read/List
                "read" or "list" => true, 

                // Editors can Write/Create/Move/Copy
                "write" or "create" or "move" or "copy" => 
                    share.Permission >= SharePermission.Editor,

                // Owners can Delete/Share
                "delete" or "share" => 
                    share.Permission == SharePermission.Owner,

                _ => false // Unknown ops denied
            };
        }

        // ==================================================================
        // PART 2: GENERIC PERMISSIONS (The "RBAC" Logic)
        // This manages roles like "admin", "auditor", "backup_operator"
        // ==================================================================

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

        public async Task ShareFileAsync(string path, string userId, SharePermission permission)
{
    // 1. Normalize path to match how we query it later
    var normalizedPath = path.Replace("\\", "/");

    // 2. Check if a share record already exists
    var share = await _context.FileShares
        .FirstOrDefaultAsync(s => s.ResourcePath == normalizedPath && s.UserId == userId);

    if (share == null)
    {
        // Create new share
        share = new FileShareEntity
        {
            ResourcePath = normalizedPath,
            UserId = userId,
            Permission = permission,
            SharedAt = DateTime.UtcNow,
            SharedByUserId = "system" // Or pass in the sharer's ID
        };
        await _context.FileShares.AddAsync(share);
    }
    else
    {
        // Update existing permission (e.g. upgrade Viewer -> Editor)
        share.Permission = permission;
        share.SharedAt = DateTime.UtcNow; // Optional: Update timestamp
        _context.FileShares.Update(share);
    }

    await _context.SaveChangesAsync();
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