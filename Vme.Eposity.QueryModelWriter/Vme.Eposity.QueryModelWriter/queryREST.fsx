open System

//TODO: Query REST
//1. Github repo
//2. C# project for subscription service (probably REST)
//  2.1 Could use a F# REST and inject the internal SS (C#) in?
//3. Domain Events Module
//4. EF / repo
//5. Organisation State record

let console message = printfn message

//Start query rest event handler
let isValidEvent event = 
    console "is %s valid?" event
    match event with 
    | "orderCreated" -> true
    | _ -> false

let loadState event = 
    console "loadState from %s" event
    (event,0)

let createOrder state =
    console "creating order"
    state //this would be the new state

let handleEvent (event, state:int) = 
    let newState = 
        match event with 
        | "orderCreated" -> createOrder state
        | _ -> state

    newState

let saveState state = 
    console "Saving state %d" state
    state

//Entry point
let events = [ 
    "orderCreated" 
    "orderCreated"
    "orderItemAdded"] 

events
|> Seq.filter isValidEvent                      
|> Seq.map (fun a -> 
                console "super logging for everyone" 
                a)
|> Seq.map loadState                            
|> Seq.map handleEvent                          
|> Seq.map saveState    
|> (fun s -> 
        console "Applying raw SQL script" 
        0) 
//End query rest event handler





//TODO: what is happening between Seq.map Seq.fold and Seq.reduce
//|> Seq.reduce (fun x y -> ("",1)) - we would use this when we need to take elemetns and create something new


//Don'think this is what we need to for an eventhandler / commandhandler



