using RabbitMQ.Client;

namespace WorkerShares.Interfaces;

public interface IConnectionWrapper : IDisposable
{
    IModel CreateModel();
}