// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StFootnoteTests.cs
// Responsibility: TE Team

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test for the StFootnote class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StFootnoteTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		// these tests are on methods of StFootnote, but we use a ScrFootnote because it's the
		// only kind of footnote we can currently add to an owning sequence.
		private IScrFootnote m_footnote;
		private IScrBook m_book;
		private IStTxtPara m_footnotePara;
		private ICmTranslation m_trans;
		private int m_vernWs; // Vernacular writing system (French)
		int m_wsEs; // Spanish writing system (for back translation)
		int m_wsUr; // Urdu writing system (used for Foreign style)
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			CoreWritingSystemDefinition ws;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ur", out ws);
			m_wsUr = ws.Handle;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out ws);
			m_wsEs = ws.Handle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_vernWs = Cache.DefaultVernWs;
			// create footnote
			IFdoServiceLocator servloc = Cache.ServiceLocator;
			m_footnote = servloc.GetInstance<IScrFootnoteFactory>().Create();
			m_book = AddBookToMockedScripture(1, "Genesis");
			m_book.FootnotesOS.Add(m_footnote);
			m_footnote.FootnoteMarker = TsStringUtils.MakeTss("a", m_vernWs);
			m_scr.DisplaySymbolInFootnote = true;
			m_scr.DisplayFootnoteReference = false;

			// create one empty footnote para
			m_footnotePara = m_footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			m_footnotePara.Contents = TsStringUtils.MakeTss(string.Empty, m_vernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the back translation for the footnote paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupBackTrans()
		{
			m_trans = AddBtToMockedParagraph(m_footnotePara, m_wsDe);
			AddBtToMockedParagraph(m_footnotePara, m_wsEs);
		}
		#endregion

		#region Test GetTextRepresentation method
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an easy StFootnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_easy1Para()
		{
			Assert.AreEqual("<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>",
				m_footnote.TextRepresentation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an easy StFootnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_withBrackets()
		{
			m_footnotePara.Contents = TsStringUtils.MakeTss("Text in <brackets>", m_vernWs);

			Assert.AreEqual("<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN></P></FN>",
				m_footnote.TextRepresentation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method when footnote markers are not being shown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_nullFootnoteMarker()
		{
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			Assert.AreEqual("<FN><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>", m_footnote.TextRepresentation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method when the footnote marker is a symbol.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_SymbolicFootnoteMarker()
		{
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			m_scr.FootnoteMarkerSymbol = "*";
			Assert.AreEqual("<FN><M>*</M><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>", m_footnote.TextRepresentation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an StFootnote with a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_charStylePara()
		{
			ITsIncStrBldr strBldr = m_footnotePara.Contents.GetIncBldr();
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Emphasis");
			strBldr.Append("Test Text");
			m_footnotePara.Contents = strBldr.GetString();
			Assert.AreEqual("<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr' CS='Emphasis'>Test Text</RUN></P></FN>",
				m_footnote.TextRepresentation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an StFootnote with a multiple character
		/// styles and text without a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_MultiCharStylePara()
		{
			ITsIncStrBldr strBldr = m_footnotePara.Contents.GetIncBldr();

			// run 1
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Emphasis");
			strBldr.Append("Test Text");

			// run 2
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				null);
			strBldr.Append("No char style");

			// run 3
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Quoted Text");
			strBldr.Append("Ahh!!!!!!");

			// run 4
			strBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_wsDe);
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Untranslated Word");
			strBldr.Append(" untranslated");

			m_footnotePara.Contents = strBldr.GetString();

			string result = m_footnote.TextRepresentation;
			Assert.AreEqual("<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN>" +
				"<RUN WS='de' CS='Untranslated Word'> untranslated</RUN></P></FN>", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an StFootnote with two paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("'ValidateAddObjectInternal' won't allow two paras in one footnote.")]
		public void GetTextRepresentation_twoParas()
		{
			m_footnotePara.Contents = TsStringUtils.MakeTss("Paragraph One", m_vernWs);

			// create second para
			IStTxtPara para = m_footnote.AddNewTextPara("Note Exegesis Paragraph");

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Paragraph Two", StyleUtils.CharStyleTextProps(
				"Foreign", m_wsUr));
			para.Contents = bldr.GetString();

			string result = m_footnote.TextRepresentation;
			Assert.AreEqual(@"<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Paragraph One</RUN></P><P><PS>Note Exegesis Paragraph</PS>" +
				@"<RUN WS='ur' CS='Foreign'>Paragraph Two</RUN></P></FN>", result);
		}
		#endregion

		#region Test GetTextRepresentation method with back translations (TE-5052)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an easy StFootnote with a
		/// back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_BT_emptyPara()
		{
			SetupBackTrans();

			string result = m_footnote.TextRepresentation;
			Assert.AreEqual(@"<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'></RUN></P></FN>", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an easy StFootnote with back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_BT_withBrackets()
		{
			SetupBackTrans();

			m_footnotePara.Contents = TsStringUtils.MakeTss("Text in <brackets>", m_vernWs);
			AddRunToMockedTrans(m_trans, m_wsEs, "Spanish BT in <brackets>", null);
			AddRunToMockedTrans(m_trans, m_wsDe, "German BT in <brackets>", null);

			string result = m_footnote.TextRepresentation;
			Assert.AreEqual("<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN>" +
				"<TRANS WS='de'><RUN WS='de'>German BT in &lt;brackets&gt;</RUN></TRANS>" +
				"<TRANS WS='es'><RUN WS='es'>Spanish BT in &lt;brackets&gt;</RUN></TRANS>" +
				"</P></FN>",
				result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an StFootnote with multiple character
		/// styles and text without a character style in the vernacular and the back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_BT_MultiCharStylePara()
		{
			SetupBackTrans();

			ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
			strBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_vernWs);

			// run 1
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Emphasis");
			strBldr.Append("Test Text");

			// run 2
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				null);
			strBldr.Append("No char style");

			// run 3
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Quoted Text");
			strBldr.Append("Ahh!!!!!!");

			m_footnotePara.Contents = strBldr.GetString();

			// Now add back translation runs with and without character styles.
			AddRunToMockedTrans(m_trans, m_wsEs, "Spanish", null);
			AddRunToMockedTrans(m_trans, m_wsEs, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			AddRunToMockedTrans(m_trans, m_wsEs, " translation!", "Emphasis");

			AddRunToMockedTrans(m_trans, m_wsDe, "German!", "Emphasis");
			AddRunToMockedTrans(m_trans, m_wsDe, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			AddRunToMockedTrans(m_trans, m_wsDe, " translation", null);

			string result = m_footnote.TextRepresentation;
			Assert.AreEqual(@"<FN><M>a</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN>" +
				"<TRANS WS='de'><RUN WS='de' CS='Emphasis'>German!</RUN><RUN WS='fr' CS='Untranslated Word'> back </RUN><RUN WS='de'> translation</RUN></TRANS>" +
				"<TRANS WS='es'><RUN WS='es'>Spanish</RUN><RUN WS='fr' CS='Untranslated Word'> back </RUN><RUN WS='es' CS='Emphasis'> translation!</RUN></TRANS>" +
				"</P></FN>", result);
		}
		#endregion

		#region Test CreateFromStringRep method
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method with just an easy string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_easy1Para()
		{
			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'></RUN></P></FN>";

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(
				m_book, footnoteRep, 0, "Note Marker");

			CompareFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method with a string containing brackets
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_withBrackets()
		{

			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN></P></FN>";

			ITsStrBldr bldr = m_footnotePara.Contents.GetBldr();
			bldr.Replace(0, 0, "Text in <brackets>", null);
			m_footnotePara.Contents = bldr.GetString();

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");

			CompareFootnote(footnote);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method with a null footnote marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_nullFootnoteMarker()
		{

			// test null footnote marker
			m_footnote.FootnoteMarker = null;

			string footnoteRep = @"<FN><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>";
			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
			CompareFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method with a character style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_charStylePara()
		{

			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN></P></FN>";

			ITsStrBldr bldr = m_footnotePara.Contents.GetBldr();
			bldr.SetStrPropValue(0, 0, (int)FwTextPropType.ktptNamedStyle, "Emphasis");
			bldr.Replace(0, 0, "Test Text", null);
			m_footnotePara.Contents = bldr.GetString();

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");

			CompareFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method with 2 character styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_twoCharStylePara()
		{

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault,
				m_vernWs);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Emphasis");
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Test Text", propsBldr.GetTextProps());
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				null);
			bldr.Replace(bldr.Length, bldr.Length, "No char style",
				propsBldr.GetTextProps());
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Quoted Text");
			bldr.Replace(bldr.Length, bldr.Length, "Ahh!!!!!!",
				propsBldr.GetTextProps());
			m_footnotePara.Contents = bldr.GetString();

			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN></P></FN>";
			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
			CompareFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method with two paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_twoParas()
		{
			ITsStrBldr bldr = m_footnotePara.Contents.GetBldr();
			bldr.Replace(0, 0, "Paragraph One", null);
			m_footnotePara.Contents = bldr.GetString();

			ReflectionHelper.SetField(typeof(SIL.FieldWorks.FDO.DomainImpl.ScrFootnote),
				"s_maxAllowedParagraphs", 2);

			// create second para
			IStTxtPara para = m_footnote.AddNewTextPara("Note Exegesis Paragraph");
			bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Paragraph Two", StyleUtils.CharStyleTextProps("Foreign", m_wsUr));
			para.Contents = bldr.GetString();

			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Paragraph One</RUN></P><P><PS>Note Exegesis Paragraph</PS>" +
				@"<RUN WS='ur' CS='Foreign'>Paragraph Two</RUN></P></FN>";
			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
			CompareFootnote(footnote);
		}
		#endregion

		#region Test CreateFromStringRep method with back translations (TE-5052)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method using an easy StFootnote with back translations
		/// and brackets.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_BT_withBrackets()
		{
			SetupBackTrans();

			// Setup expected results for the footnote
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_vernWs);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Text in <brackets>", propsBldr.GetTextProps());
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				null);
			m_footnotePara.Contents = bldr.GetString();

			// ... and now set up the expected results for the back translations of the footnote.
			AddRunToMockedTrans(m_trans, m_wsEs, "Spanish BT in <brackets>", null);
			AddRunToMockedTrans(m_trans, m_wsDe, "German BT in <brackets>", null);

			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN>" +
				@"<TRANS WS='de'><RUN WS='de'>German BT in &lt;brackets&gt;</RUN></TRANS>" +
				@"<TRANS WS='es'><RUN WS='es'>Spanish BT in &lt;brackets&gt;</RUN></TRANS></P></FN>";
			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");

			CompareFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an StFootnote with multiple character
		/// styles and text without a character style in the vernacular and the back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_BT_MultiCharStylePara()
		{
			SetupBackTrans();

			ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
			strBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_vernWs);

			// Setup expected results for the footnote
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasis"); // run 1
			strBldr.Append("Test Text");
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null); // run 2
			strBldr.Append("No char style");
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Quoted Text"); // run 3
			strBldr.Append("Ahh!!!!!!");
			m_footnotePara.Contents = strBldr.GetString();

			// ... and now set up the expected results for the back translations of the footnote.
			AddRunToMockedTrans(m_trans, m_wsEs, "Spanish", null);
			AddRunToMockedTrans(m_trans, m_wsEs, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			AddRunToMockedTrans(m_trans, m_wsEs, " translation!", "Emphasis");

			AddRunToMockedTrans(m_trans, m_wsDe, "German!", "Emphasis");
			AddRunToMockedTrans(m_trans, m_wsDe, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			AddRunToMockedTrans(m_trans, m_wsDe, " translation", null);

			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN>" +
				"<TRANS WS='de'><RUN WS='de' CS='Emphasis'>German!</RUN>" +
					"<RUN WS='fr' CS='Untranslated Word'> back </RUN><RUN WS='de'> translation</RUN></TRANS>" +
				"<TRANS WS='es'><RUN WS='es'>Spanish</RUN><RUN WS='fr' CS='Untranslated Word'> back </RUN>" +
					"<RUN WS='es' CS='Emphasis'> translation!</RUN></TRANS>" +
					"</P></FN>";

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");

			CompareFootnote(footnote);
		}
		#endregion

		#region Testing for exceptions in CreateFromStringRep
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the condition in CreateFromStringRep when a string representation does not
		/// have a recognized element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage="Unrecognized XML format for footnote.")]
		public void CreateFromStringRep_ExcptnUnknownElement()
		{
			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><BLAH>o</BLAH>";

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the condition in CreateFromStringRep when a string representation has a
		/// writing system (WS) with an unrecognized ICU locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage="Unknown ICU locale encountered: 'AINT_IT'")]
		public void CreateFromStringRep_ExcptnInvalidICU()
		{
			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='AINT_IT' CS='Emphasis'>Test Text</RUN></P></FN>";

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the condition in CreateFromStringRep when a string representation has a
		/// translation (TRANS) with an unrecognized ICU locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage="Unexpected translation element 'BLAH' encountered for ws 'en'")]
		public void CreateFromStringRep_ExcptnUnknownTRANS_Element()
		{
			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Fine so far...</RUN>" +
				"<TRANS WS='en'><BLAH>...but here's the problem</BLAH></TRANS></P></FN>";

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the condition in CreateFromStringRep when a string representation has a
		/// text run (RUN) without a writing system (WS).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage="Required attribute WS missing from RUN element.")]
		public void CreateFromStringRep_ExcptnWSMissingFromRunElement()
		{
			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN>Run without a writing system.</RUN></P></FN>";

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the condition in CreateFromStringRep when a string representation has a
		/// text run (RUN) without a writing system (WS).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage="Unrecognized XML format for footnote.")]
		public void CreateFromStringRep_ExcptnWSWithoutValue()
		{
			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS=>Run has a writing system attribute but no value.</RUN></P></FN>";

			IStFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().CreateFromStringRep(m_book,
				footnoteRep, 0, "Note Marker");
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the given footnote with the values in m_footnote.
		/// </summary>
		/// <param name="footnote">The footnote to compare</param>
		/// ------------------------------------------------------------------------------------
		private void CompareFootnote(IStFootnote footnote)
		{
			Assert.AreEqual(m_footnote.MarkerType, footnote.MarkerType);
			if (footnote.MarkerType == FootnoteMarkerTypes.SymbolicFootnoteMarker)
				AssertEx.AreTsStringsEqual(m_footnote.FootnoteMarker, footnote.FootnoteMarker);
			Assert.AreEqual(m_footnote.ParagraphsOS.Count, footnote.ParagraphsOS.Count,
				"Footnote paragraph count did not match");

			string diff = string.Empty;
			for (int i = 0; i < m_footnote.ParagraphsOS.Count; i++)
			{
				IStTxtPara expectedPara = (IStTxtPara)m_footnote.ParagraphsOS[i];
				IStTxtPara actualPara = (IStTxtPara)footnote.ParagraphsOS[i];
				bool result = TsTextPropsHelper.PropsAreEqual(expectedPara.StyleRules,
					actualPara.StyleRules, out diff);
				Assert.IsTrue(result, "paragraph " + i + " stylerules differed in: " + diff);

				result = TsStringHelper.TsStringsAreEqual(expectedPara.Contents,
					actualPara.Contents, out diff);
				Assert.IsTrue(result, "paragraph " + i + " differed in: " + diff);

				CompareFootnoteTrans(expectedPara.GetBT(), actualPara);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the translations of the footnote.
		/// </summary>
		/// <param name="expectedTrans">The expected translation</param>
		/// <param name="currentPara">The current footnote paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void CompareFootnoteTrans(ICmTranslation expectedTrans, IStTxtPara currentPara)
		{
			ICmTranslation actualTrans = currentPara.GetBT();

			if (expectedTrans == null)
			{
				Assert.IsNull(actualTrans, "Found an unexpected translation for footnote paragraph " +
					currentPara.IndexInOwner + " having text: " + currentPara.Contents.Text);
				return;
			}
			Assert.IsNotNull(actualTrans, "Translation missing for paragraph " +
					currentPara.IndexInOwner + " having text: " + currentPara.Contents.Text);

			foreach (int ws in actualTrans.Translation.AvailableWritingSystemIds)
			{
				ITsString tssExpected = expectedTrans.Translation.get_String(ws);
				ITsString tssActual = actualTrans.Translation.get_String(ws);

				if (tssExpected == null || tssExpected.Length == 0)
				{
					Assert.IsTrue(tssActual == null || tssActual.Length == 0,
						"Found an unexpected translation for WS " + ws);
					continue;
				}

				AssertEx.AreTsStringsEqual(tssExpected, tssActual);
			}
		}
		#endregion
	}
}
