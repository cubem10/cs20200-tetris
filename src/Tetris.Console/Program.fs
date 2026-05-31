namespace Tetris.Console

open System
open System.Text
open System.Threading
open Tetris.Core

module Program =
    let private terminalSize () =
        try
            Some(Console.WindowWidth, Console.WindowHeight)
        with _ ->
            None

    let private terminalIsLargeEnough () =
        match terminalSize () with
        | Some(width, height) ->
            width >= Constants.MinTerminalColumns
            && height >= Constants.MinTerminalRows
        | None -> true

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
            Console.WriteLine($"Need {Constants.MinTerminalColumns}x{Constants.MinTerminalRows} terminal.")
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
            let mutable lastTerminalSize = terminalSize ()

            Console.CursorVisible <- false
            Console.Write("\u001b[2J")

            try
                while running do
                    let currentTerminalSize = terminalSize ()

                    if currentTerminalSize <> lastTerminalSize then
                        Console.Write("\u001b[2J\u001b[H")
                        lastRender <- ""
                        lastTerminalSize <- currentTerminalSize

                    if not (terminalIsLargeEnough ()) then
                        let message = Renderer.renderSmallTerminal ()

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
