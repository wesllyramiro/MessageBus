using System;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMessageBus(this IServiceCollection services, string appId, string host, int port, string user, string pass)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(host) ||
                string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) ||
                port <= 0) throw new ArgumentNullException();

            services.AddSingleton<IMessage>(new Message(appId, host, port, user, pass));

            return services;
        }
    }
}
