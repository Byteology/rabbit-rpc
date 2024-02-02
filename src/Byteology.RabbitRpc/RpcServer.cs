using System.Reflection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Byteology.RabbitRpc;

/// <summary>
/// A class that represents an RPC server that implements a provided contract.
/// </summary>
/// <typeparam name="TContract">The contract of the RPC server.</typeparam>
public class RpcServer<TContract>
	where TContract : class
{
	/// <summary>
	/// This ensures that running servers won't be garbage collected
	/// </summary>
	private static List<object> _runningServers = new();

	private readonly IModel _channel;
	private readonly TContract _contractImplementation;
	private bool _isStarted;

	/// <summary>
	/// Creates an RPC server.
	/// </summary>
	/// <param name="connection">An opened connection to a RabbitMQ server that will be used for communication.</param>
	/// <param name="contractImplementation">An object that implements the contract.</param>
	public RpcServer(IConnection connection, TContract contractImplementation)
	{
		_channel = connection.CreateModel();
		_contractImplementation = contractImplementation;
	}

	/// <summary>
	/// Starts the RPC server.
	/// </summary>
	public void Start()
	{
		if (_isStarted)
			throw new InvalidOperationException("The server is already started.");

		HashSet<string> queueNames = new();
		foreach (MethodInfo method in typeof(TContract).GetMethods())
		{
			CommandInfo commandInfo = new(method);

			if (queueNames.Contains(commandInfo.RequestQueueName))
				throw new ArgumentException($"The contract {typeof(TContract).Name} has multiple methods with the same name.");
			queueNames.Add(commandInfo.RequestQueueName);

			_channel.QueueDeclare(queue: commandInfo.RequestQueueName,
				 durable: false,
				 exclusive: false,
				 autoDelete: false,
				 arguments: null);

			EventingBasicConsumer consumer = new(_channel);
			consumer.Received += consumeEvent;

			_channel.BasicConsume(queue: commandInfo.RequestQueueName,
								 autoAck: false,
								 consumer: consumer);
		}

		_isStarted = true;
		_runningServers.Add(this);
	}

	/// <summary>
	/// Consumes a message form RabbitMQ, maps it to a particular method in the implementation, executes said method and returns response to the queue specified in the envelop.
	/// </summary>
	private void consumeEvent(object? sender, BasicDeliverEventArgs args)
	{
		byte[] body = args.Body.ToArray();
		string replyTo = args.BasicProperties.ReplyTo;
		string correlationId = args.BasicProperties.CorrelationId;

		CommandInfo commandInfo = new(typeof(TContract), args.RoutingKey);

		byte[] response = invokeImplementation(commandInfo, body);

		if (!string.IsNullOrEmpty(replyTo))
		{
			IBasicProperties properties = _channel.CreateBasicProperties();
			properties.CorrelationId = correlationId;

			_channel.BasicPublish(exchange: string.Empty,
								  routingKey: replyTo,
								  basicProperties: properties,
								  body: response);
		}
		_channel.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
	}

	private byte[] invokeImplementation(CommandInfo commandInfo, byte[] args)
	{
		object?[]? arguments = commandInfo.DeserializeRequest(args);
		object response = commandInfo.Invoke(_contractImplementation, arguments)!;

		byte[] result = commandInfo.SerializeResponse(response);
		return result;
	}



}