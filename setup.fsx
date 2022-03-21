#r "nuget: Microsoft.Extensions.Logging"
#r "nuget: Microsoft.Extensions.Logging.Console"

open System.Diagnostics
open Microsoft.Extensions.Logging

let task name (logger: ILogger) filename args =
    use _ = logger.BeginScope name

    let startInfo = ProcessStartInfo(filename, args)
    startInfo.RedirectStandardOutput <- true
    let p = new Process(StartInfo = startInfo)

    p.OutputDataReceived.Add
    <| fun args ->
        if args.Data <> null then
            logger.LogInformation args.Data

    p.Start() |> ignore
    p.BeginOutputReadLine()
    p.WaitForExitAsync() |> Async.AwaitTask

let () =
    use loggerFactory =
        LoggerFactory.Create (fun builder ->
            builder.AddSimpleConsole(fun opts -> opts.IncludeScopes <- true)
            |> ignore)

    let logger = loggerFactory.CreateLogger "setup"

    [ async {
          use _ = logger.BeginScope "apt"
          do! task "update" logger "sudo" "apt-get update"
          do! task "upgrade" logger "sudo" "apt-get upgrade"
          do! task "autoclean" logger "sudo" "apt-get autoclean"
          do! task "autoremove" logger "sudo" "apt-get autoremove"
      }
      async { logger.LogInformation "Hello, world!" } ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
