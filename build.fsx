#r "paket:
nuget BlackFox.Fake.BuildTask
nuget Fake.Core.Target
nuget Fake.Core.Process
nuget Fake.Core.ReleaseNotes
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Fake.DotNet.FSFormatting
nuget Fake.DotNet.Fsi
nuget Fake.DotNet.NuGet
nuget Fake.Api.Github
nuget Fake.DotNet.Testing.Expecto //"

#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open System.IO
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.initEnvironment ()

let project = "aglet"
let summary = "aglet: a github label editing tool for dotnet"
let configuration = "Release"

#if DEBUG
let configuration = "Debug"
#endif


let release = Fake.Core.ReleaseNotes.load "RELEASE_NOTES.md"

let version = SemVer.parse release.NugetVersion

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "src/**/nupkg"
    |> Shell.cleanDirs 
)

Target.create "AssemblyInfo" (fun _ ->

    let getAssemblyInfoAttributes projectName =
        [ Fake.DotNet.AssemblyInfo.Title (projectName)
          Fake.DotNet.AssemblyInfo.Product project
          Fake.DotNet.AssemblyInfo.Description summary
          Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
          Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion
          Fake.DotNet.AssemblyInfo.Configuration configuration ]

    let getProjectDetails projectPath =
        let projectName = Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )
    let projectDetails = 
        !! "src/**/*.fsproj"
        |> Seq.map getProjectDetails

    printfn "Assembly info:"
    projectDetails
    |> Seq.iter (printfn "%A")

    projectDetails
    |> Seq.iter 
        (fun (projFileName, _, folderName, attributes) ->
            Fake.DotNet.AssemblyInfoFile.createFSharp (folderName </> "AssemblyInfo.fs") attributes
        )
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (Fake.DotNet.DotNet.build (fun p ->
        let msBuildParams =
            {p.MSBuildParams with 
                Properties = ([
                    "AssemblyVersion",(sprintf "%i.%i.%i" version.Major version.Minor version.Patch )
                    "ProductVersion",(sprintf "%i.%i.%i" version.Major version.Minor version.Patch )
                ] @ p.MSBuildParams.Properties)
            }
        {
            p with MSBuildParams = msBuildParams
        }
    ))
)

Target.create "Pack" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
        let msBuildParams =
            {p.MSBuildParams with 
                Properties = ([
                    "Version",(sprintf "%i.%i.%i" version.Major version.Minor version.Patch )
                ] @ p.MSBuildParams.Properties)
            }
        {
            p with MSBuildParams = msBuildParams
        }
    ))
)


let runDotNet cmd workingDir =
    let result =
        Fake.DotNet.DotNet.exec (Fake.DotNet.DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let packagedToolPath = Path.getFullName "./src/aglet/" 

Target.create "InstallLocalTool" (fun _ ->
    runDotNet "tool install --add-source nupkg aglet" packagedToolPath
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "Pack"
  ==> "InstallLocalTool"
  ==> "All"

Target.runOrDefault "All"
