using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SandboxPolicyRepository : ISandboxPolicyRepository
    {
        private readonly NexusFSDbContext _context;

        public SandboxPolicyRepository(NexusFSDbContext context)
        {
            _context = context;
        }

        public async Task<SandboxPolicy?> GetPolicyForUserAsync(string userId)
        {
            return await _context.SandboxPolicies
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task SetPolicyAsync(SandboxPolicy policy)
        {
            var existing = await GetPolicyForUserAsync(policy.UserId);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(policy);
            }
            else
            {
                await _context.SandboxPolicies.AddAsync(policy);
            }
            await _context.SaveChangesAsync();
        }
    }
}