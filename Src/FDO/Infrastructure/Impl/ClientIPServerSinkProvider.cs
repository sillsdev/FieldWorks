// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Runtime.Remoting.Channels;

namespace FwRemoteDatabaseConnector
{
	/// <summary>
	/// Implementation based upon code found
	/// at: http://stackoverflow.com/questions/527676/identifying-the-client-during-a-net-remoting-invocation
	/// Used by .NET remoting for db4oServerInfo, to provide access to IPAddress of clients.
	/// see remoting_tcp_server.config
	/// </summary>
	public class ClientIPServerSinkProvider :
		IServerChannelSinkProvider
	{
		/// <summary></summary>
		public ClientIPServerSinkProvider()
		{
		}

		/// <summary></summary>
		public ClientIPServerSinkProvider(
			IDictionary properties,
			ICollection providerData)
		{
		}

		/// <summary></summary>
		public IServerChannelSinkProvider Next
		{
			get;
			set;
		}

		/// <summary></summary>
		public IServerChannelSink CreateSink(IChannelReceiver channel)
		{
			IServerChannelSink nextSink = null;

			if (Next != null)
			{
				nextSink = Next.CreateSink(channel);
			}
			return new ClientIPServerSink(nextSink);
		}

		/// <summary></summary>
		public void GetChannelData(IChannelDataStore channelData)
		{
		}
	}
}