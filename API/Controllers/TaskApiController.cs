using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using API.Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/task")]
    public class TaskApiController : ControllerBase
    {
        private readonly ITaskInterface _taskRepo;
        private readonly RedisService _redisService;
        private readonly ElasticsearchService _elasticService;

        private const int CACHE_DURATION_MINUTES = 30;

        public TaskApiController(ITaskInterface taskInterface, RedisService redisService, ElasticsearchService elasticsearchService)
        {
            _taskRepo = taskInterface;
            _redisService = redisService;
            _elasticService = elasticsearchService;
        }

        #region Elastic search Task Name, Description, Status
        [HttpGet("search/{query}")]
        public async Task<IActionResult> SearchTasks(
            string query, 
            [FromQuery] string? status = null, 
            [FromQuery] Guid? userId = null, 
            [FromQuery] DateTime? dueDate = null,
            [FromQuery] int? estimatedDaysFilter = null) // New parameter for estimated days filter
        {
            var tasks = await _elasticService.SearchTasksAsync(query);

            if (!tasks.Any())
            {
                return NotFound(new { message = "No tasks found." });
            }

            // Apply filtering in the controller
            if (!string.IsNullOrEmpty(status))
            {
                tasks = tasks.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (userId.HasValue)
            {
                tasks = tasks.Where(t => t.UserId == userId.Value).ToList();
            }

            if (dueDate.HasValue)
            {
                tasks = tasks.Where(t => t.EndDate.HasValue && t.EndDate.Value.ToDateTime(TimeOnly.MinValue) >= dueDate.Value).ToList();
            }

            // Apply estimated days filter
            if (estimatedDaysFilter.HasValue)
            {
                var estimatedEndDate = DateTime.UtcNow.AddDays(estimatedDaysFilter.Value);
                tasks = tasks.Where(t => t.EndDate.HasValue && t.EndDate.Value.ToDateTime(TimeOnly.MinValue) <= estimatedEndDate).ToList();
            }

            // Ensure unique tasks based on TaskId
            var uniqueTasks = tasks.GroupBy(t => t.TaskId).Select(g => g.First()).ToList();

            return uniqueTasks.Any()
                ? Ok(new { data = uniqueTasks })
                : NotFound(new { message = "No tasks found." });
        }
        #endregion


        #region Get: Get All
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            const string cacheKey = "taskList";
            var cachedTaskList = await _redisService.GetListAsync<Repositories.Models.Task>(cacheKey);

            if (cachedTaskList == null || cachedTaskList.Count == 0)
            {
                cachedTaskList = await _taskRepo.GetAll();
                if (cachedTaskList != null && cachedTaskList.Any())
                {
                    await _redisService.SetListAsync(cacheKey, cachedTaskList, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                }
            }
            if (cachedTaskList == null)
            {
                // Handle the case when the list is null
                return StatusCode(500, new { message = "There was some error while retrieving tasks." });
            }
            return Ok(new
            {
                success = true,
                message = "Tasks retrieved successfully.",
                data = cachedTaskList
            });
        }
        #endregion


        #region Get: Get One
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(string id)
        {
            string taskKey = $"task_{id}";
            var cachedTask = await _redisService.GetObjectAsync<Repositories.Models.Task>(taskKey);

            if (cachedTask == null)
            {
                cachedTask = await _taskRepo.GetOne(id);
                if (cachedTask != null)
                {
                    await _redisService.SetObjectAsync(taskKey, cachedTask, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                }
            }
            if (cachedTask == null)
            {
                return NotFound(new { success = false, message = "Task not found." });
            }
            return Ok(new
            {
                success = true,
                message = "Task retrieved successfully.",
                data = cachedTask
            });
        }
        #endregion


        #region Get: Get All By User
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetByUser(string id)
        {
            string cacheKey = $"userTaskList_{id}";
            var cachedTaskList = await _redisService.GetListAsync<Repositories.Models.Task>(cacheKey);

            if (cachedTaskList == null || cachedTaskList.Count == 0)
            {
                cachedTaskList = await _taskRepo.GetAllByUser(id);
                if (cachedTaskList != null && cachedTaskList.Any())
                {
                    await _redisService.SetListAsync(cacheKey, cachedTaskList, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                }
            }
            if (cachedTaskList == null)
            {
                return StatusCode(500, new
                {
                    message = "There was some error while retrieving tasks for user.",
                });
            }
            return Ok(new
            {
                success = true,
                message = "Tasks for user retrieved successfully.",
                data = cachedTaskList
            });
        }
        #endregion


        #region Post: Create
        [HttpPost]
        public async Task<IActionResult> Create(Repositories.Models.Task model)
        {
            int affectedRows = await _taskRepo.Add(model);
            if (affectedRows <= 0)
            {
                return StatusCode(500, new { message = "Failed to add task." });
            }

            // Invalidate relevant caches
            await InvalidateTaskCaches(model.UserId.ToString());
            
            return Ok(new { message = "Task added successfully!" });
        }
        #endregion


        #region Put: Update
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Repositories.Models.Task task)
        {
            int affectedRows = await _taskRepo.Update(task);
            if (affectedRows <= 0)
            {
                return NotFound(new { message = "Task not found or not updated." });
            }

            // Invalidate relevant caches
            await InvalidateTaskCaches(task.UserId.ToString(), task.TaskId.ToString());

            return Ok(new { message = "Task updated successfully" });
        }
        #endregion


        #region Delete: Delete Task
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { success = false, message = "Invalid ID." });
            }

            // Get the task before deletion to know which caches to invalidate
            var task = await _taskRepo.GetOne(id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found." });
            }

            int affectedRows = await _taskRepo.Delete(id);
            if (affectedRows <= 0)
            {
                return NotFound(new { message = "Task not found or already deleted." });
            }

            // Invalidate relevant caches
            await InvalidateTaskCaches(task.UserId.ToString(), task.TaskId.ToString());

            return Ok(new { message = "Task deleted successfully!" });
        }
        #endregion

        #region Private Helper Methods
        private async Task InvalidateTaskCaches(string userId, string? taskId = null)
        {
            // Invalidate global task list
            await _redisService.KeyDeleteAsync("taskList");

            // Invalidate user's task list
            await _redisService.KeyDeleteAsync($"userTaskList_{userId}");

            // Invalidate specific task cache if taskId is provided
            if (!string.IsNullOrEmpty(taskId))
            {
                await _redisService.KeyDeleteAsync($"task_{taskId}");
            }
        }
        #endregion
    }
}
