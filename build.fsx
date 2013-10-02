#load "tools/includes.fsx"
open IntelliFactory.Build

let bt = BuildTool().PackageId("WebSharper.WebApi", "2.5-alpha")

let main =
    bt.FSharp.Library("IntelliFactory.WebSharper.WebApi")
        .SourcesFromProject()
        .References(fun r ->
            let wsPaths =
                [
                    "tools/net45/IntelliFactory.Html.dll"
                    "tools/net45/IntelliFactory.WebSharper.dll"
                    "tools/net45/IntelliFactory.WebSharper.Core.dll"
                    "tools/net45/IntelliFactory.WebSharper.Sitelets.dll"
                    "tools/net45/IntelliFactory.WebSharper.Web.dll"
                ]
            [
                r.Assembly("System.Configuration")
                r.Assembly("System.Web")
                r.NuGet("Microsoft.AspNet.WebApi.Core")
                    .Version("4.0.30506.0")
                    .Reference()
                r.NuGet("WebSharper").At(wsPaths).Reference()
            ])

bt.Solution [
    main

    bt.NuGet.CreatePackage()
        .Id("WebSharper.WebApi")
        .Add(main)
]
|> bt.Dispatch
