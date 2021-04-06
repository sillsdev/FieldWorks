// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

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
