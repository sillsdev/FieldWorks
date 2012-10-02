#define TurnOffBitheadFeatures

using System;
using System.Collections.Generic;
using System.Text;
using Office = Microsoft.Office.Core;
using Word = Microsoft.Office.Interop.Word;
using System.Windows.Forms;                     // for DialogResult

namespace SILConvertersOffice
{
#if !TurnOffBitheadFeatures
#if !Csc30
	using SpellingFixer30;
#else
	using SpellingFixerEC;
#endif
#endif

	internal class WordApp : OfficeApp
	{
#if !TurnOffBitheadFeatures
		private Office.CommandBarPopup ProcessDocumentPopup;
		// private Office.CommandBarButton ParagraphByParagraphMenu;
		private Office.CommandBarButton WordByWordMenu;
		private Office.CommandBarButton WordByWordFontMenu;
		private Office.CommandBarButton ProcessDocumentResetMenu;

		private Office.CommandBarPopup ProcessDocumentSpellFixerPopup;
		private Office.CommandBarButton SpellFixerAddCorrectionMenu;
		private Office.CommandBarButton SpellFixerCorrectSelectedTextMenu;
		private Office.CommandBarButton SpellFixerFindRuleMenu;
		private Office.CommandBarButton SpellFixerEditCorrectionsMenu;
		private Office.CommandBarButton SpellFixerWordByWordMenu;
		private Office.CommandBarButton SpellFixerWordByWordFontMenu;
		private Office.CommandBarButton SpellFixerResetMenu;

		private Office.CommandBarPopup RoundTripPopup;
		private Office.CommandBarButton RoundTripCheckMenu;
		private Office.CommandBarButton RoundTripCheckFontMenu;
		private Office.CommandBarButton RoundTripResetMenu;

		private Office.CommandBarButton ProcessSelectionMenu;

		private Office.CommandBarButton FindReplaceMenu;

#if !Csc30
		private static SpellingFixer m_aSF = null;
#else
		private static SpellingFixerEC m_aSF = null;
#endif
#endif

		public WordApp(object app)
			: base(app)
		{
		}

		public new Word.Application Application
		{
			get { return (Word.Application)base.Application; }
		}

#if BUILD_FOR_OFF14
		public override string GetCustomUI()
		{
			return SILConvertersOffice10.Properties.Resources.RibbonWord;
		}
#elif BUILD_FOR_OFF12
		public override string GetCustomUI()
		{
			return SILConvertersOffice07.Properties.Resources.RibbonWord;
		}
#else
		private Office.CommandBarButton SelectionConvertMenu;
		private Office.CommandBarButton FindReplaceMenu;
		private Office.CommandBarButton RoundTripCheckMenu;
		private Office.CommandBarButton ResetMenu;

