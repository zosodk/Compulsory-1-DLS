using DotNetEnv;
using RabbitMQ.Client;

namespace SharedLibrary;

public class RabbitMQConfig
{
    private readonly ConnectionFactory _factory;

    public RabbitMQConfig()
    {
        Env.Load();
        _factory = new ConnectionFactory()
        {
            HostName = Env.GetString("RABBITMQ_HOST", "localhost"),
            UserName = Env.GetString("RABBITMQ_USER", "guest"),
            Password = Env.GetString("RABBITMQ_PASSWORD", "guest"),
            Port = int.Parse(Env.GetString("RABBITMQ_PORT", "5672"))
        };
    }

    public IConnection GetRabbitMQConnection()
    {
        return _factory.CreateConnection();
    }
}