using System;
using System.Collections.Generic;
using System.Text;
using Office = Microsoft.Office.Core;
using Pub = Microsoft.Office.Interop.Publisher;
using System.Windows.Forms;                     // for DialogResult
using System.Drawing;                   // for Font

namespace SILConvertersOffice
{
	internal class PubApp : OfficeApp
	{
		public PubApp(object app)
			: base(app)
		{
		}

		public new Pub.Application Application
		{
			get { return (Pub.Application)base.Application; }
		}

#if BUILD_FOR_OFF14
		public override string GetCustomUI()
		{
			return SILConvertersOffice10.Properties.Resources.RibbonPublisher;
		}
#elif BUILD_FOR_OFF12
		public override string GetCustomUI()
		{
			return SILConvertersOffice07.Properties.Resources.RibbonPublisher;
		}
#endif

#if BUILD_FOR_OFF14
#else
		// still using this menuing technique for 2007 (but using the Ribbon for 2010 ff)
		private Office.CommandBarButton WholeDocumentConvertBtn;
		private Office.CommandBarButton ThisStoryConvertBtn;
		private Office.CommandBarButton SelectionConvertBtn;
		private Office.CommandBarButton ResetMenuBtn;

		public override void LoadMenu()
		{
			base.LoadMenu();

			try
			{
				AddMenu(ref WholeDocumentConvertBtn, NewMenuBar, "Convert &Whole Document",
					"Click this item to convert the entire document",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(WholeDocumentConvert_Click));

				AddMenu(ref ThisStoryConvertBtn, NewMenuBar, "Convert Selected S&tory",
					"Click this item to convert the selected story only",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(ThisStoryConvert_Click));

				AddMenu(ref SelectionConvertBtn, NewMenuBar, "Convert &Selection",
					"Click this item to convert the selected text only",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(SelectionConvert_Click));

				AddMenu(ref ResetMenuBtn, NewMenuBar, "&Reset",
					"Reset the unfinished conversion processes",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(Reset_Click));
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}
#endif

		bool HookDocumentClose(Pub.Document doc)
		{
			if (doc != null)
			{
				Pub.DocumentEvents_Event docEvents = (Pub.DocumentEvents_Event)doc;
				docEvents.BeforeClose += new Microsoft.Office.Interop.Publisher.DocumentEvents_BeforeCloseEventHandler(docEvents_BeforeClose);
				return true;
			}
			return false;
		}

		void docEvents_BeforeClose(ref bool Cancel)
		{
			Reset();
		}

		OfficeDocumentProcessor m_aWordByWordFontProcessor = null;

#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void WholeDocumentConvert_Click(Office.IRibbonControl control)
		{
			WholeDocumentConvert();
		}
#endif

#if BUILD_FOR_OFF14
#else
		void WholeDocumentConvert_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			WholeDocumentConvert();
		}
#endif

		protected void WholeDocumentConvert()
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			try
			{
				PubDocument doc = new PubDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				// we want to process the file word by word and show differences between
				//  the Input and (once) Converted string
				if (m_aWordByWordFontProcessor == null)
				{
					// first get the fonts the user wants to process
					FontConvertersPicker aFCsPicker = new FontConvertersPicker(doc);
					if ((aFCsPicker.ShowDialog() == DialogResult.OK) && (aFCsPicker.SelectedFontConverters.Count > 0))
					{
						FontConverters aFCs = aFCsPicker.SelectedFontConverters;
						m_aWordByWordFontProcessor = GetDocumentProcessor(aFCs, new SILConverterProcessorForm());
					}
				}

				if (m_aWordByWordFontProcessor != null)
					if (doc.ProcessWordByWord(m_aWordByWordFontProcessor))
						m_aWordByWordFontProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
				if ((m_aWordByWordFontProcessor != null) && !m_aWordByWordFontProcessor.AreLeftOvers)
					m_aWordByWordFontProcessor = null;
			}
		}

		string m_strLastStoryName = null;   // so we can detect if the user changes stories (in which case, we need to start over)
		OfficeDocumentProcessor m_aThisStoryWordByWordFontProcessor = null;

#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void ThisStoryConvert_Click(Office.IRibbonControl control)
		{
			ThisStoryConvert();
		}
#endif

#if BUILD_FOR_OFF14
#else
		void ThisStoryConvert_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			ThisStoryConvert();
		}
