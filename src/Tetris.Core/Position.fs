namespace Tetris.Core

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Position =
    let create row col = { Row = row; Col = col }
