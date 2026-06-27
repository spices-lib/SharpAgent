using A2A;
using A2A.AspNetCore;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SentenceTransformersCSharp;
using SentenceTransformersCSharp.Tokenizer;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Show Tools calling
static async ValueTask<object?> ToolCallingMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken
)
{
    StringBuilder toolDetails = new();
    toolDetails.Append($"- Tool Call: ' {context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        toolDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))})");
    }

    return await next.Invoke(context, cancellationToken);
}

await Task.Delay(1000);

Console.Clear();
Console.OutputEncoding = Encoding.UTF8;


HttpClient httpClient = new HttpClient();
const string serverUrl = "http://localhost:5000";
AGUIChatClient chatClioent = new AGUIChatClient(httpClient, serverUrl);
AIAgent agent = chatClioent.CreateAIAgent(
    tools: [AIFunctionFactory.Create(null, name: "change_color")]    
);

List<ChatMessage> messages = [new ChatMessage(ChatRole.System, "You are a nice AI Agent")];

while (true)
{
    Console.Write("> ");
    string message = Console.ReadLine() ?? "";
    if (message == string.Empty)
    {
        continue;
    }

    messages.Add(new ChatMessage(ChatRole.User, message));

    List<AgentResponseUpdate> updates = [];
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messages))
    {
        updates.Add(update);
        foreach (AIContent content in update.Contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(textContent.Text);
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case FunctionCallContent functionCallContent:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    StringBuilder toolCallDetails = new();
                    toolCallDetails.Append($"[Tool Call: {functionCallContent.Name}]");
                    if (functionCallContent.Arguments.Any())
                    {
                        toolCallDetails.Append($" (Args: {string.Join(",", functionCallContent.Arguments.Select(x => x))})");
                    }
                    toolCallDetails.Append("]");
                    Console.WriteLine(toolCallDetails);
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case FunctionResultContent functionResultContent:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    bool isError = functionResultContent.Exception != null;
                    Console.WriteLine(isError ? $"[Tool Error: {functionResultContent.Exception}]" : $"[Tool Result: {functionResultContent.Result}]");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case ErrorContent errorContent:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[Error: {errorContent.Message}]");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
        }
    }

    AgentResponse fullResponse = updates.ToAgentResponse();
    messages.AddRange(fullResponse.Messages);

    Console.WriteLine();
    Console.WriteLine();
}