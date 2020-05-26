#I @"../../../packages/Hopac/lib/netstandard2.0/"
#r "Hopac.dll"
#r "Hopac.Core.dll"
#r @"../../../packages/Http.fs/lib/netstandard2.0/Httpfs.dll"
#r @"../../../packages/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"


open Hopac
open HttpFs
open HttpFs.Client


#load "../Domain.fs"
#load "../DTO/GithubDTO.fs"
#load "../Auth.fs"
#load "../HttpUtils.fs"

open Domain
open DTO
open Auth
open HttpUtils

let authToken = "GithubDevToken" |> Auth.tryGetAuthTokenFromEnv

let req = 
    {
        Name        = "test2"
        Color       = "#000000"
        Description ="Test2 from aglet"
    }
    |> DTO.Github.LabelPostRequest.fromDomain
    |> GithubApiRequests.createPostLabelRequest "kMutagene" "aglet"
    |> GithubApiRequests.setAuth authToken.Value

let res = req |> GithubApiRequests.getResponse