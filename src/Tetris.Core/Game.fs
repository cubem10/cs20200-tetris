namespace Tetris.Core

open System

module Game =
    let private wallKickOffsets =
        [ 0, 0
          0, 1
          0, -1
          0, 2
          0, -2
          -1, 0 ]

    let scoreForClearedLines =
        function
        | 0 -> 0
        | 1 -> 100
        | 2 -> 300
        | 3 -> 500
        | 4 -> 800
        | cleared -> invalidArg (nameof cleared) "A tetromino can clear at most four lines at once."

    let create first next =
        { Board = Board.empty
          Current = Piece.spawn first
          Next = next
          Hold = None
          HoldUsed = false
          Score = 0
          Lines = 0
          Status = Playing
          LockDelay = None
          LockResetCount = 0
          FallAccumulator = TimeSpan.Zero }

    let occupiedCells state =
        state.Board
        |> Map.toList
        |> List.map fst

    let currentCells state = Piece.absoluteCells state.Current

    let private canMoveDown board piece =
        Board.canPlace board { piece with Row = piece.Row + 1 }

    let private resetLockAfterSuccessfulAdjustment state =
        match state.LockDelay with
        | None -> state
        | Some remaining ->
            let stateWithReset =
                if state.LockResetCount < Constants.MaxLockResets then
                    { state with
                        LockDelay = Some Constants.LockDelay
                        LockResetCount = state.LockResetCount + 1 }
                else
                    { state with LockDelay = Some remaining }

            if canMoveDown stateWithReset.Board stateWithReset.Current then
                { stateWithReset with LockDelay = None }
            else
                stateWithReset

    let private enterLockDelayIfResting state =
        if
            state.Status = Playing
            && Option.isNone state.LockDelay
            && not (canMoveDown state.Board state.Current)
        then
            { state with
                LockDelay = Some Constants.LockDelay
                FallAccumulator = TimeSpan.Zero }
        else
            state

    let private tryMoveBy dr dc state =
        if state.Status <> Playing then
            state
        else
            let moved =
                { state.Current with
                    Row = state.Current.Row + dr
                    Col = state.Current.Col + dc }

            if Board.canPlace state.Board moved then
                { state with Current = moved }
                |> resetLockAfterSuccessfulAdjustment
                |> enterLockDelayIfResting
            else
                state

    let moveLeft state = tryMoveBy 0 -1 state

    let moveRight state = tryMoveBy 0 1 state

    let softDrop state =
        let moved = tryMoveBy 1 0 state

        if obj.ReferenceEquals(moved, state) then
            enterLockDelayIfResting state
        else
            moved

    let rotateClockwise state =
        if state.Status <> Playing then
            state
        else
            let rotated =
                { state.Current with
                    Rotation = Tetromino.nextRotation state.Current.Rotation }

            wallKickOffsets
            |> List.tryPick (fun (dr, dc) ->
                let kicked =
                    { rotated with
                        Row = rotated.Row + dr
                        Col = rotated.Col + dc }

                if Board.canPlace state.Board kicked then
                    Some kicked
                else
                    None)
            |> function
                | None -> state
                | Some accepted ->
                    { state with Current = accepted }
                    |> resetLockAfterSuccessfulAdjustment
                    |> enterLockDelayIfResting

    let private dropToBottom (board: Board) (piece: ActivePiece) =
        let rec loop (falling: ActivePiece) =
            let next = { falling with Row = falling.Row + 1 }

            if Board.canPlace board next then
                loop next
            else
                falling

        loop piece

    let private spawnFromNext nextKind =
        Piece.spawn nextKind

    let private finishLock drawNext state =
        let boardWithPiece = Board.lockPiece state.Board state.Current
        let cleared, boardAfterClear = Board.clearCompletedLines boardWithPiece
        let spawned = spawnFromNext state.Next
        let next = drawNext()

        { state with
            Board = boardAfterClear
            Current = spawned
            Next = next
            HoldUsed = false
            Score = state.Score + scoreForClearedLines cleared
            Lines = state.Lines + cleared
            Status =
                if Board.canPlace boardAfterClear spawned then
                    Playing
                else
                    GameOver
            LockDelay = None
            LockResetCount = 0
            FallAccumulator = TimeSpan.Zero }

    let hardDrop drawNext state =
        if state.Status <> Playing then
            state
        else
            { state with Current = dropToBottom state.Board state.Current }
            |> finishLock drawNext

    let hold drawNext state =
        if state.Status <> Playing || state.HoldUsed then
            state
        else
            match state.Hold with
            | None ->
                let spawned = spawnFromNext state.Next
                let next = drawNext()

                { state with
                    Current = spawned
                    Next = next
                    Hold = Some state.Current.Kind
                    HoldUsed = true
                    Status =
                        if Board.canPlace state.Board spawned then
                            Playing
                        else
                            GameOver
                    LockDelay = None
                    LockResetCount = 0
                    FallAccumulator = TimeSpan.Zero }
            | Some held ->
                let spawned = spawnFromNext held

                { state with
                    Current = spawned
                    Hold = Some state.Current.Kind
                    HoldUsed = true
                    Status =
                        if Board.canPlace state.Board spawned then
                            Playing
                        else
                            GameOver
                    LockDelay = None
                    LockResetCount = 0
                    FallAccumulator = TimeSpan.Zero }

    let private stepAutomaticFall state =
        if canMoveDown state.Board state.Current then
            { state with
                Current = { state.Current with Row = state.Current.Row + 1 } }
            |> enterLockDelayIfResting
        else
            enterLockDelayIfResting state

    let advanceTime elapsed drawNext state =
        if state.Status <> Playing || elapsed < TimeSpan.Zero then
            state
        else
            match state.LockDelay with
            | Some remaining ->
                if canMoveDown state.Board state.Current then
                    { state with
                        LockDelay = None
                        FallAccumulator = TimeSpan.Zero }
                else
                    let updatedRemaining = remaining - elapsed

                    if updatedRemaining <= TimeSpan.Zero then
                        finishLock drawNext state
                    else
                        { state with LockDelay = Some updatedRemaining }
            | None ->
                let rec consume accumulated currentState =
                    if accumulated < Constants.FallInterval || Option.isSome currentState.LockDelay then
                        { currentState with FallAccumulator = accumulated }
                    else
                        let fallen = stepAutomaticFall currentState

                        if Option.isSome fallen.LockDelay then
                            fallen
                        else
                            consume (accumulated - Constants.FallInterval) fallen

                consume (state.FallAccumulator + elapsed) state
