using A2A;
using A2A.AspNetCore;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

Console.Clear();
Console.OutputEncoding = Encoding.UTF8;

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

// Agent
AIAgent baseAgent = new ChatClientAgent(
    chatClient,
    instructions: "你是一个文件助手。",
    name: "FileAgent",
    tools: tools
);

AIAgent agent = baseAgent
    .AsBuilder()
    .Use(ToolCallingMiddleware)
    .Build();

// AG-UI Part Begin
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAGUI();
WebApplication app = builder.Build();

app.MapAGUI("/", agent);

await app.RunAsync();