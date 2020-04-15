module Saon.Query.Tests.TestParser


open FsUnit.Xunit
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Saon
open Saon.Operators
open Saon.Query
open System.Collections.Generic
open Xunit


type QueryType = Name | Id
type Flag = CaseSensitive | CaseInsensitive

type ExampleA =
    { Query : string
      Type : QueryType
      Flag : Flag option }

let parseQueryType : Transformer<string, QueryType> =
    fun propName (value : string) ->
        match value with
        | "name" -> ParserResult.success Name
        | "id" -> ParserResult.success Id
        | _ -> ParserResult.validationFail "queryType" propName "expected one of 'name', 'id'"

let parseFlag : Transformer<string, Flag> =
    fun propName (value : string) ->
        match value with
        | "case_sensitive" -> ParserResult.success CaseSensitive
        | "case_insensitive" -> ParserResult.success CaseInsensitive
        | _ -> ParserResult.validationFail "flag" propName "expected one of 'case_sensitive', 'case_insensitive'"

let parseExampleA = queryParser {
    let! query = Query.parameter "q" Query.string
    let! typ = Query.parameter "type" (Query.string /> parseQueryType)
    let! flag = Query.optionalParameter "flag" (Query.string /> parseFlag)

    return
        { Query = query
          Type = typ
          Flag = flag }
}


[<Fact>]
let ``parse example A successfully`` () =
    let query =
        dict [ "q", StringValues("test")
               "type", StringValues("name") ]
        |> Dictionary
        |> QueryCollection
    match parseExampleA query with
    | Success result ->
        result.Query |> should equal "test"
        result.Type |> should equal Name
        result.Flag |> Option.isNone |> should be True
    | _ -> failwith "expected Success"


[<Fact>]
let ``parse optional parameter`` () =
    let query =
        dict [ "q", StringValues("test")
               "type", StringValues("name")
               "flag", StringValues("case_insensitive") ]
        |> Dictionary
        |> QueryCollection
    match parseExampleA query with
    | Success result ->
        result.Flag |> Option.isSome |> should be True
    | _ -> failwith "expected Success"
