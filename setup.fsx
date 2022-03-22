#load "logger.fsx"

open System.Diagnostics
open Microsoft.Extensions.Logging

open Logger

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
