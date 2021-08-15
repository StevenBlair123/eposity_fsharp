using System;

namespace ConsoleApp1 {
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.Client;
    using Eventuous;
    using Eventuous.Projections.MongoDB;
    using Eventuous.Subscriptions;
    using Eventuous.Subscriptions.EventStoreDB;
    using Microsoft.Extensions.Logging;

    public class EventSerializer : IEventSerializer{

        public EventSerializer(){
            ContentType = "application/json";
        }

        public Object? Deserialize(ReadOnlySpan<Byte> data, String eventType){
            return new TopLevel.Event (Encoding.Default.GetString(data), eventType);
        }

        public Byte[] Serialize(Object evt){
            throw new NotImplementedException();
        }

        public String ContentType{ get; }
    }

    public class TestEventHandler : IEventHandler {

        public TestEventHandler(String subscriptionId){
            SubscriptionId = subscriptionId;
        }

        public async Task ProcessEvents(CancellationToken cancellationToken) {
            Program.Trace($"Event Processing Starting on {events.Count} events");

            Random random = new(DateTime.Now.Millisecond);
            var i = random.Next(1, 5);

            await Task.Delay(TimeSpan.FromSeconds(i), cancellationToken);

            Program.Trace($"Event Processing Finished");

            EventsProcessed += this.events.Count;

            //TopLevel.EventHandler.handleEvents(events);
            this.events = new List<TopLevel.Event>(); //empty our queue
        }

        public Int32 EventsProcessed = 0;

        private String currentEvent;

        private String firstEvent;

        public async Task HandleEvent(Object evt, Int64? position, CancellationToken cancellationToken){
            Program.Trace($"Event appeared {((TopLevel.Event)evt).EventType}");
            Program.Trace($"position {position}");

            //if (String.IsNullOrEmpty(firstEvent)){
            //    firstEvent = ((TopLevel.Event)evt).Payload;
            //}

            //currentEvent = ((TopLevel.Event)evt).Payload;

            //Program.Trace($"firstEvent {firstEvent}");
            //Program.Trace($"currentEvent {currentEvent}");

            //TODO: Pass onto a queue until <n> met (i.e. 10)
            //We can't leave this function on <n> or the next event will arrive
            //So that is when we call TopLevel.EventHandler.handleEvents(events);
            //After completing, we signal that we are finished (ManualResetEvent)
            //Could we dispatch an event, then write the Checkpoint off the back of that?
            //events.Add((TopLevel.Event)evt);

            this.events = new List<TopLevel.Event>() { (TopLevel.Event)evt  };
            TopLevel.EventHandler.handleEvents(events);

            //TODO: either we hit the count OR a Checkpoint After ms - if we only received 4 from 5, we can't sit forever waiting
            //Do we just ditch the Count and use time?
            //Problem with controlling here is is we might not get another event, therefore our Checkpoint class surely has to manage?

            //TODO: What if we never hit 5 :|

            //if (this.events.Count == 5) {
            //    Program.Trace($"Event Processing Starting");

            //    Random random = new(DateTime.Now.Millisecond);
            //    var i = random.Next(1, 5);

            //    await Task.Delay(TimeSpan.FromSeconds(i), cancellationToken);

            //    Program.Trace($"Event Processing Finished");

            //    //TopLevel.EventHandler.handleEvents(events);
            //    this.events = new List<TopLevel.Event>(); //empty our queue
            //}

            //TopLevel.EventHandler.handleEvents(events);
            NumberOfEventsQueued++;

            if (NumberOfEventsQueued % 1000 == 0){
                Program.Trace($"Number of events collected is {NumberOfEventsQueued}");
            }
        }

        private List<TopLevel.Event> events = new();
        //IEnumerable<TopLevel.Event> events = new TopLevel.Event[] { (TopLevel.Event)evt };

        public String SubscriptionId { get; }

        public Int32 NumberOfEventsQueued = 0;
    }

