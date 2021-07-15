namespace Vme.Eposity.QueryModelWriter

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Program =
    let createHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder.UseStartup<Startup>() |> ignore
            )

    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // Exit code

//TODO: Query REST
//2. C# project for subscription service 
//  2.1 Could use a F# REST and inject the internal SS (C#) in?
//3. Domain Events Module
//4. EF / repo
//5. Organisation State record
//6. IoC
//7. REST - inject internal SS
//8. How does C# pass domain events to F# - deserialise in C#?
//9. Logging
//10.LoadTypes = should we pass into the SS