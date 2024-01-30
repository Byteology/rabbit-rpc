using Byteology.RabbitRpc;
using RabbitMQ.Client;

namespace MQLib;
/// <summary>
/// An RPC client that will send commands executing by an <see cref="RpcServer{TContract}"/> via RabbitMQ.
/// </summary>
/// <typeparam name="TContract">The contract that the server implements.</typeparam>
public class RpcClient<TContract> where TContract : class
{
	private readonly IModel _channel;
	/// <summary>
	/// used for synchronizing the request and response threads as well as cancelling response awaiting.
	/// </summary>
	private ResponseOrchestrator _responseOrchestrator;

	/// <summary>
	/// Creates a new RPC client that will send commands executing by an <see cref="RpcServer{TContract}"/>
	/// </summary>
	/// <param name="connection">The connection to the opened he connection to a RabbitMQ server that will be used for the client-server communication. </param>
	public RpcClient(IConnection connection)
	{
		// A channels is just a multiplex on the TCP socket. The only official recommendation is to have a separate channel for each thread even though they are thread safe. Because of this we are creating a separate channel for each client.  
		_channel = connection.CreateModel();
		_responseOrchestrator = new ResponseOrchestrator(_channel);
	}

	/// <summary>
	/// Starts a task that will send a command to the RPC server and await for its response.
	/// </summary>
	/// <param name="call">The command to be executed on the RPC server.</param>
	/// <param name="cancellationToken">A token used for canceling the task that waits for the response.</param>
	public Task<R> CallAsync<R>(Func<TContract, R> call, CancellationToken cancellationToken = default)
	{
		TContract proxy = CommandProxy.Create<TContract>(_channel, _responseOrchestrator, out string correlationId);
		if (cancellationToken != default)
			_responseOrchestrator.RegisterCancellationToken(cancellationToken, correlationId);

		R result = call(proxy);
		return Task.FromResult(result);
	}

	/// <summary>
	/// Starts a task that will send a command to the RPC server and await for its response.
	/// </summary>
	/// <param name="call">The command to be executed on the RPC server.</param>
	/// <param name="cancellationToken">A token used for canceling the task that waits for the response.</param>
	public Task CallAsync(Action<TContract> call, CancellationToken cancellationToken = default)
	{
		TContract proxy = CommandProxy.Create<TContract>(_channel, _responseOrchestrator, out string correlationId);
		if (cancellationToken != default)
			_responseOrchestrator.RegisterCancellationToken(cancellationToken, correlationId);

		call(proxy);
		return Task.CompletedTask;
	}
}

