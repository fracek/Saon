namespace Saon.Query

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Saon


type QueryParser<'T> = Parser<'T, IQueryCollection>

[<AutoOpen>]
module ParserBuilder =
    let queryParser = ParserBuilder<IQueryCollection>()


[<RequireQualifiedAccess>]
module Query =
    let parameter propName (func : Transformer<StringValues, 'T>) (query : IQueryCollection) =
        let found, value = query.TryGetValue(propName)
        if found then
            let result = func propName value
            result, query
        else
            let msg = sprintf "required parameter '%s' is missing" propName
            ParserResult.validationFail "missingParameter" propName msg, query

    let optionalParameter propName (func : Transformer<StringValues, 'T>) (query : IQueryCollection) =
        let found, value = query.TryGetValue(propName)
        if found then
            let result = func propName value |> ParserResult.map Some
            result, query
        else
            ParserResult.success None, query

    let string propName (value : StringValues) =
        if value.Count = 1 then
            ParserResult.success value.[0]
        else
            ParserResult.parsingFail propName "does not have string value"