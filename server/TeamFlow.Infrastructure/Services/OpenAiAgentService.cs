using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using TeamFlow.Application.Agent;

namespace TeamFlow.Infrastructure.Services;

public class OpenAiAgentService : IAgentService
{
    private readonly ChatClient _chat;
    private readonly IAgentToolExecutor _toolExecutor;
    private readonly ILogger<OpenAiAgentService> _logger;

    public OpenAiAgentService(
        IConfiguration config,
        IAgentToolExecutor toolExecutor,
        ILogger<OpenAiAgentService> logger)
    {
        var endpoint = config["AzureAi:Endpoint"]
            ?? throw new InvalidOperationException("AzureAi:Endpoint is not configured.");
        var apiKey = config["AzureAi:ApiKey"]
            ?? throw new InvalidOperationException("AzureAi:ApiKey is not configured.");
        var model = config["AzureAi:Deployments:ChatModel"] ?? "gpt-4o";

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
        _chat = azureClient.GetChatClient(model);
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async IAsyncEnumerable<AgentEvent> ChatStreamAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var messages = BuildMessages(request);
        var options = BuildOptions();

        while (true)
        {
            var toolCalls = new Dictionary<int, (string Id, string Name, string Args)>();
            string? finishReason = null;

            AsyncCollectionResult<StreamingChatCompletionUpdate>? stream = null;
            Exception? streamError = null;
            try
            {
                stream = _chat.CompleteChatStreamingAsync(messages, options, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI API call failed");
                streamError = ex;
            }

            if (streamError is not null)
            {
                yield return new AgentErrorEvent("Failed to get response from AI. Please try again.");
                yield break;
            }

            await foreach (var update in stream!.WithCancellation(ct))
            {
                if (update.FinishReason.HasValue)
                {
                    finishReason = update.FinishReason.Value.ToString();
                }

                // Stream text content
                foreach (var part in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(part.Text))
                    {
                        yield return new AgentDeltaEvent(part.Text);
                    }
                }

                // Collect tool calls
                foreach (var toolCallUpdate in update.ToolCallUpdates)
                {
                    if (!toolCalls.ContainsKey(toolCallUpdate.Index))
                    {
                        toolCalls[toolCallUpdate.Index] = (
                            toolCallUpdate.ToolCallId ?? "",
                            toolCallUpdate.FunctionName ?? "",
                            "");
                    }

                    var existing = toolCalls[toolCallUpdate.Index];
                    var argChunk = toolCallUpdate.FunctionArgumentsUpdate is { } bin
                        ? bin.ToMemory().Length > 0 ? bin.ToString() : ""
                        : "";
                    toolCalls[toolCallUpdate.Index] = (
                        string.IsNullOrEmpty(existing.Id) ? (toolCallUpdate.ToolCallId ?? existing.Id) : existing.Id,
                        string.IsNullOrEmpty(existing.Name) ? (toolCallUpdate.FunctionName ?? existing.Name) : existing.Name,
                        existing.Args + argChunk);
                }
            }

            // If no tool calls, we're done
            if (toolCalls.Count == 0)
            {
                yield return new AgentDoneEvent();
                yield break;
            }

            // Process tool calls
            var assistantToolCalls = toolCalls.Values
                .Select(tc => ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name, BinaryData.FromString(tc.Args)))
                .ToList();
            messages.Add(new AssistantChatMessage(assistantToolCalls));

            // Check if any tool call is a mutation (requires user confirmation)
            var hasMutation = toolCalls.Values.Any(tc => AgentToolCategories.IsMutation(tc.Name));

            foreach (var tc in toolCalls.Values)
            {
                if (AgentToolCategories.IsMutation(tc.Name))
                {
                    // Emit proposal — frontend opens the actual form modal with pre-filled data
                    yield return new AgentProposalEvent(tc.Name, tc.Args);
                    var proposalResult = JsonSerializer.Serialize(new { status = "opened", message = "The form has been opened with the pre-filled data. The user can now review and submit it themselves. Do NOT ask for confirmation — just briefly tell the user you have filled in the form for them." });
                    messages.Add(new ToolChatMessage(tc.Id, proposalResult));
                }
                else
                {
                    yield return new AgentToolEvent(tc.Name, "running");

                    string result;
                    Exception? toolError = null;
                    try
                    {
                        result = await _toolExecutor.ExecuteAsync(tc.Name, tc.Args, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Tool {Tool} execution failed", tc.Name);
                        result = JsonSerializer.Serialize(new { error = ex.Message });
                        toolError = ex;
                    }

                    if (toolError is not null)
                        yield return new AgentToolEvent(tc.Name, "error", toolError.Message);
                    else
                        yield return new AgentToolEvent(tc.Name, "done", result);

                    messages.Add(new ToolChatMessage(tc.Id, result));
                }
            }

            // Continue the loop to get the model's response after tool execution
        }
    }

    private static List<ChatMessage> BuildMessages(AgentRequest request)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(AgentToolDefinitions.SystemPrompt)
        };

        foreach (var msg in request.Messages)
        {
            messages.Add(msg.Role.ToLowerInvariant() switch
            {
                "user" => new UserChatMessage(msg.Content),
                "assistant" => new AssistantChatMessage(msg.Content),
                _ => new UserChatMessage(msg.Content)
            });
        }

        return messages;
    }

    private static ChatCompletionOptions BuildOptions()
    {
        var options = new ChatCompletionOptions();

        foreach (var tool in AgentToolDefinitions.Tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name,
                tool.Description,
                BinaryData.FromString(tool.ParametersJson)));
        }

        return options;
    }
}
