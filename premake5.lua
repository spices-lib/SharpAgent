workspace "SharpAgent"
    configurations { "Debug", "Release" }
    startproject "SharpAgent"

outputdir = "%{cfg.buildcfg}-%{cfg.system}-IL"

nugets =
{ 
    "Microsoft.Agents.AI:1.10.0",
    "Microsoft.Agents.AI.OpenAI:1.10.0",
    "Microsoft.Extensions.Configuration:11.0.0-preview.5.26302.115",
    "Microsoft.Extensions.Configuration.UserSecrets:11.0.0-preview.5.26302.115",
    "ModelContextProtocol:1.4.0",
    "Microsoft.SemanticKernel.Connectors.SqliteVec:1.74.0-preview",
    "SentenceTransformersCSharp:1.0.4",
    "Newtonsoft.Json:13.0.5-beta1",
}

include "Sample"
include "MultiAgents"
include "Response"