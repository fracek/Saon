namespace Saon

/// Information about a validation error.
type ValidationError =
    { /// The property for which the validation failed.
      Property : string
      /// The type of the validation that failed.
      Type : string
      /// A user-readable message.
      Message : string }


/// Result after parsing and validating.
type ParserResult<'T> =
    /// Parsing failed, for example a syntax error.
    | ParsingFailed of field: string option * message: string
    /// Validation failed.
    | ValidationFailed of Map<string, ValidationError list>
    /// Success.
    | Success of 'T

/// A `Transformer` transforms data from one type to another.
/// If the transformation fails, return an error with the transformation type and user message.
type Transformer<'T, 'U> = string -> 'T -> ParserResult<'U>

/// A validator does not change the data type, but can return an error.
type Validator<'T> = Transformer<'T, 'T>

/// Parser for elements of type 'E, producing results of type 'T.
type Parser<'T, 'E> = 'E -> ParserResult<'T> * 'E


module ParserResult =
    /// Return a success result.
    let success value = Success value

    /// Parsing of `field` failed, more information in `msg`.
    let parsingFail field msg = ParsingFailed (Some field, msg)

    /// Validation `typ` of property `propName` failed, more information in `msg`.
    let validationFail typ propName msg =
        let error =
            { Property = propName
              Type = typ
              Message = msg }
        Map.empty |> Map.add propName [error] |> ValidationFailed

    /// Return a ParserResult from a transformer result.
    let fromTransformerResult propName = function
        | Ok r -> success r
        | Error errors ->
            let propErrors = List.map (fun (typ, msg) -> { Property = propName; Type = typ; Message = msg }) errors
            Map.empty |> Map.add propName propErrors |> ValidationFailed

    /// Apply `f` only if the value is `Success`.
    let map f = function
        | Success v -> Success (f v)
        | ParsingFailed (field, msg) -> ParsingFailed (field, msg)
        | ValidationFailed failMap -> ValidationFailed failMap


module Parser =
    let init (value : 'T) : Parser<'T, 'E> =
        fun el -> (Success value, el)

    let bind (f : 'T -> Parser<'R, 'E>) (x : Parser<'T, 'E>) : Parser<'R, 'E> =
        fun json ->
            match x json with
            | Success v, json' -> f v json'
            | ParsingFailed (field, msg), json' -> ParsingFailed(field, msg), json'
            | ValidationFailed errors, json' -> ValidationFailed errors, json'

    /// Return a new parser that pipes the result of the first into the second only if the first was successful.
    let pipe (a : string -> 'T -> ParserResult<'R>) (b : string -> 'R -> ParserResult<'S>) =
        fun propName aValue ->
            match a propName aValue with
            | Success bValue ->
                b propName bValue
            | ValidationFailed failMap -> ValidationFailed failMap
            | ParsingFailed (field, msg) -> ParsingFailed (field, msg)


module Operators =
    let (/>) a b = Parser.pipe a b
