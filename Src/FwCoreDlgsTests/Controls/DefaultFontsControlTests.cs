// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Tests for DefaultFontsControl, a control used in the Writing System Properties dialog
	/// </summary>
	[TestFixture]
	public class DefaultFontsControlTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>object for testing.</summary>
		DefaultFontsControl m_fontsControl;

		/// <summary>
		/// Initialize for tests.
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			m_fontsControl = new DefaultFontsControl();
		}

		/// <summary>
		/// Tear down after tests.
		/// </summary>
		public override void TestTearDown()
		{
			base.TestTearDown();

			m_fontsControl.Dispose();
			m_fontsControl = null;
		}

		/// <summary>
		/// Make sure font names are alphabetically sorted in control.
		/// Related to FWNX-273: Fonts not in alphabetical order
		/// </summary>
		[Test]
		public void FontsAreAlphabeticallySorted()
		{
			var fontNamesNormal = m_fontsControl.DefaultFontComboBox.Items;

			for (var i = 0; i + 1 < fontNamesNormal.Count; i++)
			{
				// Check that each font in the list is alphabetically before the next font in the list
				Assert.LessOrEqual(fontNamesNormal[i] as string, fontNamesNormal[i + 1] as string, "Font names not alphabetically sorted.");
			}
		}
	}
}