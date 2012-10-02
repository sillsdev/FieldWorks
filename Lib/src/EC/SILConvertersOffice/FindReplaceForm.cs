// #define DefineToTurnOffBackgroundProcessing

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters40;
using System.IO;
using Word = Microsoft.Office.Interop.Word;
using System.Reflection;                // for Assembly

namespace SILConvertersOffice
{
	internal enum SearchAreaType
	{
		eUnknown,
		eSelection,
		eWholeDocument
	}

	internal partial class FindReplaceForm : Form
	{
		const string cstrClose = "&Close";
		const string cstrStop = "&Stop";

		private WordFindReplaceDocument m_doc;
		FindWordProcessor m_aWordByWordProcessor = null;
		protected SearchAreaType m_eSearchAreaType = SearchAreaType.eUnknown;

		public bool DifferentDoc(Word.Document doc)
		{
			return (doc == m_doc.Document);
		}

		public FindReplaceForm(WordFindReplaceDocument doc)
		{
			InitializeComponent();
			m_doc = doc;

			// progressBar.Maximum = m_doc.DocumentEnd;

			// make sure these exist so don't throw an error later when we try to add to them
			if (Properties.Settings.Default.RecentFindWhat == null)
				Properties.Settings.Default.RecentFindWhat = new System.Collections.Specialized.StringCollection();
			else
				foreach (string str in Properties.Settings.Default.RecentFindWhat)
					comboBoxFindWhat.Items.Add(str);

			if (Properties.Settings.Default.RecentReplaceWith == null)
				Properties.Settings.Default.RecentReplaceWith = new System.Collections.Specialized.StringCollection();
			else
				foreach (string str in Properties.Settings.Default.RecentReplaceWith)
					comboBoxReplaceWith.Items.Add(str);

#if BUILD_FOR_OFF14
			helpProvider.SetHelpString(this, SILConvertersOffice10.Properties.Resources.FindReplaceFormHelpString);
			helpProvider.SetHelpString(ecTextBoxFindWhat, SILConvertersOffice10.Properties.Resources.ecTextBoxFindWhatHelpString);
			helpProvider.SetHelpString(ecTextBoxReplaceWith, SILConvertersOffice10.Properties.Resources.ecTextBoxReplaceWithHelpString);
#elif BUILD_FOR_OFF12
			helpProvider.SetHelpString(this, SILConvertersOffice07.Properties.Resources.FindReplaceFormHelpString);
			helpProvider.SetHelpString(ecTextBoxFindWhat, SILConvertersOffice07.Properties.Resources.ecTextBoxFindWhatHelpString);
			helpProvider.SetHelpString(ecTextBoxReplaceWith, SILConvertersOffice07.Properties.Resources.ecTextBoxReplaceWithHelpString);
#else
			helpProvider.SetHelpString(this, Properties.Resources.FindReplaceFormHelpString);
			helpProvider.SetHelpString(ecTextBoxFindWhat, Properties.Resources.ecTextBoxFindWhatHelpString);
			helpProvider.SetHelpString(ecTextBoxReplaceWith, Properties.Resources.ecTextBoxReplaceWithHelpString);
#endif
		}

		protected void ProcessButton(FormButtons eFormButton)
		{
			try
			{
				if (backgroundWorker.IsBusy)
					throw new ApplicationException("Click the 'Stop' button to cancel the current search");
				ProcessButtonEx(eFormButton);
			}
			catch (Exception ex)
			{
				OfficeApp.DisplayException(ex);
			}
		}

