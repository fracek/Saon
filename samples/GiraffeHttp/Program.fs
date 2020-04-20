open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe


module Model =
    type Email = Email of string
    type Phone = Phone of string * string
    type LookupType = LookupName | LookupEmail
    type Lookup =
        | LookupName of string
        | LookupEmail of Email

    [<RequireQualifiedAccess>]
    type Contact =
        | Email of Email
        | Phone of Phone

    type ContactDetails =
        { Name : string
          Contact : Contact }


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

    let parseContact = jsonObjectParser {
        let! typ = Json.property "type" Json.string
        match typ with
        | "email" ->
            let! email = Json.property "email" (Json.string /> parseEmail)
            return Contact.Email email
        | _ ->
            let! prefix = Json.property "prefix" (Json.string /> Validate.hasMinLength 2)
            let! number = Json.property "number" (Json.string /> Validate.hasMinLength 3)
            return Contact.Phone (Phone (prefix, number))
    }

    let parseContactDetails = jsonObjectParser {
        let! name = Json.property "name" (Json.string /> Validate.isNotEmptyOrWhitespace)
        let! contact =
            [ "email", Json.string /> parseEmail /> Convert.withFunction Contact.Email
              "contact", Json.object parseContact ]
            |> Json.oneOf
        return { Name = name; Contact = contact }
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
            handleParserResult (parse ctx.Request.Query) handler next ctx


module Handler =
    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> setStatusCode 500 >=> json {| Message = ex.Message |}

    let createContact (contact : Model.ContactDetails) =
        fun next (ctx : HttpContext) ->
            let logger = ctx.GetLogger()
            logger.LogInformation(sprintf "Create contact %A" contact)
            json {| Message = "ok" |} next ctx

    let lookupContact (filter : Model.Lookup) =
        fun next (ctx : HttpContext) ->
            let logger = ctx.GetLogger()
            logger.LogInformation(sprintf "Lookup contact %A" filter)
            match filter with
            | Model.Lookup.LookupEmail _ ->
                json {| Message = "lookup by email" |} next ctx
            | Model.Lookup.LookupName _ ->
                json {| Message = "lookup by name" |} next ctx


let webApp =
    choose [
        POST >=> route "/" >=> Helper.bindJson Dto.parseContactDetails Handler.createContact
        GET >=> route "/" >=> Helper.bindQuery Dto.parseLookup Handler.lookupContact
    ]


let configureApp (app : IApplicationBuilder) =
    app.UseGiraffeErrorHandler(Handler.errorHandler)
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore


[<EntryPoint>]
let main _ =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    |> ignore)
        .Build()
        .Run()
    0