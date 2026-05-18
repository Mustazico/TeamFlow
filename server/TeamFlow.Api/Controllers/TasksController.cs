using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Common;
using TeamFlow.Application.Tasks;
using TeamFlow.Application.Tasks.Dtos;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<MoveTaskRequest> _moveValidator;

    public TasksController(
        ITaskService tasks,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<MoveTaskRequest> moveValidator)
    {
        _tasks = tasks;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _moveValidator = moveValidator;
    }

    [HttpGet("by-project/{projectId:guid}")]
    public async Task<IActionResult> ListForProject(Guid projectId, CancellationToken ct) =>
        Ok(await _tasks.ListForProjectAsync(projectId, ct));

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct) =>
        Ok(await _tasks.MyTasksAsync(ct));

    [HttpGet("overdue")]
    public async Task<IActionResult> Overdue(CancellationToken ct) =>
        Ok(await _tasks.OverdueAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok(await _tasks.GetAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        await _createValidator.EnsureValidAsync(req);
        var result = await _tasks.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
    {
        await _updateValidator.EnsureValidAsync(req);
        return Ok(await _tasks.UpdateAsync(id, req, ct));
    }

    [HttpPatch("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveTaskRequest req, CancellationToken ct)
    {
        await _moveValidator.EnsureValidAsync(req);
        return Ok(await _tasks.MoveAsync(id, req, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _tasks.DeleteAsync(id, ct);
        return NoContent();
    }
}
