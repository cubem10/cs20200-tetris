namespace Tetris.Core

open System

type PieceKind =
    | I
    | O
    | T
    | J
    | L
    | S
    | Z

type Rotation =
    | State0
    | StateR
    | State2
    | StateL

[<Struct; CustomComparison; CustomEquality>]
type Position =
    { Row: int
      Col: int }

    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? Position as otherPosition ->
                compare (this.Row, this.Col) (otherPosition.Row, otherPosition.Col)
            | _ -> invalidArg (nameof other) "Cannot compare Position with another type."

    override this.Equals other =
        match other with
        | :? Position as otherPosition -> this.Row = otherPosition.Row && this.Col = otherPosition.Col
        | _ -> false

    override this.GetHashCode() = HashCode.Combine(this.Row, this.Col)

type Board = Map<Position, PieceKind>

type ActivePiece =
    { Kind: PieceKind
      Rotation: Rotation
      Row: int
      Col: int }

type GameStatus =
    | Playing
    | GameOver

type GameState =
    { Board: Board
      Current: ActivePiece
      Next: PieceKind
      Hold: PieceKind option
      HoldUsed: bool
      Score: int
      Lines: int
      Status: GameStatus
      LockDelay: TimeSpan option
      LockResetCount: int
      FallAccumulator: TimeSpan }

module Constants =
    [<Literal>]
    let Width = 10

    [<Literal>]
    let Height = 20

    [<Literal>]
    let MinTerminalColumns = 40

    [<Literal>]
    let MinTerminalRows = 24

    let FallInterval = TimeSpan.FromSeconds 0.75
    let LockDelay = TimeSpan.FromSeconds 0.5
    let MaxLockResets = 15

module Position =
    let create row col = { Row = row; Col = col }

module Tetromino =
    let allKinds = [ I; O; T; J; L; S; Z ]

    let private p row col = Position.create row col

    let boxSize =
        function
        | I -> 4
        | O -> 2
        | T
        | J
        | L
        | S
        | Z -> 3

    let nextRotation =
        function
        | State0 -> StateR
        | StateR -> State2
        | State2 -> StateL
        | StateL -> State0

    let cells kind rotation =
        match kind, rotation with
        | I, State0 -> [ p 1 0; p 1 1; p 1 2; p 1 3 ]
        | I, StateR -> [ p 0 2; p 1 2; p 2 2; p 3 2 ]
        | I, State2 -> [ p 2 0; p 2 1; p 2 2; p 2 3 ]
        | I, StateL -> [ p 0 1; p 1 1; p 2 1; p 3 1 ]

        | O, _ -> [ p 0 0; p 0 1; p 1 0; p 1 1 ]

        | T, State0 -> [ p 0 1; p 1 0; p 1 1; p 1 2 ]
        | T, StateR -> [ p 0 1; p 1 1; p 1 2; p 2 1 ]
        | T, State2 -> [ p 1 0; p 1 1; p 1 2; p 2 1 ]
        | T, StateL -> [ p 0 1; p 1 0; p 1 1; p 2 1 ]

        | J, State0 -> [ p 0 0; p 1 0; p 1 1; p 1 2 ]
        | J, StateR -> [ p 0 1; p 0 2; p 1 1; p 2 1 ]
        | J, State2 -> [ p 1 0; p 1 1; p 1 2; p 2 2 ]
        | J, StateL -> [ p 0 1; p 1 1; p 2 0; p 2 1 ]

        | L, State0 -> [ p 0 2; p 1 0; p 1 1; p 1 2 ]
        | L, StateR -> [ p 0 1; p 1 1; p 2 1; p 2 2 ]
        | L, State2 -> [ p 1 0; p 1 1; p 1 2; p 2 0 ]
        | L, StateL -> [ p 0 0; p 0 1; p 1 1; p 2 1 ]

        | S, State0 -> [ p 0 1; p 0 2; p 1 0; p 1 1 ]
        | S, StateR -> [ p 0 1; p 1 1; p 1 2; p 2 2 ]
        | S, State2 -> [ p 1 1; p 1 2; p 2 0; p 2 1 ]
        | S, StateL -> [ p 0 0; p 1 0; p 1 1; p 2 1 ]

        | Z, State0 -> [ p 0 0; p 0 1; p 1 1; p 1 2 ]
        | Z, StateR -> [ p 0 2; p 1 1; p 1 2; p 2 1 ]
        | Z, State2 -> [ p 1 0; p 1 1; p 2 1; p 2 2 ]
        | Z, StateL -> [ p 0 1; p 1 0; p 1 1; p 2 0 ]

module Piece =
    let spawnColumn =
        function
        | O -> 4
        | I
        | T
        | J
        | L
        | S
        | Z -> 3

    let spawn kind =
        { Kind = kind
          Rotation = State0
          Row = 0
          Col = spawnColumn kind }

    let absoluteCells piece =
        Tetromino.cells piece.Kind piece.Rotation
        |> List.map (fun cell -> Position.create (piece.Row + cell.Row) (piece.Col + cell.Col))

module Board =
    let empty: Board = Map.empty

    let isInside (position: Position) =
        position.Row >= 0
        && position.Row < Constants.Height
        && position.Col >= 0
        && position.Col < Constants.Width

    let cellAt row col (board: Board) = Map.tryFind (Position.create row col) board

    let isOccupied (position: Position) (board: Board) = Map.containsKey position board

    let canPlace (board: Board) (piece: ActivePiece) =
        Piece.absoluteCells piece
        |> List.forall (fun position -> isInside position && not (isOccupied position board))

    let lockPiece (board: Board) (piece: ActivePiece) =
        Piece.absoluteCells piece
        |> List.fold (fun locked position -> Map.add position piece.Kind locked) board

    let fullRows (board: Board) =
        [ 0 .. Constants.Height - 1 ]
        |> List.filter (fun row ->
            [ 0 .. Constants.Width - 1 ]
            |> List.forall (fun col -> isOccupied (Position.create row col) board))

    let clearCompletedLines (board: Board) =
        let rowsToClear = fullRows board
        let rowSet = Set.ofList rowsToClear

        let shiftFor row =
            rowsToClear |> List.filter (fun clearedRow -> clearedRow > row) |> List.length

        let shifted =
            board
            |> Map.toList
            |> List.choose (fun (position, kind) ->
                if Set.contains position.Row rowSet then
                    None
                else
                    Some(Position.create (position.Row + shiftFor position.Row) position.Col, kind))
            |> Map.ofList

        rowsToClear.Length, shifted

module PieceGeneration =
    let randomGenerator (random: Random) =
        fun () ->
            let index = random.Next(Tetromino.allKinds.Length)
            Tetromino.allKinds[index]

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

                if Board.canPlace state.Board kicked then Some kicked else None)
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
        if state.Status <> Playing || elapsed <= TimeSpan.Zero then
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
