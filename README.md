# Saon: parse and validate JSON

[![GitHub Workflow Status](https://img.shields.io/github/workflow/status/fracek/Saon/Build)](https://github.com/fracek/Saon/actions)
[![Nuget](https://img.shields.io/nuget/v/Saon)](https://www.nuget.org/packages/Saon/)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Saon)](https://www.nuget.org/packages/Saon/)


**WARNING: this is an early release of Saon. Expect the API to change.**

## Goals

 - Parse _idiomatic_ json payloads into _strongly-typed_ F# objects
 - Convert json types (string, number, object, array) to F# types
 - Continue validating the payload even after one validation fails. This is planned for after FS-1063 lands
 - Provide enough information to produce helpful error response
 
 
## Usage & Tutorials

```f#
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
```

We also provide tutorials to help you get started:

 - [Use Saon with Giraffe](https://fracek.github.io/Saon/giraffe-tutorial.html)
 - [Learn how to define json parsers](https://fracek.github.io/Saon/json-tutorial.html)
 - [Learn how to parse query strings](https://fracek.github.io/Saon/query-tutorial.html)
 - [Learn how to validate and convert data](https://fracek.github.io/Saon/validate-convert-tutorial.html)


## Similar Libraries

Saon was inspired by other libraries:

 - [forma](https://github.com/mrkkrp/forma): an Haskell library to parse and validate json forms
 - [AccidentalFish.FSharp.Validation](https://github.com/JamesRandall/AccidentalFish.FSharp.Validation): an F# validation framework
 - [Chiron](https://github.com/xyncro/chiron): also uses computational expressions to parse json

 
## LICENSE

    Copyright 2020 Francesco Ceccon
    
    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at
    
        http://www.apache.org/licenses/LICENSE-2.0
    
    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
 