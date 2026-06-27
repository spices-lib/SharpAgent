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
using OpenAI;
using OpenAI.Chat;
using SentenceTransformersCSharp;
using SentenceTransformersCSharp.Tokenizer;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

// Remote Agent
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("http://localhost:5000/"));
AIAgent remoteAgent = await agentCardResolver.GetAIAgentAsync();

string apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
string githubMCPToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

const string endpoint = "https://api.deepseek.com/v1";
const string model = "deepseek-v4-flash";

// Client
OpenAIClient client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
}
);

IChatClient chatClient = client.GetChatClient(model).AsIChatClient();

List<AITool> tools = new();
tools.Add(remoteAgent.AsAIFunction());

// Agent
AIAgent baseAgent = new ChatClientAgent(
    chatClient,
    instructions: "你是一个本地助手。",
    name: "LocalAgent",
    tools: tools
);

AIAgent agent = baseAgent
    .AsBuilder()
    .Use(ToolCallingMiddleware)
    .Build();

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    AgentResponse response1 = await agent.RunAsync(input, session);
    Console.WriteLine(response1);

    Console.WriteLine();
    Console.WriteLine($"Token Usage: In = {response1.Usage!.InputTokenCount} | Out = {response1.Usage.OutputTokenCount}");
    Console.WriteLine();
}