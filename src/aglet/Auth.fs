module Auth

open System

let private tryGetEnv (envVar: string) =
    match Environment.GetEnvironmentVariable(envVar) with
    | null-> None
    | (str:string) -> Some str
    | _ -> None

let tryGetAuthTokenFromEnv (authTokenName: string) =
    tryGetEnv authTokenName
