open System
open System.Diagnostics
open System.Threading
open System.Threading.Channels

module Scope =
    type Scope =
        private
        | Scope of string list

        override this.ToString() =
            match this with
            | Scope names -> String.concat "::" names

    let root name = Scope [ name ]

    let append (Scope a) (Scope b) = Scope(a @ b)

type LogLevel =
    | Error
    | Warn
    | Info
    | Debug
    | Trace

type LogHandler = Scope.Scope -> string -> unit

type Logger =
    { error: LogHandler
      warn: LogHandler
      info: LogHandler
      debug: LogHandler
      trace: LogHandler }

module Env =
    type Env = { logger: Logger; scope: Scope.Scope }

    let make (root: string) (logger: Logger) =
        { logger = logger
          scope = Scope.root root }

    let local (env: Env) (scopeItem: string) (f: Env -> 'a) =
        let newEnv =
            { logger = env.logger
              scope = Scope.append (env.scope) (Scope.root scopeItem) }

        f newEnv

module Log =
    let error (env: Env.Env) (msg: string) = env.logger.error env.scope msg
    let warn (env: Env.Env) (msg: string) = env.logger.warn env.scope msg
    let info (env: Env.Env) (msg: string) = env.logger.info env.scope msg
    let debug (env: Env.Env) (msg: string) = env.logger.debug env.scope msg
    let trace (env: Env.Env) (msg: string) = env.logger.trace env.scope msg

let startProcess filename args =
    Process.Start(ProcessStartInfo(filename, args))

let waitForProcess (p: Process) = p.WaitForExitAsync() |> Async.AwaitTask

let aptUpdate (env: Env.Env) =
    Log.info env "apt update"
    startProcess "sudo" "apt update" |> waitForProcess

let aptUpgrade (env: Env.Env) =
    Log.info env "apt upgrade"

    startProcess "sudo" "apt upgrade"
    |> waitForProcess

let aptAutoClean (env: Env.Env) =
    Log.info env "apt autoclean"

    startProcess "sudo" "apt autoclean"
    |> waitForProcess

let aptAutoRemove (env: Env.Env) =
    Log.info env "apt autoremove"

    startProcess "sudo" "apt autoremove"
    |> waitForProcess

let now () =
    DateTime.Now.ToString("yyyy-MM-ddTHH::mm:ssZ")

let channel =
    let opts = UnboundedChannelOptions()
    opts.SingleReader <- true
    Channel.CreateUnbounded<string>(opts)

let logger =
    { error =
        (fun scope msg ->
            channel.Writer.TryWrite($"[{now ()} ERROR {scope}] {msg}")
            |> ignore)
      warn =
        (fun scope msg ->
            channel.Writer.TryWrite($"[{now ()} WARN {scope}] {msg}")
            |> ignore)
      info =
        (fun scope msg ->
            channel.Writer.TryWrite($"[{now ()} INFO {scope}] {msg}")
            |> ignore)
      debug =
        (fun scope msg ->
            channel.Writer.TryWrite($"[{now ()} DEBUG {scope}] {msg}")
            |> ignore)
      trace =
        (fun scope msg ->
            channel.Writer.TryWrite($"[{now ()} TRACE {scope}] {msg}")
            |> ignore) }

let loggingTask =
    async {
        while true do
            let! msg =
                channel.Reader.ReadAsync().AsTask()
                |> Async.AwaitTask

            printfn "%s" msg
    }

let env = Env.make "main" logger

let cts = new CancellationTokenSource()
Async.Start(loggingTask, cts.Token)

[ Env.local env "apt" (fun env ->
      async {
          do! aptUpdate env
          do! aptUpgrade env
          do! aptAutoClean env
          do! aptAutoRemove env
      })
  async { Log.info env "Hello, world" } ]
|> Async.Parallel
|> Async.RunSynchronously

cts.Cancel()
