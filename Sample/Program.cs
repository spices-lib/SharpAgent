using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using SentenceTransformersCSharp;
using SentenceTransformersCSharp.Tokenizer;

// Show Tools calling
static async ValueTask<object?> ToolCallingMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken
){
    StringBuilder toolDetails = new();
    toolDetails.Append($"- Tool Call: ' {context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        toolDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))})");
    }

    return await next.Invoke(context, cancellationToken);
}

static async Task IngestData(
    SqliteCollection<Guid, VectorEntry> collection, 
    List<(string Question, string Answer)> data, 
    AllMiniLmL6V2Embedder embedder
){
    await collection.EnsureCollectionDeletedAsync();

    await collection.EnsureCollectionExistsAsync();

    Console.Clear();
    Console.WriteLine($"开始处理 {data.Count} 条知识数据...");

    Console.Clear();
    int counter = 0;
    foreach ((string Question, string Answer) entry in data)
    {
        counter++;
        Console.Write($"\r生成向量并存储: {counter} / {data.Count}");

        string textToEmbed = $"问题：{entry.Question} 答案：{entry.Answer}";
        float[] embeddingVector = embedder.GenerateEmbedding(textToEmbed).ToArray();

        await collection.UpsertAsync(new VectorEntry
        {
            Id = Guid.NewGuid(),
            Question = entry.Question,
            Answer = entry.Answer,
            Vector = new ReadOnlyMemory<float>(embeddingVector)
        });
    }

    Console.WriteLine($"\n✅ 数据嵌入完成！共处理 {counter} 条记录。");
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

// RAG online
/*string connectionString = $"Data Source={Path.GetTempPath()}\\data.db";
VectorStore vectorStore = new SqliteVectorStore(connectionString, new SqliteVectorStoreOptions
{
    EmbeddingGenerator = client.GetEmbeddingClient("all-MiniLM-L6-v2").AsIEmbeddingGenerator()
});
VectorStoreCollection<Guid, VectorEntry> vectorStoreCollection = vectorStore.GetCollection<Guid, VectorEntry>("knowledge_base");
await vectorStoreCollection.EnsureCollectionExistsAsync();*/

// RAG offline
string modelDir = @"model/all-MiniLM-L6-v2";
string modelPath = Path.Combine(modelDir, "model_quint8_avx2.onnx");
string vocabPath = Path.Combine(modelDir, "vocab.txt");
BertTokenizer tokenizer = new BertTokenizer(vocabPath);
AllMiniLmL6V2Embedder embedder = new AllMiniLmL6V2Embedder(modelPath: modelPath, tokenizer: tokenizer);
var knowledge = DataService.GetData();
string connectionString = $"Data Source={Path.GetTempPath()}\\rag_data.db";
SqliteVectorStore vectorStore = new SqliteVectorStore(connectionString, new SqliteVectorStoreOptions
{
    EmbeddingGenerator = embedder.AsIEmbeddingGenerator()
});
var collection = vectorStore.GetCollection<Guid, VectorEntry>("knowledge_base");
await IngestData(collection, knowledge, embedder);

// Tools
PersonTools personInstance = new PersonTools();
AITool getPersonsTool = AIFunctionFactory.Create(personInstance.GetPersons, "get_persons", "获取人物信息");
AITool getPersonTool = AIFunctionFactory.Create(personInstance.GetPerson, "get_person", "获取单个人物信息");
AITool searchTool = AIFunctionFactory.Create(new SearchTool(collection).Search, "search_internal_kb");

// MCP
await using McpClient mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
    TransportMode = HttpTransportMode.StreamableHttp,
    AdditionalHeaders = new Dictionary<string, string>
    {
        { "Authorization", $"Bearer { githubMCPToken }"}
    }
}));

IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync();

List<AITool> tools = new();

tools.AddRange(mcpTools.Cast<AITool>());
tools.Add(getPersonsTool);
tools.Add(getPersonTool);
tools.Add(searchTool);

// Chat Recucer
#pragma warning disable MEAI001
IChatReducer chatReducer = new MessageCountingChatReducer(targetCount: 4);
IChatReducer chatReducer1 = new SummarizingChatReducer(chatClient, targetCount: 1, threshold: 10);
#pragma warning restore MEAI001

// Agent
AIAgent baseAgent = new ChatClientAgent(
    chatClient,
    instructions: "你是一个乐于助人的AI助手。",
    name: "DeepSeekAssistant",
    tools: tools
);

AIAgent agent = baseAgent
    .AsBuilder()
    .Use(ToolCallingMiddleware)
    .Build();

// Small Talk
/*AgentResponse response = await agent.RunAsync("你好，请介绍一下你自己");
Console.WriteLine(response);*/

// Small Talk With output structure(Deepseek模型 不支持结构化输出)
/*AgentResponse<List<Movie>> response = await agent.RunAsync<List<Movie>>("你好，请推荐几部电影");
string json = response.Text;
List<Movie> movies = response.Result;
Console.WriteLine(response);*/

/*Console.WriteLine("---");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("蛋炒饭怎么做？"))
{
    Console.Write(update);
}
Console.WriteLine();*/

// Talk Context
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