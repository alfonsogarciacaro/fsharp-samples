open Akka.FSharp
open Akka.Actor
open System.Collections.Generic

// the most basic configuration of remote actor system
let config = """
akka {  
    actor {
        provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
    }    
    remote.helios.tcp {
        transport-protocol = tcp
        port = 7000                
        hostname = localhost #192.168.2.101
    }
}
"""
open FSharp.Reflection

let makeUnion<'T> (x: obj): 'T option =
    let (|Type|) (x: obj) = x.GetType()
    let tryMakeUnion (case, fields) =
        try
            FSharpType.GetUnionCases(typeof<'T>)
            |> Seq.tryFind (fun uci -> uci.Name = case)
            |> Option.map (fun uci ->
                FSharpValue.MakeUnion(uci, List.toArray fields) |> unbox<'T>)
        with _ -> None
    match x with
    | null -> None
    | :? 'T as x -> Some x
    | Type t when FSharpType.IsUnion(t) ->
        FSharpValue.GetUnionFields(x, t)
        |> fun (uci, fields) -> tryMakeUnion (uci.Name, List.ofArray fields)
    | :? (string*obj list) as union ->
        tryMakeUnion union
    | _ -> None

let tuple (u: obj) =
    let uci, fields = FSharpValue.GetUnionFields(u, u.GetType())
    uci.Name, List.ofArray fields
    
type AdminMsg =
  | Talk of author: string * message: string
  | Enter of name: string * IActorRef
  | Leave of name: string

and UserMsg =
  | Message of author: string * message: string
  | AllowEntry
  | Expel

let makeAdmin system =
    spawn system "chat-admin" (fun mailbox ->
        printfn "Admin address: %O" mailbox.Self.Path
        let users = Dictionary<string, IActorRef>()
        let post msg = for u in users.Values do u <! msg
        let rec messageLoop() = actor {
            let! msg = mailbox.Receive()
            makeUnion<AdminMsg> msg
            |> Option.map (function
                | Enter (name, actorRef) ->
                    if users.ContainsKey name |> not
                    then
                        let msg = sprintf "User %s entered the room" name
                        post <| Message("Admin", msg)
                        users.Add(name, actorRef)
                        actorRef <! AllowEntry
                        msg
                    else sprintf "User %s is already in the room" name
                | Leave name ->
                    if users.Remove(name)
                    then
                        let msg = sprintf "User %s left the room" name
                        post <| Message("Admin", msg)
                        msg
                    else sprintf "User %s was not in the room" name
                | Talk(author, txt) ->
                    post <| tuple (Message (author, txt))
                    sprintf "User %s says: %s" author txt)
            |> function
                | Some msg -> printfn "%s" msg
                | None -> printfn "Unknown message received"
            return! messageLoop()
        }
        messageLoop())

let makeMiddleAgent f system name =
    spawn system ("middle-agent-"+name) (fun mailbox ->
        let rec messageLoop() = actor {
            let! msg = mailbox.Receive()
            f msg
            return! messageLoop()
        }
        messageLoop())

type UserState = OutOfTheRoom | InTheRoom | WaitingApproval

type [<RequireQualifiedAccess>] RndUserMsg =
    | RandomIntervention
    | UserMsg of UserMsg

let makeRandomUser system (admin: IActorRef) name sentences =
  spawn system ("chat-member-"+name) (fun mailbox ->
    let rnd = System.Random()
    let sentencesLength = List.length sentences

    let middleAgent =
        (system, name)
        ||> makeMiddleAgent(fun msg ->
            mailbox.Self <! RndUserMsg.UserMsg msg)

    let rec msgGenerator() = async {
        do! rnd.Next(4000) |> Async.Sleep
        mailbox.Self <! RndUserMsg.RandomIntervention
        return! msgGenerator()
    }
    msgGenerator() |> Async.Start

    let rec messageLoop (state: UserState) = actor {
        let! msg = mailbox.Receive()
        match msg with
        // Ignore messages from other users
        | RndUserMsg.UserMsg (Message _) -> return! messageLoop state
        | RndUserMsg.UserMsg AllowEntry -> return! messageLoop InTheRoom
        | RndUserMsg.UserMsg Expel -> return! messageLoop OutOfTheRoom
        | RndUserMsg.RandomIntervention _ ->
            match state with
            | InTheRoom ->
                // Pick a random sentence or leave the room
                match rnd.Next(sentencesLength + 1) with
                | i when i < sentencesLength ->
                    admin <! Talk(name, sentences.[i])
                    return! messageLoop state
                | _ ->
                    admin <! Leave name
                    return! messageLoop OutOfTheRoom
            | OutOfTheRoom ->
                admin <! Enter(name, middleAgent)
                return! messageLoop WaitingApproval
            | WaitingApproval ->
                return! messageLoop state // Do nothing, just keep waiting
    }
    // Start the loop with initial state
    messageLoop OutOfTheRoom
)

[<EntryPoint>]
let main _ = 
    try
        // remote system only listens for incoming connections
        // it will receive actor creation request from local-system (see: FSharp.Deploy.Local)
        use system = System.create "remote-system" (Configuration.parse config)

        let admin = makeAdmin system

        let randomUser1 =
            makeRandomUser system admin "Sarah" [
                "Hi everybody!"
                "It feels great to be here!"
                "I missed you all so much!"
                "I couldn't agree more with that."
                "Oh, just look at the time! I should be leaving..."
            ]

        let randomUser2 =
            makeRandomUser system admin "John" [
                "Hmm, I didn't expect YOU to be here."
                "I must say, I don't feel very comfortable."
                "Is this room always so boring?"
                "I shouldn't be losing my time here."
            ]

        System.Console.ReadLine() |> ignore
        0
    with
    | ex ->
        printfn "%s" ex.Message
        1
