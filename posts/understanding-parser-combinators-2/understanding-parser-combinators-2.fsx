//=====================================================================
// Source code related to post: https://fsharpforfunandprofit.com/posts/understanding-parser-combinators-2
//
// THIS IS A GENERATED FILE. DO NOT EDIT.
// To suggest changes to this file, see instructions at
// https://github.com/swlaschin/fsharpforfunandprofit.com
//=====================================================================


//===========================================
// The parser library from part 1
//===========================================

module ParserLibraryFromPart1 =

    open System

    /// Type that represents Success/Failure in parsing
    type ParseResult<'a> =
        | Success of 'a
        | Failure of string

    /// Type that wraps a parsing function
    type Parser<'T> = Parser of (string -> ParseResult<'T * string>)

    /// Parse a single character
    let pchar charToMatch =
        // define a nested inner function
        let innerFn str =
            if String.IsNullOrEmpty(str) then
                Failure "No more input"
            else
                let first = str.[0]
                if first = charToMatch then
                    let remaining = str.[1..]
                    Success (charToMatch,remaining)
                else
                    let msg = sprintf "Expecting '%c'. Got '%c'" charToMatch first
                    Failure msg
        // return the "wrapped" inner function
        Parser innerFn

    /// Run a parser with some input
    let run parser input =
        // unwrap parser to get inner function
        let (Parser innerFn) = parser
        // call inner function with input
        innerFn input

    /// Combine two parsers as "A andThen B"
    let andThen parser1 parser2 =
        let innerFn input =
            // run parser1 with the input
            let result1 = run parser1 input

            // test the result for Failure/Success
            match result1 with
            | Failure err ->
                // return error from parser1
                Failure err

            | Success (value1,remaining1) ->
                // run parser2 with the remaining input
                let result2 =  run parser2 remaining1

                // test the result for Failure/Success
                match result2 with
                | Failure err ->
                    // return error from parser2
                    Failure err

                | Success (value2,remaining2) ->
                    // combine both values as a pair
                    let newValue = (value1,value2)
                    // return remaining input after parser2
                    Success (newValue,remaining2)

        // return the inner function
        Parser innerFn

    /// Infix version of andThen
    let ( .>>. ) = andThen

    /// Combine two parsers as "A orElse B"
    let orElse parser1 parser2 =
        let innerFn input =
            // run parser1 with the input
            let result1 = run parser1 input

            // test the result for Failure/Success
            match result1 with
            | Success result ->
                // if success, return the original result
                result1

            | Failure err ->
                // if failed, run parser2 with the input
                let result2 = run parser2 input

                // return parser2's result
                result2

        // return the inner function
        Parser innerFn

    /// Infix version of orElse
    let ( <|> ) = orElse

    /// Choose any of a list of parsers
    let choice listOfParsers =
        List.reduce ( <|> ) listOfParsers

    /// Choose any of a list of characters
    let anyOf listOfChars =
        listOfChars
        |> List.map pchar // convert into parsers
        |> choice

open ParserLibraryFromPart1

// =====================================
// `map` -- transforming the contents of a parser
// =====================================

/// Combining without a map function
module MapAttempt =

    //uncomment to see the error
    (*
    let pstring str =
        str
        |> Seq.map pchar // convert into parsers
        |> Seq.reduce andThen
    *)

    let parseDigit =
        anyOf ['0'..'9']

    let parseThreeDigits =
        parseDigit .>>. parseDigit .>>. parseDigit

    run parseThreeDigits "123A"

    (*
    Success ((('1', '2'), '3'), "A")
    *)

let mapP f parser =
    let innerFn input =
        // run parser with the input
        let result = run parser input

        // test the result for Failure/Success
        match result with
        | Success (value,remaining) ->
            // if success, return the value transformed by f
            let newValue = f value
            Success (newValue, remaining)

        | Failure err ->
            // if failed, return the error
            Failure err
    // return the inner function
    Parser innerFn

