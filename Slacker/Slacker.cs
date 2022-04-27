using System;
using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Slacker
{
    class Message
    {
        public DateTime Tstamp;
        public String Sender;
        public String Msg;
        public string MsgType; //kalau mau dienhance, semisal chat atau event (join/left/ping dll) jadi nanti bisa dicustom display outputnya
    }

    class Program
    {
        static string username;
        static List<string> groups = new List<string>();
        static void Main(string[] args)
        {
            Console.Write("Tulis Nama Anda: ");
            username = Console.ReadLine();
            Console.WriteLine(" ");
            Console.Write("Daftar Grup yang akan bergabung (pisahkan dengan spasi) : ");
            string tempGroup = Console.ReadLine();
            foreach(string _g in tempGroup.Split(' '))
            {
                groups.Add(_g.ToLower());
            }

            try
            {

                IConfiguration Config = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();

                ConnectionFactory factory = new ConnectionFactory();
                factory.UserName = Config.GetSection("credentials")["username"].ToString();
                factory.Password = Config.GetSection("credentials")["password"].ToString();
                factory.VirtualHost = Config.GetSection("vhost").Value;
                // factory.Protocol = Protocols.FromEnvironment();
                factory.HostName = Config.GetSection("hostname").Value;
                factory.Port = AmqpTcpEndpoint.UseDefaultPort;
                string exchangeName = "amq.direct"; //gunakan exchange tipe fanout yang sudah disediakan di cloudamqp
                // create a connection and open a channel, dispose them when done

                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        // ensure that the queue exists before we publish to it
                        var queueName = username + "_queues";
                        bool durable = false;
                        bool exclusive = true; //supaya langsung close ketika client disconnect
                        bool autoDelete = true;

                        channel.QueueDeclare(queueName, durable, exclusive, autoDelete, null);
                        foreach(string _g in groups)
                        {
                            channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "grp_" + _g); //musti bind ke exchange tipe direct
                            //send join event
                                var msgObj2 = new Message { Tstamp = DateTime.Now, Sender = username, Msg = username + " join " + _g + " group", MsgType = "event" };
                                var _x = JsonConvert.SerializeObject(msgObj2);
                                var _data = Encoding.UTF8.GetBytes(_x);
                                channel.BasicPublish(exchange: exchangeName, routingKey: "grp_" + _g, basicProperties: null, body: _data);
                        }
                        //routing utk DM
                        channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "dm_" + username.ToLower()); //musti bind ke exchange tipe direct
                        //routing untuk broadcast
                        channel.QueueBind(queue: queueName, exchange: "amq.fanout", routingKey: ""); //bind ke fanout exchange

                        //consume part
                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            Message obj = JsonConvert.DeserializeObject<Message>(message);
                            if (obj.Sender != username) //kalo message kiriman diri sendiri abaikan ja
                            {
                                if(obj.MsgType == "event")
                                {
                                    Console.WriteLine(" [x] {0}", obj.Msg);
                                } else
                                {
                                    if (ea.RoutingKey.StartsWith("dm"))
                                    {
                                        //private message
                                        Console.WriteLine(" [x] {0} {1} : {2}", obj.Tstamp, obj.Sender, obj.Msg);
                                    }
                                    else
                                    {
                                        //group
                                        string __g = ea.RoutingKey.Substring(4);
                                        Console.WriteLine(" [x] {0} ({1}) {2} : {3}", obj.Tstamp, __g, obj.Sender, obj.Msg);
                                    }
                                }
                            }
                        };
                        channel.BasicConsume(queue: queueName,
                                            autoAck: true,
                                            consumer: consumer);


                        //publish part
                        string exitString = "udehan";
                        Console.WriteLine("ketik pesan yang akan dikirim. untuk keluar ketik '" + exitString + "'");
                        Console.WriteLine("Format: [command] [message]");
                        Console.WriteLine("command bisa '@<username>' untuk DM ke spesifik user, '<namagrup>' untuk mengirim ke grupchat tertentu, '*' untuk broadcast ke seluruh grup");
                        Console.WriteLine("------------------------");
                        while (true)
                        {
                            // read message from input
                            var _temp_msg = Console.ReadLine();
                            if (_temp_msg.ToLower().Trim() == exitString.ToLower())
                            {
                                //left grup event here
                                foreach (var _g2 in groups)
                                {
                                    var msgObj = new Message { Tstamp = DateTime.Now, Sender = username, Msg = username + " left " + _g2 + " group", MsgType = "event" };
                                    var x = JsonConvert.SerializeObject(msgObj);
                                    var data = Encoding.UTF8.GetBytes(x);
                                    channel.BasicPublish(exchange: exchangeName, routingKey: "grp_" + _g2, basicProperties: null, body: data);
                                }
                                break;
                            }
                            var splitmsg = _temp_msg.Split(null, 2);

                            if (splitmsg[0].StartsWith("@"))
                            {
                                //dm
                                var msgObj = new Message { Tstamp = DateTime.Now, Sender = username, Msg = splitmsg[1], MsgType = "chat" };
                                var x = JsonConvert.SerializeObject(msgObj);
                                var data = Encoding.UTF8.GetBytes(x);
                                channel.BasicPublish(exchange: exchangeName, routingKey: "dm_" + splitmsg[0].Substring(1).ToLower(), basicProperties: null, body: data);

                            } 
                            else if (splitmsg[0] == "*")
                            {
                                foreach(var _g2 in groups)
                                {
                                    var msgObj = new Message { Tstamp = DateTime.Now, Sender = username, Msg = splitmsg[1], MsgType = "chat" };
                                    var x = JsonConvert.SerializeObject(msgObj);
                                    var data = Encoding.UTF8.GetBytes(x);
                                    channel.BasicPublish(exchange: exchangeName, routingKey: "grp_" + _g2, basicProperties: null, body: data);
                                }
                            } 
                            else if(splitmsg[0].ToLower() == "ping" ) //egg easter command :)
                            {
                                foreach (var _g2 in groups)
                                {
                                    var msgObj = new Message { Tstamp = DateTime.Now, Sender = username, Msg = username + "(" + _g2 + ")  PING!!!", MsgType = "event" };
                                    var x = JsonConvert.SerializeObject(msgObj);
                                    var data = Encoding.UTF8.GetBytes(x);
                                    channel.BasicPublish(exchange: exchangeName, routingKey: "grp_" + _g2, basicProperties: null, body: data);
                                }
                            } else
                            {
                                //grup
                                var msgObj = new Message { Tstamp = DateTime.Now, Sender = username, Msg = splitmsg[1], MsgType = "chat" };
                                var x = JsonConvert.SerializeObject(msgObj);
                                var data = Encoding.UTF8.GetBytes(x);
                                channel.BasicPublish(exchange: exchangeName, routingKey: "grp_" + splitmsg[0].ToLower(), basicProperties: null, body: data);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("error. Message :" + e.Message);
            }
        }
    }
}
