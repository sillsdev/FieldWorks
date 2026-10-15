// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Captures the link a jump posts. It sorts ahead of the window's own link listener,
	/// which shares the High priority and was registered first, so it sees the link first.
	/// </summary>
	internal sealed class FollowLinkSpy : IxCoreColleague
	{
		public FwLinkArgs Link { get; private set; }
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
		public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
		public bool ShouldNotCall => false;
		public int Priority => (int)ColleaguePriority.High - 1;
		public bool OnFollowLink(object args)
		{
			Link = args as FwLinkArgs;
			return true;
		}

		/// <summary>
		/// Runs <paramref name="action"/> and returns the link it posts; null when it posts none.
		/// </summary>
		/// <param name="deliver">Delivers the posted messages, so the spy sees the post.</param>
		public static FwLinkArgs Capture(Mediator mediator, Action action, Action deliver)
		{
			var spy = new FollowLinkSpy();
			mediator.AddColleague(spy);
			try
			{
				action();
				deliver();
				return spy.Link;
			}
			finally
			{
				mediator.RemoveColleague(spy);
			}
		}
	}
}
