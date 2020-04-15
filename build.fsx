#r "paket: groupref build //"
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
  let releaseNotes = root </> "RELEASE_NOTES.md"
  let out = root </> "bin"
  let nugetOut = root </> "nuget"


let release = ReleaseNotes.load Paths.releaseNotes

let releaseProperties =
  [ "Version", release.NugetVersion
    "PackageReleaseNotes", String.concat "\n" release.Notes ]

Target.create "Clean" (fun _ ->
  !! "**/bin"
  ++ "**/obj"
  |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
  DotNet.build id Paths.sln
)

Target.create "Test" (fun _ ->
  DotNet.test (fun o ->
    { o with
        Configuration = DotNet.BuildConfiguration.Release
        ResultsDirectory = Some Paths.out
    }
  ) Paths.sln
)

Target.create "Pack" (fun _ ->
  DotNet.pack (fun o ->
    { o with
        Configuration = DotNet.BuildConfiguration.Release
        OutputPath = Some Paths.nugetOut
        MSBuildParams = { o.MSBuildParams with Properties = releaseProperties }
    }
  ) Paths.sln
)

Target.create "NugetPush" (fun _ ->
  let nugetApiKey = Environment.environVar "NUGET_API_KEY" |> Some
  let nugetSource = Some "https://api.nuget.org/v3/index.json"
  let nugetPackages = Paths.nugetOut </> sprintf "Saon.*-%s.nupkg" release.NugetVersion
  DotNet.nugetPush (fun o ->
    { o with
        PushParams = { o.PushParams with
                           ApiKey = nugetApiKey
                           Source = nugetSource }
    }
  ) nugetPackages
)

Target.create "Default" ignore
Target.create "Release" ignore

"Clean"
  ==> "Build"
  ==> "Test"
  ==> "Default"

"Default"
  ==> "Pack"
  ==> "NugetPush"
  ==> "Release"

Target.runOrDefault "Default"