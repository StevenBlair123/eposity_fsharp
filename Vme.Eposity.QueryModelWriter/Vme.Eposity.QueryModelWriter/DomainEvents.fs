module DomainEvents.Version1
open System

type OrganisationCreatedEvent = 
    {   organisationName : string
        dateRegistered : DateTime
        organisationId : Guid
    }

type StoreAddedEvent = 
    {   storeName : string
        dateRegistered : DateTime
        organisationId : Guid
        storeId : Guid
        externalStoreNumber : string
        externalStoreId : string
        externalStoreCode : string
    }

type TaxRateCreatedForOrganisationEvent = 
    {   code : int
        name : string
        taxRateId : Guid
        organisationId : Guid
        rate : decimal
    }
