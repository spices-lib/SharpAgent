using A2A;
using A2A.AspNetCore;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
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
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Hosting;

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

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register Services needed to run DevUI
builder.Services.AddChatClient(chatClient);
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

builder.AddAIAgent("Comic Book Guy", "You are comic-book guy from South Park")
    .WithAITool(AIFunctionFactory.Create(null));

// Register "dummy" Agent
AIAgent baseAgent = new ChatClientAgent(
    chatClient,
    instructions: "Speak like a pirate",
    name: "Real Agent",
    tools: [AIFunctionFactory.Create(null)]
);

AIAgent agent = baseAgent
    .AsBuilder()
    .Build();

builder.AddAIAgent("Real Agent", (serviceProvider, key) => agent);

// Register sample workflows
IHostedAgentBuilder frenceTranslator = builder.AddAIAgent("french-translator", "Translate any text you get into French");
IHostedAgentBuilder germanTranslator = builder.AddAIAgent("german-translator", "Translate any text you get into German");

builder.AddWorkflow("translation-workflow-sequential", (sp, key) => {
    IEnumerable<AIAgent> agentsForWorkflow = new List<IHostedAgentBuilder>() { frenceTranslator, germanTranslator }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agentsForWorkflow);
}).AddAsAIAgent();

builder.AddWorkflow("translation-workflow-sequential", (sp, key) => {
    IEnumerable<AIAgent> agentsForWorkflow = new List<IHostedAgentBuilder>() { frenceTranslator, germanTranslator }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agentsForWorkflow);
}).AddAsAIAgent();

WebApplication app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.MapOpenAIResponses();
    app.MapOpenAIConversions();
    app.MapDevUI();
}

app.Run();