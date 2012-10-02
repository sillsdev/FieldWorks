using System;

namespace NMock
{
	/// <summary>
	/// Name and argument details of an attempted call of a method
	/// </summary>
	public class Invocation
	{
		public readonly string methodName;
		public readonly object[] arguments;

		public Invocation(string methodName, object[] arguments)
		{
			this.methodName = methodName;
			this.arguments = arguments;
		}
	}
}
