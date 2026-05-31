namespace Tetris.Core

module Tetromino =
    let allKinds = [ I; O; T; J; L; S; Z ]

    let private p row col = Position.create row col

    let boxSize =
        function
        | I -> 4
        | O -> 2
        | T
        | J
        | L
        | S
        | Z -> 3

    let nextRotation =
        function
        | State0 -> StateR
        | StateR -> State2
        | State2 -> StateL
        | StateL -> State0

    let cells kind rotation =
        match kind, rotation with
        | I, State0 -> [ p 1 0; p 1 1; p 1 2; p 1 3 ]
        | I, StateR -> [ p 0 2; p 1 2; p 2 2; p 3 2 ]
        | I, State2 -> [ p 2 0; p 2 1; p 2 2; p 2 3 ]
        | I, StateL -> [ p 0 1; p 1 1; p 2 1; p 3 1 ]

        | O, _ -> [ p 0 0; p 0 1; p 1 0; p 1 1 ]

        | T, State0 -> [ p 0 1; p 1 0; p 1 1; p 1 2 ]
        | T, StateR -> [ p 0 1; p 1 1; p 1 2; p 2 1 ]
        | T, State2 -> [ p 1 0; p 1 1; p 1 2; p 2 1 ]
        | T, StateL -> [ p 0 1; p 1 0; p 1 1; p 2 1 ]

        | J, State0 -> [ p 0 0; p 1 0; p 1 1; p 1 2 ]
        | J, StateR -> [ p 0 1; p 0 2; p 1 1; p 2 1 ]
        | J, State2 -> [ p 1 0; p 1 1; p 1 2; p 2 2 ]
        | J, StateL -> [ p 0 1; p 1 1; p 2 0; p 2 1 ]

        | L, State0 -> [ p 0 2; p 1 0; p 1 1; p 1 2 ]
        | L, StateR -> [ p 0 1; p 1 1; p 2 1; p 2 2 ]
        | L, State2 -> [ p 1 0; p 1 1; p 1 2; p 2 0 ]
        | L, StateL -> [ p 0 0; p 0 1; p 1 1; p 2 1 ]

        | S, State0 -> [ p 0 1; p 0 2; p 1 0; p 1 1 ]
        | S, StateR -> [ p 0 1; p 1 1; p 1 2; p 2 2 ]
        | S, State2 -> [ p 1 1; p 1 2; p 2 0; p 2 1 ]
        | S, StateL -> [ p 0 0; p 1 0; p 1 1; p 2 1 ]

        | Z, State0 -> [ p 0 0; p 0 1; p 1 1; p 1 2 ]
        | Z, StateR -> [ p 0 2; p 1 1; p 1 2; p 2 1 ]
        | Z, State2 -> [ p 1 0; p 1 1; p 2 1; p 2 2 ]
        | Z, StateL -> [ p 0 1; p 1 0; p 1 1; p 2 0 ]
