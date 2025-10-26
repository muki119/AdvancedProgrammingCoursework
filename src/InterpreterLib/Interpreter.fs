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
    | None
    | Num of float


let isblank c = System.Char.IsWhiteSpace c // checks if is blank
let isdigit c = System.Char.IsDigit c // checks if its a number (obviously)
let lexError = System.Exception("Lexer error") // error declaration
let intVal (c: char) = (int) ((int) c - (int) '0') // fast way to turn string to number  - number representation of number minus acsii number representation of 0
let parseError = System.Exception("Parser error") // error declaration
let testCall = Console.WriteLine "F# Connected"

let removeWhitespace (input: string) : string =
    String.Concat(input.ToCharArray() |> Array.filter (fun c -> not (isblank c)))

let str2lst s = [ for c in removeWhitespace (s) -> c ] // simple function to convert string to list of characters -- remove whitespace before processing


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
            | Mod -> true
            | _ -> false

        match input with // match with the following
        | [] -> [] // empty array - add nothing
        | '+' :: tail -> Add :: scan (tail, Add) // if plus then  -> append "Add" operator to array and call scan function with the rest of the array
        | '-' :: tail ->
            let previousTokenIsOperator = isOpperator previousToken || previousToken = None // check if previous token is an operator or the start of the string

            let nextIsValue =
                match tail with
                | [] -> false
                | c :: _ when isdigit c -> true
                | _ -> false

            if
                (not previousTokenIsOperator) // if previous token is not an operator
                && nextIsValue // and the next character is a digit
            then
                Sub :: (scan (tail, Sub)) // treat '-' as subtraction operator; continue scanning from tail, previousToken = Sub
            else
                let (iStr, iVal) = scNumber (tail, 0.0) // else treat as negative number
                Num(-iVal) :: scan (iStr, None) // append Num with negative value to array and call scan on the rest of the array
        | '*' :: tail -> Mul :: scan (tail, Mul) // same as above for multiplication
        | '^' :: tail -> Exp :: scan (tail, Exp) // same as above for exponentiation
        | '%' :: tail -> Mod :: scan (tail, Mod) // same as above for modulus
        | '/' :: tail -> Div :: scan (tail, Div) // same as above for division
        | '(' :: tail -> Lpar :: scan (tail, None) // same as above for left parenthesis
        | ')' :: tail -> Rpar :: scan (tail, None) // same as above for right parenthesis
        | c :: tail when isblank c -> scan (tail, None) // if blank space then just call scan on the rest of the array
        | c :: tail when isdigit c ->
            let (iStr, iVal) = scNumber (tail, float (intVal c)) // if digit then call scNumber function to get the full number (in case of multiple digits)
            Num iVal :: scan (iStr, None) // append Num with the value to the array and call scan on the rest of the array
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
    let rec E tList = (T >> Eopt) tList // first calls to see if a add or sub opperator was called

    and Eopt (tList, value) = // takes a operator and a value
        match tList with
        | Add :: tail ->
            let (tLst, tval) = T tail // if opperator is addition then call T on the rest of the list which goes to the bottom until a number is found
            Eopt(tLst, value + tval) // then recursively call Eopt again with the updated value
        | Sub :: tail ->
            let (tLst, tval) = T tail // will basically call the rest of the functions until a number is found - will also do all the multiplication and division first due to the order of the calls (Bidmas)
            Eopt(tLst, value - tval)
        | _ -> (tList, value) // if no operator found then return the list and the value

    and T tList = (IndicesOpt >> Topt) tList

    and Topt (tList, value) =
        match tList with
        | Mul :: tail ->
            let (tLst, tval) = IndicesOpt tail
            Topt(tLst, value * tval)
        | Div :: tail ->
            let (tLst, tval) = IndicesOpt tail
            Topt(tLst, value / tval)
        | Mod :: tail ->
            let (tLst, tval) = IndicesOpt tail //-- should probably call the NR function here to get the value to mod by
            Topt(tLst, value % tval)
        | _ -> (tList, value)

    and IndicesOpt tList = (NR >> Iopt) tList // to check for indicies

    and Iopt (tList, value) =
        match tList with
        | Exp :: tail ->
            let (tLst, tval) = NR tail
            Iopt(tLst, value ** tval)
        | _ -> (tList, value)

    and NR tList = // works from the bottom up  // the actual evaluation starts here then gets returned up the chain
        match tList with
        | Num value :: tail -> (tail, value)
        | Lpar :: tail ->
            let (tLst, tval) = E tail // in the case of a left parenthesis - recursive call to break left par to get the actual values within

            match tLst with // with the output from the recursive call , try to find the right par
            | Rpar :: tail -> (tail, tval) // if the right is found , return the tail and the broken down literal values
            | _ -> raise parseError // raises error in the event a bracket cannot be found
        | _ -> raise parseError

    E tList

let rec printTList (lst: list<terminal>) : list<string> =
    match lst with
    | head :: tail ->
        Console.Write("{0} ", head.ToString())
        printTList tail

    | [] ->
        Console.Write("EOL\n")
        []


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
