// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FontHeightAdjusterTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Widgets
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FontHeightAdjuster.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FontHeightAdjusterTests: BaseTest
	{

		#region Data Members
		TestFwStylesheet m_stylesheet;
		WritingSystemManager m_wsManager;
		int m_hvoGermanWs;
		int m_hvoEnglishWs;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up some dummy styles for testing purposes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_stylesheet = new TestFwStylesheet();
			m_wsManager = new WritingSystemManager();

			// English
			CoreWritingSystemDefinition enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_hvoEnglishWs = enWs.Handle;
			Assert.IsTrue(m_hvoEnglishWs > 0, "Should have gotten an hvo for the English WS");
			// German
			CoreWritingSystemDefinition deWs;
			m_wsManager.GetOrSet("de", out deWs);
			m_hvoGermanWs = deWs.Handle;
			Assert.IsTrue(m_hvoGermanWs > 0, "Should have gotten an hvo for the German WS");
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
			fo.writingSystem = m_hvoEnglishWs;
			fo.fontSize = 21;
			fontOverrides.Add(fo);
			fo.writingSystem = m_hvoGermanWs;
			fo.fontSize = 13;
			fontOverrides.Add(fo);
			m_stylesheet.OverrideFontsForWritingSystems("StyleA", fontOverrides);

			fontOverrides.Clear();
			fo.writingSystem = m_hvoEnglishWs;
			fo.fontSize = 20;
			fontOverrides.Add(fo);
			fo.writingSystem = m_hvoGermanWs;
			fo.fontSize = 56;
			fontOverrides.Add(fo);
			m_stylesheet.OverrideFontsForWritingSystems("StyleB", fontOverrides);
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
				m_stylesheet, m_hvoGermanWs, m_wsManager));
			Assert.AreEqual(21000, FontHeightAdjuster.GetFontHeightForStyle("StyleA",
				m_stylesheet, m_hvoEnglishWs, m_wsManager));
			Assert.AreEqual(56000, FontHeightAdjuster.GetFontHeightForStyle("StyleB",
				m_stylesheet, m_hvoGermanWs, m_wsManager));
			Assert.AreEqual(20000, FontHeightAdjuster.GetFontHeightForStyle("StyleB",
				m_stylesheet, m_hvoEnglishWs, m_wsManager));
		}

		private int GetUbuntuVersion()
		{
			if (!MiscUtils.IsUnix)
				return 0;

			try
			{
				var startInfo = new ProcessStartInfo {
					RedirectStandardOutput = true,
					UseShellExecute = false,
					FileName = "lsb_release",
					Arguments = "-r -s"
				};
				using (var proc = Process.Start(startInfo))
				{
					var value = proc.StandardOutput.ReadToEnd().TrimEnd();
					proc.WaitForExit();
					if (value.Contains("."))
						return int.Parse(value.Split('.')[0]);
				}
			}
			catch (Exception)
			{
				// Just ignore and continue with the default
			}
			return -1;
		}

		private int GetExpectedFontHeightForArial()
		{
			// The font height to expect on Ubuntu 14.04 and upwards depends on the font mapping.
			// If "Arial" is mapped to LiberationSans-Regular.ttf then we get the same value as
			// on Windows.
			// However, if there is a ~/.fonts.conf file that maps Arial to Arial, then we get
			// a slightly different value, probably due to rounding errors.
			if (MiscUtils.IsUnix && GetUbuntuVersion() >= 14)
			{
				try
				{
					var startInfo = new ProcessStartInfo {
						RedirectStandardOutput = true,
						UseShellExecute = false,
						FileName = "fc-match",
						Arguments = "Arial"
					};
					using (var proc = Process.Start(startInfo))
					{
						var value = proc.StandardOutput.ReadToEnd().TrimEnd();
						proc.WaitForExit();
						if (value.StartsWith("Arial", StringComparison.Ordinal))
							return 19750;
					}
				}
				catch (Exception)
				{
					// Just ignore and continue with the default
				}
			}
			return 20750;
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
				(int)FwTextPropVar.ktpvMilliPoint, GetExpectedFontHeightForArial());
			strBldrExpected.ReplaceRgch(0, 0, "3 Hello", 7, propsBldrExpected.GetTextProps());

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleB");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoEnglishWs);
			strBldr.ReplaceRgch(0, 0, "This is 4", 9, propsBldr.GetTextProps());
			strBldrExpected.ReplaceRgch(0, 0, "This is 4", 9, propsBldr.GetTextProps());

			ITsString tss = FontHeightAdjuster.GetAdjustedTsString(strBldr.GetString(), 23000, m_stylesheet, m_wsManager);
			AssertEx.AreTsStringsEqual(strBldrExpected.GetString(), tss);
		}
	}
}
