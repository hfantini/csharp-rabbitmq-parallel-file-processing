using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Crawler
{
    class Program
    {
        private static Dictionary<string, string> cmdLineParameters;

        // RABBITMQ SPECIFIC
        private static IModel channel = null;
        
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new System.Exception("A chamada não contém argumentos.");
                }
                else
                {
                    // PARSE COMMAND LINE PARAMETERS

                    cmdLineParameters = new Dictionary<string, string>();

                    string key = null;
                    string value = null;

                    foreach(string param in args)
                    {
                        if (param[0] == '-')
                        {
                            key = param;
                        }
                        else
                        {
                            value = param;
                        }

                        // ARMAZENA O PAR E RESETA ESTRUTURAS

                        if (key != null && value != null)
                        {
                            cmdLineParameters.Add(key, value);

                            key = null;
                            value = null;
                        }
                    }

                    // VALIDA PARÂMETROS OBRIGATÓRIOS

                    if (!cmdLineParameters.ContainsKey("-d"))
                    {
                        throw new System.Exception("O valor informado no parâmetro -d é inválido ou inexsistente");
                    }
                }

                // == CONFIGURA SISTEMA DE FILAS

                configure();

                // == INICIA O PROCESSO DE PROCURA

                exec(cmdLineParameters["-d"]);
            }
            catch(Exception e)
            {
                Console.WriteLine("ERRO FATAL: " + e.Message);
                Console.WriteLine("--------------------------------");
                Console.WriteLine(e.StackTrace);
            }
        }

        public static void configure()
        {
            var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
            var connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare
            (
                queue: "QUEUE_FILES",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        public static void exec(string targetDirectory)
        {
            try
            {
                //PROCESS FILES

                Console.WriteLine($"[{DateTime.Now.ToString()}] CRAWLER: ADENTRANDO DIRETÓRIO: " + targetDirectory);

                String[] files = Directory.GetFiles(targetDirectory);

                Console.WriteLine($"[{DateTime.Now.ToString()}] CRAWLER: ENCONTRADO {files.Length} ARQUIVO(S) ");

                foreach (String file in files)
                {
                    reportFile(file);
                }

                //PROCESS DIRECTORIES

                Console.WriteLine($"[{DateTime.Now.ToString()}] CRAWLER: ANALISANDO SUBDIRETÓRIOS");
                String[] directories = Directory.GetDirectories(targetDirectory);

                Console.WriteLine($"[{DateTime.Now.ToString()}] CRAWLER: ENCONTRADO {directories.Length} SUBDIRETÓRIO(S)");

                foreach (String directory in directories)
                {
                    exec(directory);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"[{DateTime.Now.ToString()}] CRAWLER: ACESSO NEGADO");
            }
        }

        public static void reportFile(String file)
        {
            Console.WriteLine($"[{DateTime.Now.ToString()}] CRAWLER: PROCESSANDO ARQUIVO: " + file);

            channel.BasicPublish
            (
                exchange: "",
                routingKey: "QUEUE_FILES",
                basicProperties: null,
                body: Encoding.UTF8.GetBytes(file)
            );

            Console.WriteLine($"[{DateTime.Now.ToString()}] CRAWLER: ARQUIVO ENVIADO!");
        }
    }
}
