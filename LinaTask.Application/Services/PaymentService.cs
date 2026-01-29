using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LinaTask.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;

        public PaymentService(
            IPaymentRepository paymentRepository,
            ITaskRepository taskRepository,
            IUserRepository userRepository)
        {
            _paymentRepository = paymentRepository;
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return payments.Select(MapToDto);
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByStudentAsync(Guid studentId)
        {
            var payments = await _paymentRepository.GetByStudentIdAsync(studentId);
            return payments.Select(MapToDto);
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByTaskAsync(Guid taskId)
        {
            var payments = await _paymentRepository.GetByTaskIdAsync(taskId);
            return payments.Select(MapToDto);
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByStatusAsync(string status)
        {
            var payments = await _paymentRepository.GetByStatusAsync(status);
            return payments.Select(MapToDto);
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            return payment == null ? null : MapToDto(payment);
        }

        public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createPaymentDto)
        {
            // Validar que la tarea existe
            var task = await _taskRepository.GetByIdAsync(createPaymentDto.TaskId);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            // Validar que el estudiante existe
            var student = await _userRepository.GetByIdAsync(createPaymentDto.StudentId);
            if (student == null || student.Role != "student")
                throw new InvalidOperationException("Invalid student ID");

            // Calcular fees
            var platformFee = createPaymentDto.Amount * (createPaymentDto.PlatformFeePercentage / 100);
            var teacherAmount = createPaymentDto.Amount - platformFee;

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TaskId = createPaymentDto.TaskId,
                StudentId = createPaymentDto.StudentId,
                Amount = createPaymentDto.Amount,
                PlatformFee = platformFee,
                TeacherAmount = teacherAmount,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);
            return MapToDto(createdPayment);
        }

        public async Task<PaymentDto> UpdatePaymentAsync(Guid id, UpdatePaymentDto updatePaymentDto)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                throw new KeyNotFoundException($"Payment with ID {id} not found");

            if (!string.IsNullOrEmpty(updatePaymentDto.Status))
            {
                // Validar transiciones de estado permitidas
                var validTransitions = new Dictionary<string, string[]>
                {
                    { "pending", new[] { "completed", "failed" } },
                    { "completed", new[] { "refunded" } },
                    { "failed", new[] { "pending" } }
                };

                if (validTransitions.ContainsKey(payment.Status))
                {
                    if (!validTransitions[payment.Status].Contains(updatePaymentDto.Status))
                        throw new InvalidOperationException(
                            $"Invalid status transition from {payment.Status} to {updatePaymentDto.Status}");
                }

                payment.Status = updatePaymentDto.Status;
            }

            payment.CreatedAt = payment.CreatedAt.ToUniversalTime();

            var updatedPayment = await _paymentRepository.UpdateAsync(payment);
            return MapToDto(updatedPayment);
        }

        public async Task<bool> DeletePaymentAsync(Guid id)
        {
            return await _paymentRepository.DeleteAsync(id);
        }

        public async Task<decimal> GetTotalSpentByStudentAsync(Guid studentId)
        {
            return await _paymentRepository.GetTotalByStudentAsync(studentId);
        }

        public async Task<PaymentStatsDto> GetPlatformStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var payments = await _paymentRepository.GetByStatusAsync("completed");

            if (startDate.HasValue)
                payments = payments.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                payments = payments.Where(p => p.CreatedAt <= endDate.Value);

            var paymentsList = payments.ToList();

            return new PaymentStatsDto
            {
                TotalAmount = paymentsList.Sum(p => p.Amount),
                TotalPlatformFees = paymentsList.Sum(p => p.PlatformFee),
                TotalTeacherAmount = paymentsList.Sum(p => p.TeacherAmount),
                TotalPayments = paymentsList.Count
            };
        }

        private static PaymentDto MapToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                TaskId = payment.TaskId,
                TaskTitle = payment.TaskU?.Title ?? string.Empty,
                StudentId = payment.StudentId,
                StudentName = payment.Student?.Name ?? string.Empty,
                Amount = payment.Amount,
                PlatformFee = payment.PlatformFee,
                TeacherAmount = payment.TeacherAmount,
                Status = payment.Status,
                CreatedAt = payment.CreatedAt
            };
        }
    }
}
