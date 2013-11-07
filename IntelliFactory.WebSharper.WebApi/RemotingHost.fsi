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

/// Implements server-side remoting support under Web API.
/// Remoting support is needed for the server to respond to client calls
/// to methods annnotated with the `[<Remote>]` attribute.
module RemotingHost =
    open System.Web.Http
    module M = IntelliFactory.WebSharper.Core.Metadata

    /// Configures the remoting handler.
    type Options =
        {
            /// WebApi configuration object.
            HttpConfiguration : HttpConfiguration

            /// WebSharper metadata record.
            Metadata : M.Info
        }

        /// Registers the remoting support.
        member Register : unit -> unit

        /// Creates a new `Options` value.
        static member Create : HttpConfiguration * M.Info -> Options

    /// Creates a new `Options` value.
    val Configure : HttpConfiguration -> M.Info -> Options

    /// Registers the remoting support.
    val Register : Options -> unit
