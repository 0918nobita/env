#r "nuget: Microsoft.Extensions.Logging"
#r "nuget: Microsoft.Extensions.Logging.Console"

open System
open System.IO
open System.Diagnostics
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Console

type CustomConsoleFormatter() =
    inherit ConsoleFormatter("envLogger")

    override _.Write(logEntry, scopeProvider, textWriter) =
        let message = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception)

        if logEntry.Exception = null && message = null then
            ()
        else
            let logLevel =
                match logEntry.LogLevel with
                | LogLevel.Trace -> "TRACE"
                | LogLevel.Debug -> "DEBUG"
                | LogLevel.Information -> "INFO"
                | LogLevel.Warning -> "WARN"
                | LogLevel.Error -> "ERROR"
                | LogLevel.Critical -> "CRITICAL"
                | _ -> failwith "Unexpected log level"

            textWriter.Write('[')
            textWriter.Write(DateTime.Now.ToString("yyyy-MM-ddTHH::mm:ssZ"))
            textWriter.Write(' ')
            textWriter.Write(logLevel)
            textWriter.Write(' ')
            textWriter.Write(logEntry.Category)

            scopeProvider.ForEachScope(
                (fun scope (state: TextWriter) ->
                    state.Write("::")
                    state.Write(scope)),
                textWriter
            )

            textWriter.Write("] ")
            textWriter.Write(message.Trim())
            textWriter.Write(Environment.NewLine)

type ILoggingBuilder with
    member this.AddEnvLoggerConsole() =
        this
            .AddConsole(fun opts -> opts.FormatterName <- "envLogger")
            .AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>()

let task (logger: ILogger) filename args =
    use _ = logger.BeginScope $"{filename} {args}"

    let startInfo = ProcessStartInfo(filename, args)
    startInfo.RedirectStandardOutput <- true
    let p = new Process(StartInfo = startInfo)

    p.Start() |> ignore

    let memory = Array.zeroCreate<char> 256
    let mutable shouldContinue = true

    async {
        while shouldContinue do
            let! numBytes =
                p.StandardOutput.ReadAsync(memory).AsTask()
                |> Async.AwaitTask

            if numBytes > 0 then
                logger.LogInformation
                <| (new string (memory, 0, numBytes)).TrimEnd()

                shouldContinue <- true
            else
                shouldContinue <- false

        do! p.WaitForExitAsync() |> Async.AwaitTask
    }

let () =
    use loggerFactory =
        LoggerFactory.Create(fun builder -> builder.AddEnvLoggerConsole() |> ignore)

    let logger = loggerFactory.CreateLogger "setup"

    [ async {
          use _ = logger.BeginScope "apt"
          do! task logger "sudo" "apt-get update"
          do! task logger "sudo" "apt-get upgrade"
          do! task logger "sudo" "apt-get autoclean"
          do! task logger "sudo" "apt-get autoremove"
      }
      async { logger.LogInformation "Hello, world!" } ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
