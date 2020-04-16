/// Provide common validation functions.
module Saon.Validate

open System
open System.Text.RegularExpressions


// Combine

let all (validators : Validator<'T> list) : Validator<'T> =
    let folder propName value state validator =
        match state with
        | Success _ ->  validator propName value
        | ParsingFailed (field, msg) -> ParsingFailed (field, msg)
        | ValidationFailed failMap ->
            match validator propName value with
            | ValidationFailed failMap' ->
                ValidationFailedMap.merge failMap failMap' |> ValidationFailed
            | result -> result
    fun propName value ->
        List.fold (folder propName value) (Success value) validators


// Objects

/// Validate if `value` is not null.
let isNotNull : Validator<'T> =
    fun propName value ->
        if not <| isNull value then
            ParserResult.success value
        else
            ParserResult.validationFail "isNotNull" propName "must be not null"

/// Validate if `value` is equal to `target`.
let isEqual target : Validator<'T> =
    fun propName value ->
        if target = value then
            ParserResult.success value
        else
            let msg = sprintf "must be equal to %O" target
            ParserResult.validationFail "isEqual" propName msg

/// Validate if `value` is not equal to `target`.
let isNotEqual target : Validator<'T> =
    fun propName value ->
        if target <> value then
            ParserResult.success value
        else
            let msg = sprintf "must be not equal to %O" target
            ParserResult.validationFail "isNotEqual" propName msg

/// Validate if `predicate` of `value` is true.
let must (predicate : 'T -> bool) : Validator<'T> =
    fun propName value ->
        if predicate value then
            ParserResult.success value
        else
            ParserResult.validationFail "must" propName "predicate must be true"

/// Validate if `predicate` of `value` is false.
let mustNot (predicate : 'T -> bool) : Validator<'T> =
    fun propName value ->
        if not <| predicate value then
            ParserResult.success value
        else
            ParserResult.validationFail "mustNot" propName "predicate must be false"


// Sequences

/// Validate if `value` is not empty.
let isNotEmpty : Validator<seq<'T>> =
    fun propName (value : seq<'T>) ->
        if not <| Seq.isEmpty value then
            ParserResult.success value
        else
            ParserResult.validationFail "isNotEmpty" propName "must be not empty"

/// Validate if `value` is at least `minLength` elements long.
let hasMinLength minLength : Validator<seq<'T>> =
    fun propName (value : seq<'T>) ->
        if Seq.length value >= minLength then
            ParserResult.success value
        else
            let msg = sprintf "must be at least %O elements long" minLength
            ParserResult.validationFail "hasMinLength" propName msg

/// Validate if `value` is at most `maxLength` elements long.
let hasMaxLength maxLength : Validator<seq<'T>> =
    fun propName (value : seq<'T>) ->
        if Seq.length value <= maxLength then
            ParserResult.success value
        else
            let msg = sprintf "must be at most %O elements long" maxLength
            ParserResult.validationFail "hasMaxLength" propName msg

/// Validate if `value` length is between `minLength` and `maxLength`.
let hasLengthBetween minLength maxLength : Validator<seq<'T>> =
    fun propName (value : seq<'T>) ->
        let length = Seq.length value
        if minLength <= length && length <= maxLength then
            ParserResult.success value
        else
            let msg = sprintf "must be between %O and %O elements long" minLength maxLength
            ParserResult.validationFail "hasLengthBetween" propName msg


// Strings

/// Validate if the string `value` is not empty or whitespace.
let isNotEmptyOrWhitespace : Validator<string> =
    fun propName (value : string) ->
        if String.IsNullOrEmpty(value) || String.IsNullOrWhiteSpace(value) then
            ParserResult.validationFail "isNotEmptyOrWhitespace" propName "must be not empty or whitespace"
        else
            ParserResult.success value

/// Validate if `value` matches the regex `pattern`.
let matchesRegex pattern : Validator<string> =
    fun propName (value : string) ->
        if Regex.Match(value, pattern).Success then
            ParserResult.success value
        else
            let msg = sprintf "must match the regex '%s'" pattern
            ParserResult.validationFail "matchesRegex" propName msg

/// Validate if `value` is a valid email address.
///
/// We perform a simple check for the presence of the @ character, and that this character is not the first or
/// last character in the string.
let isEmail : Validator<string> =
    fun propName (value : string) ->
        let index = value.IndexOf('@')
        let isValid = index > 0 && index <> value.Length - 1 && index = value.LastIndexOf('@')
        if isValid then
            ParserResult.success value
        else
            ParserResult.validationFail "isEmail" propName "must be a valid email address"


// Numbers

/// Validate if `value` is greater than `minValue`.
let isGreaterThan minValue : Validator<'T> =
    fun propName value ->
        if value > minValue then
            ParserResult.success value
        else
            let msg = sprintf "must be greater than %O" minValue
            ParserResult.validationFail "isGreaterThan" propName msg

/// Validate if `value` is greater than or equal to `minValue`.
let isGreaterThanOrEqualTo minValue : Validator<'T> =
    fun propName value ->
        if value >= minValue then
            ParserResult.success value
        else
            let msg = sprintf "must be greater than or equal to %O" minValue
            ParserResult.validationFail "isGreaterThanOrEqualTo" propName msg

/// Validate if `value` is less than `minValue`.
let isLessThan maxValue : Validator<'T> =
    fun propName value ->
        if value < maxValue then
            ParserResult.success value
        else
            let msg = sprintf "must be less than %O" maxValue
            ParserResult.validationFail "isLessThan" propName msg

/// Validate if `value` is less than or equal to `minValue`.
let isLessThanOrEqualTo maxValue : Validator<'T> =
    fun propName value ->
        if value <= maxValue then
            ParserResult.success value
        else
            let msg = sprintf "must be less than or equal to %O" maxValue
            ParserResult.validationFail "isLessThanOrEqualTo" propName msg

/// Validate if `value` is between `minValue` and `maxValue`, inclusive.
let isBetweenInclusive minValue maxValue : Validator<'T> =
    fun propName value ->
        if minValue <= value <= maxValue then
            ParserResult.success value
        else
            let msg = sprintf "must be between %O and %O" minValue maxValue
            ParserResult.validationFail "isBetweenInclusive" propName msg

/// Validate if `value` is between `minValue` and `maxValue`, exclusive.
let isBetweenExclusive minValue maxValue : Validator<'T> =
    fun propName value ->
        if minValue < value < maxValue then
            ParserResult.success value
        else
            let msg = sprintf "must be between %O and %O" minValue maxValue
            ParserResult.validationFail "isBetweenExclusive" propName msg

