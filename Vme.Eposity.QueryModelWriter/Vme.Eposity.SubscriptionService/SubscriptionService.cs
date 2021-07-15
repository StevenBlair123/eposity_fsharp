namespace Vme.Eposity.SubscriptionService{
    using System.Threading;
    using System.Threading.Tasks;

    public class SubscriptionService{
        public Task Start(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        public Task Stop(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        protected Task ExecuteAsync(CancellationToken stoppingToken){
            return Task.CompletedTask;
        }
    }
}