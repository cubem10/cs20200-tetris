namespace Tetris.Core

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
