using System;
using System.Text;
using RabbitMQ.Client;

namespace NewTask
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost", Port = 9502 };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "task_queue",
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

                    var message = GetMessage(args);
                    var body = Encoding.UTF8.GetBytes(message);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;//Mark messages as persitent to prevent losses when RabbitMQ restarts.

                    channel.BasicPublish(exchange: "", routingKey: "task_queue", basicProperties: properties, body: body);
                    Console.WriteLine(" [x] Sent {0}", message);
                }
            }
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static string GetMessage(string[] args)
        {
            return ((args.Length > 0) ? string.Join(" ", args) : "Hello World!");
        }
    }
}
