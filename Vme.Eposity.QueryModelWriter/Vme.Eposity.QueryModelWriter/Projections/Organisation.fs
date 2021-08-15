module Projections
open DomainEvents.Version1

module Organisation = 
    open System

    type Tax = {
        code : int
        name : string
        rateId : Guid
        }

    type OrganisationState = 
        {
            id:System.Guid 
            name : string
            taxRates : Tax list
        }
        with static member Init = { id=Guid.Empty ; name=null ; taxRates = list.Empty}

    let createOrganisation (state:OrganisationState,  event:OrganisationCreatedEvent) =
        ({state with name= event.organisationName;id = event.organisationId}) 
        
    let addStore (state:OrganisationState, event : StoreAddedEvent) =
        state

    let addTaxRate (state, (code:int, name:string, taxRateId:System.Guid) ) = 
        //TODO: handle updates
        let tax:Tax = {code = code; name = name; rateId = taxRateId}
   
        let newlist = tax :: state.taxRates
        ({state with taxRates= newlist})

