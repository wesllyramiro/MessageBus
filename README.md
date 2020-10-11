# Message

lib para abstração da implementação do rabbitmq em .Net.

### Como Usar

#### Starup
```c#
using MessageBus;
...
public void ConfigureServices(IServiceCollection services)
{
  services.AddMessageBus("nome_aplicacao", RABBIT_HOST_NAME, int.Parse(RABBIT_PORT), RABBIT_USER_NAME, RABBIT_PASSWORD);
}
```
#### Publicando
```c#
using MessageBus;
...
private readonly IMessageBus _bus;

public DevolucaoServices(IMessageBus bus)
{
  _bus = bus;
}
...
public async Task<TipoRetorno> FinalizarDevolucao(FinalizarDevolucao request)
{ 
  _bus.Publish(classeQueVaiSerPublicada, routingKey: "nome_da_fila");
}
```
#### Consumindo
```c#
using MessageBus;
...
private readonly IMessageBus _bus;

public DevolucaoServices(IMessageBus bus)
{
  _bus = bus;
}
...
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
  stoppingToken.ThrowIfCancellationRequested();

  _bus.Subscribe<ClasseQueVaiSerConsumida>("nome_da_fila", Handler);

  await Task.CompletedTask;
}

```

### Dependências
- Microsoft.Extensions.DependencyInjection.Abstractions
- Polly
- RabbitMQ.Client
- Newtonsoft.Json


