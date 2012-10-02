// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FontHeightAdjusterTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using NUnit.Framework;
using NMock;
using NMock.Constraints;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.Widgets
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FontHeightAdjuster.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FontHeightAdjusterTests
	{

		#region Data Members
		TestFwStylesheet m_stylesheet;
		ILgWritingSystemFactory m_wsf;
		int m_hvoGermanWs;
		int m_hvoEnglishWs;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up some dummy styles for testing purposes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void SetupStyles()
		{
			m_stylesheet = new TestFwStylesheet();
			m_wsf = LgWritingSystemFactoryClass.Create();
			m_wsf.BypassInstall = true;

			// German
			IWritingSystem wsGerman = m_wsf.get_Engine("de");
			m_hvoGermanWs = wsGerman.WritingSystem;
			Assert.IsTrue(m_hvoGermanWs > 0, "Should have gotten an hvo for the German WS");
			// English
			IWritingSystem wsEnglish = m_wsf.get_Engine("en");
			m_hvoEnglishWs = wsEnglish.WritingSystem;
			Assert.IsTrue(m_hvoEnglishWs > 0, "Should have gotten an hvo for the English WS");
			Assert.IsTrue(m_hvoEnglishWs != m_hvoGermanWs, "Writing systems should have different IDs");

			// Create a couple of styles
			int hvoStyle = m_stylesheet.MakeNewStyle();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, "Arial");
			m_stylesheet.PutStyle("StyleA", "bla", hvoStyle, 0, hvoStyle, 1, false, false,
				propsBldr.GetTextProps());

			hvoStyle = m_stylesheet.MakeNewStyle();
			propsBldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, "Times New Roman");
			m_stylesheet.PutStyle("StyleB", "bla", hvoStyle, 0, hvoStyle, 1, false, false,
				propsBldr.GetTextProps());

			// Override the font size for each writing system and each style.
			List<FontOverride> fontOverrides = new List<FontOverride>(2);
			FontOverride fo;
			fo.writingSystem = m_hvoGermanWs;
			fo.fontSize = 13;
			fontOverrides.Add(fo);
			fo.writingSystem = m_hvoEnglishWs;
			fo.fontSize = 21;
			fontOverrides.Add(fo);
			m_stylesheet.OverrideFontsForWritingSystems("StyleA", fontOverrides);

			fontOverrides.Clear();
			fo.writingSystem = m_hvoGermanWs;
			fo.fontSize = 56;
			fontOverrides.Add(fo);
			fo.writingSystem = m_hvoEnglishWs;
			fo.fontSize = 20;
			fontOverrides.Add(fo);
			m_stylesheet.OverrideFontsForWritingSystems("StyleB", fontOverrides);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shut down the writing system factory and let the factory workers go home to spend
		/// some quality time with their families.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void DestroyTheFactory()
		{
			m_wsf.Shutdown();
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetFontHeightForStyle method.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void TestGetFontHeightForStyle()
		{
			Assert.AreEqual(13000, FontHeightAdjuster.GetFontHeightForStyle("StyleA",
				m_stylesheet, m_hvoGermanWs, m_wsf));
			Assert.AreEqual(21000, FontHeightAdjuster.GetFontHeightForStyle("StyleA",
				m_stylesheet, m_hvoEnglishWs, m_wsf));
			Assert.AreEqual(56000, FontHeightAdjuster.GetFontHeightForStyle("StyleB",
				m_stylesheet, m_hvoGermanWs, m_wsf));
			Assert.AreEqual(20000, FontHeightAdjuster.GetFontHeightForStyle("StyleB",
				m_stylesheet, m_hvoEnglishWs, m_wsf));
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetAdjustedTsString method.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void TestGetAdjustedTsString()
		{
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsStrBldr strBldrExpected = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			ITsPropsBldr propsBldrExpected;

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleA");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoGermanWs);
			strBldr.ReplaceRgch(0, 0, "Hello People", 12, propsBldr.GetTextProps());
			strBldrExpected.ReplaceRgch(0, 0, "Hello People", 12, propsBldr.GetTextProps());

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleA");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoEnglishWs);
			strBldr.ReplaceRgch(0, 0, "Hello numero dos", 16, propsBldr.GetTextProps());
			propsBldrExpected = propsBldr;
			propsBldrExpected.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 20500);
			strBldrExpected.ReplaceRgch(0, 0, "Hello numero dos", 16, propsBldrExpected.GetTextProps());

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleB");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoGermanWs);
			strBldr.ReplaceRgch(0, 0, "3 Hello", 7, propsBldr.GetTextProps());
			propsBldrExpected = propsBldr;
			propsBldrExpected.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 20750);
			strBldrExpected.ReplaceRgch(0, 0, "3 Hello", 7, propsBldrExpected.GetTextProps());

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleB");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoEnglishWs);
			strBldr.ReplaceRgch(0, 0, "This is 4", 9, propsBldr.GetTextProps());
			strBldrExpected.ReplaceRgch(0, 0, "This is 4", 9, propsBldr.GetTextProps());

			ITsString tss = FontHeightAdjuster.GetAdjustedTsString(strBldr.GetString(), 23000, m_stylesheet, m_wsf);
			AssertEx.AreTsStringsEqual(strBldrExpected.GetString(), tss);
		}
	}
}
