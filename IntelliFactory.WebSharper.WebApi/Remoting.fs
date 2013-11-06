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

module Remoting =
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text
    open System.Web.Http
    open IntelliFactory.WebSharper
    module Rem = IntelliFactory.WebSharper.Core.Remoting

    type Options =
        {
            HttpConfiguration : HttpConfiguration
            Metadata : Core.Metadata.Info
        }

    let getHeader (req: HttpRequestMessage) (name: string) =
        let mutable out = null
        if req.Headers.TryGetValues(name, &out) then
            match Seq.toList out with
            | [value] -> Some value
            | _ -> None
        else None

    let utf8 = UTF8Encoding(false, true)

    [<Sealed>]
    type RemotingHandler(opts) =
        inherit DelegatingHandler()
        let serv = Rem.Server.Create None opts.Metadata

        override dh.SendAsync(req, t) =
            let headers = getHeader req
            if Rem.IsRemotingRequest(headers) then
                let work =
                    async {
                        let! body =
                            req.Content.ReadAsStringAsync()
                            |> Async.AwaitTask
                        let! resp =
                            serv.HandleRequest {
                                Body = body
                                Headers = headers
                            }
                        let msg =
                            let c = new StringContent(resp.Content, utf8)
                            c.Headers.ContentType <- MediaTypeHeaderValue.Parse(resp.ContentType)
                            let msg = new HttpResponseMessage(HttpStatusCode.OK)
                            msg.Content <- c
                            msg
                        return msg
                    }
                Async.StartAsTask(work, cancellationToken = t)
            else
                base.SendAsync(req, t)

    type Options with

        static member Create(conf, meta) =
            {
                HttpConfiguration = conf
                Metadata = meta
            }

        member opts.Register() =
            new RemotingHandler(opts)
            |> opts.HttpConfiguration.MessageHandlers.Add

    let Configure conf meta =
        Options.Create(conf, meta)

    let Register (opts: Options) =
        opts.Register()
