// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
module App

open System
open Argu

type CliArguments =
    | Soos of saas:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Soos _ -> "MEEEEEEEEEEEEEEm"


let parser = ArgumentParser.Create<CliArguments>(programName = "dotnet-aglet")


[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        printfn "%s" (parser.PrintUsage(message ="no arguments provided."))
    else
        let parsedArgs = parser.ParseCommandLine(inputs=argv)
        printfn "parsedArgs:"
        parsedArgs.GetAllResults()
        |> List.iter (fun x -> printfn "%s" (x.ToString()))

    printfn "press any key to exit"
    Console.ReadLine() |> ignore
    0 // return an integer exit code