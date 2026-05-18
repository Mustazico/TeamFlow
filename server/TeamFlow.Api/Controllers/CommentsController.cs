using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Common;
using TeamFlow.Application.Comments;
using TeamFlow.Application.Comments.Dtos;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;
    private readonly IValidator<CreateCommentRequest> _createValidator;
    private readonly IValidator<UpdateCommentRequest> _updateValidator;

    public CommentsController(
        ICommentService comments,
        IValidator<CreateCommentRequest> createValidator,
        IValidator<UpdateCommentRequest> updateValidator)
    {
        _comments = comments;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("by-task/{taskId:guid}")]
    public async Task<IActionResult> ListForTask(Guid taskId, CancellationToken ct) =>
        Ok(await _comments.ListForTaskAsync(taskId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest req, CancellationToken ct)
    {
        await _createValidator.EnsureValidAsync(req);
        return Ok(await _comments.CreateAsync(req, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommentRequest req, CancellationToken ct)
    {
        await _updateValidator.EnsureValidAsync(req);
        return Ok(await _comments.UpdateAsync(id, req, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _comments.DeleteAsync(id, ct);
        return NoContent();
    }
}
