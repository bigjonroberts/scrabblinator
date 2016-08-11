open System
open System.Configuration
open System.Collections.Generic
open System.Net
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

let fallback     nextOption value   = if value = null then nextOption   else value    
let fallbackFunc nextOption value   = if value = null then nextOption() else value

let getConfigValue key = 
    Environment.GetEnvironmentVariable key
    |> fallback (ConfigurationManager.AppSettings.[key])
    |> fallbackFunc (fun _ -> failwith (sprintf "A config setting with the name %s could not be found." key))
    
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

let print entry = sprintf "Term: %s\r\nMeaning: %s\r\nDescription: %s" entry.Term entry.Meaning entry.Description

let whatis =
    fun (ctx : HttpContext) ->
        (match ctx.request.queryParam "q" with
        | Choice1Of2 term   -> findMatch term |> print |> OK
        | _                 -> BAD_REQUEST "No search term has been submitted.") ctx                

let app = 
    choose [ 
        GET >=> 
            choose [ 
                path "/"        >=> OK "Glossary version 0.1.0-Alpha"
                path "/whatis"  >=> whatis ]
        NOT_FOUND "Resource not found.  Please note that URLs are case sensitive." ]

let config =
    { defaultConfig with
        bindings = [ HttpBinding.mk HTTP (IPAddress.Parse "0.0.0.0") 8083us ] }

[<EntryPoint>]
let main argv =
    startWebServer config app
    0