module DTO.Github 
    
open Newtonsoft.Json
open Domain

type LabelInfoResponse = {
    [<JsonProperty("id")>]         
    ID: int
    [<JsonProperty("node_id")>]     
    NodeId : string
    [<JsonProperty("url")>]        
    Url: string
    [<JsonProperty("name")>]        
    Name: string
    [<JsonProperty("description")>] 
    Description: string
    [<JsonProperty("color")>]       
    Color: string
    [<JsonProperty("default")>]    
    IsDefault: bool
}
with 
    static member ofJson (json:string) =  
        json 
        |> JsonConvert.DeserializeObject<LabelInfoResponse>

    static member ofJsonArray (json:string) =  
        json 
        |> JsonConvert.DeserializeObject<LabelInfoResponse []>

    static member toDomain (dto: LabelInfoResponse) : Domain.IssueLabel = {
        Name = dto.Name
        Color = dto.Color
        Description = dto.Description  
    }
            

        