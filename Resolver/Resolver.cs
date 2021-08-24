using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;

namespace Resolver
{
    class Resolver
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

            if ( File.Exists("result.txt") )
            {
                File.Delete("result.txt");
            }
        }

        public static void run()
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var header = ea.BasicProperties.Headers;
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"[{DateTime.Now.ToString()}] RESOLVER: NOVA PALAVRA RECEBIDA -> " + message);

                if ( header.ContainsKey("file") )
                {
                    process( Encoding.UTF8.GetString( ( (byte[]) header["file"]) ), Encoding.UTF8.GetString(((byte[])header["at"])), message);
                }
            };

            channel.BasicConsume(queue: "QUEUE_WORDS",
                                 autoAck: true,
                                 consumer: consumer);

            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey();

            } while (key.Key != ConsoleKey.Escape);
        }

        public static void process(string file, string at, string word)
        {
            if(word.ToUpper() == "THE")
            {
                string output = @$"[{DateTime.Now.ToString()}] ENCONTREI 'THE' EM -> {file}:{at}";
                File.AppendAllText("result.txt", output);
                File.AppendAllText("result.txt", Environment.NewLine + Environment.NewLine);
            }
        }
    }
}
