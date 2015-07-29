// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using System.Collections;

namespace NMock
{
	/// <summary>
	/// Method with expectations
	/// </summary>
	public class Method : IMethod
	{
		private static object NO_RETURN_VALUE = new object();

		protected MethodSignature signature;
		protected int timesCalled = 0;
		protected CallSequence expectations;

		public Method(MethodSignature signature)
		{
			this.signature = signature;
			expectations = new CallSequence(signature.methodName);
		}

		public virtual string Name
		{
			get { return signature.methodName; }
		}

		public virtual bool HasNoExpectations
		{
			get { return expectations.Count == 0; }
		}

		public virtual void SetExpectation(MockCall aCall)
		{
			expectations.Add(aCall);
		}

		public virtual object Call(params object[] parameters)
		{
			MockCall mockCall = expectations[timesCalled];
			timesCalled++;
			return mockCall.Call(signature.methodName, parameters);
		}

		public virtual void Verify()
		{
			Mock.Assertion.AssertEquals(signature + " " + CallCountErrorMessage(),
					expectations.CountExpectedCalls, timesCalled);
		}

		private string CallCountErrorMessage()
		{
			return (timesCalled == 0 ? "never called" : "not called enough times");
		}

		/// <summary>
		/// Specialised collection class for Method Calls
		/// </summary>
		public class CallSequence
		{
			private string name;
			private ArrayList sequence = new ArrayList();

			public CallSequence(string aName)
			{
				name = aName;
			}
			public MockCall this[int timesCalled]
			{
				get
				{
					if (sequence.Count <= timesCalled)
					{
						throw new VerifyException(name + "() called too many times", sequence.Count, timesCalled + 1);
					}
					return (MockCall)sequence[timesCalled];
				}
			}
			public int Count
			{
				get { return sequence.Count; }
			}

			public int CountExpectedCalls
			{
				get
				{
					int count = 0;
					foreach (Object mockCall in sequence)
					{
						if (! (mockCall is MockNoCall))
						{
							count++;
						}
					}
					return count;
				}
			}

			public void Add(MockCall aCall)
			{
				sequence.Add(aCall);
			}
		}
	}

}
