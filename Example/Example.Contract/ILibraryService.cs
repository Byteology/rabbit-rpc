namespace Example.Contract;
public interface ILibraryService
{
	Book? GetBook(string isbn);

	IEnumerable<Book> GetBooksByAuthor(string author);

	void AddBook(Book book);

	void DeleteBook(string isbn);
}
