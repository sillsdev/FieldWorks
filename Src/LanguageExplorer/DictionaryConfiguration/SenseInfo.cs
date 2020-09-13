// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <remarks>
	/// Presently, this handles only Sense Info, but if other info needs to be handed down the call stack in the future, we could rename this
	/// </remarks>
	internal struct SenseInfo
	{
		internal int SenseCounter { get; set; }
		internal string SenseOutlineNumber { get; set; }
		internal string ParentSenseNumberingStyle { get; set; }
	}
}