// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwStyleSheetTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FDO.FDOTests.CellarTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Testing subclass of FwStyleSheet, that can access the protected method "ComputeDerivedStyles()"
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyFwStyleSheet : FwStyleSheet
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a font face name for a style.
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="fontName"></param>
		/// ------------------------------------------------------------------------------------
		internal void SetStyleFont(string styleName, string fontName)
		{
			var style = FindStyle(styleName);
			var ttpBldr = style.Rules.GetBldr();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, fontName);
			style.Rules = ttpBldr.GetTextProps();
			ComputeDerivedStyles();
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FwStyleSheet class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwStyleSheetTests : ScrInMemoryFdoTestBase
	{
		private DummyFwStyleSheet m_styleSheet;
		private ILgWritingSystemFactory m_wsf;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// For these tests we don't need to run InstallLanguage.
			m_wsf = Cache.WritingSystemFactory;

			// Done by MemoryOnlyBackendProviderTestBase FixtureSetup.
//			// The GetFontFaceNameFromStyle needs a vern WS.
//			var frenchWs = Cache.WritingSystemFactory.GetWsFromStr("fr");
//			var french = Cache.ServiceLocator.GetInstance<ILgWritingSystemRepository>().GetObject(frenchWs);
//			Cache.LanguageProject.VernWssRC.Add(french);
//			Cache.LanguageProject.CurVernWssRS.Add(french);
		}

		/// <summary>
		/// Clear out test data.
		/// </summary>
		public override void FixtureTeardown()
		{
			m_wsf = null;
			m_styleSheet = null;

			base.FixtureTeardown();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_styleSheet = new DummyFwStyleSheet();
			m_styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that adding a style and deleting a style work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDeleteStyle()
		{
			var tsPropsBldr = TsPropsBldrClass.Create();
			var ttpFormattingProps = tsPropsBldr.GetTextProps(); // default properties
			var nStylesOrig = m_styleSheet.CStyles;

			// get an hvo for the new style
			var hvoStyle = m_styleSheet.MakeNewStyle();
			var style = Cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoStyle);

			// PutStyle() adds the style to the stylesheet
			m_styleSheet.PutStyle("MyNewStyle", "bla", hvoStyle, 0,
				hvoStyle, 0, false, false, ttpFormattingProps);

			Assert.AreEqual(nStylesOrig + 1, m_styleSheet.CStyles);
			Assert.AreEqual(ttpFormattingProps, m_styleSheet.GetStyleRgch(0, "MyNewStyle"),
				"Should get correct format props for the style added");

			// Make style be based on section head and check context
			var baseOnStyle = m_scr.FindStyle(ScrStyleNames.SectionHead);
			m_styleSheet.PutStyle("MyNewStyle", "bla", hvoStyle, baseOnStyle.Hvo,
				hvoStyle, 0, false, false, ttpFormattingProps);
			Assert.AreEqual(baseOnStyle.Context, style.Context);

			// Now delete the new style
			m_styleSheet.Delete(hvoStyle);

			// Verfiy the deletion
			Assert.AreEqual(nStylesOrig, m_styleSheet.CStyles);
			Assert.IsNull(m_styleSheet.GetStyleRgch(0, "MyNewStyle"),
				"Should get null because style is not there");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure attempting to delete a built-in style throws an exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void DeleteBuiltInStyle()
		{
			// get the hvo of the Verse Number style
			var hvoStyle = -1;
			for (var i = 0; i < m_styleSheet.CStyles; i++)
			{
				if (m_styleSheet.get_NthStyleName(i) == "Verse Number")
					hvoStyle = m_styleSheet.get_NthStyle(i);
			}
			Assert.IsTrue(hvoStyle != -1, "Style 'Verse Number' should exist in DB");

			// attempting to delete this built-in style should throw an exception
			m_styleSheet.Delete(hvoStyle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFaceNameFromStyle method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFontFaceNameFromStyle()
		{
			// Get the default font names
			IWritingSystem defaultVernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			string defaultSerif = defaultVernWs.DefaultFontName;

			// do the tests
			m_styleSheet.SetStyleFont("Section Head", "Helvetica");
			Assert.AreEqual("Helvetica", m_styleSheet.GetFaceNameFromStyle("Section Head",
				defaultVernWs.Handle, m_wsf));

			m_styleSheet.SetStyleFont("Paragraph", "Symbol");
			Assert.AreEqual("Symbol", m_styleSheet.GetFaceNameFromStyle("Paragraph",
				defaultVernWs.Handle, m_wsf));

			m_styleSheet.SetStyleFont("Intro Section Head", StyleServices.DefaultFont);
			Assert.AreEqual(defaultSerif, m_styleSheet.GetFaceNameFromStyle(
				"Intro Section Head", defaultVernWs.Handle, m_wsf));

			m_styleSheet.SetStyleFont("Intro Paragraph", StyleServices.DefaultFont);
			Assert.AreEqual(defaultSerif, m_styleSheet.GetFaceNameFromStyle("Intro Paragraph",
				defaultVernWs.Handle, m_wsf));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the StyleCollection class works correct with composed and decomposed
		/// form of strings (TE-6090)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void StyleCollectionWorksWithUpperAscii()
		{
			const string styleName = "\u00e1bc";
			var style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			Cache.LanguageProject.StylesOC.Add(style);
			style.Name = styleName;

			var styleCollection = new FwStyleSheet.StyleInfoCollection {new BaseStyleInfo(style)};

			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormC)));
			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormD)));
			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormKC)));
			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormKD)));
		}
	}
}