#endif

		void ThisStoryConvert()
		{
			if (!HookDocumentClose(Application.ActiveDocument))
				return;

			try
			{
#if DEBUG
				string strPubPIOVer = Application.Version;
				MessageBox.Show(String.Format("MSPub PIA version: {0}", strPubPIOVer));
#endif
				PubStoryDocument doc = new PubStoryDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);

				// we want to process the file word by word and show differences between
				//  the Input and (once) Converted string
				if ((m_aThisStoryWordByWordFontProcessor == null) || (m_strLastStoryName != doc.StoryName))
				{
					m_aThisStoryWordByWordFontProcessor = null; // just in case we came thru the latter OR case

					// first get the fonts the user wants to process
					FontConvertersPicker aFCsPicker = new FontConvertersPicker(doc);
					if ((aFCsPicker.ShowDialog() == DialogResult.OK) && (aFCsPicker.SelectedFontConverters.Count > 0))
					{
						FontConverters aFCs = aFCsPicker.SelectedFontConverters;
						m_aThisStoryWordByWordFontProcessor = GetDocumentProcessor(aFCs, new SILConverterProcessorForm());
					}
				}

				if (m_aThisStoryWordByWordFontProcessor != null)
				{
					if (doc.ProcessWordByWord(m_aThisStoryWordByWordFontProcessor))
					{
						m_aThisStoryWordByWordFontProcessor = null;
						m_strLastStoryName = null;
					}
					else
					{
						m_strLastStoryName = doc.StoryName;
					}
				}
			}
			catch (Exception ex)
			{
				DisplayException(ex);
				if ((m_aThisStoryWordByWordFontProcessor != null) && !m_aThisStoryWordByWordFontProcessor.AreLeftOvers)
					m_aThisStoryWordByWordFontProcessor = null;
			}
		}

#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void SelectionConvert_Click(Office.IRibbonControl control)
		{
			SelectionConvert();
		}
#endif

#if BUILD_FOR_OFF14
#else
		void SelectionConvert_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			SelectionConvert();
		}
#endif

		protected void SelectionConvert()
		{
			if (Application.ActiveDocument == null)
				return;

			try
			{
				PubRangeDocument doc = new PubRangeDocument(Application.ActiveDocument, OfficeTextDocument.ProcessingType.eWordByWord);
				OfficeDocumentProcessor aSelectionProcessor = GetDocumentProcessor((FontConverters)null, new SILConverterProcessorForm());

				doc.ProcessWordByWord(aSelectionProcessor);
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}

#if BUILD_FOR_OFF12 || BUILD_FOR_OFF14
		public void Reset_Click(Office.IRibbonControl control)
		{
			Reset();
		}
#endif

#if BUILD_FOR_OFF14
#else
		void Reset_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			Reset();
		}
#endif

		void Reset()
		{
			m_aWordByWordFontProcessor = null;
			m_aThisStoryWordByWordFontProcessor = null;
		}

		protected bool IsPublisher2003
		{
			get { return (Application.Version == "11.0"); }
		}

		// the first publisher to support ribbons
		public bool IsPublisher2007
		{
			get { return (Application.Version == "12.0"); }
		}

		protected OfficeDocumentProcessor GetDocumentProcessor(FontConverters aFCs, BaseConverterForm form)
		{
			return (IsPublisher2003) ?
					new OfficeDocumentProcessor(aFCs, form) :   // Publisher 2003
					new PubDocumentProcessor(aFCs, form);       // Publisher 2007/2010
		}
	}

	/// <summary>
	/// Must override the Office Document Processor for Publisher, because of the difference in behaviour
	/// of the TextRange class between Pub 2003 and 2007, which requires me to re-write ReplaceText.
	/// </summary>
	internal class PubDocumentProcessor : OfficeDocumentProcessor
	{
		public PubDocumentProcessor(FontConverters aFCs, BaseConverterForm form)
			: base(aFCs, form)
		{
		}

		public override void ReplaceText(OfficeRange aWordRange, Font fontTarget, ref int nCharIndex, string strNewText)
		{
			PubRange thisRange = (PubRange)aWordRange;
			thisRange.ReplaceText(strNewText);

			if (fontTarget != null)
				SetRangeFont(aWordRange, fontTarget.Name);

			nCharIndex = aWordRange.EndIndex;
		}
	}
}
