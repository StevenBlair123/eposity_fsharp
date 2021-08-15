module TopLevel

type Event = {Payload:string; EventType: string} 

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

module StateRepository =
    open System
    open System.Collections.Generic
    open Projections.Organisation

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
    open System
    open System.Collections.Generic

    let mutable Map = new Dictionary<System.Type, string>()
    let mutable ReverseMap = new Dictionary<string,System.Type>(StringComparer.CurrentCultureIgnoreCase)

    let AddType<'a> name = 
        ReverseMap.Add(name,typeof<'a>)
        Map.Add(typeof<'a>,name)

    let GetType typeName = 
        ReverseMap.Item(typeName)

    let LoadDomainEventsTypeDynamically() =
        //TODO: For now, we can manually wire up
        AddType<DomainEvents.Version1.OrganisationCreatedEvent> "organisationCreatedEvent"
        AddType<DomainEvents.Version1.StoreAddedEvent> "storeAddedEvent"
        AddType<DomainEvents.Version1.TaxRateCreatedForOrganisationEvent > "taxRateCreatedForOrganisationEvent"

module DomainEventFactory = 
    let CreateDomainEvent json eventType =
        let eventType = TypeMap.GetType eventType
        (JsonHelper.deserialze(json,eventType))
    
module EventHandler = 
    open DomainEvents.Version1
    open System
    open Projections.Organisation

    let private (|InvariantEqual|_|) (str:string) arg = 
      if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0
        then Some() 
      else None

    let isValidEvent eventType = 
        //TODO: Seems a bit long winded - review
        if (TypeMap.ReverseMap.ContainsKey eventType) = false then
            Logger.writeline $"Ignoring {eventType}" 
            false
        else
            true

    let getStateId (domainEvent:obj) = 
        match domainEvent with
        | :? OrganisationCreatedEvent as s -> s.organisationId
        | :? StoreAddedEvent as s -> s.organisationId
        | :? TaxRateCreatedForOrganisationEvent as s -> s.organisationId
        | _ -> System.Guid.Empty
    
    let loadState (domainEvent:obj) = 
        let organisationId = getStateId domainEvent
        let state = StateRepository.getState organisationId
        (domainEvent,state)

    let saveState (state:OrganisationState) = 
        Logger.writeline $"Saving state {state}"
        (StateRepository.saveState state)

    let postProcessing e = 
        //Logger.writeline "Applying raw SQL script"
        e

    let handleEvents (events:Event seq) = 
        let _ = 
            events
            |> Seq.filter (fun e -> isValidEvent e.EventType)  
            |> Seq.map (fun e -> 
                            Logger.writeline $"Processing {e.EventType}" //Only log out the events we are interested in
                            e)
            |> Seq.map (fun e -> DomainEventFactory.CreateDomainEvent e.Payload e.EventType)
            |> Seq.map loadState 
            |> Seq.map EventHandlers.Organisation.handleEvent                          
            |> Seq.map saveState  
            |> Seq.map postProcessing
            |> Seq.iter (fun _ -> () ) //enumerate pipeline
        ()


    