		protected void ProcessButtonEx(FormButtons eFormButton)
		{
			if (String.IsNullOrEmpty(ecTextBoxFindWhat.Text))
				throw new ApplicationException("Enter a regular expression in the 'Find what' box");

			m_doc.Document.Application.System.Cursor = Microsoft.Office.Interop.Word.WdCursorType.wdCursorWait; // be sure to turn it off in backgroundWorker_RunWorkerCompleted

			// if there's no processor (e.g. initially, change of Find What or Replace With text)...
			if (m_aWordByWordProcessor == null)
			{
				AddToComboBox(ecTextBoxFindWhat, comboBoxFindWhat, Properties.Settings.Default.RecentFindWhat);

				// if the user clicked Find/Next, then don't give the Replace With text even if there is some
				m_aWordByWordProcessor = new FindWordProcessor(ecTextBoxFindWhat.Text, ecTextBoxReplaceWith.Text,
					checkBoxMatchCase.Checked, ecTextBoxFindWhat.Font);
			}

			// update the button pressed information
			m_aWordByWordProcessor.FormButton = eFormButton;

			// if we're doing a replacement, then save the 'Replace with' string in our settings file
			if ((eFormButton == FormButtons.ReplaceOnce) || (eFormButton == FormButtons.ReplaceAll))
			{
				AddToComboBox(ecTextBoxReplaceWith, comboBoxReplaceWith, Properties.Settings.Default.RecentReplaceWith);

				// the user may have done 'Replace' when it was found, but then later clicked said ReplaceAll,
				//  so update the value
				if (eFormButton == FormButtons.ReplaceAll)
				{
					m_aWordByWordProcessor.ReplaceAll = true;
					m_aWordByWordProcessor.NumOfReplacements = 0;
				}
			}

			// if the user manually repositioned the insertion point, then we have to start over.
			if (m_doc.HasPositionChanged)
			{
				// determine if this is a single selection or "the rest of the document" from the insertion point
				m_doc.DetermineRangeToSearch(out m_eSearchAreaType);
				System.Diagnostics.Debug.Assert(m_doc.theRangeToSearch != null);

				if (m_eSearchAreaType == SearchAreaType.eWholeDocument)
					progressBar.Maximum = m_doc.theRangeToSearch.End;
			}
			else if (m_aWordByWordProcessor.IsFound && (eFormButton == FormButtons.Next))
			{
				m_doc.BumpPastLastFound();
			}

			m_doc.theSearchProcessor = m_aWordByWordProcessor;

			progressBar.Visible = true; // be sure to turn it off during backgroundWorker_RunWorkerCompleted
			this.buttonCancel.Text = cstrStop;

			CallWorkerBee();
		}

		protected void CallWorkerBee()
		{
#if !DefineToTurnOffBackgroundProcessing
			backgroundWorker.RunWorkerAsync(m_doc);
#else
			DoWork(backgroundWorker, m_doc);
			backgroundWorker_RunWorkerCompleted(null, new RunWorkerCompletedEventArgs(null, null, false));
#endif
		}

		protected void DoWork(BackgroundWorker worker, WordFindReplaceDocument doc)
		{
			doc.Search(worker);
		}

		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;
			DoWork(worker, e.Argument as WordFindReplaceDocument);
			if (worker.CancellationPending)
				e.Cancel = true;
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			// for us the "progress" is a new range of the document we're searching thru
			int nValue = (int)e.ProgressPercentage;
			if (nValue > progressBar.Maximum)
				progressBar.Maximum = nValue;
			progressBar.Value = nValue;
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			bool bReplace = (m_aWordByWordProcessor.FormButton == FormButtons.ReplaceOnce) || (m_aWordByWordProcessor.FormButton == FormButtons.ReplaceAll);
			bool bContinue = false;  // start again pessimistic

			if (e.Error != null)
			{
				OfficeApp.DisplayException(e.Error);
			}

			// if nothing was found in the search area, then see if they want to start over again
			else if (!e.Cancelled && !m_aWordByWordProcessor.IsFound)
			{
				bool bSelection = (m_eSearchAreaType == SearchAreaType.eSelection);
				string strPrompt = null;
				if (bSelection)
				{
					if (bReplace)
						strPrompt = "Finished searching the selection. {0} replacement{1} made. Would you like to search the rest of the document?";
					else
						strPrompt = "Finished searching the selection. Would you like to search the rest of the document?";
				}
				else if (m_doc.theRangeToSearch.Start == 0)  // otherwise, whole document
				{
					if (bReplace)
					{
						strPrompt = String.Format("Finished searching the whole document. {0} replacement{1} made in all.",
							m_aWordByWordProcessor.NumOfReplacements,
							(m_aWordByWordProcessor.NumOfReplacements == 1) ? " was" : "s were");
					}
					else
						strPrompt = "Finished searching the whole document. The search item was not found.";

					MessageBox.Show(strPrompt, OfficeApp.cstrCaption, MessageBoxButtons.OK);

					// reset the state so we start over if the user clicks it again.
					m_doc.ResetPosition();
					goto ExitRoutine;
				}
				else    // otherwise, rest of the document
				{
					if (bReplace)
						strPrompt = "Finished searching the rest of the document. {0} replacement{1} made. Would you like to continue searching at the beginning of the document?";
					else
						strPrompt = "Finished searching the rest of the document. Would you like to continue searching at the beginning of the document?";
				}

				if (bReplace)
					strPrompt = String.Format(strPrompt, m_aWordByWordProcessor.NumOfReplacements,
						(m_aWordByWordProcessor.NumOfReplacements == 1) ? " was" : "s were");

				DialogResult res = MessageBox.Show(strPrompt, OfficeApp.cstrCaption, MessageBoxButtons.YesNoCancel);

				bool bReset = true; // most paths want us to reset.
				if (res == DialogResult.Cancel)
					this.Close();
				else if (res == DialogResult.Yes)
				{
					if (bSelection)
					{
						// start after where we just finished up (i.e. the end of the selection)
						m_doc.SearchAreaStart = m_doc.theRangeToSearch.End + 1;
						m_doc.ChangeToRestOfDocument(ref m_doc.theRangeToSearch, out m_eSearchAreaType);
						bReset = false;
					}
					else    // doing ReplaceAll from beginning
					{
						// make the end of the search go up to the beginning of the original search
						//  (if a) selection, b) rest, and c) beginning, then rangeToSearch.Start will
						//  be the beginnin go of the selection (which is what we want).
						m_doc.theRangeToSearch.End = m_doc.theRangeToSearch.Start;
						m_doc.theRangeToSearch.Start = 0;
					}

					bContinue = true;
				}

				if (bReset)
				{
					m_doc.ResetPosition();
				}
			}

