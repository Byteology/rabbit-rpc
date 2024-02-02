[![Nuget](https://img.shields.io/nuget/v/Byteology.RabbitRpc?style=for-the-badge)](https://www.nuget.org/packages/Byteology.RabbitRpc/)

[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Byteology_rabbit-rpc&metric=security_rating)](https://sonarcloud.io/dashboard?id=Byteology_rabbit-rpc) 
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=Byteology_rabbit-rpc&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=Byteology_rabbit-rpc)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=Byteology_rabbit-rpc&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=Byteology_rabbit-rpc)

# Byteology.RabbitRpc
This is a .NET wrapper around RabbitMQ that allows an easy way to setup RPC communication.

For more information refer to the [RabbitMQ documentation](https://www.rabbitmq.com/tutorials/tutorial-six-dotnet.html).

## Table of Contents
1. [Contract](#contract)
    1. [Contract Example](#contract-example)
1. [Establishing Connection to RabbitMQ](#establishing-connection-to-rabbitmq)
    1. [Injecting a RabbitMQ Connection](#injecting-a-rabbitmq-connection)
1. [RPC Server](#rpc-server)
    1. [Contract Implementation Example](#contract-implementation-example)
    1. [Starting an RPC Server](#starting-an-rpc-server)
        1. [Using Dependency Injection](#using-dependency-injection)
1. [RPC Client](#rpc-client)
    1. [Simple Usage](#simple-usage)
    1. [Advanced Usage](#advanced-usage)
        1. [Usage Without Dependency Injection](#usage-without-dependency-injection)
1. [Full Example](#full-example)

## Contract
The RPC server should expose some sort of contract in the form of an interface. The contract has the following limitations:
1. No two methods of the contract may have the same name. The reason for that is that the fully qualified name of the interface followed by the name of each method is used to uniquely identify a queue name via which the request communication will happen.
2. Both the parameter types and the return types of all methods should be JSON serializable. The reason for that is because both the arguments and the result of a method will be serialized to be transfered as RabbitMQ messages.

### Contract Example
``` c#
public interface ILibraryService
{
    Book? GetBook(string isbn);
    IEnumerable<Book> GetBooksByAuthor(string author);
    void AddBook(Book book);
    void DeleteBook(string isbn);
}

public record Book(string ISBN, string Name, string Author, DateTime PublicationDate);
```
## Establishing Connection to RabbitMQ
Of course both the server and the client require a connection to a working RabbitMQ instance. To create one you can follow the [RabbitMQ tutorial](https://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html). 

For your convenience we have added a simple extension method that lets you configure and inject a connection so you won't have to deal with the RabbitMQ.Client package at all.

### Injecting a RabbitMQ Connection
``` c#
using Byteology.RabbitRpc;
using Microsoft.Extensions.DependencyInjection;

IServiceCollection services;
/// ...
services.AddRabbitMQ(connectionFactory =>
{
  connectionFactory.HostName = "localhost";
  connectionFactory.Port = 5672;
  connectionFactory.UserName = "guest";
  connectionFactory.Password = "guest";
});
```

## RPC Server
First of all the server should naturally provide an implementation of the contract. This is the business logic of your server.

### Contract Implementation Example
``` c#
public class LibraryService : ILibraryService
{
  private List<Book> _books = new() {
    new Book("0345339703", "The Lord of the Rings: The Fellowship of the Ring", "J.R.R.Tolkien", new DateTime(1986, 08, 12)),
    new Book("0345339711", "The Lord of the Rings: The Two Towers", "J.R.R.Tolkien", new DateTime(1986, 08, 12)),
    new Book("0345339738", "The Lord of the Rings: The Return of the King", "J.R.R.Tolkien", new DateTime(1986, 07, 12)),
    new Book("0553293354", "Foundation", "Isaac Asimov", new DateTime(1991, 10, 01))
  };

  public void AddBook(Book book) => _books.Add(book);
  public void DeleteBook(string isbn) => _books.RemoveAll(x => x.ISBN == isbn);
  public Book? GetBook(string isbn) => _books.FirstOrDefault(x => x.ISBN == isbn);
  public IEnumerable<Book> GetBooksByAuthor(string author) => _books.Where(x => x.Author == author);
}
```
### Starting an RPC Server
You can start an RPC server by creating an instance of `RpcServer<>`. Just make sure it won't be garbage collected.
``` c#
IConnection rabbitMqConnection;
// Initialize the RabbitMQ connection
// ...

RpcServer<ILibraryService> server = new (rabbitMqConnection, new LibraryService());
server.Start();
```

#### Using Dependency Injection
Alternatively can start an RPC server during startup. To do that you should inject a RabbitMQ connection as well as a contract implementation and then call the `StartRpcServer<>()` method on the `IHost` with the type parameter of the contract.
``` c#
using Byteology.RabbitRpc;
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
```

## RPC Client
To use an RPC client you would generally want to inject it. That can be achieved by the `AddRpcClient<>` extension method.

``` c#
using Byteology.RabbitRpc;

services
  .AddRabbitMQ(/* config connection to RabbitMQ */)
  .AddRpcClient<ILibraryService>();
```

### Simple Usage
Once you have registered an RPC Client you can resolve the contract interface and use it as if you were doing local calls.
``` c#
public class SimpleExample
{
  private readonly ILibraryService _libraryService;
  public SimpleExample(ILibraryService libraryService)
  {
    _libraryService = libraryService;
  }

  public void CallServer()
  {
    IEnumerable<Book> tolkienBooks = _libraryService.GetBooksByAuthor("J.R.R.Tolkien");
    foreach (Book book in tolkienBooks)
      Console.WriteLine(book.Name);
  }
}
```

### Advanced Usage
The above example does not provide any asynchronous capabilities nor a way to cancel the remote procedure call. If you need anything simillar you should resolve an `RpcClient<>` object instead.
``` c#
public class AdvancedExample
{
  private readonly RpcClient<ILibraryService> _libraryClient;
  public AdvancedExample(RpcClient<ILibraryService> libraryClient)
  {
    _libraryClient = libraryClient;
  }

  public async void CallServer()
	{
    IEnumerable<Book> tolkienBooks = await _libraryClient.CallAsync(x => x.GetBooksByAuthor("J.R.R.Tolkien"));
    foreach (Book book in tolkienBooks)
      Console.WriteLine(book.Name);
  }
}
```
The `CallAsync` method additionally accepts an optional `CancellationToken` argument which allows you to cancel the call. Note that the call might already be queued or executed by the server in which case the token will only cancel the consumption of the response and it will clear it from the response queue.

#### Usage Without Dependency Injection
The `RpcCleint<>` class can be directly constructed by simply providing an open RabbitMQ connection
``` c#
IConnection rabbitMqConnection;
// Initialize the RabbitMQ connection
// ...

RpcServer<ILibraryService> client = new (rabbitMqConnection);
```

## Full Example
A full example of this library's usage can be found [here](https://github.com/Byteology/rabbit-rpc/tree/master/Example).
