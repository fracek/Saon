namespace Saon

open System.Text.Json


type ValidationError =
    { Property : string
      Type : string
      Message : string }


type Result<'T> =
    | ParsingFailed of field: string option * message: string
    | ValidationFailed of Map<string, ValidationError>
    | Success of 'T


module Result =
    let success value = Success value

    let parsingFail field msg = ParsingFailed (Some field, msg)

    let validationFail typ propName message =
        let error =
            { Property = propName
              Type = typ
              Message = message }
        Map.empty |> Map.add propName error |> ValidationFailed


    let map f = function
        | Success v -> Success (f v)
        | ParsingFailed (field, msg) -> ParsingFailed (field, msg)
        | ValidationFailed failMap -> ValidationFailed failMap


type Parser<'T> = JsonElement -> Result<'T> * JsonElement

module Parser =

    let init (value : 'T) : Parser<'T> =
        fun json -> (Success value, json)

    let bind (f : 'T -> Parser<'R>) (x : Parser<'T>) : Parser<'R> =
        fun json ->
            match x json with
            | Success v, json' -> f v json'
            | ParsingFailed (field, msg), json' -> ParsingFailed(field, msg), json'
            | ValidationFailed errors, json' ->
                // TODO(fra): continue parsing to combine validation errors
                ValidationFailed errors, json'


type JsonParserBuilder() =
    member __.Run (parser : Parser<'T>) =
        let validate (propName : string) (element : JsonElement) : Result<'T> =
            let result, _ = parser element
            result
        validate

    member __.Bind(parser, f) = Parser.bind f parser
    member __.Zero () = Parser.init ()
    member __.Return (value : 'R) : Parser<'R> = Parser.init value


[<AutoOpen>]
module ParserBuilder =
    let jsonParser = JsonParserBuilder()

    let createRootParser (parser : string -> JsonElement -> Result<'T>) =
        let parse (document : JsonDocument) =
            parser "" document.RootElement
        parse

    let property (propName : string) (func : string -> JsonElement -> Result<'T>) (element : JsonElement) =
        let found, propElement = element.TryGetProperty propName
        if found then
            let result = func propName propElement
            result, element
        else
            ParsingFailed (Some propName, "The property is not present"), element

    let optionalProperty (propName : string) (func : string -> JsonElement -> Result<'T>) (element : JsonElement) =
        let found, propElement = element.TryGetProperty propName
        if found then
            let result = func propName propElement |> Result.map Some
            result, element
        else
            Result.success None, element

