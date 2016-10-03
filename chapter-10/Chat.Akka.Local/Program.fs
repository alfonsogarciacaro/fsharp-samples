module Akka.Local

// Open modules in this order, as Akka.FSharp can shadow
// some members from Akka.Actor
open Akka.FSharp
open Akka.Actor

let config =  
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                transport-protocol = tcp
                hostname = 0.0.0.0
                port = 0 # get first available port
            }
        }"

// create remote deployment configuration for actor system available under `actorPath`
let remoteDeploy systemPath = 
    let address = 
        match ActorPath.TryParseAddress systemPath with
        | false, _ -> failwith "ActorPath address cannot be parsed"
        | true, a -> a
    Deploy(RemoteScope(address))

let [<Literal>] REQ = 1
let [<Literal>] RES = 2

[<EntryPoint>]
let main _args =
    System.Console.Title <- "Local: " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString()
    // remote system address according to settings provided 
    // in FSharp.Deploy.Remote configuration
    let remoteSystemAddress = "akka.tcp://remote-system@192.168.2.101:7000"
    let system = System.create "local-system" config
    
    let aref =  
        // as long as actor receive logic is serializable F# Expr, there is no need for sharing any assemblies 
        // all code will be serialized, deployed to remote system and there compiled and executed
        spawne system "remote" 
            <@ 
                fun mailbox -> 
                let rec loop(): Cont<string, unit> = 
                    actor { 
                        let! msg = mailbox.Receive()
                        printfn "Remote actor received: %s" msg
                        return! loop()
                    }
                loop() 
             @> [ SpawnOption.Deploy(remoteDeploy remoteSystemAddress) ]

//    async { 
//        let! msg = aref <? (REQ, "hello")
//        match msg with
//        | (RES, m) -> printfn "Remote actor responded: %s" m
//        | _ -> printfn "Unexpected response from remote actor"
//    }
//    |> Async.RunSynchronously

    // send example message to remotely deployed actor
    aref <! "Hello world"

    // we can still create actors in local system context
    let lref = spawn system "local" (actorOf (fun msg -> printfn "local '%s'" msg))  
    // this message should be printed in local application console
    lref <! "Hello locally"

    System.Console.ReadLine() |> ignore
    0
