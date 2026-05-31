# cs20200-tetris

Console Tetris implemented in F#.

## How to Play

To play, use binary from releases tab.
If you're using an unsupported platform, please follow the instructions in [Run](#run).

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

## Requirement Changes and Justifications

- The undersized-terminal pause message is shorter than the proposal text:
  `Resize to 40x24. Q quits.` instead of
  `Terminal is too small. Please resize to at least 40x24.` This keeps the message
  readable in a terminal that is already too small while preserving the required
  behavior: the game pauses, ignores gameplay keys, continues checking terminal
  size, and allows `Q` to quit.
- The game shows a ghost piece landing preview, which the proposal does not
  mention. This is an added visual aid only; it does not change movement,
  collision, scoring, locking, line clearing, piece generation, hold behavior, or
  any game-ending rule.
- The game uses input polling intervals of about 25 ms during gameplay and 50 ms
  during the undersized-terminal pause screen. The proposal does not specify
  polling timing, so these values are implementation details chosen to keep input
  responsive without constantly redrawing the terminal.

## LLM Usage

- I used the LLM to write the basic skeleton and help fix the console rendering
  code, ghost piece, tests, README text, and build commands.
- I had to reprompt when the screen did not redraw correctly, such as stale
  `NEXT` blocks and resize messages that were hidden or misaligned.
- The LLM was not able to find out graphical glitches correctly. It did not
  handle ANSI clears, newlines, cached frames, and resize behavior correctly at
  first.

## Run

This project requires .NET 10 SDK to build and run.
You can download .NET 10 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

```bash
dotnet run --project src/Tetris.Console/Tetris.Console.fsproj
```

## Test

```bash
dotnet run --project tests/Tetris.Tests/Tetris.Tests.fsproj
```
