using RabbitMQ.Client;

namespace WorkerShares.Interfaces;

public interface IModelWrapper : IDisposable
{
    void QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments);
    void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body);
}