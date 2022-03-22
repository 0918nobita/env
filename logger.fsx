module Logger

#r "nuget: Microsoft.Extensions.Logging"
#r "nuget: Microsoft.Extensions.Logging.Console"

open System
open System.IO
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
