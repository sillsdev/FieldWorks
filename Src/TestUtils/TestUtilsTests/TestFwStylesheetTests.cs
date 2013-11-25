// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TestFwStylesheetTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests for the TestFwStylesheet class
// </remarks>

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TestFwStylesheetTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TestFwStylesheetTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to add a new style and retrieve it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddAndRetrieveStyle()
		{
			var stylesheet = new TestFwStylesheet();
			int hvoNewStyle = stylesheet.MakeNewStyle();
			stylesheet.PutStyle("FirstStyle", "bls", hvoNewStyle, 0, hvoNewStyle, 0, false,
				false, null);
			Assert.AreEqual(hvoNewStyle, stylesheet.get_NthStyle(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to retrieve text props for a named style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetStyleRgch()
		{
			IVwStylesheet stylesheet = (IVwStylesheet)new TestFwStylesheet();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, "Times");
			ITsTextProps props1 = propsBldr.GetTextProps();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, 256);
			ITsTextProps props2 = propsBldr.GetTextProps();
			int hvoNewStyle1 = stylesheet.MakeNewStyle();
			stylesheet.PutStyle("FirstStyle", "bla", hvoNewStyle1, 0, hvoNewStyle1, 0, false,
				false, props1);
			int hvoNewStyle2 = stylesheet.MakeNewStyle();
			stylesheet.PutStyle("SecondStyle", "bla", hvoNewStyle2, 0, hvoNewStyle1, 0, false,
				false, props2);
			string sHowDifferent;
			bool fEqual = TsTextPropsHelper.PropsAreEqual(props2,
				stylesheet.GetStyleRgch(0, "SecondStyle"), out sHowDifferent);
			Assert.IsTrue(fEqual, sHowDifferent);
			fEqual = TsTextPropsHelper.PropsAreEqual(props1,
				stylesheet.GetStyleRgch(0, "FirstStyle"), out sHowDifferent);
			Assert.IsTrue(fEqual, sHowDifferent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to retrieve the name of the Next style for the given named style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetNextStyle()
		{
			var stylesheet = new TestFwStylesheet();
			int hvoNewStyle1 = stylesheet.MakeNewStyle();
			stylesheet.PutStyle("FirstStyle", "bla", hvoNewStyle1, 0, 0, 0, false, false, null);
			int hvoNewStyle2 = stylesheet.MakeNewStyle();
			stylesheet.PutStyle("SecondStyle", "bla", hvoNewStyle2, 0, hvoNewStyle1, 0, false,
				false, null);
			Assert.AreEqual("FirstStyle", stylesheet.GetNextStyle("SecondStyle"));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to retrieve the name of the Based On style for the given named style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetBasedOnStyle()
		{
			var stylesheet = new TestFwStylesheet();
			int hvoNewStyle1 = stylesheet.MakeNewStyle();
			stylesheet.PutStyle("FirstStyle", "bla", hvoNewStyle1, 0, 0, 0, false, false, null);
			int hvoNewStyle2 = stylesheet.MakeNewStyle();
			stylesheet.PutStyle("SecondStyle", "bla", hvoNewStyle2, hvoNewStyle1, 0, 0, false,
				false, null);
			Assert.AreEqual("FirstStyle", stylesheet.GetBasedOn("SecondStyle"));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to override the font size for a single writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestOverrideFontForWritingSystem_ForStyleWithNullProps()
		{
			var stylesheet = new TestFwStylesheet();
			int hvoNewStyle1 = stylesheet.MakeNewStyle();

			stylesheet.PutStyle("FirstStyle", "bla", hvoNewStyle1, 0, 0, 0, false, false, null);

			var wsf = new PalasoWritingSystemManager();
			ILgWritingSystem ws = wsf.get_Engine("de");
			int hvoGermanWs = ws.Handle;
			Assert.IsTrue(hvoGermanWs > 0, "Should have gotten an hvo for the German WS");

			// Array of 1 struct, contains writing system and font size to override
			List<FontOverride> fontOverrides = new List<FontOverride>(1);
			FontOverride aFontOverride;
			aFontOverride.writingSystem = hvoGermanWs;
			aFontOverride.fontSize = 48;
			fontOverrides.Add(aFontOverride);
			stylesheet.OverrideFontsForWritingSystems("FirstStyle", fontOverrides);

			//check results
			IVwPropertyStore vwps = VwPropertyStoreClass.Create();
			vwps.Stylesheet = stylesheet;
			vwps.WritingSystemFactory = wsf;

			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "FirstStyle");
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, hvoGermanWs);
			ITsTextProps ttp = ttpBldr.GetTextProps();

			LgCharRenderProps chrps = vwps.get_ChrpFor(ttp);
			ws.InterpretChrp(ref chrps);

			Assert.AreEqual(48, chrps.dympHeight / 1000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to override the font size for multiple writing systems, where style
		/// has underlying props set as well.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestOverrideFontsForWritingSystems_ForStyleWithProps()
		{
			var stylesheet = new TestFwStylesheet();
			int hvoNewStyle1 = stylesheet.MakeNewStyle();

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, "Arial");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 23000);
			stylesheet.PutStyle("FirstStyle", "bla", hvoNewStyle1, 0, 0, 0, false, false,
				propsBldr.GetTextProps());

			var wsf = new PalasoWritingSystemManager();
			ILgWritingSystem wsIngles = wsf.get_Engine("en");
			int hvoInglesWs = wsIngles.Handle;
			Assert.IsTrue(hvoInglesWs > 0, "Should have gotten an HVO for the English WS");

			ILgWritingSystem wsFrench = wsf.get_Engine("fr");
			int hvoFrenchWs = wsFrench.Handle;
			Assert.IsTrue(hvoFrenchWs > 0, "Should have gotten an HVO for the French WS");

			ILgWritingSystem wsGerman = wsf.get_Engine("de");
			int hvoGermanWs = wsGerman.Handle;
			Assert.IsTrue(hvoGermanWs > 0, "Should have gotten an HVO for the German WS");

			Assert.IsTrue(hvoFrenchWs != hvoGermanWs, "Should have gotten different HVOs for each WS");
			Assert.IsTrue(hvoInglesWs != hvoGermanWs, "Should have gotten different HVOs for each WS");
			Assert.IsTrue(hvoFrenchWs != hvoInglesWs, "Should have gotten different HVOs for each WS");

			// Array of structs, containing writing systems and font sizes to override.
			var fontOverrides = new List<FontOverride>(2);
			FontOverride aFontOverride;
			aFontOverride.writingSystem = hvoInglesWs;
			aFontOverride.fontSize = 34;
			fontOverrides.Add(aFontOverride);
			aFontOverride.writingSystem = hvoGermanWs;
			aFontOverride.fontSize = 48;
			fontOverrides.Add(aFontOverride);
			stylesheet.OverrideFontsForWritingSystems("FirstStyle", fontOverrides);

			//check results
			IVwPropertyStore vwps = VwPropertyStoreClass.Create();
			vwps.Stylesheet = stylesheet;
			vwps.WritingSystemFactory = wsf;

			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "FirstStyle");
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, hvoFrenchWs);
			ITsTextProps ttpFrench = ttpBldr.GetTextProps();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, hvoGermanWs);
			ITsTextProps ttpGerman = ttpBldr.GetTextProps();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, hvoInglesWs);
			ITsTextProps ttpIngles = ttpBldr.GetTextProps();

			LgCharRenderProps chrpsFrench = vwps.get_ChrpFor(ttpFrench);
			LgCharRenderProps chrpsGerman = vwps.get_ChrpFor(ttpGerman);
			LgCharRenderProps chrpsIngles = vwps.get_ChrpFor(ttpIngles);
			wsFrench.InterpretChrp(ref chrpsFrench);
			wsGerman.InterpretChrp(ref chrpsGerman);
			wsIngles.InterpretChrp(ref chrpsIngles);

			Assert.AreEqual(23, chrpsFrench.dympHeight / 1000);
			Assert.AreEqual(34, chrpsIngles.dympHeight / 1000);
			Assert.AreEqual(48, chrpsGerman.dympHeight / 1000);
		}
	}
}
