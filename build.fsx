#r "paket:
nuget FSharp.Core ~> 4.7.0
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
//"
#load ".fake/build.fsx/intellisense.fsx"
#nowarn "52"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators


module Paths =
  let root = __SOURCE_DIRECTORY__
  let sln = root </> "Saon.sln"
  let out = root </> "bin"
  let nugetOut = root </> "nuget"


Target.create "Clean" (fun _ ->
  !! "**/bin"
  ++ "**/obj"
  |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
  DotNet.build id Paths.sln
)

Target.create "Pack" (fun _ ->
  DotNet.pack (fun o ->
    { o with
        OutputPath = Some Paths.nugetOut
    }
  ) Paths.sln
)

Target.create "Test" (fun _ ->
  DotNet.test (fun o ->
    { o with
        Configuration = DotNet.BuildConfiguration.Release
        ResultsDirectory = Some Paths.out
    }
  ) Paths.sln
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "Test"
  ==> "Pack"
  ==> "All"

Target.runOrDefault "All"