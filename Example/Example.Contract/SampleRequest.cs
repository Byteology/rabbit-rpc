namespace Sample.Contract;
public record SampleRequest(int A, int B)
{
	public SampleRequest[]? Recursive { get; set; }
}
