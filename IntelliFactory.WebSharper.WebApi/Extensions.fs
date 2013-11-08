// $begin{copyright}
//
// Copyright (c) 2008-2013 IntelliFactory
//
// GNU Affero General Public License Usage
// WebSharper is free software: you can redistribute it and/or modify it under
// the terms of the GNU Affero General Public License, version 3, as published
// by the Free Software Foundation.
//
// WebSharper is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License
// for more details at <http://www.gnu.org/licenses/>.
//
// If you are unsure which license is appropriate for your use, please contact
// IntelliFactory at http://intellifactory.com/contact.
//
// $end{copyright}

namespace IntelliFactory.WebSharper.WebApi

[<AutoOpen>]
module Extensions =
    open System
    open System.IO
    open System.Reflection
    open System.Web.Http
    module M = IntelliFactory.WebSharper.Core.Metadata
    open IntelliFactory.WebSharper.Sitelets

    type Assembly =

        static member LoadFileInfo(info: FileInfo) =
            let name = AssemblyName.GetAssemblyName(info.FullName)
            match Assembly.TryLoad(name) with
            | None -> Assembly.LoadFrom(info.FullName)
            | Some a -> a

        static member TryLoad(name: AssemblyName) =
            try
                match Assembly.Load(name) with
                | null -> None
                | a -> Some a
            with _ -> None

    type DirectoryInfo with

        member dir.DiscoverAssemblies() =
            let ls pat = dir.EnumerateFiles(pat)
            let ( @ ) = Seq.append
            ls "*.dll" @ ls "*.exe"

    type M.Info with

        static member LoadFromBinDirectory(binDirectory: string) =
            let d = DirectoryInfo(binDirectory)
            d.DiscoverAssemblies()
            |> Seq.choose (fun f -> M.AssemblyInfo.Load(f.FullName))
            |> M.Info.Create

        static member LoadFromWebRoot(webRoot: string) =
            M.Info.LoadFromBinDirectory(Path.Combine(webRoot, "bin"))

    type HttpConfiguration with

        member conf.RegisterDefaultSitelet
            (
                webRoot: string,
                sitelet: Sitelet<'T>
            ) =
            let meta = M.Info.LoadFromWebRoot(webRoot)
            SiteletHost.Options.Create(conf, meta)
                .WithServerRootDirectory(webRoot)
                .Register(sitelet)
            RemotingHost.Options.Create(conf, meta).Register()

        member conf.RegisterDiscoveredSitelet(webRoot: string) =
            let binDir = DirectoryInfo(Path.Combine(webRoot, "bin"))
            let ok =
                binDir.DiscoverAssemblies()
                |> Seq.tryPick (fun assem ->
                    let assem = Assembly.LoadFileInfo(assem)
                    let aT = typeof<WebsiteAttribute>
                    match Attribute.GetCustomAttribute(assem, aT) with
                    | :? WebsiteAttribute as attr ->
                        let (sitelet, actions) = attr.Run()
                        conf.RegisterDefaultSitelet(webRoot, sitelet)
                        Some ()
                    | _ -> None)
            match ok with
            | None -> failwith "Failed to discover sitelet assemblies"
            | Some () -> ()
