using System.Reflection;
using RabbitMQ.Client;

namespace Byteology.RabbitRpc;

/// <summary>
/// A class that provides an anonymous implementation to a runtime provided interface. Its goal is to implement a contract by serializing the arguments and sending them to the appropriate queue in RabbitMQ, and then wait for the response in another thread. 
/// </summary>
internal class CommandProxy : DispatchProxy
{
	private IModel _channel = null!;
	private ResponseOrchestrator _responseOrchestrator = null!;
	private string _correlationId = null!;

	/// <summary>
	/// Creates a contract implementation by providing an opened channel to a RabbitMQ server and and a response orchestrator.
	/// </summary>
	/// <typeparam name="TContract">The contract type to implement.</typeparam>
	/// <param name="channel">An opened channel to a RabbitMQ server.</param>
	/// <param name="responseOrchestrator">A response orchestrator that will sync the request and response threads as they are different by design.</param>
	/// <param name="correlationId">The correlation ID that can be used by a cancellation token in order to cancel the consumption of a response.</param>
	/// <returns></returns>
	public static TContract Create<TContract>(IModel channel, ResponseOrchestrator responseOrchestrator, out string correlationId) where TContract : class
	{
		TContract proxy = DispatchProxy.Create<TContract, CommandProxy>();
		(proxy as CommandProxy)!.initialize(channel, responseOrchestrator);
		correlationId = (proxy as CommandProxy)!._correlationId;
		return proxy;
	}

	private void initialize(IModel channel, ResponseOrchestrator responseOrchestrator)
	{
		_channel = channel;
		_responseOrchestrator = responseOrchestrator;
		_correlationId = Guid.NewGuid().ToString();
	}

	/// <summary>
	/// Implement the contract by serializing the arguments and sending them to the appropriate queue in RabbitMQ.
	/// </summary>
	protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
	{
		Task<byte[]> responseTask = _responseOrchestrator.CreateNewAwaiter(_correlationId);
		CommandInfo commandInfo = new(targetMethod!);

		try
		{
			IBasicProperties props = _channel.CreateBasicProperties();
			props.CorrelationId = _correlationId;
			props.ReplyTo = _responseOrchestrator.ReplyQueueName;

			byte[] data = commandInfo.SerializeRequest(args);

			_channel.BasicPublish(exchange: string.Empty,
								 routingKey: commandInfo.RequestQueueName,
								 basicProperties: props,
								 body: data);
		}
		catch
		{
			_responseOrchestrator.CancelTask(_correlationId);
		}

		responseTask.Wait();

		if (responseTask.IsCanceled)
			throw new TaskCanceledException(responseTask);

		byte[] resultData = responseTask.Result;
		object? result = commandInfo.DeserializeResponse(resultData);

		return result;
	}
}
