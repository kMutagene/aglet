// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
module App

open System
open Argu
open Domain
open DTO
open Auth
open HttpUtils
open Color
open Pastel

type AthenticationMethod =
    | Token = 1
    | Env   = 2

type ListLabelsArguments =
    | [<MainCommand; ExactlyOnce; Last>] Source_Repo of repoAddress:string
    with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Source_Repo _ ->"The source repository to list labels from. Must be of form 'owner/repoName'"

type CreateLabelsArguments =
    | Json_File of json_path:string
    | Non_Interactive
    | [<MainCommand; ExactlyOnce; Last>] Target_Repo of repoAddress:string
    with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Json_File _       -> "Specify a path to a json file containing labels to create. Must have format [{name=..,description=..,color=#..},..]"
            | Non_Interactive   -> "Use with --target-repo. Don't create labels interactively, just push all from the file"
            | Target_Repo _     -> "The target repository to create labels at. Must be of form 'owner/repoName'"


type CopyLabelsArguments =
    | [<Mandatory>] Source_Repo of sourceRepoAddress:string
    | [<Mandatory>] Target_Repo of targetRepoAddress:string
    | [<AltCommandLine("-i")>] Interactive
    with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Source_Repo _ -> "The source repository to copy labels from. Must be of form 'owner/repoName'"
            | Target_Repo _ -> "The target repository to copy labels to. Must be of form 'owner/repoName'"
            | Interactive   -> "Copy labels in interactive mode. Ask for each label if you want to copy it."

type AgletCliArguments =
    | [<Mandatory>]                 Auth    of AthenticationMethod * value:string
    | [<CliPrefix(CliPrefix.None)>] List    of ParseResults<ListLabelsArguments>
    | [<CliPrefix(CliPrefix.None)>] Create  of ParseResults<CreateLabelsArguments>
    | [<CliPrefix(CliPrefix.None)>] Copy    of ParseResults<CopyLabelsArguments>
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Auth   _  -> "How to authenticate at Github. token: raw string of a authentication token. env: name of an environment variable where your token is stored"
            | List   _  -> "List all labels of the repository. <repo> must be of form 'owner:repoName'"
            | Create _  -> "Create Labels in a repository. Labels can be created interactively or taken from a json source."
            | Copy   _  -> "Copy labels from one repository to another. <from-repo> and <to-repo> must be of form 'owner/repoName'"

let parseRepoName (r:string) =
    let split = r.Split('/')
    if split.Length = 2 then
        split.[0],split.[1]
    else
        failwith "invalid repo address format. It must be of form owner/repoName"

let parseAuth a = 
    match a with
    | (AthenticationMethod.Token,t) -> t
    | (AthenticationMethod.Env,env) ->
        match Auth.tryGetAuthTokenFromEnv env with
        | Some token -> token
        | _ -> failwith "no valid token provided"
    | _ ->  "no valid token provided"


let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)

let parser = ArgumentParser.Create<AgletCliArguments>(programName = "aglet",errorHandler = errorHandler)

let tryGetDefaultString (str: string option) =
    match str with
    | Some s -> s
    | None -> ""

module ApiFunctions =

    let getAllLabels owner repo authToken = 
        GithubApiRequests.createGetLabelRequest owner repo
        |> GithubApiRequests.setAuth authToken
        |> GithubApiRequests.getResponse
        |> GithubApiRequests.getResponseBody
        |> DTO.Github.LabelInfoResponse.ofJsonArray
        |> Array.map DTO.Github.LabelInfoResponse.toDomain

module ConsoleFormatting  =
    
    let formatLabelConsoleOutput (label:IssueLabel) =
        let backgroundColor = label.Color |> Colors.fromHex
        let foregroundColor = Colors.toForegroundTextColor backgroundColor

        label.Name
        |> Console.setBackgroundColor backgroundColor
        |> Console.setForegroundColor foregroundColor
        

[<AutoOpen>]
module MessagePrompts =

    let prompt (msg:string) =
        System.Console.Write(msg)
        System.Console.ReadLine().Trim()
        |> function | "" -> None | s -> Some s
        |> Option.map (fun s -> s.Replace ("\"","\\\""))

    let rec promptYesNo msg =
        match prompt (sprintf "%s [Yn]: " msg) with
        | Some "Y" | Some "y" -> true
        | Some "N" | Some "n" -> false
        | _ -> System.Console.WriteLine("Sorry, invalid answer"); promptYesNo msg

