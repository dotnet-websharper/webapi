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
    open System.IO
    open System.Web.Http
    module M = IntelliFactory.WebSharper.Core.Metadata
    open IntelliFactory.WebSharper.Sitelets

    type M.Info with

        static member LoadFromBinDirectory(binDirectory: string) =
            let ls pat = Directory.EnumerateFiles(binDirectory, pat)
            let ( @ ) = Seq.append
            ls "*.dll" @ ls "*.exe"
            |> Seq.choose (fun f -> M.AssemblyInfo.Load(f))
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
