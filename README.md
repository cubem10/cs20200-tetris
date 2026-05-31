# cs20200-tetris

Console Tetris implemented in F# from `project_proposal.pdf`.

## Run

```bash
dotnet run --project src/Tetris.Console/Tetris.Console.fsproj
```

## How to Play

Move and rotate the falling tetrominoes to fill horizontal rows. Completed rows
clear from the board and increase your score. The game ends when a new piece
cannot spawn.

The dim preview on the board is the ghost piece. It shows where the current
piece will land if you hard-drop it.

## Key Bindings

| Key | Action |
| --- | --- |
| Left arrow | Move left |
| Right arrow | Move right |
| Down arrow | Soft drop |
| Up arrow | Rotate clockwise |
| Space | Hard drop |
| C | Hold or swap the current piece |
| R | Restart from the game-over screen |
| Q | Quit |

## Rules

- A soft drop moves the current piece down by one row.
- A hard drop immediately locks the current piece at its landing position.
- Hold can be used once per falling piece. It becomes available again after the
  piece locks.
- Rows score by the number cleared at once: 1 row = 100, 2 rows = 300, 3 rows =
  500, and 4 rows = 800.

## Changes After Proposal
Input polling is an implementation detail: normal gameplay checks queued keys about
every 25 ms, and the undersized-terminal pause screen checks for `Q` about every
50 ms. 

## Test

```bash
dotnet run --project tests/Tetris.Tests/Tetris.Tests.fsproj
```
