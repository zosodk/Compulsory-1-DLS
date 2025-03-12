using RabbitMQ.Client;
using WorkerShares.Interfaces;

namespace CleanerService;

public class ConnectionWrapper : IConnectionWrapper
{
    private readonly IConnection _connection;

    public ConnectionWrapper(IConnection connection)
    {
        _connection = connection;
    }

    public IModel CreateModel()
    {
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
