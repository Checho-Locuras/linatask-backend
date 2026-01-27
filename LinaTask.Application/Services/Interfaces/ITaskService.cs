using global::LinaTask.Application.DTOs;
using global::LinaTask.Domain.Interfaces;
using global::LinaTask.Domain.Models;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskDto>> GetAllTasksAsync();
        Task<IEnumerable<TaskDto>> GetTasksByStudentAsync(Guid studentId);
        Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(string status);
        Task<TaskDto?> GetTaskByIdAsync(Guid id);
        Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto);
        Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto);
        Task<bool> DeleteTaskAsync(Guid id);
    }
}
