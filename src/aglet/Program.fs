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



type CliArguments =
    | AuthToken     of token:string
    | AuthTokenEnv  of tokenEnvironmentVariable: string
    | List_Labels   of repoOwner:string * repoName: string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | AuthToken _       -> "Github authentication token to use for Requests to the github api. Here is how you can reate one: https://help.github.com/en/github/authenticating-to-github/creating-a-personal-access-token-for-the-command-line:"
            | AuthTokenEnv _    -> "the name of an environment variable that holds a github auth token"
            | List_Labels       -> "List"

let parser = ArgumentParser.Create<CliArguments>(programName = "dotnet-aglet")

let tryGetDefaultString (str: string option) =
    match str with
    | Some s -> s
    | None -> ""



[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        printfn "%s" (parser.PrintUsage(message ="no arguments provided."))
    else
        let parsedArgs = parser.ParseCommandLine(inputs=argv)

        let authToken =
            match parsedArgs.TryGetResult AuthToken with
            | Some token -> token
            | _ ->  match parsedArgs.TryGetResult AuthTokenEnv with
                    | None     -> failwith "no valid token provided"
                    | Some str -> 
                        match Auth.tryGetAuthTokenFromEnv str with
                        | Some token -> token
                        | _ -> failwith "no valid token provided"

        if (parsedArgs.Contains List_Labels) then
            let owner,repo = (parsedArgs.TryGetResult List_Labels).Value
            let allLabels = 
                GithubApiRequests.createGetLabelRequest owner repo
                |> GithubApiRequests.setAuth authToken
                |> GithubApiRequests.getResponse
                |> GithubApiRequests.getResponseBody
                |> DTO.Github.LabelInfoResponse.ofJsonArray
                |> Array.map DTO.Github.LabelInfoResponse.toDomain

            allLabels
            |> Seq.iter (fun label ->
                let backgroundColor = label.Color |> Colors.fromHex
                let foregroundColor = Colors.toForegroundTextColor backgroundColor
                Console.WriteLine(label.Name.Pastel((foregroundColor |> Colors.toHex false)).PastelBg((backgroundColor |> Colors.toHex false)))
            )

    printfn "press any key to exit"
    Console.ReadLine() |> ignore
    0 // return an integer exit code