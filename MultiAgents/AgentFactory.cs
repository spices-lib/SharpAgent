using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Net;

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

    private IChatClient CreateAiClient()
    {
        OpenAIClient client = new OpenAIClient(new ApiKeyCredential(configuration.apiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(configuration.endpoint)
            }
        );

        return client.GetChatClient(configuration.model).AsIChatClient();
    }
}