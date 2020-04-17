module Saon.Json.Tests.TestParser

open FsUnit.Xunit
open Saon
open Saon.Operators
open Saon.Json
open System
open System.Text.Json
open Xunit

type A = {B : int64; C : string }

type B = { A : A; D : string }

type Contact =
    | Email of string
    | Phone of prefix: string * number: string

type Person =
    { Name : string
      Contact : Contact }

let parseA = jsonObjectParser {
    let! b = Json.property "b" Json.int64
    let! c = Json.property "c" (Json.string /> Validate.isNotEmptyOrWhitespace)
    return { B = b; C = c }
}

let parseB = jsonObjectParser {
    let! a = Json.property "a" (Json.object parseA)
    let! d = Json.property "d" Json.string
    return { A = a; D = d }
}

let parseBWithEmbeddedA = jsonObjectParser {
    let! a = Json.embeddedObject parseA
    let! d = Json.property "d" Json.string
    return { A = a; D = d }
}

let parseListOfStrings = Json.list (Json.string /> Validate.isNotEmptyOrWhitespace)
let parseListOfObjects = Json.list (Json.object parseA)

let parsePersonInternal oneOf = jsonObjectParser {
    let parsePhone = jsonObjectParser {
        let! prefix = Json.property "prefix" Json.string
        let! number = Json.property "number" Json.string
        return Phone (prefix, number)
    }
    let toEmail _ value = ParserResult.success (Email value)

    let! name = Json.property "name" Json.string
    let! contact = oneOf [ "email", (Json.string /> toEmail); "phone", (Json.object parsePhone) ]
    return { Name = name; Contact = contact }
}

let parsePerson = parsePersonInternal Json.oneOf
let parsePersonStrict = parsePersonInternal Json.onlyOneOf


[<Fact>]
let ``parse nested records`` () =
    let json = """ { "a": { "b": 10, "c": "nice" }, "d": "foobar" } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parseB) document with
    | Success r ->
        r.A.B |> should equal 10L
        r.A.C |> should equal "nice"
        r.D |> should equal "foobar"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``validation error on nested field`` () =
    let json = """ { "a": { "b": 10, "c": "" }, "d": "foobar" } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parseB) document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 1
        let err = Map.tryFind "a.c" failMap
        err |> Option.isSome |> should be True
        let err = err.Value
        err.Length |> should equal 1
        err.Head.Type |> should equal "isNotEmptyOrWhitespace"
    | _ -> failwith "Expected ValidationFailed"


[<Fact>]
let ``parse list of strings`` () =
    let json = """ ["a", "b", "c"] """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser <| parseListOfStrings "") document with
    | Success xs ->
        xs.Length |> should equal 3
        xs.Head |> should equal "a"
        xs.Tail.Tail.Head |> should equal "c"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``validation error on list of strings`` () =
    let json = """ ["a", "", "c", ""] """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser <| parseListOfStrings "") document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 2
        Map.tryFind "[1]" failMap |> Option.isSome |> should be True
        Map.tryFind "[3]" failMap |> Option.isSome |> should be True
    | _ -> failwith "Expected ValidationFailed"


[<Fact>]
let ``parse list of objects`` () =
    let json = """ [{"b": 10, "c": "xxx"}] """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser <| parseListOfObjects "") document with
    | Success [r] ->
        r.B |> should equal 10L
        r.C |> should equal "xxx"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``validation error on list of objects`` () =
    let json = """ [{"b": 10, "c": "xxx"}, {"b": -2, "c": ""}] """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser <| parseListOfObjects "") document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 1
        Map.tryFind "[1].c" failMap |> Option.isSome |> should be True
    | _ -> failwith "Expected ValidationFailed"


[<Fact>]
let ``parse an embedded object`` () =
    let json = """ { "b": 10, "c": "nice", "d": "foobar" } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parseBWithEmbeddedA) document with
    | Success r ->
        r.A.B |> should equal 10L
        r.A.C |> should equal "nice"
        r.D |> should equal "foobar"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``parse oneOf is successful`` () =
    let json = """ { "name": "foo", "phone": { "prefix": "+44", "number": "000000" } } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parsePerson) document with
    | Success r ->
        r.Name |> should equal "foo"
        r.Contact |> should equal (Phone("+44", "000000"))
    | _ -> failwith "Expected Success"


[<Fact>]
let ``parse oneOf is successful even with multiple properties present`` () =
    let json = """ { "name": "foo", "phone": { "prefix": "+44", "number": "000000" }, "email": "b@c" } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parsePerson) document with
    | Success r ->
        r.Name |> should equal "foo"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``parse oneOf fails with no property present`` () =
    let json = """ { "name": "foo" } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parsePerson) document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 1
    | _ -> failwith "Expected ValidationError"


[<Fact>]
let ``parse onlyOneOf is successful if there is only one match`` () =
    let json = """ { "name": "foo", "phone": { "prefix": "+44", "number": "000000" } } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parsePersonStrict) document with
    | Success r ->
        r.Name |> should equal "foo"
        r.Contact |> should equal (Phone("+44", "000000"))
    | _ -> failwith "Expected Success"


[<Fact>]
let ``parse onlyOneOf fails if more than one matches are present`` () =
    let json = """ { "name": "foo", "phone": { "prefix": "+44", "number": "000000" }, "email": "b@c" } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parsePersonStrict) document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 1
    | _ -> failwith "Expected ValidationError"


[<Fact>]
let ``parse onlyOneOf fails with no property present`` () =
    let json = """ { "name": "foo" } """
    let document = JsonDocument.Parse(json)
    match (Json.createDocumentParser parsePersonStrict) document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 1
    | _ -> failwith "Expected ValidationError"
