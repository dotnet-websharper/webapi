namespace OwinSelfHost

open System.IO
open System.Web.Http
open global.Owin
open Microsoft.Owin.Hosting
open Microsoft.Owin.Extensions
open Microsoft.Owin.Helpers
open IntelliFactory.WebSharper
open IntelliFactory.WebSharper.WebApi

[<Sealed>]
type Startup() =

    member __.Configuration(appB: IAppBuilder) =
        let webRoot =
            Path.Combine(__SOURCE_DIRECTORY__, "..", "Sitelets")
            |> Path.GetFullPath
        printfn "Configuring with %s" webRoot
        let conf =
            let c = new HttpConfiguration()
            let s = global.Sitelets.Site.Main
            c.RegisterDefaultSitelet(webRoot, s)
            c
        appB.UseStaticFiles(webRoot)
            .UseWebApi(conf)
        |> ignore

module Main =

    [<EntryPoint>]
    let Start args =
        let url = "http://localhost:9000/"
        use server = WebApp.Start<Startup>(url)
        stdout.WriteLine("Serving {0}", url)
        stdin.ReadLine() |> ignore
        0
