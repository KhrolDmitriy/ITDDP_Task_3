using Client_v3.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client_v3
{
    public partial class MainWindow : Window
    {
        public string nickName { get; set; }
        IConnection conn{ get; set; }
        // Создание и конфигурирование подключения к очереди
        private IModel GetRabbitChannel(string exchangeName, string queueName, string routingKey)
        {
            IModel model = conn.CreateModel();
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            model.QueueDeclare(queueName, false, false, false, null);
            model.QueueBind(queueName, exchangeName, routingKey, null);
            return model;
        }

        // Соединение с броккером
        private IConnection GetRabbitConnection()
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            IConnection conn = factory.CreateConnection();
            return conn;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        // нажатие кнопки Авторизация
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Login.Text))
                return;
            nickName = Login.Text;
            ButtonLogin.IsEnabled = false;
            Login.IsEnabled = false;
            conn = GetRabbitConnection();
            SendMessage(new Message() { Login = nickName, Text = "Authorization" });
            UpdateWall();
            ButtonLogin.Visibility = Visibility.Hidden;
            Login.IsEnabled = false;
            Wall.Visibility = Visibility.Visible;
            SendMessageBt.Visibility = Visibility.Visible;
            Message.Visibility = Visibility.Visible;
        }
        async Task UpdateWall()
        {
            await Task.Run(() =>
            {
                IModel model = GetRabbitChannel(nickName, nickName, nickName);
                var subscription = new Subscription(model, nickName, false);
                while (true)
                {
                    BasicDeliverEventArgs basicDeliveryEventArgs = subscription.Next();
                    string messageContent = Encoding.UTF8.GetString(basicDeliveryEventArgs.Body);
                    Message message = JsonConvert.DeserializeObject<Message>(messageContent);
                    Wall.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        TextBlock item = new TextBlock() { Width = 570, TextWrapping = TextWrapping.Wrap, Text = $"{message.Login}: {message.Text}" };
                        Wall.Items.Add(item);
                    }));
                    //subscription.Ack(basicDeliveryEventArgs);
                }
            });
        }

        // нажатие кнопки отправить
        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(new Message() { Login = nickName, Text = Message.Text });
            Message.Text = String.Empty;
        }
        void SendMessage(Message message)
        {
            IModel model = GetRabbitChannel("server", "queueName", "routingKey");
            byte[] messageBodyBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            model.BasicPublish("server", "routingKey", null, messageBodyBytes);
        }
    }
}
