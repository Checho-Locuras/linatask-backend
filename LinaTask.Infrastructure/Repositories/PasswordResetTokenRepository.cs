using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly LinaTaskDbContext _context;

        public PasswordResetTokenRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken?> GetValidTokenAsync(Guid userId, string token)
        {
            return await _context.PasswordResetTokens
                .Where(t => t.UserId == userId
                    && t.Token == token
                    && !t.IsUsed
                    && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken resetToken)
        {
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();
            return resetToken;
        }

        public async Task UpdateAsync(PasswordResetToken resetToken)
        {
            _context.PasswordResetTokens.Update(resetToken);
            await _context.SaveChangesAsync();
        }

        public async Task InvalidateAllUserTokensAsync(Guid userId)
        {
            var tokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CleanExpiredTokensAsync()
        {
            var expiredTokens = await _context.PasswordResetTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            _context.PasswordResetTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}