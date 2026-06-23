using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TestExecutor(AIAgent agent, string id) : Executor(id)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        var routes = protocolBuilder.RouteBuilder;

        routes.AddHandler<string, string>(HandleAsync);

        protocolBuilder.SendsMessage<string>();
        protocolBuilder.YieldsOutput<string>();

        return protocolBuilder;
    }

    public async ValueTask<string> HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(message);
        AgentResponse response = await agent.RunAsync(message);
        Console.WriteLine(response.Text);
        return response.Text;
    }
}