namespace SharedLibrary;

// public static class RabbitMQConfig
// {
//     public static IConnection GetRabbitMQConnection()
//     {
//         Env.Load();
//         var factory = new ConnectionFactory()
//         {
//             HostName = Env.GetString("RABBITMQ_HOST", "localhost"),
//             UserName = Env.GetString("RABBITMQ_USER", "guest"),
//             Password = Env.GetString("RABBITMQ_PASSWORD", "guest"),
//             Port = int.Parse(Env.GetString("RABBITMQ_PORT", "5672"))
//         };
//
//         return factory.CreateConnection();
//     }
// }