namespace NMock
{
	public interface IInvocationHandler
	{
		/// <summary>
		/// Processes a method invocation on a proxy instance and returns the result.
		/// This method will be invoked on an invocation handler when a method is invoked on a proxy instance
		/// with which the invocation handler is associated.
		/// </summary>
		object Invoke(string methodName, object[] args, string[] types);
	}
}