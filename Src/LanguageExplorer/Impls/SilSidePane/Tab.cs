<<<<<<< HEAD:Src/LanguageExplorer/Impls/SilSidePane/Tab.cs
// SilSidePane, Copyright 2009-2020 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
||||||| f013144d5:Src/XCore/SilSidePane/Tab.cs
﻿// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
=======
﻿// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.
>>>>>>> develop:Src/XCore/SilSidePane/Tab.cs

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Tab on a SidePane.
	/// </summary>
	internal sealed class Tab : Item
	{
		private bool _enabled;

		/// <summary>
		/// Actual underlying widget associated with this Tab instance
		/// </summary>
		internal new OutlookBarButton UnderlyingWidget { get; set; }

		/// <summary>
		/// Is this tab enabled.
		/// </summary>
		internal bool Enabled
		{
			get => _enabled;
			set
			{
				_enabled = value;
				if (UnderlyingWidget != null)
				{
					UnderlyingWidget.Enabled = _enabled;
				}
			}
		}

		/// <summary />
		internal Tab(string name)
			: base(name)
		{
			Enabled = true;
		}
	}
}