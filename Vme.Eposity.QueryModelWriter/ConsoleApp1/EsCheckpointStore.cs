namespace ConsoleApp1{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.Client;
    using Eventuous.Subscriptions;

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

        #endregion

        #region Constructors

        public EsCheckpointStore(EventStoreClient client,
                                 String subscriptionName,
                                 UInt64 checkpointAfter,
                                 IEventHandler eventHandler){
            this._client = client;
            this.CheckpointAfter = checkpointAfter;
            this.EventHandler = eventHandler;
            this._streamName = EsCheckpointStore.CheckpointStreamPrefix + subscriptionName;
        }

        #endregion

        #region Methods

        public void Monitor(){
            this.Stopwatch = Stopwatch.StartNew();

            async void ThreadStart
                (){
                Program.Trace("Starting monitoring...");

                EsCheckpointStore es = this;

                //TODO: This would run for as long as our subscription was running
                while (true){
                    Boolean storeCheckpoint = false;

                    if (this.Stopwatch.ElapsedMilliseconds >= 1000 && this.EventsSinceLastCheckpoint == 0){
                        if (this.CaughtUp == false){
                            //Simulate catchup?
                            Program.Trace($"Number of events processed  {((TestEventHandler)this.EventHandler).EventsProcessed}");
                            Program.Trace($"Number of Checkpoints written  {this.NumberOfCheckpointsWritten}");
                            Program.Trace($"Current Checkpoint {this.CurrentCheckpoint.Position}.");

                            this.CaughtUp = true;
                        }
                    }

                    if (this.Stopwatch.ElapsedMilliseconds >= 1000 && this.EventsSinceLastCheckpoint > 0){
                        Program.Trace("Elapsed time and have at least 1 event since last checkpoint");
                        storeCheckpoint = true;
                    }

                    if (this.EventsSinceLastCheckpoint > 0){
                        if (this.EventsSinceLastCheckpoint % this.CheckpointAfter == 0){
                            Program.Trace($"Number of events [{this.EventsSinceLastCheckpoint}] triggered Checkpoint save");
                            storeCheckpoint = true;
                        }
                    }

                    if (storeCheckpoint){
                        this.CaughtUp = false;

                        //NOTE: I think we have to invoke this here in case our handler has tanked events
                        //but not yet hit the threshold to process!
                        await ((TestEventHandler)this.EventHandler).ProcessEvents("Monitor",CancellationToken.None);

                        Program.Trace($"CHECKPOINT {this.CurrentCheckpoint.Position}. Events since last checkpoint was {this.EventsSinceLastCheckpoint}");
                        this.Stopwatch.Restart();
                        this.EventsSinceLastCheckpoint = 0;

                        this.NumberOfCheckpointsWritten++;
                    }

                    await Task.Delay(100, CancellationToken.None);
                }
            }

            Thread thread = new(ThreadStart);

            thread.Start();

            this.MonitoringRunning = true;
        }

        public ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, CancellationToken cancellationToken = new()){
            if (this.MonitoringRunning == false){
                this.Monitor();
            }

            //Program.Trace($"StoreCheckpoint position is {checkpoint.Position}");

            //return checkpoint;

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