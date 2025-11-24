using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly NexusFSDbContext _context;
		private readonly IPasswordHasher<UserEntity> _passwordHasher;

        public UserRepository(NexusFSDbContext context, IPasswordHasher<UserEntity> passwordHasher)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
			_passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher)); 
        }
        
        #region Basic CRUD operations
        public async Task<UserEntity?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("User ID cannot be null or empty", nameof(id));

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        }
        public async Task<IEnumerable<UserEntity>> GetAllAsync(int pageNumber = 1, int pageSize = 50)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
            
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.DeletedAt == null)
                .OrderBy(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<UserEntity> AddAsync(UserEntity user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(user.Username))
                throw new ArgumentException("Username is required", nameof(user.Username));

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new ArgumentException("Email is required", nameof(user.Email));

            if (string.IsNullOrWhiteSpace(user.Provider))
                throw new ArgumentException("Provider is required", nameof(user.Provider));

        
            var existingUsername = await _context.Users
                .AnyAsync(u => u.Username == user.Username && u.DeletedAt == null);
            
            if (existingUsername)
                throw new InvalidOperationException($"Username '{user.Username}' already exists");

      
            var existingEmail = await _context.Users
                .AnyAsync(u => u.Email == user.Email && u.DeletedAt == null);
            
            if (existingEmail)
                throw new InvalidOperationException($"Email '{user.Email}' already exists");

       
            if (user.Provider == "Basic" && !string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash); 
            }
           
            user.Id = Guid.NewGuid().ToString();
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }


        public async Task UpdateAsync(UserEntity user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Id)) throw new ArgumentNullException("User ID is required for update", nameof(user.Id));
            
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == user.Id && u.DeletedAt == null);
            
            if (existingUser == null) throw new KeyNotFoundException($"User with ID '{user.Id}' not found");
            
            if (user.Username != existingUser.Username)
            {
                var duplicateUsername = await _context.Users
                    .AnyAsync(u => u.Username == user.Username && u.Id != user.Id && u.DeletedAt == null);
                
                if (duplicateUsername)
                    throw new InvalidOperationException($"Username '{user.Username}' already exists");
            }

            if (user.Email != existingUser.Email)
            {
                var duplicateEmail = await _context.Users
                    .AnyAsync(u => u.Email == user.Email && u.Id != user.Id && u.DeletedAt == null);
                
                if (duplicateEmail)
                    throw new InvalidOperationException($"Email '{user.Email}' already exists");
            }
            
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;
            existingUser.LastLogin = user.LastLogin;
            existingUser.UpdatedAt = DateTime.UtcNow;
            
            if (!string.IsNullOrWhiteSpace(user.PasswordHash) && 
                user.PasswordHash != existingUser.PasswordHash)
            {
                existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, user.PasswordHash);
            }

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("User ID cannot be null or empty", nameof(id));

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            
            if (user == null)
                throw new KeyNotFoundException($"User with ID '{id}' not found");

            // Soft delete
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task RestoreAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("User ID cannot be null or empty", nameof(id));

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt != null);
            
            if (user == null)
                throw new KeyNotFoundException($"Deleted user with ID '{id}' not found");

            user.DeletedAt = null;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        #endregion
        
        #region Authentication Queries

        public async Task<UserEntity?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && u.DeletedAt == null);
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && u.DeletedAt == null);
        }
        public async Task<UserEntity?> GetByProviderIdAsync(string provider, string providerId)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider cannot be null or empty", nameof(provider));

            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => 
                    u.Provider == provider && 
                    u.ProviderId == providerId &&
                    u.IsActive &&
                    u.DeletedAt == null);
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        #endregion
        
        #region Refresh Token Management

        public async Task UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiryTime)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            
            if (user == null)
                throw new KeyNotFoundException($"User with ID '{userId}' not found");

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = expiryTime;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
        }
        public async Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => 
                    u.RefreshToken == refreshToken &&
                    u.RefreshTokenExpiryTime > DateTime.UtcNow &&
                    u.IsActive &&
                    u.DeletedAt == null);
        }
         public async Task<UserEntity?> ValidateCredentialsAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            var user = await _context.Users
                .FirstOrDefaultAsync(u => 
                    u.Username == username && 
                    u.Provider == "Basic" &&
                    u.IsActive && 
                    u.DeletedAt == null);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            return result == PasswordVerificationResult.Success ? user : null;
        }

        #endregion
        
        #region Query Helpers

        public async Task<int> GetTotalCountAsync(bool includeInactive = false)
        {
            if (includeInactive)
                return await _context.Users.CountAsync(u => u.DeletedAt == null);
            
            return await _context.Users.CountAsync(u => u.IsActive && u.DeletedAt == null);
        }
        public async Task<bool> UsernameExistsAsync(string username, string? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var query = _context.Users.Where(u => u.Username == username && u.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(excludeUserId))
                query = query.Where(u => u.Id != excludeUserId);

            return await query.AnyAsync();
        }
        public async Task<bool> EmailExistsAsync(string email, string? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var query = _context.Users.Where(u => u.Email == email && u.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(excludeUserId))
                query = query.Where(u => u.Id != excludeUserId);

            return await query.AnyAsync();
        }
        public async Task<IEnumerable<UserEntity>> SearchAsync(
            string searchTerm, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync(pageNumber, pageSize);

            var normalizedSearch = searchTerm.ToLower();

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && 
                            u.DeletedAt == null &&
                            (u.Username.ToLower().Contains(normalizedSearch) || 
                             u.Email.ToLower().Contains(normalizedSearch)))
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<IEnumerable<UserEntity>> GetByRoleAsync(
            string role, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be null or empty", nameof(role));

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == role && u.IsActive && u.DeletedAt == null)
                .OrderBy(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        #endregion
    }
}
