module Saon.Parse

open System.Text.Json


/// Return a new parser that pipes the result of the first into the second only if the first was successful.
let pipe (a : string -> 'T -> Result<'R>) (b : string -> 'R -> Result<'S>) =
    fun propName aValue ->
        match a propName aValue with
        | Success bValue ->
            b propName bValue
        | ValidationFailed failMap -> ValidationFailed failMap
        | ParsingFailed (field, msg) -> ParsingFailed (field, msg)


module Operators =
    let (/>) a b = pipe a  b


/// Return `ParsingFailed` if `f` raises an exception.
let internal catchFail propName (f : unit -> 'T) =
    try
        f () |> Success
    with ex ->
        ParsingFailed (Some propName, ex.Message)

/// Get the `element` string value.
let string propName (element : JsonElement) =
    catchFail propName (fun _ -> element.GetString())

/// Get the `element` int64 value.
let int64 propName (element : JsonElement) =
    catchFail propName (fun _ -> element.GetInt64())

/// Get the `element` bool value.
let bool propName (element : JsonElement) =
    catchFail propName (fun _ -> element.GetBoolean())

/// Apply the `inner` parser to each children of the json array `element`.
let list (inner : string -> JsonElement -> Result<'T>) propName (element : JsonElement) =
    let folder state el =
        match state with
        | Success xs ->
            match inner propName el with
            | Success x -> Success (x :: xs)
            | ValidationFailed f -> ValidationFailed f
            | ParsingFailed (field, msg) -> ParsingFailed (field, msg)
        | ValidationFailed failMap ->
            match inner propName el with
            | Success _ -> ValidationFailed failMap
            | ValidationFailed failMap' ->
                Map.fold (fun s k v -> Map.add k v s) failMap failMap' |> ValidationFailed
            | ParsingFailed (field, msg) -> ParsingFailed (field, msg)
        | ParsingFailed (field, msg) -> ParsingFailed (field, msg)

    let result = element.EnumerateArray() |> Seq.fold folder (Success [])
    match result with
    | Success xs -> List.rev xs |> Success
    | fail -> fail
