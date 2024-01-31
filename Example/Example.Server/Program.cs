using Byteology.RabbitRpc;
using Example.Contract;
using Example.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddRabbitMQ(connectionFactory =>
		{
			connectionFactory.HostName = "localhost";
			connectionFactory.Port = 5672;
			connectionFactory.UserName = "guest";
			connectionFactory.Password = "guest";
		});
		services.AddSingleton<ILibraryService, LibraryService>();
	})
	.Build()
	.StartRpcServer<ILibraryService>()
	.RunAsync();