		public override void LoadMenu()
		{
			base.LoadMenu();

			try
			{
				AddMenu(ref SelectionConvertMenu, NewMenuBar, "Convert &selection",
					"Click this item to convert the selected text",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SelectionConvert_Click));

				AddMenu(ref FindReplaceMenu, NewMenuBar, "&Regex find/replace",
					"Click this item to search the document using Regular Expression syntax",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(FindReplace_Click));

				AddMenu(ref RoundTripCheckMenu, NewMenuBar, "&Check round-trip conversion",
					"Click this item to check a bidirectional converter's round-trip capability",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(RoundTripCheck_Click));

				AddMenu(ref ResetMenu, NewMenuBar, "&Reset",
					"Reset the unfinished conversion processes",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(Reset_Click));

#if !TurnOffBitheadFeatures
				ProcessDocumentPopup = (Office.CommandBarPopup)NewMenuBar.Controls.Add(Office.MsoControlType.msoControlPopup, missing, missing, missing, true);
				ProcessDocumentPopup.Caption = "&Process Document";

				AddMenu(ref ParagraphByParagraphMenu, ProcessDocumentPopup, "&Paragraph by paragraph (choose EncConverter)",
					"Click this item to process the text in this document with a converter from the system repository in a paragraph-by-paragraph fashion",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(ParagraphByParagraph_Click));

				AddMenu(ref WordByWordMenu, ProcessDocumentPopup, "&Word by word (choose EncConverter)",
					"Click this item to process the text in this document with a converter from the system repository in a word-by-word fashion",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(WordByWord_Click));

				AddMenu(ref WordByWordFontMenu, ProcessDocumentPopup, "Word by word (choose EncConverter and &Font)",
					"Click this item to process the text in this document (in a particular font) with a converter from the system repository in a word-by-word fashion",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(WordByWordFont_Click));

				AddMenu(ref ProcessDocumentResetMenu, ProcessDocumentPopup, "&Reset",
					"Reset the list of found items and unfinished conversion processes",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(Reset_Click));

				AddMenu(ref ProcessSelectionMenu, NewMenuBar, "&Process Selection",
					"Click this item to process the selected text with a converter from the system repository",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(ProcessSelection_Click));

				AddMenu(ref FindReplaceMenu, NewMenuBar, "&Unicode/Regular Expression Find/Replace",
					"Click this item to search the document for a particular word and/or using Regular Expressions",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(FindReplace_Click));

				ProcessDocumentSpellFixerPopup = (Office.CommandBarPopup)NewMenuBar.Controls.Add(Office.MsoControlType.msoControlPopup, missing, missing, missing, true);
				ProcessDocumentSpellFixerPopup.Caption = "&SpellFixer Commands";

				AddMenu(ref SpellFixerAddCorrectionMenu, ProcessDocumentSpellFixerPopup, "&Add Correction",
					"Click this item to add a correction for the selected word to the selected SpellFixer project",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SpellFixerAddCorrection_Click));

				AddMenu(ref SpellFixerCorrectSelectedTextMenu, ProcessDocumentSpellFixerPopup, "&Correct Selected Text",
					"Click this item to correct the selected text using the selected SpellFixer project",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SpellFixerCorrectSelectedText_Click));

				AddMenu(ref SpellFixerFindRuleMenu, ProcessDocumentSpellFixerPopup, "&Display Rule",
					"Click this item to display the rule that applies (if any) to the selected text using the selected SpellFixer project",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SpellFixerFindRule_Click));

				AddMenu(ref SpellFixerEditCorrectionsMenu, ProcessDocumentSpellFixerPopup, "&Edit Corrections",
					"Click this item to display all the rules in the selected SpellFixer project for editing",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SpellFixerEditCorrections_Click));

				AddMenu(ref SpellFixerWordByWordMenu, ProcessDocumentSpellFixerPopup, "&Correct whole document",
					"Click this item to process the text in this document in a word-by-word fashion to correct misspelled words using the selected SpellFixer project",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SpellFixerWordByWord_Click));

				AddMenu(ref SpellFixerWordByWordFontMenu, ProcessDocumentSpellFixerPopup, "Correct whole document (choose &font)",
					"Click this item to process the text in this document (in a particular font) in a word-by-word fashion to correct misspelled words using the selected SpellFixer project",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SpellFixerWordByWordFont_Click));

				AddMenu(ref SpellFixerResetMenu, ProcessDocumentSpellFixerPopup, "&Reset",
					"Reset the active SpellFixer project, the list of found items, and unfinished conversion processes",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SpellFixerReset_Click));

				RoundTripPopup = (Office.CommandBarPopup)NewMenuBar.Controls.Add(Office.MsoControlType.msoControlPopup, missing, missing, missing, true);
				RoundTripPopup.Caption = "&Round-trip Checking";

				AddMenu(ref RoundTripCheckMenu, RoundTripPopup, "&Check Round-Trip conversion",
					"Click this item to check a bidirectional converter's round-trip capability",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(RoundTripCheck_Click));

				AddMenu(ref RoundTripCheckFontMenu, RoundTripPopup, "Check Round-Trip conversion (choose &Font)",
					"Click this item to check a bidirectional converter's round-trip capability",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(RoundTripCheckFont_Click));

				AddMenu(ref RoundTripResetMenu, RoundTripPopup, "&Reset",
					"Reset any unfinished round-trip checking processes",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(RoundTripReset_Click));
