using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpGet("getAllTasks")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetAllTasks()
        {
            try
            {
                var tasks = await _taskService.GetAllTasksAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tareas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTaskById/{id:guid}")]
        public async Task<ActionResult<TaskDto>> GetTaskById(Guid id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                    return NotFound($"Tarea con ID {id} no encontrada");

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tarea {TaskId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTasksByStudentId/{studentId:guid}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByStudentId(Guid studentId)
        {
            try
            {
                var tasks = await _taskService.GetTasksByStudentAsync(studentId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tareas del estudiante");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("getTasksByStatus/{status}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByStatus(string status)
        {
            try
            {
                var tasks = await _taskService.GetTasksByStatusAsync(status);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tareas por estado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("createTask")]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            try
            {
                var task = await _taskService.CreateTaskAsync(createTaskDto);
                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tarea");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("updateTask/{id:guid}")]
        public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            try
            {
                var task = await _taskService.UpdateTaskAsync(id, updateTaskDto);
                return Ok(task);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Tarea con ID {id} no encontrada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tarea {TaskId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("deleteTask/{id:guid}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            try
            {
                var deleted = await _taskService.DeleteTaskAsync(id);
                if (!deleted)
                    return NotFound($"Tarea con ID {id} no encontrada");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar tarea {TaskId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}