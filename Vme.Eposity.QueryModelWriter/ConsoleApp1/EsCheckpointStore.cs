namespace ConsoleApp1{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.Client;
    using Eventuous.Subscriptions;

    public class Coordinator{
        public Coordinator(){
            //TODO: Configuration

        }

        public List<TopLevel.Event> events = new();

        private Thread thread;

        public void EventAddedToQueue(TopLevel.Event @event) {
            lock (padlock){
                //Program.Trace($"Event added to queue");
                NumberOfEventsWaitingToBeProcessed++;
                events.Add(@event);
            }
        }

        public Int32 EventsProcessed;

        public Int32 EventsProcessedSinceLastCheckpoint;

        public async Task WaitOnProcessing(){
            if (events.Count >= 50){
                await Process();
            }
        }

        private Object padlock = new ();

        private async Task Process(){
            lock(padlock){
                if (this.events.Count > 0){
                    //Program.Trace($"About to process {this.events.Count} events");
                    //TopLevel.EventHandler.handleEvents(this.events);

                    EventsProcessed += this.events.Count;
                    EventsProcessedSinceLastCheckpoint += this.events.Count;

                    //TODO: Kick off the processing
                    Task.Delay(100, CancellationToken.None).Wait();
                    events = new();

                    //Program.Trace($"processing finished. Total events processed {EventsProcessed}");
                }
            }
        }

        public void StoreCheckPoint(ulong position) {
            //TODO: is this even relevant? - think so as this will guids us when tow rite the enxt checkpoint?
            this.Position = position;
        }

        public void Start(){
            ThreadStart threadStart = new ThreadStart( async () => await this.T());

            thread = new(threadStart);

            thread.Start();

            this.MonitoringRunning = true;
        }

        private Boolean AllowedToRun = false;

        public Int32 NumberOfCheckpointsWritten;

        private async Task T(){

            this.Stopwatch = Stopwatch.StartNew();

            AllowedToRun = true;

            while (AllowedToRun){
                // Console.WriteLine("Running...");
                //Boolean storeCheckpoint = false;

                if (this.Stopwatch.ElapsedMilliseconds >= 1000 && this.events.Any()){
                    await this.Process(); //empty whatever we have
                }

                //TODO: Do we need to write a checkpoint
                //EventsSinceLastCheckpoint
                //EventsProcessed
                //Date last checkpoint
                //Position of last checkpoint

                Boolean checkPointRequired=false;

                if (EventsProcessedSinceLastCheckpoint > 100){
                    //Program.Trace($"EventsProcessedSinceLastCheckpoint is {EventsProcessedSinceLastCheckpoint}");
                    checkPointRequired = true;
                }

                if (this.Stopwatch.ElapsedMilliseconds >= 1000 && EventsProcessedSinceLastCheckpoint >0) {
                    //Program.Trace($"Elapsed > 1000 and EventsProcessedSinceLastCheckpoint is {EventsProcessedSinceLastCheckpoint}");
                    checkPointRequired = true;
                    this.Stopwatch.Restart();
                }

                if (checkPointRequired){
                    if (this.Position > this.LastPosition){
                        Program.Trace($"CHECKPOINT at Position {this.Position}. last position was {LastPosition}");
                        this.LastPosition = this.Position;
                    }
                    else{
                        Program.Trace($"Positions match so no Checkpoint written. Position {this.Position} and Last Position {LastPosition}");
                    }

                    EventsProcessedSinceLastCheckpoint = 0;
                    NumberOfCheckpointsWritten++;
                }

                if (this.Stopwatch.ElapsedMilliseconds >= 1000 && this.EventsProcessedSinceLastCheckpoint == 0) {
                    if (this.CaughtUp == false) {
                        //Simulate catchup?
                        Program.Trace($"Number of events processed  {this.EventsProcessed}");
                        Program.Trace($"Number of Checkpoints written  {this.NumberOfCheckpointsWritten}");
                        Program.Trace($"Current Checkpoint {this.Position}.");

                        this.CaughtUp = true;
                    }
                }

                ////TODO:
                ////1. we need to know the number of events processed since last check

                ////1. if we havent' written a checkpoint > 1000 AND we processed > 0 events since last checkpoint
                ////Does the Eh keep track and reset once done?
                //if (eh.EventsProcessed > 0) {

                //    //TODO: this wont work. Position could be > than eventprocessed (skipped events / truncate etc) - this can cause an overflow value
                //    //We need a way of track the number of events processed

                //    //2. we have written <n> events since last checkpoint and < 1000 since last checkpoint
                //    //i.e we processed 10 events before 1 second elapsed. so might want to write a checkpoint for this
                //    if (this.Stopwatch.ElapsedMilliseconds < 1000 && ((UInt64)eh.EventsProcessed - es.CurrentCheckpoint.Position.Value) > 10) {
                //        Program.Trace("Processed more than 10 events");
                //        storeCheckpoint = true;

                //    }

                //    if (this.Stopwatch.ElapsedMilliseconds >= 1000) {
                //        if (es.CurrentCheckpoint.Position.Value < (UInt64)eh.EventsProcessed) {
                //            Program.Trace("Elapsed time and have at least 1 event since last checkpoint");
                //            storeCheckpoint = true;
                //        }


                //        Program.Trace("Restarting timer");

                //        //TODO: When 1000 elapsed, should we reset out timer? - means we wont check this cindrion again for at 1 second
                //        this.Stopwatch.Restart();
                //    }

                //    if ((UInt64)eh.EventsProcessed == es.CurrentCheckpoint.Position.Value) {
                //        if (this.CaughtUp == false) {
                //            //Simulate catchup?
                //            Program.Trace($"Number of events processed  {((TestEventHandler)this.EventHandler).EventsProcessed}");
                //            Program.Trace($"Number of Checkpoints written  {this.NumberOfCheckpointsWritten}");
                //            Program.Trace($"Current Checkpoint {this.CurrentCheckpoint.Position}.");

                //            this.CaughtUp = true;
                //        }
                //    }

                //    if (storeCheckpoint) {
                //        this.CaughtUp = false;

                //        //NOTE: I think we have to invoke this here in case our handler has tanked events
                //        //but not yet hit the threshold to process!
                //        await((TestEventHandler)this.EventHandler).ProcessEvents("Monitor", CancellationToken.None);

                //        Program.Trace($"CHECKPOINT {this.CurrentCheckpoint.Position}. Events since last checkpoint was {this.EventsSinceLastCheckpoint}");
                //        this.Stopwatch.Restart();
                //        this.EventsSinceLastCheckpoint = 0;

                //        this.NumberOfCheckpointsWritten++;
                //    }
                //} else {
                //    Program.Trace("No events processed");
                //}

                await Task.Delay(100, CancellationToken.None);
            }
        }


        //public void Monitor() {
        //    this.Stopwatch = Stopwatch.StartNew();

        //    async void ThreadStart
        //        () {
        //        Program.Trace("Starting monitoring...");

        //        EsCheckpointStore es = this;
        //        TestEventHandler eh = ((TestEventHandler)this.EventHandler);

        //        LastCheckPointStatus lastCheckPointStatus = new(DateTime.Now, 0, 0);

        //        //How can we "know" the number of events processed?

        //        //TODO: This would run for as long as our subscription was running
                
        //    }

        //    Thread thread = new(ThreadStart);

        //    thread.Start();

        //    this.MonitoringRunning = true;
        //}

        private void Coordinator_StoreCheckPoint(object sender, ulong e) {
            //Current checkpoint position (but not neccesrily persisted
            this.Position = 0;
        }



        private void Coordinator_EventAddedToQueue(object sender, EventArgs e){
            NumberOfEventsWaitingToBeProcessed++;
        }

        private Int32 NumberOfEventsWaitingToBeProcessed = 0;

        private ulong Position = 0;

        private Boolean MonitoringRunning;

        private Stopwatch Stopwatch;

        private Boolean CaughtUp;

        private UInt64 LastPosition;

        private void Coordinator_EventAppeared(object sender, EventArgs e) {
            //TODO: Unsure if this is actually needed. This could include truncated events
            
        }



        public void Stop(){
            //TODO: Stop thread
            AllowedToRun = false;


        }

    }

    /// <summary>
    /// TODO: COuld we just use SQL Server for Checkpoints?
    /// </summary>
    public class EsCheckpointStore : ICheckpointStore{
        #region Fields

        public Checkpoint CurrentCheckpoint;

        public Int32 NumberOfCheckpointsWritten;

        private readonly EventStoreClient _client;

        private readonly String _streamName;

        private Boolean CaughtUp;

        private readonly UInt64 CheckpointAfter; //Number of events processed before checkpoint

        private readonly IEventHandler EventHandler;

        private readonly Coordinator Coordinator;

        private UInt64 EventsSinceLastCheckpoint;

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

        private Boolean MonitoringRunning;

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

        record LastCheckPointStatus(DateTime datetime, UInt32 totalEventsProcessed,UInt32 checkpointsWritten);

        #endregion

        #region Constructors

        public EsCheckpointStore(EventStoreClient client,
                                 String subscriptionName,
                                 UInt64 checkpointAfter,
                                 IEventHandler eventHandler, Coordinator coordinator) {
            this._client = client;
            this.CheckpointAfter = checkpointAfter;
            this.EventHandler = eventHandler;
            this.Coordinator = coordinator;
            this._streamName = EsCheckpointStore.CheckpointStreamPrefix + subscriptionName;

            //TODO DItch eventhandler for now

            //((EventHandler)coordinator.ThresholdReached).Invoke(null,new EventArgs());
                
                
                //.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Methods

        //public void Monitor(){
        //    this.Stopwatch = Stopwatch.StartNew();

        //    async void ThreadStart
        //        (){
        //        Program.Trace("Starting monitoring...");

        //        EsCheckpointStore es = this;
        //        TestEventHandler eh = ((TestEventHandler)this.EventHandler);

        //        LastCheckPointStatus lastCheckPointStatus = new(DateTime.Now, 0,0);

        //        //How can we "know" the number of events processed?

        //        //TODO: This would run for as long as our subscription was running
        //        while (true){
        //            Boolean storeCheckpoint = false;

        //            if (this.Stopwatch.ElapsedMilliseconds >= 1000 && this.EventsSinceLastCheckpoint == 0){
        //                if (this.CaughtUp == false){
        //                    //Simulate catchup?
        //                    //Program.Trace($"Number of events processed  {((TestEventHandler)this.EventHandler).EventsProcessed}");
        //                    Program.Trace($"Number of Checkpoints written  {this.NumberOfCheckpointsWritten}");
        //                    Program.Trace($"Current Checkpoint {this.CurrentCheckpoint.Position}.");

        //                    this.CaughtUp = true;
        //                }
        //            }

        //            //TODO:
        //            //1. we need to know the number of events processed since last check

        //            //1. if we havent' written a checkpoint > 1000 AND we processed > 0 events since last checkpoint
        //            //Does the Eh keep track and reset once done?
        //            if (eh.EventsProcessed > 0){

        //                //TODO: this wont work. Position could be > than eventprocessed (skipped events / truncate etc) - this can cause an overflow value
        //                //We need a way of track the number of events processed

        //                //2. we have written <n> events since last checkpoint and < 1000 since last checkpoint
        //                //i.e we processed 10 events before 1 second elapsed. so might want to write a checkpoint for this
        //                if (this.Stopwatch.ElapsedMilliseconds < 1000 && ((UInt64)eh.EventsProcessed - es.CurrentCheckpoint.Position.Value) > 10) {
        //                    Program.Trace("Processed more than 10 events");
        //                    storeCheckpoint = true;

        //                }

        //                if (this.Stopwatch.ElapsedMilliseconds >= 1000){
        //                    if (es.CurrentCheckpoint.Position.Value < (UInt64)eh.EventsProcessed){
        //                        Program.Trace("Elapsed time and have at least 1 event since last checkpoint");
        //                        storeCheckpoint = true;
        //                    }


        //                    Program.Trace("Restarting timer");

        //                    //TODO: When 1000 elapsed, should we reset out timer? - means we wont check this cindrion again for at 1 second
        //                    this.Stopwatch.Restart();
        //                }

        //                if ((UInt64)eh.EventsProcessed == es.CurrentCheckpoint.Position.Value) {
        //                    if (this.CaughtUp == false) {
        //                        //Simulate catchup?
        //                        Program.Trace($"Number of events processed  {((TestEventHandler)this.EventHandler).EventsProcessed}");
        //                        Program.Trace($"Number of Checkpoints written  {this.NumberOfCheckpointsWritten}");
        //                        Program.Trace($"Current Checkpoint {this.CurrentCheckpoint.Position}.");

        //                        this.CaughtUp = true;
        //                    }
        //                }

        //                if (storeCheckpoint) {
        //                    this.CaughtUp = false;

        //                    //NOTE: I think we have to invoke this here in case our handler has tanked events
        //                    //but not yet hit the threshold to process!
        //                    await ((TestEventHandler)this.EventHandler).ProcessEvents("Monitor", CancellationToken.None);

        //                    Program.Trace($"CHECKPOINT {this.CurrentCheckpoint.Position}. Events since last checkpoint was {this.EventsSinceLastCheckpoint}");
        //                    this.Stopwatch.Restart();
        //                    this.EventsSinceLastCheckpoint = 0;

        //                    this.NumberOfCheckpointsWritten++;
        //                }
        //            }
        //            else{
        //                Program.Trace("No events processed");
        //            }

        //            await Task.Delay(100, CancellationToken.None);
        //        }
        //    }

        //    Thread thread = new(ThreadStart);

        //    thread.Start();

        //    this.MonitoringRunning = true;
        //}

        public ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, CancellationToken cancellationToken = new()){
            //if (this.MonitoringRunning == false){
            //    this.Monitor();
            //}

            //Program.Trace($"StoreCheckpoint position is {checkpoint.Position}");

            //return checkpoint;

            this.Coordinator.StoreCheckPoint(checkpoint.Position.GetValueOrDefault());

            
            this.EventsSinceLastCheckpoint++;

            this.CurrentCheckpoint = new(checkpoint.Id, checkpoint.Position);
            ValueTask<Checkpoint> vt = new(this.CurrentCheckpoint);

            //this._counters[checkpoint.Id]++;
            //if (this._counters[checkpoint.Id] < this._batchSize)
            //    return checkpoint;
            //ReplaceOneResult replaceOneResult = await this.Checkpoints.ReplaceOneAsync<Checkpoint>((Expression<Func<Checkpoint, bool>>)(x => x.Id == checkpoint.Id), checkpoint, Eventuous.Projections.MongoDB.Tools.MongoDefaults.DefaultReplaceOptions, cancellationToken).NoContext<ReplaceOneResult>();

            return vt;
        }

        ValueTask<Checkpoint> ICheckpointStore.GetLastCheckpoint(String checkpointId, CancellationToken cancellationToken){
            Program.Trace("GetLastCheckpoint called");
            this.CurrentCheckpoint = new(checkpointId, null);
            ValueTask<Checkpoint> vt = new(this.CurrentCheckpoint);

            return vt;
        }

        #endregion

        #region Others

        private const String CheckpointStreamPrefix = "checkpoint";

        #endregion
    }
}