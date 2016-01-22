#I @"tools\FAKE\tools\"
#r @"tools\FAKE\tools\FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open Fake.Git
open System.IO

let projectName           = "Dynamics CRM Solution Exchanger"

//Directories
let buildDir              = @".\build\"
let appBuildDir           = buildDir + @"app\"

let deployDir               = @".\Publish\"
let appdeployDir            = deployDir + @"app\"

let packagesDir             = @".\packages\"


let nugetDir = @".\nuget\" 
let nugetDeployDir = "TBD"

let mutable version         = "1.0"
let mutable build           = buildVersion
let mutable nugetVersion    = ""
let mutable asmVersion      = ""
let mutable asmInfoVersion  = ""
let mutable setupVersion    = ""

let gitbranch = Git.Information.getBranchName "."
let sha = Git.Information.getCurrentHash()

Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir; nugetDir]
)

Target "RestorePackages" (fun _ ->
   RestorePackages()
)

Target "BuildVersions" (fun _ ->

    let safeBuildNumber = if not isLocalBuild then build else "0"

    asmVersion      <- version + "." + safeBuildNumber
    asmInfoVersion  <- asmVersion + " - " + gitbranch + " - " + sha

    nugetVersion    <- version + "." + safeBuildNumber
    setupVersion    <- version + "." + safeBuildNumber

    match gitbranch with
        | "master" -> ()
        | "develop" -> (nugetVersion <- nugetVersion + " - " + "beta")
        | _ -> (nugetVersion <- nugetVersion + " - " + gitbranch)

    SetBuildNumber nugetVersion
)
Target "AssemblyInfo" (fun _ ->
    BulkReplaceAssemblyInfoVersions "src/" (fun f ->
                                              {f with
                                                  AssemblyVersion = asmVersion
                                                  AssemblyInformationalVersion = asmInfoVersion})
)

Target "BuildApp" (fun _->
    let SetMSBuildToolsVersion (toolsVersion:string option) =
        trace "Setting MSBuild tools version..."

        match toolsVersion with
        | Some version -> MSBuildHelper.MSBuildDefaults <- { MSBuildHelper.MSBuildDefaults with ToolsVersion = toolsVersion }
        | None -> trace "No MSBuild tools version provided, using default."

    Some "4.0" |> SetMSBuildToolsVersion
        
    !! @"src\app\**\*.fsproj"
      |> MSBuildRelease appBuildDir "Build"
      |> Log "Build - Output: "
)

Target "CreateNuget" (fun _ ->     
    CreateDir nugetDir

    "DynamicsCRMSolutionExchanger.nuspec"
          |> NuGet (fun p -> 
            {p with               
                Project = projectName
                Version = nugetVersion
                NoPackageAnalysis = true                           
                ToolPath = @".\tools\Nuget\Nuget.exe"                             
                OutputPath = nugetDir 
            })

)

Target "PublishNugetToFeed" (fun _ ->
    XCopy nugetDir nugetDeployDir 
)

Target "Publish" (fun _ ->
    CreateDir appdeployDir

    !! (buildDir @@ @"/**/*.* ")
      -- " *.pdb"
        |> CopyTo appdeployDir 
)

Target "Zip" (fun _ ->
    !! (buildDir @@ @"\**\*.* ")
        -- " *.zip"
            |> Zip appBuildDir (deployDir + projectName + version + ".zip")
)

"Clean"
  ==> "RestorePackages"
  ==> "BuildVersions"
  =?> ("AssemblyInfo", not isLocalBuild )
  ==> "BuildApp"
  ==> "Zip"
  ==> "Publish"
  ==> "CreateNuget"
  ==> "PublishNugetToFeed"


RunTargetOrDefault "Publish"