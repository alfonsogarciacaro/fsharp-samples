#r "../node_modules/fable-core/Fable.Core.dll"
#r "../node_modules/fable-react/Fable.React.dll"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React.Props
module R=Fable.Helpers.React

type Todo = { id: DateTime; text: string }
type TodoList = { items: Todo list; text: string }

type TodoListView(props) =
    inherit React.Component<TodoList,obj>(props)

    member this.render() =
        let createItem (item: Todo) =
            R.li [Key (string item.id)] [R.str item.text]
        R.ul [] (List.map createItem this.props.items)

type TodoAppView(props) as this =
    inherit React.Component<obj,TodoList>(props)
    do this.state <- { items = []; text = "" }

    member this.onChange (e: React.SyntheticEvent) =
        this.setState({ this.state with text = unbox e.target?value })

    member this.handleSubmit (e: React.SyntheticEvent) =
        e.preventDefault()
        let nextItems = this.state.items@[{ text=this.state.text; id=DateTime.Now }]
        this.setState({ items=nextItems; text="" })

    member this.render() =
        R.div [] [
            R.h3 [] [R.str "TODO"]
            R.com<TodoListView,_,_> this.state []
            R.form [OnSubmit this.handleSubmit] [
                R.input [OnChange this.onChange; Value (U2.Case1 this.state.text)] []
                R.button [] [sprintf "Add #%i" (this.state.items.Length + 1) |> R.str]
            ]
        ]

ReactDom.render(
    R.com<TodoAppView,_,_> None [],
    Browser.document.getElementById "content")
