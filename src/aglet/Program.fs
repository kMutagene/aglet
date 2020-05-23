// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
module App

open System
open Argu

type CliArguments =
    | Working_Directory of path:string
    | Listener of host:string * port:int
    | Data of base64:byte[]
    | Port of tcp_port:int
    | Log_Level of level:int
    | Detach
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Working_Directory _ -> "specify a working directory."
            | Listener _ -> "specify a listener (hostname : port)."
            | Data _ -> "binary data in base64 encoding."
            | Port _ -> "specify a primary port."
            | Log_Level _ -> "set the log level."
            | Detach _ -> "detach daemon from console."


let parser = ArgumentParser.Create<CliArguments>(programName = "dotnet aglet")

// Define a function to construct a message to print
let from whom =
    sprintf "from %s" whom

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