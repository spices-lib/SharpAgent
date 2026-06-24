using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

Configuration configuration = new Configuration
{
    apiKey   = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY"),
    endpoint = "https://api.deepseek.com/v1",
    model    = "deepseek-v4-flash"
};

AgentFactory agentFactory = new(configuration);

TestExecutor step0  = new(agentFactory.CreateHelpfulAgent(), "step0");
TestExecutor1 step1 = new(agentFactory.CreateCustomAgent(), "step1");
TestExecutor1 succeed = new(agentFactory.CreateCustomAgent(), "succeed");
TestExecutor1 faled = new(agentFactory.CreateCustomAgent(), "faled");

WorkflowBuilder builder = new(step0);

builder.AddEdge(
    source: step0,
    target: step1
);

builder.AddSwitch(
    source: step1,
    SwitchBuilder =>
    {
        SwitchBuilder.AddCase<string>(x => x!.Contains("正确"), succeed);
        SwitchBuilder.AddCase<string>(x => x!.Contains("错误"), faled);
    }
);

Microsoft.Agents.AI.Workflows.Workflow workflow = builder.Build();

Console.Clear();
Console.OutputEncoding = Encoding.UTF8;

const string input = "你好，1+1=2 是正确的吗？, 请直接回答我是正确还是错误";

StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine($"{executorComplete.ExecutorId} Completed");
    }
}

static async void Sequential()
{
    Configuration configuration = new Configuration
    {
        apiKey   = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY"),
        endpoint = "https://api.deepseek.com/v1",
        model    = "deepseek-v4-flash"
    };
    
    AgentFactory agentFactory = new(configuration);
    
    AIAgent summaryAgent = agentFactory.CreateSummaryAgent();
    AIAgent translateAgent = agentFactory.CreateTranslateAgent();

    Workflow workflow = AgentWorkflowBuilder.BuildSequential(summaryAgent, translateAgent);

    string legalText = """
                       假设这里有一段巨长的文章
                       """;

    var message = new List<ChatMessage>
    {
        new (ChatRole.User, legalText) 
    };

    StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, message);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

    List<ChatMessage> result = new();
    await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
    {
        if (evt is WorkflowOutputEvent completed)
        {
            result = (List<ChatMessage>)completed.Data;
            break;
        }
    }
    
    foreach (ChatMessage msg in result.Where(x => x.Role != ChatRole.User))
    {
        Console.WriteLine($"{msg.AuthorName}");
        Console.WriteLine($"{msg.Text}");
    }
}

static async void Concurrent()
{
    Configuration configuration = new Configuration
    {
        apiKey   = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY"),
        endpoint = "https://api.deepseek.com/v1",
        model    = "deepseek-v4-flash"
    };
    
    AgentFactory agentFactory = new(configuration);
    
    AIAgent legalAgent = agentFactory.CreateLegalAgent();
    AIAgent spellingErrorAgent = agentFactory.CreateSpellingErrorAgent();

    Workflow workflow = AgentWorkflowBuilder.BuildConcurrent([legalAgent, spellingErrorAgent]);

    string legalText = """
                       假设这里有一段巨长的文章
                       """;

    var message = new List<ChatMessage>
    {
        new (ChatRole.User, legalText) 
    };

    StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, message);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

    List<ChatMessage> result = new();
    await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
    {
        if (evt is WorkflowOutputEvent completed)
        {
            result = (List<ChatMessage>)completed.Data;
            break;
        }
    }
    
    foreach (ChatMessage msg in result.Where(x => x.Role != ChatRole.User))
    {
        Console.WriteLine($"{msg.AuthorName}");
        Console.WriteLine($"{msg.Text}");
    }
}