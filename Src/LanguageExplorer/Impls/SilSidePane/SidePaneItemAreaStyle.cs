<<<<<<< HEAD:Src/LanguageExplorer/Impls/SilSidePane/SidePaneItemAreaStyle.cs
// SilSidePane, Copyright 2010-2020 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
||||||| f013144d5:Src/XCore/SilSidePane/SidePaneItemAreaStyle.cs
﻿// SilSidePane, Copyright 2010 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
=======
﻿// Copyright (c) 2010 SIL International
// SilOutlookBar is licensed under the MIT license.
>>>>>>> develop:Src/XCore/SilSidePane/SidePaneItemAreaStyle.cs

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Style of the item area
	/// </summary>
	/// <remarks>Must be public, because some test uses the enum as a parameter to a method that must be public.</remarks>
	public enum SidePaneItemAreaStyle
	{
		/// <summary>Strip of buttons with large icons</summary>
		Buttons,
		/// <summary>List with small icons</summary>
		List,
		/// <summary>List with small icons, implemented as a ToolStrip</summary>
		StripList,
	}
}