#endif
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}
#endif

		bool HookDocumentClose(Word.Document doc)
		{
			if (doc != null)
			{
				Word.DocumentEvents2_Event docEvents2 = (Word.DocumentEvents2_Event)doc;
				docEvents2.Close += new Microsoft.Office.Interop.Word.DocumentEvents2_CloseEventHandler(DocumentClosed);
				return true;
			}
			return false;
		}

#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void SelectionConvert_Click(Office.IRibbonControl control)
#else
		void SelectionConvert_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
#endif
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			try
			{
				WordSelectionDocument doc = new WordSelectionDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);
#if !QueryAllFonts
				// first get the fonts the user wants to process
				OfficeDocumentProcessor aSelectionProcessor = null;
				FontConvertersPicker aFCsPicker = new FontConvertersPicker(doc);
				if ((aFCsPicker.ShowDialog() == DialogResult.OK) && (aFCsPicker.SelectedFontConverters.Count > 0))
				{
					FontConverters aFCs = aFCsPicker.SelectedFontConverters;
					aSelectionProcessor = new OfficeDocumentProcessor(aFCs, new SILConverterProcessorForm());
				}
#else
				OfficeDocumentProcessor aSelectionProcessor = new OfficeDocumentProcessor((FontConverters)null, new SILConverterProcessorForm());
#endif
				if (aSelectionProcessor != null)
					doc.ProcessWordByWord(aSelectionProcessor);
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}

		FindReplaceForm m_formFindReplace = null;

#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void FindReplace_Click(Office.IRibbonControl control)
#else
		void FindReplace_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
#endif
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			try
			{
				if (m_formFindReplace == null)
				{
					WordFindReplaceDocument doc = new WordFindReplaceDocument(Application.ActiveDocument);
					m_formFindReplace = new FindReplaceForm(doc);
				}

				m_formFindReplace.Show();
				m_formFindReplace.BringToFront();
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}

		RoundTripCheckWordProcessor m_aRoundTripCheckWordProcessor = null;
#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void RoundTripCheck_Click(Office.IRibbonControl control)
#else
		void RoundTripCheck_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
#endif
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				if (m_aRoundTripCheckWordProcessor == null)
					m_aRoundTripCheckWordProcessor = new RoundTripCheckWordProcessor(null);

				if (doc.ProcessWordByWord(m_aRoundTripCheckWordProcessor))
					m_aRoundTripCheckWordProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}

			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
}

		public void DocumentClosed()
		{
			if (m_formFindReplace != null)
				m_formFindReplace.Close();
			m_formFindReplace = null;
			m_aRoundTripCheckWordProcessor = null;
		}

#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void Reset_Click(Office.IRibbonControl control)
#else
		void Reset_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
#endif
		{
			DocumentClosed();
		}

