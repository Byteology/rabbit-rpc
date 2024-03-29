using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Byteology.RabbitRpc;

/// <summary>
/// Contains extension methods for injecting RPC clients. 
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Injects a RabbitMQ connection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configureConnection">A callback for building the RabbitMQ connection</param>
	public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<ConnectionFactory> configureConnection)
	{
		services.AddSingleton<IConnection>(s =>
		{
			ConnectionFactory factory = new();
			configureConnection(factory);
			IConnection connection = factory.CreateConnection();
			return connection;
		});

		return services;
	}

	/// <summary>
	/// Injects a scoped RPC Client.
	/// </summary>
	/// <typeparam name="TContract">The contract provided by the server.</typeparam>
	/// <param name="services">The service collection.</param>
	public static IServiceCollection AddRpcClient<TContract>(this IServiceCollection services)
		where TContract : class
	{
		services.AddScoped<TContract>(s =>
		{
			IConnection connection = s.GetRequiredService<IConnection>();
			IModel channel = connection.CreateModel();
			ResponseOrchestrator responseOrchestrator = new(channel);
			TContract proxy = CommandProxy.Create<TContract>(channel, responseOrchestrator, out string _);
			return proxy;
		});
		services.AddScoped<RpcClient<TContract>>();

		return services;
	}
}
