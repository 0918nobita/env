open System.Diagnostics

let aptUpdate = ProcessStartInfo("sudo", "apt update")
let aptUpgrade = ProcessStartInfo("sudo", "apt upgrade")
let aptAutoClean = ProcessStartInfo("sudo", "apt autoclean")
let aptAutoRemove = ProcessStartInfo("sudo", "apt autoremove")

let startAndWait (pInfo: ProcessStartInfo) =
    Process.Start(pInfo).WaitForExitAsync()
    |> Async.AwaitTask

async {
    do! startAndWait aptUpdate
    do! startAndWait aptUpgrade
    do! startAndWait aptAutoClean
    do! startAndWait aptAutoRemove
}
|> Async.RunSynchronously
