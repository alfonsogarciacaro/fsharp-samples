module MyLibTests

#r "../packages/NUnit/lib/net45/nunit.framework.dll"
#load "MyLib.fs"

open MyLib
open NUnit.Framework

[<Test>]
let ``Adding two plus two yields four``() =
    Assert.AreEqual(4, add 2 2)

[<TestCase(2, 2, 4)>]
[<TestCase(2, 3, 5)>]
[<TestCase(5, 5, 10)>]
let ``Adding integers``(x: int, y: int, result: int) =
    Assert.AreEqual(result, add x y)

// The test above can also be rewritten as:

[<TestCase(2, 2, ExpectedResult=4)>]
[<TestCase(2, 3, ExpectedResult=5)>]
[<TestCase(5, 5, ExpectedResult=10)>]
let ``Adding integers - 2``(x: int, y: int) =
    add x y

// It is also possible to use a generator of test cases

[<TestCaseSource("addCases")>]
let ``Adding integers - 3``(x: int, y: int, result: int) =
    Assert.AreEqual(result, add x y)

let addCases = seq {
    for i in [0..2..10] do
        for j in [10..-3..-10] do
            yield [| i; j; i+j |]
}

// Setting up the environment

[<OneTimeSetUp>]
let oneTimeSetup() =
    printfn "This setup happens only once"

[<SetUp>]
let setup() =
    printfn "This setup happens before every test"

[<TearDown>]
let cleanup() =
    printfn "This cleanup happens after every test"

[<OneTimeTearDown>]
let oneTimeCleanup() =
    printfn "This cleanup happens only once"


// Helper to make assertions more idiomatic in F#

let equals expected actual =
    Assert.AreEqual(expected, actual)

let ``Adding two plus two yields four``() =
    add 2 2
    |> equals 4

// Using helpers from FsUnit

#r "../packages/FsUnit/lib/net45/FsUnit.NUnit.dll"
open FsUnit

4.99 |> should (equalWithin 0.05) 5.0
4.0 |> should not' ((equalWithin 0.05) 5.0)
7 |> should greaterThan 3
7 |> should greaterThanOrEqualTo 3

// Asynchronous tests

type Message = string * AsyncReplyChannel<string>

[<Test>]
let ``MailboxProcessor.postAndAsyncReply works``() =
    async {
        let formatString = "Msg: {0} - {1}"
        let agent = MailboxProcessor<Message>.Start(fun inbox ->
            let rec loop n = async {
                let! (message, replyChannel) = inbox.Receive()
                do! Async.Sleep(100) // Delay a bit
                replyChannel.Reply(String.Format(formatString, n, message))
                if message <> "Bye" then do! loop (n + 1)
            }
            loop 0)
        let! resp = agent.PostAndAsyncReply(fun replyChannel -> "Hi", replyChannel)
        equal "Msg: 0 - Hi" resp
        let! resp = agent.PostAndAsyncReply(fun replyChannel -> "Bye", replyChannel)
        equal "Msg: 1 - Bye" resp
    } |> Async.RunSynchronously