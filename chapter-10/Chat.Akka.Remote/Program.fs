open Akka.FSharp

// the most basic configuration of remote actor system
let config = """
akka {  
    actor {
        provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
    }    
    remote.helios.tcp {
        transport-protocol = tcp
        port = 9001                 
        hostname = localhost
    }
}
"""

[<EntryPoint>]
let main _ = 
    try
        System.Console.Title <- "Remote: " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString()
        // remote system only listens for incoming connections
        // it will receive actor creation request from local-system (see: FSharp.Deploy.Local)
        use system = System.create "remote-system" (Configuration.parse config)
        System.Console.ReadLine() |> ignore
        0
    with
    | ex ->
        printfn "%s" ex.Message
        1
