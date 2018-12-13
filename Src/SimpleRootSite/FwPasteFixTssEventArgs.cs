// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary />
	public delegate void FwPasteFixTssEventHandler(EditingHelper sender, FwPasteFixTssEventArgs e);

	/// <summary>
	/// This event argument class is used for fixing the text properties of Pasted text in
	/// EditingHelper objects whose owning SimpleRootSite object requires specific properties.
	/// See LT-1445 for motivation.  Other final adjustments to the ITsString value
	/// may also be made if there's any such need.  The handler function is called just before
	/// replacing the selection in the root box with the given ITsString.
	/// </summary>
	public class FwPasteFixTssEventArgs
	{
		/// <summary />
		/// <param name="tss">The ITsString to paste.</param>
		/// <param name="tsi">The TextSelInfo of the selection at the start of the paste.</param>
		public FwPasteFixTssEventArgs(ITsString tss, TextSelInfo tsi)
		{
			TsString = tss;
			TextSelInfo = tsi;
			EventHandled = false;
		}

		/// <summary>
		/// Gets or sets the TsString to paste (handlers can modify this).
		/// </summary>
		public ITsString TsString { get; set; }

		/// <summary>
		/// The TextSelInfo of the selection at the start of the paste
		/// </summary>
		public TextSelInfo TextSelInfo { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the event was handled.
		/// </summary>
		public bool EventHandled { get; set; }
	}
}