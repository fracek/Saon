(**
# Saon: catchy description here

Saon is an F# library to parse and validate json payloads and query strings. It builds on top of the `System.Text.Json`
and `Microsoft.AspNetCore.Http.Features` packages.

**WARNING**: despite the version number, Saon is still in the very early stages. I'm still experimenting with the
API since I'm not satisfied how the json converters and validation functions are composed together.

### How to get Saon

Saon is ready to [download from Nuget](https://www.nuget.org/packages/Saon/).
At the moment, there are four package you can download:

 * `Saon`: this is an umbrella package that includes all the other packages. If you're using Saon in an Asp.Net Core
    application, you should use this package
 * `Saon.Json`: this package contains the functions to parse json data
 * `Saon.Query`: this package contains the functions to parse query strings
 * `Saon.Shared`: this is the core package that defines the base types and functions

*)


(*** hide ***)
#I "../../src/Saon/bin/Release/netstandard2.0/"
#I "../../packages/fsharpformattingbuild/Microsoft.AspNetCore.Http.Features/lib/netstandard2.0"
#I "../../packages/fsharpformattingbuild/Microsoft.Extensions.Primitives/lib/netstandard2.0"
#r "Saon.Shared.dll"
#r "Saon.Json.dll"
#r "Saon.Query.dll"
#r "Saon.dll"
#r "Microsoft.Extensions.Primitives"
#r "Microsoft.AspNetCore.Http.Features"
open Saon
open Saon.Json
open Saon.Operators
open Saon.Query
open System.Text.Json


(**
## Using with Giraffe

Saon can be easily integrated with Giraffe to provide json and query strings parsing and validation. You can find
more information in the [tutorial](giraffe-tutorial.html).
*)

let webApp =
    choose [
        POST >=> route "/" >=> Helper.bindJson Dto.parseContactDetails Handler.createContact
        GET >=> route "/" >=> Helper.bindQuery Dto.parseLookup Handler.lookupContact
    ]

(**
## Parsing json payloads

One of the goals of this library is to enable developers to parse *idiomatic* json payloads and convert them to
strictly typed F# objects.

*)

type Email = Email of string
type ContactDetails =
    { Name : string
      Email : Email }

let parseEmail =
    Validate.isNotEmptyOrWhitespace
    /> Validate.isEmail
    /> Convert.withFunction Email

let parseContactDetails = jsonObjectParser {
    let! name = Json.property "name" (Json.string /> Validate.isNotEmptyOrWhitespace)
    let! email = Json.property "email" (Json.string /> parseEmail)
    return { Name = name; Email = email }
}

(**
You can find more information about the json module in the [tutorial](json-tutorial.html).

## Parsing query strings

Saon includes a module to parse query strings represented by the `IQueryStringCollection` interface.
*)

type LookupType = Name | Email

let parseLookupType paramName = function
    | "name" -> ParserResult.success Name
    | "email" -> ParserResult.success Email
    | _ -> ParserResult.validationFail "type" paramName "must be one of 'name', 'email'"

type Lookup =
    | Name of string
    | Email of string

let parseLookup = queryParser {
    let! lookupType = Query.parameter "type" (Query.string /> parseLookupType)
    match lookupType with
    | LookupType.LookupName ->
        let! value = Query.parameter "value" Query.string
        return LookupName value
    | LookupType.LookupEmail ->
        let! value = Query.parameter "value" (Query.string /> Validate.isEmail)
        return Email value |> LookupEmail
}

(**
You can find more information about the query module in the [tutorial](query-tutorial.html).

## Validation and conversion

Saon includes a range of validation and conversion functions. You can use them as building block to build your
very own parsers! You can read more in the [tutorial](validate-convert-tutorial.html).

*)

let parsePositiveAmount = Convert.stringToDecimal /> Validate.isGreaterThan 0m
let parseSize = parsePositiveAmount /> Convert.withFunction Size
let parsePrice = parsePositiveAmount /> Convert.withFunction Price

(**

## Contributing and License

This library is published under the Apache-2.0 license. You can find a copy of the
[license in the repository](https://github.com/fracek/Saon).

The project is [hosted on GitHub](https://github.com/fracek/Saon), where you can [report issues](https://github.com/fracek/Saon/issues)
and [open pull requests](https://github.com/fracek/Saon/pulls).
*)