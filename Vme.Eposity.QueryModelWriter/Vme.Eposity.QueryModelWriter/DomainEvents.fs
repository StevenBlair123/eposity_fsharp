module DomainEvents.Version1
open System


type OrganisationCreatedEvent = 
    {   organisationName : string
        dateRegistered : DateTime
        organisationId : Guid
    }
