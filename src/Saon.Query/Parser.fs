namespace Saon.Query

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Saon


/// A parser for `IQueryCollection`s.
type QueryParser<'T> = Parser<'T, IQueryCollection>


[<AutoOpen>]
module ParserBuilder =
    /// Computational Expression to build query parsers.
    let queryParser = ParserBuilder<IQueryCollection>()


/// Function to work with queries.
[<RequireQualifiedAccess>]
module Query =
    /// Apply the transformation `func` to the parameter `paramName`.
    let parameter paramName (func : Transformer<StringValues, 'T>) (query : IQueryCollection) =
        let found, value = query.TryGetValue(paramName)
        if found then
            let result = func paramName value
            result, query
        else
            let msg = sprintf "required parameter '%s' is missing" paramName
            ParserResult.validationFail "missingParameter" paramName msg, query

    /// Apply the transformation `func` to the optional parameter `paramName`.
    let optionalParameter paramName (func : Transformer<StringValues, 'T>) (query : IQueryCollection) =
        let found, value = query.TryGetValue(paramName)
        if found then
            let result = func paramName value |> ParserResult.map Some
            result, query
        else
            ParserResult.success None, query

    /// Get the parameter string value. If the parameter has more than one value, it will fail.
    let string propName (value : StringValues) =
        if value.Count = 1 then
            ParserResult.success value.[0]
        else
            ParserResult.parsingFail propName "does not have string value"