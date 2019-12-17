using Client_v3.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static IConnection conn { get; set; }
        static List<string> users = new List<string>();
        static void Main(string[] args)
        {
            Console.WriteLine("Start server");
            conn = GetRabbitConnection();
            IModel server = GetRabbitChannel("server", "queueName", "routingKey");
            var subscription = new Subscription(server, "queueName", false);

            while (true)
            {
                BasicDeliverEventArgs basicDeliveryEventArgs = subscription.Next();
                string messageContent = Encoding.UTF8.GetString(basicDeliveryEventArgs.Body);
                Message message = JsonConvert.DeserializeObject<Message>(messageContent);
                Console.WriteLine(message.Login + ": " + message.Text);
                if (message.Text == "Authorization")
                {
                    users.Add(message.Login);
                }
                else
                {
                    SendMessageToAll(message);
                }
                //subscription.Ack(basicDeliveryEventArgs);
            }
        }
        static void SendMessageToAll(Message message)
        {
            foreach (string s in users)
            {
                IModel model = GetRabbitChannel(s, s, s);
                byte[] messageBodyBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                model.BasicPublish(s, s, null, messageBodyBytes);
            }
        }

        // Создание и конфигурирование подключения к очереди
        private static IModel GetRabbitChannel(string exchangeName, string queueName, string routingKey)
        {
            IModel model = conn.CreateModel();
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            model.QueueDeclare(queueName, false, false, false, null);
            model.QueueBind(queueName, exchangeName, routingKey, null);
            return model;
        }

        private static IConnection GetRabbitConnection()
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            IConnection conn = factory.CreateConnection();
            return conn;
        }
    }
}
