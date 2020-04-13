module Saon.Tests.TestParse

open FsCheck.Xunit
open FsUnit.Xunit
open Saon
open System.Text.Json
open Xunit


[<Fact>]
let ``parse a valid string is always successfull`` () =
    let element = JsonDocument.Parse("\"foobar\"").RootElement
    match Parse.string "prop" element with
    | Success _ -> ()
    | _ -> failwith "Expected Success result"


[<Fact>]
let ``parse a number as string is an error`` () =
    let element = JsonDocument.Parse("42").RootElement
    match Parse.string "prop" element with
    | ParsingFailed (field, msg) ->
        field |> should equal (Some "prop")
        msg.Length |> should greaterThan 0
    | _ ->
        failwith "Expected ParsingFailed result"
