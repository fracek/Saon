module Saon.Shared.Tests.TestConvert


open FsCheck.Xunit
open FsUnit.Xunit
open Saon
open System


[<Property>]
let ``parse valid decimal number returns success`` (n : decimal) =
    let s = n.ToString()
    match Convert.stringToDecimal "s" s with
    | Success v ->
        v |> should equal n
    | _ -> failwith "expected Success"

[<Property>]
let ``parse invalid decimal number returns validation failed`` (n : decimal) =
    let s = n.ToString() + "."
    match Convert.stringToDecimal "s" s with
    | ValidationFailed failMap ->
        failMap.Count |> should equal 1
    | _ -> failwith "expected ValidationFailed"

[<Property>]
let ``parse valid date returns success`` (dt : DateTimeOffset) =
    let pattern = "yyyy-MM-dd'T'HH:mm:ss.FFFK"
    let s = dt.ToString(pattern, System.Globalization.CultureInfo.InvariantCulture)
    match Convert.stringToDateTimeOffsetISO8601 "s" s with
    | Success v ->
        v |> should equal dt
    | _ -> failwith "expected Success"