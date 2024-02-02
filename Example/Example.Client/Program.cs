using Byteology.RabbitRpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Example.Contract;
using Example.Client;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddRabbitMQ(connectionFactory =>
		{
			connectionFactory.HostName = "localhost";
			connectionFactory.Port = 5672;
			connectionFactory.UserName = "guest";
			connectionFactory.Password = "guest";
		});
		services.AddRpcClient<ILibraryService>();

		services.AddSingleton<SimpleExample>();
		services.AddSingleton<AdvancedExample>();
	})
	.Build();

// host.Services.GetRequiredService<SimpleExample>().Start(); 
await host.Services.GetRequiredService<AdvancedExample>().Start();