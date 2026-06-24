using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Configuration
{
    public required string apiKey { get; set; }
    public required string endpoint { get; set; }
    public required string model { get; set; }
}

public class AgentFactory(Configuration configuration)
{
    public AIAgent CreateHelpfulAgent()
    {
        AIAgent baseAgent = new ChatClientAgent(
            CreateAiClient(),
            instructions: "你是一个乐于助人的AI助手。",
            name: "DeepSeekAssistant"
        );

        AIAgent agent = baseAgent
            .AsBuilder()
            .Build();

        return agent;
    }

    public AIAgent CreateCustomAgent()
    {
        AIAgent baseAgent = new ChatClientAgent(
            CreateAiClient(),
            name: "DeepSeekAssistant1"
        );

        AIAgent agent = baseAgent
            .AsBuilder()
            .Build();

        return agent;
    }

    public AIAgent CreateStringAgent()
    {
        AIAgent baseAgent = new ChatClientAgent(
            CreateAiClient(),
            instructions: "你是一个字符处理工具",
            name: "StringAgent",
            tools:
            [
                AIFunctionFactory.Create(StringTools.Reverse),
                AIFunctionFactory.Create(StringTools.Uppercase),
                AIFunctionFactory.Create(StringTools.Lowercase),
            ]
        );

        AIAgent agent = baseAgent
            .AsBuilder()
            .Use(FunctionCallMiddleware)
            .Build();

        return agent;
    }
    
    public AIAgent CreateNumberAgent()
    {
        AIAgent baseAgent = new ChatClientAgent(
            CreateAiClient(),
            instructions: "你是一个数字处理工具",
            name: "NumberAgent",
            tools:
            [
                AIFunctionFactory.Create(NumberTools.RandomNumber),
                AIFunctionFactory.Create(NumberTools.AnswerToEverythingNumber),
            ]
        );

        AIAgent agent = baseAgent
            .AsBuilder()
            .Use(FunctionCallMiddleware)
            .Build();

        return agent;
    }
    
    public AIAgent CreateDelegateAgent()
    {
        AIAgent baseAgent = new ChatClientAgent(
            CreateAiClient(),
            instructions: "你是一个数字处理工具和字符处理工具的委托，不要自己干活",
            name: "DelegateAgent",
            tools:
            [
                CreateStringAgent().AsAIFunction(new AIFunctionFactoryOptions(
                {
                    Name = "StringAgentAsTool"
                }),
                CreateNumberAgent().AsAIFunction(new AIFunctionFactoryOptions(
                {
                    Name = "NumberAgentAsTool"
                })
            ]
        );

        AIAgent agent = baseAgent
            .AsBuilder()
            .Use(FunctionCallMiddleware)
            .Build();

        return agent;
    }
    
    private IChatClient CreateAiClient()
    {
        OpenAIClient client = new OpenAIClient(new ApiKeyCredential(configuration.apiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(configuration.endpoint)
            }
        );

        return client.GetChatClient(configuration.model).AsIChatClient();
    }
    
    private static async ValueTask<object> FunctionCallMiddleware(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object>> next,
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
}