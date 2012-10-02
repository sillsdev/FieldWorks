using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using NUnit.Framework;

namespace NMock.Remoting
{
	[TestFixture]
	public class MockServerTest
	{
		[Ignore("This does currently not work")]
		[Test]
		public void MarshalRemotingMock()
		{
			RemotingMock mock = new RemotingMock(typeof(Foo));
			mock.Expect("Bar");

			TcpChannel channel = new TcpChannel(1234);
			using (MockServer server = new MockServer(mock.MarshalByRefInstance, channel, "mock.rem"))
			{
				Foo foo = (Foo)RemotingServices.Connect(typeof(Foo), "tcp://localhost:1234/mock.rem");
				foo.Bar();
			}

			mock.Verify();
		}

		public interface Foo
		{
			void Bar();
		}
	}
}
