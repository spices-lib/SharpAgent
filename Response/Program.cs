#pragma warning disable OPENAI001
using ModelContextProtocol.Protocol;
using OpenAI;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

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

// Deepseek do not support response API
ResponsesClient responseClient = client.GetResponsesClient();


#pragma warning restore OPENAI001