    class Program {
        public static Action<String> Trace = (message) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}");

        static async Task Subscription(CancellationToken cancellationToken){

            var settings = EventStoreClientSettings
                .Create("esdb://admin:changeit@production.eposity.com:2113?tls=false");
            var client = new EventStoreClient(settings);
            StreamSubscriptionOptions streamPersistentSubscriptionOptions = new(){
                SubscriptionId = "Test1",
                ResolveLinkTos = true,
                StreamName = "$ce-OrganisationAggregate",
                ThrowOnError = true
            };

            TestEventHandler testEventHandler = new (streamPersistentSubscriptionOptions.SubscriptionId);
            IEventSerializer eventSerializer = new EventSerializer();

            ICheckpointStore checkpointStore = new EsCheckpointStore(client, "Test1",500, testEventHandler);
            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                                         builder.AddSimpleConsole(options => {
                                             options.IncludeScopes = true;
                                             options.SingleLine = true;
                                             options.TimestampFormat = "hh:mm:ss ";
                                         }));

            Eventuous.Subscriptions.EventStoreDB.StreamSubscription subscription = new(client, 
                                                                                       streamPersistentSubscriptionOptions,
                                                                                       checkpointStore, 
                                                                                       new[] { testEventHandler }, 
                                                                                       eventSerializer,
                                                                                       loggerFactory);

            //Coordinator coordinator = new (5, 1000, checkpointStore);

            await subscription.StartAsync(cancellationToken);

           // await coordinator.Monitor(CancellationToken.None);

