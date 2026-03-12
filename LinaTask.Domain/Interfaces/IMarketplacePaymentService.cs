using LinaTask.Domain.DTOs;

namespace LinaTask.Domain.Interfaces
{
    public interface IMarketplacePaymentService
    {
        Task<MarketplacePaymentDto?> GetByTaskIdAsync(Guid taskId);

        Task<MarketplacePaymentDto> InitiatePaymentAsync(
            InitiatePaymentRequestDto dto,
            Guid studentId);

        Task<MarketplacePaymentDto> ConfirmPaymentHeldAsync(
            Guid taskId,
            string externalPaymentId);

        Task<MarketplacePaymentDto> ConfirmSessionPaymentAsync(
            Guid sessionId,
            string externalPaymentId);

        Task HandleWebhookApprovalAsync(string externalPaymentId);

        Task<MarketplacePaymentDto> ReleasePaymentAsync(Guid taskId, Guid studentId);

        Task<MarketplacePaymentDto> RefundPaymentAsync(Guid taskId);

        Task ProcessAutoReleasesAsync();
    }
}