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

/// Convert from string to DateTimeOffset using `parse` to convert.
let stringToDateTimeOffset (parse : string -> bool * DateTimeOffset) : Transformer<string, DateTimeOffset> =
    fun propName (value : string) ->
        let parsed, dt = parse value
        if parsed then
            ParserResult.success dt
        else
            ParserResult.validationFail "stringToDateTimeOffset" propName "malformed datetime"

let internal parseISO8601DateTimeOffset (value : string) =
    let pattern = "yyyy-MM-dd'T'HH:mm:ss.FFFK"
    DateTimeOffset.TryParseExact(value, [| pattern |], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces)

/// Convert from string to DateTimeOffset, the date is formatted as ISO8601.
let stringToDateTimeOffsetISO8601 : Transformer<string, DateTimeOffset> =
    stringToDateTimeOffset parseISO8601DateTimeOffset