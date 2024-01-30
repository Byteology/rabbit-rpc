using MQLib;
using RabbitMQ.Client;
using Sample.Contract;
using Sample.Server;

var factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest", Port = 5672 };
using IConnection connection = factory.CreateConnection();
using IModel channel = connection.CreateModel();


RpcServer<ISampleContract> server = new(connection, new Service());
server.Start();




Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();