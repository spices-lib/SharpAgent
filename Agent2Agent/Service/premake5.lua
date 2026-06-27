project "Agent2AgentService"
    kind "ConsoleApp"
    language "C#"
    dotnetframework "net10.0"

    targetdir("%{wks.location}/bin/" .. outputdir .. "/%{prj.name}")
	objdir("%{wks.location}/bin-int/" .. outputdir .. "/%{prj.name}")

    files 
    { 
        "**.cs" 
    }

    nuget 
    { 
        nugets,
        "Microsoft.Agents.AI.Workflows:1.11.1",
        "Microsoft.Agents.AI.Hosting.A2A.AspNetCore:1.11.1-preview.260625.1",
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