(*
val mapP :
    f:('a -> 'b) -> Parser<'a> -> Parser<'b>
*)

// infix version of `map`

let ( <!> ) = mapP

// This makes using `map` with the pipeline idiom much more convenient:

let ( |>> ) x f = mapP f x

/// Using the "map" function to build other parsers
module MapExample =

    let parseDigit = anyOf ['0'..'9']

    let parseThreeDigitsAsStr =
        // create a parser that returns a tuple
        let tupleParser =
            parseDigit .>>. parseDigit .>>. parseDigit

        // create a function that turns the tuple into a string
        let transformTuple ((c1, c2), c3) =
            System.String [| c1; c2; c3 |]

        // use "map" to combine them
        mapP transformTuple tupleParser

/// Test the "map" function
module Map_Test =

    let parseDigit = anyOf ['0'..'9']

    let parseThreeDigitsAsStr =
        (parseDigit .>>. parseDigit .>>. parseDigit)
        |>> fun ((c1, c2), c3) -> System.String [| c1; c2; c3 |]

    run parseThreeDigitsAsStr "123A"  // Success ("123", "A")

    let parseThreeDigitsAsInt =
        mapP int parseThreeDigitsAsStr

    run parseThreeDigitsAsInt "123A"  // Success (123, "A")

    (*
    val parseThreeDigitsAsInt : Parser<int>
    *)

// =====================================
// `apply` and `return` -- lifting functions to the world of Parsers
// =====================================

let returnP x =
    let innerFn input =
        // ignore the input and return x
        Success (x,input )
    // return the inner function
    Parser innerFn

(*
val returnP :
    'a -> Parser<'a>
*)

let applyP fP xP =
    // create a Parser containing a pair (f,x)
    (fP .>>. xP)
    // map the pair by applying f to x
    |> mapP (fun (f,x) -> f x)

(*
val applyP :
    Parser<('a -> 'b)> -> Parser<'a> -> Parser<'b>
*)

// The infix version of `applyP`
let ( <*> ) = applyP


// lift a two parameter function to Parser World
let lift2 f xP yP =
    returnP f <*> xP <*> yP

