module Saon.Convert

/// Provide useful conversion functions.
open System

/// Convert from string to decimal.
let stringToDecimal : Transformer<string, decimal> =
    fun propName (value : string) ->
        let parsed, decimalValue = Decimal.TryParse(value)
        if parsed then
            ParserResult.success decimalValue
        else
            ParserResult.validationFail "stringToDecimal" propName "malformed decimal number"

/// Convert from string to DateTimeOffset.
let stringToDateTimeOffset : Transformer<string, DateTimeOffset> =
    fun propName (value : string) ->
        let parsed, dt = DateTimeOffset.TryParse(value)
        if parsed then
            ParserResult.success dt
        else
            ParserResult.validationFail "stringToDateTimeOffset" propName "malformed datetime"
