// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Tests for EditingHelper class that test code that deals with the clipboard.
	/// </summary>
	/// <remarks>This derived class uses the real clipboard.</remarks>
	[TestFixture]
	public class EditingHelperTests_ClipboardReal: EditingHelperTests_Clipboard
	{
		protected override void SetClipboardAdapter()
		{
			// do nothing so that we get the default (system) clipboard
		}
	}
}