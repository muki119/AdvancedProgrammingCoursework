module InterpreterLib.Interpreter
open System

type Number =
    | Int of int
    | Float of float

type Functions =
    | Sin
    | Cos
    | Tan
    | Log
    | Ln
    | Sqrt

type Irrationals = | Pi

let functionMap =
    Map.ofList
        [ (Sin, Math.Sin)
          (Cos, Math.Cos)
          (Tan, Math.Tan)
          (Log, Math.Log10)
          (Ln, Math.Log)
          (Sqrt, Math.Sqrt) ]

let Irrationals = Map.ofList [ ("pi", Math.PI) ]

let mathFunc funcName value =
    match Map.tryFind funcName functionMap with
    | Some f -> f value
    | None -> raise (System.Exception(sprintf "Function `%s` not found" (funcName.ToString())))

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
    | Irr of Irrationals
    | Num of Number
    | Sym of string
    | Func of Functions



let isblank c = System.Char.IsWhiteSpace c // checks if is blank
let isdigit c = System.Char.IsDigit c // checks if its a number (obviously)

let isAlpha c = System.Char.IsLetter c // checks if its a letter

let isZero num =
    match num with
    | Int i when i = 0 -> true
    | Float f when f = 0.0 -> true
    | _ -> false

let toNumberFloat (num: float) : Number = Float(num)

let NumberToString (num: Number) : string =
    match num with
    | Int i -> i.ToString()
    | Float f -> f.ToString()

let toPrimativeFloat (num: Number) : float =
    match num with
    | Int i -> float i
    | Float f -> f


let lexError = System.Exception("Lexer error") // error declaration
let intVal (c: char) = (int) ((int) c - (int) '0') // fast way to turn string to number  - number representation of number minus acsii number representation of 0
let parseError = System.Exception("Parser error") // error declaration

let modulusError =
    System.Exception("Cannot perform modulus operation between non-integer values") // error declaration

let zeroDivisionError = System.Exception("Cannot Divide by zero") // error declaration

let variableNotFoundError (varName: string) =
    System.Exception(sprintf "Variable `%s` not found" varName)

let incompatibleTypesError = System.Exception("Incompatible types for operation") // error declaration

let removeWhitespace (input: string) : string =
    String.Concat(input.ToCharArray() |> Array.filter (fun c -> not (isblank c))) // turns string into char array to output of the 'ToCharArray' function is piped to filter function which removes all that is blank , then concatenated back into a string

let str2lst s = [ for c in removeWhitespace (s) -> c ] // simple function to convert string to list of characters, remove whitespace before processing

let mutable SymbolTable = Map.empty<string, Number> // empty map for variables

let resolveVar (name: string) =
    match Map.tryFind name SymbolTable with
    | Some v -> Some v
    | _ -> raise (variableNotFoundError name)


// potentially turned into a generic so it can return int or float
// or hold a parameter to indicate which type to return
let rec scNumber (iStr, iVal: Number) = // recursive function to scan integer values
    match iStr with
    // when is interger - remain an int value - and return an int
    // when is dot - change to float value andd return a float
    | c :: tail when isdigit c ->
        match iVal with
        | Float f -> scNumber (tail, Float(f * 10.0 + float (intVal c))) // if digit then recursively call function , passing the rest of the number string and updating the float value accordingly
        | Int i -> scNumber (tail, Int(10 * i + (intVal c))) // if digit then recursively call function , passing the rest of the number string and updating the integer value accordingly
    | c :: tail when c = '.' -> // if dot then return rest of string and float value
        let rec scFloat (fTail, fValue: float, decimalPlace: float) =
            match fTail with
            | c :: tail when isdigit c ->
                // if its a digit behind the dot , then divide it by the divider place value , and add it to the total float value , then increase the decimal place value by a factor of 10
                scFloat (tail, fValue + (float (intVal c) / decimalPlace), decimalPlace * 10.0)
            | _ -> (fTail, fValue)

        let initialValue =
            match iVal with
            | Int i -> float i
            | Float f -> f

        let (fStr, fVal) = scFloat (tail, initialValue, 10.0)

        // check if expo notation follows after decimal
        match fStr with
        | 'E' :: eTail
        | 'e' :: eTail ->
            // parse optional sign for expo
            let (expSign, expTail) =
                match eTail with
                | '+' :: rest -> (1.0, rest)
                | '-' :: rest -> (-1.0, rest)
                | _ -> (1.0, eTail)

            // parse expo digits
            let rec scExponent (eTail, expVal: int) =
                match eTail with
                | c :: rest when isdigit c -> scExponent (rest, expVal * 10 + (intVal c))
                | _ -> (eTail, expVal)

            let (finalStr, expValue) = scExponent (expTail, 0)
            let result = fVal * (10.0 ** (expSign * float expValue))
            (finalStr, Float result)
        | _ -> (fStr, Float fVal) // return the rest of string and float
    | 'E' :: tail
    | 'e' :: tail -> // if expo notes e or E after int then parse
        let baseValue =
            match iVal with
            | Int i -> float i
            | Float f -> f

        // parse optional sign for expo
        let (expSign, expTail) =
            match tail with
            | '+' :: rest -> (1.0, rest)
            | '-' :: rest -> (-1.0, rest)
            | _ -> (1.0, tail)

        // parse expo digits
        let rec scExponent (eTail, expVal: int) =
            match eTail with
            | c :: rest when isdigit c -> scExponent (rest, expVal * 10 + (intVal c))
            | _ -> (eTail, expVal)

        let (finalStr, expValue) = scExponent (expTail, 0)
        let result = baseValue * (10.0 ** (expSign * float expValue))
        (finalStr, Float result)
    | _ -> (iStr, iVal) // return the rest of the string and the integer value when no more digits found

