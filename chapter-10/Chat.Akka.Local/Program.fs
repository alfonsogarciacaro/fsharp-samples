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
                hostname = localhost
                port = 9001
            }
        }"

// create remote deployment configuration for actor system available under `actorPath`
let remoteDeploy systemPath = 
    let address = 
        match ActorPath.TryParseAddress systemPath with
        | false, _ -> failwith "ActorPath address cannot be parsed"
        | true, a -> a
    Deploy(RemoteScope(address))

module Msg =
    let [<Literal>] Talk = "Talk"
    let [<Literal>] Enter = "Enter"
    let [<Literal>] Input = "Input"
    let [<Literal>] Message = "Message"

open FSharp.Reflection

let makeHumanUser system (name: string) =
    let remoteSystemAddress = "akka.tcp://remote-system@localhost:7000"
    spawne system "chat-member-human-remote"
        <@
            let (!) (x:obj) = box x
            fun mailbox ->
                let admin =
                    sprintf "%s/user/chat-admin" remoteSystemAddress
                    |> select <| mailbox.Context.System
                let rec messageLoop listen: Cont<string*obj list, unit> = actor {
                    let! msg = mailbox.Receive()
                    let listen =
                        match msg with
                        // | Msg.Message, [author; txt] when listen ->
                        //     printfn "%-8O> %O" author txt
                        //     listen
                        | Msg.Input, [txt] when listen ->
                            admin <! (Msg.Talk, [!name; !txt])
                            true
                        | _ -> listen
                    return! messageLoop listen
                }
                admin <! (Msg.Enter, [!name; !mailbox.Self])
                messageLoop true
        @> [ SpawnOption.Deploy(remoteDeploy remoteSystemAddress) ]

[<EntryPoint>]
let main _args =
    let system = System.create "local-system" config
    printf "Type your name: "
    let name = System.Console.ReadLine()
    let humanUser = makeHumanUser system name 

    let rec consoleLoop(): Async<unit> = async {
        printf "> "
        let txt = System.Console.ReadLine()
        if System.String.IsNullOrWhiteSpace(txt) |> not then
            humanUser <! ("Input", [box txt])
        // Wait a bit to receive your own messafe from the admin
        // do! Async.Sleep 200
        // // Get the messages stored by the humanUser actor
        // let! msgs = localHumanUser <? HumanMsg.Output
        // msgs |> Array.iter (printfn "%s")
        return! consoleLoop()
    }
    printfn "Type a message to send it to the chat and read others' interventions."
    printfn @"Leave the line blank to ""pass your turn""."
    consoleLoop() |> Async.RunSynchronously

    0
