#r "nuget: Microsoft.Extensions.Logging"
#r "nuget: Microsoft.Extensions.Logging.Console"

open System.Diagnostics
open Microsoft.Extensions.Logging

let startProcess filename args =
    Process.Start(ProcessStartInfo(filename, args))

let waitForProcess (p: Process) = p.WaitForExitAsync() |> Async.AwaitTask

let aptUpdate (logger: ILogger) =
    logger.LogInformation "apt update"
    startProcess "sudo" "apt update" |> waitForProcess

let aptUpgrade (logger: ILogger) =
    logger.LogInformation "apt upgrade"

    startProcess "sudo" "apt upgrade"
    |> waitForProcess

let aptAutoClean (logger: ILogger) =
    logger.LogInformation "apt autoclean"

    startProcess "sudo" "apt autoclean"
    |> waitForProcess

let aptAutoRemove (logger: ILogger) =
    logger.LogInformation "apt autoremove"

    startProcess "sudo" "apt autoremove"
    |> waitForProcess

let () =
    use loggerFactory =
        LoggerFactory.Create (fun builder ->
            builder.AddSimpleConsole(fun opts -> opts.IncludeScopes <- true)
            |> ignore)

    let logger = loggerFactory.CreateLogger "setup"

    [ async {
          use _ = logger.BeginScope "apt"
          do! aptUpdate logger
          do! aptUpgrade logger
          do! aptAutoClean logger
          do! aptAutoRemove logger
      }
      async { logger.LogInformation "Hello, world!" } ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
