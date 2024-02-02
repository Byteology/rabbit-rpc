using Example.Contract;

namespace Example.Server;
public class LibraryService : ILibraryService
{
	private readonly List<Book> _books = new() {
		new Book("0345339703", "The Lord of the Rings: The Fellowship of the Ring", "J.R.R.Tolkien", new DateOnly(1986, 08, 12)),
		new Book("0345339711", "The Lord of the Rings: The Two Towers", "J.R.R.Tolkien", new DateOnly(1986, 08, 12)),
		new Book("0345339738", "The Lord of the Rings: The Return of the King", "J.R.R.Tolkien", new DateOnly(1986, 07, 12)),
		new Book("0553293354", "Foundation", "Isaac Asimov", new DateOnly(1991, 10, 01))
	};

	public void AddBook(Book book) => _books.Add(book);
	public void DeleteBook(string isbn) => _books.RemoveAll(x => x.ISBN == isbn);
	public Book? GetBook(string isbn) => _books.Find(x => x.ISBN == isbn);

	public IEnumerable<Book> GetBooksByAuthor(string author) => _books.Where(x => x.Author == author);
}
