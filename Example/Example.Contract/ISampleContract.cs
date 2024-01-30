namespace Sample.Contract;
public interface ISampleContract
{
	int Sum(int a, int b);
	void SumWrite(int a, int b);

	void Hello();

	SampleResponse Complex(SampleRequest a);

}
