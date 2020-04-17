(**
# Tutorial: Using Saon with Giraffe

In this tutorial you will learn how to use Saon together with Giraffe or Saturn.

We start by defining the model for our application and the json and query parsers. You can read more about the
[json parser](json-tutorial.html) and [query parser](query-tutorial.html) in their respective tutorials.

For now, the important thing to note is that `parseContactDetails` has signature
`JsonElement -> ParserResult<Model.ContactDetails>` and `parseLookup` has signature
`IQueryCollection -> ParserResult<Model.Lookup>`.

*)


module Model =
    type Email = Email of string
    type LookupType = LookupName | LookupEmail
    type Lookup =
        | LookupName of string
        | LookupEmail of Email

    type ContactDetails =
        { Name : string
          Email : Email }


module Dto =
    open Model
    open Saon
    open Saon.Query
    open Saon.Json
    open Saon.Operators

    let parseEmail =
        Validate.isNotEmptyOrWhitespace
        /> Validate.isEmail
        /> Convert.withFunction Email

    let parseContactDetails = jsonObjectParser {
        let! name = Json.property "name" (Json.string /> Validate.isNotEmptyOrWhitespace)
        let! email = Json.property "email" (Json.string /> parseEmail)
        return { Name = name; Email = email }
    }

    let parseLookupType paramName = function
        | "name" -> ParserResult.success LookupType.LookupName
        | "email" -> ParserResult.success LookupType.LookupEmail
        | _ -> ParserResult.validationFail "type" paramName "must be one of 'name', 'email'"

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
## Helper function to parse json payloads and query strings

We now define a couple of helper functions that you can re-use in your application. The functions are similar to
Giraffe own `bindJson` and `bindQueryString`, the difference is that they take a Saon parser as an additional parameter.

*)
module Helper =
    open Microsoft.AspNetCore.Http.Features
    open Saon
    open System.Text.Json

    let internal badRequest payload : HttpHandler =
        clearResponse >=> setStatusCode 400 >=> json payload

    let internal handleParserResult (result : ParserResult<'T>) (handler : 'T -> HttpHandler) : HttpHandler =
        match result with
        | Success value -> handler value
        | ParsingFailed (field, msg) ->
            badRequest {| Message = msg |}
        | ValidationFailed failMap ->
            badRequest {| Message = "validation failed"; Errors = failMap |}

    let bindJson<'T> (parse : JsonElement -> ParserResult<'T>) (handler : 'T -> HttpHandler) : HttpHandler =
        fun next (ctx : HttpContext) ->
            task {
                let! document = JsonDocument.ParseAsync(ctx.Request.Body)
                return! handleParserResult (parse document.RootElement) handler next ctx
            }

    let bindQuery<'T> (parse : IQueryCollection -> ParserResult<'T>) (handler : 'T -> HttpHandler) : HttpHandler =
        fun next (ctx : HttpContext) ->
            task {
                return! handleParserResult (parse ctx.Request.Query) handler next ctx
            }

(**
## Http handling

We can now use the helper functions together with the parsers to define the http handlers.

*)

module Handler =
    let createContact (contact : Model.ContactDetails) =
        fun next (ctx : HttpContext) ->
            let logger = ctx.GetLogger()
            logger.LogInformation(sprintf "Create contact %A" contact)
            json {| Message = "ok" |} next ctx

    let lookupContact (filter : Model.Lookup) =
        fun next (ctx : HttpContext) ->
            let logger = ctx.GetLogger()
            logger.LogInformation(sprintf "Lookup contact %A" filter)
            json {| Message = "ok" |} next ctx


let webApp =
    choose [
        POST >=> route "/" >=> Helper.bindJson Dto.parseContactDetails Handler.createContact
        GET >=> route "/" >=> Helper.bindQuery Dto.parseLookup Handler.lookupContact
    ]

(**
## Testing it out

We can start by sending valid http requests to the endpoints to see everything is working well on the happy path.


    POST http://localhost:5000/
    Content-Type: application/json

    { "name": "Su", "email": "su@foo.bar" }

    =>

    HTTP/1.1 200 OK
    Date: Fri, 17 Apr 2020 17:02:47 GMT
    Content-Type: application/json; charset=utf-8
    Server: Kestrel
    Content-Length: 16

    {
      "message": "ok"
    }


And


    GET http://localhost:5000/?type=email&value=su@foo.bar

    =>

    HTTP/1.1 200 OK
    Date: Fri, 17 Apr 2020 17:03:21 GMT
    Content-Type: application/json; charset=utf-8
    Server: Kestrel
    Content-Length: 29

    {
      "message": "lookup by email"
    }


If we send the wrong payload to the create endpoint, we will receive a detailed error message


    POST http://localhost:5000/
    Content-Type: application/json

    { "name": "", "email": "su@foo.bar" }

    =>

    HTTP/1.1 400 Bad Request
    Date: Fri, 17 Apr 2020 17:05:15 GMT
    Content-Type: application/json; charset=utf-8
    Server: Kestrel
    Content-Length: 147

    {
      "errors": {
        "name": [
          {
            "property": "name",
            "type": "isNotEmptyOrWhitespace",
            "message": "must be not empty or whitespace"
          }
        ]
      },
      "message": "validation failed"
    }


Likewise for the lookup endpoint


    GET http://localhost:5000/?type=email&value=su

    =>

    HTTP/1.1 400 Bad Request
    Date: Fri, 17 Apr 2020 17:05:57 GMT
    Content-Type: application/json; charset=utf-8
    Server: Kestrel
    Content-Length: 132

    {
      "errors": {
        "value": [
          {
            "property": "value",
            "type": "isEmail",
            "message": "must be a valid email address"
          }
        ]
      },
      "message": "validation failed"
    }
*)