namespace Tetris.Tests

module Program =
    [<EntryPoint>]
    let main _ =
        let tests: (string * (unit -> unit)) list =
            [ "Tetromino cells match proposal", Tests.tetrominoCellsMatchProposal
              "Spawn positions are centered", Tests.spawnPositionsAreCentered
              "Movement respects walls and blocks", Tests.movementRespectsWallsAndBlocks
              "Rotations use wall and floor kicks", Tests.rotationsUseWallAndFloorKicks
              "O rotation changes state only", Tests.oPieceRotationChangesStateOnly
              "Hard drop locks and spawns next", Tests.hardDropLocksPieceAndSpawnsNext
              "Line clearing and scoring work", Tests.lineClearingAndScoringWork
              "Hold stores, swaps, and locks once", Tests.holdStoresSwapsAndLocksOncePerPiece
              "Automatic falling and lock delay work", Tests.automaticFallingAndLockDelayWork
              "Long elapsed frame stops at lock delay", Tests.longElapsedFrameStopsAtLockDelay
              "Lock delay resets are limited and can exit", Tests.lockDelayResetsAreLimitedAndCanExitDelay
              "Capped rotation is allowed without resetting delay", Tests.cappedRotationIsAllowedWithoutResettingDelay
              "Capped resting rotation cannot extend delay", Tests.cappedRestingRotationCannotExtendDelay
              "Capped piece gets new delay after landing again", Tests.cappedPieceGetsNewDelayAfterLandingAgain
              "Moving off ledge prevents stale lock expiry", Tests.movingOffLedgePreventsStaleLockExpiry
              "Hard drop during lock delay clears timing state", Tests.hardDropDuringLockDelayClearsTimingState
              "Hold during lock delay clears timing state", Tests.holdDuringLockDelayClearsTimingState
              "Game over occurs when next spawn is blocked", Tests.gameOverOccursWhenNextSpawnIsBlocked
              "Game over ignores queued inputs and elapsed time", Tests.gameOverIgnoresQueuedInputsAndElapsedTime
              "Game over render does not draw failed spawn", Tests.gameOverRenderDoesNotDrawFailedSpawn
              "Random generator produces tetromino kinds", Tests.randomGeneratorProducesTetrominoKinds ]

        let mutable failures = 0

        for name, test in tests do
            try
                test ()
                printfn $"PASS {name}"
            with ex ->
                failures <- failures + 1
                printfn $"FAIL {name}"
                printfn $"  {ex.Message}"

        if failures = 0 then
            printfn $"All {tests.Length} tests passed."
            0
        else
            printfn $"{failures} test(s) failed."
            1
