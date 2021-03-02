using System;
using CQRSConsole;
using EasyNetQ; 
namespace MS2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var bus = EasyNetQ.RabbitHutch.CreateBus("host=localhost"))
            {

                bus.PubSub.Subscribe<CustomerCreatedEvent>("my_subscription_id", msg => msg.ToString());
                 //{
                 //    Console.WriteLine(msg.ToString());
                 //});


            }
        }
    }
}
