// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;

namespace NMock
{
	/// <summary>
	/// Method without expecations that always returns the same value
	/// </summary>
	public class CallMethodWithoutExpectation : IMethod
	{
		private string name;
		private MockCall call;

		public CallMethodWithoutExpectation(MethodSignature signature)
		{
			this.name = signature.methodName;
		}

		public string Name
		{
			get { return name; }
		}

		public virtual void SetExpectation(MockCall call)
		{
			this.call = call;
		}

		public virtual object Call(params object[] parameters)
		{
			return call.Call(name, parameters);
		}

		public virtual void Verify()
		{
			// noop
		}
	}
}
