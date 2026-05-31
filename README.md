# cs20200-tetris

Console Tetris implemented in F# from `project_proposal.pdf`.

## Run

```bash
dotnet run --project src/Tetris.Console/Tetris.Console.fsproj
```

Controls: left/right/down arrows move, up rotates, space hard-drops, `C` holds, `Q`
quits, and `R` restarts from the game-over screen.

Input polling is an implementation detail: normal gameplay checks queued keys about
every 25 ms, and the undersized-terminal pause screen checks for `Q` about every
50 ms. `project_proposal.pdf` specifies fall timing and lock delay, but does not
specify an input polling interval.

## Test

```bash
dotnet run --project tests/Tetris.Tests/Tetris.Tests.fsproj
```
