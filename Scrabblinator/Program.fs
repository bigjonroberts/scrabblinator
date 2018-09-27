open System
open System.Configuration
open System.Net
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open System.Text.RegularExpressions

// -------------------------------
// CONFIG
// -------------------------------

module Config =
    let fallback     nextOption value   = if value = null then nextOption   else value    
    let fallbackFunc nextOption value   = if value = null then nextOption() else value
    let fallbackToError msg             = fallbackFunc (fun _ -> failwith msg)

    let getValue key = 
        Environment.GetEnvironmentVariable key
        |> fallback (ConfigurationManager.AppSettings.[key])
        |> fallbackToError (sprintf "The config setting with the name %s could not be found." key)

    let getValueOrDefault key default1 = 
        Environment.GetEnvironmentVariable key
        |> fallback (ConfigurationManager.AppSettings.[key])
        |> fallback default1

// -------------------------------
// SCRABBLETEXT DOMAIN
// -------------------------------

module ScrabbleText =

    let prefix = Config.getValueOrDefault "PREFIX" ""    

    let port = Config.getValueOrDefault "PORT" "8083" |> uint16

    type GenMode =
      | Emoji
      | Image

    let genMode =
        match (Config.getValueOrDefault "GENMODE" "Emoji").ToLower() with
        | "image" -> Image
        | _ -> Emoji

    // stolen from http://www.fssnip.net/29/title/Regular-expression-active-pattern
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None

    let asciiChar = 
        seq { 65 .. 90 }
        |> Seq.append (seq { 97 .. 121 })
        |> Seq.map char
        |> Set.ofSeq

    let scrabbleize genMode (text:string) =
    //   let toEmoji (c:char) =
    //     match string c with
    //     //   | Regex @"^\p{L}*$" chr -> List.head chr
    //       | Regex @"^[a-zA-Z]*$" chr -> chr
    //       | _ -> [ "blank" ]
    //     |> List.tryHead
    //     |> function | Some a -> a | None -> "blank"
    //     |> sprintf ":%s%s:" prefix
      let toEmoji (c:char) = 
        match Set.contains c asciiChar with
          | true -> sprintf ":%s%c:" prefix c
          | false -> sprintf ":%sblank:" prefix
      match genMode with
        | Emoji -> 
            let emojiText = text |> Seq.map toEmoji 
            let emojiList = Seq.toList emojiText
            String.concat " " emojiList
        | Image -> "not implemented yet"

// -------------------------------
// SLACK INTEGRATION
// -------------------------------

module SlackIntegration =

    let tokens = 
        Config.getValue "TOKEN"
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

open ScrabbleText
open SlackIntegration

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