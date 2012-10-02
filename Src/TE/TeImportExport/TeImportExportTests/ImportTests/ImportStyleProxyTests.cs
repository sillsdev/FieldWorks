// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportStyleProxyTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE.ImportTests
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// ImportStyleProxyTests class to test ImportStyleProxy
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportStyleProxyTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private FwStyleSheet m_styleSheet;
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

			m_styleSheet = new FwStyleSheet();
			// Force load of styles
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			Assert.IsTrue(scr.StylesOC.Count > 0);
			m_styleSheet.Init(Cache, scr.Hvo, ScriptureTags.kflidStyles);
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
			Assert.IsTrue(cStylesOrig > 10);
			Assert.IsNotNull(m_styleSheet.GetStyleRgch(0, "Section Head"));
			Assert.IsNotNull(m_styleSheet.GetStyleRgch(0, "Verse Number"));

			// create four new proxies;  verify that they properly determine if they are
			//  mapped to the TE default stylesheet
			int wsVern = Cache.DefaultVernWs;
			int wsAnal = Cache.DefaultAnalWs;
			ImportStyleProxy proxy1 = new ImportStyleProxy("Section Head",
				StyleType.kstParagraph, wsVern, ContextValues.Text, m_styleSheet);
			Assert.IsFalse(proxy1.IsUnknownMapping, "Section Head style should exist in DB");

			ImportStyleProxy proxy2 = new ImportStyleProxy("Verse Number",
				StyleType.kstCharacter, wsVern, ContextValues.Text, m_styleSheet);
			Assert.IsFalse(proxy2.IsUnknownMapping, "Verse Number style should exist in DB");

			string proxy3Name = "Tom Bogle";
			ImportStyleProxy proxy3 = new ImportStyleProxy(proxy3Name,
				StyleType.kstParagraph, wsVern, m_styleSheet); //defaults to Text context
			Assert.IsTrue(proxy3.IsUnknownMapping, "Tom Bogle style shouldn't exist in DB");

			string proxy4Name = "Todd Jones";
			ImportStyleProxy proxy4 = new ImportStyleProxy(proxy4Name,
				StyleType.kstCharacter, wsVern, m_styleSheet); //defaults to Text context
			Assert.IsTrue(proxy4.IsUnknownMapping, "Todd Jones style shouldn't exist in DB");

			// verify basic proxy info - name, context, structure, function, styletype, endmarker
			Assert.AreEqual("Section Head", proxy1.StyleId);
			Assert.AreEqual(ContextValues.Text, proxy1.Context);
			Assert.AreEqual(StructureValues.Heading, proxy1.Structure);
			Assert.AreEqual(StyleType.kstParagraph, proxy1.StyleType);
			Assert.IsNull(proxy1.EndMarker);

			Assert.AreEqual(ContextValues.Text, proxy2.Context);
			Assert.AreEqual(StructureValues.Body, proxy2.Structure);
			Assert.AreEqual(FunctionValues.Verse, proxy2.Function);
			Assert.AreEqual(StyleType.kstCharacter, proxy2.StyleType);
			Assert.IsNull(proxy2.EndMarker);

			Assert.AreEqual(ContextValues.Text, proxy3.Context);
			// getting the text props will cause the style to be created in the database
			ITsTextProps props = proxy3.TsTextProps;
			IStStyle dbStyle = m_styleSheet.FindStyle(proxy3Name);
			Assert.AreEqual(ScrStyleNames.NormalParagraph, dbStyle.BasedOnRA.Name);
			Assert.AreEqual(StyleType.kstParagraph, proxy3.StyleType);
			Assert.IsNull(proxy3.EndMarker);

			Assert.AreEqual(ContextValues.Text, proxy4.Context);
			props = proxy4.TsTextProps;
			dbStyle = m_styleSheet.FindStyle(proxy4Name);
			Assert.IsNull(dbStyle.BasedOnRA);
			Assert.AreEqual(StyleType.kstCharacter, proxy4.StyleType);
			Assert.IsNull(proxy4.EndMarker);

			// use SetFormat to add formatting props to unmapped proxy3
			ITsPropsBldr tsPropertiesBldr = TsPropsBldrClass.Create();
			tsPropertiesBldr.SetIntPropValues((int)FwTextPropType.ktptItalic,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvForceOn);
			ITsTextProps formatProps3 = tsPropertiesBldr.GetTextProps();
			proxy3.SetFormat(formatProps3, false);

			// Test retrieval of ParaProps and TsTextProps
			Assert.IsNotNull(proxy1.TsTextProps);
			Assert.IsNotNull(proxy1.ParaProps);
			Assert.IsNotNull(proxy2.TsTextProps);
			Assert.IsNull(proxy2.ParaProps, "No para props for a char style");
			// Besides returning the props, retrieving ParaProps or TsTextProps adds a
			// previously unmapped style to the stylesheet, so that proxy becomes mapped
			// Next two calls force creation of new styles
			Assert.IsNotNull(proxy3.TsTextProps); // has benefit of SetFormat
			Assert.IsNull(proxy4.ParaProps); // no benefit of SetFormat
			Assert.IsFalse(proxy3.IsUnknownMapping,
				"Tom Bogle style should be created when getting TsTextProps");
			Assert.IsFalse(proxy4.IsUnknownMapping,
				"Todd Jones style should be created when getting ParaProps");
			// verify that two new styles were added to the style sheet
			Assert.AreEqual(cStylesOrig + 2, m_styleSheet.CStyles);

			// verify that the added styles have the appropriate context, etc
			IStStyle style = m_styleSheet.FindStyle("Tom Bogle");
			Assert.AreEqual(ContextValues.Text, (ContextValues)style.Context);
			Assert.AreEqual(StructureValues.Body, (StructureValues)style.Structure);
			Assert.AreEqual(FunctionValues.Prose, (FunctionValues)style.Function);

			// Test the styletype override from stylesheet
			// We will attempt to construct a paragraph style proxy,
			//  but since in the stylesheet Chapter Number is a character style,
			//  character will override
			ImportStyleProxy proxy = new ImportStyleProxy("Chapter Number",
				StyleType.kstParagraph, wsVern, m_styleSheet); //override as char style
			Assert.AreEqual(StyleType.kstCharacter, proxy.StyleType,
				"Should override as character style");

			// verify TagType, EndMarker info
			proxy = new ImportStyleProxy("Xnote", // This style doesn't exist in DB
				StyleType.kstParagraph, wsVern, ContextValues.Note, m_styleSheet);
			proxy.EndMarker = "Xnote*";
			Assert.AreEqual(ContextValues.Note, proxy.Context);
			Assert.AreEqual("Xnote*", proxy.EndMarker);

			// Verify that proxy doesn't attempt to create style when context is EndMarker
			proxy = new ImportStyleProxy("Xnote*",
				0, 0, ContextValues.EndMarker, m_styleSheet);
			int cStylesX = m_styleSheet.CStyles;
			// These calls should not add new style
			Assert.IsNull(proxy.TsTextProps); //no props returned
			Assert.IsNull(proxy.ParaProps); //no props returned
			Assert.AreEqual(ContextValues.EndMarker, proxy.Context);
			Assert.IsTrue(proxy.IsUnknownMapping, "Xnote* should not exist");
			Assert.AreEqual(cStylesX, m_styleSheet.CStyles);
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
			Assert.IsTrue(proxy.IsUnknownMapping, "MyNewStyle style not should exist in DB");

			// Besides returning the props, retrieval of ParaProps forces creation of a real style
			// in stylesheet
			byte[] rgParaProps = proxy.ParaProps;
			Assert.IsFalse(proxy.IsUnknownMapping, "style should be created when getting ParaProps");
			Assert.AreEqual(nStylesOrig + 1, m_styleSheet.CStyles);

			// get the hvo of the new style
			int hvoStyle = -1;
			for (int i = 0; i < m_styleSheet.CStyles; i++)
			{
				if (m_styleSheet.get_NthStyleName(i) == "MyNewStyle")
				{
					hvoStyle = m_styleSheet.get_NthStyle(i);
				}
			}
			Assert.IsTrue(hvoStyle != -1, "Style 'MyNewStyle' should exist in DB");

			// Now delete the new style
			m_styleSheet.Delete(hvoStyle);

			// Verfiy the deletion
			Assert.AreEqual(nStylesOrig, m_styleSheet.CStyles);
			Assert.IsNull(m_styleSheet.GetStyleRgch(0, "MyNewStyle"),
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
			Assert.IsFalse(proxy1.IsUnknownMapping, "style should exist in DB");
			Assert.AreEqual(cStylesOrig, m_styleSheet.CStyles);
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
			Assert.IsFalse(proxy1.IsUnknownMapping, "style should exist in DB");
			Assert.AreEqual(cStylesOrig, m_styleSheet.CStyles);
		}
		#endregion
	}
}
