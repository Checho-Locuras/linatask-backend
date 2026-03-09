using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;

namespace LinaTask.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;

        public TaskService(ITaskRepository taskRepository, IUserRepository userRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<TaskDto>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();
            return tasks.Select(MapToDto);
        }

        public async Task<IEnumerable<TaskDto>> GetTasksByStudentAsync(Guid studentId)
        {
            var tasks = await _taskRepository.GetByStudentIdAsync(studentId);
            return tasks.Select(MapToDto);
        }

        public async Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(string status)
        {
            var tasks = await _taskRepository.GetByStatusAsync(status);
            return tasks.Select(MapToDto);
        }

        public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            return task == null ? null : MapToDto(task);
        }

        public async Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto)
        {
            var student = await _userRepository.GetByIdAsync(createTaskDto.StudentId);
            if (student == null || !student.UserRoles.Any(ur => ur.Role.Name == "student"))
                throw new InvalidOperationException("Invalid student ID");

            var task = new TaskU
            {
                Id = Guid.NewGuid(),
                StudentId = createTaskDto.StudentId,
                Title = createTaskDto.Title,
                Description = createTaskDto.Description,
                Subject = createTaskDto.Subject,
                Deadline = createTaskDto.Deadline,
                Budget = createTaskDto.Budget,
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            var createdTask = await _taskRepository.CreateAsync(task);
            return MapToDto(createdTask);
        }

        public async Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {id} not found");

            if (!string.IsNullOrEmpty(updateTaskDto.Title))
                task.Title = updateTaskDto.Title;

            if (updateTaskDto.Description != null)
                task.Description = updateTaskDto.Description;

            if (updateTaskDto.Subject != null)
                task.Subject = updateTaskDto.Subject;

            if (updateTaskDto.Deadline.HasValue)
                task.Deadline = updateTaskDto.Deadline;

            if (updateTaskDto.Budget.HasValue)
                task.Budget = updateTaskDto.Budget;

            if (!string.IsNullOrEmpty(updateTaskDto.Status))
                task.Status = updateTaskDto.Status;

            task.CreatedAt = task.CreatedAt.ToUniversalTime();

            var updatedTask = await _taskRepository.UpdateAsync(task);
            return MapToDto(updatedTask);
        }

        public async Task<bool> DeleteTaskAsync(Guid id)
        {
            return await _taskRepository.DeleteAsync(id);
        }

        private static TaskDto MapToDto(TaskU task)
        {
            return new TaskDto
            {
                Id = task.Id,
                StudentId = task.StudentId,
                StudentName = task.Student?.Name ?? string.Empty,
                Title = task.Title,
                Description = task.Description,
                Subject = task.Subject,
                Deadline = task.Deadline,
                Budget = task.Budget,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                OffersCount = task.Offers?.Count ?? 0
            };
        }
    }
}
