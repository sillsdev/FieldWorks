using System;

namespace NMock
{
	//TODO: should be renamed to CallMethodAtLeastOnce
	public class SingleMethod : IMethod
	{
		private string name;
		private MockCall expectation;
		private int timesCalled = 0;

		public SingleMethod(string name)
		{
			this.name = name;
		}

		public string Name
		{
			get { return name; }
		}

		public virtual void SetExpectation(MockCall expectation)
		{
			this.expectation = expectation;
		}

		public virtual object Call(params object[] parameters)
		{
			object obj = expectation.Call(name, parameters);
			timesCalled++;
			return obj;
		}

		public virtual void Verify()
		{
			Mock.Assertion.Assert(name + "() never called.", timesCalled > 1);
		}
	}
}