(*
val lift2 :
    f:('a -> 'b -> 'c) -> Parser<'a> -> Parser<'b> -> Parser<'c>
*)

/// Test the lifting functions
module Lift_Test =

    let addP =
        lift2 (+)

    (*
    val addP :
        Parser<int> -> Parser<int> -> Parser<int>
    *)


    let startsWith (str:string) (prefix:string) =
        str.StartsWith(prefix)

    let startsWithP =
        lift2 startsWith

    (*
    val startsWith :
        str:string -> prefix:string -> bool

    val startsWithP :
        Parser<string> -> Parser<string> -> Parser<bool>
    *)

// =====================================
// `sequence` -- transforming a list of Parsers into a single Parser
// =====================================

let rec sequence parserList =
    // define the "cons" function, which is a two parameter function
    let cons head tail = head::tail

    // lift it to Parser World
    let consP = lift2 cons

    // process the list of parsers recursively
    match parserList with
    | [] ->
        returnP []
    | head::tail ->
        consP head (sequence tail)

(*
val sequence :
    Parser<'a> list -> Parser<'a list>
*)


/// Test the "sequence" function
module Sequence_Test =

    let parsers = [ pchar 'A'; pchar 'B'; pchar 'C' ]
    let combined = sequence parsers

    run combined "ABCD"
    // Success (['A'; 'B'; 'C'], "D")

// =====================================
// Implementing the `pstring` parser
// =====================================


/// Helper to create a string from a list of chars
let charListToStr charList =
     charList |> List.toArray |> System.String

// match a specific string
let pstring str =
    str
    // convert to list of char
    |> List.ofSeq
    // map each char to a pchar
    |> List.map pchar
    // convert to Parser<char list>
    |> sequence
    // convert Parser<char list> to Parser<string>
    |> mapP charListToStr

module PString_Test =

    let parseABC = pstring "ABC"

    run parseABC "ABCDE"  // Success ("ABC", "DE")
    run parseABC "A|CDE"  // Failure "Expecting 'B'. Got '|'"
    run parseABC "AB|DE"  // Failure "Expecting 'C'. Got '|'"

// =====================================
// `many` and `many1` -- matching a parser multiple times
// =====================================

let rec parseZeroOrMore parser input =
    // run parser with the input
    let firstResult = run parser input
    // test the result for Failure/Success
    match firstResult with
    | Failure err ->
        // if parse fails, return empty list
        ([],input)
    | Success (firstValue,inputAfterFirstParse) ->
        // if parse succeeds, call recursively
        // to get the subsequent values
        let (subsequentValues,remainingInput) =
            parseZeroOrMore parser inputAfterFirstParse
        let values = firstValue::subsequentValues
        (values,remainingInput)

/// match zero or more occurrences of the specified parser
let many parser =

    let innerFn input =
        // parse the input -- wrap in Success as it always succeeds
        Success (parseZeroOrMore parser input)

    Parser innerFn

(*
val many :
    Parser<'a> -> Parser<'a list>
*)

/// Test the "many" function
module Many_Test =

    // test #1
    let manyA = many (pchar 'A')

    // test some success cases
    run manyA "ABCD"  // Success (['A'], "BCD")
    run manyA "AACD"  // Success (['A'; 'A'], "CD")
    run manyA "AAAD"  // Success (['A'; 'A'; 'A'], "D")

    // test a case with no matches
    run manyA "|BCD"  // Success ([], "|BCD")

    // test #2
    let manyAB = many (pstring "AB")

    run manyAB "ABCD"  // Success (["AB"], "CD")
    run manyAB "ABABCD"  // Success (["AB"; "AB"], "CD")
    run manyAB "ZCD"  // Success ([], "ZCD")
    run manyAB "AZCD"  // Success ([], "AZCD")

    // test #3
    let whitespaceChar = anyOf [' '; '\t'; '\n']
    let whitespace = many whitespaceChar

    run whitespace "ABC"  // Success ([], "ABC")
    run whitespace " ABC"  // Success ([' '], "ABC")
    run whitespace "\tABC"  // Success (['\t'], "ABC")


/// match one or more occurrences of the specified parser
let many1 parser =
    let innerFn input =
        // run parser with the input
        let firstResult = run parser input
        // test the result for Failure/Success
        match firstResult with
        | Failure err ->
            Failure err // failed
        | Success (firstValue,inputAfterFirstParse) ->
            // if first found, look for zeroOrMore now
            let (subsequentValues,remainingInput) =
                parseZeroOrMore parser inputAfterFirstParse
            let values = firstValue::subsequentValues
            Success (values,remainingInput)
    Parser innerFn

(*
val many1 :
    Parser<'a> -> Parser<'a list>
*)


/// Test the "many1" function
module Many1_Test =

    // define parser for one digit
    let digit = anyOf ['0'..'9']

    // define parser for one or more digits
    let digits = many1 digit

    run digits "1ABC"  // Success (['1'], "ABC")
    run digits "12BC"  // Success (['1'; '2'], "BC")
    run digits "123C"  // Success (['1'; '2'; '3'], "C")
    run digits "1234"  // Success (['1'; '2'; '3'; '4'], "")

    run digits "ABC"   // Failure "Expecting '9'. Got 'A'"

// =====================================
// Parsing an integer
// =====================================

/// This implementation does not handle negative numbers
module PIntV1 =
    let pint =
        // helper
        let resultToInt digitList =
            // ignore int overflow for now
            digitList |> List.toArray |> System.String |> int

        // define parser for one digit
        let digit = anyOf ['0'..'9']

        // define parser for one or more digits
        let digits = many1 digit

        // map the digits to an int
        digits
        |> mapP resultToInt


module PIntV1_Test =
    open PIntV1

    run pint "1ABC"  // Success (1, "ABC")
    run pint "12BC"  // Success (12, "BC")
    run pint "123C"  // Success (123, "C")
    run pint "1234"  // Success (1234, "")

    run pint "ABC"   // Failure "Expecting '9'. Got 'A'"

// =====================================
// `opt` -- matching a parser zero or one time
// =====================================

let opt p =
    let some = p |>> Some
    let none = returnP None
    some <|> none

module Opt_Test =
    let digit = anyOf ['0'..'9']
    let digitThenSemicolon = digit .>>. opt (pchar ';')

    run digitThenSemicolon "1;"  // Success (('1', Some ';'), "")
    run digitThenSemicolon "1"   // Success (('1', None), "")


let pint =
    // helper
    let resultToInt (sign,charList) =
        let i = charList |> List.toArray |> System.String |> int
        match sign with
        | Some ch -> -i  // negate the int
        | None -> i

    // define parser for one digit
    let digit = anyOf ['0'..'9']

    // define parser for one or more digits
    let digits = many1 digit

    // parse and convert
    opt (pchar '-') .>>. digits
    |>> resultToInt

module PIntV2_Test =

    run pint "123C"   // Success (123, "C")
    run pint "-123C"  // Success (-123, "C")

// =====================================
// Throwing results away
// =====================================

/// Keep only the result of the left side parser
let (.>>) p1 p2 =
    // create a pair
    p1 .>>. p2
    // then only keep the first value
    |> mapP (fun (a,b) -> a)

/// Keep only the result of the right side parser
let (>>.) p1 p2 =
    // create a pair
    p1 .>>. p2
    // then only keep the second value
    |> mapP (fun (a,b) -> b)


module Discard_Test =

    let digit = anyOf ['0'..'9']

    // use .>> below
    let digitThenSemicolon = digit .>> opt (pchar ';')

    run digitThenSemicolon "1;"  // Success ('1', "")
    run digitThenSemicolon "1"   // Success ('1', "")

    // How about an example with whitespace?

    let whitespaceChar = anyOf [' '; '\t'; '\n']
    let whitespace = many1 whitespaceChar

    let ab = pstring "AB"
    let cd = pstring "CD"
    let ab_cd = (ab .>> whitespace) .>>. cd

    run ab_cd "AB \t\nCD"   // Success (("AB", "CD"), "")

// =====================================
// Introducing `between`
// =====================================


/// Keep only the result of the middle parser
let between p1 p2 p3 =
    p1 >>. p2 .>> p3

module Between_Test =

    let pdoublequote = pchar '"'
    let quotedInteger = between pdoublequote pint pdoublequote

    run quotedInteger "\"1234\""   // Success (1234, "")
    run quotedInteger "1234"       // Failure "Expecting '"'. Got '1'"

    let pspace = anyOf [' '; '\t'; '\n'; '\r']
    let pwhitespace = many pspace
    let ignoreWhitespaceAround p1 = between pwhitespace p1 pwhitespace

    let parseABC = pstring "ABC"
    run parseABC " ABC "   // fails because of whitespace
                           // Failure "Expecting 'A'. Got ' '"

    let parse_ABC_ = ignoreWhitespaceAround parseABC
    run parse_ABC_ " ABC " //  Success ("ABC", "")
    run parse_ABC_ " \tABC\n " //  Success ("ABC", "")


// =====================================
// Parsing lists with separators
// =====================================

/// Parses one or more occurrences of p separated by sep
let sepBy1 p sep =
    let sepThenP = sep >>. p
    p .>>. many sepThenP
    |>> fun (p,pList) -> p::pList

/// Parses zero or more occurrences of p separated by sep
let sepBy p sep =
    sepBy1 p sep <|> returnP []

module SepBy_Test =

    let comma = pchar ','
    let digit = anyOf ['0'..'9']

    let zeroOrMoreDigitList = sepBy digit comma
    let oneOrMoreDigitList = sepBy1 digit comma

    run oneOrMoreDigitList "1;"      // Success (['1'], ";")
    run oneOrMoreDigitList "1,2;"    // Success (['1'; '2'], ";")
    run oneOrMoreDigitList "1,2,3;"  // Success (['1'; '2'; '3'], ";")
    run oneOrMoreDigitList "Z;"      // Failure "Expecting '9'. Got 'Z'"

    run zeroOrMoreDigitList "1;"     // Success (['1'], ";")
    run zeroOrMoreDigitList "1,2;"   // Success (['1'; '2'], ";")
    run zeroOrMoreDigitList "1,2,3;" // Success (['1'; '2'; '3'], ";")
    run zeroOrMoreDigitList "Z;"     // Success ([], "Z;")

// =====================================
// What about `bind`?
// =====================================

/// "bindP" takes a parser-producing function f, and a parser p
/// and passes the output of p into f, to create a new parser
let bindP f p =
    let innerFn input =
        let result1 = run p input
        match result1 with
        | Failure err ->
            // return error from parser1
            Failure err
        | Success (value1,remainingInput) ->
            // apply f to get a new parser
            let p2 = f value1
            // run parser with remaining input
            run p2 remainingInput
    Parser innerFn

(*
val bindP :
    f:('a -> Parser<'b>) -> Parser<'a> -> Parser<'b>
*)

let ( >>= ) p f = bindP f p

// =====================================
// Reimplementing other combinators with `bindP` and `returnP`
// =====================================

module BindP_Reimplementations =

    let mapP f =
        bindP (f >> returnP)

    let andThen p1 p2 =
        p1 >>= (fun p1Result ->
        p2 >>= (fun p2Result ->
            returnP (p1Result,p2Result) ))

    let applyP fP xP =
        fP >>= (fun f ->
        xP >>= (fun x ->
            returnP (f x) ))

    // (assuming "many" is defined)

    let many1 p =
        p      >>= (fun head ->
        many p >>= (fun tail ->
            returnP (head::tail) ))

//===========================================
// The complete parser library so far
//===========================================

module ParserLibrary_Complete =

    open System

    /// Type that represents Success/Failure in parsing
    type ParseResult<'a> =
        | Success of 'a
        | Failure of string

    /// Type that wraps a parsing function
    type Parser<'T> = Parser of (string -> ParseResult<'T * string>)

    /// Parse a single character
    let pchar charToMatch =
        // define a nested inner function
        let innerFn str =
            if String.IsNullOrEmpty(str) then
                Failure "No more input"
            else
                let first = str.[0]
                if first = charToMatch then
                    let remaining = str.[1..]
                    Success (charToMatch,remaining)
                else
                    let msg = sprintf "Expecting '%c'. Got '%c'" charToMatch first
                    Failure msg
        // return the "wrapped" inner function
        Parser innerFn

    /// Run a parser with some input
    let run parser input =
        // unwrap parser to get inner function
        let (Parser innerFn) = parser
        // call inner function with input
        innerFn input

    /// "bindP" takes a parser-producing function f, and a parser p
    /// and passes the output of p into f, to create a new parser
    let bindP f p =
        let innerFn input =
            let result1 = run p input
            match result1 with
            | Failure err ->
                // return error from parser1
                Failure err
            | Success (value1,remainingInput) ->
                // apply f to get a new parser
                let p2 = f value1
                // run parser with remaining input
                run p2 remainingInput
        Parser innerFn

    /// Infix version of bindP
    let ( >>= ) p f = bindP f p

    /// Lift a value to a Parser
    let returnP x =
        let innerFn input =
            // ignore the input and return x
            Success (x,input)
        // return the inner function
        Parser innerFn

    /// apply a function to the value inside a parser
    let mapP f =
        bindP (f >> returnP)

    /// infix version of mapP
    let ( <!> ) = mapP

    /// "piping" version of mapP
    let ( |>> ) x f = mapP f x

    /// apply a wrapped function to a wrapped value
    let applyP fP xP =
        fP >>= (fun f ->
        xP >>= (fun x ->
            returnP (f x) ))

    /// infix version of apply
    let ( <*> ) = applyP

    /// lift a two parameter function to Parser World
    let lift2 f xP yP =
        returnP f <*> xP <*> yP

    /// Combine two parsers as "A andThen B"
    let andThen p1 p2 =
        p1 >>= (fun p1Result ->
        p2 >>= (fun p2Result ->
            returnP (p1Result,p2Result) ))

    /// Infix version of andThen
    let ( .>>. ) = andThen

    /// Combine two parsers as "A orElse B"
    let orElse p1 p2 =
        let innerFn input =
            // run parser1 with the input
            let result1 = run p1 input

            // test the result for Failure/Success
            match result1 with
            | Success result ->
                // if success, return the original result
                result1

            | Failure err ->
                // if failed, run parser2 with the input
                let result2 = run p2 input

                // return parser2's result
                result2

        // return the inner function
        Parser innerFn

    /// Infix version of orElse
    let ( <|> ) = orElse

    /// Choose any of a list of parsers
    let choice listOfParsers =
        List.reduce ( <|> ) listOfParsers

    /// Choose any of a list of characters
    let anyOf listOfChars =
        listOfChars
        |> List.map pchar // convert into parsers
        |> choice

    /// Convert a list of Parsers into a Parser of a list
    let rec sequence parserList =
        // define the "cons" function, which is a two parameter function
        let cons head tail = head::tail

        // lift it to Parser World
        let consP = lift2 cons

        // process the list of parsers recursively
        match parserList with
        | [] ->
            returnP []
        | head::tail ->
            consP head (sequence tail)

    /// (helper) match zero or more occurrences of the specified parser
    let rec parseZeroOrMore parser input =
        // run parser with the input
        let firstResult = run parser input
        // test the result for Failure/Success
        match firstResult with
        | Failure err ->
            // if parse fails, return empty list
            ([],input)
        | Success (firstValue,inputAfterFirstParse) ->
            // if parse succeeds, call recursively
            // to get the subsequent values
            let (subsequentValues,remainingInput) =
                parseZeroOrMore parser inputAfterFirstParse
            let values = firstValue::subsequentValues
            (values,remainingInput)

    /// matches zero or more occurrences of the specified parser
    let many parser =
        let innerFn input =
            // parse the input -- wrap in Success as it always succeeds
            Success (parseZeroOrMore parser input)

        Parser innerFn

    /// matches one or more occurrences of the specified parser
    let many1 p =
        p      >>= (fun head ->
        many p >>= (fun tail ->
            returnP (head::tail) ))

    /// Parses an optional occurrence of p and returns an option value.
    let opt p =
        let some = p |>> Some
        let none = returnP None
        some <|> none

    /// Keep only the result of the left side parser
    let (.>>) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the first value
        |> mapP (fun (a,b) -> a)

    /// Keep only the result of the right side parser
    let (>>.) p1 p2 =
        // create a pair
        p1 .>>. p2
        // then only keep the second value
        |> mapP (fun (a,b) -> b)

    /// Keep only the result of the middle parser
    let between p1 p2 p3 =
        p1 >>. p2 .>> p3

    /// Parses one or more occurrences of p separated by sep
    let sepBy1 p sep =
        let sepThenP = sep >>. p
        p .>>. many sepThenP
        |>> fun (p,pList) -> p::pList

    /// Parses zero or more occurrences of p separated by sep
    let sepBy p sep =
        sepBy1 p sep <|> returnP []
