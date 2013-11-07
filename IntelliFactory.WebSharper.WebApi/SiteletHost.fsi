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

/// Supports hosting WebSharper sitelets and remoting components in WebAPI.
module SiteletHost =
    open System
    open System.Web.Http
    open IntelliFactory.WebSharper
    open IntelliFactory.WebSharper.Sitelets

    /// An immutable type for configuring how to host sitelets using WebAPI.
    type Options =
        {
            /// The `Debug` flag for WebSharper.
            Debug : bool

            /// Configuration object from WebApi.
            HttpConfiguration : HttpConfiguration

            /// Json provider used by WebSharper.
            JsonProvider : Core.Json.Provider

            /// Metadata object used by WebSharper.
            Metadata : Core.Metadata.Info

            /// The absolute path to the application root directory on
            /// the file system, which is by default set to ".".
            /// It is recommended to set this to `Server.MapPath("~")`.
            ServerRootDirectory : string

            /// The URL prefix, such as `websharper`, to have the
            /// sitelet served under a sub-URL.
            UrlPrefix : string
        }

        /// Registers a sitelet with current options.
        member Register : Sitelet<'T> -> unit

        /// Sets the debug flag to `true`.
        member WithDebug : unit -> Options

        /// Sets the debug flag.
        member WithDebug : bool -> Options

        /// Functionally updates the curernt `HttpConfiguration` parameter.
        member WithHttpConfiguration : HttpConfiguration -> Options

        /// Set a URL prefix, such as `websharper`, to have the sitelet served
        /// under a sub-URL.
        member WithUrlPrefix : string -> Options

        /// Sets the absolute path to the application root directory on
        /// the file system, which is by default set to ".".
        /// It is recommended to set this to `Server.MapPath("~")`.
        member WithServerRootDirectory : string -> Options

        /// Constructs a new sitelet hosting configuration based on the
        /// WebAPI configurator object.
        static member Create : config: HttpConfiguration -> Options

        /// Construction function with a specific `Metadata.Info` object.
        static member Create : config: HttpConfiguration * Core.Metadata.Info -> Options

    /// Registers a sitelet for hosting with WebAPI. This is typically called
    /// on application startup.
    val RegisterSitelet : Sitelet<'T> -> Options -> unit

    /// See `Options.Create`.
    val Configure : HttpConfiguration -> Options

