using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;             // for DialogResult
using System.Drawing;                   // for Font
using System.Reflection;                // for InvokeMember
using Office = Microsoft.Office.Core;
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
	internal abstract class OfficeApp
	{
		protected object m_app = null;
		protected object missing = Type.Missing;
		protected Office.CommandBar NewMenuBar = null;
		internal const string cstrMenuTitle = "SIL Converters";

		protected OfficeApp(object app)
		{
			m_app = app;
		}

		public virtual object Application
		{
			get { return m_app; }
		}

		public virtual void LoadMenu()
		{
			Office.CommandBars oCommandBars = null;

			try
			{
				oCommandBars = (Office.CommandBars)m_app.GetType().InvokeMember("CommandBars", BindingFlags.GetProperty, null, m_app, null);
			}
			catch (Exception)
			{
			}

			try
			{
				NewMenuBar = oCommandBars[cstrMenuTitle];
			}
			catch (Exception)
			{
				// doesn't exist yet, so create it
				NewMenuBar = oCommandBars.Add(cstrMenuTitle, 1, missing, true);
				NewMenuBar.Visible = true;
			}

			ReleaseComObject(oCommandBars);
		}

		protected void AddMenu(ref Office.CommandBarButton btn, Office.CommandBarPopup popup,
			string strCaption, string strTooltip,
			Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler handler)
		{
			btn = (Office.CommandBarButton)popup.Controls.Add(Office.MsoControlType.msoControlButton, missing, missing, missing, true);
			AddMenu(btn, strCaption, strTooltip, handler);
		}

		protected void AddMenu(ref Office.CommandBarButton btn, Office.CommandBar bar,
			string strCaption, string strTooltip,
			Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler handler)
		{
			btn = (Office.CommandBarButton)bar.Controls.Add(Office.MsoControlType.msoControlButton, missing, missing, missing, true);
			AddMenu(btn, strCaption, strTooltip, handler);
		}

		protected void AddMenu(Office.CommandBarButton btn, string strCaption, string strTooltip,
			Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler handler)
		{
			btn.Style = Microsoft.Office.Core.MsoButtonStyle.msoButtonCaption;
			btn.Caption = strCaption;
			btn.Tag = strCaption;
			btn.TooltipText = strTooltip;
			btn.Click += handler;

			// this setting, along with a registry key of DemandLoad = 8 (or 9), causes us not to be
			//  loaded until the user actually requests us.
#if DEBUG   // when debugging, we go directly with the .Net assembly (rather than the shim)
			btn.OnAction = "!<SILConvertersOffice.Connect>";
#else
			btn.OnAction = "!<SILConvertersOfficeShim.Connect>";
#endif
		}

		internal static void DisplayException(Exception ex)
		{
			if (ex.Message != Connect.cstrAbortMessage)
			{
				string strMessage = ex.Message;
				if (ex.InnerException != null)
					strMessage += String.Format("{0}{0}[cause: {1}]", Environment.NewLine, ex.InnerException.Message);

				MessageBox.Show(strMessage, Connect.cstrCaption);
			}
		}

		// needed or Access stays running after exit.
		// for access only (it appears) you have to release *every* access object you use explicitly
		//  you can't even say "Application.CurrentDb().TableDefs"
		//  because that will cause the CurrentDb to leak. Instead, you have to say:
		// dao.Database aDb = Application.CurrentDb();
		// dao.TablesDefs aTDs = aDb.TableDefs;...
		// you also can't use 'foreach' because then even if you explicitly try to release
		//  the object, it still hangs... must use "for" loop with an integer index on the Count
		internal static void ReleaseComObject(object obj)
		{
			if (obj == null)
				return;

			try
			{
				System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
			}
			catch
			{
			}
			finally
			{
				obj = null;
			}
		}

		public virtual void Release()
		{
			UnloadMenu();
			ReleaseComObject(m_app);
		}

		// all sub-classes must gracefully remove their buttons
		public virtual void UnloadMenu()
		{
			ReleaseComObject(NewMenuBar);
		}

		internal static EncConverters GetEncConverters
		{
			get
			{
				try
				{
					return DirectableEncConverter.EncConverters;
				}
				catch (Exception ex)
				{
					DisplayException(ex);
				}
				return null;
			}
		}
	}

	/// <summary>
	/// This class defines the interface for a "Range" object, but in a way that
	/// is more friendly towards normal .Net implementations (e.g. the Start/End references are
	/// zero-based from the Range upon which it is based, the Font returned is a .Net Font, etc).
	/// This class is sub-classed by Word.Range, Pub.TextRange, etc.
	/// </summary>
	internal abstract class OfficeRange
	{
		protected object m_aRangeBasedOn = null;
		protected Type m_typeRange = null;
		protected int m_nOffset;

		public OfficeRange(object basedOnRange)
		{
			m_typeRange = basedOnRange.GetType();
			m_aRangeBasedOn = GetProperty(basedOnRange, "Duplicate");
		}

		public OfficeRange()    // some sub-classes don't use this scheme (e.g. Access)
		{
		}

		public virtual int Start
		{
			get { return (int)GetProperty("Start"); }
			set { SetProperty("Start", value ); }
		}

		public virtual int StartIndex
		{
			get { return Start - m_nOffset; }
			set { Start = value + m_nOffset; }
		}

		public virtual int End
		{
			get { return (int)GetProperty("End"); }
			set { SetProperty("End", value); }
		}

		public virtual int EndIndex
		{
			get { return End - m_nOffset; }
			set { End = value + m_nOffset; }
		}

		// not working for publisher
		public virtual void Select()
		{
			if( m_typeRange != null )
				m_typeRange.InvokeMember("Select", BindingFlags.InvokeMethod, null, m_aRangeBasedOn, null);
		}

		public abstract string Text
		{
			get;
			set;
		}

		public virtual string FontName
		{
			get
			{
				// use MajorityFont just in case it isn't totally there
				object font = GetProperty("Font");
				return (string)GetProperty(font, "Name");
			}
			set
			{
				object font = GetProperty("Font");
				if( m_typeRange != null )
					m_typeRange.InvokeMember("Name", BindingFlags.SetProperty, null, font, new object[] { value });
			}
		}

		protected void SetProperty(string strPropertyName, object value)
		{
			System.Diagnostics.Debug.Assert(m_aRangeBasedOn != null);
			if( m_typeRange != null )
				m_typeRange.InvokeMember(strPropertyName, BindingFlags.SetProperty, null, m_aRangeBasedOn, new object[] { value });
		}

		protected object GetProperty(string strPropertyName)
		{
			System.Diagnostics.Debug.Assert(m_aRangeBasedOn != null);
			if (m_typeRange != null)
				return GetProperty(m_aRangeBasedOn, strPropertyName);
			else
				return null;
		}

		protected object GetProperty(object obj, string strPropertyName)
		{
			if (m_typeRange != null)
				return m_typeRange.InvokeMember(strPropertyName, BindingFlags.GetProperty, null, obj, null);
			else
				return null;
		}

		public virtual void DealWithNullText()  // give Word the opportunity to fixup a null text thing
		{
		}
	}

	internal abstract class OfficeDocument
	{
		protected object m_baseDocument = null;

		protected const char chTab = '\t';
		protected const char chNL = '\r';
		protected const char chFootnote = '\u0002';
		protected const char chSpace = ' ';
		protected const char chFormFeed = '\f';
		protected const char chCellBreak = '\u0007';
		protected const char chInlineGraphics = '\u0001';

		protected static char[] m_achParagraphTerminators = new char[] { chNL, chFormFeed };
		protected static char[] m_achWhiteSpace = new char[] { chSpace, chTab, chFootnote };
		protected static char[] m_achWordTerminators = new char[] { chSpace, chTab, chNL, chFootnote };

		public OfficeDocument(object doc)
		{
			m_baseDocument = doc;
		}

		public abstract int WordCount
		{
			get;
		}

		public abstract bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor);
	}

	internal abstract class OfficeTextDocument : OfficeDocument
	{
		protected ProcessingType m_eType = ProcessingType.eWordByWord;
		public enum ProcessingType
		{
			eWordByWord,
			eIsoFormattedRun
		}

		public OfficeTextDocument(object doc, ProcessingType eType)
			: base(doc)
		{
			m_eType = eType;
		}

		public abstract string SelectedText
		{
			get;
		}

		public abstract bool IsSelection
		{
			get;
		}

		public abstract OfficeRange SelectionRange
		{
			get;
		}

		public override bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor)
		{
			return ProcessWordByWord(aWordProcessor, m_eType);
		}

		protected abstract OfficeRange Duplicate(OfficeRange aParagraphRange);
		public abstract bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor, ProcessingType eType);

		protected bool ProcessRangeAsWords(OfficeDocumentProcessor aWordProcessor, OfficeRange aParagraphRange, int nCharIndex)
		{
			string strText = aParagraphRange.Text;
			int nLength = (strText != null) ? strText.Length : 0;

			while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achParagraphTerminators, nCharIndex, 1) == -1))
			{
				// get a copy of the range to work with
				OfficeRange aWordRange = Duplicate(aParagraphRange);

				// skip past initial spaces
				while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achWhiteSpace, nCharIndex, 1) != -1))
					nCharIndex++;

				// make sure we haven't hit the end of the paragraph (i.e. '\r') or range (beyond len)
				if (nCharIndex < nLength)
				{
					if (strText.IndexOfAny(m_achParagraphTerminators, nCharIndex, 1) == -1)
					{
						// set the start index
						aWordRange.StartIndex = nCharIndex;

						// run through the characters in the word (i.e. until, space, NL, etc)
						while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achWordTerminators, nCharIndex, 1) == -1))
							nCharIndex++;

						// set the end of the range after the first space after the word (but not for NL)
						if (++nCharIndex >= nLength)
							--nCharIndex;

						aWordRange.EndIndex = nCharIndex;

						// make sure the word has text (sometimes it doesn't)
						if (aWordRange.Text == null)  // e.g. Figure "1" returns a null Text string
							// if it does, see if it's "First Character" has any text (which it does in this case)
							aWordRange.DealWithNullText();

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

		protected bool ProcessWholeRange(OfficeDocumentProcessor aWordProcessor, OfficeRange aParagraphRange, int nCharIndex)
		{
			string strText = aParagraphRange.Text;
			int nLength = (strText != null) ? strText.Length : 0;

			while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achParagraphTerminators, nCharIndex, 1) == -1))
			{
				// get a copy of the range to work with
				OfficeRange aWordRange = Duplicate(aParagraphRange);

				// skip past initial spaces
				while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achWhiteSpace, nCharIndex, 1) != -1))
					nCharIndex++;

				// make sure we haven't hit the end of the paragraph (i.e. '\r') or range (beyond len)
				if (nCharIndex < nLength)
				{
					if (strText.IndexOfAny(m_achParagraphTerminators, nCharIndex, 1) == -1)
					{
						// set the start index
						aWordRange.StartIndex = nCharIndex;

						// run through the characters in the word (i.e. until, space, NL, etc)
						while ((nCharIndex < nLength) && (strText.IndexOfAny(m_achWordTerminators, nCharIndex, 1) == -1))
							nCharIndex++;

						// set the end of the range after the first space after the word (but not for NL)
						if (++nCharIndex >= nLength)
							--nCharIndex;

						aWordRange.EndIndex = nCharIndex;

						// make sure the word has text (sometimes it doesn't)
						if (aWordRange.Text == null)  // e.g. Figure "1" returns a null Text string
							// if it does, see if it's "First Character" has any text (which it does in this case)
							aWordRange.DealWithNullText();

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
	}

	internal class OfficeDocumentProcessor
	{
		public delegate bool ProcessWord(OfficeRange aWordRange, ref int nCharIndex);
		private ProcessWord m_aWordProcessor = null;
		protected FontConverters m_mapFontsEncountered = null;
		protected Dictionary<string, string> m_mapCheckedInputStrings = new Dictionary<string, string>();
		protected FontConverter m_aFC = null;
		protected FontConverters m_aFCs = null;
		protected BaseConverterForm m_formDisplayValues = null;
		protected bool m_bReplaceAll = false;
		protected bool m_bAutoReplace = false;
		private OfficeRange m_rangeLast = null;  // if cancel is done, so we can pick up where we left off

		protected bool m_bSuspendUI = false;

		/// <summary>
		/// This constructor is for when you have a collection of Font to Converter mappings AND a form
		/// to use to display the differences.
		/// </summary>
		/// <param name="aFCs"></param>
		/// <param name="form"></param>
		public OfficeDocumentProcessor(FontConverters aFCs, BaseConverterForm form)
		{
			m_aFCs = aFCs;
			Form = form;
			Process = CompareInputOutputProcess;    // good default
		}

		/// <summary>
		/// This constructor is for when you have a single IEncConverter that is to be applied to
		/// all the data regardless of the font AND when you have your own form to use to display the
		/// differences.
		/// </summary>
		/// <param name="aFC"></param>
		/// <param name="form"></param>
		public OfficeDocumentProcessor(FontConverter aFC, BaseConverterForm form)
		{
			m_aFC = aFC;
			Form = form;
			Process = CompareInputOutputProcess;    // good default
		}

		/// <summary>
		/// This constructor is for when you have a form, but no Font to Converter mapping(s)
		/// This results in the user being prompted
		/// </summary>
		/// <param name="form"></param>
		public OfficeDocumentProcessor(BaseConverterForm form)
		{
			Form = form;
			Process = CompareInputOutputProcess;    // good default
		}

		/// <summary>
		/// This constructor is used when you want to set all the member variables (e.g. when
		/// you don't want to set a form or provide a different comparison routine)
		/// </summary>
		public OfficeDocumentProcessor()
		{
			SuspendUI = true;
		}

		public ProcessWord Process
		{
			get { return m_aWordProcessor; }
			set { m_aWordProcessor = value; }
		}

		public BaseConverterForm Form
		{
			get { return m_formDisplayValues; }
			set { m_formDisplayValues = value; }
		}

		public bool AreLeftOvers
		{
			get { return (m_rangeLast != null); }
		}

		public OfficeRange LeftOvers
		{
			get { return m_rangeLast; }
			set { m_rangeLast = value; }
		}

		public bool SuspendUI
		{
			get { return m_bSuspendUI; }
			set { m_bSuspendUI = value; }
		}

		public bool ReplaceAll
		{
			get { return m_bReplaceAll; }
			set { m_bReplaceAll = value; }
		}

		public bool AutoReplaceOnNextFind
		{
			get { return m_bAutoReplace; }
			set { m_bAutoReplace = value; }
		}

		public virtual void ReplaceText(OfficeRange aWordRange, Font fontTarget, ref int nCharIndex, string strNewText)
		{
			aWordRange.Text = strNewText;
			if (fontTarget != null)
				SetRangeFont(aWordRange, fontTarget.Name);
			nCharIndex = aWordRange.EndIndex;
		}

		public virtual void SetRangeFont(OfficeRange aWordRange, string strFontName)
		{
			aWordRange.FontName = strFontName;
		}

		protected virtual FormButtons ConvertProcessing(OfficeRange aWordRange, FontConverter aThisFC, string strInput, ref int nCharIndex, ref string strReplace)
		{
			// here's the meat of the WordShowConversionDiffProcessor engine: only process
			//  the word if the input is different from the converted output
			string strOutput = aThisFC.DirectableEncConverter.Convert(strInput);

			FormButtons res = FormButtons.None;
			if (!Form.SkipIdenticalValues || (strInput != strOutput))
			{
				if (ReplaceAll)
				{
					strReplace = strOutput;
					res = FormButtons.ReplaceAll;
				}
				else
				{
					res = Form.Show(aThisFC, strInput, strOutput);

					// just in case it's Replace or ReplaceAll, our replacement string is the 'Forward' conversion
					strReplace = Form.ForwardString;
				}
			}

			return res;
		}

		public bool CompareInputOutputProcess(OfficeRange aWordRange, ref int nCharIndex)
		{
			FormButtons res = FormButtons.None;
			do
			{
				string strInput = aWordRange.Text;
				if (String.IsNullOrEmpty(strInput))
					return true;

				// not technically required, but this'll help users (but only for the font we're looking for.
				if (!SuspendUI)
					aWordRange.Select();

				// did the caller give us a set of Fonts to scan?
				string strFontName = aWordRange.FontName;
				FontConverter aThisFC = null;
				if (m_aFC != null)
					aThisFC = m_aFC;
				else if (m_aFCs != null)
				{
					if (m_aFCs.ContainsKey(strFontName))
						aThisFC = m_aFCs[strFontName];
				}
				else // otherwise, query the user directly
					aThisFC = QueryUserForFontScan(strFontName);

				res = FormButtons.None;
				if (aThisFC == null)
				{
					// not a font that we're processing
					continue;
				}

				// see if we've already checked this word
				string strReplace = null;
				if (!m_mapCheckedInputStrings.TryGetValue(strInput, out strReplace))
				{
					res = ConvertProcessing(aWordRange, aThisFC, strInput, ref nCharIndex, ref strReplace);

					if (res == FormButtons.Cancel)
						return false;
					else if ((res == FormButtons.ReplaceAll) || (res == FormButtons.ReplaceOnce) || (res == FormButtons.ReplaceEvery))
					{
						ReplaceAll |= (res == FormButtons.ReplaceAll);

						// this means replace the word in situ, with what was converted
						ReplaceText(aWordRange, aThisFC.DirectableEncConverter.TargetFont, ref nCharIndex, strReplace);

						// keep track of this word so that if it comes up again, we'll replace it as is.
						if (AutoReplaceOnNextFind || (res == FormButtons.ReplaceEvery))
							m_mapCheckedInputStrings.Add(strInput, strReplace);
					}
					else if ((aThisFC.RhsFont != null) && (res != FormButtons.Next))
					{
						// even if the string doesn't change, if we have an output font, we have to set it.
						SetRangeFont(aWordRange, aThisFC.RhsFont.Name);
					}
				}
				else
				{
					// this particular input string has already been approved for replacement
					ReplaceText(aWordRange, aThisFC.DirectableEncConverter.TargetFont, ref nCharIndex, strReplace);
				}
			} while (res == FormButtons.Redo);

			return true;
		}

		protected virtual FontConverter QueryForFontConvert(string strFontName)
		{
			FontConverter aFC = null;
			FontConvertersPicker aFontConverterPicker = new FontConvertersPicker(strFontName);
			if (aFontConverterPicker.ShowDialog() == DialogResult.OK)
				aFC = aFontConverterPicker.SelectedFontConverters[strFontName];
			return aFC;
		}

		protected FontConverter QueryUserForFontScan(string strFontName)
		{
			if ((strFontName == null) || (strFontName.Length == 0))
				return null;

			// make sure our collection exists
			if (m_mapFontsEncountered == null)
				m_mapFontsEncountered = new FontConverters();

			if (!m_mapFontsEncountered.ContainsKey(strFontName))
			{
				DialogResult res = MessageBox.Show(String.Format("Do you want to convert words in the {0} font?", strFontName), Connect.cstrCaption, MessageBoxButtons.YesNoCancel);

				FontConverter aFC = null;
				if (res == DialogResult.Cancel)
					throw new ApplicationException(Connect.cstrAbortMessage);
				if (res == DialogResult.Yes)
				{
					aFC = QueryForFontConvert(strFontName);
					/*
					FontConvertersPicker aFontConverterPicker = new FontConvertersPicker(strFontName);
					if( aFontConverterPicker.ShowDialog() == DialogResult.OK )
						aFC = aFontConverterPicker.SelectedFontConverters[strFontName];
					*/
					// aFC = new FontConverter(strFontName);
				}

				m_mapFontsEncountered.Add(strFontName, aFC);
			}

			return m_mapFontsEncountered[strFontName];
		}
	}
}
