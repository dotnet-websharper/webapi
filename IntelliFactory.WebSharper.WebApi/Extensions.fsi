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

/// Utility extension methods providing a facade to common setup scenarios.
[<AutoOpen>]
module Extensions =
    open System.IO
    open System.Web.Http
    module M = IntelliFactory.WebSharper.Core.Metadata
    open IntelliFactory.WebSharper.Sitelets

    type M.Info with

        /// Reads all assemblies under `bin`, discovering WebSharper ones.
        static member LoadFromBinDirectory :
            binDirectory: string -> M.Info

        /// Reads all assemblies under `${webRoot}/bin`,
        /// discovering WebSharper ones.
        static member LoadFromWebRoot :
            webRoot: string -> M.Info

    type HttpConfiguration with

        /// Registers remoting host and sitelet host with
        /// default settings to serve the given sitelet.
        member RegisterDefaultSitelet :
            webRoot: string * sitelet: Sitelet<'T> -> unit

        /// Like `RegisterDefaultSitelet`, but discovers sitelet
        /// by looking for `WebsiteAttribute`-annotated assemblies.
        member RegisterDiscoveredSitelet :
            webRoot: string -> unit
