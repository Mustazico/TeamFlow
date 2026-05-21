using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Application.Agent;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/agent")]
[EnableRateLimiting("agent")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agent;
    private readonly IAgentToolExecutor _toolExecutor;

    public AgentController(IAgentService agent, IAgentToolExecutor toolExecutor)
    {
        _agent = agent;
        _toolExecutor = toolExecutor;
    }

    [HttpPost("chat")]
    public async Task Chat([FromBody] AgentRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var writer = Response.Body;
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await foreach (var evt in _agent.ChatStreamAsync(request, ct))
        {
            var data = evt switch
            {
                AgentDeltaEvent delta => JsonSerializer.Serialize(new { type = "delta", content = delta.Content }, jsonOptions),
                AgentToolEvent tool => JsonSerializer.Serialize(new { type = "tool", name = tool.ToolName, status = tool.Status, result = tool.Result }, jsonOptions),
                AgentProposalEvent proposal => JsonSerializer.Serialize(new { type = "proposal", action = proposal.Action, parameters = proposal.ParametersJson }, jsonOptions),
                AgentDoneEvent => JsonSerializer.Serialize(new { type = "done" }, jsonOptions),
                AgentErrorEvent err => JsonSerializer.Serialize(new { type = "error", message = err.Message }, jsonOptions),
                _ => null
            };

            if (data is null) continue;

            var line = $"data: {data}\n\n";
            await writer.WriteAsync(System.Text.Encoding.UTF8.GetBytes(line), ct);
            await writer.FlushAsync(ct);
        }
    }

    /// <summary>
    /// Execute a confirmed action proposal.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest request, CancellationToken ct)
    {
        if (!AgentToolCategories.IsMutation(request.Action))
            return BadRequest(new { error = "Invalid action." });

        var result = await _toolExecutor.ExecuteAsync(request.Action, request.ParametersJson, ct);
        return Ok(JsonSerializer.Deserialize<JsonElement>(result));
    }
}

public record ConfirmRequest(string Action, string ParametersJson);
