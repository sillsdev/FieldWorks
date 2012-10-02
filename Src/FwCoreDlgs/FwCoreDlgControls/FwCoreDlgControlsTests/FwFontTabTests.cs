// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StyleInfoTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the font control tab.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwFontTabTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>Field Works font tab control for testing.</summary>
		FwFontTab m_fontTab;

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize for tests.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_fontTab = new FwFontTab();
			m_fontTab.FillFontInfo(Cache);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Tear down after tests.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			base.TestTearDown();

			m_fontTab.Dispose();
			m_fontTab = null;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test selecting a user-defined character style when it is based on a style with an
		/// unspecified font and the user-defined character style specifies it.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void UserDefinedCharacterStyle_ExplicitFontName()
		{
			// Create a style with an unspecified font name.
			var charStyle = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LangProject.StylesOC.Add(charStyle);
			charStyle.Context = ContextValues.Text;
			charStyle.Function = FunctionValues.Prose;
			charStyle.Structure = StructureValues.Body;
			charStyle.Type = StyleType.kstCharacter;
			var basedOn = new StyleInfo(charStyle);

			// Create a user-defined character style inherited from the previously-created character
			// style, but this style has a font name specified.
			var charStyleInfo = new StyleInfo("New Char Style", basedOn,
				StyleType.kstCharacter, Cache);
//			FontInfo charFontInfo = charStyleInfo.FontInfoForWs(Cache.DefaultVernWs);
			m_fontTab.UpdateForStyle(charStyleInfo);

			// Select a font name for the style (which will call the event handler
			// m_cboFontNames_SelectedIndexChanged).
			var cboFontNames = ReflectionHelper.GetField(m_fontTab, "m_cboFontNames") as FwInheritablePropComboBox;
			Assert.IsNotNull(cboFontNames);
			cboFontNames.AdjustedSelectedIndex = 1;
			// Make sure we successfully set the font for this user-defined character style.
			Assert.IsTrue(charStyleInfo.FontInfoForWs(-1).m_fontName.IsExplicit);
			Assert.AreEqual("<default font>", charStyleInfo.FontInfoForWs(-1).m_fontName.Value,
				"The font should have been set to the default font.");
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure font names are alphabetically sorted in combobox.
		/// Related to FWNX-273: Fonts not in alphabetical order
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void FillFontNames_IsAlphabeticallySorted()
		{
			const int firstActualFontNameInListLocation = 4;
			CallMethod(m_fontTab,"FillFontNames", true);
			var fontNames = ((ComboBox) GetField(m_fontTab, "m_cboFontNames")).Items;
			for (var i = firstActualFontNameInListLocation; i+1 < fontNames.Count; i++)
			{
				// Check that each font in the list is alphabetically before the next font in the list
				Assert.LessOrEqual(fontNames[i] as string, fontNames[i+1] as string, "Font names not alphabetically sorted.");
			}
		}
	}
}