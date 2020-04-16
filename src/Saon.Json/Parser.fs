namespace Saon.Json

open System.Text.Json
open Saon

/// A `JsonParser` parses `JsonElement`s.
type JsonParser<'T> = Parser<'T, JsonElement>

[<AutoOpen>]
module ParserBuilder =
    /// Computational expression to parse JSON objects.
    let jsonObjectParser = ParserBuilder<JsonElement>()


[<RequireQualifiedAccess>]
module Json =
    open System

    /// Create a parser to parse `JsonDocument`s.
    let createDocumentParser (parser : JsonElement -> ParserResult<'T>) =
        let parse (document : JsonDocument) =
            parser document.RootElement
        parse

    /// Return `ParsingFailed` if `f` raises an exception.
    let internal catchFail propName (f : unit -> 'T) =
        try
            f () |> Success
        with ex ->
            ParsingFailed (Some propName, ex.Message)

    /// Apply the transformation `func` to the `element` property `propName`.
    let property (propName : string) (func : Transformer<JsonElement, 'T>) (element : JsonElement) =
        let found, propElement = element.TryGetProperty propName
        if found then
            let result = func propName propElement
            result, element
        else
            let msg = sprintf "required property '%s' is missing" propName
            ParserResult.validationFail "missingProperty" propName msg, element

    /// Apply the transformation `func` to the `element` optional property `propName`.
    let optionalProperty (propName : string) (func : Transformer<JsonElement, 'T>) (element : JsonElement) =
        let found, propElement = element.TryGetProperty propName
        if found then
            let result = func propName propElement |> ParserResult.map Some
            result, element
        else
            ParserResult.success None, element

    /// Get the `element` string value.
    let string propName (element : JsonElement) =
        catchFail propName (fun _ -> element.GetString())

    /// Get the `element` int64 value.
    let int64 propName (element : JsonElement) =
        catchFail propName (fun _ -> element.GetInt64())

    /// Get the `element` bool value.
    let bool propName (element : JsonElement) =
        catchFail propName (fun _ -> element.GetBoolean())

    /// Parse the `element` as a JSON object using the `inner` parser.
    let object (inner : JsonElement -> ParserResult<'T>) propName (element : JsonElement) =
        let addNestLevel field =
            if String.IsNullOrEmpty(propName) then
                field
            elif String.IsNullOrEmpty(field) then
                propName
            else
                sprintf "%s.%s" propName field
        match inner element with
        | Success v -> Success v
        | ParsingFailed (None, msg) -> ParsingFailed (None, msg)
        | ParsingFailed (Some field, msg) ->
            let field = addNestLevel field
            ParsingFailed (Some field, msg)
        | ValidationFailed failMap ->
            failMap
            |> Map.fold (fun m k v -> Map.add (addNestLevel k) v m) Map.empty
            |> ValidationFailed

    /// Apply the `inner` parser to each children of the json array `element`.
    let list (inner : Transformer<JsonElement, 'T>) propName (element : JsonElement) =
        let folder (state, idx) el =
            let nextIdx = idx + 1
            let addNestLevel k =
                if String.IsNullOrEmpty(k) then
                    sprintf "[%d]" idx
                else
                    sprintf "[%d].%s" idx k

            match state with
            | Success xs ->
                match inner propName el with
                | Success x -> Success (x :: xs), nextIdx
                | ValidationFailed failMap ->
                    let failMap' =
                        failMap
                        |> Map.fold (fun m k v -> Map.add (addNestLevel k) v m) Map.empty
                    ValidationFailed failMap', nextIdx
                | ParsingFailed (field, msg) -> ParsingFailed (field, msg), nextIdx
            | ValidationFailed failMap ->
                match inner propName el with
                | Success _ -> ValidationFailed failMap, nextIdx
                | ValidationFailed failMap' ->
                    let failMap'' =
                        Map.fold (fun s k v -> Map.add (addNestLevel k) v s) failMap failMap'
                    ValidationFailed failMap'', nextIdx
                | ParsingFailed (field, msg) -> ParsingFailed (field, msg), nextIdx
            | ParsingFailed (field, msg) -> ParsingFailed (field, msg), nextIdx

        let result = element.EnumerateArray() |> Seq.fold folder (Success [], 0)
        match result with
        | Success xs, _ -> List.rev xs |> Success
        | fail, _ -> fail
