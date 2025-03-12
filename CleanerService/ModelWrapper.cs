using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RabbitMQ.Client;
using WorkerShares.Interfaces;

namespace CleanerService;

public class ModelWrapper : IModelWrapper
{
    private readonly IModel _model;

    public ModelWrapper(IModel model)
    {
        _model = model;
    }

    public void QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
    {
        _model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
    }

    public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
    {
        _model.BasicPublish(exchange, routingKey, basicProperties, body);
    }
    public void Dispose()
    {
        _model?.Dispose();
    }
}