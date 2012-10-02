// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StFootnoteTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using NUnit.Framework;
using NUnit;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StFootnoteTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private StFootnote m_footnote;
		private ScrBook m_book;
		private ITsStrFactory m_strFact;
		private StTxtPara m_footnotePara;
		private ICmTranslation m_trans;
		private int m_vernWs; // Vernacular writing system (French)
		int m_wsDe; // German writing system (for back translation)
		int m_wsEs; // Spanish writing system (for back translation)
		int m_wsUr; // Urdu writing system (used for Foreign style)
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();

			// create footnote
			m_footnote = new StFootnote();
			m_book = (ScrBook)m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_book.FootnotesOS.Append(m_footnote);
			m_footnote.FootnoteMarker.Text = "o";
			m_footnote.DisplayFootnoteMarker = true;
			m_footnote.DisplayFootnoteReference = false;

			// create one empty footnote para
			StTxtPara para = new StTxtPara();
			m_footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);

			m_strFact = TsStrFactoryClass.Create();
			m_vernWs = Cache.LangProject.DefaultVernacularWritingSystem;
			para.Contents.UnderlyingTsString = m_strFact.MakeString(string.Empty, m_vernWs);
			m_footnotePara = (StTxtPara)m_footnote.ParagraphsOS[0];

			m_wsUr = InMemoryFdoCache.s_wsHvos.Ur; // used with 'foreign' character style
			m_wsDe = InMemoryFdoCache.s_wsHvos.De; // used for back translations
			m_wsEs = InMemoryFdoCache.s_wsHvos.Es;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the back translation for the footnote paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupBackTrans()
		{
			m_trans = m_scrInMemoryCache.AddBtToMockedParagraph(m_footnotePara, m_wsDe);
			m_scrInMemoryCache.AddBtToMockedParagraph(m_footnotePara, m_wsEs);
		}
		#endregion

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_footnote = null;;
			m_book = null;
			if (m_strFact != null)
				Marshal.ReleaseComObject(m_strFact);
			m_strFact = null;;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test GetTextRepresentation method
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an easy StFootnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_easy1Para()
		{
			CheckDisposed();

			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>",
				result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an easy StFootnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_withBrackets()
		{
			CheckDisposed();

			m_footnotePara.Contents.UnderlyingTsString =
				m_strFact.MakeString("Text in <brackets>", m_vernWs);

			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN></P></FN>", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method with ShowReference and then without
		/// ShowMarker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_showReference()
		{
			CheckDisposed();

			m_footnote.DisplayFootnoteReference = true;
			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><ShowReference/><P>" +
				"<PS>Note General Paragraph</PS><RUN WS='fr'></RUN></P></FN>", result);

			m_footnote.DisplayFootnoteMarker = false;
			result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowReference/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>",
				result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method with a null footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_nullFootnoteMarker()
		{
			CheckDisposed();

			// test null footnote marker
			m_footnote.FootnoteMarker.Text = null;
			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an StFootnote with a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_charStylePara()
		{
			CheckDisposed();

			ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Emphasis");
			strBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
									(int)FwTextPropVar.ktpvDefault, m_vernWs);
			strBldr.Append("Test Text");
			m_footnotePara.Contents.UnderlyingTsString = strBldr.GetString();
			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr' CS='Emphasis'>Test Text</RUN></P></FN>", result);
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
			CheckDisposed();

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

			// run 4
			strBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_wsDe);
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"Untranslated Word");
			strBldr.Append(" untranslated");

			m_footnotePara.Contents.UnderlyingTsString = strBldr.GetString();

			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN>" +
				"<RUN WS='de' CS='Untranslated Word'> untranslated</RUN></P></FN>", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTextRepresentation method using an StFootnote with two paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextRepresentation_twoParas()
		{
			CheckDisposed();

			m_footnotePara.Contents.UnderlyingTsString =
				m_strFact.MakeString("Paragraph One", m_vernWs);

			// create second para
			StTxtPara para = new StTxtPara();
			m_footnote.ParagraphsOS.Append(para);

			// Set the paragraph style
			para.StyleRules = StyleUtils.ParaStyleTextProps("Note Exegesis Paragraph");

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Paragraph Two", StyleUtils.CharStyleTextProps(
				"Foreign", m_wsUr));
			para.Contents.UnderlyingTsString = bldr.GetString();

			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
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
			CheckDisposed();
			SetupBackTrans();

			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
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
			CheckDisposed();
			SetupBackTrans();

			m_footnotePara.Contents.UnderlyingTsString = m_strFact.MakeString(
				"Text in <brackets>", m_vernWs);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, "Spanish BT in <brackets>", null);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, "German BT in <brackets>", null);

			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN>" +
				@"<TRANS WS='es'><RUN WS='es'>Spanish BT in &lt;brackets&gt;</RUN></TRANS>" +
				@"<TRANS WS='de'><RUN WS='de'>German BT in &lt;brackets&gt;</RUN></TRANS></P></FN>",
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
			CheckDisposed();
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

			m_footnotePara.Contents.UnderlyingTsString = strBldr.GetString();

			// Now add back translations with and without character styles.
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, "Spanish", null);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, " translation!", "Emphasis");

			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, "German!", "Emphasis");
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, " translation", null);

			string result = m_footnote.GetTextRepresentation();
			Assert.AreEqual(@"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN>" +
				"<TRANS WS='es'><RUN WS='es'>Spanish</RUN><RUN WS='fr' CS='Untranslated Word'> back </RUN><RUN WS='es' CS='Emphasis'> translation!</RUN></TRANS>" +
				"<TRANS WS='de'><RUN WS='de' CS='Emphasis'>German!</RUN><RUN WS='fr' CS='Untranslated Word'> back </RUN><RUN WS='de'> translation</RUN></TRANS>" +
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
			CheckDisposed();

			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'></RUN></P></FN>";

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");

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
			CheckDisposed();

			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN></P></FN>";

			ITsStrBldr bldr = m_footnotePara.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "Text in <brackets>", null);
			m_footnotePara.Contents.UnderlyingTsString = bldr.GetString();

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");

			CompareFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateFromStringRep method with/without the showreference tag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromStringRep_showReference()
		{
			CheckDisposed();

			string footnoteRep = @"<FN><M>o</M><ShowMarker/><ShowReference/><P>" +
				@"<PS>Note General Paragraph</PS><RUN WS='fr'></RUN></P></FN>";
			m_footnote.DisplayFootnoteReference = true;
			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
			CompareFootnote(footnote);

			footnoteRep = @"<FN><M>o</M><ShowReference/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>";
			m_footnote.DisplayFootnoteMarker = false;
			footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			CheckDisposed();

			// test null footnote marker
			m_footnote.FootnoteMarker.Text = null;

			string footnoteRep = @"<FN><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				"<RUN WS='fr'></RUN></P></FN>";
			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			CheckDisposed();

			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN></P></FN>";

			ITsStrBldr bldr = m_footnotePara.Contents.UnderlyingTsString.GetBldr();
			bldr.SetStrPropValue(0, 0, (int)FwTextPropType.ktptNamedStyle, "Emphasis");
			bldr.Replace(0, 0, "Test Text", null);
			m_footnotePara.Contents.UnderlyingTsString = bldr.GetString();

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");

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
			CheckDisposed();

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
			m_footnotePara.Contents.UnderlyingTsString = bldr.GetString();

			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN></P></FN>";
			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			CheckDisposed();

			ITsStrBldr bldr = m_footnotePara.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "Paragraph One", null);
			m_footnotePara.Contents.UnderlyingTsString = bldr.GetString();

			// create second para
			StTxtPara para = new StTxtPara();
			m_footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps("Note Exegesis Paragraph");
			bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Paragraph Two", StyleUtils.CharStyleTextProps("Foreign", m_wsUr));
			para.Contents.UnderlyingTsString = bldr.GetString();

			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Paragraph One</RUN></P><P><PS>Note Exegesis Paragraph</PS>" +
				@"<RUN WS='ur' CS='Foreign'>Paragraph Two</RUN></P></FN>";
			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			CheckDisposed();
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
			m_footnotePara.Contents.UnderlyingTsString = bldr.GetString();

			// ... and now set up the expected results for the back translations of the footnote.
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, "Spanish BT in <brackets>", null);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, "German BT in <brackets>", null);

			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Text in &lt;brackets&gt;</RUN>" +
				@"<TRANS WS='es'><RUN WS='es'>Spanish BT in &lt;brackets&gt;</RUN></TRANS>" +
				@"<TRANS WS='de'><RUN WS='de'>German BT in &lt;brackets&gt;</RUN></TRANS></P></FN>";
			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");

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
			CheckDisposed();
			SetupBackTrans();

			ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
			strBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_vernWs);

			// Setup expected results for the footnote
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Emphasis"); // run 1
			strBldr.Append("Test Text");
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null); // run 2
			strBldr.Append("No char style");
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Quoted Text"); // run 3
			strBldr.Append("Ahh!!!!!!");
			m_footnotePara.Contents.UnderlyingTsString = strBldr.GetString();

			// ... and now set up the expected results for the back translations of the footnote.
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, "Spanish", null);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsEs, " translation!", "Emphasis");

			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, "German!", "Emphasis");
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, m_vernWs, " back ", ScrStyleNames.UntranslatedWord);
			m_scrInMemoryCache.AddRunToMockedTrans(m_trans, m_wsDe, " translation", null);

			// Define text representation and create a footnote from it.
			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr' CS='Emphasis'>Test Text</RUN><RUN WS='fr'>No char style</RUN>" +
				"<RUN WS='fr' CS='Quoted Text'>Ahh!!!!!!</RUN>" +
				"<TRANS WS='es'><RUN WS='es'>Spanish</RUN><RUN WS='fr' CS='Untranslated Word'> back </RUN>" +
					"<RUN WS='es' CS='Emphasis'> translation!</RUN></TRANS>" +
				"<TRANS WS='de'><RUN WS='de' CS='Emphasis'>German!</RUN>" +
					"<RUN WS='fr' CS='Untranslated Word'> back </RUN><RUN WS='de'> translation</RUN></TRANS></P></FN>";

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");

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

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='AINT_IT' CS='Emphasis'>Test Text</RUN></P></FN>";

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS='fr'>Fine so far...</RUN>" +
				"<TRANS WS='en'><BLAH>...but here's the problem</BLAH></TRANS></P></FN>";

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN>Run without a writing system.</RUN></P></FN>";

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
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
			string footnoteRep = @"<FN><M>o</M><ShowMarker/><P><PS>Note General Paragraph</PS>" +
				@"<RUN WS=>Run has a writing system attribute but no value.</RUN></P></FN>";

			StFootnote footnote = StFootnote.CreateFromStringRep(m_book,
				(int)ScrBook.ScrBookTags.kflidFootnotes, footnoteRep, 0, "Note Marker");
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the given footnote with the values in m_footnote.
		/// </summary>
		/// <param name="footnote">The footnote to compare</param>
		/// ------------------------------------------------------------------------------------
		private void CompareFootnote(StFootnote footnote)
		{
			Assert.AreEqual(m_footnote.DisplayFootnoteMarker, footnote.DisplayFootnoteMarker,
				"Display footnote marker values did not match");
			Assert.AreEqual(m_footnote.DisplayFootnoteReference, footnote.DisplayFootnoteReference,
				"Display footnote reference values did not match");
			Assert.AreEqual(m_footnote.FootnoteMarker.Text, footnote.FootnoteMarker.Text);
			Assert.AreEqual(m_footnote.ParagraphsOS.Count, footnote.ParagraphsOS.Count,
				"Footnote paragraph count did not match");

			string diff = string.Empty;
			for (int i = 0; i < m_footnote.ParagraphsOS.Count; i++)
			{
				StTxtPara expectedPara = (StTxtPara)m_footnote.ParagraphsOS[i];
				StTxtPara actualPara = (StTxtPara)footnote.ParagraphsOS[i];
				bool result = TsTextPropsHelper.PropsAreEqual(expectedPara.StyleRules,
					actualPara.StyleRules, out diff);
				Assert.IsTrue(result, "paragraph " + i + " stylerules differed in: " + diff);

				result = TsStringHelper.TsStringsAreEqual(expectedPara.Contents.UnderlyingTsString,
					actualPara.Contents.UnderlyingTsString,	out diff);
				Assert.IsTrue(result, "paragraph " + i + " differed in: " + diff);

				CompareFootnoteTrans((CmTranslation)expectedPara.GetBT(), (CmTranslation)actualPara.GetBT(),
					actualPara.Hvo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the translations of the footnote.
		/// </summary>
		/// <param name="expectedTrans">The expected translation</param>
		/// <param name="actualTrans">The actual translation</param>
		/// <param name="currentParaHvo">The current footnote paragraph hvo.</param>
		/// ------------------------------------------------------------------------------------
		private void CompareFootnoteTrans(CmTranslation expectedTrans, CmTranslation actualTrans,
			int currentParaHvo)
		{
			if (expectedTrans == null)
			{
				Assert.IsNull(actualTrans,
					"Found an unexpected translation for paragraph hvo " + currentParaHvo);
				return;
			}
			Assert.IsNotNull(actualTrans, "Translation missing for paragraph hvo " + currentParaHvo);

			// The list of translation writing systems will be set to all the possible
			// writing systems with the InMemoryCache version of GetUsedScriptureTransWsForPara.
			List<int> transWs = m_inMemoryCache.Cache.GetUsedScriptureTransWsForPara(currentParaHvo);

			foreach (int ws in transWs)
			{
				ITsString tssExpected = expectedTrans.Translation.GetAlternativeTss(ws);
				ITsString tssActual = actualTrans.Translation.GetAlternativeTss(ws);

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