#if !TurnOffBitheadFeatures
#if !Csc30
		internal SpellingFixer GetSpellFixer
		{
			get
			{
				if (m_aSF == null)
					m_aSF = new SpellingFixer();
#else
		internal SpellingFixerEC GetSpellFixer
		{
			get
			{
				if (m_aSF == null)
				{
					m_aSF = new SpellingFixerEC();
					m_aSF.LoginProject();
				}
#endif

				return m_aSF;
			}
		}

		bool IsGoodTest(string strSelectedText)
		{
			return ((strSelectedText.Length > 0) && (strSelectedText != "\r"));
		}

		void CorrectSelectedText()
		{
			string strSelectedText = Application.Selection.Text;
			if (IsGoodTest(strSelectedText))
			{
				string strOutput = GetSpellFixer.SpellFixerEncConverter.Convert(strSelectedText);
				if (strOutput != strSelectedText)
					Application.Selection.Text = strOutput;
			}
		}

		void SpellFixerEditCorrections_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				GetSpellFixer.EditSpellingFixes();
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		void SpellFixerFindRule_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				string strSelectedText = Application.Selection.Text;
				if (IsGoodTest(strSelectedText))
					GetSpellFixer.FindReplacementRule(strSelectedText);
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		void SpellFixerCorrectSelectedText_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				CorrectSelectedText();
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		void SpellFixerAddCorrection_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				GetSpellFixer.AssignCorrectSpelling(Application.Selection.Text);
				CorrectSelectedText();
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		OfficeDocumentProcessor m_aWordByWordSpellFixerProcessor = null;
		void SpellFixerWordByWord_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				if (m_aWordByWordSpellFixerProcessor == null)
				{
					SpellFixerProcessorForm form = new SpellFixerProcessorForm(GetSpellFixer);
					FontConverter aFC = new FontConverter(GetSpellFixer);
					m_aWordByWordSpellFixerProcessor = new OfficeDocumentProcessor(aFC, form);
				}

				if (doc.ProcessWordByWord(m_aWordByWordSpellFixerProcessor))
					m_aWordByWordSpellFixerProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		OfficeDocumentProcessor m_aWordByWordFontSpellFixerProcessor = null;
		void SpellFixerWordByWordFont_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				// we want to process the file word by word and show differences between
				//  the Input and (once) Converted string
				if (m_aWordByWordFontSpellFixerProcessor == null)
				{
					SpellFixerProcessorForm form = new SpellFixerProcessorForm(GetSpellFixer);

					// next get the fonts the user wants to process
					FontConvertersPicker aFCsPicker = new FontConvertersPicker(doc, GetSpellFixer.SpellFixerEncConverter);

					if ((aFCsPicker.ShowDialog() == DialogResult.OK) && (aFCsPicker.SelectedFontConverters.Count > 0))
						m_aWordByWordFontSpellFixerProcessor = new OfficeDocumentProcessor(aFCsPicker.SelectedFontConverters, form);
					else
						return;
				}

				if (m_aWordByWordFontSpellFixerProcessor != null)
					if (doc.ProcessWordByWord(m_aWordByWordFontSpellFixerProcessor))
						m_aWordByWordFontSpellFixerProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		OfficeDocumentProcessor m_aParagraphByParagraphProcessor = null;
		void ParagraphByParagraph_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;

			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eIsoFormattedRun);

				if (m_aParagraphByParagraphProcessor == null)
					m_aParagraphByParagraphProcessor = new OfficeDocumentProcessor(new SILConverterProcessorForm());

				if (doc.ProcessWordByWord(m_aParagraphByParagraphProcessor))
					m_aParagraphByParagraphProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}

			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		OfficeDocumentProcessor m_aWordByWordProcessor = null;
		void WordByWord_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;

			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				if (m_aWordByWordProcessor == null)
					m_aWordByWordProcessor = new OfficeDocumentProcessor(new SILConverterProcessorForm());

				if (doc.ProcessWordByWord(m_aWordByWordProcessor))
					m_aWordByWordProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}

			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		OfficeDocumentProcessor m_aWordByWordFontProcessor = null;
		void WordByWordFont_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				// we want to process the file word by word and show differences between
				//  the Input and (once) Converted string
				if (m_aWordByWordFontProcessor == null)
				{
					// first get the fonts the user wants to process
					FontConvertersPicker aFCsPicker = new FontConvertersPicker(doc);
					if ((aFCsPicker.ShowDialog() == DialogResult.OK) && (aFCsPicker.SelectedFontConverters.Count > 0))
					{
						FontConverters aFCs = aFCsPicker.SelectedFontConverters;
						m_aWordByWordFontProcessor = new OfficeDocumentProcessor(aFCs, new SILConverterProcessorForm());
					}
				}

				if (m_aWordByWordFontProcessor != null)
					if (doc.ProcessWordByWord(m_aWordByWordFontProcessor))
						m_aWordByWordFontProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}

			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		RoundTripCheckWordProcessor m_aRoundTripCheckWordProcessor = null;
		void RoundTripCheck_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				if (m_aRoundTripCheckWordProcessor == null)
					m_aRoundTripCheckWordProcessor = new RoundTripCheckWordProcessor(null);

				if (doc.ProcessWordByWord(m_aRoundTripCheckWordProcessor))
					m_aRoundTripCheckWordProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}

			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		RoundTripCheckWordProcessor m_aRoundTripCheckFontWordProcessor = null;
		void RoundTripCheckFont_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			Application.System.Cursor = Word.WdCursorType.wdCursorWait;
			try
			{
				WordDocument doc = new WordDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				if (m_aRoundTripCheckFontWordProcessor == null)
				{
					// first get the fonts the user wants to process
					FontConvertersPicker aFCsPicker = new FontConvertersPicker(doc);
					if (aFCsPicker.ShowDialog() == DialogResult.OK)
					{
						FontConverters aFCs = aFCsPicker.SelectedFontConverters;
						m_aRoundTripCheckFontWordProcessor = new RoundTripCheckWordProcessor(aFCs);
					}
				}

				if (m_aRoundTripCheckFontWordProcessor != null)
					if (doc.ProcessWordByWord(m_aRoundTripCheckFontWordProcessor))
						m_aRoundTripCheckFontWordProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}

			Application.System.Cursor = Word.WdCursorType.wdCursorNormal;
		}

		void SelectionConvert_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			try
			{
				WordSelectionDocument doc = new WordSelectionDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);
				OfficeDocumentProcessor aSelectionProcessor = new OfficeDocumentProcessor((FontConverters)null, new SILConverterProcessorForm());
				doc.ProcessWordByWord(aSelectionProcessor);
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}

		FindReplaceForm m_formFindReplace = null;
		void FindReplace_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			try
			{
				if (m_formFindReplace == null)
				{
					WordFindReplaceDocument doc = new WordFindReplaceDocument(Application.ActiveDocument);
					m_formFindReplace = new FindReplaceForm(doc);
				}

				m_formFindReplace.Show();
				m_formFindReplace.BringToFront();
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}

		bool HookDocumentClose(Word.Document doc)
		{
			if (doc != null)
			{
				Word.DocumentEvents2_Event docEvents2 = (Word.DocumentEvents2_Event)doc;
				docEvents2.Close += new Microsoft.Office.Interop.Word.DocumentEvents2_CloseEventHandler(DocumentClosed);
				return true;
			}
			return false;
		}

		void Reset_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			m_aWordByWordProcessor = null;
			m_aWordByWordFontProcessor = null;
		}

		void SpellFixerReset_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			m_aSF = null;
			m_aWordByWordSpellFixerProcessor = null;
			m_aWordByWordFontSpellFixerProcessor = null;
		}

		void RoundTripReset_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			m_aRoundTripCheckWordProcessor = null;
			m_aRoundTripCheckFontWordProcessor = null;
		}

		public void DocumentClosed()
		{
			if (m_formFindReplace != null)
				m_formFindReplace.Close();
			m_formFindReplace = null;

			m_aWordByWordProcessor = null;
			m_aWordByWordFontProcessor = null;

			m_aWordByWordSpellFixerProcessor = null;
			m_aWordByWordFontSpellFixerProcessor = null;

			m_aRoundTripCheckWordProcessor = null;
			m_aRoundTripCheckFontWordProcessor = null;
		}
#endif
	}
}
