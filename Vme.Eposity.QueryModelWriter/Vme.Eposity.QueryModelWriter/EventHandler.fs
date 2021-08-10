module TopLevel

type Event = {Payload:string; EventType: string} 
type OrganisationState = {id:string; count : int}
    with static member Init = { id="" ; count=0 }

module JsonHelper = 
    open FSharp.Json
    open Newtonsoft.Json.Linq

    let deserialze json = 
        Json.deserialize<Event> json

    let serialize data = 
        Json.serialize data

    let getProperyFromJson json propertyName = 
        let parsed = JObject.Parse(json)
        let property = parsed.Item(propertyName).ToString()
        property

module Logger =
    let formattedDate = System.DateTime.Now.ToString "yyyy/MM/dd HH:mm:ss.fff"
    let writeline message = printfn "%s - %s" formattedDate message

module OrganisationState = 
   
    let createOrganisation (state : OrganisationState , event : string) =
        ({state with count = state.count + 1}) 
        
    let addStore (state:OrganisationState) =
        ({state with count = state.count + 1})

module StateRepository =
    open System.Collections.Generic

    let mutable state = new Dictionary<string, OrganisationState>()

    let add (s :OrganisationState)= 
        let newState = {OrganisationState.Init with id = s.id }
        newState

    let getState organisationId = 
        if state.Count =0 || state.ContainsKey(organisationId)=false then
            let s = {OrganisationState.Init with id = organisationId }    
            state.Add (organisationId, s)

        (state.Item(organisationId))

    let saveState (s:OrganisationState) = 
        let result = state 
                    |> Seq.map (|KeyValue|) 
                    |> Map.ofSeq 
                    |> Map.remove s.id
                    |> Map.add s.id s
                    |> Map.toSeq
                    |> dict 
                    |> Dictionary

        state <- result
        s
    
module EventHandler = 
    let isValidEvent eventType = 
        match eventType with 
        | "organisationCreatedEvent" -> true
        | "storeAddedEvent" -> true
        | _ -> false

    let loadState (event:Event) = 
        let organisationId = JsonHelper.getProperyFromJson event.Payload "organisationId"
        Logger.writeline $"loading state for organisation id {organisationId}"
        let state = StateRepository.getState organisationId
        (event,state) //event and state

    let handleEvent (event:Event, state:OrganisationState) = 
        Logger.writeline $"Handling {event.EventType}" 

        match event.EventType with 
        | "organisationCreatedEvent" -> OrganisationState.createOrganisation(state, event.Payload)
        | "storeAddedEvent" -> OrganisationState.addStore state
        | _ -> state

    let saveState (state:OrganisationState) = 
        Logger.writeline $"Saving state {state}"
        (StateRepository.saveState state)

    let handleEvents (events:Event seq) = 
        let x = 
            events
            |> Seq.map (fun e -> 
                            Logger.writeline $"Processing {e.EventType}" 
                            e)
            |> Seq.filter (fun e -> isValidEvent e.EventType)  
            |> Seq.map loadState                            
            |> Seq.map handleEvent                          
            |> Seq.map saveState  
            |> Seq.map (fun e -> 
                    Logger.writeline "Applying raw SQL script" 
                    e)
            |> Seq.map (fun s -> Logger.writeline $"final state is {s}" )

        Seq.length x


    

