namespace TeamFlow.Application.Agent;

public record AgentMessage(string Role, string Content);

public record AgentRequest(IReadOnlyList<AgentMessage> Messages, Guid? ProjectContextId = null);

public interface IAgentService
{
    IAsyncEnumerable<AgentEvent> ChatStreamAsync(AgentRequest request, CancellationToken ct);
}

public abstract record AgentEvent;
public record AgentDeltaEvent(string Content) : AgentEvent;
public record AgentToolEvent(string ToolName, string Status, string? Result = null) : AgentEvent;
public record AgentProposalEvent(string Action, string ParametersJson) : AgentEvent;
public record AgentDoneEvent() : AgentEvent;
public record AgentErrorEvent(string Message) : AgentEvent;
