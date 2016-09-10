// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open System.IO

// Directories
let buildDir  = "./build/"

// version info
let version = "0.1"

let runExe projectDir =
    let projectName = Path.GetFileName(projectDir)
    let exePath = buildDir </> projectDir </> projectName + ".exe"
    ProcessHelper.directExec (fun info ->
        if EnvironmentHelper.isLinux        
        then info.FileName <- "mono"; info.Arguments <- exePath
        else info.FileName <- exePath)
    |> ignore

let buildDebug projectDir =
    let projectName = Path.GetFileName(projectDir)
    let buildDir = buildDir </> projectDir
    CleanDir buildDir
    MSBuildDebug buildDir "Build" [projectDir </> projectName + ".fsproj"]
    |> ignore

Target "chapter-10/MailboxProcessor" (fun _ ->
    let projectDir = "chapter-10" </> "MailboxProcessor"
    buildDebug projectDir
    runExe projectDir
)

Target "chapter-10/Orleankka" (fun _ ->
    let projectDir = "chapter-10" </> "Orleankka"
    buildDebug projectDir
    runExe projectDir
)

Target "Help" (fun _ ->
    printfn "Pass the chapter and the name of the project to run. Example:"
    printfn "build chapter-10/MailboxProcessor"    
)

// start build
RunTargetOrDefault "Build"
