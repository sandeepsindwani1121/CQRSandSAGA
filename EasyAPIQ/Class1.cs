using System;
using EasyNetQ;
namespace EasyAPIQ
{
    public class Class1
    {
        IBus bus = RabbitHutch.CreateBus("HelloBus");
        
          

    }
}
