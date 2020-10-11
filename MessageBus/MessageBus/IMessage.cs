using System;
using System.Threading.Tasks;
using MessageBus.Integration;

namespace MessageBus
{
    public interface IMessage
    {

        bool IsConnected { get; }
        void Publish<T>(T payload, string exchange = "", string routingKey = "");
        void Subscribe<T>(string queueName, Func<T, Task> onMessage) where T : class;
        void Subscribe<T>(string queueName, Func<T, IChannel, DeliverEventArgs, Task> onMessage) where T : class;
        void Close();
    }
}
