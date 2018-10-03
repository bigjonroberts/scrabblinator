namespace Scrabblinator

open System
open Xunit

module Tests =


    [<Fact>]
    let ``Check Emoji Characters Inside Conversion Set Boundaries`` () =
        let checkInBoundary c = 
            Assert.Equal(sprintf ":%c:" c,ScrabbleText.toEmoji c)
        checkInBoundary 'a'
        checkInBoundary 'b'
        checkInBoundary 'z'
        checkInBoundary 'y'
        checkInBoundary 'A'
        checkInBoundary 'B'
        checkInBoundary 'Z'
        checkInBoundary 'Y'

    [<Fact>]
    let ``Check Emoji Characters Outside Conversion Set Boundaries`` () =
        let checkOutBoundary c = 
            Assert.Equal(":blank:",ScrabbleText.toEmoji c)
        let moveChar m = int >> (+) m >> char
        'a' |> moveChar -1 |> checkOutBoundary
        'z' |> moveChar 1 |> checkOutBoundary
        'A' |> moveChar -1 |> checkOutBoundary
        'Z' |> moveChar 1 |> checkOutBoundary
