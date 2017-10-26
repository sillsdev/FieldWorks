// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Parameter class that holds things needed to create a MultiPane instance
	/// </summary>
	internal class MultiPaneParameters
	{
		/// <summary />
		internal SplitterChildControlParameters FirstControlParameters { get; set; }
		/// <summary />
		internal SplitterChildControlParameters SecondControlParameters { get; set; }
		/// <summary />
		internal Orientation Orientation { get; set; }
		/// <summary />
		internal string ToolMachineName { get; set; }
		/// <summary />
		internal IArea Area { get; set; }
		/// <summary />
		internal int FirstCollapseZone { get; set; }
		/// <summary />
		internal int SecondCollapseZone { get; set; }
		/// <summary />
		internal string Id { get; set; }
		/// <summary>
		/// Optional. Defaults to "50%".
		/// </summary>
		internal string DefaultFixedPaneSizePoints { get; set; }
		/// <summary>
		/// Optional
		/// </summary>
		internal string DefaultPrintPane { get; set; }
		/// <summary />
		internal string DefaultFocusControl { get; set; }
		/// <summary>
		/// Optional
		/// </summary>
		internal string PersistContext { get; set; }
		/// <summary>
		/// Optional
		/// </summary>
		internal string Label { get; set; }
	}
}