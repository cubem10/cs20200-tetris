# cs20200-tetris

Console Tetris implemented in F# from `project_proposal.pdf`.

## Run

```bash
dotnet run --project src/Tetris.Console/Tetris.Console.fsproj
```

Controls: left/right/down arrows move, up rotates, space hard-drops, `C` holds, `Q`
quits, and `R` restarts from the game-over screen.

## Test

```bash
dotnet run --project tests/Tetris.Tests/Tetris.Tests.fsproj
```
