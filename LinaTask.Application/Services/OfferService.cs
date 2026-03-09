using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;
using System.Threading.Tasks;

namespace LinaTask.Application.Services
{
    public class OfferService : IOfferService
    {
        private readonly IOfferRepository _offerRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;

        public OfferService(
            IOfferRepository offerRepository,
            ITaskRepository taskRepository,
            IUserRepository userRepository)
        {
            _offerRepository = offerRepository;
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<OfferDto>> GetAllOffersAsync()
        {
            var offers = await _offerRepository.GetAllAsync();
            return offers.Select(MapToDto);
        }

        public async Task<IEnumerable<OfferDto>> GetOffersByTaskAsync(Guid taskId)
        {
            var offers = await _offerRepository.GetByTaskIdAsync(taskId);
            return offers.Select(MapToDto);
        }

        public async Task<IEnumerable<OfferDto>> GetOffersByTeacherAsync(Guid teacherId)
        {
            var offers = await _offerRepository.GetByTeacherIdAsync(teacherId);
            return offers.Select(MapToDto);
        }

        public async Task<OfferDto?> GetOfferByIdAsync(Guid id)
        {
            var offer = await _offerRepository.GetByIdAsync(id);
            return offer == null ? null : MapToDto(offer);
        }

        public async Task<OfferDto> CreateOfferAsync(CreateOfferDto createOfferDto)
        {
            // Validar que la tarea existe
            var task = await _taskRepository.GetByIdAsync(createOfferDto.TaskId);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            // Validar que el profesor existe
            var teacher = await _userRepository.GetByIdAsync(createOfferDto.TeacherId);
            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "teacher"))
                throw new InvalidOperationException("Invalid teacher ID");

            // Validar que no exista ya una oferta del mismo profesor para esta tarea
            if (await _offerRepository.ExistsAsync(createOfferDto.TaskId, createOfferDto.TeacherId))
                throw new InvalidOperationException("Teacher already made an offer for this task");

            var offer = new Offer
            {
                Id = Guid.NewGuid(),
                TaskId = createOfferDto.TaskId,
                TeacherId = createOfferDto.TeacherId,
                Price = createOfferDto.Price,
                Message = createOfferDto.Message,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            var createdOffer = await _offerRepository.CreateAsync(offer);
            return MapToDto(createdOffer);
        }

        public async Task<OfferDto> UpdateOfferAsync(Guid id, UpdateOfferDto updateOfferDto)
        {
            var offer = await _offerRepository.GetByIdAsync(id);
            if (offer == null)
                throw new KeyNotFoundException($"Offer with ID {id} not found");

            if (updateOfferDto.Price.HasValue)
                offer.Price = updateOfferDto.Price.Value;

            if (updateOfferDto.Message != null)
                offer.Message = updateOfferDto.Message;

            if (!string.IsNullOrEmpty(updateOfferDto.Status))
                offer.Status = updateOfferDto.Status;

            offer.CreatedAt = offer.CreatedAt.ToUniversalTime();

            var updatedOffer = await _offerRepository.UpdateAsync(offer);
            return MapToDto(updatedOffer);
        }

        public async Task<bool> DeleteOfferAsync(Guid id)
        {
            return await _offerRepository.DeleteAsync(id);
        }

        private static OfferDto MapToDto(Offer offer)
        {
            return new OfferDto
            {
                Id = offer.Id,
                TaskId = offer.TaskId,
                TaskTitle = offer.Task?.Title ?? string.Empty,
                TeacherId = offer.TeacherId,
                TeacherName = offer.Teacher?.Name ?? string.Empty,
                Price = offer.Price,
                Message = offer.Message,
                Status = offer.Status,
                CreatedAt = offer.CreatedAt, 
            };
        }
    }
}