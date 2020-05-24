#I @"../../../packages/Hopac/lib/netstandard2.0/"
#r "Hopac.dll"
#r "Hopac.Core.dll"
#r @"../../../packages/Http.fs/lib/netstandard2.0/Httpfs.dll"


open Hopac
open HttpFs
open HttpFs.Client


module Request =
       
    let queryStringItems (qItems: (string*string) list) (r:Request) =
        let rec loop (qItems: (string*string) list) (r:Request) =
            match qItems with
            | []         -> r
            | (name,value)::tail   -> 
                loop tail (r |> Request.queryStringItem name value)
        loop qItems r

open System

let tryGeEnv (envVar: string) =
    match Environment.GetEnvironmentVariable(envVar) with
    | null-> None
    | (str:string) -> Some str
    | _ -> None

let token = 
    match tryGeEnv "GithubDevToken" with
    | Some token    -> token
    | _             -> "" 

let testQuery = 
    Request.createUrl 
        Get
        "https://api.github.com/repos/kMutagene/CSBar/labels"
    |> Request.setHeader (
        UserAgent "aglet")
    |> Request.setHeader(
        Authorization token
    )


let response = 
    job {
        let! response = getResponse testQuery // disposed at the end of async, don't
                                            // fetch outside async body
        // the above doesn't download the response, so you'll have to do that:
        //let! bodyStr = Response.readBodyAsString response
        // OR:
        //let! bodyBs = Response.readBodyAsBytes

        // remember HttpFs doesn't buffer the stream (how would we know if we're
        // downloading 3GiB?), so once you use one of the above methods, you can't do it
        // again, but have to buffer/stash it yourself somewhere.
        return response
    }
    |> run

response
|> Response.readBodyAsString
|> run