			// do it again!
			if (bContinue)
			{
				CallWorkerBee();
				return;
			}

		ExitRoutine:
			this.buttonCancel.Text = cstrClose;
			progressBar.Visible = false;
			m_doc.Document.Application.System.Cursor = Microsoft.Office.Interop.Word.WdCursorType.wdCursorNormal;
		}

		public new void Show()
		{
			Word.Range aRange = m_doc.Document.Application.Selection.Words.First;
			this.ecTextBoxFindWhat.Text = aRange.Text;
			this.ecTextBoxFindWhat.Font = this.comboBoxFindWhat.Font =
				this.ecTextBoxReplaceWith.Font = this.comboBoxReplaceWith.Font =
				new Font(aRange.Font.Name, 12);
			base.Show();
		}

		private void contextMenuStripExprBuilder_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			string strItem = ((ToolStripItem)e.ClickedItem).Text;
			if (strItem != regularExpressionHelpToolStripMenuItem.Text)
			{
				int nIndex = strItem.IndexOf(' ');
				this.ecTextBoxFindWhat.SelectedText = strItem.Substring(0, nIndex);
			}
		}

		private void FindReplaceForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			// clear out the EncConverters object we created since we're probably done for a while (that
			//  is static so we don't have to keep buying it over and over again for simple changes in the
			//  expression).
			FindWordProcessor.m_aECs = null;
			ResetBackgroundWorker();
			Properties.Settings.Default.Save(); // in case something was changed
			e.Cancel = true;
			Hide();
		}

		protected void AddToComboBox(EcTextBox tb, ComboBox cb, System.Collections.Specialized.StringCollection sc)
		{
			string strText = tb.Text;
			if (!String.IsNullOrEmpty(strText))
			{
				if (cb.Items.Contains(strText))
					cb.Items.Remove(strText);

				cb.Items.Insert(0, strText);

				// also save some in the project config file for later recall
				if (sc.Contains(strText))
					sc.Remove(strText);

				sc.Insert(0, strText);
			}
		}

		private void buttonFindNext_Click(object sender, EventArgs e)
		{
			ProcessButton(FormButtons.Next);
		}

		private void buttonReplace_Click(object sender, EventArgs e)
		{
			ProcessButton(FormButtons.ReplaceOnce);
		}

		private void buttonReplaceAll_Click(object sender, EventArgs e)
		{
			ProcessButton(FormButtons.ReplaceAll);
		}

		private void ecTextBox_TextChanged(object sender, EventArgs e)
		{
			m_eSearchAreaType = SearchAreaType.eUnknown;
			m_aWordByWordProcessor = null;
		}

		private void buttonExpressionBuilder_Click(object sender, EventArgs e)
		{
			ToolStripDropDownDirection dir = ToolStripDropDownDirection.BelowRight;
			this.buttonExpressionBuilder.ContextMenuStrip.Show(PointToScreen(buttonExpressionBuilder.Location), dir);
		}

		private void comboBoxFindWhat_SelectedIndexChanged(object sender, EventArgs e)
		{
			ecTextBoxFindWhat.Text = (string)comboBoxFindWhat.SelectedItem;
		}

		private void comboBoxReplaceWith_SelectedIndexChanged(object sender, EventArgs e)
		{
			ecTextBoxReplaceWith.Text = (string)comboBoxReplaceWith.SelectedItem;
		}

		private void regularExpressionHelpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// launch the ICU help
			string strCommandLine = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + @"\SIL\Help\ICU Regular Expression Plug-in About box.htm";
			OfficeApp.LaunchProgram(strCommandLine, null);
		}

		private void checkBoxMatchCase_CheckedChanged(object sender, EventArgs e)
		{
			m_aWordByWordProcessor = null;
		}

		private void ResetBackgroundWorker()
		{
			if (this.backgroundWorker.IsBusy)
				this.backgroundWorker.CancelAsync();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			if (buttonCancel.Text == cstrStop)
				ResetBackgroundWorker();
			else
				this.Close();
		}
	}

	internal class FindWordProcessor : OfficeDocumentProcessor
	{
		public bool IsFound = false;
		public bool IsAnyCRs = false;
		public FormButtons FormButton = FormButtons.Cancel;
		protected char[] m_achDelimiter = new char[] { '\u001f' };
		public int NumOfParagraphsToSearch = 1;
		public int NumOfReplacements = 0;
		static public EncConverters m_aECs = null;
		protected IEncConverter m_aECRegex = null;
		protected string[] astrReplaceDoubleEscapeCodes = new string[] { @"\t", @"\r", @"\n", @"\a", @"\b", @"\f" };
		protected string[] astrReplaceEscapeCodes = new string[] { "\t", "\r", "\n", "\a", "\b", "\f" };

		public FindWordProcessor(string strFindWhat, string strReplaceWith, bool bMatchCase, Font font)
		{
			if (m_aECs == null)
				m_aECs = new EncConverters(true);

			// for some reason, the text that goes to constructing the 'Find what' part of the CRegexMatcher is
			//  expecting doubly-escaped text (e.g. "\\r" for CR), but the text that goes for the 'Replace with'
			//  (i.e. the CRegex::ReplaceAll) is expecting singly-escaped text. Our text boxes, however, will always
			//  return these as doubly-escaped. So for the 'Replace with' stuff, we need to turn certain doubly-
			//  escaped codes into their single-escaped flavors).
			int nNumEscapeCodes = astrReplaceDoubleEscapeCodes.Length;
			for (int i = 0; i < nNumEscapeCodes; i++)
				strReplaceWith = strReplaceWith.Replace(astrReplaceDoubleEscapeCodes[i], astrReplaceEscapeCodes[i]);

			m_aECRegex = InitSearchFontConverter(strFindWhat, strReplaceWith, font, bMatchCase);
			AutoReplaceOnNextFind = false;  // we'll take care of this as well

			// Normally, we search the text one paragraph at a time for the FindWhat string, but if the user
			//  has multiple "\r"s in the search string, it means we're supposed to find stuff beyond paragraph
			//  boundaries. So indicate how many paragraphs we should include in the Search based on the number
			//  of "\r"s in the search string...
			string[] strTokens = strFindWhat.Split(new string[] { @"\r\n", @"\r" }, StringSplitOptions.None);
			NumOfParagraphsToSearch = strTokens.Length;
			IsAnyCRs = (NumOfParagraphsToSearch > 1);

			// ... however, we don't need to get an extra paragraph if the \r occurs at the end of the FindWhat string
			if (String.IsNullOrEmpty(strTokens[NumOfParagraphsToSearch - 1]))
			{
				NumOfParagraphsToSearch--;
			}
		}

		// initialize an EncConverter which will tell us when we've hit a match. Uses ICU RegEx (though, there's
		//  no reason this couldn't be .Net regex...)
		protected IEncConverter InitSearchFontConverter(string strFindWhat, string strReplaceWith, Font font, bool bMatchCase)
		{
			// we're going to use a temporary ICU RegEx EncConverter to do our 'searching' for us.
			// get a blank ICU Regex converter that we can program with our FindWhat string
			// but, it may be null if ICU isn't installed
			System.Diagnostics.Debug.Assert(m_aECs != null);
			IEncConverter aIEC = null;
			try
			{
				aIEC = m_aECs.NewEncConverterByImplementationType(EncConverters.strTypeSILicuRegex);
			}
			catch (Exception ex)
			{
				throw new ApplicationException("ICU doesn't appear to have been installed. This feature won't work without ICU.", ex);
			}

			// we search for different things depending on whether it's "Find" (only) vs. "Find & Replace"
			// delimit the replacement string with something we can detect, which we wouldn't otherwise expect
			//  to find in user text.
			string strConverterSpec = String.Format("{0}->{2}{1}{2}{3}", strFindWhat, strReplaceWith,
				m_achDelimiter[0], (bMatchCase) ? " /i" : null);

			// Give it a friendly name that isn't likely to conflict with anything 'real' the user might name it.
			string strName = String.Format("{0} FindReplaceConverter", OfficeApp.cstrCaption);
			ConvType eConvType = ConvType.Unicode_to_Unicode;
			string strDummy = null;
			int nProcType = (int)ProcessTypeFlags.ICURegularExpression;

			// initialize it so it's ready and put it in the current repository object (as a temporary converter)
			aIEC.Initialize(strName, strConverterSpec, ref strDummy, ref strDummy, ref eConvType,
				ref nProcType, 0, 0, true);

			return aIEC;
		}

		public void ReplaceText(WordRange aRunRange, string strNewText)
		{
			try
			{
				aRunRange.Text = strNewText;
			}
			catch (Exception ex)
			{
				if (ex.Message == "The range cannot be deleted.")
				{
					aRunRange.End++;
					string strText = aRunRange.Text;
					if (strText[strText.Length - 1] == '\n')
					{
						throw new ApplicationException(String.Format("The paragraphs in this document end with both a carriage return (i.e. \\r) and a line feed character (i.e. \\n).{0}You need to change your 'Find what' string to include both (i.e. '\r\n'), or the found text can't be replaced.", Environment.NewLine));
					}
				}
			}
		}

		internal bool DontReplaceOnNextFind = false;
		public enum FindResult
		{
			eNothingFound,
			eFindFound,
			eReplaceFound
		}

		protected string[] GetFirstReplacementSplitArray(ref string strInput, ref string strOutput)
		{
			string[] astrSegments = strOutput.Split(m_achDelimiter);

			// must be odd (before, ||: replace, following :||), where any or all can be null
			if ((astrSegments.Length % 2) != 1)
				throw new ApplicationException("Oops... can't search in this document. Please send it to 'silconverters_support@sil.org' and indicate what the 'Find what' string was.");

			if (astrSegments.Length > 3)
			{
				int nInputLength = strInput.Length;
				int nDiffLength = nInputLength;
				int nTempInputLength = nInputLength;
				string strTempInput = null;
				do
				{
					// do a binary search until we get only one replacement
					nDiffLength = Math.Max(nDiffLength / 2, 1);
					if (astrSegments.Length > 3)
						nTempInputLength = Math.Max(nTempInputLength - nDiffLength, 1);
					else
						nTempInputLength = Math.Min(nTempInputLength + nDiffLength, nInputLength);

					strTempInput = strInput.Substring(0, nTempInputLength);
					strOutput = m_aECRegex.Convert(strTempInput);
					astrSegments = strOutput.Split(m_achDelimiter);
					System.Diagnostics.Trace.WriteLine(String.Format("{0}: diff:{1} segments:{2}", nTempInputLength, nDiffLength, astrSegments.Length));
				} while (astrSegments.Length != 3);

				System.Diagnostics.Trace.WriteLine(null);
				strInput = strTempInput;
			}

			return astrSegments;
		}

		// determine if this section of text (some portion of one or more paragraphs) has the find what text in it
		public FindResult FindReplaceCompare(WordRange aRunRange, ref int SearchAreaStart, ref int FoundAreaLength)
		{
			FindResult res = FindResult.eNothingFound;
			string strInput = aRunRange.Text;
			if (String.IsNullOrEmpty(strInput) || (m_aECRegex == null))
				return res;

			// otherwise 'convert' it to see if the 'Find what' string is in it
			string strOutput = m_aECRegex.Convert(strInput);

			// if the input string is different from the output string, then the FindWhat string was found.
			if (strInput != strOutput)
			{
#if !DefineToNotUseSplitAndConvert
				// The way the convert works is that it will replace each instance of the input string that matches
				//  the FindWhat syntax (i.e. there may be more than one replacement we have to deal with).
				// here's the problem: if there was more than one replacment, then I really can't tell what portion
				//  of the FindWhat text each replacement goes with. Consider the string "ababbbbbabb". If the 'Find
				//  what' string is "ab+", then it breaks up to: "ab", "abbbbb", and "abb", but if these three are
				//  right in a row and the replacement is "", then the resulting strOutput will be <delim><delim>...
				//  repeated 3 times. So there's no way to tell which portion of the input text corresponds to which
				//  portion of the output text. My original stab at it just treated them evenly and divide by the
				//  number of consecutive replacements... this works if the 'FindWhat' is something like "ab". But all
				//  bets are off if the user uses the "+" (eat-em-up) expression code.
				// I think the only solution is to always only deal with a single replacement... So... if we have more
				//  than one match in a particular output (which we can tell by the Length of astrSegments), do a binary
				//  search until we have only one.
				string[] astrSegments = GetFirstReplacementSplitArray(ref strInput, ref strOutput);

				// get the index to the first character of the replacement (which is the same as the first character
				//  of the 'Find what' string as well).
				int nIndex = astrSegments[0].Length;

				// remember this so we pick up here later
				SearchAreaStart += nIndex;
				if (nIndex > 0)
					aRunRange.Start = SearchAreaStart;

				// the replacement string is easy. It's just whatever's in the one'th element of the Split array.
				string strReplacementString = astrSegments[1];

				// There might be some stuff between the end of the replacement and the end of the input string.
				string strStuffFollowingMatch = astrSegments[2];   // may be null

				// get the index to the end of the 'Find what' string
				int nEndOfFindWhatSelection;
				if (String.IsNullOrEmpty(strStuffFollowingMatch))
					nEndOfFindWhatSelection = strInput.Length;
				else
					nEndOfFindWhatSelection = strInput.Length - strStuffFollowingMatch.Length;
#else
				// this could probably be done more elegantly with Split rather than what I do in the #else case
				string[] astrSegments = strOutput.Split(m_achDelimiter);

				// must be odd (before, ||: replace, following :||), where any or all can be null
				int nTotalSegments = astrSegments.Length;
				System.Diagnostics.Debug.Assert((nTotalSegments % 2) == 1);

				// get the index to the first character of the replacement (which is the same as the first character
				//  of the 'Find what' as well).
				int nIndex = astrSegments[0].Length;

				// remember this so we pick up here later
				SearchAreaStart += nIndex;
				if (nIndex > 0)
					aRunRange.Start = SearchAreaStart;

				// the replacement string is easy. It's just whatever's in the 1st element of the Split array.
				//  but we have to figure out what the 'Find what' text is so that we can select it (so we can replace
				//  it). This is not so easy, because it could be anything and not just a string of text like in a normal
				//  find.
				string strReplacementString = astrSegments[1];

				// Between the end of the first replacement and the beginning of the next (if multiple replacments)
				//  is a string which should match something in the original, which we can search for
				string strStuffFollowingMatch = astrSegments[2];   // may be null

				int nEndOfFindWhatSelection;
				if (String.IsNullOrEmpty(strStuffFollowingMatch))
				{
					// If the 'Find what' is repeated twice in a row, then the stuff in-between the two instances of
					//  replacement text will be null.
					// Detect this by looking at the length of the even number string array elements (2, 4, etc),
					//  which are the segments following the replacements. This tells us what we have to divide by
					//  to get the proportion for only one find.
					int nNumReplacmentsInARow = 1;
					int nNextReplacementIndex = 2;
					nTotalSegments--;
					while ((nNextReplacementIndex < nTotalSegments) && String.IsNullOrEmpty(astrSegments[nNextReplacementIndex]))
					{
						nNumReplacmentsInARow++;
						nNextReplacementIndex += 2;
					}

					if (nNextReplacementIndex < astrSegments.Length)
						strStuffFollowingMatch = astrSegments[nNextReplacementIndex];

					int numerator;
					if (String.IsNullOrEmpty(strStuffFollowingMatch))
						numerator = strInput.Length;
					else
						numerator = strInput.IndexOf(strStuffFollowingMatch, nIndex + 1);
					nEndOfFindWhatSelection = ((numerator - nIndex) / nNumReplacmentsInARow) + nIndex;
				}
				else
					nEndOfFindWhatSelection = strInput.IndexOf(strStuffFollowingMatch, nIndex + 1);

				/*
				int nIndex = strOutput.IndexOf(m_achDelimiter[0]);
				System.Diagnostics.Debug.Assert(nIndex != -1);
				SearchAreaStart += nIndex;
				if (nIndex > 0)
					aRunRange.Start = SearchAreaStart;

				int nEndOfReplacement = strOutput.IndexOf(m_achDelimiter[0], nIndex + 1);
				System.Diagnostics.Debug.Assert(nEndOfReplacement != -1);

				// the replacement string is what's between these two
				string strReplacementString = strOutput.Substring(nIndex + 1, (nEndOfReplacement - nIndex - 1));

				// now the complicated part. Between the end of the first replacement and the next
				//  one is a string which should match something in the original. But if the replacement
				//  were null, then it could be the very next character...
				// This also handles the situation where there may be several "found whats"
				int nNumReplacmentsInARow = 1;
				int nNextIndex = nEndOfReplacement + 1;
				while ((nNextIndex < strOutput.Length) && (strOutput[nNextIndex] == m_achDelimiter[0]))
				{
					nNumReplacmentsInARow++;
					nEndOfReplacement = strOutput.IndexOf(m_achDelimiter[0], nNextIndex + 1);
					nNextIndex = nEndOfReplacement + 1;
				}

				if (nNextIndex < strOutput.Length)
				{
					nEndOfReplacement = strOutput.IndexOf(m_achDelimiter[0], nNextIndex + 1);
					if (nEndOfReplacement == -1)
						nEndOfReplacement = strOutput.Length;
				}
				else if (nNextIndex == strOutput.Length)
					nNextIndex--;

				string strStuffFollowingMatch = strOutput.Substring(nNextIndex, nEndOfReplacement - nNextIndex);

				nEndOfFindWhatSelection = ((strInput.Length - nIndex) / nNumReplacmentsInARow) + nIndex;
				if (!String.IsNullOrEmpty(strStuffFollowingMatch))
				{
					nEndOfFindWhatSelection = strInput.IndexOf(strStuffFollowingMatch, nIndex + 1) / nNumReplacmentsInARow;
				}
				*/
#endif // !UseSplitToFindReplacements

				FoundAreaLength = nEndOfFindWhatSelection - nIndex;
				aRunRange.End = SearchAreaStart + FoundAreaLength;

				// if we're doing ReplaceAll or Replace when the FindWhat string has been found...
				System.Diagnostics.Debug.Assert(FormButton != FormButtons.Cancel);  // means it wasn't initialized
				if (!DontReplaceOnNextFind &&
					((ReplaceAll) || ((FormButton == FormButtons.ReplaceOnce) && (nIndex == 0))))
				{
					if (FormButton == FormButtons.ReplaceOnce)
						DontReplaceOnNextFind = true;  // so we do a virtual find next after this

					// this means replace the word in situ, with what was converted
					ReplaceText(aRunRange, strReplacementString);
					NumOfReplacements++;

					// start the next search after this replaced value
					string strReplacedText = aRunRange.Text;   // this may not be exactly the same as strReplace (e.g. after replacing final '\r')
					if (!String.IsNullOrEmpty(strReplacedText))
					{
						FoundAreaLength = strReplacedText.Length;
						SearchAreaStart += FoundAreaLength;
					}

					res = FindResult.eReplaceFound;
				}
				else
				{
					// select just the search string and return as if cancelled so the outer loop can prompt for it.
					IsFound = true;

					// select the FindWhat text found
					aRunRange.Select();

					res = FindResult.eFindFound;
				}
			}
			else if (FormButton == FormButtons.ReplaceOnce)
			{
				// otherwise, if the user clicked ReplaceOnce and we didn't find it right away,
				//  then change it to function like Find instead.
				DontReplaceOnNextFind = true;  // so we do a virtual find next after this
			}

			return res;
		}
	}

	internal class WordFindReplaceDocument : WordDocument
	{
		public FindWordProcessor theSearchProcessor;
		public Word.Range theRangeToSearch;

		public int SearchAreaStart = -1;
		protected int FoundAreaLength = -1;
		protected int nSelectionStart = -1;
		protected int nSelectionEnd = -1;
		protected bool m_bLocationChanged = false;
		protected Word.Paragraph m_paraSearch = null;

		public WordFindReplaceDocument(Word.Document doc)
			: base(doc, ProcessingType.eWordByWord)
		{
		}

		public void ChangeToRestOfDocument(ref Word.Range aRunRange, out SearchAreaType eType)
		{
			// i.e. rest of the document
			eType = SearchAreaType.eWholeDocument;

			// we only really want to make the End = end of the document, but querying the end of the document
			//  is a very expensive call, so save the current start, call 'WholeStory' (which is inexpensive, but
			//  selects the whole document), and then reset the original start
			SearchAreaStart = aRunRange.Start;
			aRunRange.WholeStory();
			aRunRange.Start = SearchAreaStart;
		}

		public void DetermineRangeToSearch(out SearchAreaType eType)
		{
			// this is either a selection chunk or "the rest of the document" from the insertion point
			theRangeToSearch = Document.Application.Selection.Range.Duplicate;

			// to determine if this is a selection or not, see if the start and end insertation points
			//  are the same (i.e. no selection).
			// but we really don't want this to be considered a selection, if it's just one word, so
			//  make sure that there are at least two words selected before calling it a selection
			// also, if the location hasn't changed since our last Find or Replace, then it's also
			//  not a selection.
			if ((theRangeToSearch.Start == theRangeToSearch.End)
				|| !m_bLocationChanged
				|| (theRangeToSearch.Text.Trim().IndexOf(' ') == -1))
			{
				ChangeToRestOfDocument(ref theRangeToSearch, out eType);
			}
			else
			{
				eType = SearchAreaType.eSelection;
			}

			// we call this function whenever the location has changed. In both cases, we then
			//  want to restart the search from the beginning paragraph of the range.
			m_paraSearch = null;
		}

		/// <summary>
		/// Use this method just prior to returning to Word execution so that we can then call
		/// HasPositionChanged just prior to resuming to search in order to detect the situation
		/// where the user manually changed the cursor location since the last time (and therefore
		/// we have to reset our "RangeToSearch")
		/// </summary>
		protected void RememberCurrentPosition()
		{
			nSelectionStart = Document.Application.Selection.Start;
			nSelectionEnd = Document.Application.Selection.End;
		}

		public bool HasPositionChanged
		{
			get
			{
				m_bLocationChanged = ((nSelectionStart != Document.Application.Selection.Start)
					|| (nSelectionEnd != Document.Application.Selection.End));

				return m_bLocationChanged || (theRangeToSearch == null);
			}
		}

		public void ResetPosition()
		{
			nSelectionStart = nSelectionEnd = SearchAreaStart = FoundAreaLength = -1;
			m_paraSearch = null;
			if (theSearchProcessor != null)
				theSearchProcessor.NumOfReplacements = 0;
		}

		public void BumpPastLastFound()
		{
			SearchAreaStart += FoundAreaLength;
		}

		public void Search(BackgroundWorker worker)
		{
			theSearchProcessor.DontReplaceOnNextFind = theSearchProcessor.IsFound = false;

			ProcessParagraphsFindAndReplace(theSearchProcessor, theRangeToSearch, worker);

			RememberCurrentPosition();
		}

		protected object offset1 = 1;

		protected WordRange GetStartingRange(Word.Range aRunRange)
		{
			// the search range might have a different start value than the paragraphs we're going to acquire
			if (m_paraSearch == null)
			{
				m_paraSearch = aRunRange.Paragraphs.First;
				System.Diagnostics.Debug.Assert(m_paraSearch != null);
			}

			Word.Range aParagraphRange = m_paraSearch.Range;
			System.Diagnostics.Debug.Assert(SearchAreaStart <= aParagraphRange.End);

			// if the end of the paragraph is exactly at the beginning of the search offset, then skip to
			//  the next paragraph (this could be the case if we were searching for "\r" on a previous 'Find')
			if (SearchAreaStart == aParagraphRange.End)
			{
				m_paraSearch = m_paraSearch.Next(ref offset1);
				if (m_paraSearch == null)
					return null; // done
				aParagraphRange = m_paraSearch.Range;
			}
			else if (SearchAreaStart > aParagraphRange.Start)
			{
				aParagraphRange.Start = SearchAreaStart;
			}

			return new WordRange(aParagraphRange);
		}

		public void ProcessParagraphsFindAndReplace(FindWordProcessor aWordProcessor, Word.Range theRangeToSearch,
			BackgroundWorker worker)
		{
			// get Range object we can manipulate for searching (i.e. don't muck with the original
			WordRange aRunRange = GetStartingRange(theRangeToSearch);
			if (aRunRange == null)
				return;

			// how many paragraphs ahead we have to look for a match
			int nEndRangeToSearch = -1;   // might change from loop to loop if we do replacements (so update inside loop)
			object nOffsetParagraph = aWordProcessor.NumOfParagraphsToSearch - 1;
			do
			{
				SearchAreaStart = aRunRange.Start;

				// this updates the progress bar
				worker.ReportProgress(SearchAreaStart);

				// if the search string contains multiple '\r's, then we have to look ahead that many paragraphs
				int SearchAreaEnd;
				nEndRangeToSearch = theRangeToSearch.End;   // might change from loop to loop if we do replacements
				if (0 < (int)nOffsetParagraph)
				{
					Word.Paragraph aEndParagraph = m_paraSearch.Next(ref nOffsetParagraph);
					if (aEndParagraph == null)
						return; // means we can't possibly find it now
					SearchAreaEnd = Math.Min(aEndParagraph.Range.End, nEndRangeToSearch);
					aRunRange.End = SearchAreaEnd;
				}
				else
				{
					int nEndOfRun = aRunRange.End;
					if (nEndOfRun > nEndRangeToSearch)
					{
						aRunRange.End = SearchAreaEnd = nEndRangeToSearch;
					}
					else
						SearchAreaEnd = nEndOfRun;
				}

				// loop until the end of the paragraph and process it as a whole unit (we may have to do this multiple times)
				bool bGoToNext = true;
				while (SearchAreaStart < SearchAreaEnd)
				{
					// keep track of the last index so we can tell whether anything was found or not
					FindWordProcessor.FindResult res = aWordProcessor.FindReplaceCompare(aRunRange, ref SearchAreaStart, ref FoundAreaLength);

					// if we found it and need to stop (or if the user had cancelled the search)...
					bGoToNext = true;   // assume we will
					if ((res == FindWordProcessor.FindResult.eFindFound) || worker.CancellationPending)
						return;

					// otherwise, it might have been a replacement and we have to update the end of the paragraph
					//  value and the end of the range and do it again (e.g. find the next occurrence to replace)
					else if (res == FindWordProcessor.FindResult.eReplaceFound)
					{
						// we might have actually removed the paragraph mark, in which case the next paragraph
						//  is now part of the current paragraph. Unfortunately, the aParagraphRange isn't
						//  updated.
						// if the search string contained '\r's, then we have to look ahead all over again
						if (aWordProcessor.IsAnyCRs)
						{
							bGoToNext = false;
							break;
						}
						else
						{
							Word.Range aParagraphRange = m_paraSearch.Range;

							SearchAreaEnd = aParagraphRange.End;
							aRunRange.End = SearchAreaEnd;
							aRunRange.Start = SearchAreaStart;
						}
					}

					// otherwise, it means we didn't find it here and we're done with this paragraph
					else
					{
						System.Diagnostics.Debug.Assert(!aWordProcessor.IsFound);
						break;
					}
				}

				// advance to the next paragraph
				if (bGoToNext)
					m_paraSearch = m_paraSearch.Next(ref offset1);

				if (m_paraSearch != null)
				{
					aRunRange = new WordRange(m_paraSearch.Range);

					// in the case we replace some CRs, we re-start where we left off (after the replaced text)
					if (!bGoToNext)
						aRunRange.Start = SearchAreaStart;
				}

			} while ((m_paraSearch != null) && (aRunRange.Start < nEndRangeToSearch));
		}
	}
}