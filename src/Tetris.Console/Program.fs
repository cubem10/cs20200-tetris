namespace Tetris.Console

open System
open System.Text
open System.Threading
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
        builder.Append(line).Append("\u001b[K").AppendLine() |> ignore

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
        "\u001b[HTerminal is too small. Please resize to at least 40x24.\u001b[K\nPress Q to quit.\u001b[K\u001b[J"

module Program =
    let private terminalIsLargeEnough () =
        try
            Console.WindowWidth >= Constants.MinTerminalColumns
            && Console.WindowHeight >= Constants.MinTerminalRows
        with _ ->
            true

    let private keyToAction (key: ConsoleKeyInfo) =
        match key.Key with
        | ConsoleKey.LeftArrow -> Some "left"
        | ConsoleKey.RightArrow -> Some "right"
        | ConsoleKey.DownArrow -> Some "down"
        | ConsoleKey.UpArrow -> Some "rotate"
        | ConsoleKey.Spacebar -> Some "hardDrop"
        | ConsoleKey.C -> Some "hold"
        | ConsoleKey.R -> Some "restart"
        | ConsoleKey.Q -> Some "quit"
        | _ -> None

    let private applyGameplayInput drawNext action state =
        match action with
        | "left" -> Game.moveLeft state
        | "right" -> Game.moveRight state
        | "down" -> Game.softDrop state
        | "rotate" -> Game.rotateClockwise state
        | "hardDrop" -> Game.hardDrop drawNext state
        | "hold" -> Game.hold drawNext state
        | _ -> state

    [<EntryPoint>]
    let main _ =
        if not (terminalIsLargeEnough ()) then
            Console.WriteLine("Terminal must be at least 40 columns wide and 24 rows tall.")
            1
        else
            Console.OutputEncoding <- Encoding.UTF8

            let random = Random()
            let drawNext = PieceGeneration.randomGenerator random
            let mutable state = Game.create (drawNext()) (drawNext())
            let mutable running = true
            let mutable quitRequested = false
            let mutable lastRender = ""
            let mutable lastTick = DateTime.UtcNow

            Console.CursorVisible <- false
            Console.Write("\u001b[2J")

            try
                while running do
                    if not (terminalIsLargeEnough ()) then
                        let message = Renderer.renderSmallTerminal ()

                        if message <> lastRender then
                            Console.Write(message)
                            lastRender <- message

                        while Console.KeyAvailable do
                            let key = Console.ReadKey(true)

                            if key.Key = ConsoleKey.Q then
                                quitRequested <- true
                                running <- false

                        lastTick <- DateTime.UtcNow
                        Thread.Sleep 50
                    else
                        let mutable restartRequested = false

                        while Console.KeyAvailable do
                            let key = Console.ReadKey(true)

                            match keyToAction key with
                            | Some "quit" ->
                                quitRequested <- true
                                running <- false
                            | Some "restart" when state.Status = GameOver -> restartRequested <- true
                            | Some action when state.Status = Playing ->
                                state <- applyGameplayInput drawNext action state
                            | _ -> ()

                        if restartRequested then
                            state <- Game.create (drawNext()) (drawNext())

                        let now = DateTime.UtcNow
                        let elapsed = now - lastTick
                        lastTick <- now

                        state <- Game.advanceTime elapsed drawNext state

                        let frame = Renderer.render state

                        if frame <> lastRender then
                            Console.Write(frame)
                            lastRender <- frame

                        Thread.Sleep 25

                0
            finally
                Console.CursorVisible <- true
                if quitRequested then
                    Console.Write("\u001b[0m\u001b[2J\u001b[H")
                else
                    Console.Write("\u001b[0m")
