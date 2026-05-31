namespace Tetris.Tests

module Assert =
    let equal expected actual message =
        if expected <> actual then
            failwithf "%s Expected: %A Actual: %A" message expected actual

    let isTrue condition message =
        if not condition then
            failwith message

    let isFalse condition message = isTrue (not condition) message
