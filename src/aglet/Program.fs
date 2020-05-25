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

type CliArguments =
    | [<Mandatory>] Auth    of AthenticationMethod * value:string
    | List_Labels           of repo:string
    | Create_Labels         
    | CopyLabels            of from_repo:string * to_repo:string

with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Auth _            -> "How to authenticate at Github. token: raw string of a authentication token. env: name of an environment variable where your token is stored"
            | List_Labels   _   -> "List all labels of the repository. <repo> must be of form 'owner:repoName'"
            | Create_Labels _   -> "Create Labels interactively"
            | CopyLabels    _   -> "copy labels from one repository to another. <from-repo> and <to-repo> must be of form 'owner:repoName'"

let parseRepoName (r:string) =
    let split = r.Split(':')
    if split.Length = 2 then
        split.[0],split.[1]
    else
        failwith "invalid repo address format. It must be of form owner:repoName"

let parseAuth a = 
    match a with
    | (AthenticationMethod.Token,t) -> t
    | (AthenticationMethod.Env,env) ->
        match Auth.tryGetAuthTokenFromEnv env with
        | Some token -> token
        | _ -> failwith "no valid token provided"
    | _ ->  "no valid token provided"


let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)

let parser = ArgumentParser.Create<CliArguments>(programName = "aglet",errorHandler = errorHandler)

let tryGetDefaultString (str: string option) =
    match str with
    | Some s -> s
    | None -> ""

[<EntryPoint>]
let main argv =

    printfn "\r\n"

    let parsedArgs      = parser.ParseCommandLine(argv)
    let authToken       = parsedArgs.PostProcessResult (Auth, parseAuth)
    let listRepo        = parsedArgs.TryPostProcessResult (List_Labels, parseRepoName)
    let copyLabelRepos  = parsedArgs.TryPostProcessResult (CopyLabels, (fun (f,t) -> (parseRepoName f , parseRepoName t)))

    printfn "parsedArgs:\r\n"

    parsedArgs
        .GetAllResults()
        |> List.iter (fun x -> x.ToString() |> Console.WriteLine)

    match listRepo,copyLabelRepos with
    | Some (lOwner,lRepo), None -> 
        let allLabels = 
            GithubApiRequests.createGetLabelRequest lOwner lRepo
            |> GithubApiRequests.setAuth authToken
            |> GithubApiRequests.getResponse
            |> GithubApiRequests.getResponseBody
            |> DTO.Github.LabelInfoResponse.ofJsonArray
            |> Array.map DTO.Github.LabelInfoResponse.toDomain

        allLabels
        |> Seq.iter (fun label ->
            let backgroundColor = label.Color |> Colors.fromHex
            let foregroundColor = Colors.toForegroundTextColor backgroundColor
            Console.WriteLine(
                label.Name
                |> Console.setBackgroundColor backgroundColor
                |> Console.setForegroundColor foregroundColor
            )
        )
    | None, Some ((fromOwner,fromRepo),(toOwner,toRepo)) ->
        Console.WriteLine "not implemented yet."

    | _ -> ()

    printfn "press any key to exit"
    Console.ReadLine() |> ignore
    0 // return an integer exit code