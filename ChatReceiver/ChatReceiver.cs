using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;

namespace ChatReceiver
{
    class Message
    {
        public DateTime Tstamp;
        public String Sender;
        public String Msg;
    }

    class Program
    {
        static void Main(string[] args)
        {
            try {
                ConnectionFactory factory = new ConnectionFactory();
                factory.UserName = "";
                factory.Password = "";
                factory.VirtualHost = "";
                // factory.Protocol = Protocols.FromEnvironment();
                factory.HostName = "";
                factory.Port = AmqpTcpEndpoint.UseDefaultPort;

                // create a connection and open a channel, dispose them when done
                using(var connection = factory.CreateConnection()) {
                    using(var channel = connection.CreateModel()) {
                        // ensure that the queue exists before we publish to it
                        var queueName = "forumChatQueues";
                        bool durable = false;
                        bool exclusive = false;
                        bool autoDelete = true;

                        channel.QueueDeclare(queueName, durable, exclusive, autoDelete, null);

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Message obj = JsonConvert.DeserializeObject<Message>(message);
                            Console.WriteLine(" [x] {0} {1} : {2}", obj.Tstamp, obj.Sender, obj.Msg);
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
