module Saon.Parse

open System.Text.Json


let combine (a : string -> 'T -> Result<'R>) (b : string -> 'R -> Result<'S>) =
    fun propName aValue ->
        match a propName aValue with
        | Success bValue ->
            b propName bValue
        | ValidationFailed failMap -> ValidationFailed failMap
        | ParsingFailed (field, msg) -> ParsingFailed (field, msg)


let internal catchFail propName (f : unit -> 'T) =
    try
        f () |> Success
    with ex ->
        ParsingFailed (Some propName, ex.Message)

let string propName (element : JsonElement) =
    catchFail propName (fun _ -> element.GetString())

let int64 propName (element : JsonElement) =
    catchFail propName (fun _ -> element.GetInt64())

let bool propName (element : JsonElement) =
    catchFail propName (fun _ -> element.GetBoolean())

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


module Operators =
    let (/>) a b = combine a b