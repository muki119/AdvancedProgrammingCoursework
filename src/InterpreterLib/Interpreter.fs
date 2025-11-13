module InterpreterLib.Interpreter

// Simple Interpreter in F#
// Author: R.J. Lapeer
// Date: 23/10/2022
// Reference: Peter Sestoft, Grammars and parsing with F#, Tech. Report
open System


type terminal =
    | Add
    | Sub
    | Mul
    | Div
    | Lpar
    | Rpar
    | Exp
    | Mod
    | Assign
    | None
    | Num of float
    | Sym of string



let isblank c = System.Char.IsWhiteSpace c // checks if is blank
let isdigit c = System.Char.IsDigit c // checks if its a number (obviously)

let isAlpha c = System.Char.IsLetter c // checks if its a letter
let lexError = System.Exception("Lexer error") // error declaration
let intVal (c: char) = (int) ((int) c - (int) '0') // fast way to turn string to number  - number representation of number minus acsii number representation of 0
let parseError = System.Exception("Parser error") // error declaration

let zeroDivisionError = System.Exception("Cannot Divide by zero") // error declaration

let variableNotFoundError (varName: string) =
    System.Exception(sprintf "Variable `%s` not found" varName)

let testCall = Console.WriteLine "F# Connected"

let removeWhitespace (input: string) : string =
    String.Concat(input.ToCharArray() |> Array.filter (fun c -> not (isblank c))) // turns string into char array |> output of the 'ToCharArray' function is piped to filter function which removes all that is blank , then concatenated back into a string

let str2lst s = [ for c in removeWhitespace (s) -> c ] // simple function to convert string to list of characters -- remove whitespace before processing

let mutable SymbolTable = Map.empty<string, float> // empty map for variables

let resolveVar (name: string) : float option =
    let value = Map.tryFind name SymbolTable

    match value with
    | Some v -> Some v
    | _ -> raise (variableNotFoundError name)

let rec scNumber (iStr, iVal: float) = // recursive function to scan integer values
    match iStr with
    | c :: tail when isdigit c -> scNumber (tail, 10.0 * iVal + (float (intVal c))) // if digit then recursively call function , passing the rest of the number string and updating the integer value accordingly
    | c :: tail when c = '.' -> // if dot then return rest of string and float value
        let rec scFloat (fTail, fValue: float, decimalPlace: float) =
            match fTail with
            | c :: tail when isdigit c ->
                // if its a digit behind the dot , then divide it by the divider place value , and add it to the total float value , then increase the decimal place value by a factor of 10
                scFloat (tail, fValue + (float (intVal c) / decimalPlace), decimalPlace * 10.0)
            | _ -> (fTail, fValue)

        let (fStr, fVal) = scFloat (tail, float iVal, 10.0)
        (fStr, fVal) // return the rest of the string and the float value
    | _ -> (iStr, iVal) // return the rest of the string and the integer value when no more digits found

