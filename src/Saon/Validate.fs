module Saon.Validate

/// Provide common validation functions.
open System


/// Validate if `value` is not empty.
let isNotEmpty propName (value : seq<'T>) =
    if not <| Seq.isEmpty value then
        Result.success value
    else
        Result.validationFail "isNotEmpty" propName "Value must be not empty"

/// Validate if the string `value` is not empty or whitespace.
let isNotEmptyOrWhitespace propName (value : string) =
    if String.IsNullOrEmpty(value) || String.IsNullOrWhiteSpace(value) then
        Result.validationFail "isNotEmptyOrWhitespace" propName "Value must be not empty or whitespace"
    else
        Result.success value

/// Validate if `value` is greater than `minValue`.
let isGreaterThan minValue propName value =
    if value > minValue then
        Result.success value
    else
        let msg = sprintf "Value must be greater than %O" minValue
        Result.validationFail "isGreaterThan" propName msg

/// Validate if `value` is greater than or equal to `minValue`.
let isGreaterThanOrEqualTo minValue propName value =
    if value >= minValue then
        Result.success value
    else
        let msg = sprintf "Value must be greater than or equal to %O" minValue
        Result.validationFail "isGreaterThanOrEqualTo" propName msg