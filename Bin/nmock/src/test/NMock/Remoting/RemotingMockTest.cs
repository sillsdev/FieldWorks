using System;
using NUnit.Framework;
using NMock.Remoting;

namespace NMock.Remoting
{
	[TestFixture]
	public class RemotingMockTest : Assertion
	{
		[Test]
		public void GenerateMockFromInterface()
		{
			RemotingMock mock = new RemotingMock(typeof(Base));
			MarshalByRefObject instance = mock.MarshalByRefInstance;
			AssertNotNull(instance);
		}

		public interface Base
		{
			void Foo();
		}
	}
}
