using Microsoft.Agents.AI.Workflows;
using System;
using System.Text;

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