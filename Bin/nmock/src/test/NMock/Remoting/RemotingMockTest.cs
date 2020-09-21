// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

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
