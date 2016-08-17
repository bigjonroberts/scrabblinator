open System
open System.Configuration
open System.Collections.Generic
open System.Net
open System.Text
open Microsoft.FSharp.Core.Option
open FSharp.Data
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors

type GlossaryEntry =
    {
        Term        : string
        Meaning     : string
        Description : string
    }

type SlackRequest =
    {
        Token       : string
        TeamId      : string
        TeamDomain  : string
        ChannelId   : string
        ChannelName : string
        UserId      : string
        UserName    : string
        Command     : string
        Text        : string
        ResponseUrl : string
    }
    static member FromHttpContext (ctx : HttpContext) =
        let splitByLineBreaks = fun (s : string) -> s.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
        let postData =
            ctx.request.rawForm
            |> Encoding.UTF8.GetString
            |> splitByLineBreaks
            |> Array.map (fun s -> 
                let parts = s.Split [| '=' |]
                parts.[0], parts.[1])
            |> Map.ofArray

        {
            Token       = postData.["token"]
            TeamId      = postData.["team_id"]
            TeamDomain  = postData.["team_domain"]
            ChannelId   = postData.["channel_id"]
            ChannelName = postData.["channel_name"]
            UserId      = postData.["user_id"]
            UserName    = postData.["user_name"]
            Command     = postData.["command"]
            Text        = postData.["text"]
            ResponseUrl = postData.["response_url"]
        }

type ValidationResponse =
    | Valid     of SlackRequest
    | Invalid   of string

let fallback     nextOption value   = if value = null then nextOption   else value    
let fallbackFunc nextOption value   = if value = null then nextOption() else value
let fallbackToError msg = fallbackFunc (fun _ -> failwith msg)

let getConfigValue key = 
    Environment.GetEnvironmentVariable key
    |> fallback (ConfigurationManager.AppSettings.[key])
    |> fallbackToError (sprintf "A config setting with the name %s could not be found." key)
    
let csvRowToGlossaryEntry (row : CsvRow) =
    { 
        Term        = row.GetColumn "Term"; 
        Meaning     = row.GetColumn "Meaning"; 
        Description = row.GetColumn "Description"
    }

let glossary =
    getConfigValue "URL_OR_PATH_TO_CSV"
    |> CsvFile.Load
    |> fun x -> x.Rows
    |> Seq.map csvRowToGlossaryEntry

let token = getConfigValue "TOKEN"

let validateRequest (slackRequest : SlackRequest) =
    if slackRequest.Token = token
    then Valid slackRequest
    else Invalid "Invalid token in request. Your Slack team is not permitted to use this service."

/// See: https://en.wikipedia.org/wiki/Levenshtein_distance#Computing_Levenshtein_distance
/// Inefficient, but good enough for prototype
let rec levenshteinDistance (x     : string)
                            (lenX  : int)
                            (y     : string)
                            (lenY  : int) =
    let min3 x y z = min x (min y z)

    if      lenX = 0 then lenY
    else if lenY = 0 then lenX
    else
        let cost = if x.[lenX - 1] = y.[lenY - 1] then 0 else 1
        min3
            ((levenshteinDistance x (lenX - 1) y  lenY     ) |> (+) 1)            
            ((levenshteinDistance x  lenX      y (lenY - 1)) |> (+) 1)
            ((levenshteinDistance x (lenX - 1) y (lenY - 1)) |> (+) cost)

let calcDistance (searchTerm : string) (entry : GlossaryEntry) =
    let x = searchTerm.ToLower()
    let y = entry.Term.ToLower()
    levenshteinDistance x x.Length y y.Length

let findMatch (searchTerm : string) =
    glossary
    |> Seq.minBy (calcDistance searchTerm)

let format entry = sprintf "Term: %s\r\nMeaning: %s\r\nDescription: %s" entry.Term entry.Meaning entry.Description                

let whatis (req : SlackRequest) =
    findMatch req.Text |> format

let slackCommand (f : SlackRequest -> string) =
    fun (ctx : HttpContext) ->
        (match SlackRequest.FromHttpContext ctx |> validateRequest with
        | Invalid msg -> BAD_REQUEST msg
        | Valid   req -> f req |> OK) ctx

let app = 
    choose [ 
        GET >=> 
            choose [ 
                path "/" >=> OK "Glossary version 0.1.0-Alpha" ]
        POST >=>
            choose [
                path "/whatis" >=> slackCommand whatis ]
        NOT_FOUND "Resource not found.  Please note that URLs are case sensitive." ]

let config =
    { defaultConfig with
        bindings = [ HttpBinding.mk HTTP (IPAddress.Parse "0.0.0.0") 8083us ] }

[<EntryPoint>]
let main argv =
    startWebServer config app
    0