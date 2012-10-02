namespace NMock.Constraints
{
	public interface IConstraint
	{
		bool Eval(object val);
		object ExtractActualValue(object actual);
		string Message { get; }
	}

}
