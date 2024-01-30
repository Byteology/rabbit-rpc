using Sample.Contract;

namespace Sample.Server;
public class Service : ISampleContract
{
	public int Sum(int a, int b) => a + b;

	public void SumWrite(int a, int b) => Console.WriteLine(a + b);

	public void Hello()
	{
		Console.WriteLine("Hello");
	}

	public SampleResponse Complex(SampleRequest a)
	{
		int result = 0;
		result += a.A + a.B;
		if (a.Recursive != null)
			foreach (SampleRequest t in a.Recursive)
				result += Complex(t).Result;

		return new SampleResponse(result);
	}
}
