module Saon.Validate

/// Provide common validation functions.
open System


// Sequences

/// Validate if `value` is not empty.
let isNotEmpty : Validator<seq<'T>> =
    fun propName (value : seq<'T>) ->
        if not <| Seq.isEmpty value then
            ParserResult.success value
        else
            ParserResult.validationFail "isNotEmpty" propName "must be not empty"

(*
/// Validate if `value` is at least `minLength` elements long.
let hasMinLength minLength : Validator<seq<'T>> =
    fun (value : seq<'T>) ->
        if Seq.length value >= minLength then
            Ok value
        else
            let msg = sprintf "must be at least %O elements long" minLength
            Error ["hasMinLength", msg]

/// Validate if `value` is at most `maxLength` elements long.
let hasMaxLength maxLength : Validator<seq<'T>> =
    fun (value : seq<'T>) ->
        if Seq.length value <= maxLength then
            Ok value
        else
            let msg = sprintf "must be at most %O elements long" maxLength
            Error ["hasMaxLength", msg]

*)
// Strings

/// Validate if the string `value` is not empty or whitespace.
let isNotEmptyOrWhitespace : Validator<string> =
    fun propName (value : string) ->
        if String.IsNullOrEmpty(value) || String.IsNullOrWhiteSpace(value) then
            ParserResult.validationFail "isNotEmptyOrWhitespace" propName "must be not empty or whitespace"
        else
            ParserResult.success value

(*
// Numbers

/// Validate if `value` is greater than `minValue`.
let isGreaterThan minValue : Validator<'T> =
    fun value ->
        if value > minValue then
            Ok value
        else
            let msg = sprintf "must be greater than %O" minValue
            Error ["isGreaterThan", msg]

/// Validate if `value` is greater than or equal to `minValue`.
let isGreaterThanOrEqualTo minValue : Validator<'T> =
    fun value ->
        if value >= minValue then
            Ok value
        else
            let msg = sprintf "must be greater than or equal to %O" minValue
            Error ["isGreaterThanOrEqualTo", msg]
*)