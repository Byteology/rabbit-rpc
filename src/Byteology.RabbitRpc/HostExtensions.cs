using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Byteology.RabbitRpc;

/// <summary>
/// Contains extension methods for starting RPC servers. 
/// </summary>
public static class HostExtensions
{
	/// <summary>
	/// Runs a RPC server.
	/// </summary>
	/// <typeparam name="TContract">The contract of the server. The DI service container will be used to get the implementation.</typeparam>
	/// <param name="host">The program's host.</param>
	public static IHost StartRpcServer<TContract>(this IHost host)
		where TContract : class
	{
		IConnection connection = host.Services.GetRequiredService<IConnection>();
		TContract implementation = host.Services.GetRequiredService<TContract>();
		RpcServer<TContract> server = new(connection, implementation);
		server.Start();
		return host;
	}
}
