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

module SiteletHost =
    open System
    open System.Collections.Generic
    open System.Collections.Specialized
    open System.Configuration
    open System.IO
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text
    open System.Threading
    open System.Threading.Tasks
    open System.Web
    open System.Web.Http
    open System.Web.Http.Routing
    open IntelliFactory.WebSharper
    open IntelliFactory.WebSharper.Sitelets

    module Rem = IntelliFactory.WebSharper.Core.Remoting
    module Res = IntelliFactory.WebSharper.Core.Resources
    module P = IntelliFactory.WebSharper.PathConventions

    type D = HttpRouteValueDictionary
    type NVC = NameValueCollection
    type Req = HttpRequestMessage
    type Resp = HttpResponseMessage

    type Options =
        {
            Debug : bool
            HttpConfiguration : HttpConfiguration
            JsonProvider : Core.Json.Provider
            Metadata : Core.Metadata.Info
            ServerRootDirectory : string
            UrlPrefix : string
        }

        member o.WithDebug() = o.WithDebug(true)
        member o.WithDebug(d) = { o with Debug = d }
        member o.WithHttpConfiguration(c) = { o with HttpConfiguration = c }
        member o.WithServerRootDirectory(d) = { o with ServerRootDirectory = d }
        member o.WithUrlPrefix(t) = { o with UrlPrefix = t }

        static member Create(conf) =
            {
                Debug = false
                JsonProvider = Core.Json.Provider.Create()
                Metadata = Core.Metadata.Info.Create([])
                HttpConfiguration = conf
                ServerRootDirectory = "."
                UrlPrefix = ""
            }

        static member Create(conf, meta) =
            {
                Debug = false
                JsonProvider = Core.Json.Provider.CreateTyped(meta)
                Metadata = meta
                HttpConfiguration = conf
                ServerRootDirectory = "."
                UrlPrefix = ""
            }

    let routeTemplate cfg =
        match cfg.UrlPrefix with
        | "" -> "{*action}"
        | p -> p + "/{*action}"

    [<Sealed>]
    type WebHandler(handle: Req -> Async<Resp>) =
        inherit HttpMessageHandler()

        override wh.SendAsync(request, token) =
            Async.StartAsTask(handle request, TaskCreationOptions.None, token)

    [<Sealed>]
    type WebRoute(template, handler) =
        inherit HttpRoute(template, D(), D(), D(), new WebHandler(handler))

    let convMethod =
        let d =
            dict [
                HttpMethod.Delete, Http.Method.Delete
                HttpMethod.Get, Http.Method.Get
                HttpMethod.Head, Http.Method.Head
                HttpMethod.Options, Http.Method.Options
                HttpMethod.Post, Http.Method.Post
                HttpMethod.Put, Http.Method.Put
                HttpMethod.Trace, Http.Method.Trace
            ]
        fun m ->
            let mutable out = Unchecked.defaultof<_>
            if d.TryGetValue(m, &out) then out else
                Http.Method.Custom m.Method

    let convHeaders (h: HttpRequestHeaders) =
        [|
            for KeyValue (name, values) in h do
                for value in values do
                    yield Http.Header.Custom name value
        |]

    let convCookies (cookies: seq<CookieHeaderValue>) =
        let c = HttpCookieCollection()
        for cookie in cookies do
            for ck in cookie.Cookies do
                let out = HttpCookie(ck.Name, ck.Value)
                out.Domain <- cookie.Domain
                if cookie.Expires.HasValue then
                    out.Expires <- cookie.Expires.Value.UtcDateTime
                out.HttpOnly <- cookie.HttpOnly
                out.Path <- cookie.Path
                out.Secure <- cookie.Secure
                c.Add(out)
        c

    [<Sealed>]
    type CustomPostedFile(len: Nullable<int64>, ct, fn, str) =
        inherit HttpPostedFileBase()

        override x.ContentLength =
            if len.HasValue then int len.Value else base.ContentLength

        override x.ContentType = ct
        override x.FileName = fn
        override x.InputStream = str

    let parseMultiPartFormData (req: Req) (files: ResizeArray<_>) (post: NVC) =
        let parts = req.Content.ReadAsMultipartAsync().Result
        let files = ResizeArray()
        for content in parts.Contents do
            let d = content.Headers.ContentDisposition
            if d.DispositionType.ToLower() = "form-data" then
                match d.FileName with
                | null ->
                    post.Add(d.Name, content.ReadAsStringAsync().Result)
                | fileName ->
                    let len = content.Headers.ContentLength
                    let ct = string content.Headers.ContentType
                    let str = content.ReadAsStreamAsync().Result
                    files.Add(CustomPostedFile(len, ct, fileName, str))

    let convReq (req: Req) : Http.Request =
        let files = ResizeArray()
        let post = NVC()
        let mutable body = Stream.Null
        if req.Content.IsMimeMultipartContent() then
            parseMultiPartFormData req files post
        elif req.Content.IsFormData() then
            let c = req.Content.ReadAsFormDataAsync().Result
            for k in c do
                post.Add(k, c.[k])
        else
            body <- req.Content.ReadAsStreamAsync().Result
        let get =
            let c = HttpUtility.ParseQueryString(req.RequestUri.Query)
            Http.ParameterCollection(c)
        {
            Body = req.Content.ReadAsStreamAsync().Result
            Cookies = req.Headers.GetCookies() |> convCookies
            Files = files
            Get = get
            Headers = convHeaders req.Headers
            Method = convMethod req.Method
            Post = Http.ParameterCollection(post)
            ServerVariables = Http.ParameterCollection([])
            Uri =
                let action =
                    match req.GetRouteData().Values.["action"] with
                    | null -> ""
                    | a -> a :?> string
                Uri(action, UriKind.Relative)
        }

    let convResp (resp: Http.Response) : Resp =
        let statusCode : HttpStatusCode =
            enum resp.Status.Code
        let msg = new HttpResponseMessage(statusCode)
        for h in resp.Headers do
            if h.Name.ToLower().StartsWith("content") |> not then
                msg.Headers.Add(h.Name, h.Value)
        msg.Content <-
            let bytes =
                use out = new MemoryStream()
                resp.WriteBody(out :> _)
                out.ToArray()
            let c = new ByteArrayContent(bytes)
            for h in resp.Headers do
                if h.Name.ToLower().StartsWith("content") then
                    c.Headers.Add(h.Name, h.Value)
            c
        msg

    let buildResourceContext cfg : Res.Context =
        let isDebug = cfg.Debug
        let pu = P.PathUtility.VirtualPaths(cfg.HttpConfiguration.VirtualPathRoot)
        {
            DebuggingEnabled = isDebug
            DefaultToHttp = false
            GetSetting = fun (name: string) ->
                match ConfigurationManager.AppSettings.[name] with
                | null -> None
                | x -> Some x
            GetAssemblyRendering = fun name ->
                let aid = P.AssemblyId.Create(name.FullName)
                let url = if isDebug then pu.JavaScriptPath(aid) else pu.MinifiedJavaScriptPath(aid)
                Res.RenderLink url
            GetWebResourceRendering = fun ty resource ->
                let id = P.AssemblyId.Create(ty)
                let kind =
                    if resource.EndsWith(".js") || resource.EndsWith(".ts")
                        then P.ResourceKind.Script
                        else P.ResourceKind.Content
                P.EmbeddedResource.Create(kind, id, resource)
                |> pu.EmbeddedPath
                |> Res.RenderLink
        }

    [<Sealed>]
    type ContextBuilder(cfg) =
        let info = cfg.Metadata
        let json = cfg.JsonProvider
        let appPath = cfg.HttpConfiguration.VirtualPathRoot
        let resContext = buildResourceContext cfg

        let ( ++ ) a b =
            let a =
                match a with
                | "" -> "/"
                | _ -> VirtualPathUtility.AppendTrailingSlash(a)
            let b =
                match b with
                | "" -> "."
                | _ -> b
            VirtualPathUtility.Combine(a, b)

        let resolveUrl u =
            if VirtualPathUtility.IsAppRelative(u) then
                VirtualPathUtility.ToAbsolute(u, appPath)
            else
                u

        member b.GetContext<'T when 'T : equality>(site: Sitelet<'T>, req: Http.Request) : Context<'T> =
            let link = site.Router.Link
            let prefix = cfg.UrlPrefix
            let p = appPath ++ prefix
            let link x =
                match link x with
                | None -> failwithf "Failed to link to %O" (box x)
                | Some loc ->
                    if loc.IsAbsoluteUri then string loc else
                        let loc =
                            match string loc with
                            | "" | "/" -> "."
                            | s when s.StartsWith("/") -> s.Substring(1)
                            | s -> s
                        p ++ loc
            {
                ApplicationPath = appPath
                Link = link
                Json = json
                Metadata = info
                ResolveUrl = resolveUrl
                ResourceContext = resContext
                Request = req
                RootFolder = cfg.ServerRootDirectory
            }

    let notFound (req: Req) =
        req.CreateErrorResponse(HttpStatusCode.NotFound,
            "Content not found")

    let dispatch (cb: ContextBuilder) (s: Sitelet<'T>) (req: Req) : Async<Resp> =
        async {
            let request = convReq req
            let ctx = cb.GetContext(s, request)
            match s.Router.Route(request) with
            | None -> return notFound req
            | Some action ->
                let content = s.Controller.Handle(action)
                let response = Content.ToResponse content ctx
                return convResp response
        }

    let RegisterSitelet (sitelet: Sitelet<'T>) (config: Options)  =
        let cb = ContextBuilder(config)
        let handle req = dispatch cb sitelet req
        let route = WebRoute(routeTemplate config, handle)
        config.HttpConfiguration.Routes.Add(typeof<'T>.Name, route)

    type Options with
        member opts.Register(s) =
            RegisterSitelet s opts

    let Configure http =
        Options.Create(http)
