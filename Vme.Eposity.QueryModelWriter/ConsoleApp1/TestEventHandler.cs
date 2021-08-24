namespace ConsoleApp1{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Eventuous.Subscriptions;

    public class TestEventHandler : IEventHandler{
        private readonly Coordinator Coordinator;

        #region Fields

        

        

        #endregion

        #region Constructors

        public TestEventHandler(String subscriptionId, Coordinator coordinator){
            this.Coordinator = coordinator;
            this.SubscriptionId = subscriptionId;
        }

        #endregion

        #region Properties

        public String SubscriptionId{ get; }

        #endregion

        #region Methods

        public async Task ProcessEvents(String source,CancellationToken cancellationToken) {
            //NOTE This is our process method.
            //This cna either be called from HandleEvent (1 event at a time)
            //or if a checkpoint needs written

            //Program.Trace($"Before WaitOne from {source} - Total Events process {this.EventsProcessed}");

            //this.ManualResetEvent.WaitOne();

            //this.ManualResetEvent.Reset();

            //Program.Trace($"After WaitOne from {source} - number of events is {this.events.Count} ");

            //if (this.events.Any()){

            //    //TODO: Split into a category (i.e. per Organisation)
            //    //Should this happen in C# or F#
            //    //I am wary of desrialising early on, then having to do it again in the f# pipeline

            //    //TODO: Lifetime counter needed?

            //    this.EventsProcessed += this.events.Count;
            //    //TopLevel.EventHandler.handleEvents(this.events);

            //    Random random = new(DateTime.Now.Millisecond);
            //    var i = random.Next(1, 2);

            //    await Task.Delay(TimeSpan.FromSeconds(i), cancellationToken);

            //    this.events = new List<TopLevel.Event>(); //empty our queue
            //}

            //Program.Trace($"About to .Set from {source} - Total Events process {this.EventsProcessed}");
            //this.ManualResetEvent.Set();
        }

        private readonly ManualResetEvent ManualResetEvent = new (true);

        

        public async Task HandleEvent(Object evt, Int64? position, CancellationToken cancellationToken){
            //START TEST CODE

            this.Coordinator.EventAddedToQueue((TopLevel.Event)evt);

            

            //TODO: Dont check 5, and see if we have been instructed to wait
            //Could hide this inside the Coordinator so no manual reset event expose
            await this.Coordinator.WaitOnProcessing();


            //if (this.events.Count < 5) {
            //    //Program.Trace($"Bucket Processing Starting");
            //    //Bank this event for later
            //    return;
            //}

            ////TODO: Could we just Wait, rather than kick off ProcessEvents?
            //await this.ProcessEvents("HandleEvent", cancellationToken);

            ////TODO: Singl number processed?
            //this.Coordinator.EventsProcessed(5); //TODO: Where do we source this from?

            return;

            //END TEST CODE


            //Boolean bucketEnabled = false;

            //this.ManualResetEvent.WaitOne();

            //this.events.Add((TopLevel.Event)evt);

            //if (bucketEnabled){
            //    //this.events.Add((TopLevel.Event)evt);

            //    //TODO: What if we never hit 5? - this is handled by the Checkpoint
            //    if (this.events.Count < 5){
            //        //Program.Trace($"Bucket Processing Starting");
            //        //Bank this event for later
            //        return;
            //    }
            //}
            ////else{
            ////    //TODO: Call ProcessEvents?
            ////    TopLevel.EventHandler.handleEvents(new[]{(TopLevel.Event)evt});
            ////    this.EventsProcessed++;
            ////}

            ////TODO: When in bucket mode have we got the risk of calling this call, then the checkpoint call at the same time?
            ////When we hit theshold value (5) we want to immediately process rather than waiting
            ////on the Checkpoint store instructing us to process.
            ////At the moment, I think we would end up sending the same 5 events in the event handler
            ////Could the thresholad check be managed from the CheckpointStore?
            ////The problem with delegating control to checkpoint, is we have to block on the threshold value (5) or more events come through :|

            ////I suspect we need a signal (ManualResetEvent)
            ////When we call ProcessEvents, it signals it is in progress.
            ////Our CheckPointStore waits on the signal, then calls ProcessEvents
            ////and events could now be empty.

            //await this.ProcessEvents("HandleEvent", cancellationToken);
        }

        #endregion
    }
}