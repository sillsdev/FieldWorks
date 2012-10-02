// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwLinkReceiver.cs
// Authorship History: John Hatton
// Last reviewed:
//
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// This class is used to make an application received hyperlink requests over TCP
	/// JohnT: I don't understand this fully, but FWIW, this is part of the architecture that
	/// allows FieldWorks applications to be targets of URLs. This (in its Request method)
	/// is where the request to display a Hyperlink actually arrives in the application.
	/// If it's not working on a development machine, you may need to add a registry entry
	/// by double-clicking fw\src\Common\FieldWorksLink\HookupSILFWToC-FW-Output-Debug.reg.
	/// Compare fw\Src\Common\FieldWorksLink and fw\Src\Common\Linker for other key parts
	/// of the architecture. Also FwXApp.HandleIncomingLink() (makes sure there is a window
	/// open on the relevant database, and forwards the link to one of them by sending its
	/// mediator the "FollowLink" message). However, this is NOT handled by the window,
	/// but by the LinkListener, which first sends through the mediator the SetToolFromName
	/// message to select the right tool, then if the link has a GUID, sends JumpToRecord.
	/// It may also set some property table entries. (Since the JumpToRecord is broadcast,
	/// and therefore goes into the message queue, the properties actually get set first.)
	/// </summary>
	public class FwLinkReceiver : MarshalByRefObject
	{
		protected static int s_port;
		protected static string s_appName;

		public delegate void LinkEventHandler(FwLink link);

		/// <summary>Clients will subscribe to this, via StartListening(), to be informed when there is a link request</summary>
		protected static event LinkEventHandler OnLinkRequest;

		/// <summary>
		/// Clients should instead use the factory method. This is needed by the remoting system.
		/// </summary>
		public FwLinkReceiver()
		{
		}

		public static void StopReceiving()
		{
			//todo: how do we unregister? RemotingConfiguration does not have a command to do this.
			//RemotingConfiguration.
			//ChannelServices.UnregisterChannel(channel);
		}

		/// <summary>
		/// a factory method which creates the listener and makes it start listening.
		/// </summary>
		/// <param name="appName"></param>
		/// <returns></returns>

		public static FwLinkReceiver StartReceiving(string appName, LinkEventHandler handler)
		{
			s_appName = appName;

			s_port =6000; //broker.RegisterAndGetPort(appName);

			FwLinkReceiver.OnLinkRequest += handler;

			try
			{
				//.Net 2.0 introduced a security boolean here.
				//If set to true here, then when we try to do the
				//ChannelServices.Activate, it hangs until the program quits
				//then launches the app again.
				ChannelServices.RegisterChannel(new TcpChannel(s_port), false);
			}
			catch
			{
				throw new ApplicationException(Strings.ListenerPortTaken);
			}

			//nb: to future maintainers, be careful about deciding to release this port during some kind of destructor
			//nb: "there is a time delay for the system to release the port. It
			//	is about 2 minutes. After that the port is free and you can reuse it."

			RemotingConfiguration.ApplicationName = appName;

			RemotingConfiguration.RegisterWellKnownServiceType( typeof(FwLinkReceiver),
				"receiver",
				WellKnownObjectMode.Singleton
				);

			//although we don't really need to use it, we create this object now
			//so that we can preload with appropriate properties.
			string path = FwLink.GetPortPath(s_port, appName);
			FwLinkReceiver listener = (FwLinkReceiver)Activator.GetObject(typeof(FwLinkReceiver), path);

			if (listener == null)
				throw new ApplicationException(Strings.CannotCreateListener);

			return listener;
		}

		/// <summary>
		/// Used by remote clients to see that they actually have a real connection (a kludge)
		/// If they try to call this, but they don't have a real collection, then they get an exception.
		/// </summary>
		public void TestLink()
		{

		}

		/// <summary>
		/// called by the FwLink, which may be running in another application.
		/// </summary>
		/// <param name="link"></param>
		public void Request(FwLink link)
		{
			if (OnLinkRequest != null )
			{
				//trigger the event on anyone who has subscribed to it
				OnLinkRequest(link);
			}
		}
	}
}
