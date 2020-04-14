# Saon: parse and validate JSON

[![GitHub Workflow Status](https://img.shields.io/github/workflow/status/fracek/Saon/Build)](https://github.com/fracek/Saon/actions)
[![Nuget](https://img.shields.io/nuget/v/Saon)](https://www.nuget.org/packages/Saon/)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Saon)](https://www.nuget.org/packages/Saon/)


**WARNING: this is an early release of Saon. Expect the API to change.**

## Goals

 - Parse _idiomatic_ JSON payloads into _strongly-typed_ F# objects
 - Continue validating the payload even after one validation fails
 - Provide enough information to produce helpful error response
 
## Usage

For now, refer to the test files to see Saon in action.


## Similar Libraries

Saon was inspired by other libraries:

 - [forma](https://github.com/mrkkrp/forma): an Haskell library to parse and validate JSON forms.
 - [AccidentalFish.FSharp.Validation](https://github.com/JamesRandall/AccidentalFish.FSharp.Validation): an F# validation framework.
 
 
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
 