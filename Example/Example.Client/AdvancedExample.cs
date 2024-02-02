using Byteology.RabbitRpc;
using Example.Contract;

namespace Example.Client;
public class AdvancedExample
{
	private readonly RpcClient<ILibraryService> _libraryClient;
	public AdvancedExample(RpcClient<ILibraryService> libraryClient)
	{
		_libraryClient = libraryClient;
	}

	public async Task Start()
	{
		IEnumerable<Book> tolkienBooks = await _libraryClient.CallAsync(x => x.GetBooksByAuthor("J.R.R.Tolkien"));
		foreach (Book book in tolkienBooks)
			Console.WriteLine(book.Name);
	}
}
