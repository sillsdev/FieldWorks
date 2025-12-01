// Copyright (c) 2003-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace ParatextImport.ImportTests
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// ImportStyleProxyTests class to test ImportStyleProxy
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportStyleProxyTests : ScrInMemoryLcmTestBase
	{
		#region Member variables
		private LcmStyleSheet m_styleSheet;
		#endregion

		#region Setup/Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up Undo action and initialize the stylesheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_styleSheet = new LcmStyleSheet();
			// ReSharper disable once UnusedVariable - Force load of styles
			var scr = Cache.LangProject.TranslatedScriptureOA;
			Assert.That(Cache.LangProject.StylesOC.Count > 0, Is.True);
			m_styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_styleSheet = null;
			base.TestTearDown();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests of the <see cref="ImportStyleProxy"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BasicTest()
		{
			int cStylesOrig = m_styleSheet.CStyles;
			Assert.That(cStylesOrig > 10, Is.True);
			Assert.That(m_styleSheet.GetStyleRgch(0, "Section Head"), Is.Not.Null);
			Assert.That(m_styleSheet.GetStyleRgch(0, "Verse Number"), Is.Not.Null);

			// create four new proxies;  verify that they properly determine if they are
			//  mapped to the TE default stylesheet
			int wsVern = Cache.DefaultVernWs;
			int wsAnal = Cache.DefaultAnalWs;
			ImportStyleProxy proxy1 = new ImportStyleProxy("Section Head",
				StyleType.kstParagraph, wsVern, ContextValues.Text, m_styleSheet);
			Assert.That(proxy1.IsUnknownMapping, Is.False, "Section Head style should exist in DB");

			ImportStyleProxy proxy2 = new ImportStyleProxy("Verse Number",
				StyleType.kstCharacter, wsVern, ContextValues.Text, m_styleSheet);
			Assert.That(proxy2.IsUnknownMapping, Is.False, "Verse Number style should exist in DB");

			string proxy3Name = "Tom Bogle";
			ImportStyleProxy proxy3 = new ImportStyleProxy(proxy3Name,
				StyleType.kstParagraph, wsVern, m_styleSheet); //defaults to Text context
			Assert.That(proxy3.IsUnknownMapping, Is.True, "Tom Bogle style shouldn't exist in DB");

			string proxy4Name = "Todd Jones";
			ImportStyleProxy proxy4 = new ImportStyleProxy(proxy4Name,
				StyleType.kstCharacter, wsVern, m_styleSheet); //defaults to Text context
			Assert.That(proxy4.IsUnknownMapping, Is.True, "Todd Jones style shouldn't exist in DB");

			// verify basic proxy info - name, context, structure, function, styletype, endmarker
			Assert.That(proxy1.StyleId, Is.EqualTo("Section Head"));
			Assert.That(proxy1.Context, Is.EqualTo(ContextValues.Text));
			Assert.That(proxy1.Structure, Is.EqualTo(StructureValues.Heading));
			Assert.That(proxy1.StyleType, Is.EqualTo(StyleType.kstParagraph));
			Assert.That(proxy1.EndMarker, Is.Null);

			Assert.That(proxy2.Context, Is.EqualTo(ContextValues.Text));
			Assert.That(proxy2.Structure, Is.EqualTo(StructureValues.Body));
			Assert.That(proxy2.Function, Is.EqualTo(FunctionValues.Verse));
			Assert.That(proxy2.StyleType, Is.EqualTo(StyleType.kstCharacter));
			Assert.That(proxy2.EndMarker, Is.Null);

			Assert.That(proxy3.Context, Is.EqualTo(ContextValues.Text));
			// getting the text props will cause the style to be created in the database
			ITsTextProps props = proxy3.TsTextProps;
			IStStyle dbStyle = m_styleSheet.FindStyle(proxy3Name);
			Assert.That(dbStyle.BasedOnRA.Name, Is.EqualTo(ScrStyleNames.NormalParagraph));
			Assert.That(proxy3.StyleType, Is.EqualTo(StyleType.kstParagraph));
			Assert.That(proxy3.EndMarker, Is.Null);

			Assert.That(proxy4.Context, Is.EqualTo(ContextValues.Text));
			props = proxy4.TsTextProps;
			dbStyle = m_styleSheet.FindStyle(proxy4Name);
			Assert.That(dbStyle.BasedOnRA, Is.Null);
			Assert.That(proxy4.StyleType, Is.EqualTo(StyleType.kstCharacter));
			Assert.That(proxy4.EndMarker, Is.Null);

			// use SetFormat to add formatting props to unmapped proxy3
			ITsPropsBldr tsPropertiesBldr = TsStringUtils.MakePropsBldr();
			tsPropertiesBldr.SetIntPropValues((int)FwTextPropType.ktptItalic,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvForceOn);
			ITsTextProps formatProps3 = tsPropertiesBldr.GetTextProps();
			proxy3.SetFormat(formatProps3, false);

			// Test retrieval of ParaProps and TsTextProps
			Assert.That(proxy1.TsTextProps, Is.Not.Null);
			Assert.That(proxy2.TsTextProps, Is.Not.Null);
			// Besides returning the props, retrieving ParaProps or TsTextProps adds a
			// previously unmapped style to the stylesheet, so that proxy becomes mapped
			// Next two calls force creation of new styles
			Assert.That(proxy3.TsTextProps, Is.Not.Null); // has benefit of SetFormat
			Assert.That(proxy3.IsUnknownMapping, Is.False, "Tom Bogle style should be created when getting TsTextProps");
			Assert.That(proxy4.IsUnknownMapping, Is.False, "Todd Jones style should be created when getting ParaProps");
			// verify that two new styles were added to the style sheet
			Assert.That(m_styleSheet.CStyles, Is.EqualTo(cStylesOrig + 2));

			// verify that the added styles have the appropriate context, etc
			IStStyle style = m_styleSheet.FindStyle("Tom Bogle");
			Assert.That((ContextValues)style.Context, Is.EqualTo(ContextValues.Text));
			Assert.That((StructureValues)style.Structure, Is.EqualTo(StructureValues.Body));
			Assert.That((FunctionValues)style.Function, Is.EqualTo(FunctionValues.Prose));

			// Test the styletype override from stylesheet
			// We will attempt to construct a paragraph style proxy,
			//  but since in the stylesheet Chapter Number is a character style,
			//  character will override
			ImportStyleProxy proxy = new ImportStyleProxy("Chapter Number",
				StyleType.kstParagraph, wsVern, m_styleSheet); //override as char style
			Assert.That(proxy.StyleType, Is.EqualTo(StyleType.kstCharacter), "Should override as character style");

			// verify TagType, EndMarker info
			proxy = new ImportStyleProxy("Xnote", // This style doesn't exist in DB
				StyleType.kstParagraph, wsVern, ContextValues.Note, m_styleSheet);
			proxy.EndMarker = "Xnote*";
			Assert.That(proxy.Context, Is.EqualTo(ContextValues.Note));
			Assert.That(proxy.EndMarker, Is.EqualTo("Xnote*"));

			// Verify that proxy doesn't attempt to create style when context is EndMarker
			proxy = new ImportStyleProxy("Xnote*",
				0, 0, ContextValues.EndMarker, m_styleSheet);
			int cStylesX = m_styleSheet.CStyles;
			// These calls should not add new style
			Assert.That(proxy.TsTextProps, Is.Null); //no props returned
			Assert.That(proxy.Context, Is.EqualTo(ContextValues.EndMarker));
			Assert.That(proxy.IsUnknownMapping, Is.True, "Xnote* should not exist");
			Assert.That(m_styleSheet.CStyles, Is.EqualTo(cStylesX));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the stylesheet is able to delete a new style added by the proxy.
		/// </summary>
		/// <remarks>If the proxy incorrectly creates the style as a built-in style, attempts
		/// to delete the style should fail.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteNewStyle()
		{
			int nStylesOrig = m_styleSheet.CStyles;

			// set up a proxy
			int wsVern = Cache.DefaultVernWs;
			ImportStyleProxy proxy = new ImportStyleProxy("MyNewStyle",
				StyleType.kstParagraph, wsVern, ContextValues.General, m_styleSheet);
			Assert.That(proxy.IsUnknownMapping, Is.True, "MyNewStyle style not should exist in DB");

			// Besides returning the props, retrieval of TsTextProps forces creation of a real style
			// in stylesheet
			ITsTextProps ttps = proxy.TsTextProps;
			Assert.That(proxy.IsUnknownMapping, Is.False, "style should be created when getting ParaProps");
			Assert.That(m_styleSheet.CStyles, Is.EqualTo(nStylesOrig + 1));

			// get the hvo of the new style
			int hvoStyle = -1;
			for (int i = 0; i < m_styleSheet.CStyles; i++)
			{
				if (m_styleSheet.get_NthStyleName(i) == "MyNewStyle")
				{
					hvoStyle = m_styleSheet.get_NthStyle(i);
				}
			}
			Assert.That(hvoStyle != -1, Is.True, "Style 'MyNewStyle' should exist in DB");

			// Now delete the new style
			m_styleSheet.Delete(hvoStyle);

			// Verify the deletion
			Assert.That(m_styleSheet.CStyles, Is.EqualTo(nStylesOrig));
			Assert.That(m_styleSheet.GetStyleRgch(0, "MyNewStyle"), Is.Null,
				"Should get null because style is not there");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a proxy for an existing style where the normalization of the proxy's
		/// style name doesn't match the normalization of the style in the stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateProxyForDecomposedStyleWhenExistingStyleIsComposed()
		{
			int hvoStyle = m_styleSheet.MakeNewStyle();
			m_styleSheet.PutStyle("\u0100", "Test", hvoStyle, 0, 0,
				(int)StyleType.kstParagraph, false, false, null);
			int cStylesOrig = m_styleSheet.CStyles;

			ImportStyleProxy proxy1 = new ImportStyleProxy("\u0041\u0304",
				StyleType.kstParagraph, Cache.DefaultVernWs, ContextValues.Text, m_styleSheet);
			Assert.That(proxy1.IsUnknownMapping, Is.False, "style should exist in DB");
			Assert.That(m_styleSheet.CStyles, Is.EqualTo(cStylesOrig));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a proxy for an existing style where the normalization of the proxy's
		/// style name doesn't match the normalization of the style in the stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateProxyForComposedStyleWhenExistingStyleIsDecomposed()
		{
			int hvoStyle = m_styleSheet.MakeNewStyle();
			m_styleSheet.PutStyle("\u0041\u0304", "Test", hvoStyle, 0, 0,
				(int)StyleType.kstParagraph, false, false, null);
			int cStylesOrig = m_styleSheet.CStyles;

			ImportStyleProxy proxy1 = new ImportStyleProxy("\u0100",
				StyleType.kstParagraph, Cache.DefaultVernWs, ContextValues.Text, m_styleSheet);
			Assert.That(proxy1.IsUnknownMapping, Is.False, "style should exist in DB");
			Assert.That(m_styleSheet.CStyles, Is.EqualTo(cStylesOrig));
		}
		#endregion
	}
}
