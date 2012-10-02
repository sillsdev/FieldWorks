using System;
using System.Collections.Generic;
using System.Text;
using Office = Microsoft.Office.Core;
using Pub = Microsoft.Office.Interop.Publisher;
using System.Windows.Forms;                     // for DialogResult
using System.Drawing;                           // for Font

namespace SILConvertersOffice
{
	internal class PubDocument : OfficeTextDocument
	{
		public PubDocument(Pub.Document doc, ProcessingType eType)
			: base(doc, eType)
		{
		}

		public Pub.Document Document
		{
			get { return (Pub.Document)m_baseDocument; }
		}

		public override int WordCount
		{
			// no '.Words.Count' in Publisher, so make up something
			get
			{
				char[] achsWhiteSpace = new char[] { ' ' };
				int nRoughWordCount = 0;
				foreach (Pub.Story aStory in Document.Stories)
				{
					PubStory pubStory = new PubStory(aStory);
					foreach (PubRange aRange in pubStory.Paragraphs)
					{
						string strParagraphText = aRange.Text;
						string [] astrWords = strParagraphText.Split(achsWhiteSpace);
						nRoughWordCount += astrWords.Length;
					}
				}
				return nRoughWordCount;
			}
		}

		public override string SelectedText
		{
			get { return SelectionRange.Text; }
		}

		public override bool IsSelection
		{
			get
			{
				Pub.Selection aSelection = Document.Selection;
				return (aSelection.TextRange.Start != aSelection.TextRange.End);
			}
		}

		public override OfficeRange SelectionRange
		{
			get { return new PubRange(Document.Selection.TextRange); }
		}

		public override bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor, ProcessingType eType)
		{
			Pub.Stories aStories = Document.Stories;
			int nStoryId = 1;
			if (aWordProcessor.AreLeftOvers)
			{
				DialogResult res = MessageBox.Show("Click 'Yes' to restart where you left off, 'No' to start over at the top, and 'Cancel' to quit", OfficeApp.cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.No)
					aWordProcessor.LeftOvers = null;
				else if (res == DialogResult.Cancel)
					return true;
				else
				{
					System.Diagnostics.Debug.Assert(res == DialogResult.Yes);
					PubRange rngPub = (PubRange)aWordProcessor.LeftOvers;
					nStoryId = rngPub.StoryID;
				}
			}

			for (; nStoryId <= aStories.Count; nStoryId++)
			{
				Pub.Story aStory = aStories[nStoryId];

				bool bResult = ProcessStory(aWordProcessor, aStory);

				if (aWordProcessor.AreLeftOvers)
				{
					PubRange rngPub = (PubRange)aWordProcessor.LeftOvers;
					rngPub.StoryID = nStoryId;  // remember which story we were doing when the user cancelled
				}

				if (!bResult)
					return false;

				aWordProcessor.ReplaceAll = false;  // stop after each story
			}

			return true;
		}

