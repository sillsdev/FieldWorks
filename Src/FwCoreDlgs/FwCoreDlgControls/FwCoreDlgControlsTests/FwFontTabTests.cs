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
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the font control tab.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwFontTabTests : InMemoryFdoTestBase
	{
		/// <summary>Field Works font tab control for testing.</summary>
		FwFontTab m_fontTab;

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize for tests.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			base.Initialize();

			m_fontTab = new FwFontTab();
			m_fontTab.FillFontInfo(Cache);
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
			IStStyle charStyle = new StStyle();
			Cache.LangProject.StylesOC.Add(charStyle);
			charStyle.Context = ContextValues.Text;
			charStyle.Function = FunctionValues.Prose;
			charStyle.Structure = StructureValues.Body;
			charStyle.Type = StyleType.kstCharacter;
			StyleInfo basedOn = new StyleInfo(charStyle);

			// Create a user-defined character style inherited from the previously-created character
			// style, but this style has a font name specified.
			StyleInfo charStyleInfo = new StyleInfo("New Char Style", basedOn,
				StyleType.kstCharacter, Cache);
			FontInfo charFontInfo = charStyleInfo.FontInfoForWs(Cache.DefaultVernWs);
			m_fontTab.UpdateForStyle(charStyleInfo);

			// Select a font name for the style (which will call the event handler
			// m_cboFontNames_SelectedIndexChanged).
			FwInheritablePropComboBox cboFontNames =
				ReflectionHelper.GetField(m_fontTab, "m_cboFontNames") as FwInheritablePropComboBox;
			Assert.IsNotNull(cboFontNames);
			cboFontNames.AdjustedSelectedIndex = 2;
			// Make sure we successfully set the font for this user-defined character style.
			Assert.IsTrue(charStyleInfo.FontInfoForWs(-1).m_fontName.IsExplicit);
			Assert.AreEqual("<default pub font>", charStyleInfo.FontInfoForWs(-1).m_fontName.Value,
				"The font should have been set to the default publication font.");

			cboFontNames.AdjustedSelectedIndex = 3;
			// Make sure we successfully set the font for this user-defined character style.
			Assert.IsTrue(charStyleInfo.FontInfoForWs(-1).m_fontName.IsExplicit);
			Assert.AreEqual("<default sans serif>", charStyleInfo.FontInfoForWs(-1).m_fontName.Value,
				"The font should have been set to the default heading font (i.e., sans-serif).");
		}
	}
}
