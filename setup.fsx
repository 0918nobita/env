open System.Diagnostics

let p = Process.Start(ProcessStartInfo("git", "status"))

p.WaitForExitAsync()
|> Async.AwaitTask
|> Async.RunSynchronously
