namespace Tetris.Console

open System.Text
open Tetris.Core

module Renderer =
    let private reset = "\u001b[0m"
    let private sideGap = "  "

    let private color =
        function
        | I -> "\u001b[36m"
        | O -> "\u001b[33m"
        | T -> "\u001b[35m"
        | J -> "\u001b[34m"
        | L -> "\u001b[37m"
        | S -> "\u001b[32m"
        | Z -> "\u001b[31m"

    let private block kind = $"{color kind}██{reset}"

    let private ghostBlock kind = $"{color kind}\u001b[2m░░{reset}"

    let private empty = "  "

    let private appendClearedLine (builder: StringBuilder) (line: string) =
        builder.Append(line).Append("\u001b[K\r\n") |> ignore

    let private appendClearedText (builder: StringBuilder) (line: string) =
        builder.Append(line).Append("\u001b[K") |> ignore

    let private shapePreview kind =
        let size = Tetromino.boxSize kind
        let cells = Tetromino.cells kind State0 |> Set.ofList

        [ for row in 0 .. size - 1 do
              let line =
                  [ for col in 0 .. size - 1 ->
                        if Set.contains (Position.create row col) cells then
                            block kind
                        else
                            empty ]
                  |> String.concat ""

              line.PadRight(8) ]

    let private pieceOverlay piece =
        piece
        |> Piece.absoluteCells
        |> List.map (fun position -> position, piece.Kind)
        |> Map.ofList

    let private ghostPiece state =
        let rec drop (piece: ActivePiece) =
            let next = { piece with Row = piece.Row + 1 }

            if Board.canPlace state.Board next then
                drop next
            else
                piece

        drop state.Current

    let private ghostPieceOverlay state =
        if state.Status <> Playing then
            Map.empty
        else
            let ghost = ghostPiece state

            if ghost.Row = state.Current.Row then
                Map.empty
            else
                pieceOverlay ghost

    let private cellText state currentOverlay ghostOverlay row col =
        let position = Position.create row col

        match Map.tryFind position currentOverlay with
        | Some kind -> block kind
        | None ->
            match Board.cellAt row col state.Board with
            | Some kind -> block kind
            | None ->
                match Map.tryFind position ghostOverlay with
                | Some kind -> ghostBlock kind
                | None -> empty

    let private infoLines state =
        let nextPreview = shapePreview state.Next

        let holdPreview =
            match state.Hold with
            | Some held -> shapePreview held
            | None -> [ "(empty)" ]

        let baseLines =
            [ "NEXT:"
              yield! nextPreview
              ""
              "HOLD:"
              yield! holdPreview
              ""
              $"SCORE: {state.Score}"
              $"LINES: {state.Lines}"
              ""
              "[←→↓↑ SPACE C Q]" ]

        if state.Status = GameOver then
            baseLines
            @ [ ""
                "GAME OVER"
                $"Final Score: {state.Score}"
                $"Final Lines: {state.Lines}" ]
        else
            baseLines

    let render state =
        let info = infoLines state
        let currentOverlay = pieceOverlay state.Current
        let ghostOverlay = ghostPieceOverlay state
        let builder = StringBuilder()

        builder.Append("\u001b[H") |> ignore
        appendClearedLine builder ("+--------------------+" + sideGap + (List.tryItem 0 info |> Option.defaultValue ""))

        for row in 0 .. Constants.Height - 1 do
            let field =
                [ for col in 0 .. Constants.Width - 1 -> cellText state currentOverlay ghostOverlay row col ]
                |> String.concat ""

            let side = List.tryItem (row + 1) info |> Option.defaultValue ""
            appendClearedLine builder $"|{field}|{sideGap}{side}"

        appendClearedLine builder "+--------------------+"

        if state.Status = GameOver then
            appendClearedLine builder "Press R to restart or Q to quit."

        builder.Append("\u001b[J") |> ignore

        builder.ToString()

    let renderSmallTerminal () =
        let builder = StringBuilder()

        builder.Append("\u001b[H") |> ignore
        appendClearedText builder $"Resize to {Constants.MinTerminalColumns}x{Constants.MinTerminalRows}. Q quits."
        builder.ToString()
