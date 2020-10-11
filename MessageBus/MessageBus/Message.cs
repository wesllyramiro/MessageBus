using MessageBus.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MessageBus
{
    public class Message : IMessage
    {
        private IConnection _connection;
        private IModel _channel;

        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _appId;

        public Message(string appId, string host, int port, string user, string pass)
        {
            _appId = appId;
            _host = host;
            _port = port;
            _user = user;
            _pass = pass;
        }
        public bool IsConnected => _connection?.IsOpen ?? false;
        public void Publish<T>(T payload, string exchange = "", string routingKey = "")
        {
            TryConnect();

            var props = _channel.CreateBasicProperties();
            props.AppId = _appId;
            props.UserId = _user;
            props.MessageId = Guid.NewGuid().ToString("N");
            props.Persistent = true;

            var body = Serialize<T>(payload);

            _channel.BasicPublish(exchange, routingKey, props, body);
        }
        public void Subscribe<T>(string queueName, Func<T, Task> onMessage) where T : class
        {
            PreparingConsumer(queueName, async (_, ea) =>
            {
                await onMessage(Convert<T>(ea));

                _channel.BasicAck(ea.DeliveryTag, false);
            });
        }
        public void Subscribe<T>(string queueName, Func<T, IChannel, DeliverEventArgs, Task> onMessage) where T : class
        {
            PreparingConsumer(queueName, async (_, ea) =>
                await onMessage(Convert<T>(ea), (IChannel)_channel, (DeliverEventArgs)ea));
        }

        private void PreparingConsumer(string queueName, AsyncEventHandler<BasicDeliverEventArgs> handler)
        {
            TryConnect();

            _channel.QueueDeclarePassive(queueName);
            _channel.BasicQos(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += handler;

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        private T Convert<T>(BasicDeliverEventArgs args)
        {
            try
            {
                var msg = Encoding.UTF8.GetString(args.Body.ToArray());

                var serializerSetting = new JsonSerializerSettings()
                {
                    ContractResolver = new LowerCaseContractResolver()
                };

                return JsonConvert.DeserializeObject<T>(msg, serializerSetting);
            }
            catch (JsonException)
            {
                _channel.BasicNack(args.DeliveryTag, false, false);
                throw;
            }
        }
        private byte[] Serialize<T>(T payload)
        {
            try
            {
                var serializerSetting = new JsonSerializerSettings()
                {
                    ContractResolver = new LowerCaseContractResolver()
                };

                var msg = JsonConvert.SerializeObject(payload, serializerSetting);

                return Encoding.UTF8.GetBytes(msg); ;
            }
            catch (JsonException)
            {
                throw;
            }
        }

        private void TryConnect()
        {
            if (IsConnected) return;

            var policy = Policy.Handle<AlreadyClosedException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            policy.Execute(() =>
            {
                var connectionFactory = new ConnectionFactory
                {
                    HostName = _host,
                    Port = _port,
                    UserName = _user,
                    Password = _pass,
                    ClientProvidedName = _appId,
                    DispatchConsumersAsync = true
                };
                _connection = connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();
            });
        }

        public void Close()
        {
            _connection.Close();
        }
        public class LowerCaseContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return base.ResolvePropertyName(propertyName.ToLower());
            }
        }
    }
}