module Saon.Shared.Tests.TestValidate


open FsCheck.Xunit
open FsUnit.Xunit
open Saon
open System
open Xunit


module ``test isNotNull`` =
    [<Fact>]
    let ``passes for non null objects`` () =
        match Validate.isNotNull "s" "foobar" with
        | Success _ -> ()
        | _ -> failwith "expected Success"

    [<Fact>]
    let ``fails for null objects`` () =
        match Validate.isNotNull "s" null with
        | ValidationFailed _ -> ()
        | _ -> failwith "expected ValidationFailed"


module ``test hasMinLength`` =
    [<Fact>]
    let ``works with strings`` () =
        match Validate.hasMinLength 5 "" "foobar" with
        | Success _ ->
            ()
        | _ -> failwith "expected Success"
        
    [<Fact>]
    let ``works with sequences`` () =
        match Validate.hasMinLength 3 "" [1; 2; 3] with
        | Success _ ->
            ()
        | _ -> failwith "expected Success"
        
        
module ``test isIn`` =
    [<Fact>]
    let ``works with lists`` () =
        match Validate.isIn [1; 2; 3] "" 3 with
        | Success 3 ->
            ()
        | _ -> failwith "expected Success"
        