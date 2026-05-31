namespace Tetris.Core

open System

module PieceGeneration =
    let randomGenerator (random: Random) =
        fun () ->
            let index = random.Next(Tetromino.allKinds.Length)
            Tetromino.allKinds[index]
