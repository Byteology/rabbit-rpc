using Example.Contract;

namespace Example.Client;
public class SimpleExample
{
	private readonly ILibraryService _libraryService;
	public SimpleExample(ILibraryService libraryService)
	{
		_libraryService = libraryService;
	}

	public void Start()
	{
		IEnumerable<Book> tolkienBooks = _libraryService.GetBooksByAuthor("J.R.R.Tolkien");
		foreach (Book book in tolkienBooks)
			Console.WriteLine(book.Name);
	}
}
