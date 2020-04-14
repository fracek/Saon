module Saon.Convert

/// Provide useful conversion functions.
open System

/// Convert from string to decimal.
let stringToDecimal propName (value : string) =
    let parsed, decimalValue = Decimal.TryParse(value)
    if parsed then
        Result.success decimalValue
    else
        Result.parsingFail propName "Malformed decimal number"

/// Convert from string to DateTimeOffset.
let stringToDateTimeOffset propName (value : string) =
    let parsed, dt = DateTimeOffset.TryParse(value)
    if parsed then
        Result.success dt
    else
        Result.parsingFail propName "Malformed datetime"
