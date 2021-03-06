module EventHandlers

module Organisation = 
    open DomainEvents.Version1
    open Projections.Organisation

    let handleEvent (event:obj, state:OrganisationState) = 
        match event with
        | :? OrganisationCreatedEvent as s -> createOrganisation(state, s)
        | :? StoreAddedEvent as s -> addStore(state, s)
        | :? TaxRateCreatedForOrganisationEvent as s -> addTaxRate (state,  {code = s.code ; name = s.name ; rateId = s.taxRateId ; taxRate = s.rate})
        | _ -> state

