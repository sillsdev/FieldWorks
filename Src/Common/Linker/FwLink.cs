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
// File: FwLinkLink.cs
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
using System.Runtime.Serialization;

namespace SIL.FieldWorks.Linking
{
	/// <summary>
	/// provides a message object specifically for asking FieldWorks applications
	/// (whether the current one or another one) to to various navigation activities.
	/// </summary>
	///
	[Serializable]
	public class FwLink
	{
		protected string m_appName;

		/// <summary>
		/// constructs a link which just send you to the named application
		/// </summary>
		/// <param name="appName"></param>
		public FwLink(string application)
		{
			m_appName =application;
		}

		public void Activate ()
		{
			if(FwLinkListener.AttemptLink(this))
				return;

			throw new ApplicationException ("Could not connect to " +m_appName);
		}

		public string ApplicationName
		{
			get
			{
				return m_appName;
			}
		}
		public override string ToString()
		{
			UriBuilder builder = new UriBuilder("tcp","localhost");
			builder.Path = "link";
			builder.Query="app="+m_appName;
			return builder.Uri.AbsoluteUri;
		}
	}
}
