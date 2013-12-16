// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeStylesXmlAccessorTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Globalization;
using System.Threading;
using System.Linq;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	#region DummyTeStylesXmlAccessor
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// DummyTeStylesXmlAccessor class exposes aspects of <see cref="TeStylesXmlAccessor"/>
	/// class for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class DummyTeStylesXmlAccessor : TeStylesXmlAccessor
	{
		private static int s_reservedStyleCount;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the base constructor.
		/// </summary>
		/// <param name="scr"></param>
		/// ------------------------------------------------------------------------------------
		public DummyTeStylesXmlAccessor(IScripture scr) :
			base(scr)
		{
			m_databaseStyles = scr.StylesOC;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the base constructor and initializes the styles node directly.
		/// </summary>
		/// <param name="scr"></param>
		/// <param name="teStyles"></param>
		/// ------------------------------------------------------------------------------------
		public DummyTeStylesXmlAccessor(IScripture scr, XmlNode teStyles) :
			this(scr)
		{
			m_sourceStyles = teStyles;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the CreateScrStyles method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void CallCreateScrStyles(IScripture scr, XmlNode teStyles)
		{
			IThreadedProgress progressDlg = new DummyProgressDlg();
			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(scr, teStyles);
			acc.InitLoading(progressDlg, scr, teStyles);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose protected method for testing, with simpler arguments.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="scr">The scripture object.</param>
		/// <param name="teStyles"></param>
		/// ------------------------------------------------------------------------------------
		public void InitLoading(IProgress progressDlg, IScripture scr, XmlNode teStyles)
		{
			m_scr = scr;
			CreateStyles(progressDlg, scr.StylesOC, teStyles);
			s_reservedStyleCount = m_htReservedStyles.Count;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the LoadTeStylesDoc method
		/// </summary>
		/// <returns>An XmlNode representing the Styles in TeStyles.xml
		/// </returns>
		/// ------------------------------------------------------------------------------------
		static public XmlNode CallLoadTeStylesDoc(IScripture scr)
		{
			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(scr);
			return acc.LoadDoc();
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the ReplaceFormerStyles method
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public void CallReplaceFormerStyles()
		{
			base.ReplaceFormerStyles();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make DeleteDeprecatedStylesAndDetermineReplacements accessible
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallDeleteDeprecatedStylesAndDetermineReplacements()
		{
			IThreadedProgress progressDlg = new DummyProgressDlg();
			m_progressDlg = progressDlg;
			DeleteDeprecatedStylesAndDetermineReplacements();
			m_progressDlg = null;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the CreateScrStyles method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateScrStyles()
		{
			CreateStyles();
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// This is slow, so don't do it for tests.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override void ReplaceFormerStyles()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the m_styleReplacements member
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, string> StyleReplacements
		{
			get { return m_styleReplacements; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the m_userModifiedStyles member
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<string> CallUserModifiedStyles
		{
			get { return UserModifiedStyles; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the count of reserved styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int ReservedStyleCount
		{
			get { return s_reservedStyleCount; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hashtable of original styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, IStStyle> OriginalStyles
		{
			get { return m_htOrigStyles; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hashtable of updated styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, IStStyle> UpdatedStyles
		{
			get { return m_htUpdatedStyles; }
		}
	}
	#endregion

	#region class TeStylesXmlAccessorTests_WithoutCache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the TeStylesXmlAccessor class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeStylesXmlAccessorTests_WithoutCache: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		#region Test reading of XML attributes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test TeStylesXmlAccessorGetBoolAttribute method when valid data is passed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBoolAttributeTest()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version='1.0' ?>" +
				"<blah test-true=\"true\" test-yes=\"yes\" "+
				"test-false=\"false\" test-no=\"no\"/>");

			XmlNode tag = doc.SelectSingleNode("blah");
			XmlAttributeCollection attribs = tag.Attributes;
			Assert.IsTrue(TeStylesXmlAccessor.GetBoolAttribute(attribs, "test-true", "test1", "file.xml"));
			Assert.IsTrue(TeStylesXmlAccessor.GetBoolAttribute(attribs, "test-yes", "test2", "file.xml"));
			Assert.IsFalse(TeStylesXmlAccessor.GetBoolAttribute(attribs, "test-false", "test3", "file.xml"));
			Assert.IsFalse(TeStylesXmlAccessor.GetBoolAttribute(attribs, "test-no", "test4", "file.xml"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test GetBoolAttribute method when invalid data is passed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InstallationException))]
		public void GetBoolAttributeBrokenTest()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version='1.0' ?><blah test-fail=\"bogus\"/>");
			XmlNode tag = doc.SelectSingleNode("blah");
			XmlAttributeCollection attribs = tag.Attributes;
			TeStylesXmlAccessor.GetBoolAttribute(attribs, "test-fail", "test1", "file.xml");
		}
		#endregion

		#region GetHelpTopicForStyle tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeStylesXmlAccessor.GetHelpTopicForStyle"/> method with a factory
		/// style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetHelpTopicForStyle_Factory()
		{
			Assert.AreEqual(
				@"Redirect.htm#its:Using_Styles.chm::/Using_Styles/Styles_Grouped_by_Type/Chapters_and_Verses/Chapter_Number_example.htm",
				TeStylesXmlAccessor.GetHelpTopicForStyle("Chapter Number"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeStylesXmlAccessor.GetHelpTopicForStyle"/> method with a
		/// user-defined style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetHelpTopicForStyle_UserDefined()
		{
			Assert.AreEqual(
				@"Advanced_Tasks/Customizing_Styles/User-defined_style.htm",
				TeStylesXmlAccessor.GetHelpTopicForStyle("My Style"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeStylesXmlAccessor.GetHelpTopicForStyle"/> method with a
		/// factory style name but a direct link to a help topic instead of a folder in
		/// using styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetHelpTopicForStyle_DirectTopicLink()
		{
			Assert.AreEqual(
				@"Help_topic_does_not_exist.htm",
				TeStylesXmlAccessor.GetHelpTopicForStyle("Remark"));
		}
		#endregion

		#region CompatibleContext tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the CompatibleContext method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCompatibleContext_Basic()
		{
			Assert.IsTrue(TeStylesXmlAccessor.CompatibleContext(ContextValues.Note,
				ContextValues.Note));
			Assert.IsFalse(TeStylesXmlAccessor.CompatibleContext(ContextValues.Book,
				ContextValues.InternalMappable));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the CompatibleContext method when
		/// passed style is internalMappable and passed context is internal.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCompatibleContext_AllowInternalConversion()
		{
			Assert.IsTrue(TeStylesXmlAccessor.CompatibleContext(ContextValues.InternalMappable,
				ContextValues.Internal));
			Assert.IsTrue(TeStylesXmlAccessor.CompatibleContext(ContextValues.Internal,
				ContextValues.InternalMappable));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the CompatibleContext method when
		/// attempting to convert style to/from General.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCompatibleContext_AllowSpecificToGeneralConversion()
		{
			Assert.IsTrue(TeStylesXmlAccessor.CompatibleContext(ContextValues.Text,
				ContextValues.General));
			Assert.IsTrue(TeStylesXmlAccessor.CompatibleContext(ContextValues.IntroTitle,
				ContextValues.General));
			Assert.IsFalse(TeStylesXmlAccessor.CompatibleContext(ContextValues.General,
				ContextValues.Publication));
		}
		#endregion

		#region InterpretMeasurementAttribute tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InterpretMeasurementAttribute method for a zero value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InterpretMeasurementAttribute_zero()
		{
			Assert.AreEqual(0, StylesXmlAccessor.InterpretMeasurementAttribute("0", string.Empty, string.Empty, "styles.xml"));
			Assert.AreEqual(0, StylesXmlAccessor.InterpretMeasurementAttribute(string.Empty, string.Empty, string.Empty, "styles.xml"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InterpretMeasurementAttribute method for values in points.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InterpretMeasurementAttribute_points()
		{
			Assert.AreEqual(0, StylesXmlAccessor.InterpretMeasurementAttribute("0", string.Empty, string.Empty, "styles.xml"));
			Assert.AreEqual(5000, StylesXmlAccessor.InterpretMeasurementAttribute("5 pt", string.Empty, string.Empty, "styles.xml"));
			Assert.AreEqual(5678, StylesXmlAccessor.InterpretMeasurementAttribute("5.678 pt", string.Empty, string.Empty, "styles.xml"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InterpretMeasurementAttribute method for values in inches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InterpretMeasurementAttribute_inches()
		{
			Assert.AreEqual(5 * 72000, StylesXmlAccessor.InterpretMeasurementAttribute("5 in", string.Empty, string.Empty, "styles.xml"));
			Assert.AreEqual((int)(5.678 * 72000), StylesXmlAccessor.InterpretMeasurementAttribute("5.678 in", string.Empty, string.Empty, "styles.xml"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the InterpretMeasurementAttribute method when the default locale is something
		/// that uses commas for decimal separators.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InterpretMeasurementAttribute_otherCulture()
		{
			CultureInfo saveCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
			try
			{
				Assert.AreEqual((int)(9.5 * 72000), StylesXmlAccessor.InterpretMeasurementAttribute("9.5 in", string.Empty, string.Empty, "styles.xml"));
				Assert.AreEqual(5600, StylesXmlAccessor.InterpretMeasurementAttribute("5.6 pt", string.Empty, string.Empty, "styles.xml"));
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = saveCulture;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the InterpretMeasurementAttribute method when
		/// invalid data is passed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InstallationException))]
		public void InterpretMeasurementAttributeInvalidTest()
		{
			TeStylesXmlAccessor.InterpretMeasurementAttribute("12 bg", "test-fail", "bogus", "styles.xml");
		}
		#endregion

		#region GetDefaultStyleForContext tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a default character style for the context. Should always be
		/// default paragraph characters (or an unspecified style).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDefaultStyleForContext_WithCharStyle()
		{
			Assert.AreEqual(string.Empty,
				TeStylesXmlAccessor.GetDefaultStyleForContext(ContextValues.General, true));
		}
		#endregion
	}
	#endregion

	#region class TeStylesXmlAccessorTests_InMemoryCache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests that use an in-memory cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeStylesXmlAccessorTests_InMemoryCache: ScrInMemoryFdoTestBase
	{
		#region data members
		private IStStyle m_styleOrig;
		private IStStyle m_styleReplace;
		#endregion

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				m_styleOrig = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
				m_scr.StylesOC.Add(m_styleOrig);
				m_styleOrig.Name = "Cool style";
				m_styleOrig.Context = ContextValues.Intro;
				m_styleOrig.IsBuiltIn = true;

				m_styleReplace = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
				m_scr.StylesOC.Add(m_styleReplace);
				m_styleReplace.Name = "Cooler style";
				m_styleReplace.IsBuiltIn = true;
			});
		}

		public override void FixtureTeardown()
		{
			m_styleOrig = null;
			m_styleReplace = null;

			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test upgrading the styles to compatible contexts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestUpgradingToCompatibleStyles()
		{
			m_styleReplace.Context = ContextValues.Intro;

			// Create an xml style document with some styles
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
				"<Styles>" + Environment.NewLine +
				"<markup version=\"78A16A60-5644-48e8-9B77-A1F65A9EA253\"/>" + Environment.NewLine +
				"<replacements>" + Environment.NewLine +
				"<change old=\"Cool_style\" new=\"Cooler_style\"/>" + Environment.NewLine +
				"</replacements>" + Environment.NewLine +
				"</Styles>");

			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr,
				doc.SelectSingleNode("Styles"));

			acc.OriginalStyles.Add("Cool style", m_styleOrig);
			acc.UpdatedStyles.Add("Cooler style", m_styleReplace);
			acc.CallDeleteDeprecatedStylesAndDetermineReplacements();

			Assert.AreEqual(1, acc.StyleReplacements.Count);
			Assert.AreEqual("Cooler style", acc.StyleReplacements["Cool style"]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test upgrading the styles to incompatible contexts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InstallationException))]
		public void TestUpgradingToIncompatibleStyles()
		{
			m_styleReplace.Context = ContextValues.Text;

			// Create an xml style document with some styles
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
				"<Styles>" + Environment.NewLine +
				"<markup version=\"78A16A60-5644-48e8-9B77-A1F65A9EA253\"/>" + Environment.NewLine +
				"<replacements>" + Environment.NewLine +
				"<change old=\"Cool_style\" new=\"Cooler_style\"/>" + Environment.NewLine +
				"</replacements>" + Environment.NewLine +
				"</Styles>");

			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr,
				doc.SelectSingleNode("Styles"));

			acc.OriginalStyles.Add("Cool style", m_styleOrig);
			acc.UpdatedStyles.Add("Cooler style", m_styleReplace);
			acc.CallDeleteDeprecatedStylesAndDetermineReplacements();
		}

		#region GetType tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetType method when context is General and
		/// type is Paragraph. This should fail miserably.
		/// JohnT: moved here because this is now an instance method, so some of the checking
		/// can be virtual.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InstallationException))]
		public void TestGetType_InvalidContextForType()
		{
			// Create an xml style document with 1 style
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<tag userlevel=\"0\" id=\"Yaddah\" context=\"general\" type=\"paragraph\">" + Environment.NewLine +
				"</tag>");
			XmlAttributeCollection attributes = doc.SelectSingleNode("tag").Attributes;
			new DummyTeStylesXmlAccessor(m_scr).GetType(attributes, "test", ContextValues.General);
		}
		#endregion
	}
	#endregion

	#region class TeStylesXmlAccessorTests_WithCache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the <see cref="TeStylesXmlAccessor"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeStylesXmlAccessorTests_WithCache : ScrInMemoryFdoTestBase
	{
		private int m_wsEs;

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_wsEs = Cache.WritingSystemFactory.GetWsFromStr("es");
		}

		#region CreateScrStyles tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test CreateScrStyles method when valid data
		/// is passed and existing DB has an empty, uninitialized stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrStylesTest_NewDB()
		{
			// Start fresh
			Cache.LangProject.TranslatedScriptureOA = null;
			IScripture scr = Cache.LanguageProject.TranslatedScriptureOA =
				Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			Assert.AreEqual(0, scr.StylesOC.Count, "For this test, we want to start with an empty list");

			// Create an xml style document with some styles
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
				"<Styles>" + Environment.NewLine +
				"<markup version=\"78A16A60-5644-48e8-9B77-A1F65A9EA253\">" + Environment.NewLine +
				// Chapter Number (must have non-real context, structure, use, etc. to test the
				// creation of internal styles)
				"<tag guid=\"0f4ee4e0-6954-4e1d-8a9a-b36346e6fa33\" userlevel=\"0\" id=\"Chapter_Number\" structure=\"heading\" use=\"verse\" context=\"backTranslation\" type=\"paragraph\">" + Environment.NewLine +
				"<sfm>\\c</sfm>" + Environment.NewLine +
				"<usage wsId=\"en\">Start of chapter</usage>" + Environment.NewLine +
				"<font size=\"20 pt\" bold=\"false\" italic=\"false\" color=\"black\" superscript=\"false\" dropCap=\"2 lines\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Hebrew Title
				"<tag guid=\"d8a64396-aa10-4662-9348-5fa4f9e725e8\" userlevel=\"1\" id=\"Hebrew_Title\" structure=\"heading\" context=\"text\" type=\"paragraph\">" + Environment.NewLine +
				"<sfm>\\d</sfm>" + Environment.NewLine +
				"<usage wsId=\"es\">Título hebreo</usage>" + Environment.NewLine +
				"<font size=\"10 pt\" bold=\"false\" italic=\"false\" color=\"black\" superscript=\"false\" dropCap=\"false\"/>" + Environment.NewLine +
				"<paragraph basedOn=\"Paragraph\" next=\"Paragraph\" lineSpacing=\"12 pt\" alignment=\"center\" background=\"white\" indentLeft=\"0\" indentRight=\"0\" firstLine=\"8 pt\" spaceBefore=\"0\" spaceAfter=\"0\" border=\"top\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Paragraph (must have non-real context, structure, use, etc. to test the
				// creation of internal styles)
				"<tag guid=\"5c246da3-8e17-4e80-832a-e43576802dd0\" userlevel=\"0\" id=\"Paragraph\" structure=\"heading\" use=\"list\" context=\"general\" type=\"character\">" + Environment.NewLine +
				"<usage wsId=\"en\">Generic prose paragraph</usage>" + Environment.NewLine +
				"<usage wsId=\"es\">Un párrafo general</usage>" + Environment.NewLine +
				"<sfm>\\p</sfm>" + Environment.NewLine +
				"<font type=\"default\"/>" + Environment.NewLine +
				"<paragraph next=\"Gumby\" firstLine=\"8 pt\" basedOn=\"Mamba\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Don't specify Normal at all. Should get created in code anyway
				"</markup>" + Environment.NewLine +
				"</Styles>");

			//Run CreateScrStyles()
			DummyTeStylesXmlAccessor.CallCreateScrStyles(scr, doc.SelectSingleNode("Styles"));

			//Verify the styles
			ICmResource styleSheetResource = (from res in scr.ResourcesOC
											  where res.Name.Equals("TeStyles")
											  select res).FirstOrDefault();
			Debug.Assert(styleSheetResource != null, "Style sheet resource not in database.");
			Assert.AreEqual(new Guid("78A16A60-5644-48e8-9B77-A1F65A9EA253"),
				styleSheetResource.Version);
			Assert.AreEqual(DummyTeStylesXmlAccessor.ReservedStyleCount + 1, scr.StylesOC.Count,
				"Should have added all the reserved styles (including the 2 in the test XML), plus Hebrew Title.");
			foreach (IStStyle style in scr.StylesOC)
			{
				switch(style.Name)
				{
					case ScrStyleNames.ChapterNumber:
					{
						Assert.AreEqual(0, style.UserLevel);
						Assert.AreEqual(FunctionValues.Chapter, style.Function);
						Assert.AreEqual(StyleType.kstCharacter, style.Type);
						ITsTextProps tts = style.Rules;
						int nVar;
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptItalic,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptBold,
							out nVar));
						Assert.AreEqual(20000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFontSize,
							out nVar));
						Assert.AreEqual(0,
							tts.GetIntPropValues((int)FwTextPropType.ktptForeColor,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptSuperscript,
							out nVar));
						Assert.AreEqual("Start of chapter",
							style.Usage.get_String(m_wsEn).Text);
						break;
					}
					case "Hebrew Title":
					{
						Assert.AreEqual(1, style.UserLevel);
						Assert.AreEqual(ContextValues.Text, style.Context);
						Assert.AreEqual(StructureValues.Heading, style.Structure);
						Assert.AreEqual(StyleType.kstParagraph, style.Type);
						ITsTextProps tts = style.Rules;
						int nVar;
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptItalic,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptBold,
							out nVar));
						Assert.AreEqual(10000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFontSize,
							out nVar));
						Assert.AreEqual(0,
							tts.GetIntPropValues((int)FwTextPropType.ktptForeColor,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptSuperscript,
							out nVar));
						Assert.AreEqual((int)FwTextAlign.ktalCenter,
							tts.GetIntPropValues((int)FwTextPropType.ktptAlign,
							out nVar));
						Assert.AreEqual(500,
							tts.GetIntPropValues((int)FwTextPropType.ktptBorderTop,
							out nVar));
						Assert.AreEqual(12000,
							tts.GetIntPropValues((int)FwTextPropType.ktptLineHeight,
							out nVar));
						Assert.AreEqual("Paragraph", style.BasedOnRA.Name);
						Assert.AreEqual("Paragraph", style.NextRA.Name);
						Assert.AreEqual("Ti\u0301tulo hebreo",
							style.Usage.get_String(m_wsEs).Text);
						break;
					}
					case ScrStyleNames.NormalParagraph:
					{
						Assert.AreEqual(0, style.UserLevel);
						Assert.AreEqual(ContextValues.Text, style.Context);
						Assert.AreEqual(StructureValues.Body, style.Structure);
						Assert.AreEqual(FunctionValues.Prose, style.Function);
						Assert.AreEqual(StyleType.kstParagraph, style.Type);
						ITsTextProps tts = style.Rules;
						int nVar;
						Assert.AreEqual(8000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFirstIndent,
							out nVar));
						Assert.AreEqual("Normal", style.BasedOnRA.Name);
						Assert.AreEqual("Paragraph", style.NextRA.Name);
						Assert.AreEqual("Generic prose paragraph",
							style.Usage.get_String(m_wsEn).Text);
						Assert.AreEqual("Un pa\u0301rrafo general",
							style.Usage.get_String(m_wsEs).Text);
						break;
					}
					case ScrStyleNames.Normal:
					{
						Assert.AreEqual(0, style.UserLevel);
						Assert.AreEqual(ContextValues.Internal, style.Context);
						Assert.AreEqual(StyleType.kstParagraph, style.Type);
						Assert.IsNull(style.BasedOnRA);
						Assert.AreEqual("Paragraph", style.NextRA.Name);
						break;
					}
					case ScrStyleNames.SectionHead:
					case ScrStyleNames.IntroParagraph:
					case ScrStyleNames.IntroSectionHead:
					case ScrStyleNames.Remark:
					case ScrStyleNames.MainBookTitle:
					case ScrStyleNames.NormalFootnoteParagraph:
					case ScrStyleNames.CrossRefFootnoteParagraph:
					case ScrStyleNames.Figure:
					case ScrStyleNames.Header:
						Assert.AreEqual(StyleType.kstParagraph, style.Type, style.Name + " should be reserved Paragraph style");
						break;
					case ScrStyleNames.CanonicalRef:
					case ScrStyleNames.FootnoteMarker:
					case ScrStyleNames.FootnoteTargetRef:
					case ScrStyleNames.UntranslatedWord:
					case ScrStyleNames.VerseNumber:
					case ScrStyleNames.NotationTag:
						Assert.AreEqual(StyleType.kstCharacter, style.Type);
						break;
					default:
						Assert.Fail("Got an unexpected style: " + style.Name);
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the CreateScrStyles method when valid data
		/// is passed to update to a different stylesheet version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrStylesTest_UpdateDB()
		{
			// PART 1: Setup a new Scripture with a simple stylesheet
			Cache.LangProject.TranslatedScriptureOA = null;
			IScripture scr = Cache.LanguageProject.TranslatedScriptureOA =
				Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			Assert.AreEqual(0, scr.StylesOC.Count, "For this test, we want to start with an empty list");

			// Create an xml style document with some styles
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
				"<Styles>" + Environment.NewLine +
				"<markup version=\"2D065FD9-0F30-4104-8246-52F7103AC78F\">" + Environment.NewLine +
				// Chapter Number
				"<tag guid=\"0f4ee4e0-6954-4e1d-8a9a-b36346e6fa33\" userlevel=\"0\" id=\"Chapter_Number\" structure=\"body\" use=\"chapter\" context=\"text\" type=\"character\">" + Environment.NewLine +
				"<sfm>\\c</sfm>" + Environment.NewLine +
				"<usage wsId=\"en\">Start of chapter</usage>" + Environment.NewLine +
				"<font type=\"heading\" size=\"20 pt\" bold=\"false\" italic=\"false\" color=\"black\" superscript=\"false\" dropCap=\"2 lines\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Hebrew Title
				"<tag guid=\"d8a64396-aa10-4662-9348-5fa4f9e725e8\" userlevel=\"0\" id=\"Hebrew_Title\" structure=\"heading\" context=\"text\" type=\"paragraph\">" + Environment.NewLine +
				"<sfm>\\d</sfm>" + Environment.NewLine +
				"<usage wsId=\"es\">Título hebreo</usage>" + Environment.NewLine +
				"<font size=\"10 pt\" bold=\"false\" italic=\"false\" color=\"black\" superscript=\"false\" dropCap=\"false\"/>" + Environment.NewLine +
				"<paragraph basedOn=\"Paragraph\" next=\"Paragraph\" alignment=\"center\" background=\"white\" indentLeft=\"0\" indentRight=\"0\" firstLine=\"8 pt\" spaceBefore=\"0\" spaceAfter=\"0\" border=\"top\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Unused Replaced Style
				"<tag guid=\"5c246da3-8e17-4e80-832a-e43576802dd0\" userlevel=\"2\" id=\"Unused_Replaced_Style\" context=\"text\" type=\"paragraph\">" + Environment.NewLine +
				"<usage wsId=\"en\">This unused style shouldn't show up in replacement list</usage>" + Environment.NewLine +
				"<font/>" + Environment.NewLine +
				"<paragraph basedOn=\"Paragraph\" next=\"Paragraph\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Unused Deleted Style
				"<tag guid=\"1808b50f-5ad7-43c1-bdbf-9fc1fe3c1d5c\" userlevel=\"1\" id=\"Unused_Deleted_Style\" context=\"text\" type=\"paragraph\">" + Environment.NewLine +
				"<usage wsId=\"en\">This unused style shouldn't show up in deleted list</usage>" + Environment.NewLine +
				"<font/>" + Environment.NewLine +
				"<paragraph basedOn=\"Paragraph\" next=\"Paragraph\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Paragraph
				"<tag guid=\"45193615-3eac-4c57-980a-2bba7fe4dd08\" userlevel=\"0\" id=\"Paragraph\" structure=\"body\" use=\"prose\" context=\"text\" type=\"paragraph\">" + Environment.NewLine +
				"<usage wsId=\"en\">Generic prose paragraph</usage>" + Environment.NewLine +
				"<usage wsId=\"es\">Un párrafo general</usage>" + Environment.NewLine +
				"<sfm>\\p</sfm>" + Environment.NewLine +
				"<font size=\"10 pt\"/>" + Environment.NewLine +
				"<paragraph next=\"Paragraph\" firstLine=\"8 pt\" basedOn=\"Normal\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Delete Me
				"<tag guid=\"12640d9e-6cea-492c-a97b-daa83a6b75d8\" userlevel=\"0\" id=\"Delete_Me\" context=\"general\" type=\"character\">" + Environment.NewLine +
				"<usage wsId=\"en\">Junk</usage>" + Environment.NewLine +
				"<font/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				// Normal
				"<tag guid=\"914dcc30-e2ca-4042-ae91-a5d30e3ca13e\" userlevel=\"0\" id=\"Normal\" context=\"internal\" type=\"paragraph\">" + Environment.NewLine +
				"<font size=\"10 pt\" bold=\"false\" italic=\"false\" color=\"black\" superscript=\"false\"/>" + Environment.NewLine +
				"<paragraph next=\"Paragraph\" alignment=\"left\" background=\"white\" indentLeft=\"0\" indentRight=\"0\" firstLine=\"0 pt\" spaceBefore=\"0\" spaceAfter=\"0\"/>" + Environment.NewLine +
				"</tag>" + Environment.NewLine +
				"</markup>" + Environment.NewLine +
				"</Styles>");

			// Run CreateScrStyles() the first time
			XmlNode teStyles = doc.SelectSingleNode("Styles");
			DummyTeStylesXmlAccessor.CallCreateScrStyles(scr, teStyles);

			// PART 2: Update the xml style document with 2 new styles, 1 updated style, and 2 deleted styles

			// Select a node in the existing stylesheet to change (and to insert a new style before)
			XmlNode markup = StylesXmlAccessor.GetMarkupNode(teStyles);

			// Change the version number
			XmlNode version = markup.Attributes.GetNamedItem("version");
			version.Value = "60250A17-9B56-466b-8E7B-E27A0EB03D3E";

			// Add a new character style
			XmlNode styleNode = doc.CreateNode(XmlNodeType.Element, "tag", null);
			styleNode.Attributes.Append(NewAttribute(doc, "guid", "ac9282df-3426-43c6-a2c7-0fbdfb323a82"));
			styleNode.Attributes.Append(NewAttribute(doc, "userlevel", "2"));
			styleNode.Attributes.Append(NewAttribute(doc, "id", "MyNewCharStyle"));
			styleNode.Attributes.Append(NewAttribute(doc, "structure", "body"));
			styleNode.Attributes.Append(NewAttribute(doc, "use", "prose"));
			styleNode.Attributes.Append(NewAttribute(doc, "context", "text"));
			styleNode.Attributes.Append(NewAttribute(doc, "type", "character"));
			styleNode.InnerXml = "<usage wsId=\"en\">Use this for fun</usage>" + Environment.NewLine +
				"<font size=\"23 pt\" bold=\"true\" italic=\"true\" color=\"green\" superscript=\"false\"/>";
			markup.ReplaceChild(styleNode, markup.SelectSingleNode("tag[@id='Delete_Me']"));

			// Replace a paragraph style with a new one
			styleNode = doc.CreateNode(XmlNodeType.Element, "tag", null);
			styleNode.Attributes.Append(NewAttribute(doc, "guid", "797d2f7d-576f-49ee-b7fd-ab5bfcf251a3"));
			styleNode.Attributes.Append(NewAttribute(doc, "userlevel", "1"));
			styleNode.Attributes.Append(NewAttribute(doc, "id", "MyNewParaStyle"));
			styleNode.Attributes.Append(NewAttribute(doc, "context", "text"));
			styleNode.Attributes.Append(NewAttribute(doc, "type", "paragraph"));
			styleNode.InnerXml = "<usage wsId=\"en\">Replaces Hebrew Title</usage>" + Environment.NewLine +
				"<font size=\"13 pt\" bold=\"false\" italic=\"true\" superscript=\"false\"/>" + Environment.NewLine +
				"<paragraph next=\"Paragraph\" basedOn=\"Paragraph\" alignment=\"center\"/>";
			markup.ReplaceChild(styleNode, markup.SelectSingleNode("tag[@id='Hebrew_Title']"));

			// Modify the Paragraph style
			styleNode = markup.SelectSingleNode("tag[@id='Paragraph']");
			styleNode.SelectSingleNode("font").Attributes.GetNamedItem("size").Value = "96 pt";
			styleNode.SelectSingleNode("paragraph").Attributes.GetNamedItem("next").Value = "MyNewParaStyle";

			// Remove the two unused styles
			styleNode = markup.SelectSingleNode("tag[@id='Unused_Replaced_Style']");
			markup.RemoveChild(styleNode);
			styleNode = markup.SelectSingleNode("tag[@id='Unused_Deleted_Style']");
			markup.RemoveChild(styleNode);

			XmlNode replacements = doc.CreateNode(XmlNodeType.Element, "replacements", null);
			replacements.InnerXml = "<change old=\"Hebrew_Title\" new=\"MyNewParaStyle\"/>" +
				"<change old=\"Unused_Replaced_Style\" new=\"Paragraph\"/>";
			teStyles.AppendChild(replacements);


			// Run CreateScrStyles() the second time
			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(scr, teStyles);
			IThreadedProgress progressDlg = new DummyProgressDlg();
			acc.InitLoading(progressDlg, scr, teStyles);

			//Verify the styles
			ICmResource styleSheetResource = (from res in scr.ResourcesOC
											  where res.Name.Equals("TeStyles")
											  select res).FirstOrDefault();
			Debug.Assert(styleSheetResource != null, "Style sheet resource not in database.");
			Assert.AreEqual(new Guid("60250A17-9B56-466b-8E7B-E27A0EB03D3E"),
				styleSheetResource.Version);
			Assert.AreEqual(DummyTeStylesXmlAccessor.ReservedStyleCount + 2, scr.StylesOC.Count,
				"Should have added all the reserved styles (including the 3 in the test XML), plus 4 additional ones in the original stylesheet (but then they all get deleted, so don't count them), plus 2 new ones in the updated stylesheet.");
			ITsTextProps tts;
			int nVar;
			foreach (IStStyle style in scr.StylesOC)
			{
				tts = style.Rules;
				switch(style.Name)
				{
					case "MyNewCharStyle":
						Assert.AreEqual(2, style.UserLevel);
						Assert.AreEqual(FunctionValues.Prose, style.Function);
						Assert.AreEqual(StyleType.kstCharacter, style.Type);
						Assert.AreEqual((int)FwTextToggleVal.kttvInvert,
							tts.GetIntPropValues((int)FwTextPropType.ktptItalic,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvInvert,
							tts.GetIntPropValues((int)FwTextPropType.ktptBold,
							out nVar));
						Assert.AreEqual(23000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFontSize,
							out nVar));
						Assert.AreEqual(0x8000,
							tts.GetIntPropValues((int)FwTextPropType.ktptForeColor,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptSuperscript,
							out nVar));
						Assert.AreEqual("Use this for fun",
							style.Usage.get_String(m_wsEn).Text);
						break;
					case "MyNewParaStyle":
						Assert.AreEqual(1, style.UserLevel);
						Assert.AreEqual(FunctionValues.Prose, style.Function);
						Assert.AreEqual(StyleType.kstParagraph, style.Type);
						Assert.AreEqual((int)FwTextToggleVal.kttvInvert,
							tts.GetIntPropValues((int)FwTextPropType.ktptItalic,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptBold,
							out nVar));
						Assert.AreEqual(13000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFontSize,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptSuperscript,
							out nVar));
						Assert.AreEqual((int)FwTextAlign.ktalCenter,
							tts.GetIntPropValues((int)FwTextPropType.ktptAlign,
							out nVar));
						Assert.AreEqual("Paragraph", style.BasedOnRA.Name);
						Assert.AreEqual("Paragraph", style.NextRA.Name);
						Assert.AreEqual("Replaces Hebrew Title",
							style.Usage.get_String(m_wsEn).Text);
						break;
					case "Chapter Number":
						Assert.AreEqual(0, style.UserLevel);
						Assert.AreEqual(FunctionValues.Chapter, style.Function);
						Assert.AreEqual(StyleType.kstCharacter, style.Type);
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptItalic,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptBold,
							out nVar));
						Assert.AreEqual(20000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFontSize,
							out nVar));
						Assert.AreEqual(0,
							tts.GetIntPropValues((int)FwTextPropType.ktptForeColor,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptSuperscript,
							out nVar));
						Assert.AreEqual("Start of chapter",
							style.Usage.get_String(m_wsEn).Text);
						break;
					case "Paragraph":
						Assert.AreEqual(0, style.UserLevel);
						Assert.AreEqual(ContextValues.Text, style.Context);
						Assert.AreEqual(StructureValues.Body, style.Structure);
						Assert.AreEqual(FunctionValues.Prose, style.Function);
						Assert.AreEqual(StyleType.kstParagraph, style.Type);
						Assert.AreEqual(96000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFontSize,
							out nVar));
						Assert.AreEqual(8000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFirstIndent,
							out nVar));
						Assert.AreEqual("Normal", style.BasedOnRA.Name);
						Assert.AreEqual("Paragraph", style.NextRA.Name,
							"This value is fixed. Can't be changed by modifying stylesheet.");
						Assert.AreEqual("Generic prose paragraph",
							style.Usage.get_String(m_wsEn).Text);
						Assert.AreEqual("Un pa\u0301rrafo general",
							style.Usage.get_String(m_wsEs).Text);
						break;
					case "Normal":
						Assert.AreEqual(0, style.UserLevel);
						Assert.AreEqual(ContextValues.Internal, style.Context);
						Assert.AreEqual(StyleType.kstParagraph, style.Type);
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptItalic,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptBold,
							out nVar));
						Assert.AreEqual(10000,
							tts.GetIntPropValues((int)FwTextPropType.ktptFontSize,
							out nVar));
						Assert.AreEqual(0,
							tts.GetIntPropValues((int)FwTextPropType.ktptForeColor,
							out nVar));
						Assert.AreEqual((int)FwTextToggleVal.kttvOff,
							tts.GetIntPropValues((int)FwTextPropType.ktptSuperscript,
							out nVar));
						Assert.AreEqual((int)FwTextAlign.ktalLeading,
							tts.GetIntPropValues((int)FwTextPropType.ktptAlign,
							out nVar));
						Assert.IsNull(style.BasedOnRA);
						Assert.AreEqual("Paragraph", style.NextRA.Name);
						break;
					case ScrStyleNames.SectionHead:
					case ScrStyleNames.IntroParagraph:
					case ScrStyleNames.IntroSectionHead:
					case ScrStyleNames.Remark:
					case ScrStyleNames.MainBookTitle:
					case ScrStyleNames.NormalFootnoteParagraph:
					case ScrStyleNames.CrossRefFootnoteParagraph:
					case ScrStyleNames.Figure:
					case ScrStyleNames.Header:
						Assert.AreEqual(StyleType.kstParagraph, style.Type);
						break;
					case ScrStyleNames.CanonicalRef:
					case ScrStyleNames.FootnoteMarker:
					case ScrStyleNames.FootnoteTargetRef:
					case ScrStyleNames.UntranslatedWord:
					case ScrStyleNames.VerseNumber:
					case ScrStyleNames.NotationTag:
						Assert.AreEqual(StyleType.kstCharacter, style.Type);
						break;
					default:
						Assert.Fail("Got an unexpected style: " + style.Name);
						break;
				}
			}
			Assert.AreEqual(2, acc.StyleReplacements.Count);
			Assert.AreEqual("MyNewParaStyle", acc.StyleReplacements["Hebrew Title"]);
			Assert.AreEqual(String.Empty, acc.StyleReplacements["Delete Me"]);
		}

		#region EnsureCompatibleFactoryStyle tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test EnsureCompatibleFactoryStyle method when
		/// passed style is already kosher.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestEnsureCompatibleFactoryStyle_Easy()
		{
			IStStyle paraStyle = m_scr.FindStyle("Paragraph");

			int hvoParaStyle = paraStyle.Hvo;
			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr, null);
			IStStyle style = acc.EnsureCompatibleFactoryStyle(paraStyle,
				StyleType.kstParagraph, ContextValues.Text, StructureValues.Body,
				FunctionValues.Prose);
			Assert.AreEqual(hvoParaStyle, style.Hvo);
			Assert.AreEqual(ContextValues.Text, (ContextValues)style.Context);
			Assert.AreEqual(StructureValues.Body, (StructureValues)style.Structure);
			Assert.AreEqual(FunctionValues.Prose, (FunctionValues)style.Function);
			Assert.AreEqual(0, acc.StyleReplacements.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the EnsureCompatibleFactoryStyle method when
		/// passed style is an existing factory style with incompatible
		/// context/structure/funtion.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
#if DEBUG
		[ExpectedException(typeof(InstallationException),
			ExpectedMessage="Cannot redefine factory style Paragraph to have a different Type, Context, Structure, or Function in TeStyles.xml")]
#else
		[ExpectedException(typeof(InstallationException))]
#endif
		public void TestEnsureCompatibleFactoryStyle_InvalidContextStructureFunction()
		{
			IStStyle paraStyle = m_scr.FindStyle("Paragraph");

			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr, null);
			acc.EnsureCompatibleFactoryStyle(paraStyle,
				StyleType.kstParagraph, ContextValues.InternalMappable,
				StructureValues.Heading, FunctionValues.Verse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test EnsureCompatibleFactoryStyle method when
		/// passed style is an existing factory style with incompatible style type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
#if DEBUG
		[ExpectedException(typeof(InstallationException),
		   ExpectedMessage = "Cannot redefine factory style Paragraph to have a different Type, Context, Structure, or Function in TeStyles.xml")]
#else
		[ExpectedException(typeof(InstallationException))]
#endif
		public void TestEnsureCompatibleFactoryStyle_InvalidStyleType()
		{
			IStStyle paraStyle = m_scr.FindStyle("Paragraph");

			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr, null);
			acc.EnsureCompatibleFactoryStyle(paraStyle, StyleType.kstCharacter,
				ContextValues.Text, StructureValues.Body, FunctionValues.Prose);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the EnsureCompatibleFactoryStyle method when
		/// passed style is a factory style changing to an internal context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestEnsureCompatibleFactoryStyle_FactoryStyleToInternal()
		{
			IFdoOwningCollection<IStStyle> styles = m_scr.StylesOC;
			int countOfStylesOrig = styles.Count;
			IStStyle origStyle = m_scr.FindStyle("Normal");
			origStyle.Context = ContextValues.Text;

			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr, null);
			IStStyle newFactoryStyle = acc.EnsureCompatibleFactoryStyle(origStyle,
				StyleType.kstParagraph, ContextValues.Internal, StructureValues.Undefined,
				FunctionValues.Prose);
			Assert.AreEqual(origStyle, newFactoryStyle);
			Assert.AreEqual("Normal", origStyle.Name);
			Assert.AreEqual(countOfStylesOrig, styles.Count);
			Assert.IsTrue(origStyle.IsBuiltIn);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test EnsureCompatibleFactoryStyle method when
		/// passed style is a user-defined style with incompatible context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestEnsureCompatibleFactoryStyle_RedefineUserStyle()
		{
			IFdoOwningCollection<IStStyle> styles = m_scr.StylesOC;
			int countOfStylesOrig = styles.Count;
			// For this test, make Paragraph be a user-defined style
			IStStyle userDefStyle = m_scr.FindStyle("Paragraph");
			userDefStyle.IsBuiltIn = false;

			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr, null);
			acc.EnsureCompatibleFactoryStyle(userDefStyle,
				StyleType.kstParagraph, ContextValues.Title, StructureValues.Undefined,
				FunctionValues.Prose);
			Assert.IsNull(m_scr.FindStyle("Paragraph"),
				"New factory style should be created but not yet named. Old Paragraph style should be renamed.");
			Assert.AreEqual(countOfStylesOrig + 1, m_scr.StylesOC.Count, "A new style should have been created");
			Assert.AreEqual("Paragraph_User", userDefStyle.Name);
			Assert.AreEqual(ContextValues.Text, (ContextValues)userDefStyle.Context);
			Assert.AreEqual(StructureValues.Body, (StructureValues)userDefStyle.Structure);
			Assert.AreEqual(FunctionValues.Prose, (FunctionValues)userDefStyle.Function);
			Assert.IsFalse(userDefStyle.IsBuiltIn);

			Assert.AreEqual(1, acc.StyleReplacements.Count);
			Assert.AreEqual("Paragraph_User", acc.StyleReplacements["Paragraph"]);
		}
		#endregion

		#region ReplaceFormerStyles test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For all practical purposes, this is really a test of the C++ FwDbMergeStyles class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Can't do this until we have new Undo stuff. This test will be slow anyway. Maybe move into acceptance tests.")]
		public void TestReplaceFormerStyles()
		{
			DummyTeStylesXmlAccessor acc = new DummyTeStylesXmlAccessor(m_scr, null);

			string headingStyleNameToDelete = null;
			string bodyStyleNameToDelete = null;
			foreach (IStStyle style in m_scr.StylesOC)
			{
				if (style.Context == ContextValues.Text)
				{
					if (style.Structure == StructureValues.Heading &&
						style.Type == StyleType.kstParagraph &&
						style.Name != ScrStyleNames.SectionHead)
						headingStyleNameToDelete = style.Name;
					else if (style.Structure == StructureValues.Body &&
						style.Type == StyleType.kstParagraph &&
						style.Name != ScrStyleNames.NormalParagraph)
						bodyStyleNameToDelete = style.Name;
				}
				if (headingStyleNameToDelete != null && bodyStyleNameToDelete != null)
					break;
			}
			Assert.IsNotNull(headingStyleNameToDelete, "Couldn't find a real style for test -- need to create a bogus heading para style");
			Assert.IsNotNull(bodyStyleNameToDelete, "Couldn't find a real style for test -- need to create a bogus body para style");
			IStStyle styDeleteThisTitleStyle = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			m_scr.StylesOC.Add(styDeleteThisTitleStyle);
			styDeleteThisTitleStyle.Name = "Auf wiedersehen";
			styDeleteThisTitleStyle.Context = ContextValues.Title;
			styDeleteThisTitleStyle.Type = StyleType.kstParagraph;

			// Create a few paragraphs using some fake styles
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection sect = book.SectionsOS[0];
			IStTxtPara hPara0 = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
			IStTxtPara cPara0 = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			IStTxtPara tPara0 = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
			book = m_scr.ScriptureBooksOS[1];
			IFdoOwningSequence<IScrSection> sections = book.SectionsOS;
			sect = sections[sections.Count - 1];
			IStTxtPara hPara1 = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
			IFdoOwningSequence<IStPara> paras = sect.ContentOA.ParagraphsOS;
			IStTxtPara cPara1 = (IStTxtPara)paras[paras.Count - 1];
			IStTxtPara tPara1 = (IStTxtPara)book.TitleOA.ParagraphsOS[0];

			// Change the paragraph style to something bogus
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "To Be Replaced");
			ITsTextProps props = bldr.GetTextProps();
			hPara0.StyleRules = props;
			cPara0.StyleRules = props;
			tPara0.StyleRules = props;
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, headingStyleNameToDelete);
			hPara1.StyleRules = bldr.GetTextProps();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, bodyStyleNameToDelete);
			cPara1.StyleRules = bldr.GetTextProps();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styDeleteThisTitleStyle.Name);
			tPara1.StyleRules = bldr.GetTextProps();

			// Add some runs using a bogus character style
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Wacky Char Style");
			props = bldr.GetTextProps();

			ITsStrBldr strBldr = hPara0.Contents.GetBldr();
			strBldr.Replace(0, 0, "glub", props);
			hPara0.Contents = strBldr.GetString();

			strBldr = cPara0.Contents.GetBldr();
			strBldr.Replace(3, 3, "glub", props);
			cPara0.Contents = strBldr.GetString();

			strBldr = tPara0.Contents.GetBldr();
			strBldr.Replace(3, 3, "glub", props);
			tPara0.Contents = strBldr.GetString();

			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Be Gone");
			props = bldr.GetTextProps();

			strBldr = hPara1.Contents.GetBldr();
			strBldr.Replace(3, 3, "glub", props);
			hPara1.Contents = strBldr.GetString();

			strBldr = cPara1.Contents.GetBldr();
			strBldr.Replace(3, 3, "glub", props);
			cPara1.Contents = strBldr.GetString();

			// Simulate a paragraph style replacement
			acc.StyleReplacements["To Be Replaced"] = "With This";
			acc.StyleReplacements["Wacky Char Style"] = "Sane Char Style";
			//acc.DeletedStyles.Add(headingStyleNameToDelete);
			//acc.DeletedStyles.Add(bodyStyleNameToDelete);
			//acc.DeletedStyles.Add(styDeleteThisTitleStyle.Name);
			//acc.DeletedStyles.Add("Be Gone");
			acc.CallReplaceFormerStyles();

			// Check replaced paragraph style
			string styleName = hPara0.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual("With This", styleName);
			styleName = cPara0.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual("With This", styleName);
			styleName = tPara0.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual("With This", styleName);
			// Check deleted paragraph styles
			styleName = hPara1.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.SectionHead, styleName);
			styleName = cPara1.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.NormalParagraph, styleName);
			styleName = tPara1.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, styleName);

			//Check replaced character style
			props = hPara0.Contents.get_PropertiesAt(4);
			styleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual("Sane Char Style", styleName);
			props = cPara0.Contents.get_PropertiesAt(4);
			styleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual("Sane Char Style", styleName);
			props = tPara0.Contents.get_PropertiesAt(4);
			styleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual("Sane Char Style", styleName);
			// Check deleted character styles
			props = hPara1.Contents.get_PropertiesAt(4);
			styleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(null, styleName);
			props = cPara1.Contents.get_PropertiesAt(4);
			styleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(null, styleName);
			props = tPara1.Contents.get_PropertiesAt(4);
			styleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(null, styleName);
		}
		#endregion
		#endregion

		#region helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an attribute
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="name"></param>
		/// <param name="attrValue"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private XmlAttribute NewAttribute(XmlDocument doc, string name, string attrValue)
		{
			XmlAttribute a = doc.CreateAttribute(name);
			a.Value = attrValue;
			return a;
		}

		#endregion
	}
	#endregion
}
