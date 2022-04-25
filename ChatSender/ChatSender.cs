using System;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;

namespace ChatSender
{
    class Message
    {
        public DateTime Tstamp;
        public String Sender;
        public String Msg;
    }

    class Program
    {
        static string username;
        static void Main(string[] args)
        {
            Console.Write("Tulis Nama Anda: ");
            username = Console.ReadLine();
            Console.WriteLine(" ");
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
                        string queueName = "forumChatQueues";
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
                            var msgObj = new Message{ Tstamp = DateTime.Now, Sender = username, Msg = message };
                            var x =JsonConvert.SerializeObject(msgObj);
                            // the data put on the queue must be a byte array
                            var data = Encoding.UTF8.GetBytes(x);
                            // publish to the "default exchange", with the queue name as the routing key
                            var exchangeName = "";
                            var routingKey = queueName;
                            channel.BasicPublish(exchangeName, routingKey, null, data);
                        }
                    }
                }
            } catch(Exception e) {
                Console.WriteLine("error. Message :" + e.Message);
            }
        }
    }
}
