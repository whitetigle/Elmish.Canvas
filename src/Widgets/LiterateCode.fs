module LiterateCode

open Fable.Core
open Fable.Helpers
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Fable.PowerPack
open Fable.Import

type Paragraph =
    | Code of string
    | Content of string

type Tag =
    | Code
    | Content
    | Hide

type ParserState =
    { Paragraphs : Paragraph list
      CurrentBlock : Tag
      CapturedLines : string list }

let (|Prefix|_|) (p:string) (s:string) =
    if s.StartsWith(p) then
        Some(s.Substring(p.Length))
    else
        None


let parseText (text : string) =
    let lines = text.Split('\n') |> Array.toList

    let rec parse (lines : string list) (state : ParserState) =
        match lines with
        | line::rest ->
            match state.CurrentBlock with
            | Tag.Hide ->
                let newState =
                    if line.Trim() = "(* end-hide *)" then
                        { state with CapturedLines = [ ]
                                     CurrentBlock = Tag.Code }
                    else
                        state

                parse rest newState
            | Tag.Content ->
                let newState =
                    if line.Trim() = "*)" then
                        { state with CapturedLines = []
                                     Paragraphs =
                                        state.CapturedLines
                                        |> String.concat "\n"
                                        |> Paragraph.Content
                                        |> List.singleton
                                        |> List.append state.Paragraphs
                                     CurrentBlock = Tag.Code }
                    else
                        { state with CapturedLines =
                                        line
                                        |> List.singleton
                                        |> List.append state.CapturedLines }

                parse rest newState
            | Tag.Code ->
                let newState =
                    if line.Trim() = "(**" then
                        { state with CapturedLines = []
                                     Paragraphs =
                                        state.CapturedLines
                                        |> String.concat "\n"
                                        |> Paragraph.Code
                                        |> List.singleton
                                        |> List.append state.Paragraphs
                                     CurrentBlock = Tag.Content }
                    else if line.Trim() = "(* hide *)" then
                        { state with CapturedLines = []
                                     Paragraphs =
                                        state.CapturedLines
                                        |> String.concat "\n"
                                        |> Paragraph.Code
                                        |> List.singleton
                                        |> List.append state.Paragraphs
                                     CurrentBlock = Tag.Hide }
                    else
                        { state with CapturedLines =
                                        line
                                        |> List.singleton
                                        |> List.append state.CapturedLines }
                parse rest newState

        | [] ->
            match state.CurrentBlock with
            | Tag.Code ->
                state.CapturedLines
                |> String.concat "\n"
                |> Paragraph.Code
                |> List.singleton
                |> List.append state.Paragraphs
            | Tag.Content ->
                state.CapturedLines
                |> String.concat "\n"
                |> Paragraph.Content
                |> List.singleton
                |> List.append state.Paragraphs
            | Hide ->
                state.Paragraphs

    parse lines { Paragraphs = []
                  CurrentBlock = Tag.Code
                  CapturedLines = [] }
    |> List.filter (function
        | Paragraph.Content "" -> false
        | Paragraph.Code "" -> false
        | _ -> true
    )

[<Pojo>]
type Props =
    { FilePath : string }

[<Pojo>]
type State =
    { Content : Paragraph list }

type LiterateCode (props) =
    inherit React.Component<Props, State>(props)
    do base.setInitState({ Content = [] })

    override this.componentDidMount() =
        let url = "/Demos/SegmentsFollowMouse.fs"
        promise {
            let! res = Fetch.fetch url []
            let! text = res.text()
            let parsed = parseText text

            this.setContent(parsed)
        }
        |> Promise.start

    member this.setContent(text) =
        this.setState({ this.state with Content = text })

    override this.render() =
        this.state.Content
        |> List.map (function
            | Paragraph.Content text ->
                Render.contentFromMarkdown [ ]
                    text
            | Paragraph.Code text ->
                ReactHighlight.highlight [ ReactHighlight.ClassName "fsharp" ]
                    [ str text ]
        )
        |> Content.content [ ]

let view filePath =
    ofType<LiterateCode,_,_>
        { FilePath = filePath }
        [ ]
