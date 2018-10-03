namespace Scrabblinator

open System.Text.RegularExpressions

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

    let scrabbleize genMode (text:string) =
      match genMode with
        | Emoji -> 
            let emojiText = text |> Seq.map toEmoji 
            let emojiList = Seq.toList emojiText
            String.concat " " emojiList
        | Image -> "not implemented yet"