[<EntryPoint>]
let main argv =

    printfn "\r\n"

    let parsedArgs      = parser.ParseCommandLine(argv)

    let authToken       = parsedArgs.PostProcessResult (Auth, parseAuth)

    let listArgs        = parsedArgs.GetResults(List)
    let createArgs      = parsedArgs.GetResults(Create)
    let copyArgs        = parsedArgs.GetResults(Copy)

    parsedArgs
        .GetAllResults()
        |> List.iter (fun x -> x.ToString() |> Console.WriteLine)

    match listArgs,createArgs,copyArgs with
    | repo::_,[],[] ->
        let owner,repo = repo.PostProcessResult(ListLabelsArguments.Source_Repo,parseRepoName)
        ApiFunctions.getAllLabels owner repo authToken
        |> Array.iter (ConsoleFormatting.formatLabelConsoleOutput >> Console.WriteLine)

    | [],cArgs,[] when cArgs.Length > 0 -> 
        ()
    | _ -> ()

    //match listRepo,copyLabelRepos with
    //| Some (lOwner,lRepo), None -> 
    //    Console.WriteLine((sprintf "Found the following labels in %s:%s\r\n" lOwner lRepo).Pastel("#00FF00"))

    //    ApiFunctions.getAllLabels lOwner lRepo authToken
    //    |> Seq.iter (ConsoleFormatting.formatLabelConsoleOutput >> Console.WriteLine)

    //| None, Some ((fromOwner,fromRepo),(toOwner,toRepo)) ->
    //    let fromLabels = ApiFunctions.getAllLabels fromOwner fromRepo authToken

    //    let toLabels = ApiFunctions.getAllLabels toOwner toRepo authToken

    //    let labelNamesAlreadyThere = toLabels |> Array.map (fun x -> x.Name)

    //    let labelsAlreadyThere = 
    //        fromLabels
    //        |> Array.filter ( fun label ->
    //            (Array.contains label.Name labelNamesAlreadyThere )
    //        )
    //    let diff =
    //        fromLabels
    //        |> Array.filter ( fun label ->
    //            not (Array.contains label.Name labelNamesAlreadyThere )
    //        )

    //    Console.WriteLine((sprintf "%s:%s already contains the following labels:\r\n" fromOwner fromRepo))
            
    //    labelsAlreadyThere
    //    |> Seq.iter (ConsoleFormatting.formatLabelConsoleOutput >> Console.WriteLine)

    //    Console.WriteLine((sprintf "The following new labels will be created in %s:%s\r\n" toOwner toRepo).Pastel("#00FF00"))

    //    diff
    //    |> Seq.iter (ConsoleFormatting.formatLabelConsoleOutput >> Console.WriteLine)

    //    if MessagePrompts.promptYesNo (sprintf "Copy all these labels to %s:%s? (y/n)" toOwner toRepo) then 
    //        diff
    //        |> Array.iter (fun label ->
    //            let req =
    //                label
    //                |> DTO.Github.LabelPostRequest.fromDomain
    //                |> GithubApiRequests.createPostLabelRequest toOwner toRepo
    //                |> GithubApiRequests.setAuth authToken
                
    //            let res = req |> GithubApiRequests.getResponse

    //            if HttpUtils.GithubApiRequests.isSuccess res then 
    //                "label ".Pastel("#00FF00")
    //                + (label |> ConsoleFormatting.formatLabelConsoleOutput)
    //                + (sprintf " successfully created in %s:%s\r\n" toOwner toRepo).Pastel("#00FF00")
    //                |> Console.WriteLine
    //            else
    //                "failed to create label ".Pastel("#FF0000")
    //                + (label |> ConsoleFormatting.formatLabelConsoleOutput)
    //                + (sprintf " in %s:%s\r\n" toOwner toRepo).Pastel("#FF0000")
    //                |> Console.WriteLine
    //        )


    //    else 
    //        ()
        

    //| _ -> ()

    printfn "press any key to exit"
    Console.ReadLine() |> ignore
    0 // return an integer exit code