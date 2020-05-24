module HttpUtils 

open Hopac
open HttpFs
open HttpFs.Client
open Auth

module internal Request =
           
    let queryStringItems (qItems: (string*string) list) (r:Request) =
        let rec loop (qItems: (string*string) list) (r:Request) =
            match qItems with
            | []         -> r
            | (name,value)::tail   -> 
                loop tail (r |> Request.queryStringItem name value)
        loop qItems r

module GithubApiRequests =
    
    let setAuth (authToken:string) (r:Request) =
        r
        |> Request.setHeader(
            Authorization authToken
        )

    let createGetLabelRequest owner repo =
        Request.createUrl 
            Get
            (sprintf "https://api.github.com/repos/%s/%s/labels" owner repo)
        |> Request.setHeader (
            UserAgent "aglet")
    
    let getResponse request = 
        job {
            let! response = getResponse request // disposed at the end of async, don't
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
    
    let getResponseBody (res:Response) =
        res
        |> Response.readBodyAsString
        |> run