namespace Scrabblinator

open System
open System.Configuration

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
