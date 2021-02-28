using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace rabbitmq
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "locationSampleQueue",
                        durable: false, 
                        exclusive: false, 
                        autoDelete: false,
                        arguments: null);
                    string message = "Latitude: 12345678.99, Longitude: 34252677.77 and Time: " + DateTime.Now;
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange: "", routingKey: "locationSampleQueue",
                                             basicProperties: null,
                                             body: body);

                    //consume MSMQ
                    Int16 messageCount = Convert.ToInt16(channel.MessageCount("locationSampleQueue"));
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine(" Location received: " + message);
                    };
                }
            }
        }

        public class Location
        {
            public DateTime Date { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

    }
}
