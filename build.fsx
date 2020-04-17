#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"
#nowarn "52"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System.IO


module Paths =
    let root = __SOURCE_DIRECTORY__
    let sln = root </> "Saon.sln"
    let releaseNotes = root </> "RELEASE_NOTES.md"
    let out = root </> "bin"
    let nugetOut = root </> "nuget"
    let packages = root </> "packages"
    let docs = root </> "docs"
    let docsOut = docs </> "output"


module ProjectInfo =
    let description = ""
    let authors = [ "Francesco Ceccon" ]
    let github = "https://github.com/fracek/saon"


let release = ReleaseNotes.load Paths.releaseNotes

let releaseProperties =
    [ "Version", release.NugetVersion
      "PackageReleaseNotes",
      String.concat "\n" release.Notes ]

Target.create "Clean" (fun _ -> !!"**/bin" ++ "**/obj" |> Shell.cleanDirs)

Target.create "Build" (fun _ -> DotNet.build id Paths.sln)

Target.create "Test" (fun _ ->
    DotNet.test (fun o ->
        { o with
              Configuration = DotNet.BuildConfiguration.Release
              ResultsDirectory = Some Paths.out }) Paths.sln)

Target.create "GenerateDocs" (fun _ ->
    Shell.cleanDir Paths.docsOut
    Directory.ensure Paths.docsOut
    let docsContent = Paths.docs </> "content"

    let toolDir = Paths.packages </> "fsharpformattingbuild" </> "FSharp.Formatting.CommandTool" </> "tools"
    let toolPath = ProcessUtils.findLocalTool "FSFORMATTING" "fsformatting.exe" [ toolDir ]

    let docsTemplate = "docpage.cshtml"

    let layoutRoots =
        [ Paths.docs </> "templates"
          Paths.docs </> "templates" </> "reference" ]

    let projInfo =
        [ "project-name", "Saon"
          "page-description", ProjectInfo.description
          "page-author", String.separated ", " ProjectInfo.authors
          "github-link", ProjectInfo.github
          "root", "" ]

    Shell.copy docsContent [ Paths.root @@ "RELEASE_NOTES.md" ]
    Shell.copyDir Paths.docsOut (Paths.docs </> "assets") FileFilter.allFiles

    FSFormatting.createDocs (fun s ->
        { s with
              Source = docsContent
              OutputDirectory = Paths.docsOut
              Template = docsTemplate
              ProjectParameters = projInfo
              LayoutRoots = layoutRoots
              FsiEval = true
              ToolPath = toolPath })

    let dllPatterns =
        [ !!"src/Saon.Shared/bin/Release/**/Saon.Shared.dll"
          !!"src/Saon.Json/bin/Release/**/Saon.Json.dll"
          !!"src/Saon.Query/bin/Release/**/Saon.Query.dll"
          !!"src/Saon/bin/Release/**/Saon.dll" ]

    let apiDocsOut = Paths.docsOut </> "reference"
    Directory.ensure apiDocsOut

    let dlls =
        dllPatterns
        |> Seq.map (GlobbingPattern.setBaseDir Paths.root)
        |> Seq.concat
        |> List.ofSeq

    let libDirs =
        dlls
        |> Seq.map Path.GetDirectoryName
        |> Seq.distinct
        |> List.ofSeq

    FSFormatting.createDocsForDlls (fun s ->
        { s with
              OutputDirectory = apiDocsOut
              LayoutRoots = layoutRoots
              LibDirs = libDirs
              ProjectParameters = projInfo
              SourceRepository = ProjectInfo.github + "/blob/master" }) dlls)


Target.create "Pack" (fun _ ->
    DotNet.pack (fun o ->
        { o with
              Configuration = DotNet.BuildConfiguration.Release
              OutputPath = Some Paths.nugetOut
              MSBuildParams = { o.MSBuildParams with Properties = releaseProperties } }) Paths.sln)

Target.create "NugetPush" (fun _ ->
    let nugetApiKey = Environment.environVar "NUGET_API_KEY" |> Some
    let nugetSource = Some "https://api.nuget.org/v3/index.json"
    let nugetPackages = Paths.nugetOut </> sprintf "Saon.*%s.nupkg" release.NugetVersion
    DotNet.nugetPush (fun o ->
        { o with
              PushParams =
                  { o.PushParams with
                        ApiKey = nugetApiKey
                        Source = nugetSource } }) nugetPackages)

Target.create "Default" ignore
Target.create "Release" ignore

"Clean" ==> "Build" ==> "Test" ==> "Default"

"Default"
"Default"
"Default"
"Default" ==> "Pack" ==> "NugetPush" ==> "Release"

Target.runOrDefault "Default"
