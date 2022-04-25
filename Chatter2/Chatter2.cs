using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Chatter2
{
    class Message
    {
        public DateTime Tstamp;
        public String Sender;
        public String Msg;
        //public string MsgType; //kalau mau dienhance, semisal chat atau event (join/left/ping dll) jadi nanti bisa dicustom display outputnya
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
                        var queueName = username + "_queues";
                        bool durable = false;
                        bool exclusive = true; //supaya langsung close ketika client disconnect
                        bool autoDelete = true;

                        channel.QueueDeclare(queueName, durable, exclusive, autoDelete, null);
                        channel.QueueBind(queue: queueName, exchange:"amq.fanout", routingKey: ""); //musti bind ke exchange tipe fanout

                        //consume part
                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Message obj = JsonConvert.DeserializeObject<Message>(message);
                           // if(obj.Sender != username) {
                                Console.WriteLine(" [x] {0} {1} : {2}", obj.Tstamp, obj.Sender, obj.Msg);
                            //}
                        };
                        channel.BasicConsume(queue:queueName,
                                            autoAck: true,
                                            consumer: consumer);


                        //publish part
                        string exitString = "udehan";
                        Console.WriteLine("ketik pesan yang akan dikirim. untuk keluar ketik '" + exitString + "'");
                        while(true) {
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
                            var exchangeName = "amq.fanout"; //gunakan exchange tipe fanout yang sudah disediakan di cloudamqp 
                            var routingKey = ""; //karena fan out, tidak perlu dirouting ke spesifik queue
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
