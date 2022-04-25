using System;
using RabbitMQ.Client;
using System.Text;

using Microsoft.Extensions.Configuration;


namespace Send
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
                using(var connection = factory.CreateConnection()) {
                    using(var channel = connection.CreateModel()) {
                    // ensure that the queue exists before we publish to it
                    string queueName = "helloqueue";
                    bool durable = false;
                    bool exclusive = false;
                    bool autoDelete = true;

                    channel.QueueDeclare(queueName, durable, exclusive, autoDelete, null);

                    string exitString = "udehan";
                    while(true) {
                        Console.WriteLine("ketik pesan yang akan dikirim. untuk keluar ketik '" + exitString + "'");
                        // read message from input
                        var message = Console.ReadLine();
                        if(message.ToLower().Trim() == exitString.ToLower()) {
                            break;
                        }
                        // the data put on the queue must be a byte array
                        var data = Encoding.UTF8.GetBytes(message);
                        // publish to the "default exchange", with the queue name as the routing key
                        var exchangeName = "";
                        var routingKey = queueName;
                        channel.BasicPublish(exchangeName, routingKey, null, data);
                    }

                    }
                }
            }catch(Exception e) {
                Console.WriteLine("error. Message :" + e.Message);
            }
        }
    }
}
