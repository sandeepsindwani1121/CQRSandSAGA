using System;
using EasyNetQ;
namespace EasyNet
{
    class Program
    {
        static void Main(string[] args)
        {
            IBus bus = RabbitHutch.CreateBus("host=localhost");
            bus.PubSub.Publish("Hello sandeep");

            bus.PubSub.SubscribeAsync<String>(
            "my_subscription_id", msg => Console.WriteLine(msg));
        }


    }
}
