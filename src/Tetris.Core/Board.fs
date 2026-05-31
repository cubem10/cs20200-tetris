namespace Tetris.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
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
