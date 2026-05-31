namespace Tetris.Tests

open System
open Tetris.Core

module Tests =
    open Helpers

    let tetrominoCellsMatchProposal () =
        let expected =
            [ (I, State0), [ p 1 0; p 1 1; p 1 2; p 1 3 ]
              (I, StateR), [ p 0 2; p 1 2; p 2 2; p 3 2 ]
              (I, State2), [ p 2 0; p 2 1; p 2 2; p 2 3 ]
              (I, StateL), [ p 0 1; p 1 1; p 2 1; p 3 1 ]
              (O, State0), [ p 0 0; p 0 1; p 1 0; p 1 1 ]
              (O, StateR), [ p 0 0; p 0 1; p 1 0; p 1 1 ]
              (O, State2), [ p 0 0; p 0 1; p 1 0; p 1 1 ]
              (O, StateL), [ p 0 0; p 0 1; p 1 0; p 1 1 ]
              (T, State0), [ p 0 1; p 1 0; p 1 1; p 1 2 ]
              (T, StateR), [ p 0 1; p 1 1; p 1 2; p 2 1 ]
              (T, State2), [ p 1 0; p 1 1; p 1 2; p 2 1 ]
              (T, StateL), [ p 0 1; p 1 0; p 1 1; p 2 1 ]
              (J, State0), [ p 0 0; p 1 0; p 1 1; p 1 2 ]
              (J, StateR), [ p 0 1; p 0 2; p 1 1; p 2 1 ]
              (J, State2), [ p 1 0; p 1 1; p 1 2; p 2 2 ]
              (J, StateL), [ p 0 1; p 1 1; p 2 0; p 2 1 ]
              (L, State0), [ p 0 2; p 1 0; p 1 1; p 1 2 ]
              (L, StateR), [ p 0 1; p 1 1; p 2 1; p 2 2 ]
              (L, State2), [ p 1 0; p 1 1; p 1 2; p 2 0 ]
              (L, StateL), [ p 0 0; p 0 1; p 1 1; p 2 1 ]
              (S, State0), [ p 0 1; p 0 2; p 1 0; p 1 1 ]
              (S, StateR), [ p 0 1; p 1 1; p 1 2; p 2 2 ]
              (S, State2), [ p 1 1; p 1 2; p 2 0; p 2 1 ]
              (S, StateL), [ p 0 0; p 1 0; p 1 1; p 2 1 ]
              (Z, State0), [ p 0 0; p 0 1; p 1 1; p 1 2 ]
              (Z, StateR), [ p 0 2; p 1 1; p 1 2; p 2 1 ]
              (Z, State2), [ p 1 0; p 1 1; p 2 1; p 2 2 ]
              (Z, StateL), [ p 0 1; p 1 0; p 1 1; p 2 0 ] ]

        for ((kind, rotation), cells) in expected do
            Assert.equal (cellSet cells) (Tetromino.cells kind rotation |> cellSet) $"Cells for {kind} {rotation}"

    let spawnPositionsAreCentered () =
        let cases =
            [ I, 3
              O, 4
              T, 3
              J, 3
              L, 3
              S, 3
              Z, 3 ]

        for kind, expectedCol in cases do
            let piece = Piece.spawn kind
            Assert.equal 0 piece.Row $"Spawn row for {kind}"
            Assert.equal expectedCol piece.Col $"Spawn column for {kind}"
            Assert.equal State0 piece.Rotation $"Spawn rotation for {kind}"

    let movementRespectsWallsAndBlocks () =
        let leftWall =
            Game.create O I
            |> Game.moveLeft
            |> Game.moveLeft
            |> Game.moveLeft
            |> Game.moveLeft
            |> Game.moveLeft

        Assert.equal 0 leftWall.Current.Col "O piece stops at the left wall."

        let rightWall =
            Game.create O I
            |> Game.moveRight
            |> Game.moveRight
            |> Game.moveRight
            |> Game.moveRight
            |> Game.moveRight

        Assert.equal 8 rightWall.Current.Col "O piece stops at the right wall."

        let blockedBoard = board [ p 0 3 ] Z

        let blocked =
            { Game.create O I with
                Board = blockedBoard }
            |> Game.moveLeft

        Assert.equal 4 blocked.Current.Col "Piece does not move into occupied cells."

    let rotationsUseWallAndFloorKicks () =
        let rightKick =
            { Game.create I O with
                Current =
                    { Kind = I
                      Rotation = StateR
                      Row = 0
                      Col = 7 } }
            |> Game.rotateClockwise

        Assert.equal State2 rightKick.Current.Rotation "I piece rotates near right wall."
        Assert.equal 6 rightKick.Current.Col "I piece kicks left near right wall."

        let leftKick =
            { Game.create I O with
                Current =
                    { Kind = I
                      Rotation = StateR
                      Row = 0
                      Col = -2 } }
            |> Game.rotateClockwise

        Assert.equal State2 leftKick.Current.Rotation "I piece rotates near left wall."
        Assert.equal 0 leftKick.Current.Col "I piece uses the +2 wall kick near left wall."

        let floorKick =
            { Game.create T O with
                Current =
                    { Kind = T
                      Rotation = State0
                      Row = 18
                      Col = 3 } }
            |> Game.rotateClockwise

        Assert.equal StateR floorKick.Current.Rotation "T piece rotates near floor."
        Assert.equal 17 floorKick.Current.Row "T piece kicks one row upward near floor."

    let oPieceRotationChangesStateOnly () =
        let state = Game.create O I
        let before = Piece.absoluteCells state.Current |> cellSet
        let rotated = Game.rotateClockwise state
        let after = Piece.absoluteCells rotated.Current |> cellSet

        Assert.equal StateR rotated.Current.Rotation "O rotation state advances."
        Assert.equal before after "O piece cell layout is unchanged by rotation."

    let hardDropLocksPieceAndSpawnsNext () =
        let afterDrop = Game.create O T |> Game.hardDrop (provider [ I ])

        Assert.equal T afterDrop.Current.Kind "Next piece becomes current after lock."
        Assert.equal I afterDrop.Next "A new next piece is drawn after lock."
        Assert.equal false afterDrop.HoldUsed "Hold becomes available after lock."

        for position in [ p 18 4; p 18 5; p 19 4; p 19 5 ] do
            Assert.equal (Some O) (Map.tryFind position afterDrop.Board) "Hard-dropped O locks at the bottom."

    let lineClearingAndScoringWork () =
        Assert.equal 0 (Game.scoreForClearedLines 0) "No line score."
        Assert.equal 100 (Game.scoreForClearedLines 1) "Single line score."
        Assert.equal 300 (Game.scoreForClearedLines 2) "Double line score."
        Assert.equal 500 (Game.scoreForClearedLines 3) "Triple line score."
        Assert.equal 800 (Game.scoreForClearedLines 4) "Tetris score."

        let oneLineSetup =
            [ for col in 0 .. 9 do
                  if col <> 4 && col <> 5 then
                      p 19 col ]

        let oneLineState =
            { Game.create O T with
                Board = board oneLineSetup J }
            |> Game.hardDrop (provider [ I ])

        Assert.equal 100 oneLineState.Score "Clearing one line awards 100 points."
        Assert.equal 1 oneLineState.Lines "Cleared line count increments."
        Assert.isFalse (Map.containsKey (p 19 0) oneLineState.Board) "Filled bottom row is removed."
        Assert.equal (Some O) (Map.tryFind (p 19 4) oneLineState.Board) "Blocks above a cleared row shift down."

        let twoRows =
            [ for row in [ 18; 19 ] do
                  for col in 0 .. 9 do
                      p row col ]

        let cleared, shifted = Board.clearCompletedLines (board (p 17 0 :: twoRows) S)
        Assert.equal 2 cleared "Two complete rows are detected."
        Assert.equal (Some S) (Map.tryFind (p 19 0) shifted) "Rows above two cleared lines shift by two."

    let holdStoresSwapsAndLocksOncePerPiece () =
        let draw = provider [ T; S ]
        let held = Game.create I O |> Game.hold draw

        Assert.equal (Some I) held.Hold "Current piece moves into empty hold."
        Assert.equal O held.Current.Kind "Next piece becomes current after hold."
        Assert.equal T held.Next "Holding into an empty slot draws a new next piece."
        Assert.equal true held.HoldUsed "Hold is marked as used."

        let ignored = Game.hold draw held
        Assert.equal held ignored "Hold cannot be used twice for the same current piece."

        let afterLock = Game.hardDrop draw held
        Assert.equal false afterLock.HoldUsed "Locking a piece makes hold available again."

        let swapped = Game.hold draw afterLock
        Assert.equal I swapped.Current.Kind "Held piece swaps back in."
        Assert.equal (Some afterLock.Current.Kind) swapped.Hold "Current piece becomes the held piece after swap."

    let automaticFallingAndLockDelayWork () =
        let afterFall = Game.create O I |> Game.advanceTime Constants.FallInterval (provider [ T ])
        Assert.equal 1 afterFall.Current.Row "A piece automatically falls after 0.75 seconds."

        let resting =
            { Game.create O I with
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 18
                      Col = 4 } }

        let inDelay = Game.advanceTime Constants.FallInterval (provider [ T ]) resting
        Assert.isTrue (Option.isSome inDelay.LockDelay) "Resting piece enters lock delay."
        Assert.equal Playing inDelay.Status "Piece does not lock immediately when lock delay starts."

        let stillWaiting = Game.advanceTime (TimeSpan.FromSeconds 0.49) (provider [ T ]) inDelay
        Assert.equal Playing stillWaiting.Status "Piece remains active before the lock delay expires."

        let locked = Game.advanceTime (TimeSpan.FromSeconds 0.02) (provider [ T ]) stillWaiting
        Assert.equal I locked.Current.Kind "Piece locks when lock delay expires."

    let longElapsedFrameStopsAtLockDelay () =
        let afterLongFrame = Game.create O I |> Game.advanceTime (TimeSpan.FromSeconds 60.0) (provider [ T ])

        Assert.equal O afterLongFrame.Current.Kind "A long frame does not skip past the resting current piece."
        Assert.equal 18 afterLongFrame.Current.Row "The current piece falls to its resting row."
        Assert.equal I afterLongFrame.Next "The next piece is not consumed before the lock delay expires."
        Assert.equal 0 afterLongFrame.Board.Count "The resting piece is not locked during the same long fall frame."
        Assert.isTrue (Option.isSome afterLongFrame.LockDelay) "A long frame starts lock delay instead of locking immediately."

    let lockDelayResetsAreLimitedAndCanExitDelay () =
        let delayed =
            { Game.create O I with
                Board = board [ p 18 4 ] J
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 16
                      Col = 4 }
                LockDelay = Some(TimeSpan.FromSeconds 0.1)
                LockResetCount = 0 }

        let offLedge = Game.moveRight delayed
        Assert.equal None offLedge.LockDelay "Moving off a ledge exits lock delay."
        Assert.equal 1 offLedge.LockResetCount "Successful lock-delay movement increments reset count."

        let maxed =
            { Game.create O I with
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 18
                      Col = 4 }
                LockDelay = Some(TimeSpan.FromSeconds 0.1)
                LockResetCount = Constants.MaxLockResets }

        let moved = Game.moveLeft maxed
        Assert.equal (Some(TimeSpan.FromSeconds 0.1)) moved.LockDelay "Move no longer resets the timer after 15 resets."
        Assert.equal Constants.MaxLockResets moved.LockResetCount "Reset count does not exceed 15."
        Assert.equal 3 moved.Current.Col "Movement is still allowed after 15 resets."

        let maxedOnLedge =
            { delayed with
                LockResetCount = Constants.MaxLockResets }

        let cappedOffLedge = Game.moveRight maxedOnLedge
        Assert.equal None cappedOffLedge.LockDelay "Moving off a ledge exits lock delay even after 15 resets."
        Assert.equal Constants.MaxLockResets cappedOffLedge.LockResetCount "Exiting delay preserves the capped reset count."
        Assert.equal 5 cappedOffLedge.Current.Col "Capped movement off a ledge is still applied."

    let cappedRotationIsAllowedWithoutResettingDelay () =
        let cappedResting =
            { Game.create T J with
                Current =
                    { Kind = T
                      Rotation = State0
                      Row = 18
                      Col = 3 }
                LockDelay = Some(TimeSpan.FromSeconds 0.1)
                LockResetCount = Constants.MaxLockResets }

        let rotated = Game.rotateClockwise cappedResting
        Assert.equal StateR rotated.Current.Rotation "Rotation is still allowed after 15 resets."
        Assert.equal 17 rotated.Current.Row "A capped rotation may still use a floor kick."
        Assert.equal (Some(TimeSpan.FromSeconds 0.1)) rotated.LockDelay "Rotation no longer resets the timer after 15 resets."
        Assert.equal Constants.MaxLockResets rotated.LockResetCount "Rotation does not increase the capped reset count."

    let cappedRestingRotationCannotExtendDelay () =
        let draw = provider [ T ]
        let frameTime = TimeSpan.FromMilliseconds 25.0

        let mutable state =
            { Game.create O J with
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 18
                      Col = 4 }
                LockDelay = Some Constants.LockDelay
                LockResetCount = Constants.MaxLockResets }

        for _ in 1 .. 25 do
            state <- state |> Game.rotateClockwise |> Game.advanceTime frameTime draw

        Assert.equal J state.Current.Kind "Capped rotation locks when the original delay expires."
        Assert.equal 4 state.Board.Count "The capped piece is locked after the original delay expires."

    let movingOffLedgePreventsStaleLockExpiry () =
        let delayed =
            { Game.create O I with
                Board = board [ p 18 4 ] J
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 16
                      Col = 4 }
                LockDelay = Some(TimeSpan.FromSeconds 0.01)
                LockResetCount = 0 }

        let movedThenTicked =
            delayed
            |> Game.moveRight
            |> Game.advanceTime (TimeSpan.FromSeconds 0.2) (provider [ T ])

        Assert.equal O movedThenTicked.Current.Kind "The current piece remains active after moving off a ledge."
        Assert.equal 16 movedThenTicked.Current.Row "A stale expired lock timer does not force a lock."
        Assert.equal 5 movedThenTicked.Current.Col "The successful move is preserved."
        Assert.equal None movedThenTicked.LockDelay "The piece stays out of lock delay while it can fall."
        Assert.equal 1 movedThenTicked.Board.Count "Only the original support block remains locked."

    let hardDropDuringLockDelayClearsTimingState () =
        let delayed =
            { Game.create O T with
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 18
                      Col = 4 }
                LockDelay = Some(TimeSpan.FromSeconds 0.01)
                LockResetCount = 14
                FallAccumulator = TimeSpan.FromSeconds 0.74 }

        let afterHardDrop = Game.hardDrop (provider [ S ]) delayed
        let afterSameFrameTick = Game.advanceTime (TimeSpan.FromSeconds 0.01) (provider [ Z ]) afterHardDrop

        Assert.equal TimeSpan.Zero afterHardDrop.FallAccumulator "Old fall accumulation is cleared immediately after hard drop."
        Assert.equal T afterSameFrameTick.Current.Kind "Hard drop spawns exactly one next current piece."
        Assert.equal S afterSameFrameTick.Next "Hard drop consumes exactly one new next piece."
        Assert.equal 4 afterSameFrameTick.Board.Count "The hard-dropped piece is locked exactly once."
        Assert.equal None afterSameFrameTick.LockDelay "Old lock-delay state is cleared after hard drop."
        Assert.equal 0 afterSameFrameTick.LockResetCount "Old lock reset count is cleared after hard drop."
        Assert.equal (TimeSpan.FromSeconds 0.01) afterSameFrameTick.FallAccumulator "Only the new tick elapsed time is accumulated after hard drop."

    let holdDuringLockDelayClearsTimingState () =
        let delayed =
            { Game.create I O with
                LockDelay = Some(TimeSpan.FromSeconds 0.01)
                LockResetCount = 14
                FallAccumulator = TimeSpan.FromSeconds 0.74 }

        let afterHold = Game.hold (provider [ T ]) delayed

        Assert.equal O afterHold.Current.Kind "Hold replaces the current piece with the previous next piece."
        Assert.equal T afterHold.Next "Hold into an empty slot consumes exactly one new next piece."
        Assert.equal (Some I) afterHold.Hold "The old current piece is held instead of being locked."
        Assert.equal 0 afterHold.Board.Count "Hold does not lock the old current piece."
        Assert.equal None afterHold.LockDelay "Old lock-delay state is cleared after hold."
        Assert.equal 0 afterHold.LockResetCount "Old lock reset count is cleared after hold."
        Assert.equal TimeSpan.Zero afterHold.FallAccumulator "Old fall accumulation is cleared after hold."

    let gameOverOccursWhenNextSpawnIsBlocked () =
        let blockedSpawnBoard = board [ p 0 4 ] Z

        let over =
            { Game.create O T with
                Board = blockedSpawnBoard
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 18
                      Col = 4 } }
            |> Game.hardDrop (provider [ I ])

        Assert.equal GameOver over.Status "Game ends when the new current piece cannot spawn."

    let gameOverIgnoresQueuedInputsAndElapsedTime () =
        let blockedSpawnBoard = board [ p 0 4 ] Z

        let over =
            { Game.create O T with
                Board = blockedSpawnBoard
                Current =
                    { Kind = O
                      Rotation = State0
                      Row = 18
                      Col = 4 } }
            |> Game.hardDrop (provider [ I ])

        let afterQueuedActions =
            over
            |> Game.moveLeft
            |> Game.moveRight
            |> Game.softDrop
            |> Game.rotateClockwise
            |> Game.hardDrop (provider [ S ])
            |> Game.hold (provider [ Z ])
            |> Game.advanceTime (TimeSpan.FromSeconds 10.0) (provider [ J ])

        Assert.equal GameOver over.Status "Test setup reaches game over."
        Assert.equal over afterQueuedActions "Game-over state ignores queued gameplay actions and elapsed time."

    let randomGeneratorProducesTetrominoKinds () =
        let draw = PieceGeneration.randomGenerator (Random 123)

        for _ in 1 .. 100 do
            Assert.isTrue (List.contains (draw()) Tetromino.allKinds) "Random generator returns a valid tetromino."
