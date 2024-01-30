using MQLib;
using RabbitMQ.Client;
using Sample.Contract;

var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest", Port = 5672 };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();


RpcClient<ISampleContract> c = new RpcClient<ISampleContract>(connection);
//Console.WriteLine(await c.CallAsync(x => x.Sum(1, 2)));
//await c.CallAsync(x => x.SumWrite(1, 2));
//await c.CallAsync(x => x.Hello());
SampleRequest request = new SampleRequest(4, 3);
request.Recursive = new SampleRequest[] { new SampleRequest(1, 2), new SampleRequest(3, 4) };
request.Recursive[0].Recursive = new SampleRequest[] { new SampleRequest(3, 8) };


SampleResponse response = await c.CallAsync(x => x.Complex(request));

Console.WriteLine(response.Result);




// const string message = "Hello World!";
// var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new SampleRequest(1, 2), new JsonSerializerOptions(JsonSerializerDefaults.Web)));

// channel.BasicPublish(exchange: string.Empty,
// 					 routingKey: "Sample.Contract.ISampleContract.Sum",
// 					 basicProperties: null,
// 					 body: body);
// Console.WriteLine($" [x] Sent {message}");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();