workspace "SharpAgent"
    configurations { "Debug", "Release" }
    startproject "SharpAgent"

outputdir = "%{cfg.buildcfg}-%{cfg.system}-IL"

project "SharpAgent"
    kind "ConsoleApp"
    language "C#"
    dotnetframework "net10.0"

    targetdir("%{wks.location}/bin/" .. outputdir .. "/%{prj.name}")
	objdir("%{wks.location}/bin-int/" .. outputdir .. "/%{prj.name}")

    files 
    { 
        "src/**.cs" 
    }

    nuget 
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
   
    filter "configurations:Debug"
        symbols "On"
        optimize "Off"
   
        postbuildcommands {

            -- win: robocopy target path is relative to output path
            "robocopy \"%{wks.location}\\model\" \"model\" /E /NJH /NJS /NDL /NC /NS /NP & if \"%%errorlevel%%\" leq \"1\" exit 0"
        }

    filter "configurations:Release"
        symbols "Off"
        optimize "On"

        postbuildcommands {

            "robocopy \"%{wks.location}\\model\" \"model\" /E /NJH /NJS /NDL /NC /NS /NP & if \"%%errorlevel%%\" leq \"1\" exit 0"
        }