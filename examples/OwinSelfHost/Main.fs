namespace MiniOwin

open System.IO
open System.Web.Http
open global.Owin
open Microsoft.Owin.Hosting
open Microsoft.Owin.Extensions
open Microsoft.Owin.Helpers
open IntelliFactory.WebSharper
open IntelliFactory.WebSharper.WebApi

[<AutoOpen>]
module Start =

    let webRoot =
        Path.Combine(__SOURCE_DIRECTORY__, "..", "Sitelets")
        |> Path.GetFullPath

[<Sealed>]
type Startup() =

    member __.Configuration(builder: IAppBuilder) =
        printfn "Configuring with %s" webRoot
        let conf = new HttpConfiguration()
        let s = global.Sitelets.Site.Main
        let assemblies =
            [
                typeof<global.Sitelets.Action>.Assembly
            ]
        let meta =
            Core.Metadata.Info.Create [
                for a in assemblies do
                    match Core.Metadata.AssemblyInfo.LoadReflected a with
                    | None -> ()
                    | Some info -> yield info
            ]
        SiteletHosting.Options.Create(conf, meta)
            .WithServerRootDirectory(webRoot)
            .Register(s)
        Remoting.Options.Create(conf, meta)
            .Register()
        let builder =
            builder
                .UseStaticFiles(webRoot)
                .UseWebApi(conf)
        ()

module Main =

    [<EntryPoint>]
    let Start args =
        let url = "http://localhost:9000/"
        use server = WebApp.Start<Startup>(url)
        stdout.WriteLine("Serving {1} at {0}", url, webRoot)
        stdin.ReadLine() |> ignore
        0
