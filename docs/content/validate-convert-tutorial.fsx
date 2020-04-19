(**
# Tutorial: Validate and convert data

Json and query data is limited to a few basic types: strings, numbers, and booleans.
Saon provides functions to convert from these types to richer F# types.

All conversion functions have the same signature:

*)

type Transformer<'T, 'U> = string -> 'T -> ParserResult<'U>

(*** hide ***)
#I "../../src/Saon/bin/Release/netstandard2.0/"
#r "Saon.Shared.dll"
open Saon

(**
The first parameter is the property or parameter name, the second parameter is the value being converted, and
the result is the discriminated union `ParserResult<'U>` that will contain the new value or an error.

Saon calls transformers where the type of the data does no change _validators_, since the most common use case is
to validate their inputs.

You can find a list of conversion functions in the [`Convert` module reference](reference/saon-convert.html), and
a list of validation functions in the [`Validate` module reference](reference/saon-validate.html).

## Converting between types

As an example, let's take a look at the `Convert.stringToDecimal` function.

*)

Convert.stringToDecimal "my_prop" "123.4567890123456789"

(**
Since the value `"123.4567890123456789" is a properly formatted decimal number, the function will return
`Success 123.4567890123456789M`.

If the value is not a valid number, the function will return a `ValidationFailed` that contains an helpful error
message.


    [lang=fsharp]
    ValidationFailed
        (map [("my_prop", [{ Property = "my_prop"
                             Type = "stringToDecimal"
                             Message = "malformed decimal number" }])])


## Validation

Validators are similar to converters, but they return the original input if the validation was successful, otherwise
they return a `ValidationFailed` with more information.

Saon includes a good amount of validators, you can find an up-to-date list in the
[reference](reference/saon-validate.html).

*)

// This validation will pass
Validate.hasLengthBetween 1 10 "my_array" ["a"; "b"; "c"]

// This will fail
Validate.hasLengthBetween 1 10 "my_string" "this string is too long, will fail :("

(**

## Composing existing transformation functions

Composing transformers is awkward because of the property name parameter. Saon includes a `Parser.pipe` function
that given a function `f` and a function `g`, will pass the result of `f` into `g` only if `f` is successful.

*)

let parsePositiveAmount = Parser.pipe Convert.stringToDecimal (Validate.isGreaterThan 0m)

(**
Composing transformers is such an important function that Saon includes an operator that can help make it easier.
*)
open Saon.Operators

let parseSize = parsePositiveAmount /> Convert.withFunction Size
let parsePrice = parsePositiveAmount /> Convert.withFunction Price

(**

## Defining new transformations

Transformers are just functions so you can easily define your own. As you use Saon in your project, you will grow a
collection of validators tailored to your domain, such as the `parseSize` and `parsePrice` functions above.

*)

let isOdd propName value =
    if value % 2 = 1 then
        ParserResult.success value
    else
        ParserResult.validationFail "isOdd" propName "must be an odd number"

isOdd "my_int" 3