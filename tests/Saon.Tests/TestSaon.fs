module Saon.Tests.TestSaon


open FsUnit.Xunit
open System
open System.Text.Json
open System.Text.Json
open Saon
open Saon.Parse.Operators
open Xunit


type ProductId = ProductId of string * string
type OrderId = OrderId of string
type Liquidity = Maker | Taker
type Side = Buy | Sell

type Fill =
    { TradeId : int64
      ProductId : ProductId
      Price : decimal
      Size : decimal
      OrderId : OrderId
      CreatedAt : DateTimeOffset
      Liquidity : Liquidity
      Fee : decimal
      Settled : bool
      Side : Side }

type ReportParams =
    { StartDate : DateTimeOffset
      EndDate : DateTimeOffset }

type Report =
    { CompletedAt : DateTimeOffset option
      FileUrl : string option
      Params : ReportParams }


let validateProductId propName (value : string) =
    match value.Split('-') with
    | [| p1; p2 |] -> ProductId (p1, p2) |> Result.success
    | _ -> Result.validationFail "product_id" propName "Invalid product_id format"

let validateSide propName (value : string) =
    match value with
    | "buy" -> Result.success Buy
    | "sell" -> Result.success Sell
    | _ -> Result.validationFail "side" propName "Invalid side"

let validateLiquidity propName (value : string) =
    match value with
    | "T" -> Result.success Taker
    | "M" -> Result.success Maker
    | _ -> Result.validationFail "liquidity" propName "Invalid liquidity"

let stringToDecimal propName (value : string) =
    let parsed, decimalValue = Decimal.TryParse(value)
    if parsed then
        Result.success decimalValue
    else
        Result.parsingFail propName "Malformed decimal number"

let stringToDateTimeOffset propName (value : string) =
    let parsed, dt = DateTimeOffset.TryParse(value)
    if parsed then
        Result.success dt
    else
        Result.parsingFail propName "Malformed datetime"


let parseFill = createRecordParser<Fill> () {
    let! tradeId = property "trade_id" Parse.int64
    let! productId = property "product_id" (Parse.string /> validateProductId)
    let! price = property "price" (Parse.string /> stringToDecimal)
    let! size = property "size" (Parse.string /> stringToDecimal)
    let! orderId = property "order_id" Parse.string
    let! createdAt = property "created_at" (Parse.string /> stringToDateTimeOffset)
    let! liquidity = property "liquidity" (Parse.string /> validateLiquidity)
    let! fee = property "fee" (Parse.string /> stringToDecimal)
    let! settled = property "settled" Parse.bool
    let! side = property "side" (Parse.string /> validateSide)
    return
        { TradeId = tradeId
          ProductId = productId
          Price = price
          Size = size
          OrderId = OrderId orderId
          CreatedAt = createdAt
          Liquidity = liquidity
          Fee = fee
          Settled = settled
          Side = side }
}

let parseFills = Parse.list parseFill

let parseReportParams = createRecordParser<ReportParams> () {
    let! startDate = property "start_date" (Parse.string /> stringToDateTimeOffset)
    let! endDate = property "end_date" (Parse.string /> stringToDateTimeOffset)
    return { StartDate = startDate; EndDate = endDate }
}


let parseReport = createRecordParser<Report> () {
    let! completedAt = optionalProperty "completed_at" (Parse.string /> stringToDateTimeOffset)
    let! fileUrl = optionalProperty "file_url" Parse.string
    let! parameters = property "params" parseReportParams
    return
        { CompletedAt = completedAt
          FileUrl = fileUrl
          Params = parameters }
}


[<Fact>]
let ``parse fill`` () =
    let json = """
    [{
        "trade_id": 74,
        "product_id": "BTC-USD",
        "price": "10.00",
        "size": "0.01",
        "order_id": "d50ec984-77a8-460a-b958-66f114b0de9b",
        "created_at": "2014-11-07T22:19:28.578544Z",
        "liquidity": "T",
        "fee": "0.00025",
        "settled": true,
        "side": "buy"
    }]"""
    let document = JsonDocument.Parse(json)
    match (createRootParser parseFills) document with
    | Success [fill] ->
        fill.Side |> should equal Buy
    | _ -> failwith "Expected Success"

[<Fact>]
let ``parse report`` () =
    let json = """
    {
        "id": "0428b97b-bec1-429e-a94c-59232926778d",
        "type": "fills",
        "status": "pending",
        "created_at": "2015-01-06T10:34:47.000Z",
        "expires_at": "2015-01-13T10:35:47.000Z",
        "params": {
            "start_date": "2014-11-01T00:00:00.000Z",
            "end_date": "2014-11-30T23:59:59.000Z"
        }
    }"""
    let document = JsonDocument.Parse(json)
    match (createRootParser parseReport) document with
    | Success report ->
        report.CompletedAt |> Option.isNone |> should be True
        report.FileUrl |> Option.isNone |> should be True
        report.Params.StartDate.Year |> should equal 2014
    | _ -> failwith "Expected Success"
