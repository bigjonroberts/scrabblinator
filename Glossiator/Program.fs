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
        let get key =
            match ctx.request.formData key with
            | Choice1Of2 x  -> x
            | _             -> ""
        {
            Token       = get "token"
            TeamId      = get "team_id"
            TeamDomain  = get "team_domain"
            ChannelId   = get "channel_id"
            ChannelName = get "channel_name"
            UserId      = get "user_id"
            UserName    = get "user_name"
            Command     = get "command"
            Text        = get "text"
            ResponseUrl = get "response_url"
        }

type ValidationResponse<'T> =
    | Valid     of 'T
    | Invalid   of string

let fallback     nextOption value   = if value = null then nextOption   else value    
let fallbackFunc nextOption value   = if value = null then nextOption() else value
let fallbackToError msg             = fallbackFunc (fun _ -> failwith msg)

let getConfigValue key = 
    Environment.GetEnvironmentVariable key
    |> fallback (ConfigurationManager.AppSettings.[key])
    |> fallbackToError (sprintf "The config setting with the name %s could not be found." key)
    
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

/// See: https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#F.23
let inline min3 one two three = 
    if one < two && one < three then one
    elif two < three then two
    else three

let wagnerFischer (search: string) (term: string) =
    let s = search.ToLower()
    let t = term.ToLower()

    let sLen = s.Length
    let tLen = t.Length
    let d = Array2D.create (sLen + 1) (tLen + 1) 0

    for i = 0 to sLen do d.[i, 0] <- i
    for j = 0 to tLen do d.[0, j] <- j    

    for j = 1 to tLen do
        for i = 1 to sLen do
            if s.[i-1] = t.[j-1] then
                d.[i, j] <- d.[i-1, j-1]
            else
                d.[i, j] <-
                    min3
                        (d.[i-1, j  ] + 1) // a deletion
                        (d.[i  , j-1] + 1) // an insertion
                        (d.[i-1, j-1] + 1) // a substitution
    d.[sLen, tLen]

let calcDistance (searchTerm : string) (entry : GlossaryEntry) =
    wagnerFischer searchTerm entry.Term

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
                path "/" >=> OK "Glossary version 0.1.0-beta" ]
        POST >=>
            choose [
                path "/whatis"  >=> slackCommand whatis
                path "/whatis/" >=> slackCommand whatis ]
        NOT_FOUND "Resource not found. Please note that URLs are case sensitive." ]

let config =
    { defaultConfig with
        bindings = [ HttpBinding.mk HTTP (IPAddress.Parse "0.0.0.0") 8083us ] }

[<EntryPoint>]
let main argv =
    startWebServer config app
    0