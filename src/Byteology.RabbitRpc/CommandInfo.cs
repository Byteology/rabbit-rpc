using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Byteology.RabbitRpc;

/// <summary>
/// A class that encapsulates all required reflection for interacting with a contract provided command.
/// </summary>
internal class CommandInfo
{
	private readonly JsonSerializerOptions _serializationOptions;
	private readonly MethodInfo _methodInfo;

	/// <summary>
	/// The queue name at which the RPC server is listening for requests to this particular command.
	/// </summary>
	public string RequestQueueName { get; }

	/// <summary>
	/// The return type of the command. 
	/// </summary>
	public Type? ResultType { get; }

	/// <summary>
	/// Extracts command information from a contract method info.
	/// </summary>
	/// <param name="method">The contract method to be used.</param>
	public CommandInfo(MethodInfo method)
	{
		_serializationOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
		_methodInfo = method;

		Type contractType = method.DeclaringType!;

		RequestQueueName = $"{contractType.FullName}.{method.Name}";
		ResultType = method.ReturnType;
	}

	/// <summary>
	/// The RPC server listens on separate queues for each method. This constructor simply maps the a request queue name to a method that will actually execute the request.
	/// </summary>
	/// <param name="contractType">The contract type to be used for resolving the method.</param>
	/// <param name="queueName">The request queue name.</param> 
	public CommandInfo(Type contractType, string queueName) : this(contractType.GetMethod(queueName.Split('.')[^1])!) { }

	/// <summary>
	/// Invokes the actual implementation of the request.
	/// </summary>
	/// <param name="implementation">An object that implements the contract.</param>
	/// <param name="arguments">The arguments of the command</param>
	/// <returns>An object containing the returned value of the invoked method or null for void.</returns>
	public object? Invoke(object implementation, object?[]? arguments) => _methodInfo.Invoke(implementation, arguments);

	/// <summary>
	/// Serializes the request arguments for use in RabbitMQ.
	/// </summary>
	/// <param name="obj">An array containing the request arguments</param>
	/// <returns>The serialized request.</returns>
	public byte[] SerializeRequest(object?[]? obj)
	{
		if (obj is null || obj.Length == 0)
			return Array.Empty<byte>();

		string json = JsonSerializer.Serialize(obj, obj.GetType(), _serializationOptions);
		byte[] result = Encoding.UTF8.GetBytes(json);
		return result;
	}

	/// <summary>
	/// Serializes the response value for use in RabbitMQ.
	/// </summary>
	/// <param name="obj">The response object.</param>
	/// <returns>The serialized response.</returns>
	public byte[] SerializeResponse(object? obj)
	{
		if (obj is null)
			return Array.Empty<byte>();

		string json = JsonSerializer.Serialize(obj, obj.GetType(), _serializationOptions);
		byte[] result = Encoding.UTF8.GetBytes(json);
		return result;
	}

	/// <summary>
	/// Deserializes a response value based on the return type of the method.
	/// </summary>
	/// <param name="data">The serialized value.</param>
	/// <returns>The deserialized response.</returns>
	public object? DeserializeResponse(byte[] data)
	{
		if (data is null || data.Length == 0 || ResultType == null)
			return null;

		string json = new string(Encoding.UTF8.GetChars(data));
		object? obj = JsonSerializer.Deserialize(json, ResultType, _serializationOptions);
		return obj;
	}

	/// <summary>
	/// Deserializes the incoming request arguments. The parameter types are extracted from the the method info via reflection.
	/// </summary>
	/// <param name="data">The serialized request arguments in the form of encoded JSON serialized array.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public object?[]? DeserializeRequest(byte[] data)
	{
		ParameterInfo[] parameters = _methodInfo.GetParameters();

		if (parameters.Length == 0 && data.Length != 0)
			throw new ArgumentException("Method does not require any arguments but received a request with a body.");

		if (data.Length == 0)
			return null;

		string json = new string(Encoding.UTF8.GetChars(data));

		JsonElement[]? parsedArguments = JsonSerializer.Deserialize<JsonElement[]>(json, _serializationOptions);

		if (parsedArguments == null || (parsedArguments.Length != parameters.Length))
			throw new ArgumentException("Method received the wrong number of parameters.");

		List<object?> result = new();

		for (int i = 0; i < parameters.Length; i++)
		{
			object? deserializedArgument = JsonSerializer.Deserialize(parsedArguments[i], parameters[i].ParameterType, _serializationOptions);
			result.Add(deserializedArgument);
		}

		if (result.Count == 0)
			return null;

		return result.ToArray();
	}
}
