namespace Tetris.Core

open System

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
