// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File:
// Responsibility:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary></summary>
	[TestFixture]
	public class FwFontDialogTests : BaseTest
	{
		/// <summary>
		/// Object to test
		/// </summary>
		private FwFontDialog m_dialog;

		#region Setup
		/// <summary></summary>
		[SetUp]
		public void TestSetup()
		{
			m_dialog = new FwFontDialog(null);
		}

		/// <summary></summary>
		[TearDown]
		public void TestTearDown()
		{
			m_dialog.Dispose();
		}
		#endregion

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure font names are alphabetically sorted.
		/// Related to FWNX-273: Fonts not in alphabetical order
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void FillFontList_IsAlphabeticallySorted()
		{
			const int firstFontInListLocation = 3;
			CallMethod(m_dialog,"FillFontList");
			var fontNames = ((ListBox) GetField(m_dialog, "m_lbFontNames")).Items;
			for (var i = firstFontInListLocation; i+1 < fontNames.Count; i++)
			{
				// Check that each font in the list is alphabetically before the next font in the list
				Assert.LessOrEqual(fontNames[i] as string, fontNames[i+1] as string, "Font names not alphabetically sorted.");
			}
		}
	}
}