let lexer input =
    let rec scan (input) = // recursive function (rec keyword indicates its recursive)
        match input with // match with the following
        | [] -> [] // empty array - add nothing
        | '+' :: tail -> Add :: scan (tail) // if plus then  -> append "Add" operator to array and call scan function with the rest of the array
        | '-' :: tail -> Sub :: scan (tail) // same as above for subtraction
        | '*' :: tail -> Mul :: scan (tail) // same as above for multiplication
        | '^' :: tail -> Exp :: scan (tail) // same as above for exponentiation
        | '%' :: tail -> Mod :: scan (tail) // same as above for modulus
        | '/' :: tail -> Div :: scan (tail) // same as above for division
        | '(' :: tail -> Lpar :: scan (tail) // same as above for left parenthesis
        | ')' :: tail -> Rpar :: scan (tail) // same as above for right parenthesis
        | '=' :: tail -> Assign :: scan (tail) // same as above for assignment operator
        | c :: tail when isAlpha c -> // if letter then start building variable string -- variable has to start with a letter - much like programming languages
            let rec buildVarString (strTail, char) =
                match strTail with
                | c :: tail when isAlpha c || isdigit c -> buildVarString (tail, char + c.ToString()) // if letter or digit then keep building the variable string
                | _ -> (strTail, char) // else return the rest of the string and the built variable string

            let (vStr, vName) = buildVarString (tail, c.ToString()) // call the buildVarString function to get the full variable name

            match vName.ToLower() with
            | "sin" -> Func Sin :: scan (vStr)
            | "cos" -> Func Cos :: scan (vStr)
            | "tan" -> Func Tan :: scan (vStr)
            | "log" -> Func Log :: scan (vStr)
            | "sqrt" -> Func Sqrt :: scan (vStr)
            | "ln" -> Func Ln :: scan (vStr)
            | "pi" -> Irr Pi :: scan (vStr)
            | _ -> Sym vName :: scan (vStr) // if not a function , then append Sym with the variable name to the array and call scan on the rest of the array

        | c :: tail when isblank c -> scan (tail) // if blank space then just call scan on the rest of the array
        | c :: tail when isdigit c ->
            let (iStr, iVal) = scNumber (tail, Int(intVal c)) // if digit then call scNumber function to get the full number (in case of multiple digits)
            // check number type and save it in
            Num iVal :: scan (iStr) // append Num with the value to the array and call scan on the rest of the array
        | _ -> raise lexError // raise lexer error if none of the above match

    scan (str2lst input) // call scan function on the input string converted to a list of characters

// Grammar in BNF:
// <E>        ::= <T> <Eopt>
// <Eopt>     ::= "+" <T> <Eopt> | "-" <T> <Eopt> | <empty>
// <T>        ::= <NR> <Topt>
// <Topt>     ::= "*" <NR> <Topt> | "/" <NR> <Topt> | <empty>
// <NR>       ::= "Num" <value> | "(" <E> ")"

