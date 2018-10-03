open System.Net
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Scrabblinator.ScrabbleText
open Scrabblinator


[<AutoOpen>]
module SlackIntegration =
    let tokens = 
        Config.getValue "TOKENS"
        |> (fun s -> s.Split(';'))
        |> Set.ofArray

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
            // printfn "%A" ctx.request
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

    let validateRequest (slackRequest : SlackRequest) =
        printfn "slack request: %A" slackRequest
        match tokens.Contains slackRequest.Token with
          | true -> Valid slackRequest
          | false -> Invalid "Invalid token in request. Your Slack team is not permitted to use this service."        

    let SLACK_RESPONSE (text : string) =
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
        |> sprintf "{ \"response_type\": \"in_channel\", \"text\": \"%s\"}"
        |> OK
        >=> Writers.setMimeType "application/json"

    let slackCommand (f : SlackRequest -> string) =
        fun (ctx : HttpContext) ->
            //printfn "context:%A" ctx.request.rawForm
            (match SlackRequest.FromHttpContext ctx |> validateRequest with
            | Invalid msg -> BAD_REQUEST msg
            | Valid   req -> f req |> SLACK_RESPONSE) ctx

// -------------------------------
// WEB SERVICE
// -------------------------------

let scrabble (req : SlackRequest) = scrabbleize genMode req.Text

let testit = scrabbleize genMode "damnit"

let app = 
    choose [ 
        GET  >=> OK "Scrabblinator version 0.1.0-beta"
        POST >=>
            choose [
                path "/scrabble"  >=> slackCommand scrabble
                path "/scrabble/" >=> slackCommand scrabble ]
        NOT_FOUND "Resource not found. Please note that URLs are case sensitive." ]

let config =
    { defaultConfig with
        bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") port ] }

[<EntryPoint>]
let main argv =
    startWebServer config app
    0