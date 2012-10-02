using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;

namespace NMock.Remoting
{
	public class MockServer : IDisposable
	{
		private MarshalByRefObject mock;
		private IChannel channel;
		private ObjRef mockRef;

		public MockServer(MarshalByRefObject mock, IChannel channel, string uri)
		{
			this.mock = mock;
			this.channel = channel;
			ChannelServices.RegisterChannel(channel, true);
			mockRef = RemotingServices.Marshal(mock, uri);
		}

		public void Dispose()
		{
			try
			{
				if (mockRef != null)
				{
					RemotingServices.Disconnect(mock);
				}
			}
			catch (Exception) { throw; }
			finally { ChannelServices.UnregisterChannel(channel); }
		}
	}
}