let typeCoerce a b : (Number * Number) = // turns both numbers into floats unless both are integers
    match (a, b) with
    | (Int i, Int j) -> (Int i, Int j)
    | (Int i, Float f) -> (Float(float i), Float f)
    | (Float f, Int j) -> (Float f, Float(float j))
    | (Float f, Float g) -> (Float f, Float g)

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
            let (value, tval) = typeCoerce value tval

            match (value, tval) with
            | (Int v, Int tv) -> Eopt(tLst, Int(v + tv), var) // then recursively call Eopt again with the updated value
            | (Float v, Float tv) -> Eopt(tLst, Float(v + tv), var) // then recursively call Eopt again with the updated value
            | _ -> raise incompatibleTypesError
        | Sub :: tail ->
            let (tLst, tval, var) = T tail // will basically call the rest of the functions until a number is found - will also do all the multiplication and division first due to the order of the calls (Bidmas)
            let (value, tval) = typeCoerce value tval

            match (value, tval) with
            | (Int v, Int tv) -> Eopt(tLst, Int(v - tv), var)
            | (Float v, Float tv) -> Eopt(tLst, Float(v - tv), var)
            | _ -> raise incompatibleTypesError
        | _ -> (tList, value, var) // if no operator found then return the list and the value

    and T tList = (IndicesOpt >> Topt) tList

    and Topt (tList, value, var) =
        match tList with
        | Mul :: tail ->
            let (tLst, tval, var) = IndicesOpt tail
            let (value, tval) = typeCoerce value tval

            match (value, tval) with
            | (Int v, Int tv) -> Topt(tLst, Int(v * tv), var)
            | (Float v, Float tv) -> Topt(tLst, Float(v * tv), var)
            | _ -> raise incompatibleTypesError
        | Div :: tail ->
            let (tLst, tval, var) = IndicesOpt tail
            let (value, tval) = typeCoerce value tval

            if isZero tval then
                raise zeroDivisionError
            else
                match (value, tval) with
                | (Int v, Int tv) -> Topt(tLst, Int(v / tv), var)
                | (Float v, Float tv) -> Topt(tLst, Float(v / tv), var)
                | _ -> raise incompatibleTypesError
        | Mod :: tail ->
            let (tLst, tval, var) = IndicesOpt tail
            let (value, tval) = typeCoerce value tval

            if isZero tval then
                raise zeroDivisionError
            else
                match (value, tval) with
                | (Int v, Int tv) -> Topt(tLst, Int(v % tv), var) // because modulus only works on integers
                | _ -> raise modulusError
        | _ -> (tList, value, var)

    and IndicesOpt tList = (NR >> Iopt) tList // to check for indicies

    and Iopt (tList, value, var) =
        match tList with
        | Exp :: tail ->
            let (tLst, tval, var) = NR tail
            let (value, tval) = typeCoerce value tval

            match (value, tval) with
            | (Int v, Int tv) -> Iopt(tLst, Int(pown v tv), var)
            | (Float v, Float tv) -> Iopt(tLst, Float((v ** tv)), var)
            | _ -> raise incompatibleTypesError

        | _ -> (tList, value, var)

    and NR tList = // works from the bottom up  // the actual evaluation starts here then gets returned up the chain
        match tList with

        | Sub :: tail ->
            let (tLst, tval, var) = NR tail // if a negative number is found , call NR again on the rest of the list

            match tval with
            | Int v -> (tLst, Int(-v), var) // return the rest of the list , negative value and variable char
            | Float v -> (tLst, Float(-v), var) // return the rest of the list ,
        | Num value :: tail -> (tail, value, "_") // if number found then return the rest of the list , the number value and a placeholder char for variable
        | Func mFunc :: tail ->
            // if function is found , look for brackets afterwards
            match tail with
            | Lpar :: tTail ->
                let (tLst, tval, var) = E tTail // call E on the rest of the list after the left paren

                match tLst with
                | Rpar :: restTail ->
                    // if right paren found then evaluate the function here
                    let primativeVal = toPrimativeFloat tval
                    let result = mathFunc mFunc primativeVal
                    (restTail, Float result, var) // return the rest of the list after right paren , the function evaluated value and placeholder char
                | _ -> raise parseError // raise error if no right paren found
            | _ -> raise parseError // raise error if no left paren found after function
        | Irr irr :: tail ->
            match irr with
            | Pi ->
                let piValue = Irrationals.["pi"]
                (tail, Float piValue, "_") // return the rest of the list , pi value and placeholder char


        | Sym var :: tail ->
            if not tail.IsEmpty && tail.Head = Assign then
                // add another param to all functions to pass symbol char up the chain
                (tail, Int(0), var) // if assignment found after variable , return tail , 0 value ( since value will be assigned later ) and the variable character
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

// Helper functions for C# interop
let setVariable (name: string) (value: Number) : unit =
    SymbolTable <- SymbolTable.Add(name, value)

let clearVariables () : unit =
    SymbolTable <- Map.empty<string, Number>

let evaluateWithX (expr: string) (xValue: float) : float =
    SymbolTable <- SymbolTable.Add("x", Float xValue)
    let tokens = lexer expr
    let (_, result, _) = parseNeval tokens

    match result with
    | Int i -> float i
    | Float f -> f
