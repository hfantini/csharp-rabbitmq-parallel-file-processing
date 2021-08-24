using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Reader
{
    class Detector
    {
        private static IModel channel = null;

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
                queue: "QUEUE_FILES_TXT",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            channel.QueueDeclare
            (
                queue: "QUEUE_FILES_PDF",
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

                Console.WriteLine($"[{DateTime.Now.ToString()}] DETECTOR: NOVA MENSAGEM RECEBIDA: " + message);
                process(message);

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            channel.BasicConsume(queue: "QUEUE_FILES",
                                 autoAck: false,
                                 consumer: consumer);

            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey();

            } while (key.Key != ConsoleKey.Escape);
        }

        public static void process(string file)
        {
            if (File.Exists(file))
            {
                if (Path.GetExtension(file) == ".txt")
                {
                    send(file, "txt");
                }
                else
                {
                    // VERIFICA SE É PDF
                    byte[] expected = new byte[4] { 0x25, 0x50, 0x44, 0x46 };
                    byte[] buffer = new byte[4];

                    FileStream fileStream = null;

                    try
                    {
                        fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                        var bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                        if(bytesRead != buffer.Length)
                        {
                            throw new Exception();
                        }

                        if ( StructuralComparisons.StructuralEqualityComparer.Equals(buffer, expected) )
                        {
                            send(file, "pdf");
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("DETECTOR: NÃO FOI POSSÍVEL LER O ARQUIVO.");
                    }
                    finally
                    {
                        if (fileStream != null)
                        {
                            fileStream.Close();
                        }
                    }
                }
            }
        }

        public static void send(string file, string format)
        {
            if (file != null && file != "")
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] DETECTOR: ARQUIVO DETECTADO -> " + format);

                if (format == "txt")
                {
                    channel.BasicPublish
                    (
                        exchange: "",
                        routingKey: "QUEUE_FILES_TXT",
                        basicProperties: null,
                        body: Encoding.UTF8.GetBytes(file)
                    );
                }
                else if(format == "pdf")
                {
                    channel.BasicPublish
                    (
                        exchange: "",
                        routingKey: "QUEUE_FILES_PDF",
                        basicProperties: null,
                        body: Encoding.UTF8.GetBytes(file)
                    );
                }
            }
        }
    }
}
