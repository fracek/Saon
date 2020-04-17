(**
# Tutorial: Parsing json data

In this tutorial we show how to parse and validate json data.
We will consider an example HTTP endpoint to create orders on a fictional exchange.

The exchange accepts market and limit orders. Both order types have the following properties:

 * **client\_id**: an optional id assigned by the client
 * **type**: `market` or `limit`. Defaults to `limit`
 * **side**: `buy` or `sell`
 * **size**: the order size. To avoid floating point errors, this value is sent as a string
 * **product**: the product being sold or bought

Limit orders expand market orders to include the following properties:

 * **price**: the limit price
 * **time\_in\_force**: optional, can be `gtc` or `fok`. Defaults to `gtc`

A json payload for a market order looks like the following:

    [lang=js]
    {
      "type": "market",
      "client_id": "my-id",
      "side": "buy",
      "size": "120.0",
      "product": "GBP-EUR"
    }

While one for a limit order looks like:

    [lang=js]
    {
      "client_id": "my-id",
      "side": "buy",
      "size": "120.0",
      "price": "1.15",
      "product": "GBP-EUR"
    }



## Initialisation

For this tutorial, we will be using the `System.Text.Json` package to parse json documents, and `Saon` and `Saon.Json`
to parse and validate the data.

*)

#I "../../src/Saon/bin/Release/netstandard2.0/"
#r "Saon.Shared.dll"
#r "Saon.Json.dll"
#r "Saon.Query.dll"
#r "Saon.dll"
open Saon
open Saon.Json
open Saon.Operators
open Saon.Query
open System.Text.Json

(**
## Domain Types

We start by modeling the domain. We try to avoid using types such as `string` or `decimal` for our fields to enforce
stricter type checking by the compiler.

*)


type OrderId = OrderId of string
type Side = Buy | Sell
type ProductId = ProductId of string
type Size = Size of decimal
type Price = Price of decimal
type TimeInForce = GTC | FOK

type LimitOrder =
    { ClientOrderId : OrderId option
      Side : Side
      ProductId : ProductId
      Size : Size
      Price : Price
      TimeInForce : TimeInForce }

type MarketOrder =
    { ClientOrderId : OrderId option
      Side : Side
      ProductId : ProductId
      Size : Size }

type OrderType = Limit | Market

type Order =
    | Market of MarketOrder
    | Limit of LimitOrder

(**
##  Validation and Type Conversion

We define several utility functions to convert from json types (such as strings and numbers) to F# sharp types.

A conversion function from value of type `'T` to value of type `'R` has a signature
like `string -> 'T -> ParserResult<'R>`, where the first parameter is the property name and the second is the
value being converted. The function returns `ParserResult.Success` if the conversion was successful, otherwise it
returns `ParserResult.ValidationFailed` with more information about the error.

*)

let parseOrderType propName = function
    | "limit" -> ParserResult.success OrderType.Limit
    | "market" -> ParserResult.success OrderType.Market
    | _ -> ParserResult.validationFail "orderType" propName "must be 'limit' or 'market'"

let parseSide propName = function
    | "buy" -> ParserResult.success Buy
    | "sell" -> ParserResult.success Sell
    | _ -> ParserResult.validationFail "side" propName "must be 'buy' or 'sell'"

let parseTimeInForce propName = function
    | "gtc" -> ParserResult.success GTC
    | "fok" -> ParserResult.success FOK
    | _ -> ParserResult.validationFail "timeInForce" propName "must be 'gtc' or 'fok'"

let parsePositiveAmount = Convert.stringToDecimal /> Validate.isGreaterThan 0m
let parseSize = parsePositiveAmount /> Convert.withFunction Size
let parsePrice = parsePositiveAmount /> Convert.withFunction Price

(**
To build json objects parsers, we can use the `jsonObjectParser` computational expression.

We use the `Json.property` and `Json.optionalProperty` functions to get and parse required and optional properties
of our json object. The first parameter is the name of the property, while the second parameter is a function
that converts from `JsonElement` to our type.
*)

let parseLimitOrder = jsonObjectParser {
    let! clientId = Json.optionalProperty "client_id" (Json.string /> Convert.withFunction OrderId)
    let! product = Json.property "product" (Json.string /> Convert.withFunction ProductId)
    let! side = Json.property "side" (Json.string /> parseSide)
    let! size = Json.property "size" (Json.string /> parseSize)
    let! price = Json.property "price" (Json.string /> parsePrice)
    let! tif = Json.optionalProperty "time_in_force" (Json.string /> parseTimeInForce)
    return
      { ClientOrderId = clientId
        ProductId = product
        Side = side
        Size = size
        Price = price
        TimeInForce = Option.defaultValue GTC tif }
}

let parseMarketOrder = jsonObjectParser {
    let! clientId = Json.optionalProperty "client_id" (Json.string /> Convert.withFunction OrderId)
    let! product = Json.property "product" (Json.string /> Convert.withFunction ProductId)
    let! side = Json.property "side" (Json.string /> parseSide)
    let! size = Json.property "size" (Json.string /> parseSize)
    return
      { ClientOrderId = clientId
        ProductId = product
        Side = side
        Size = size }
}

(**
Our payload includes the fields for market and limit orders in the root json object. Saon allows us to pass the
current `JsonElement` to an object sub parser by using the `Json.embeddedObject` function.

*)
let parseOrder = jsonObjectParser {
    let! orderType = Json.optionalProperty "type" (Json.string /> parseOrderType)
    match orderType with
    | Some OrderType.Limit | None ->
        let! order = Json.embeddedObject parseLimitOrder
        return Limit order
    | Some OrderType.Market ->
        let! order = Json.embeddedObject parseMarketOrder
        return Market order
}

(**
## Using the parser

We are now ready to test the parser. We tart by defining the json payload and parsing it into a `JsonDocument` object,
then we call `parseOrder` to parse it to obtain a `ParserResult<Order>`.

*)

let parseOrderPayload (payload : string) =
    let document = JsonDocument.Parse(payload)
    parseOrder document.RootElement

let marketOrderPayload = """
{
  "type": "market",
  "client_id": "my-id",
  "side": "buy",
  "size": "120.0",
  "price": "1.15",
  "product": "GBP-EUR"
}
"""
let marketOrder = parseOrderPayload marketOrderPayload

let limitOrderPayload = """
{
  "client_id": "my-id",
  "side": "buy",
  "size": "120.0",
  "price": "1.15",
  "product": "GBP-EUR"
}
"""
let limitOrder = parseOrderPayload limitOrderPayload

(**
If the json payload is malformed, the parser will return a `ValidationFailed` result with more information about the error.

*)

let badOrderTypePayload = """
{
  "type": "invalid",
  "client_id": "my-id",
  "side": "buy",
  "size": "120.0",
  "price": "1.15",
  "product": "GBP-EUR"
}
"""
parseOrderPayload badOrderTypePayload


let sizeIsNotANumber = """
{
  "type": "limit",
  "client_id": "my-id",
  "side": "buy",
  "size": "12O",
  "price": "1.15",
  "product": "GBP-EUR"
}
"""
parseOrderPayload sizeIsNotANumber

(**
## More about the json module

You can find more information about the Json module in the [API reference](reference/saon-json-json.html) section.
*)