let lexer input =
    let rec scan (input, previousToken) = // recursive function (rec keyword indicates its recursive)
        let isOpperator token =
            match token with
            | Add
            | Sub
            | Mul
            | Div
            | Exp
            | Lpar
            | Rpar
            | Assign
            | Mod -> true
            | _ -> false

        match input with // match with the following
        | [] -> [] // empty array - add nothing
        | '+' :: tail -> Add :: scan (tail, Add) // if plus then  -> append "Add" operator to array and call scan function with the rest of the array
        | '-' :: tail ->
            let previousTokenIsOperator = isOpperator previousToken || previousToken = None // check if previous token is an operator or the start of the string

            let nextIsValue =
                match tail with // looks at head of the tail to see if its a digit
                | [] -> false
                | c :: _ when isdigit c || c = '-' || isAlpha c -> true // also check for negative sign to allow for multiple negative signs
                | _ -> false

            if
                (not previousTokenIsOperator) // if previous token is not an operator
                && nextIsValue // and the next character is a digit
            then
                Sub :: (scan (tail, Sub)) // treat '-' as subtraction operator; continue scanning from tail, previousToken = Sub
            else if // append Num with negative value to array and call scan on the rest of the array
                not tail.IsEmpty && isAlpha tail.Head
            then
                Num -1.0 :: Mul :: scan (tail, Num -1.0)
            else
                let (iStr, iVal) = scNumber (tail, 0.0) // else treat as negative number
                Num -iVal :: scan (iStr, Num -iVal) // treat as multiplication by -1 if next character is a letter (variable)

        | '*' :: tail -> Mul :: scan (tail, Mul) // same as above for multiplication
        | '^' :: tail -> Exp :: scan (tail, Exp) // same as above for exponentiation
        | '%' :: tail -> Mod :: scan (tail, Mod) // same as above for modulus
        | '/' :: tail -> Div :: scan (tail, Div) // same as above for division
        | '(' :: tail -> Lpar :: scan (tail, Lpar) // same as above for left parenthesis
        | ')' :: tail -> Rpar :: scan (tail, Rpar) // same as above for right parenthesis
        | '=' :: tail -> Assign :: scan (tail, Assign) // same as above for assignment operator
        | c :: tail when isAlpha c -> // if letter then start building variable string -- variable has to start with a letter - much like programming languages
            let rec buildVarString (strTail, char) =
                match strTail with
                | c :: tail when isAlpha c || isdigit c -> buildVarString (tail, char + c.ToString()) // if letter or digit then keep building the variable string
                | _ -> (strTail, char) // else return the rest of the string and the built variable string

            let (vStr, vName) = buildVarString (tail, c.ToString()) // call the buildVarString function to get the full variable name
            Sym vName :: scan (vStr, Sym vName) // append Sym with the

        | c :: tail when isblank c -> scan (tail, previousToken) // if blank space then just call scan on the rest of the array
        | c :: tail when isdigit c ->
            let (iStr, iVal) = scNumber (tail, float (intVal c)) // if digit then call scNumber function to get the full number (in case of multiple digits)
            Num iVal :: scan (iStr, Num iVal) // append Num with the value to the array and call scan on the rest of the array
        | _ -> raise lexError // raise lexer error if none of the above match

    scan (str2lst input, None) // call scan function on the input string converted to a list of characters

let getInputString () : string =
    Console.Write("Enter an expression: ")
    Console.ReadLine()

// Grammar in BNF:
// <E>        ::= <T> <Eopt>
// <Eopt>     ::= "+" <T> <Eopt> | "-" <T> <Eopt> | <empty>
// <T>        ::= <NR> <Topt>
// <Topt>     ::= "*" <NR> <Topt> | "/" <NR> <Topt> | <empty>
// <NR>       ::= "Num" <value> | "(" <E> ")"

let parser tList = // recursive descent parser implementation -- works in BIDMAS order from bottom up
    let rec E tList = (T >> Eopt) tList // >> is forward function composition operator: let inline (>>) f g x = g(f(x)) // performs a recursive descent parse

    and Eopt tList = // if addition or subtraction found then call T on the rest of the list and then Eopt again recursively
        match tList with
        | Add :: tail -> (T >> Eopt) tail
        | Sub :: tail -> (T >> Eopt) tail
        | _ -> tList

    and T tList = (NR >> Topt) tList

    and Topt tList = // if division or multiplication found then call NR on the rest of the list and then Topt again recursively
        match tList with
        | Mul :: tail -> (NR >> Topt) tail
        | Div :: tail -> (NR >> Topt) tail
        | _ -> tList

    and NR tList = // works from the bottom up -- checks for numbers and parentheses
        match tList with
        | Num value :: tail -> tail // if number found then return the rest of the list (since its recursive , the returned will be passed up the chain)
        | Lpar :: tail ->
            match E tail with // if left parenthesis found then call E on the rest of the list to find the matching right parenthesis
            | Rpar :: tail -> tail
            | _ -> raise parseError // raises error if no matching right parenthesis found
        | _ -> raise parseError

    E tList



