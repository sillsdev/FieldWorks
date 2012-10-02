using System;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace FwRemoteDatabaseConnector
{
	/// <summary>
	/// Used to store the Ipaddress used instread of CallContext as CallContext data has
	/// different lifetime on mono compared to .NET.
	/// </summary>
	internal static class CallContextExtensions
	{
		[ThreadStatic] static IPAddress _ipaddress;

		internal static IPAddress GetIpAddress()
		{
			return _ipaddress;
		}

		internal static void SetIpAddress(IPAddress ipaddress)
		{
			_ipaddress = ipaddress;
		}
	}

	/// <summary>
	/// Implementation based upon code found
	/// at: http://stackoverflow.com/questions/527676/identifying-the-client-during-a-net-remoting-invocation
	/// Used by .NET remoting for db4oServerInfo, to provide access to IPAddress of clients.
	/// constructed in ClientIPServerSinkProvider.
	/// </summary>
	public class ClientIPServerSink :
		BaseChannelObjectWithProperties,
		IServerChannelSink
	{
		/// <summary></summary>
		public ClientIPServerSink(IServerChannelSink next)
		{
			NextChannelSink = next;
		}

		/// <summary></summary>
		public IServerChannelSink NextChannelSink
		{
			get; set;
		}

		/// <summary></summary>
		public void AsyncProcessResponse(
			IServerResponseChannelSinkStack sinkStack,
			Object state,
			IMessage message,
			ITransportHeaders headers,
			Stream stream)
		{
			SetClientIpAddressDataHelper(headers);
			sinkStack.AsyncProcessResponse(message, headers, stream);
		}

		private static void SetClientIpAddressDataHelper(ITransportHeaders headers)
		{
			var ip = headers[CommonTransportKeys.IPAddress] as IPAddress;
			CallContextExtensions.SetIpAddress(ip);
		}

		/// <summary></summary>
		public Stream GetResponseStream(
			IServerResponseChannelSinkStack sinkStack,
			Object state,
			IMessage message,
			ITransportHeaders headers)
		{
			return null;
		}

		/// <summary></summary>
		public ServerProcessing ProcessMessage(
			IServerChannelSinkStack sinkStack,
			IMessage requestMsg,
			ITransportHeaders requestHeaders,
			Stream requestStream,
			out IMessage responseMsg,
			out ITransportHeaders responseHeaders,
			out Stream responseStream)
		{
			if (NextChannelSink != null)
			{
				SetClientIpAddressDataHelper(requestHeaders);
				ServerProcessing spres = NextChannelSink.ProcessMessage(
					sinkStack,
					requestMsg,
					requestHeaders,
					requestStream,
					out responseMsg,
					out responseHeaders,
					out responseStream);
				return spres;
			}

			responseMsg = null;
			responseHeaders = null;
			responseStream = null;
			return new ServerProcessing();
		}
	}
}