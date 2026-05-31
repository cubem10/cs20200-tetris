namespace Tetris.Core

open System

type PieceKind =
    | I
    | O
    | T
    | J
    | L
    | S
    | Z

type Rotation =
    | State0
    | StateR
    | State2
    | StateL

[<Struct; CustomComparison; CustomEquality>]
type Position =
    { Row: int
      Col: int }

    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? Position as otherPosition ->
                compare (this.Row, this.Col) (otherPosition.Row, otherPosition.Col)
            | _ -> invalidArg (nameof other) "Cannot compare Position with another type."

    override this.Equals other =
        match other with
        | :? Position as otherPosition -> this.Row = otherPosition.Row && this.Col = otherPosition.Col
        | _ -> false

    override this.GetHashCode() = HashCode.Combine(this.Row, this.Col)

type Board = Map<Position, PieceKind>

type ActivePiece =
    { Kind: PieceKind
      Rotation: Rotation
      Row: int
      Col: int }

type GameStatus =
    | Playing
    | GameOver

type GameState =
    { Board: Board
      Current: ActivePiece
      Next: PieceKind
      Hold: PieceKind option
      HoldUsed: bool
      Score: int
      Lines: int
      Status: GameStatus
      LockDelay: TimeSpan option
      LockResetCount: int
      FallAccumulator: TimeSpan }
