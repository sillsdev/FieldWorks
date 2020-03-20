// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>Paste status indicates how writing systems should be handled during a paste</summary>
	public enum PasteStatus
	{
		/// <summary>When pasting, use the writing system at the destination</summary>
		UseDestWs,
		/// <summary>When pasting, preserve the original writing systems, even if new writing systems
		/// would need to be created.</summary>
		PreserveWs,
		/// <summary>Cancel paste operation.</summary>
		CancelPaste
	}
}