            Console.WriteLine($"After StartAsync");

        }
        static async Task Main(string[] args){
            TopLevel.TypeMap.LoadDomainEventsTypeDynamically();

            await Subscription(CancellationToken.None);

            //Console.ReadKey();
            //return;

            //TODO: Subscription Service / Eventous
            //feed into F# entry point
            //ActionBlock in C# for grouping or in f#?

            //Bootstrap
            

            //List<TopLevel.Event> events = new ();

            //String event1 = "{\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"dateRegistered\": \"2021-05-25T13:33:21.0411205+00:00\",\r\n  \"organisationName\": \"Richard Inglis\"\r\n}";
            //String event2 = "{\r\n  \"externalStoreCode\": \"DEFAULT_STORE\",\r\n  \"externalStoreId\": \"HAR\",\r\n  \"externalStoreNumber\": \"709\",\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"storeId\": \"1902ddab-f568-b136-0822-fa4a9720fe14\",\r\n  \"storeName\": \"Harbour Parade\"\r\n}";
            //String event3 = "{\r\n  \"externalStoreCode\": \"9781C712-ADBC-CE44-9D-A3B9D90313AAE8\",\r\n  \"externalStoreId\": \"FH \",\r\n  \"externalStoreNumber\": \"3\",\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"storeId\": \"b4aab2ad-547f-6a42-2aec-cd4118828737\",\r\n  \"storeName\": \"FH SCO\"\r\n}";
            //String event4 = "{\r\n  \"externalStoreCode\": \"00CE22AA-E7BA-A747-97-652AD86AD11B49\",\r\n  \"externalStoreId\": \"LQ \",\r\n  \"externalStoreNumber\": \"111\",\r\n  \"organisationId\": \"76b2bed5-b19a-49b3-a8cd-8d5ada1f30b5\",\r\n  \"storeId\": \"c8deb036-9e80-f19f-8981-2e440f64a445\",\r\n  \"storeName\": \"LQ SCO\"\r\n}";
            //String event5 = "{\r\n  \"organisationId\": \"066e0734-4ea2-480a-ac82-a5adb9e160fe\",\r\n  \"dateRegistered\": \"2021-05-26T09:27:49.7744089+00:00\",\r\n  \"organisationName\": \"Bath Uni\"\r\n}";

            //events.Add(new TopLevel.Event(event1,"organisationCreatedEvent"));
            //events.Add(new TopLevel.Event(event2, "storeAddedEvent"));
            //events.Add(new TopLevel.Event(event3, "storeAddedEvent"));
            //events.Add(new TopLevel.Event(event4, "storeAddedEvent"));
            //events.Add(new TopLevel.Event(event5, "organisationCreatedEvent"));

            ////FSharpList<String> niceSharpList = ListModule.OfSeq(events);

            //Console.WriteLine($"Passing {events.Count} events handler");

            //FSharpList<String> numbers = FSharpList<string>.Cons(
            //                                                     "1",
            //                                                     FSharpList<string>.Cons("2",
            //                                                                             FSharpList<string>.Cons(
            //                                                                                                     "3",
            //                                                                                                     FSharpList<string>.Empty)));

            //TopLevel.EventHandler.handleEvents(events);

            //Console.WriteLine($"{result} events processed");

            //Console.WriteLine($"Finished");

            Console.ReadKey();
        }
    }

    /// <summary>
    /// TODO: COuld we just use SQL Server for Checkpoints?
    /// </summary>
    public class EsCheckpointStore : ICheckpointStore {
        const string CheckpointStreamPrefix = "checkpoint";
        readonly EventStoreClient _client;

        private readonly UInt64 CheckpointAfter; //Number of events processed before checkpoint

        private readonly IEventHandler EventHandler;

        readonly string _streamName;

        public EsCheckpointStore(EventStoreClient client, 
                                 string subscriptionName,
                                 ulong checkpointAfter, 
                                 IEventHandler eventHandler) {
            this._client = client;
            this.CheckpointAfter = checkpointAfter;
            this.EventHandler = eventHandler;
            this._streamName = EsCheckpointStore.CheckpointStreamPrefix + subscriptionName;
        }

        //public ValueTask<Checkpoint> GetLastCheckpoint(String checkpointId,
        //                                                                                    CancellationToken cancellationToken = new CancellationToken()) {
        //    //var result = this._client
        //    //                 .ReadStreamAsync(Direction.Backwards, this._streamName, StreamPosition.End, 1);

        //    //if (await result.ReadState == ReadState.StreamNotFound)
        //    //{
        //    //    return null;
        //    //}

        //    //var eventData = await result.FirstAsync();

        //    //if (eventData.Equals(default(ResolvedEvent)))
        //    //{
        //    //    await this.StoreCheckpoint(Position.Start.CommitPosition);
        //    //    return null;
        //    //}

        //    //return eventData.Deserialize<Checkpoint>()?.Position;
        //    Checkpoint checkpoint = new Checkpoint(checkpointId, 0);


        //    return new ValueTask<Checkpoint>(checkpoint);

        //    //return new ValueTask<Checkpoint>(() => checkpoint));

        //}


        private Stopwatch Stopwatch;

        private Boolean CaughtUp = false;

        public void Monitor(){

                Stopwatch = Stopwatch.StartNew();

                async void ThreadStart
                    () {
                    Program.Trace($"Starting monitoring...");

                    EsCheckpointStore es = this;

                    //TODO: This would run for as long as our subscription was running
                while (true) {
                        Boolean storeCheckpoint = false;

                        if (this.Stopwatch.ElapsedMilliseconds >= 1000 && this.EventsSinceLastCheckpoint == 0) {

                            if (this.CaughtUp == false) {
                                //Simulate catchup?
                                Program.Trace("Caught up!");
                                Program.Trace($"Number of events processed  {((TestEventHandler)this.EventHandler).EventsProcessed}");
                                Program.Trace($"Number of Checkpoints written  {this.NumberOfCheckpointsWritten}");

                                Program.Trace($"Total events seen  {this.TotalEventsProcessed}");
                                Program.Trace($"Current Checkpoint {this.CurrentCheckpoint.Position}.");

                            CaughtUp = true;
                            }
                        }

                        if (this.Stopwatch.ElapsedMilliseconds >= 1000 && this.EventsSinceLastCheckpoint > 0) {
                            Program.Trace($"Elapsed time and have at least 1 event since last checkpoint");
                            storeCheckpoint = true;
                        }

                        if (this.EventsSinceLastCheckpoint > 0) {
                            if (this.EventsSinceLastCheckpoint % this.CheckpointAfter == 0) {
                                Program.Trace($"Number of events [{this.EventsSinceLastCheckpoint}] triggered Checkpoint save");
                                storeCheckpoint = true;
                            }
                        }

                        if (storeCheckpoint) {

                            //TODO: Call our handler?
                            await ((TestEventHandler)this.EventHandler).ProcessEvents(CancellationToken.None);

                            Program.Trace($"CHECKPOINT {this.CurrentCheckpoint.Position}.");
                            this.Stopwatch.Restart();
                            this.EventsSinceLastCheckpoint = 0;

                            NumberOfCheckpointsWritten++;
                        }

                        await Task.Delay(100, CancellationToken.None);
                    }
                }

                Thread thread = new(ThreadStart);

                thread.Start();

                MonitoringRunning = true;
        }

        public async ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, CancellationToken cancellationToken = new ()){
            //if (MonitoringRunning == false){
            //    this.Monitor();
            //}

            Program.Trace($"StoreCheckpoint position is {checkpoint.Position}");

            return checkpoint;

            EventsSinceLastCheckpoint++;
            TotalEventsProcessed++;

            CurrentCheckpoint = new(checkpoint.Id, checkpoint.Position);
            ValueTask<Checkpoint> vt = new(CurrentCheckpoint);


            //this._counters[checkpoint.Id]++;
            //if (this._counters[checkpoint.Id] < this._batchSize)
            //    return checkpoint;
            //ReplaceOneResult replaceOneResult = await this.Checkpoints.ReplaceOneAsync<Checkpoint>((Expression<Func<Checkpoint, bool>>)(x => x.Id == checkpoint.Id), checkpoint, Eventuous.Projections.MongoDB.Tools.MongoDefaults.DefaultReplaceOptions, cancellationToken).NoContext<ReplaceOneResult>();
           
            
            
            // ILogger<MongoCheckpointStore> log = this._log;
            //if ((log != null ? (log.IsEnabled(LogLevel.Debug) ? 1 : 0) : 0) != 0)
                //this._log.LogDebug("[{CheckpointId}] Checkpoint position set to {Checkpoint}", (object)checkpoint.Id, (object)checkpoint.Position);
            //return checkpoint;

            //return vt;
        }

        public Int32 NumberOfCheckpointsWritten = 0;

        public Checkpoint CurrentCheckpoint;

        private ulong EventsSinceLastCheckpoint = 0;
        private ulong TotalEventsProcessed = 0;

        //public ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint,
        //                                                                                  CancellationToken cancellationToken = new CancellationToken()) {
        //    //var @event = new Checkpoint { Position = checkpoint };

        //    //var preparedEvent =
        //    //    new EventData(
        //    //                  Uuid.NewUuid(),
        //    //                  "$checkpoint",
        //    //                  Encoding.UTF8.GetBytes(
        //    //                                         JsonConvert.SerializeObject(@event)
        //    //                                        )
        //    //                 );

        //    //return this._client.AppendToStreamAsync(
        //    //                                        this._streamName,
        //    //                                        StreamState.Any,
        //    //                                        new List<EventData> { preparedEvent }
        //    //                                       );

        //    return new ValueTask<Checkpoint>();
        //}

        private Boolean MonitoringRunning = false;

        ValueTask<Eventuous.Subscriptions.Checkpoint> ICheckpointStore.GetLastCheckpoint(String checkpointId, CancellationToken cancellationToken){
            Program.Trace($"GetLastCheckpoint called");
            CurrentCheckpoint = new(checkpointId, null);
            ValueTask<Checkpoint> vt = new(CurrentCheckpoint);

            return vt;
        }
    }
}
