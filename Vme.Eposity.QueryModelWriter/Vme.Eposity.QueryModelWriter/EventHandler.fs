module TopLevel

type Event = {Payload:string; EventType: string} 
type OrganisationState = 
    {
        id:System.Guid 
        name : string
        count : int
    }
    with static member Init = { id=System.Guid.Empty ; count=0 ; name=null }

module JsonHelper = 
    open System
    open FSharp.Json

    let deserialze (json:string, eventType:Type) = 
        let mutable options = System.Text.Json.JsonSerializerOptions()
        (System.Text.Json.JsonSerializer.Deserialize(json,eventType,options))

    let serialize data = 
        Json.serialize data

module Logger =
    let private formattedDate = System.DateTime.Now.ToString "yyyy/MM/dd HH:mm:ss.fff"
    let writeline message = printfn "%s - %s" formattedDate message

module OrganisationState = 
    let createOrganisation (state : OrganisationState , event : DomainEvents.Version1.OrganisationCreatedEvent) =
        ({state with count = state.count + 1 ; name= event.organisationName}) 
        
    let addStore (state:OrganisationState, event : DomainEvents.Version1.StoreAddedEvent) =
        ({state with count = state.count + 1})

module StateRepository =
    open System
    open System.Collections.Generic

    let mutable state = new Dictionary<Guid, OrganisationState>()

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

module TypeMap = 
    open System.Collections.Generic

    let mutable Map = new Dictionary<System.Type, string>()
    let mutable ReverseMap = new Dictionary<string,System.Type>()

    let AddType<'a> name = 
        ReverseMap.Add(name,typeof<'a>)
        Map.Add(typeof<'a>,name)

    let GetType typeName = 
        //TODO: ignore case
        ReverseMap.Item(typeName)

    let LoadDomainEventsTypeDynamically() =
        //TODO: For now, we can manually wire up
        AddType<DomainEvents.Version1.OrganisationCreatedEvent> "organisationCreatedEvent"
        AddType<DomainEvents.Version1.StoreAddedEvent> "storeAddedEvent"

module DomainEventFactory = 
    let CreateDomainEvent json eventType =
        let eventType = TypeMap.GetType eventType
        (JsonHelper.deserialze(json,eventType))
    
module EventHandler = 
    open DomainEvents.Version1

    let isValidEvent eventType = 
        //TODO: Check TypeMap instead
        match eventType with 
        | "organisationCreatedEvent" -> true
        | "storeAddedEvent" -> true
        | _ -> false

    let getStateId (domainEvent:obj) = 
        match domainEvent with
        | :? OrganisationCreatedEvent as s -> s.organisationId
        | :? StoreAddedEvent as s -> s.organisationId
        | _ -> System.Guid.Empty
    
    let loadState (domainEvent:obj) = 
        let organisationId = getStateId domainEvent
        let state = StateRepository.getState organisationId
        (domainEvent,state)

    let handleEvent (event:obj, state:OrganisationState) = 
        match event with
        | :? OrganisationCreatedEvent as s -> OrganisationState.createOrganisation(state, s)
        | :? StoreAddedEvent as s -> OrganisationState.addStore(state, s)
        | _ -> state

    let saveState (state:OrganisationState) = 
        Logger.writeline $"Saving state {state}"
        (StateRepository.saveState state)

    let handleEvents (events:Event seq) = 
        let _ = 
            events
            |> Seq.map (fun e -> 
                            Logger.writeline $"Processing {e.EventType}" 
                            e)
            |> Seq.filter (fun e -> isValidEvent e.EventType)  
            |> Seq.map (fun e -> DomainEventFactory.CreateDomainEvent e.Payload e.EventType)
            |> Seq.map loadState 
            |> Seq.map handleEvent                          
            |> Seq.map saveState  
            |> Seq.map (fun e -> 
                    Logger.writeline "Applying raw SQL script" 
                    e)
            |> Seq.iter (fun s -> Logger.writeline $"final state is {s}" ) //enumerate pipeline
        ()


    

