module Akka.Remote

open Akka.FSharp
open Akka.Actor
open System

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
let main args =
    use system =
        Configuration.parse config
        |> System.create "remote-system"
    Console.ReadLine() |> ignore
    0