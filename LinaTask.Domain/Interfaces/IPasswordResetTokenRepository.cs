using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetToken?> GetValidTokenAsync(Guid userId, string token);
        Task<PasswordResetToken> CreateAsync(PasswordResetToken resetToken);
        Task UpdateAsync(PasswordResetToken resetToken);
        Task InvalidateAllUserTokensAsync(Guid userId);
        Task CleanExpiredTokensAsync();
    }
}
