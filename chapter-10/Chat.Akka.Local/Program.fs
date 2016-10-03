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
                hostname = localhost
                port = 9001
            }
        }"

// return Deploy instance able to operate in remote scope
let deployRemotely =
    Address.Parse >> RemoteScope >> Deploy

let spawnRemote systemOrContext remoteSystemAddress actorName expr = 
    deployRemotely remoteSystemAddress
    |> SpawnOption.Deploy
    |> List.singleton
    |> spawne systemOrContext actorName expr

[<EntryPoint>]
let main args =
    let system = System.create "local-system" config
    
    let aref =  
        spawnRemote system "akka.tcp://remote-system@localhost:9001/" "hello"
            // actorOf wraps custom handling function with message receiver logic
            <@ actorOf (fun msg -> printfn "received '%s'" msg) @>

    // send example message to remotely deployed actor
    aref <! "Hello world"

    // thanks to location transparency, we can select 
    // remote actors as if they where existing on local node
    let sref = select "akka://local-system/user/hello" system  
    sref <! "Hello again"

    // we can still create actors in local system context
    let lref = spawn system "local" (actorOf (fun msg -> printfn "local '%s'" msg))  
    // this message should be printed in local application console
    lref <! "Hello locally"

    0
