using LinaTask.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
        Task<IEnumerable<PaymentDto>> GetPaymentsByStudentAsync(Guid studentId);
        Task<IEnumerable<PaymentDto>> GetPaymentsByTaskAsync(Guid taskId);
        Task<IEnumerable<PaymentDto>> GetPaymentsByStatusAsync(string status);
        Task<PaymentDto?> GetPaymentByIdAsync(Guid id);
        Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createPaymentDto);
        Task<PaymentDto> UpdatePaymentAsync(Guid id, UpdatePaymentDto updatePaymentDto);
        Task<bool> DeletePaymentAsync(Guid id);
        Task<decimal> GetTotalSpentByStudentAsync(Guid studentId);
        Task<PaymentStatsDto> GetPlatformStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
