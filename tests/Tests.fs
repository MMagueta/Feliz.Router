module Tests

open Feliz.Router
open Fable.Mocha
open Fable.SimpleJson

type test =
    static member pass() = Expect.isTrue true "It must be true"
    static member fail() = Expect.isTrue false "It must be false"
    static member failwith x = failwith x
    static member passWith x = Expect.isTrue true

let itCompiles() = Router.navigate "users"

let routerTests =
    testList "Router tests" [
        testCase "Route.Int works" <| fun _ ->
            match [ "5"; "-1";  "" ] with
            | [ Route.Int 5; Route.Int -1; "" ] -> test.pass()
            | [ Route.Int 5; Route.Int -1; Route.Int value ] -> test.failwith (string value)
            | _ -> test.fail()

        testCase "Router.urlSegments works" <| fun _ ->
            [
                "", [ ]
                "#" , [ ]
                "#/", [ ]
                "#/?", [ ]
                "#?", [ ]
                "#home", [ "home" ]
                "#home/", [ "home" ]
                "#/home", [ "home" ]
                "#/home/", [ "home" ]
                "#/home/users", [ "home"; "users" ]
                "#/home/users/", [ "home"; "users" ]
                "#/home/users/settings", [ "home"; "users"; "settings" ]
                "#/home/users/1", [ "home"; "users"; "1" ]
                "#/users?id=1", [ "users"; "?id=1" ]
                @"#/search?q=whats%20up", [ "search"; @"?q=whats%20up" ]
                "#/?token=jwt", [ "?token=jwt" ]
                "#?token=jwt", [ "?token=jwt" ]
                "#?pretty", [ "?pretty" ]
                "/", [ ]
                "/?", [ ]
                "?", [ ]
                "home", [ "home" ]
                "home/", [ "home" ]
                "/home", [ "home" ]
                "/home/", [ "home" ]
                "/home/users", [ "home"; "users" ]
                "/home/users/", [ "home"; "users" ]
                "/home/users/settings", [ "home"; "users"; "settings" ]
                "/home/users/1", [ "home"; "users"; "1" ]
                "/users?id=1", [ "users"; "?id=1" ]
                @"/search?q=whats%20up", [ "search"; @"?q=whats%20up" ]
                "/?token=jwt", [ "?token=jwt" ]
                "?token=jwt", [ "?token=jwt" ]
                "?pretty", [ "?pretty" ]
            ]
            |> List.iter (fun (input, output) -> 
                Expect.equal (Router.urlSegments input RouteMode.Hash) output (sprintf "Input of %s should output %A" input output))

        testCase "RouteMode affects how the URL segments are cleaned up" <| fun _ ->
            ("/some/path#", RouteMode.Hash)
            ||> Router.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

            ("/Feliz.MaterialUI/#", RouteMode.Hash)
            ||> Router.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

            ("/Feliz.MaterialUI#", RouteMode.Hash)
            ||> Router.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

            ("/some/path#/", RouteMode.Hash)
            ||> Router.urlSegments
            |> fun output -> Expect.equal output [ ] "Hash at the end means route starts there"

            ("/some/path#", RouteMode.Path)
            ||> Router.urlSegments
            |> fun output -> Expect.equal output ["some";"path"] "Path segments are read correctly"

            ("/Feliz.MaterialUI#", RouteMode.Path)
            ||> Router.urlSegments
            |> fun output -> Expect.equal output [ "Feliz.MaterialUI" ] "Path segments are read correctly"

        testCase "Router.urlSegments decodes URL segments" <| fun _ ->
            let hashInput = "#/Hello%20World"
            let pathInput = "/Hello%20World"
            let expected  = [ "Hello World" ]
            Expect.equal expected (Router.urlSegments hashInput RouteMode.Hash)  "They are equal"
            Expect.equal expected (Router.urlSegments pathInput RouteMode.Path)  "They are equal"

        testCase "Route.Query works" <| fun _ ->
            match [ "users"; "?id=1" ] with
            | [ "users"; Route.Query [ "id", "1" ] ] -> test.pass()
            | otherwise -> test.fail()

            match [ "users"; "?id=1" ] with
            | [ "users"; Route.Query [ "id", Route.Int userId ] ] -> Expect.equal userId 1 "They are equal"
            | otherwise -> test.fail()

            match [ "users"; "?id=1&limit=5" ] with
            | [ "users"; Route.Query [ "id", Route.Int userId; "limit", Route.Int limit ] ] ->
                Expect.equal userId 1 "They are equal"
                Expect.equal limit 5 "They are equal"
            | otherwise ->
                test.fail()

        testCase "Boolean query string paramters" <| fun _ ->
            match "?value=true" with
            | Route.Query [ "value", Route.Bool true ] -> test.pass()
            | otherwise -> test.fail()

            match "?value=1" with
            | Route.Query [ "value", Route.Bool true ] -> test.pass()
            | otherwise -> test.fail()

            match "?value=0" with
            | Route.Query [ "value", Route.Bool false ] -> test.pass()
            | otherwise -> test.fail()

            match "?value=false" with
            | Route.Query [ "value", Route.Bool false ] -> test.pass()
            | otherwise -> test.fail()

            match "?users=all&pretty" with
            | Route.Query [ "users", "all"; "pretty", "" ]  -> test.pass()
            | otherwise -> test.fail()

        testCase "Double and decimal Route" <| fun _ ->
            match "?lng=12.12411241&lat=2.3451241" with
            | Route.Query [ "lng", Route.Decimal 12.12411241M; "lat", Route.Decimal 2.3451241M ] ->
                test.pass()
            | otherwise ->
                test.fail()

            match "?lng=12.12&lat=2.345" with
            | Route.Query [ "lng", Route.Number 12.12; "lat", Route.Number 2.345 ] ->
                test.pass()
            | otherwise ->
                test.fail()

        testCase "encodeQueryString for a single argument" <| fun _ ->
            let input = [ "id", "1" ]
            let expected = "?id=1"
            let actual = Router.encodeQueryString input
            Expect.equal actual expected "They are equal"

        testCase "encodeQueryString for an empty map" <| fun _ ->
            let input = [  ]
            let expected = ""
            let actual = Router.encodeQueryString input
            Expect.equal actual expected "They are equal"

        testCase "encodeQueryString for multiple arguments" <| fun _ ->
            let input = [ "id", "1"; "limit", "5" ]
            let expected = "?id=1&limit=5"
            let actual = Router.encodeQueryString input
            Expect.equal actual expected "They are equal"

        testCase "encode segments works" <| fun _ ->
            [
                [ "users" ], "#/users"
                [ "#/home" ], "#/home"
                [ "#/hello?value=1" ], "#/hello?value=1"
                [ "#/hello/home" ], "#/hello/home"
                [ "#about" ], "#/about"
                [ "users"; "home" ], "#/users/home"
                [ "#one"; "two" ], "#/one/two"
                [ "users"; "1" ], "#/users/1"
                [ "users" + Router.encodeQueryString [ "id", "1" ] ], "#/users?id=1"
                [ "search" + Router.encodeQueryString [ "q", "whats up" ] ], @"#/search?q=whats%20up"
                [ "products" + Router.encodeQueryStringInts [ "id", 1 ] ], "#/products?id=1"
                [ "users" + Router.encodeQueryString [ ] ], "#/users"
            ]
            |> List.iter (fun (input, output) -> Expect.equal (Router.encodeParts input RouteMode.Hash) output "They are equal")

            [
                [ "users" ], "/users"
                [ "/home" ], "/home"
                [ "/hello?value=1" ], "/hello?value=1"
                [ "/hello/home" ], "/hello/home"
                [ "about" ], "/about"
                [ "users"; "home" ], "/users/home"
                [ "/one"; "two" ], "/one/two"
                [ "users"; "1" ], "/users/1"
                [ "users" + Router.encodeQueryString [ "id", "1" ] ], "/users?id=1"
                [ "search" + Router.encodeQueryString [ "q", "whats up" ] ], @"/search?q=whats%20up"
                [ "products" + Router.encodeQueryStringInts [ "id", 1 ] ], "/products?id=1"
                [ "users" + Router.encodeQueryString [ ] ], "/users"
            ]
            |> List.iter (fun (input, output) -> Expect.equal (Router.encodeParts input RouteMode.Path) output "They are equal")
        
        testCase "Router.formatPath overload with segment list and query works" (fun _ ->
            [
                [], ["foo","bar"], "/?foo=bar"
                [ "hello" ], [], "/hello"
                [ "hello" ], ["foo","bar"], "/hello?foo=bar"
                [ "hello"; "friend" ], ["foo","bar"], "/hello/friend?foo=bar"
            ]
            |> List.iter (fun (sgs,qry,res) -> Expect.equal (Router.formatPath(sgs,qry)) res "They are equal")
        )
        
        testCase "Router.format overload with segment list and query works" (fun _ ->
            [
                [], ["foo","bar"], "#/?foo=bar"
                [ "hello" ], [], "#/hello"
                [ "hello" ], ["foo","bar"], "#/hello?foo=bar"
                [ "hello"; "friend" ], ["foo","bar"], "#/hello/friend?foo=bar"
            ]
            |> List.iter (fun (sgs,qry,res) -> Expect.equal (Router.format(sgs,qry)) res "They are equal")
        )
    ]

[<EntryPoint>]
let main args = Mocha.runTests routerTests