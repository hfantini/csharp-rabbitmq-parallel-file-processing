using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Reader
{
    class Reader
    {
        private static IModel channel= null;

        static void Main(string[] args)
        {
            configure();
            run();
        }

        public static void configure()
        {
            var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
            var connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.BasicQos(0, 1, false);

            channel.QueueDeclare
            (
                queue: "QUEUE_WORDS",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        public static void run()
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"[{DateTime.Now.ToString()}] READER: NOVA MENSAGEM RECEBIDA: " + message);
                process(message);
            };

            channel.BasicConsume(queue: "QUEUE_FILES_TXT",
                                 autoAck: true,
                                 consumer: consumer);

            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey();

            } while (key.Key != ConsoleKey.Escape);
        }

        public static void process(string file)
        {
            if( File.Exists(file) )
            {
                if( Path.GetExtension(file) == ".txt" )
                {
                    int line = 0;

                    IEnumerable<string> lines = File.ReadLines(file);

                    foreach(string fulline in lines)
                    {
                        string buffer = "";
                        int pos = 1;
                        line++;

                        for(int count = 0; count < fulline.Length; count++)
                        {
                            Boolean dispatch = false;
                            char value = fulline[count];

                            if(value == ' ')
                            {
                                dispatch = true;
                            }
                            else if(count == fulline.Length - 1)
                            {
                                buffer += value;
                                dispatch = true;
                            }
                            else
                            {
                                buffer += value;
                            }

                            if(dispatch)
                            {
                                if (buffer != "")
                                {
                                    send(buffer, file, line, pos);
                                    buffer = "";

                                    pos = count + 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void send(string message, string file, int line, int pos)
        {
            if (message != null && message != "")
            {
                Dictionary<string, object> headers = new Dictionary<string, object>();
                headers.Add("file", file);
                headers.Add("at", $"{line}:{pos}");

                IBasicProperties properties = channel.CreateBasicProperties();
                properties.Headers = headers;

                channel.BasicPublish
                (
                    exchange: "",
                    routingKey: "QUEUE_WORDS",
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(message)
                );
            }
        }
    }
}
