module Saon.Json.Tests.TestParser

open FsUnit.Xunit
open Saon
open Saon.Operators
open Saon.Json
open System
open System.Text.Json
open Xunit

type A = {B : int64; C : string }
type R = { A : A; D : string }

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

let parseListOfStrings = Json.list (Json.string /> Validate.isNotEmptyOrWhitespace)
let parseListOfObjects = Json.list (Json.object parseA)


[<Fact>]
let ``parse nested records`` () =
    let json = """ { "a": { "b": 10, "c": "nice" }, "d": "foobar" } """
    let document = JsonDocument.Parse(json)
    match (createRootParser parseB) document with
    | Success r ->
        r.A.B |> should equal 10L
        r.A.C |> should equal "nice"
        r.D |> should equal "foobar"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``validation error on nested field`` () =
    let json = """ { "a": { "b": 10, "c": "" }, "d": "foobar" } """
    let document = JsonDocument.Parse(json)
    match (createRootParser parseB) document with
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
    match (createRootParser <| parseListOfStrings "") document with
    | Success xs ->
        xs.Length |> should equal 3
        xs.Head |> should equal "a"
        xs.Tail.Tail.Head |> should equal "c"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``validation error on list of strings`` () =
    let json = """ ["a", "", "c", ""] """
    let document = JsonDocument.Parse(json)
    match (createRootParser <| parseListOfStrings "") document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 2
        Map.tryFind "[1]" failMap |> Option.isSome |> should be True
        Map.tryFind "[3]" failMap |> Option.isSome |> should be True
    | _ -> failwith "Expected ValidationFailed"


[<Fact>]
let ``parse list of objects`` () =
    let json = """ [{"b": 10, "c": "xxx"}] """
    let document = JsonDocument.Parse(json)
    match (createRootParser <| parseListOfObjects "") document with
    | Success [r] ->
        r.B |> should equal 10L
        r.C |> should equal "xxx"
    | _ -> failwith "Expected Success"


[<Fact>]
let ``validation error on list of objects`` () =
    let json = """ [{"b": 10, "c": "xxx"}, {"b": -2, "c": ""}] """
    let document = JsonDocument.Parse(json)
    match (createRootParser <| parseListOfObjects "") document with
    | ValidationFailed failMap ->
        Map.count failMap |> should equal 1
        Map.tryFind "[1].c" failMap |> Option.isSome |> should be True
    | _ -> failwith "Expected ValidationFailed"
