// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

namespace SIL.SilSidePane
{
	/// <summary>
	/// Tab on a SidePane.
	/// </summary>
	public class Tab : Item
	{
		private bool _enabled;

		/// <summary>
		/// Actual underlying widget associated with this Tab instance
		/// </summary>
		internal new OutlookBarButton UnderlyingWidget { get; set; }

		/// <summary>
		/// Is this tab enabled.
		/// </summary>
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				_enabled = value;
				if (UnderlyingWidget != null)
					UnderlyingWidget.Enabled = _enabled;
			}
		}

		public Tab(string name) : base(name)
		{
			Enabled = true;
		}
	}
}
