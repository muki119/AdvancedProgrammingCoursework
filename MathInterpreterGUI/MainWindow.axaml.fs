namespace MathInterpreterGUI

open System
open Avalonia.Controls
open Avalonia.Markup.Xaml

type MainWindow () as this = 
    inherit Window ()

    do this.InitializeComponent()
       this.SetupEventHandlers()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member private this.SetupEventHandlers() =
        // ui elements
        let inputBox = this.FindControl<TextBox>("inputBox")
        let outputBox = this.FindControl<TextBox>("outputBox")
        let errorBox = this.FindControl<TextBlock>("errorBox")
        let evaluateButton = this.FindControl<Button>("evaluateButton")
        let clearButton = this.FindControl<Button>("clearButton")

        // event handlers
        evaluateButton.Click.Add(fun _ -> 
            try
                let tokens = Interpreter.lexer inputBox.Text
                let _, result = Interpreter.parseNeval tokens
                outputBox.Text <- string result
                errorBox.Text <- ""
            with ex ->
                outputBox.Text <- ""
                errorBox.Text <- ex.Message
        )

        clearButton.Click.Add(fun _ ->
            inputBox.Text <- ""
            outputBox.Text <- "Results will appear here..."
            errorBox.Text <- ""
        )

