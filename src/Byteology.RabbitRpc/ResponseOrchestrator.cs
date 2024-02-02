using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Byteology.RabbitRpc;

/// <summary>
/// A class used for syncing an RPC response back to the requesting thread.
/// </summary>
internal class ResponseOrchestrator
{
	/// <summary>
	/// Used to map all unfinished requests to a correlation id. When a response arrives it will complete the matching tasks in order to sync the result with the original caller. It is thread safe as all responses arrive in separate threads.
	/// </summary>
	private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _callbackMapper;

	public string ReplyQueueName { get; }

	/// <summary>
	/// Creates a new <see cref="ResponseOrchestrator"/> that listens on an opened channel to a RabbitMQ server.
	/// </summary>
	/// <param name="channel">An opened channel to a RabbitMQ server.</param>
	public ResponseOrchestrator(IModel channel)
	{
		_callbackMapper = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();

		// The queue is transient as we are listening for what is basically a callback.
		ReplyQueueName = channel.QueueDeclare().QueueName;

		EventingBasicConsumer consumer = new(channel);
		consumer.Received += consumeCallback;

		channel.BasicConsume(consumer: consumer,
							 queue: ReplyQueueName,
							 autoAck: true);
	}

	/// <summary>
	/// Receives a response message and syncs its data to the request thread by matching it by the correlation ID of the envelop. 
	/// </summary>
	private void consumeCallback(object? sender, BasicDeliverEventArgs args)
	{
		if (!_callbackMapper.TryRemove(args.BasicProperties.CorrelationId, out var tcs))
			return;

		byte[] data = args.Body.ToArray();
		tcs.TrySetResult(data);
	}

	/// <summary>
	/// Starts a task that will return the raw response to an RPC request. The caller will wait for it and the orchestrator will complete it with the appropriate data when a response with matching correlation id arrives in another thread.
	/// </summary>
	/// <param name="correlationId">The correlation id that will be used for this request.</param>
	/// <returns>A task that will be completed by the response consumer and will contain the raw response data from the RabbitMQ message.</returns>
	public Task<byte[]> CreateNewAwaiter(string correlationId)
	{
		TaskCompletionSource<byte[]> tcs = new();
		_callbackMapper.TryAdd(correlationId, tcs);
		return tcs.Task;
	}


	/// <summary>
	/// Cancels a started task and removes the task completion source in order to avoid data piling up in the dictionary and allowing the requesting thread to stop waiting.
	/// </summary>
	/// <param name="correlationId"></param>
	public void CancelTask(string correlationId)
	{
		_callbackMapper.TryRemove(correlationId, out TaskCompletionSource<byte[]>? taskCompletionSource);
		if (taskCompletionSource != null)
			taskCompletionSource.SetCanceled();
	}

	public void RegisterCancellationToken(string correlationId, CancellationToken token)
	{
		token.Register(() => CancelTask(correlationId));
	}
}
