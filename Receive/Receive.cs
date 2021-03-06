using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;

namespace Receive
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                IConfiguration Config = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();

                ConnectionFactory factory = new ConnectionFactory();
                factory.UserName = Config.GetSection("credentials")["username"].ToString();
                factory.Password = Config.GetSection("credentials")["password"].ToString();
                factory.VirtualHost = Config.GetSection("vhost").Value;
                // factory.Protocol = Protocols.FromEnvironment();
                factory.HostName = Config.GetSection("hostname").Value;
                factory.Port = AmqpTcpEndpoint.UseDefaultPort;

                // create a connection and open a channel, dispose them when done
                using (var connection = factory.CreateConnection()) {
                    using(var channel = connection.CreateModel()) {
                        // ensure that the queue exists before we publish to it
                        var queueName = "helloqueue";
                        bool durable = false;
                        bool exclusive = false;
                        bool autoDelete = true;

                        channel.QueueDeclare(queueName, durable, exclusive, autoDelete, null);

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Console.WriteLine(" [x] Received {0}", message);
                        };
                        channel.BasicConsume(queue:queueName,
                                            autoAck: true,
                                            consumer: consumer);

                        Console.WriteLine(" Press [enter] to exit.");
                        Console.ReadLine();
                    }
                }
            } catch(Exception e) {
                Console.WriteLine("error. Message :" + e.Message);
            }
        }
    }
}
