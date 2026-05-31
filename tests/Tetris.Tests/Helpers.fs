namespace Tetris.Tests

open System.Collections.Generic
open Tetris.Core

module Helpers =
    let p row col = Position.create row col

    let board positions kind =
        positions |> List.fold (fun board position -> Map.add position kind board) Board.empty

    let provider (pieces: PieceKind list) =
        let queue = Queue<PieceKind>(Seq.ofList pieces)

        fun () ->
            if queue.Count = 0 then
                I
            else
                queue.Dequeue()

    let cellSet cells = Set.ofList cells

    let ghostCells state =
        let rec drop (piece: ActivePiece) =
            let next = { piece with Row = piece.Row + 1 }

            if Board.canPlace state.Board next then
                drop next
            else
                piece

        drop state.Current |> Piece.absoluteCells |> cellSet
