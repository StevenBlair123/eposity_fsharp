module Projections
open DomainEvents.Version1

module Organisation = 
    open System

    type Tax = {
        code : int
        name : string
        rateId : Guid
        taxRate : decimal
        }

    type OrganisationState = {
            id:Guid 
            name : string
            taxRates : Tax list
        }
        with static member Init = { id=Guid.Empty ; name=null ; taxRates = list.Empty}

    let createOrganisation (state:OrganisationState,  event:OrganisationCreatedEvent) =
        ({state with name= event.organisationName;id = event.organisationId}) 
        
    let addStore (state:OrganisationState, event : StoreAddedEvent) =
        state

    let addTaxRate (state, tax ) = 
        //TODO: handle updates
        let newlist = tax :: state.taxRates
        ({state with taxRates= newlist})

