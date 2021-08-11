using System;

namespace ConsoleApp1 {
    using System.Collections.Generic;
    using Microsoft.FSharp.Collections;

    class Program {
        static void Main(string[] args) {

            //TODO: Subscription Service / Eventous
            //feed into F# entry point
            //ActionBlock in C# for grouping or in f#?

            //Bootstrap
            TopLevel.TypeMap.LoadDomainEventsTypeDynamically();

            List<TopLevel.Event> events = new ();

            String event1 = "{\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"dateRegistered\": \"2021-05-25T13:33:21.0411205+00:00\",\r\n  \"organisationName\": \"Richard Inglis\"\r\n}";
            String event2 = "{\r\n  \"externalStoreCode\": \"DEFAULT_STORE\",\r\n  \"externalStoreId\": \"HAR\",\r\n  \"externalStoreNumber\": \"709\",\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"storeId\": \"1902ddab-f568-b136-0822-fa4a9720fe14\",\r\n  \"storeName\": \"Harbour Parade\"\r\n}";
            String event3 = "{\r\n  \"externalStoreCode\": \"9781C712-ADBC-CE44-9D-A3B9D90313AAE8\",\r\n  \"externalStoreId\": \"FH \",\r\n  \"externalStoreNumber\": \"3\",\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"storeId\": \"b4aab2ad-547f-6a42-2aec-cd4118828737\",\r\n  \"storeName\": \"FH SCO\"\r\n}";
            String event4 = "{\r\n  \"externalStoreCode\": \"00CE22AA-E7BA-A747-97-652AD86AD11B49\",\r\n  \"externalStoreId\": \"LQ \",\r\n  \"externalStoreNumber\": \"111\",\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"storeId\": \"c8deb036-9e80-f19f-8981-2e440f64a445\",\r\n  \"storeName\": \"LQ SCO\"\r\n}";
            String event5 = "{\r\n  \"organisationId\": \"066e0734-4ea2-480a-ac82-a5adb9e160fe\",\r\n  \"dateRegistered\": \"2021-05-26T09:27:49.7744089+00:00\",\r\n  \"organisationName\": \"Bath Uni\"\r\n}";

            events.Add(new TopLevel.Event(event1,"organisationCreatedEvent"));
            events.Add(new TopLevel.Event(event2, "storeAddedEvent"));
            events.Add(new TopLevel.Event(event3, "storeAddedEvent"));
            events.Add(new TopLevel.Event(event4, "storeAddedEvent"));
            events.Add(new TopLevel.Event(event5, "organisationCreatedEvent"));

            //FSharpList<String> niceSharpList = ListModule.OfSeq(events);

            Console.WriteLine($"Passing {events.Count} events handler");

            //FSharpList<String> numbers = FSharpList<string>.Cons(
            //                                                     "1",
            //                                                     FSharpList<string>.Cons("2",
            //                                                                             FSharpList<string>.Cons(
            //                                                                                                     "3",
            //                                                                                                     FSharpList<string>.Empty)));

            TopLevel.EventHandler.handleEvents(events);

            //Console.WriteLine($"{result} events processed");

            Console.WriteLine($"Finished");

            Console.ReadKey();
        }
    }
}
