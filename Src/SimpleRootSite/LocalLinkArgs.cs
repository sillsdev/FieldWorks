// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class is used for the communication between VwBaseVc.DoHotLinkAction and LinkListener.OnHandleLocalHotlink.
	/// </summary>
	public class LocalLinkArgs
	{
		/// <summary>
		/// LinkListener sets this true if it determines that the link is local (and has handled it).
		/// </summary>
		public bool LinkHandledLocally { get; set; }

		/// <summary>
		/// This is used by simple root site to pass in the link.
		/// </summary>
		public string Link { get; set; }
	}
}