		protected bool ProcessStory(OfficeDocumentProcessor aWordProcessor, Pub.Story aStory)
		{
			PubStory aPubStory = new PubStory(aStory);
			foreach (PubRange aParagraphRange in aPubStory.Paragraphs)
			{
				// if we're picking up where we left off and we're not there yet...
				int nCharIndex = 0;
				if (aWordProcessor.AreLeftOvers)
				{
					if (aWordProcessor.LeftOvers.Start > aParagraphRange.End)
						continue;   // skip to the next paragraph

					nCharIndex = aWordProcessor.LeftOvers.StartIndex;
					aWordProcessor.LeftOvers = null; // turn off "left overs"
				}

				if (!ProcessRangeAsWords(aWordProcessor, aParagraphRange, nCharIndex))
					return false;
			}

			// see if the user would like us to adjust the size to make it fit.
			if (    !aWordProcessor.SuspendUI
				&&  (aStory.HasTextFrame == Office.MsoTriState.msoTrue)
				&&  (aStory.TextFrame.Overflowing == Office.MsoTriState.msoTrue))
			{
				if (MessageBox.Show("The story has overflowed the text frame. Would you like me to adjust the text size so it fits?", OfficeApp.cstrCaption, MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					try
					{
						aStory.TextFrame.AutoFitText = Pub.PbTextAutoFitType.pbTextAutoFitShrinkOnOverflow;
					}
					catch (Exception ex)
					{
						// this error is occasionally thrown when we try to do the auto fit... if it happens, just ignore it.
						// (it's just to save the user from having to reformat)
						if (ex.Message != "Error HRESULT E_FAIL has been returned from a call to a COM component.")
							throw;
					}
				}
			}

			return true;
		}

		protected bool ProcessParagraphs(OfficeDocumentProcessor aWordProcessor, PubParagraphs aPubParagraphs)
		{
			foreach (PubRange aParagraphRange in aPubParagraphs)
			{
				int nCharIndex = 0;
				if (!ProcessRangeAsWords(aWordProcessor, aParagraphRange, nCharIndex))
					return false;
			}

			return true;
		}

		protected override OfficeRange Duplicate(OfficeRange aParagraphRange)
		{
			return new PubRange((PubRange)aParagraphRange);
		}
		/*
		protected bool ProcessParagraph(OfficeDocumentProcessor aWordProcessor, PubRange aParagraphRange, int nCharIndex)
		{
			string strText = aParagraphRange.Text;
			int nLength = (strText != null) ? strText.Length : 0;

			while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achParagraphTerminators, nCharIndex, 1) == -1))
			{
				// get a copy of the range to work with
				PubRange aWordRange = new PubRange(aParagraphRange);

				// skip past initial spaces
				while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achWhiteSpace, nCharIndex, 1) != -1))
					nCharIndex++;

				// make sure we haven't hit the end of the paragraph (i.e. '\r')
				if (nCharIndex < nLength)
				{
					if (strText.IndexOfAny(m_achParagraphTerminators, nCharIndex, 1) == -1)
					{
						// set the start index
						aWordRange.StartIndex = nCharIndex;

						// run through the characters in the word (i.e. until, space, NL, etc)
						while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achWordTerminators, nCharIndex, 1) == -1))
							nCharIndex++;

						// set the end of the range after the first space after the word
						aWordRange.EndIndex = ++nCharIndex;

						// make sure the word has text (sometimes it doesn't)
						if (aWordRange.Text == null)  // e.g. Figure "1" returns a null Text string
							continue;

						// finally check it.
						if (!aWordProcessor.Process(aWordRange, ref nCharIndex))
						{
							aWordProcessor.LeftOvers = aWordRange;
							return false;
						}
					}

					strText = aParagraphRange.Text; // incase of replace, we've changed it.
					nLength = (strText != null) ? strText.Length : 0;
				}
				else
					break;
			}

			return true;
		}
	*/
	}

	/// <summary>
	/// This is a Publisher document which is just one "Story" (for 'the selected Story' handlers)
	/// </summary>
	internal class PubStoryDocument : PubDocument
	{
		public PubStoryDocument(Pub.Document doc, OfficeTextDocument.ProcessingType eType)
			: base(doc, eType)
		{
		}

		public string StoryName
		{
			// I'm not 100% sure this is legitimate, but nothing in the
			//  Document.Selection.TextRange.Story object seems to be a unique identifier of a Story object
			get { return Document.Selection.ShapeRange.Name; }
		}

		public override bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor)
		{
			if (aWordProcessor.AreLeftOvers)
			{
				DialogResult res = MessageBox.Show("Click 'Yes' to restart where you left off, 'No' to start over at the top, and 'Cancel' to quit", OfficeApp.cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.No)
					aWordProcessor.LeftOvers = null;
				if (res == DialogResult.Cancel)
					return true;
			}

			if (Document.Selection.Type == Pub.PbSelectionType.pbSelectionNone)
				throw new ApplicationException("No story selected!");

			return ProcessStory(aWordProcessor, Document.Selection.TextRange.Story);
		}
	}

	/// <summary>
	/// This is a Publisher document which is just the Selected Text (might be more than 1 paragraph)
	/// </summary>
	internal class PubRangeDocument : PubDocument
	{
		public PubRangeDocument(Pub.Document doc, OfficeTextDocument.ProcessingType eType)
			: base(doc, eType)
		{
		}

		public override bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor)
		{
			if (Document.Selection.Type == Pub.PbSelectionType.pbSelectionNone)
				throw new ApplicationException("Nothing selected!");

			PubRange aSelectionRange = (PubRange)SelectionRange;
			PubParagraphs aParagraphs = aSelectionRange.Paragraphs;
			if (aParagraphs.Count > 1)
				return ProcessParagraphs(aWordProcessor, aParagraphs);
			else
			{
				int nCharIndex = 0;
				return aWordProcessor.Process(aSelectionRange, ref nCharIndex);
			}
		}
	}
}