let parseNeval tList =
    let rec A tList = (E >> AssignOpt) tList // basically calls E first to evaluate any expression

    and AssignOpt (tList, value, var) =
        // resolve variable assignment here
        // find an equals sign to assign a variable
        let varToBeAssigned = var

        match tList with
        | Assign :: tail ->
            // if assign is found and a var char is passed up from the E function
            // then assign the value from another AssignOpt call which evaluates the rest of the list
            let (tLst, tval, var) = E tail // evaluate the rest of the list after the assignment operator
            SymbolTable <- SymbolTable.Add(varToBeAssigned, tval) // add the variable and its value to the symbol table
            (tLst, tval, var) // return the rest of the list , the value assigned and the variable char
        | _ -> (tList, value, var) // if no assignment found then return the list , value and variable char


    and E tList = (T >> Eopt) tList // first calls to see if a add or sub opperator was called

    and Eopt (tList, value, var) = // takes a operator and a value
        match tList with
        | Add :: tail ->
            let (tLst, tval, var) = T tail // if opperator is addition then call T on the rest of the list which goes to the bottom until a number is found
            Eopt(tLst, value + tval, var) // then recursively call Eopt again with the updated value
        | Sub :: tail ->
            let (tLst, tval, var) = T tail // will basically call the rest of the functions until a number is found - will also do all the multiplication and division first due to the order of the calls (Bidmas)
            Eopt(tLst, value - tval, var)
        | _ -> (tList, value, var) // if no operator found then return the list and the value

    and T tList = (IndicesOpt >> Topt) tList

    and Topt (tList, value, var) =
        match tList with
        | Mul :: tail ->
            let (tLst, tval, var) = IndicesOpt tail
            Topt(tLst, value * tval, var)
        | Div :: tail ->
            let (tLst, tval, var) = IndicesOpt tail

            if tval = 0.0 then
                raise zeroDivisionError
            else
                Topt(tLst, value / tval, var)
        | Mod :: tail ->
            let (tLst, tval, var) = IndicesOpt tail

            if tval = 0.0 then
                raise zeroDivisionError
            else
                Topt(tLst, value % tval, var)
        | _ -> (tList, value, var)

    and IndicesOpt tList = (NR >> Iopt) tList // to check for indicies

    and Iopt (tList, value, var) =
        match tList with
        | Exp :: tail ->
            let (tLst, tval, var) = NR tail
            Iopt(tLst, value ** tval, var)
        | _ -> (tList, value, var)

    and NR tList = // works from the bottom up  // the actual evaluation starts here then gets returned up the chain
        match tList with
        | Num value :: tail -> (tail, value, "_") // if number found then return the rest of the list , the number value and a placeholder char for variable
        | Sym var :: tail ->
            if not tail.IsEmpty && tail.Head = Assign then
                // add another param to all functions to pass symbol char up the chain
                (tail, 0.0, var) // if assignment found after variable , return tail , 0.0 value ( since value will be assigned later ) and the variable character
            else
                // otherwise resolve variable value and return its value , with no assignment field
                match resolveVar (var) with // try to find the variable in the symbol table
                | Some v -> (tail, v, "_") // if found return the tail , the variable value and placeholder char
                | _ -> raise parseError

        | Lpar :: tail ->
            let (tLst, tval, var) = E tail // in the case of a left parenthesis - recursive call to break left par to get the actual values within

            match tLst with // with the output from the recursive call , try to find the right par
            | Rpar :: tail -> (tail, tval, var) // if the right is found , return the tail and the broken down literal values
            | _ -> raise parseError // raises error in the event a bracket cannot be found
        | _ -> raise parseError

    A tList

let rec printTList (lst: list<terminal>) : list<string> =
    match lst with
    | head :: tail ->
        Console.Write("{0} ", head.ToString())
        printTList tail

    | [] ->
        Console.Write("EOL\n")
        []

// Helper functions for C# interop
let setVariable (name: string) (value: float) : unit =
    SymbolTable <- SymbolTable.Add(name, value)

let clearVariables () : unit =
    SymbolTable <- Map.empty<string, float>

let evaluateWithX (expr: string) (xValue: float) : float =
    SymbolTable <- SymbolTable.Add("x", xValue)
    let tokens = lexer expr
    let (_, result, _) = parseNeval tokens
    result


// [<EntryPoint>]
// let main argv  =
//     Console.WriteLine("Simple Interpreter")
//     let input:string = getInputString()
//     let oList = lexer input // run lexer function
//     let sList = printTList oList; // print the list
//     let pList = printTList (parser oList)
//     let Out = parseNeval oList
//     Console.WriteLine()
//     Console.WriteLine("Result = {0}", snd Out)
//     0
