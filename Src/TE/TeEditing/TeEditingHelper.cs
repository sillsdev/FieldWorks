// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeEditingHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices; // needed for Marshal
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.IText;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.Ling;
using System.Drawing;

namespace SIL.FieldWorks.TE
{
	#region TeProjectSettings
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Store the project-specific settings used in TeEditingHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeProjectSettings
	{
		private static readonly Dictionary<FdoCache, TeProjectSettings> s_settings =
			new Dictionary<FdoCache, TeProjectSettings>();

		private bool m_fSendSyncMessages;
		private bool m_fReceiveSyncMessages;
		private bool m_fShowSpellingErrors;

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to send sync messages.
		/// </summary>
		/// <value><c>true</c> to send sync messages; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool SendSyncMessages
		{
			get { return m_fSendSyncMessages; }
			set { m_fSendSyncMessages = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to receive sync messages.
		/// </summary>
		/// <value><c>true</c> to receive sync messages; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ReceiveSyncMessages
		{
			get { return m_fReceiveSyncMessages; }
			set { m_fReceiveSyncMessages = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether spelling errors should be displayed for
		/// all TE windows for this project.
		/// </summary>
		public bool ShowSpellingErrors
		{
			get { return m_fShowSpellingErrors; }
			set { m_fShowSpellingErrors = value; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the project settings.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static TeProjectSettings LoadSettings(FdoCache cache)
		{
			if (s_settings.ContainsKey(cache))
				return s_settings[cache];

			TeProjectSettings projectSettings = new TeProjectSettings();
			s_settings.Add(cache, projectSettings);

			if (cache != null && cache.ServerName.Trim() != string.Empty &&
				cache.DatabaseName.Trim() != string.Empty)
			{
				string serverAndDb = cache.ServerName.Trim() + "\\" + cache.DatabaseName.Trim();
				if (FwApp.App != null)
				{
					RegistryKey key = FwApp.App.SettingsKey.CreateSubKey(serverAndDb);
					if (key != null)
					{
						projectSettings.m_fSendSyncMessages =
							((int)key.GetValue("SendSyncMessage", 1) == 1);
						projectSettings.m_fReceiveSyncMessages =
							((int)key.GetValue("ReceiveSyncMessage", 0) == 1);
						projectSettings.m_fShowSpellingErrors =
							((int)key.GetValue("ShowSpellingErrors", 0) == 1);
					}
				}
			}
			return projectSettings;
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class contains editing methods shared by different TE views.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeEditingHelper : FwEditingHelper
	{
		#region Member variables
		/// <summary></summary>
		protected IScripture m_scr;
		private readonly TeViewType m_viewType;
		private readonly int m_filterInstance;
		private FilteredScrBooks m_bookFilter;
		private InsertVerseMessageFilter m_InsertVerseMessageFilter;
		private Cursor m_restoreCursor;
		private SelectionHelper m_lastFootnoteTextRepSelection;
		private int m_newFootnoteIndex;
		private bool m_selectionUpdateInProcess;
		private ScrReference m_oldReference;
		private string m_sPrevSelectedText;
		private ILgCharacterPropertyEngine m_cpe;
		private FocusMessageHandling m_syncHandler;
		/// <summary></summary>
		protected TeProjectSettings m_projectSettings;
		private StVc.ContentTypes m_contentType;
		AnnotationAdjuster m_annotationAdjuster;
		private IGetTeStVc m_mainTransView;
		//private bool m_fPromptUserForWs;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor is for testing so the class can be mocked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper() : base(null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeEditingHelper"/> class.
		/// </summary>
		/// <param name="callbacks">The callbacks.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="viewType">Type of the view.</param>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper(IEditingCallbacks callbacks, FdoCache cache, int filterInstance,
			TeViewType viewType) : base(cache, callbacks)
		{
			CanDoRtL = true;
			if (m_cache != null)
				m_scr = m_cache.LangProject.TranslatedScriptureOA;

			m_viewType = viewType;
			m_filterInstance = filterInstance;
			m_projectSettings = TeProjectSettings.LoadSettings(cache);
			m_annotationAdjuster = new AnnotationAdjuster(cache, this);
			PasteFixTssEvent += RemoveHardFormatting;
			m_syncHandler = new FocusMessageHandling(this);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to set the content type, when the view we are 'helping' is some sort of BT.
		/// Enhance JohnT: if we extend the ViewTypes we may be able to determine this from it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StVc.ContentTypes ContentType
		{
			get { return m_contentType; }
			set { m_contentType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call this if the editing helper is for a view which requires a segmented BT to be kept
		/// synchronized...for example, the vernacular pane of the BT draft view or the parallel
		/// print layout BT view. It causes the annotation adjuster to ensure that any new segments
		/// it makes are real and have free translation annnotations.
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public void EnableBtSegmentUpdate(int ws)
		{
			m_annotationAdjuster.BtWs = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allow more precise monitoring of text edits and resulting prop changes,
		/// so our AnnotationAdjuster can adjust annotations appropriately.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool MonitorTextEdits
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the hard formatting from text that is pasted.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:SIL.FieldWorks.Common.RootSites.FwPasteFixTssEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveHardFormatting(EditingHelper sender, FwPasteFixTssEventArgs e)
		{
			ITsString tss = e.TsString;
			ITsStrBldr tssBldr = tss.GetBldr();
			for (int i = 0; i < tss.RunCount; i++)
			{
				TsRunInfo runInfo;
				ITsTextProps props = tss.FetchRunInfo(i, out runInfo);
				ITsPropsBldr propsBuilder = TsPropsBldrClass.Create();

				// Copy the style name from the run properties
				string style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				if (style != null)
					propsBuilder.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, style);

				// Copy the WS information from the run properties
				int var;
				int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (ws > 0)
					propsBuilder.SetIntPropValues((int)FwTextPropType.ktptWs, var, ws);

				// Copy the ORC information from the run properties
				string orcData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (orcData != null)
					propsBuilder.SetStrPropValue((int)FwTextPropType.ktptObjData, orcData);

				tssBldr.SetProperties(runInfo.ichMin, runInfo.ichLim, propsBuilder.GetTextProps());
			}

			e.TsString = tssBldr.GetString();
		}

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
			{
				Debug.Assert(m_cpe == null);
				return;
			}

			if (disposing)
			{
				// Log disposing event - removed logging as part of fix for TE-6551
				//string message = "Disposing TeEditingHelper...\n" +
				//    "Stack Trace:\n" + Environment.StackTrace;
				//SIL.Utils.Logger.WriteEvent(message);

				// Dispose managed resources here.
				if (m_InsertVerseMessageFilter != null)
					Application.RemoveMessageFilter(m_InsertVerseMessageFilter);

				if (m_syncHandler != null)
				{
					m_syncHandler.ReferenceChanged -= ScrollToReference;
					m_syncHandler.AnnotationChanged -= ScrollToCitedText;
					m_syncHandler.Dispose();
				}

				if (m_annotationAdjuster != null)
					m_annotationAdjuster.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			PasteFixTssEvent -= RemoveHardFormatting;
			m_syncHandler = null;
			m_scr = null;
			m_bookFilter = null;
			m_InsertVerseMessageFilter = null;
			m_restoreCursor = null;
			m_lastFootnoteTextRepSelection = null;
			m_oldReference = null;
			if (m_cpe != null)
			{
				if (Marshal.IsComObject(m_cpe))
					Marshal.ReleaseComObject(m_cpe);
				m_cpe = null;
			}
			m_annotationAdjuster = null;
			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Properties

		/// <summary>
		/// If this helper is for a view which can display segmented back translations, this should be
		/// set to the draft view which displays the main translation. It is used to highlight the relevant
		/// sentence when the selection is in a back translation. NB this is typically NOT the view to
		/// which this helper belongs! The type is set to this more restricted interface because of
		/// DLL dependencies.
		/// </summary>
		public IGetTeStVc MainTransView
		{
			get { return m_mainTransView; }
			set { m_mainTransView = value; }
		}

		/// <summary>
		/// If this helper is for a view which can display segmented back translations, this should be
		/// set to the VC which displays the main translation. It is used to highlight the relevant
		/// sentence when the selection is in a back translation.
		/// </summary>
		private TeStVc MainTransVc
		{
			get
			{
				if (MainTransView == null)
					return null;
				return MainTransView.Vc;
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the project settings.
		/// </summary>
		/// <value>The project settings.</value>
		/// ------------------------------------------------------------------------------------
		internal TeProjectSettings ProjectSettings
		{
			get { return m_projectSettings; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to send sync messages.
		/// </summary>
		/// <value><c>true</c> to send sync messages; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool SendSyncMessages
		{
			get
			{
				CheckDisposed();
				return m_projectSettings.SendSyncMessages;
			}
			set
			{
				CheckDisposed();
				m_projectSettings.SendSyncMessages = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to show spelling errors.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowSpellingErrors
		{
			get
			{
				CheckDisposed();
				return m_projectSettings.ShowSpellingErrors;
			}
			set
			{
				CheckDisposed();
				m_projectSettings.ShowSpellingErrors = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to receive sync messages.
		/// </summary>
		/// <value><c>true</c> to receive sync messages; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ReceiveSyncMessages
		{
			get
			{
				CheckDisposed();
				return m_projectSettings.ReceiveSyncMessages;
			}
			set
			{
				CheckDisposed();
				m_projectSettings.ReceiveSyncMessages = value;

				// If we're not doing either kind of linking dispose of the link.
				if (m_syncHandler != null)
					m_syncHandler.EnableLibronixLinking = (value && m_projectSettings.SendSyncMessages);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TeViewType of this view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeViewType ViewType
		{
			get
			{
				CheckDisposed();
				return m_viewType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TeViewType of the printable view corresponding to this view. Note that the
		/// returned TeViewType may not correspond to a real view, created or otherwise.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeViewType CorrespondingPrintableViewType
		{
			get
			{
				CheckDisposed();
				// For BT draft view we have two draft windows - the BT side has both Scripture and BT set
				if ((m_viewType & (TeViewType.Scripture | TeViewType.BackTranslation)) ==
					(TeViewType.Scripture | TeViewType.BackTranslation))
				{
					return (m_viewType | TeViewType.Print) & ~(TeViewType.Draft | TeViewType.Scripture);
				}
				return ((m_viewType | TeViewType.Print) & ~TeViewType.Draft);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Counts the bits set to one.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>Number of set bits</returns>
		/// ------------------------------------------------------------------------------------
		private static int CountSetBits(int value)
		{
			int ones = 0;
			while (value > 0)
			{
				if ((value & 1) != 0)
					ones++;
				value >>= 1;
			}

			return ones;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view type string.
		/// </summary>
		/// <value>The view type string.</value>
		/// ------------------------------------------------------------------------------------
		public static string ViewTypeString(TeViewType type)
		{
			StringBuilder bldr = new StringBuilder();

			// Content types
			if ((type & TeViewType.BackTranslation) != 0)
				bldr.Append(TeViewType.BackTranslation);
			if ((type & TeViewType.Scripture) != 0)
				bldr.Append(TeViewType.Scripture);
			if ((type & TeViewType.Checks) != 0)
				bldr.Append(TeViewType.Checks);

			// If we have a scripture or BT view, Draft or Print has to be set
			Debug.Assert((type & (TeViewType.Draft | TeViewType.Print)) != 0 ||
				(type & (TeViewType.Scripture | TeViewType.BackTranslation)) == 0);
			if ((type & TeViewType.Draft) != 0)
				bldr.Append(TeViewType.Draft);
			else if ((type & TeViewType.Print) != 0)
				bldr.Append(TeViewType.Print);

			// If we have a scripture or BT view, Horizontal or Vertical has to be set
			Debug.Assert((type & (TeViewType.Horizontal | TeViewType.Vertical)) != 0 ||
				(type & (TeViewType.Scripture | TeViewType.BackTranslation)) == 0);
			if ((type & TeViewType.Vertical) != 0)
				bldr.Append(TeViewType.Vertical);
			else if ((type & TeViewType.Horizontal) != 0)
				bldr.Append(TeViewType.Horizontal);

			// View types
			Debug.Assert(CountSetBits((int)(type & TeViewType.ViewTypeMask)) == 1,
				"Need exactly one view type");
			TeViewType viewType = (type & TeViewType.ViewTypeMask);
			bldr.Append(viewType);

			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current book filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FilteredScrBooks BookFilter
		{
			get
			{
				CheckDisposed();

				if (m_bookFilter == null)
				{
					m_bookFilter = FilteredScrBooks.GetFilterInstance(m_cache, m_filterInstance);
					// if the filter is still null then make one for testing
					if (m_bookFilter == null && InTestMode)
						m_bookFilter = new FilteredScrBooks(m_cache, m_filterInstance);
				}
				return m_bookFilter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the current selection is a valid place to insert a chapter or verse
		/// number.
		/// </summary>
		/// TE-2740: Don't allow numbers to be inserted in intro material
		/// TE-8478: Don't allow numbers to be inserted into picture captions
		/// ------------------------------------------------------------------------------------
		public virtual bool CanInsertNumberInElement
		{
			get
			{
				CheckDisposed();

				int tag;
				int hvoSel;


				return (CurrentSelection != null &&
					GetSelectedScrElement(out tag, out hvoSel) &&
					tag == (int)ScrSection.ScrSectionTags.kflidContent &&
					!InSegmentBt() &&
					!InIntroSection &&
					!IsPictureSelected &&
					CanReduceSelectionToIpAtTop);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the selection is in a user prompt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsSelectionInUserPrompt
		{
			get { return IsSelectionInPrompt(CurrentSelection); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the selection is in a user prompt. (Unless it is in a
		/// picture..I (JohnT) am not sure how a picture selection comes to seem to be in a user
		/// prompt, but see TE-7813: double-clicking one crashes here if we don't check.)
		/// </summary>
		/// <param name="helper">The selection to check.</param>
		/// <returns>true if the selection is in a user prompt; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSelectionInPrompt(SelectionHelper helper)
		{
			return (helper != null && helper.Selection.SelType != VwSelType.kstPicture &&
				helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor) == SimpleRootSite.kTagUserPrompt &&
				helper.GetTextPropId(SelectionHelper.SelLimitType.End) == SimpleRootSite.kTagUserPrompt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer true if the selection is in a segmented BT segment, that is, in the
		/// comment of a CmAnnotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InSegmentBt()
		{
			if(CurrentSelection == null)
			return false;

			int textTag = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.Top);
			if (textTag == (int) CmAnnotation.CmAnnotationTags.kflidComment)
				return true;
			if (textTag != SimpleRootSite.kTagUserPrompt)
				return false;
			// Selection is in a prompt...is it a BT prompt?
			SelLevInfo[] vsli = CurrentSelection.LevelInfo;
			if (vsli.Length < 1)
				return false; // Huh?
			return vsli[0].tag == StTxtPara.SegmentFreeTranslationFlid(Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance can reduce selection to ip at top.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance can reduce selection to ip at top; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CanReduceSelectionToIpAtTop
		{
			get
			{
				if (InTestMode)
				{
					return true;
				}
				SelectionHelper selHelper = SelectionHelper.ReduceSelectionToIp(Callbacks.EditedRootBox.Site,
					SelectionHelper.SelLimitType.Top, false, false);
				if (selHelper == null)
				{
					return false;
				}
				return selHelper.SetSelection(Callbacks.EditedRootBox.Site,false, false) != null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a chapter number can be inserted at the current IP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool CanInsertChapterNumber
		{
			get
			{
				CheckDisposed();

				// Only possible if we have a selection
				if (CurrentSelection == null)
					return false;
				if (InSegmentBt()) // and it's not in the segmented BT.
					return false;

				// Get the paragraph text where the selection is
				ITsString paraString;
				int ich;
				bool fAssocPrev;
				int hvoObj;
				int wsAlt; //the WS of the multiString alt, if selection is in a back translation
				int propTag;
				IVwRootSite rootSite = CurrentSelection.RootSite;
				SelectionHelper selHelperIP = SelectionHelper.ReduceSelectionToIp(rootSite,
					SelectionHelper.SelLimitType.Top, false, false);
				if (selHelperIP == null)
				{
					return false;
				}
				IVwSelection selIP = selHelperIP.SetSelection(rootSite, false, false);
				if (selIP == null)
				{
					return false;
				}
				selIP.TextSelInfo(false, out paraString, out ich, out fAssocPrev,
					out hvoObj, out propTag, out wsAlt);

				// We allow "inserting" chapter numbers inside or next to existing chapter numbers
				// in BT's because it really does an update to "fix" them if necessary.
				if (propTag == (int)CmTranslation.CmTranslationTags.kflidTranslation)
					return true;

				// Get the style names to the left and right of the IP
				ITsTextProps ttp;
				string leftStyle = null;
				string rightStyle = null;
				//				int ipLocation = selHelperIP.IchAnchor;
				int ipLocation = selHelperIP.GetIch(SelectionHelper.SelLimitType.Top);
				if (ipLocation > 0)
				{
					ttp = paraString.get_PropertiesAt(ipLocation - 1);
					leftStyle = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				}
				if (ipLocation <= paraString.Length)
				{
					ttp = paraString.get_PropertiesAt(ipLocation);
					rightStyle = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				}

				// If the left or right style is chapter number then don't allow insertion
				if (leftStyle == ScrStyleNames.ChapterNumber || rightStyle == ScrStyleNames.ChapterNumber)
					return false;

				// If both the left and right styles are verse numbers then don't allow insertion
				if (leftStyle == ScrStyleNames.VerseNumber && rightStyle == ScrStyleNames.VerseNumber)
					return false;

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the book for the current selection. Returns -1 if the
		/// selection is undefined, or something else more destructive.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor. When setting the BookIndex, the insertion point will be
		/// placed at the beginning of the book's title.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int BookIndex
		{
			get
			{
				CheckDisposed();
				return CurrentSelection == null ? -1 :
					((ITeView)Control).LocationTracker.GetBookIndex(CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
			}
			set
			{
				CheckDisposed();

				if (value >= 0 && value < BookFilter.BookCount)
					SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, value, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section for the current selection. Returns -1 if the selection
		/// is not in a section.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int SectionIndex
		{
			get
			{
				CheckDisposed();

				return ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
					CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the paragraph where the insertion point is located.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParagraphIndex
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return 0;
				return CurrentSelection.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs).ihvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system of the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ViewConstructorWS
		{
			get
			{
				CheckDisposed();

				// To support back translations in multiple languages, need to choose the
				// correct default WS.
				if (CurrentSelection != null && CurrentSelection.LevelInfo.Length > 0)
				{
					// try getting WS from the selection - might be front or back translation
					// in side-by-side BT print layout
					int hvo = CurrentSelection.LevelInfo[0].hvo;
					return Callbacks.GetWritingSystemForHvo(hvo);
				}

				return RootVcDefaultWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current rootbox's view constructor's default writing system, or the default
		/// analysis WS if the rootbox's VC is not an StVC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int RootVcDefaultWritingSystem
		{
			get
			{
				StVc stvc = ViewConstructor as StVc;
				return stvc == null ? m_cache.DefaultAnalWs : stvc.DefaultWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Just like CurrentStartRef and CurrentEndRef but returns a range of references to
		/// account for a selection  in a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ScrReference[] CurrentRefRange
		{
			get
			{
				CheckDisposed();
				return GetCurrentAnchorRefRange();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the start ref at the current insertion point.
		/// </summary>
		/// <remarks>
		/// This property is not guaranteed to return a ScrReference containing the book, chapter,
		/// AND verse.  It will return as much as it can, but not neccessarily all of it. It
		/// will not search back into a previous section if it can't find the verse number in
		/// the current section. This means that if a verse crosses a section break, the verse
		/// number will be inferred from the section start ref. For now, section refs are not
		/// very reliable, but this should work pretty well when TE-278 and TE-329 are
		/// completed.
		/// ENHANCE: Consider checking the end of the previous section if
		/// the verse number isn't found, and if it's still in the same chapter, return the
		/// verse number from the end of the previous section instead.
		/// The Word of God will not return void. - Isaiah 55:11.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ScrReference CurrentStartRef
		{
			get
			{
				CheckDisposed();

				ScrReference[] curRef = GetCurrentRefRange(CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
				return curRef[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the end ref at the current insertion point.
		/// </summary>
		/// <remarks>
		/// This property is not guaranteed to return a ScrReference containing the book, chapter,
		/// AND verse.  It will return as much as it can, but not neccessarily all of it. It
		/// will not search back into a previous section if it can't find the verse number in
		/// the current section. This means that if a verse crosses a section break, the verse
		/// number will be inferred from the section start ref. For now, section refs are not
		/// very reliable, but this should work pretty well when TE-278 and TE-329 are
		/// completed.
		/// ENHANCE: Consider checking the end of the previous section if
		/// the verse number isn't found, and if it's still in the same chapter, return the
		/// verse number from the end of the previous section instead.
		/// The Word of God will not return void. - Isaiah 55:11.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ScrReference CurrentEndRef
		{
			get
			{
				CheckDisposed();

				ScrReference[] curRef = GetCurrentRefRange(CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
				return curRef[1];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference[] GetCurrentAnchorRefRange()
		{
			CheckDisposed();

			IVwRootSite rootsite = Callbacks.EditedRootBox.Site;
			SelectionHelper helper = SelectionHelper.ReduceSelectionToIp(
				rootsite, SelectionHelper.SelLimitType.Anchor, false, false);

			return GetCurrentRefRange(helper, SelectionHelper.SelLimitType.Anchor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference[] GetCurrentEndRefRange()
		{
			CheckDisposed();

			IVwRootSite rootsite = Callbacks.EditedRootBox.Site;
			SelectionHelper helper = SelectionHelper.ReduceSelectionToIp(
				rootsite, SelectionHelper.SelLimitType.End, false, false);

			return GetCurrentRefRange(helper, SelectionHelper.SelLimitType.End);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Common utility for the CurrentRef* properties
		/// </summary>
		/// <param name="selection"></param>
		/// <param name="selLimit">The limit of the selection (anchor, end, etc.) to get the
		/// reference of</param>
		/// <returns>the start and end reference of the given selection, as an array of two
		/// ScrReference objects</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual ScrReference[] GetCurrentRefRange(SelectionHelper selection,
			SelectionHelper.SelLimitType selLimit)
		{
			if (m_cache == null || selection == null || BookFilter == null)
				return new ScrReference[] {ScrReference.Empty, ScrReference.Empty};

			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			// If there is a current book...
			BCVRef start = new BCVRef();
			BCVRef end = new BCVRef();

			int iBook = tracker.GetBookIndex(selection, selLimit);
			if (iBook >= 0 && BookFilter.BookCount > 0)
			{
				try
				{
					ScrBook book = BookFilter.GetBook(iBook);

					// if there is not a current section, then use the book and chapter/verse of 0.
					int hvoSection = tracker.GetSectionHvo(CurrentSelection, selLimit);
					if (hvoSection >= 0)
					{
						// If there is a section...
						ScrSection section = new ScrSection(m_cache, hvoSection);
						int paraHvo = selection.GetLevelInfoForTag(
							(int)StText.StTextTags.kflidParagraphs, selLimit).hvo;
						ScrTxtPara scrPara = new ScrTxtPara(m_cache, paraHvo);
						// Get the ich at either the beginning or the end of the selection,
						// as specified with limit. (NB that this is relative to the property, not the whole paragraph.)
						int ich;
						// Get the TsString, whether in vern or BT
						ITsString tss;
						SelLevInfo segInfo;
						int refWs;
						if (selection.GetLevelInfoForTag(StTxtPara.SegmentsFlid(Cache), selLimit, out segInfo))
						{
							// selection is in a segmented BT segment. Figure the reference based on where the segment is
							// in the underlying paragraph.
							tss = scrPara.Contents.UnderlyingTsString; // for check below on range of ich.
							CmBaseAnnotation seg = new CmBaseAnnotation(Cache, segInfo.hvo);
							ich = seg.BeginOffset;
							Debug.Assert(seg.BeginObjectRAHvo == scrPara.Hvo);
							refWs = -1; // ich is in the paragraph itself, not some CmTranslation
						}
						else
						{
							ich = selection.GetIch(selLimit);
							// Get the TsString, whether in vern or BT
							tss = selection.GetTss(selLimit);
							refWs = GetCurrentBtWs(selLimit); // figures out whether it's in a CmTranslation or the para itself.
						}
						Debug.Assert(tss == null || ich <= tss.Length);
						if (tss != null && ich <= tss.Length)
						{
							scrPara.GetBCVRefAtPosition(refWs, ich, true, out start, out end);

							// If the chapter number is 0, then use the chapter from the section reference
							if (end.Chapter == 0)
								end.Chapter = BCVRef.GetChapterFromBcv(section.VerseRefMin);
							if (start.Chapter == 0)
								start.Chapter = BCVRef.GetChapterFromBcv(section.VerseRefMin);
						}
					}
					else
					{
						// either it didn't find a level or it didn't find an index. Either way,
						// it couldn't find a section.
						start.Book = end.Book = book.CanonicalNum;
					}
				}
				catch
				{
					// Bummer man, something went wrong... don't sweat it though, it happens...
					// This can occur if you are in the introduction or other location that lacks
					// relevant information or other necessary stuff.
				}
			}
			return new ScrReference[] {new ScrReference(start, m_scr.Versification),
				new ScrReference(end, m_scr.Versification)}; ;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the current selection is in a book title.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool InBookTitle
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return false;
				SelLevInfo levelInfo;
				return CurrentSelection.GetLevelInfoForTag(
					(int)ScrBook.ScrBookTags.kflidTitle, out levelInfo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the current selection is in a section head.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a section head and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool InSectionHead
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return false;

				SelLevInfo headInfo;
				return CurrentSelection.GetLevelInfoForTag(
					(int)ScrSection.ScrSectionTags.kflidHeading, out headInfo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates where the current selection is in an introduction section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool InIntroSection
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null || Control == null)
					return false;

				int hvoSection = ((ITeView)Control).LocationTracker.GetSectionHvo(CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
				if (hvoSection < 0)
					return false;

				// We come here a lot (gets called from Update handler), so we don't want to
				// construct a ScrSection object here but get the value we're interested in
				// directly from the cache. Creating a ScrSection object checks if the HVO
				// is valid. In doing so it does a query on the database (select Class$ from CmObject...)
				// This happens only in Debug, but getting the interesting value directly from
				// the cache makes it easier to do SQL profiling.
				return ScrSection.IsIntroSection(m_cache, hvoSection);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Hvo of the paragraph where the insertion point is located.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int ParagraphHvo
		{
			get
			{
				if (CurrentSelection == null)
					return 0;
				SelLevInfo paraInfo;
				bool fLevelFound = CurrentSelection.GetLevelInfoForTag(
					(int)StText.StTextTags.kflidParagraphs, out paraInfo);
				return fLevelFound ? paraInfo.hvo : 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Hvo of the surviving paragraph. This will be either the paragraph before
		/// the selected paragraph if we have an IP or the paragraph at the top of a range
		/// selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int SurvivorParagraphHvo(SelectionHelper selHelper, IStText text, int ihvo, bool fMergeNext)
		{
			if (!selHelper.IsRange)
			{
				int iSurvivor = fMergeNext ? ihvo + 1 : ihvo - 1;
				return (iSurvivor >= 0 && iSurvivor < text.ParagraphsOS.Count ?
					text.ParagraphsOS.HvoArray[iSurvivor] : -1);
			}

			int paraLev = selHelper.GetLevelForTag((int)StText.StTextTags.kflidParagraphs, SelectionHelper.SelLimitType.Top);
			SelLevInfo[] rgSelLevInfo = selHelper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			return rgSelLevInfo[paraLev].hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Hvo of the StText where the insertion point is located, if it is in a
		/// ScrSection Content field.
		/// Returns 0 if IP is not in a ScrSection Content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoOfScrSectionContent
		{
			get
			{
				CheckDisposed();

				if (CurrentSelection == null)
					return 0;
				SelLevInfo contentInfo;
				bool fLevelFound = CurrentSelection.GetLevelInfoForTag(
					(int)ScrSection.ScrSectionTags.kflidContent, out contentInfo);
				return fLevelFound ? contentInfo.hvo : 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets state of "Insert Verse Number" mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InsertVerseActive
		{
			get
			{
				CheckDisposed();
				return m_InsertVerseMessageFilter != null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the selected picture. If a picture isn't selected, then zero is
		/// returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PictureHvo
		{
			get
			{
				CheckDisposed();
				return (IsPictureSelected ? CurrentSelection.LevelInfo[0].hvo : 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the current selection is in back translation data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsBackTranslation
		{
			get
			{
				CheckDisposed();

				if (m_viewType == TeViewType.BackTranslationParallelPrint)
				{
					SelectionHelper helper = CurrentSelection;
					if (helper != null)
					{
						if (helper.NumberOfLevels == 0)
							return false;

						return (m_cache.GetClassOfObject(helper.LevelInfo[0].hvo) == CmTranslation.kClassId);
					}
				}

				return (m_viewType & TeViewType.BackTranslation) != 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the selection is at the end of a section.
		/// </summary>
		/// <remarks>This is only valid if the current selection is in the vernacular</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool AtEndOfSection
		{
			get
			{
				CheckDisposed();

				SelectionHelper helper = CurrentSelection;
				if (helper == null)
					return false;

				// Check to see if the selection is at the end of the current paragraph
				SelLevInfo paraInfo;
				if (!helper.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs,
					out paraInfo))
				{
					return false;
				}

				StTxtPara para = new StTxtPara(m_cache, paraInfo.hvo);
				if (helper.IchAnchor != para.Contents.Length)
					return false;

				// Check to see if this is the last paragraph in the section. This is checked
				// by looking at the owning section's paragraph list to see if the last
				// paragraph is the one where the selection is.
				StText text = new StText(m_cache, para.OwnerHVO);
				if (m_cache.GetClassOfObject(text.OwnerHVO) != ScrSection.kClassId)
					return false;

				// Only get section if text is the content of the section
				if (text.OwningFlid != (int)ScrSection.ScrSectionTags.kflidContent)
					return false;

				// Make sure we're in the last paragraph of the text
				return text.ParagraphsOS.HvoArray[text.ParagraphsOS.Count - 1] == para.Hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the selection is at the start of the first Scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool AtBeginningOfFirstScriptureSection
		{
			get
			{
				CheckDisposed();

				// If there is no selection, or the selection is not at the start of a paragraph,
				// or there are no books in the book filter, then we are not at the start of the
				// first scripture section
				if (CurrentSelection == null || CurrentSelection.IchAnchor != 0 ||
					BookIndex < 0 || SectionIndex < 0)
				{
					return false;
				}

				// Get the current section. If it is an intro section then we can't be at the
				// start of a Scripture section.
				ScrBook book = BookFilter.GetBook(BookIndex);
				ScrSection section = (ScrSection)book.SectionsOS[SectionIndex];
				if (section.IsIntro)
					return false;

				// If the previous section is a scripture section, then we can't be at the
				// start of the first scripture section.
				ScrSection prevSection = section.PreviousSection;
				if (prevSection != null && !prevSection.IsIntro)
					return false;

				// Check to see if the selection is at the start of the current section
				SelectionHelper helper = CurrentSelection;
				SelLevInfo paraInfo = helper.GetLevelInfoForTag(
					(int)StText.StTextTags.kflidParagraphs);
				StTxtPara para = new StTxtPara(m_cache, paraInfo.hvo);

				// Make sure that the paragraph is a heading
				StText text = new StText(m_cache, para.OwnerHVO);
				if (text.OwningFlid != (int)ScrSection.ScrSectionTags.kflidHeading)
					return false;

				// Make sure we're in the first paragraph of the text
				return (para.IndexInOwner == 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the following section (if any) is a Scripture section. If this is the
		/// last section, then this returns true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool ScriptureCanImmediatelyFollowCurrentSection
		{
			get
			{
				CheckDisposed();

				SelectionHelper helper = CurrentSelection;
				if (helper == null)
					return false;

				// Get the section from the selection
				int hvoSection = ((ITeView)Control).LocationTracker.GetSectionHvo(
					CurrentSelection, SelectionHelper.SelLimitType.Anchor);
				if (hvoSection < 0)
					return false;
				ScrSection currSection = new ScrSection(m_cache, hvoSection);

				// Get the book that owns the section
				int[] sections = currSection.OwningBook.SectionsOS.HvoArray;

				// Inserting a Scripture section after the last section in the book is always kosher.
				if (currSection.Hvo == sections[sections.Length - 1])
					return true;

				// Find the position of the section in the list of sections
				for (int i = 0; i < sections.Length - 1; i++)
				{
					// When it is found, see if the next one is a scripture section
					if (sections[i] == currSection.Hvo)
					{
						ScrSection nextSection = new ScrSection(m_cache, sections[i + 1]);
						if (BCVRef.GetVerseFromBcv(nextSection.VerseRefMax) != 0) // Scripture always has verse # >= 1
							return true;
					}
				}
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Unicode character properties engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		private ILgCharacterPropertyEngine UnicodeCharProps
		{
			get
			{
				if (m_cpe == null)
					m_cpe = m_cache.UnicodeCharProps;
				return m_cpe;
			}
		}
		#endregion

		#region Overrides of FwEditingHelper

		/// <summary>
		/// Override to suppress tab key. The default implementation is designed to move between
		/// 'fields' by finding a subsequent selection in a different property. All the text in
		/// TE is in the same property, so it searches the whole document, which is very slow.
		/// See TE-8117 for other possible behaviors we might implement one day.
		/// </summary>
		protected override bool CallOnExtendedKey(int chw, VwShiftStatus ss)
		{
			if (chw == 9) // tab
				return false;
			return base.CallOnExtendedKey(chw, ss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called by a view when the views code is about to delete a paragraph. We
		/// need to save the back translations by moving them to whatever paragraph the deleted
		/// one is merging with.
		/// </summary>
		/// <param name="selHelper">The selection helper</param>
		/// <param name="hvoObject">HVO of the StTxtPara to be deleted</param>
		/// <param name="hvoOwner">HVO of the StText that owns the para</param>
		/// <param name="tag">flid in which para is owned</param>
		/// <param name="ihvo">index of paragraph in text</param>
		/// <param name="fMergeNext"><c>true</c> if this paragraph is merging with the
		/// following paragraph.</param>
		/// ------------------------------------------------------------------------------------
		public override void AboutToDelete(SelectionHelper selHelper, int hvoObject,
			int hvoOwner, int tag, int ihvo, bool fMergeNext)
		{
			CheckDisposed();

			// do the normal processing to combine the paragraphs
			base.AboutToDelete(selHelper, hvoObject, hvoOwner, tag, ihvo, fMergeNext);

			// only do the rest of the processing if we are about to delete a paragraph.
			if (tag == (int)StText.StTextTags.kflidParagraphs)
			{
				// Now need to refresh footnote cache for surviving paragraph - footnotes in the
				// deleted paragraph will still refer to it as the containing paragraph.
				IStText text = new StText(Cache, hvoOwner);
				int hvoSurvivor = SurvivorParagraphHvo(selHelper, text, ihvo, fMergeNext);
				if (hvoSurvivor > 0)
					ScrFootnote.ReplaceReferencesToParagraph(Cache, hvoObject, hvoSurvivor);
			}
			// And finally give the annotation adjust a chance to do its thing. (Nulls only in mock testing.)
			if (m_annotationAdjuster != null && selHelper != null && selHelper.RootSite != null)
				m_annotationAdjuster.AboutToDelete(selHelper.Selection, selHelper.RootSite.RootBox, hvoObject, hvoOwner, tag, ihvo, fMergeNext);
		}

		/// <summary>
		/// Override to adjust annotations for the resegmented string.
		/// Enhance JohnT: possibly we don't need to break segments for a picture? But it's nice to have it come
		/// out in the BT window.
		/// </summary>
		protected override void InsertPictureOrc(CmPicture pict, ITsString tss, int ich, int hvoObj, int propTag, int ws)
		{
			m_annotationAdjuster.OnAboutToModify(m_cache, hvoObj);
			base.InsertPictureOrc(pict, tss, ich, hvoObj, propTag, ws);
			m_annotationAdjuster.OnFinishedEdit();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TeEditingHelper adds an extra menu option to run the spelling dialog on all
		/// Scripture texts.
		/// </summary>
		/// <param name="mousePos">The location of the mouse</param>
		/// <param name="rootb">The rootbox</param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <param name="menu">to add items to.</param>
		/// <returns>
		/// the number of menu items added (not counting a possible separator line)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int MakeSpellCheckMenuOptions(Point mousePos, IVwRootBox rootb,
			Rectangle rcSrcRoot, Rectangle rcDstRoot, ContextMenuStrip menu)
		{
			int index = base.MakeSpellCheckMenuOptions(mousePos, rootb, rcSrcRoot, rcDstRoot, menu);
			if (menu.Items.Count == 0)
				return 0; // can't do it.

			AddToDictMenuItem prevItem = null;
			foreach (object item in menu.Items)
			{
				if (item is AddToDictMenuItem)
				{
					prevItem = item as AddToDictMenuItem;
					break;
				}
			}

			if (prevItem == null)
				return index; // also can't do it.

			ToolStripMenuItem changeSpellingItem =
				new ToolStripMenuItem(TeResourceHelper.GetResourceString("ksChangeSpellingMultiple"));

			changeSpellingItem.Tag = new ChangeSpellingInfo(prevItem.Word, prevItem.WritingSystem, m_cache);
			changeSpellingItem.Click += changeSpellingItem_Click;
			menu.Items.Insert(index++, changeSpellingItem);
			return index;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the changeSpellingItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void changeSpellingItem_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			if (item != null && item.Tag is ChangeSpellingInfo)
				((ChangeSpellingInfo)item.Tag).DoIt(EditedRootBox.Site);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the char style combo box should always be refreshed on selection
		/// changes.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [force char style combo refresh]; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public override bool ForceCharStyleComboRefresh
		{
			get
			{
				CheckDisposed();
				return (ViewType == TeViewType.BackTranslationParallelPrint);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the applicable style contexts which will be passed to the Apply Style dialog
		/// and used for populating the Character style list on the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override List<ContextValues> ApplicableStyleContexts
		{
			get
			{
				CheckDisposed();

				if (ParagraphHvo == 0)
					return null;
				ScrTxtPara para = new ScrTxtPara(m_cache, ParagraphHvo);

				List<ContextValues> allowedContexts = new List<ContextValues>();
				allowedContexts.Add(ContextValues.General);
				if (para.Context != ContextValues.General)
					allowedContexts.Add(para.Context);
				if (InternalContext != ContextValues.General)
					allowedContexts.Add(InternalContext);
				if (IsBackTranslation)
					allowedContexts.Add(ContextValues.BackTranslation);

				return allowedContexts;
			}
			set
			{
				CheckDisposed();
				base.ApplicableStyleContexts = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a mouse-down event.
		/// Inserts a verse number if "insert verse" mode is active.
		/// </summary>
		/// <remarks>
		/// Called when the Zebra is killed
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override void HandleMouseDown()
		{
			CheckDisposed();

			if (InsertVerseActive || InTestMode)
			{
				if (m_InsertVerseMessageFilter != null)
					m_InsertVerseMessageFilter.InsertVerseInProgress = true;
				InsertVerseNumber();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the current view is a Scripture view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsScriptureView
		{
			get
			{
				return (ViewType & TeViewType.Scripture) != 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the current view is a trial publication view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsTrialPublicationView
		{
			get
			{
				return ((ViewType & (TeViewType.Scripture | TeViewType.View2 | TeViewType.Print))
					== (TeViewType.Scripture | TeViewType.View2 | TeViewType.Print));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a correction view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCorrectionView
		{
			get
			{
				return (ViewType & (TeViewType.BackTranslation | TeViewType.View3)) ==
					(TeViewType.BackTranslation | TeViewType.View3);
			}
		}

		// (TimS): When it is determined that we don't need this code anymore (i.e. after the verse
		//		editing is squared away) then delete this code.

		//		/// ------------------------------------------------------------------------------------
		//		/// <summary>
		//		/// Show a verse box for the selection at point <code>point</code>.
		//		/// </summary>
		//		/// <param name="point">where user is pointing</param>
		//		/// ------------------------------------------------------------------------------------
		//		public void ShowVerseBox(System.Drawing.Point point)
		//		{
		//				CheckDisposed();
		//
		//			bool isSelectionInVerseNumber = IsSelectionInVerseNumber();
		//			int ichAnchor = CurrentSelection.AssocPrev && isSelectionInVerseNumber ?
		//				CurrentSelection.IchAnchor - 1 : CurrentSelection.IchAnchor;
		//			if (ichAnchor < 0)
		//				ichAnchor = 0;
		//			// Get the text of the current run (hopefully the verse number?)
		//			SelLevInfo[] selectionInfo = CurrentSelection.LevelInfo;
		//			StTxtPara paragraph = new StTxtPara(m_cache, selectionInfo[0].hvo);
		//			ITsString tssParagraph = paragraph.Contents.UnderlyingTsString;
		//			int currentRun = tssParagraph.get_RunAt(ichAnchor);
		//			string verseText = tssParagraph.get_RunText(currentRun);
		//
		//			// Make a text box appear for the user to edit the verse number in
		//
		//			// If the current run is not a verse number then show an empty box.
		//			VerseBox verseBox = new VerseBox(isSelectionInVerseNumber ?
		//				verseText : string.Empty);
		//			verseBox.Closing += new System.ComponentModel.CancelEventHandler(verseBox_Closing);
		//			verseBox.Location = Control.PointToScreen(
		//				new System.Drawing.Point(point.X - verseBox.Width / 2,
		//				point.Y - verseBox.Height / 2));
		//			verseBox.Show();
		//		}
		//
		//		/// ------------------------------------------------------------------------------------
		//		/// <summary>
		//		/// When a verse box closes, if the user desired a change, then apply that change to
		//		/// the text.
		//		/// </summary>
		//		/// <param name="sender"></param>
		//		/// <param name="e"></param>
		//		/// ------------------------------------------------------------------------------------
		//		private void verseBox_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		//		{
		//			VerseBox verseBox = sender as VerseBox;
		//
		//			// remove handler from the Closing so we don't call this function more than once
		//			// for a given verse box.
		//			verseBox.Closing -= new System.ComponentModel.CancelEventHandler(verseBox_Closing);
		//
		//			if (verseBox != null && verseBox.NewVerseText != null)
		//			{
		//				string undo;
		//				string redo;
		//				TeResourceHelper.MakeUndoRedoLabels("kstidUndoChangeVerseNumber", out undo, out redo);
		//				using (new UndoTaskHelper(Callbacks.EditedRootBox.Site, undo, redo, false))
		//				{
		//					string newVerseText = verseBox.NewVerseText;
		//					bool isSelectionInVerseNumber = IsSelectionInVerseNumber();
		//					int ichAnchor = CurrentSelection.AssocPrev && isSelectionInVerseNumber ?
		//						CurrentSelection.IchAnchor - 1 : CurrentSelection.IchAnchor;
		//					// Get the verse number that the user clicked
		//					SelLevInfo[] selectionInfo = CurrentSelection.LevelInfo;
		//					StTxtPara paragraph = new StTxtPara(m_cache, selectionInfo[0].hvo);
		//					ITsString tssParagraph = paragraph.Contents.UnderlyingTsString;
		//					int currentRun = tssParagraph.get_RunAt(ichAnchor);
		//					int runStart = tssParagraph.get_MinOfRun(currentRun);
		//					int runEnd = tssParagraph.get_LimOfRun(currentRun);
		//
		//					// Use the right writing system
		//					int nothing; // junk variable to let us call a function
		//					int writingSystem = tssParagraph.get_Properties(currentRun).GetIntPropValues(
		//						(int)FwTextPropType.ktptWs, out nothing);
		//
		//					// Apply the verse number to the paragraph
		//
		//					ITsStrBldr bldr = tssParagraph.GetBldr();
		//					int startRunLocation; // where the run starts in the paragraph
		//					int endRunLocation; // where the run ends in the paragraph
		//					// Only replace some of the text if the user had clicked a verse. If they
		//					// selected the Verse Number style with the insertion point somewhere, then
		//					// we want to insert the verse number text right where the insertion point is.
		//					if (isSelectionInVerseNumber)
		//					{
		//						startRunLocation = runStart;
		//						endRunLocation = runEnd;
		//					}
		//					else
		//					{
		//						startRunLocation = ichAnchor;
		//						endRunLocation = ichAnchor;
		//					}
		//					// Modify the paragraph of the verse with the new verse number, and
		//					// write the paragraph back into the text.
		//					bldr.Replace(startRunLocation, endRunLocation, newVerseText,
		//						StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, writingSystem));
		//					paragraph.Contents.UnderlyingTsString = bldr.GetString();
		//				}
		//			}
		//		}
		//
		//		/// ------------------------------------------------------------------------------------
		//		/// <summary>
		//		/// Is the selection in a non-zero-length run of Verse Number style?
		//		/// </summary>
		//		/// <returns>false if not in a run of Verse Number style or if the run is zero-length;
		//		/// true otherwise</returns>
		//		/// ------------------------------------------------------------------------------------
		//		private bool IsSelectionInVerseNumber()
		//		{
		//			SelLevInfo[] selectionInfo = CurrentSelection.LevelInfo;
		//			StTxtPara paragraph = new StTxtPara(m_cache, selectionInfo[0].hvo);
		//			ITsString tssParagraph = paragraph.Contents.UnderlyingTsString;
		//			int ichAnchor;
		//			if (CurrentSelection.IchAnchor == 0 || !CurrentSelection.AssocPrev)
		//				ichAnchor = CurrentSelection.IchAnchor;
		//			else
		//				ichAnchor = CurrentSelection.IchAnchor - 1;
		//			int currentRun = tssParagraph.get_RunAt(ichAnchor);
		//			// If the start and end of the run are the same, it is an empty run and we
		//			// don't want to behave like we are in a run with a real verse number in it.
		//			if (tssParagraph.get_LimOfRun(currentRun) == tssParagraph.get_MinOfRun(currentRun))
		//				return false;
		//			return tssParagraph.get_Properties(currentRun).GetStrPropValue(
		//				(int)FwTextPropType.ktptNamedStyle) == ScrStyleNames.VerseNumber;
		//		}
		#endregion

		#region Overrides of EditingHelper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the caption props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsTextProps CaptionProps
		{
			get
			{
				CheckDisposed();
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.Figure);
				return bldr.GetTextProps();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the PropChanges during a paste should be delayed
		/// until the paste is complete.
		/// </summary>
		/// <remarks>For the BackTranslationParallelPrint pasting causes PropChanges to occur
		/// that cause the selection to be lost if we do not delay the PropChanges (TE-8207).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override bool DelayPastePropChanges
		{
			get
			{
				return m_viewType == TeViewType.BackTranslationParallelPrint;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the cutting of text into the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if cutting is possible.
		/// </returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanCut()</c>.</remarks>
		/// ------------------------------------------------------------------------------------
		public override bool CanCut()
		{
			// make sure we can't cut when a user prompt is selected
			if (CurrentSelection == null)
				return base.CanCut();

			int anchorId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
			int endId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.End);
			return base.CanCut() && anchorId != SimpleRootSite.kTagUserPrompt &&
				endId != SimpleRootSite.kTagUserPrompt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the copying of text into the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if copying is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanCopy()
		{
			// make sure we can't copy when a user prompt is selected
			if (CurrentSelection == null)
				return base.CanCopy();

			int anchorId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
			int endId = CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.End);
			return base.CanCopy() && anchorId != SimpleRootSite.kTagUserPrompt &&
				endId != SimpleRootSite.kTagUserPrompt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new object, given a text representation (e.g., from the clipboard), unless
		/// we are in a footnote or in back translation.
		/// </summary>
		/// <param name="cache">FDO cache representing the DB connection to use</param>
		/// <param name="sTextRep">Text representation of object</param>
		/// <param name="selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object
		/// </param>
		/// <returns>The GUID of the new object, or <c>Guid.Empty</c> if it's illegal to
		/// insert an object in this location.</returns>
		/// ------------------------------------------------------------------------------------
		public override Guid MakeObjFromText(FdoCache cache, string sTextRep,
			IVwSelection selDst, out int kodt)
		{
			CheckDisposed();

			kodt = 0;
			Guid guid = Guid.Empty;
			try
			{
				// Prevent objects from getting created in invalid locations: for example,
				// we are in a footnote, a picture caption or the back translation
				int tagSelection;
				int hvoSelection;
				GetSelectedScrElement(out tagSelection, out hvoSelection);
				if (tagSelection == (int)ScrBook.ScrBookTags.kflidFootnotes ||
					tagSelection == 0 || (m_viewType & TeViewType.FootnoteView) != 0 ||
					IsPictureSelected || IsBackTranslation)
				{
					return guid;
				}

				IScrBook book = GetCurrentBook(cache);

				if (!CmPicture.IsPicture(sTextRep))
				{
					if (m_lastFootnoteTextRepSelection == CurrentSelection)
						m_newFootnoteIndex++; // same selection, increment footnote index
					else
						m_newFootnoteIndex = FindFootnotePosition(book, CurrentSelection);

					// try to make footnote
					StFootnote footnote = StFootnote.CreateFromStringRep((CmObject)book,
						(int)ScrBook.ScrBookTags.kflidFootnotes, sTextRep, m_newFootnoteIndex,
						ScrStyleNames.FootnoteMarker);
					guid = cache.GetGuidFromId(footnote.Hvo);
					kodt = (int)FwObjDataTypes.kodtOwnNameGuidHot;
					//ScrFootnote.RecalculateFootnoteMarkers(book, 0);
					m_lastFootnoteTextRepSelection = CurrentSelection;
				}
			}
			catch
			{
				// try a different type
			}

			return guid == Guid.Empty ?
				base.MakeObjFromText(cache, sTextRep, selDst, out kodt) : guid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// triggered in Disposing DataUpdateMonitor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void OnFinishedEdit()
		{
			base.OnFinishedEdit();
			m_annotationAdjuster.OnFinishedEdit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Triggered when we are starting a top-level editing operation that may require annotation adjustment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void OnAboutToEdit()
		{
			base.OnAboutToEdit();
			m_annotationAdjuster.OnAboutToEdit();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Let the annotation adjuster decide whether we can afford to handle multiple
		/// keystrokes as a unit. Currently the answer is always "no".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool KeepCollectingInput(int nextChar)
		{
			return m_annotationAdjuster.KeepCollectingInput(nextChar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current book.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns>The current book.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual IScrBook GetCurrentBook(FdoCache cache)
		{
			return new ScrBook(cache, ((ITeView)Control).LocationTracker.GetBookHvo(
				CurrentSelection, SelectionHelper.SelLimitType.Anchor));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides the OnKeyPress event in Rootsite to handle typing in a chapter number or
		/// other special cases. Also ensures that the change is wrapped in a single UndoTask
		/// with whatever the annotation adjuster does.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers"></param>
		/// <param name="graphics"></param>
		/// ------------------------------------------------------------------------------------
		public override void OnKeyPress(KeyPressEventArgs e, Keys modifiers, IVwGraphics graphics)
		{
			CheckDisposed();

			// We shouldn't ever handle key presses in the editing helper if we can't edit anyway
			if (!Editable)
			{
				base.OnKeyPress(e, modifiers, graphics);
				return;
			}

			// ENHANCE (TomB/BryanW/TimS): If the user types a number, followed immediately
			// by a non-numeric character, the CollectTypedInput method could mess us up
			// because the alpha character would never get fed into this method. The result
			// would be that the alpha char(s) would get added to the chapter number and
			// the user will have to remove the formatting manually. If this becomes a
			// major issue, we can fix this by setting a flag to temporarily disable
			// CollectTypedInput whenever we get a Chapter digit here.
			bool removeCharStyle = false;
			if (modifiers != Keys.Control && modifiers != Keys.Alt && CurrentSelection != null)
			{
				ITsTextProps ttp = CurrentSelection.GetSelProps(SelectionHelper.SelLimitType.Top);
				if (ttp != null)
				{
					string style = ttp.GetStrPropValue((int) FwTextPropType.ktptNamedStyle);
					if (style == ScrStyleNames.ChapterNumber && !char.IsDigit(e.KeyChar))
						removeCharStyle = true;
					else if (style == ScrStyleNames.VerseNumber && !char.IsDigit(e.KeyChar) &&
							 !SelIPInVerseNumber(CurrentSelection.Selection))
					{
						removeCharStyle = true;
					}
				}
			}

			string stUndo, stRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoTyping", out stUndo, out stRedo);
			stUndo = string.Format(stUndo, e.KeyChar);
			stRedo = string.Format(stRedo, e.KeyChar);
			using (new UndoTaskHelper(Callbacks.EditedRootBox.Site, stUndo, stRedo, false))
			{
				if (removeCharStyle)
					RemoveCharFormattingWithUndo(true);

				// We do this to keep the key press (and annotation side effects)
				// in the same undo task
				m_annotationAdjuster.StartKeyPressed(e, modifiers);
				base.OnKeyPress(e, modifiers, graphics);
				m_annotationAdjuster.EndKeyPressed();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is overriden in order to set the selection properties when around an
		/// Object Replacement Character.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="graphics"></param>
		/// <returns><c>true</c> if we handled the key, <c>false</c> otherwise (e.g. we're
		/// already at the end of the rootbox and the user pressed down arrow key).</returns>
		/// ------------------------------------------------------------------------------------
		public override bool OnKeyDown(KeyEventArgs e, IVwGraphics graphics)
		{
			CheckDisposed();

			IVwSelection vwselOrig = null;

			if (IsSelectionInPrompt(CurrentSelection))
			{
				Debug.Assert(CurrentSelection.IsRange);
				bool? fCollapseToTop = null;
				switch (e.KeyCode)
				{
					case Keys.Right:
						fCollapseToTop = (new LgWritingSystem(m_cache, m_cache.DefaultUserWs)).RightToLeft;
						break;
					case Keys.Left:
						fCollapseToTop = !(new LgWritingSystem(m_cache, m_cache.DefaultUserWs)).RightToLeft;
						break;
					case Keys.F7: // Forced left even in RtL
						fCollapseToTop = true;
						break;
					case Keys.F8: // Forced right even in RtL
						fCollapseToTop = false;
						break;
				}
				if (fCollapseToTop != null)
				{
					// If we're in a prompt, we want the range selection to behave like an IP.
					// To accomplish this we change the current selection into an IP before
					// we send the key stroke to the Views code.
					vwselOrig = CurrentSelection.Selection;
					m_selectionUpdateInProcess = true;
					try
					{
						CurrentSelection.ReduceToIp((bool)fCollapseToTop ? SelectionHelper.SelLimitType.Top :
							SelectionHelper.SelLimitType.Bottom, false, true);

						m_viewSelection = null; // Make sure CurrentSelection isn't out-of-date
					}
					finally
					{
						m_selectionUpdateInProcess = false;
					}
				}
			}

			if (!base.OnKeyDown(e, graphics))
			{
				if (vwselOrig != null)
					vwselOrig.Install();
				return false;
			}

			if (DataUpdateMonitor.IsUpdateInProgress(m_cache.MainCacheAccessor))
				return true; //discard this event

			if (Callbacks.EditedRootBox != null)
			{
				switch (e.KeyCode)
				{
					case Keys.PageUp:
					case Keys.PageDown:
					case Keys.End:
					case Keys.Home:
					case Keys.Left:
					case Keys.Up:
					case Keys.Right:
					case Keys.Down:
					{
						// REVIEW (TimS): Is there an easier way to do this?????

						if (CurrentSelection == null ||
							CurrentSelection.LevelInfo.Length == 0)
							break;

						// If selection is an IP then see if we are on one side or the other of a
						// run containing an Object Replacement Character. If so, associate the
						// selection away from the ORC.
						if (!CurrentSelection.IsRange && CurrentSelection.SelProps != null)
						{
							string sguid = CurrentSelection.SelProps.GetStrPropValue(
								(int)FwTextPropType.ktptObjData);
							if (sguid != null)
							{
								ITsString tss = CurrentSelection.GetTss(SelectionHelper.SelLimitType.Anchor);
								ITsTextProps tssProps =	tss.get_PropertiesAt(CurrentSelection.IchAnchor);
								CurrentSelection.AssocPrev = (tssProps.GetStrPropValue(
									(int)FwTextPropType.ktptObjData) != null);
								CurrentSelection.SetSelection(true);
							}
						}
						break;
					}
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the writing system hvo of the current selection if it's in a back translation;
		/// otherwise return -1 if vernacular.
		/// </summary>
		/// <param name="selLimit">the part of the selection considered (top, bottom, end, anchor)
		/// </param>
		/// <returns>the hvo of the writing system used in the selection in the BT, or -1 if
		/// vernacular
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private int GetCurrentBtWs(SelectionHelper.SelLimitType selLimit)
		{
			if (CurrentSelection.GetTextPropId(selLimit) == (int)CmTranslation.CmTranslationTags.kflidTranslation)
				return CurrentSelection.GetWritingSystem(selLimit);
			else
				return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="vttp"></param>
		/// ------------------------------------------------------------------------------------
		protected override void ChangeStyleForPaste(IVwSelection vwsel, ref ITsTextProps[] vttp)
		{
			base.ChangeStyleForPaste(vwsel, ref vttp);

			Debug.Assert(vttp.Length > 0);
			ITsTextProps props = vttp[0];
			if (props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) ==
				ScrStyleNames.ChapterNumber)
			{
				RemoveCharFormatting(vwsel, ref vttp, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply style to selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ApplyStyle(string sStyleToApply, IVwSelection vwsel,
			ITsTextProps[] vttpPara, ITsTextProps[] vttpChar)
		{
			CheckDisposed();

			IStStyle newStyle = m_scr.FindStyle(sStyleToApply);

			base.ApplyStyle(newStyle != null ? newStyle.Name : null, vwsel, vttpPara, vttpChar);
			// TODO (TimS): we might eventually handle applying a verse number seperatly.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply style to selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ApplyStyle(string sStyleToApply)
		{
			CheckDisposed();

			// Make sure that the Chapter Number style is not applied to non-numeric data
			if (sStyleToApply == ScrStyleNames.ChapterNumber && CurrentSelection != null)
			{
				ITsString tssSelected;
				CurrentSelection.Selection.GetSelectionString(out tssSelected, string.Empty);
				if (tssSelected.Text != null)
				{
					foreach (char ch in tssSelected.Text)
					{
						if (!Char.IsDigit(ch))
						{
							MiscUtils.ErrorBeep();
							return;
						}
					}
				}
			}
			base.ApplyStyle(sStyleToApply);

			// Update the footnote markers if we changed the style of a footnote
			if ((sStyleToApply == ScrStyleNames.CrossRefFootnoteParagraph ||
				sStyleToApply == ScrStyleNames.NormalFootnoteParagraph) &&
				ParagraphHvo > 0)
			{
				// Save the selection
				SelectionHelper prevSelection = CurrentSelection;

				if (m_cache.MarkerIndexCache != null)
					m_cache.MarkerIndexCache.ClearCache();
				StTxtPara para = new StTxtPara(m_cache, ParagraphHvo);
				Debug.Assert(m_cache.GetClassOfObject(para.OwnerHVO) == StFootnote.kClassId);

				StFootnote footnote = new StFootnote(m_cache, para.OwnerHVO);
				ScrBook book = new ScrBook(m_cache, footnote.OwnerHVO);

				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
					book.Hvo, (int)ScrBook.ScrBookTags.kflidFootnotes, 0,
					book.FootnotesOS.Count, book.FootnotesOS.Count);

				// restore the selection
				prevSelection.SetSelection(true);
			}
		}

		// this code was added back when we were trying to display some special UI for editing
		// verse numbers, rather than allowing normal editing.
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Changes the mouse cursor if we are over a verse number
		///// </summary>
		///// <param name="sel">The selection</param>
		///// <returns>True if we set a cursor, false otherwise</returns>
		///// ------------------------------------------------------------------------------------
		//protected override bool SetCustomCursor(IVwSelection sel)
		//{
		//    if (Callbacks.EditedRootBox == null)
		//        return false;
		//    SelectionHelper helper = SelectionHelper.Create(sel, Callbacks.EditedRootBox.Site);
		//    if (helper == null || helper.SelProps == null)
		//        return false;
		//    string styleName =
		//        helper.SelProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
		//    if (styleName == ScrStyleNames.VerseNumber)
		//    {
		//        Control.Cursor = Cursors.Arrow;
		//        return true;
		//    }
		//    return false;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is used to get a notification when an owning object-replacement character
		/// (ORC) is deleted. ORCs are used to mark locations in the text of things like pictures
		/// or footnotes. In the case of footnotes, when an owning footnote ORC is deleted, we
		/// need to find the corresponding footnote and delete it.
		/// </summary>
		/// <param name="guid">The GUID of the footnote being deleted.</param>
		/// ------------------------------------------------------------------------------------
		public override void ObjDeleted(ref Guid guid)
		{
			CheckDisposed();

			if (PreventObjDeletions)
				return;

			// This is a check to avoid fallout from a time when we incorrectly imported BT
			// footnote ORCs as owning.
			if (IsBackTranslation)
				return;

			int footnoteHvo = m_cache.GetIdFromGuid(guid);
			if (footnoteHvo == 0)
				return;

			int clsid = m_cache.GetClassOfObject(footnoteHvo);
			if (clsid == 0)
				return;

			// Delete the footnote.
			if (clsid == StFootnote.kClassId)
			{
				using (new WaitCursor(Control))
				{
					ScrFootnote footnote = new ScrFootnote(m_cache, footnoteHvo);
					int paraHvo = footnote.ContainingParagraphHvo;
					// If there's no (vernacular) paragraph with an ORC that references this footnote,
					// there can't be a translation either.
					if (paraHvo > 0)
					{
						StTxtPara para = new StTxtPara(m_cache, paraHvo);
						para.DeleteAnyBtMarkersForFootnote(guid);
					}
					ScrFootnote.DeleteFootnote(footnote);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a value determining if the new writing systems should be created as a side-effect
		/// of a paste operation.
		/// </summary>
		/// <param name="wsf">writing system factory containing the new writing systems</param>
		/// <param name="destWs">The destination writing system (writing system used at the
		/// selection).</param>
		/// <returns>
		/// 	an indication of how the paste should be handled.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			destWs = 0;
			// For vernacular and back translation text, we return the default Ws for that view.
			if (IsScriptureView)
				destWs = Cache.DefaultVernWs;
			if (IsBackTranslation)
				destWs = Cache.DefaultAnalWs;

			if (destWs == 0)
				destWs = Cache.DefaultVernWs; // set to default vernacular, if 0.

			return PasteStatus.UseDestWs;
		}
		#endregion

		#region Apply Style and related text adjusting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override ApplyParagraphStyle so that changes in text structure can be handled
		/// correctly.
		/// </summary>
		/// <param name="newParagraphStyle"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool ApplyParagraphStyle(string newParagraphStyle)
		{
			CheckDisposed();

			// Paragraph style changes are not permitted in a back translation view.
			if (IsBackTranslation)
			{
				MiscUtils.ErrorBeep();
				return false;
			}

			// Paragraph style changes are restricted to paragraphs in a single StText
			if (!IsSingleTextSelection())
				return false;

			// Check contexts of new and existing paragraph style
			SelLevInfo[] levInfo =
				CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);

			// only apply a paragraph style to a paragraph....
			if (m_cache.GetClassOfObject(levInfo[0].hvo) != StTxtPara.kClassId)
				return false;

			ScrTxtPara para = new ScrTxtPara(m_cache, levInfo[0].hvo);
			string curStyleName = para.StyleName;

			IStStyle curStyle = m_scr.FindStyle(curStyleName);
			IStStyle newStyle = m_scr.FindStyle(newParagraphStyle);
			if (curStyle == null)
				curStyle = ((FwStyleSheet)Callbacks.EditedRootBox.Stylesheet).FindStyle(para.DefaultStyleName);
			if (curStyle == null || newStyle == null)
			{
				// This should no longer be possible, but to be safe...
				Debug.Assert(curStyle != null);
				Debug.Assert(newStyle != null);
				return true;
			}

			//REVIEW: If you apply IntroPara style to a section head, what behavior??
			//TODO (MarkB):  If Contexts are not equal, do not apply the change. This should
			//	only happen in case of a program error so some kind of debug message
			//	might be appropriate. Maybe do this in the AdjustTextStructure function.
			//REVIEW (MarkB): AdjustTextStructure only if (Context = "Intro" OR "Text") AND
			//	Structures are not equal. Maybe do this in the AdjustTextStructure function.
			bool applyStyleChange = true;
			if ((curStyle.Context != newStyle.Context &&
				newStyle.Context != ContextValues.General) ||
				curStyle.Structure != newStyle.Structure)
			{
				applyStyleChange = AdjustTextStructure(CurrentSelection, curStyle, newStyle);
			}

			if (applyStyleChange)
				return base.ApplyParagraphStyle(newStyle.Name);
			else
				return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if anchor and end of selection are in paragraphs belonging to the same
		/// StText.
		/// </summary>
		/// <returns><code>true</code> if selection in pargraphs of one StText</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSingleTextSelection()
		{
			if (CurrentSelection == null || CurrentSelection.Selection == null)
				return false;

			// If not a range selection, must be in one text
			if (!CurrentSelection.Selection.IsRange)
				return true;

			// If number of levels in anchor and end are different, they can't be
			// in the same text.
			if (CurrentSelection.GetNumberOfLevels(SelectionHelper.SelLimitType.Anchor) !=
				CurrentSelection.GetNumberOfLevels(SelectionHelper.SelLimitType.End))
			{
				return false;
			}

			SelLevInfo[] anchor =
				CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			SelLevInfo[] end = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.End);

			// Selections should be same except possibly at level 0 (paragraphs of StText).
			for (int i = 1; i < anchor.Length; i++)
				if (anchor[i].ihvo != end[i].ihvo || anchor[i].tag != end[i].tag)
					return false;

			// All checks passed - is in same text
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Enter, Delete, and Backspace require special handling in TE.
		/// </summary>
		/// <param name="ch">Typed character</param>
		/// <param name="fCalledFromKeyDown">True if this method gets called from OnKeyDown</param>
		/// <param name="stuInput">input string</param>
		/// <param name="cchBackspace">number of backspace characters in stuInput</param>
		/// <param name="cchDelForward">number of delete characters in stuInput</param>
		/// <param name="ss">Status of Shift/Control/Alt key</param>
		/// <param name="graphics">graphics for processing input</param>
		/// <param name="modifiers">key modifiers - shift status, etc.</param>
		/// <remarks>I (EberhardB) added the parameter <paramref name="fCalledFromKeyDown"/>
		/// to be able to distinguish between Ctrl-Delete and Ctrl-Backspace.</remarks>
		/// -----------------------------------------------------------------------------------
		protected override void OnCharAux(char ch, bool fCalledFromKeyDown, string stuInput,
			int cchBackspace, int cchDelForward, VwShiftStatus ss,
			IVwGraphics graphics, Keys modifiers)
		{
			if (ch == '\r')
			{
				if (IsBackTranslation)
					GoToNextPara();
				else
					HandleEnterKey(fCalledFromKeyDown, stuInput, cchBackspace, cchDelForward, ss, graphics, modifiers);
				return;
			}

			// Call the base to handle the key
			base.OnCharAux(ch, fCalledFromKeyDown, stuInput, cchBackspace, cchDelForward, ss,
				graphics, modifiers);

			if ((ch == 8 || ch == 127) && IsPictureReallySelected && CanDelete())
				DeletePicture();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the deleting of text (or a picture) is possible.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override bool CanDelete()
		{
			if (!base.CanDelete())
				return false;
			if (Callbacks.EditedRootBox.Selection.SelType == VwSelType.kstPicture)
			{
				if (ContentType != StVc.ContentTypes.kctNormal)
				{
					return false; // can't delete pictures in BT views.
				}
				// Also can't delete in BT side of parallel side-by-side view.
				SelLevInfo info;
				if (CurrentSelection.GetLevelInfoForTag(StTxtPara.SegmentsFlid(Cache), SelectionHelper.SelLimitType.Anchor, out info))
					return false;
			}
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// If user presses Enter and a new style is applied to the following paragraph, we
		/// need to mark that style as being in use. If in a section Head, we might need to fix
		/// the structure.
		/// </summary>
		/// <param name="fCalledFromKeyDown">True if this method gets called from OnKeyDown</param>
		/// <param name="stuInput">input string</param>
		/// <param name="cchBackspace">number of backspace characters in stuInput</param>
		/// <param name="cchDelForward">number of delete characters in stuInput</param>
		/// <param name="ss">Status of Shift/Control/Alt key</param>
		/// <param name="graphics">graphics for processing input</param>
		/// <param name="modifiers">key modifiers - shift status, etc.</param>
		/// <remarks>I (EberhardB) added the parameter <paramref name="fCalledFromKeyDown"/>
		/// to be able to distinguish between Ctrl-Delete and Ctrl-Backspace.</remarks>
		/// -----------------------------------------------------------------------------------
		protected void HandleEnterKey(bool fCalledFromKeyDown, string stuInput,
			int cchBackspace, int cchDelForward, VwShiftStatus ss,
			IVwGraphics graphics, Keys modifiers)
		{
			if (IsPictureSelected) // Enter should do nothing if a picture or caption is selected.
				return;

			SelLevInfo[] levInfo;
			// If we are at the end of a heading paragraph, we need to check the "next" style to
			// see if it is a body type. If it is not, then allow processing to proceed as normal.
			// If it is a body type, then don't create a new paragraph, just move down to the start
			// of the first body paragraph in the section.
			if (InSectionHead)
			{
				// if the selection is a range selection then try to delete the selected text first.
				if (CurrentSelection.Selection.IsRange)
				{
					ITsStrFactory factory = TsStrFactoryClass.Create();
					CurrentSelection.Selection.ReplaceWithTsString(
						factory.MakeString("", m_cache.DefaultVernWs));
					// If selection is still a range selection, the deletion failed and we don't
					// need to do anything else.
					if (CurrentSelection.Selection.IsRange || !InSectionHead)
						return;
				}

				// If the heading style has a following style that is a body style and we are at the
				// end of the paragraph then move the IP to the beginning of the body paragraph.
				levInfo = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
				// This is the paragraph that was originally selected
				ScrTxtPara headPara = new ScrTxtPara(m_cache, levInfo[0].hvo);
				IStStyle headParaStyle = m_scr.FindStyle(headPara.StyleName);
				IStStyle followStyle = headParaStyle != null ? headParaStyle.NextRA : null;

				if (followStyle != null && followStyle.Structure == StructureValues.Body &&
					SelectionAtEndParagraph())
				{
					// if there is another section head paragraph, then the section needs to be split
					ScrSection section = new ScrSection(m_cache,
						((ITeView)Control).LocationTracker.GetSectionHvo(CurrentSelection,
						SelectionHelper.SelLimitType.Anchor));
					if (CurrentSelection.LevelInfo[0].ihvo < section.HeadingOA.ParagraphsOS.Count - 1)
					{
						// Setting the style rules destroys the selection, so we have to remember
						// the current location before we change the style rules.
						int iBook = BookIndex;
						int iSection = SectionIndex;

						// break the section
						// create a new empty paragraph in the first section
						// set the IP to the start of the new paragraph
						CreateSection(BCVRef.GetVerseFromBcv(section.VerseRefMin) == 0);
						Debug.Assert(CurrentSelection != null && CurrentSelection.IsValid,
							"Creating the section didn't set a selection");
						StTxtPara contentPara = new StTxtPara(m_cache, CurrentSelection.LevelInfo[0].hvo);
						contentPara.StyleRules = StyleUtils.ParaStyleTextProps(followStyle.Name);
						SetInsertionPoint(iBook, iSection, 0, 0, false);
					}
					else
					{
						SetInsertionPoint(BookIndex, SectionIndex, 0, 0, false);
						// If the first paragraph is not empty, then insert a new paragraph with the
						// follow-on style of the section head.
						StTxtPara contentPara = new StTxtPara(m_cache, CurrentSelection.LevelInfo[0].hvo);
						if (contentPara.Contents.Length > 0)
						{
							StTxtParaBldr bldr = new StTxtParaBldr(m_cache);
							bldr.ParaProps = StyleUtils.ParaStyleTextProps(followStyle.Name);
							bldr.AppendRun(String.Empty, StyleUtils.CharStyleTextProps(null,
								m_cache.DefaultVernWs));
							bldr.CreateParagraph(contentPara.OwnerHVO, 0);
							SetInsertionPoint(BookIndex, SectionIndex, 0, 0, false);
						}
					}
					return;
				}
			}

			// Call the base to handle the key
			base.OnCharAux('\r', fCalledFromKeyDown, stuInput, cchBackspace, cchDelForward, ss,
				graphics, modifiers);

			try
			{
				levInfo = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
				ScrTxtPara para = new ScrTxtPara(m_cache, levInfo[0].hvo);
				IStStyle style = m_scr.FindStyle(para.StyleName);
				if (style != null)
					style.InUse = true;
			}
			catch
			{
				// Oh, well. We tried.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if current selection is at the end of the paragraph
		/// </summary>
		/// <remarks>This is only valid if the current selection is in the vernacular</remarks>
		/// <returns>true if selection is at end</returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionAtEndParagraph()
		{
			SelLevInfo paraInfo =
				CurrentSelection.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs);
			StTxtPara para = new StTxtPara(m_cache, paraInfo.hvo);
			return para.Contents.Length == CurrentSelection.IchAnchor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes changes in text structure when the context of the new paragraph style does not
		/// match the context of the current paragraph style.
		/// </summary>
		/// <remarks>This method properly adjusts the text structure for newStyle; the style is
		/// not applied here - the caller should do that.</remarks>
		/// <param name="selHelper">the selection to which the new style is being applied</param>
		/// <param name="curStyle">current style at the selection anchor</param>
		/// <param name="newStyle">new style to be applied (has a new style context)</param>
		/// <returns>Returns true if the style change can now be applied.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool AdjustTextStructure(SelectionHelper selHelper,
			IStStyle curStyle, IStStyle newStyle)
		{
			SelLevInfo[] top = selHelper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			SelLevInfo[] bottom = selHelper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom);
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int scrLevelCount = tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent);

			// Adjustments will only be done in section level selections.

			if (top.Length != scrLevelCount || bottom.Length != scrLevelCount)
				return true;

			// CASE 1: Change a section head to a content paragraph
			// If style of paragraphs of the section heading are changed to something
			// that is not a section head style, restructure heading paragraphs
			if (top[1].tag == (int)ScrSection.ScrSectionTags.kflidHeading &&
				newStyle.Structure == StructureValues.Body)
			{
				// these will be adjusted to the new indices after the structure is changed
				int iSection = tracker.GetSectionIndexInBook(selHelper, SelectionHelper.SelLimitType.Top);
				int iPara;
				bool firstSectionInContext = (iSection == 0);
				int iBook = tracker.GetBookIndex(selHelper, SelectionHelper.SelLimitType.Top);
				ScrBook book = BookFilter.GetBook(iBook);
				ScrSection section = (ScrSection)book.SectionsOS[iSection];
				// for an interior section, need to check context of next section
				if (!firstSectionInContext)
				{
					IScrSection prevSection = book.SectionsOS[iSection - 1];
					firstSectionInContext = !SectionsHaveSameContext(section, prevSection);
				}
				// If all paragraphs of heading are selected, merge selected section
				// with previous section
				if (AreAllParagraphsSelected(top, bottom, section.HeadingOA))
				{
					if (!firstSectionInContext)
					{
						iPara = ScrSection.MergeIntoPreviousSectionContent(m_cache, book, iSection);
						iSection--;
					}
					else
					{
						// Just need to move all heading paragraphs to the content. The move
						// method will create a new empty heading paragraph.
						iPara = 0;
						section.MoveHeadingParasToContent(0);
					}
				}
				// If selection starts at first paragraph of the heading, move heading paragraphs to
				// content of previous section.
				else if (top[0].ihvo == 0)
				{
					if (!firstSectionInContext)
					{
						iPara = ScrSection.MoveHeadingToPreviousSectionContent(m_cache,
							book, iSection, bottom[0].ihvo);
						iSection--;
					}
					else
					{
						// In this case, we want the selected paragraphs to become the content
						// of a new section, and the preceding paragraph(s) to become the
						// heading of the new section.
						section.ChangeParagraphToSectionContent(top[0].ihvo,
							bottom[0].ihvo - top[0].ihvo + 1);
						// update insertion point to first paragraph(s) of new section content
						iPara = 0;
					}
				}
				else
				{
					// If selection bottoms at the last paragraph of the heading, move heading
					// paragraphs to content of this section
					if (section.HeadingOA.ParagraphsOS.Count - 1 == bottom[0].ihvo)
					{
						section.MoveHeadingParasToContent(top[0].ihvo);
						iPara = 0;
					}
					else
						// The selection must be only inner paragraphs in the section head,
						// not including the first or last paragraphs.
						// In this case, we want the selected paragraph(s) to become content for
						// the heading paragraph(s) above it, and the following section head
						// paragraph(s) become the heading of a new section object.
					{
						int iParaStart = top[0].ihvo;
						int iParabottom = bottom[0].ihvo;
						section.SplitSectionHeading(iSection, iParaStart, iParabottom);

						iPara = 0;
					}
				}

				// Select all paragraphs in content that were part of section heading (and have
				// now been moved to iSection, iPara)
				SetInsertionPoint(iBook, iSection, iPara, 0, false);
				// Get bottom point of selection and update it to point to beginning of
				// last paragraph of old heading.  Use offsets of current selection.
				SelectionHelper newHelper = SelectionHelper.Create(Callbacks.EditedRootBox.Site);
				SelLevInfo[] levInfo = newHelper.GetLevelInfo(SelectionHelper.SelLimitType.End);
				levInfo[0].ihvo += bottom[0].ihvo - top[0].ihvo;
				newHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
				newHelper.IchAnchor = selHelper.GetIch(SelectionHelper.SelLimitType.Top);
				newHelper.IchEnd = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
				newHelper.SetSelection(true);
			} //bottom of CASE 1 "Change a section head to a scripture paragraph"

				// CASE 2: Change scripture paragraph to a section head
				// - only if the new style has "section head" structure, the selection is in
				// one paragraph, and that paragraph is part of the section content
			else if (top[1].tag == (int)ScrSection.ScrSectionTags.kflidContent &&
				newStyle.Structure == StructureValues.Heading)
			{
				// Check selected paragraphs for chapter or verse numbers - style will not be
				// changed if any are found.
				ScrSection section = new ScrSection(m_cache, tracker.GetSectionHvo(selHelper,
					SelectionHelper.SelLimitType.Top));
				for (int i = top[0].ihvo; i <= bottom[0].ihvo; i++)
				{
					ScrTxtPara para =
						new ScrTxtPara(m_cache, section.ContentOA.ParagraphsOS.HvoArray[i]);
					if (para.HasChapterOrVerseNumbers())
					{
						// Cancel the request if chapter or verse numbers are present.
						// display message box if not running in a test
						if (!InTestMode)
						{
							MessageBox.Show(Control,
								TeResourceHelper.GetResourceString("kstidParaHasNumbers"),
								TeResourceHelper.GetResourceString("kstidApplicationName"),
								MessageBoxButtons.OK);
						}
						return false;
					}
				}

				int iBook = tracker.GetBookIndex(selHelper, SelectionHelper.SelLimitType.Top);
				int iSection = tracker.GetSectionIndexInBook(selHelper, SelectionHelper.SelLimitType.Top);
				int iPara;
				ScrBook book = section.OwningBook;
				// End of book is end of context type
				bool lastSectionInContext = (iSection == book.SectionsOS.Count - 1);
				// for an interior section, need to check context of next section
				if (!lastSectionInContext)
				{
					IScrSection nextSection = book.SectionsOS[iSection + 1];
					lastSectionInContext = !SectionsHaveSameContext(section, nextSection);
				}

				if (AreAllParagraphsSelected(top, bottom, section.ContentOA))
				{
					if (!lastSectionInContext)
					{
						// Need to combine this section with the following section.
						// Heading of combined section will be section1.Heading +
						// section1.Content + section2.Heading
						iPara = section.HeadingOA.ParagraphsOS.Count;
						IScrSection nextSection = book.SectionsOS[iSection + 1];
						StText.MoveTextContents(section.ContentOA, nextSection.HeadingOA, false);
						StText.MoveTextContents(section.HeadingOA, nextSection.HeadingOA, false);
						nextSection.AdjustReferences();
						book.SectionsOS.RemoveAt(iSection);
					}
					else
					{
						// Just need to move all content paragraphs to the heading. The move
						// method will create a new empty content paragraph.
						iPara = section.HeadingOA.ParagraphsOS.Count;
						section.MoveContentParasToHeading(section.ContentOA.ParagraphsOS.Count - 1);
					}
				}
				else if (top[0].ihvo == 0)
				{
					// Move the first content paragraphs to become the last para of the
					// section head
					section.MoveContentParasToHeading(bottom[0].ihvo);

					iPara = section.HeadingOA.ParagraphsOS.Count -
						(bottom[0].ihvo - top[0].ihvo + 1);
				}
				else if (bottom[0].ihvo == section.ContentOA.ParagraphsOS.Count - 1 &&
					!lastSectionInContext)
				{
					// Move the last content paragraphs to become the first para of the next
					// section in the book.
					ScrSection.MoveContentParasToNextSectionHeading(m_cache, book, iSection,
						top[0].ihvo);
					// update insertion point to first paragraph(s) of next section head
					iSection++;
					iPara = 0;
				}
				else
				{
					// In this case, we want the selected paragraph to become the heading
					// of a new section, and the following paragraph(s) to become the
					// content of the new section object.
					section.ChangeParagraphToSectionHead(top[0].ihvo,
						bottom[0].ihvo - top[0].ihvo + 1);
					// update insertion point to first paragraph(s) of next section head
					iSection++;
					iPara = 0;
				}

				// Select all paragraphs in content that were part of section heading (and have
				// now been moved to iSection, iPara)
				SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, iBook,
					iSection, iPara);
				// Get bottom point of selection and update it to point to beginning of
				// last paragraph of old heading.  Use offsets of current selection.
				SelectionHelper newHelper =
					SelectionHelper.Create(Callbacks.EditedRootBox.Site);
				SelLevInfo[] levInfo = newHelper.GetLevelInfo(SelectionHelper.SelLimitType.End);
				levInfo[0].ihvo += bottom[0].ihvo - top[0].ihvo;
				newHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
				newHelper.IchAnchor = selHelper.GetIch(SelectionHelper.SelLimitType.Top);
				newHelper.IchEnd = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
				newHelper.SetSelection(true);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the selection represented by the given top and bottom selection
		/// level information covers the entire range of paragraphs in the selcted text
		/// </summary>
		/// <param name="top">The selection levels corresponding to the top of the selection</param>
		/// <param name="bottom">The selection levels corresponding to the bottom of the
		/// selection</param>
		/// <param name="text">The text.</param>
		/// <returns>
		/// 	<c>true</c> if all paragraphs of the text are included in the selection;
		///		<c>false</c> otherwise
		/// </returns>
		/// <exception cref="Exception">If the selection is invalid, spans multiple StTexts, or
		/// covers more paragraphs than actually exist in the StText</exception>
		/// ------------------------------------------------------------------------------------
		private bool AreAllParagraphsSelected(SelLevInfo[] top, SelLevInfo[] bottom, IStText
			text)
		{
			if (text.Hvo != top[1].hvo || top[1].hvo != bottom[1].hvo ||
				text.ParagraphsOS.Count <= bottom[0].ihvo)
			{
				throw new Exception("Selection is invalid! Top and bottom of selection do not " +
					"correspond to the paragraphs of the same text. textHvo = " + text.Hvo +
					"; top[1].hvo = " + top[1].hvo + "; bottom[1].hvo = " + bottom[1].hvo +
					"; text.ParagraphsOS.Count = " + text.ParagraphsOS.Count +
					"; bottom[0].ihvo = " + bottom[0].ihvo +
					". Please save a backup copy of your database so that developers can attempt to find the cause of this problem (TE-5717).");
			}

			if (top[0].ihvo != 0)
				return false;

			// verify that number of selected paragraphs equals paragraphs in StText
			StText topText = new StText(m_cache, top[1].hvo);
			return topText.ParagraphsOS.Count == bottom[0].ihvo + 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to determine if the selection within StText has an edge at or near
		/// the section boundary. I.e.: the selection top is within the first paragraph
		/// in the heading of the section OR the selection bottom is within the last paragraph
		/// in the content of the section.
		/// </summary>
		/// <param name="top">Top of the selection.</param>
		/// <param name="bottom">Bottom of the selection.</param>
		/// <returns>True if the selection is within an StText
		/// AND EITHER the selection top is within the first paragraph of the heading
		/// OR the selection bottom is within the last paragraph of the content.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionNearSectionBoundary(SelLevInfo[] top, SelLevInfo[] bottom)
		{
			StText text = new StText(m_cache, bottom[1].hvo);
			int headingFlid = (int)ScrSection.ScrSectionTags.kflidHeading;
			int contentFlid = (int)ScrSection.ScrSectionTags.kflidContent;

			return
				// Applies only within an StText;
				(top[1].ihvo == bottom[1].ihvo)  &&
				// Top within first paragraph of heading
				((top[1].tag == headingFlid && top[0].ihvo == 0) ||
				// Bottom within last paragraph of content
				(bottom[1].tag == contentFlid && text.ParagraphsOS.Count == bottom[0].ihvo + 1));
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to determine if the selection within StText has an edge adjacent to
		/// the section's other StText. I.e.: the selection bottom is within the last paragraph
		/// in the heading of the section OR the selection top is within the first paragraph
		/// in the content of the section.
		/// </summary>
		/// <param name="top">Top of the selection.</param>
		/// <param name="bottom">Bottom of the selection.</param>
		/// <returns>True if the selection is within an StText
		/// AND EITHER the selection bottom is within the last paragraph of the heading
		/// OR the selection top is within the first paragraph of the content.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionNearInternalBoundary(SelLevInfo[] top, SelLevInfo[] bottom)
		{
			StText text = new StText(m_cache, top[1].hvo);
			int headingFlid = (int)ScrSection.ScrSectionTags.kflidHeading;
			int contentFlid = (int)ScrSection.ScrSectionTags.kflidContent;

			return
				// Applies only within an StText;
				(top[1].ihvo == bottom[1].ihvo) &&
				// Top within first paragraph of content
				((top[1].tag == contentFlid && top[0].ihvo == 0) ||
				// Bottom within last paragraph of heading
				(bottom[1].tag == headingFlid && text.ParagraphsOS.Count == bottom[0].ihvo + 1));
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to determine if the selection within StText has an edge adjacent to
		/// a different context. I.e.: the selection top is within the first paragraph
		/// of a context OR the selection bottom is within the last paragraph of a context.
		/// </summary>
		/// <param name="top">Top of the selection.</param>
		/// <param name="bottom">Bottom of the selection.</param>
		/// <returns>True if the selection is within an StText
		/// AND EITHER the selection top is within the first paragraph of the context
		/// OR the selection bottom is within the last paragraph of the context.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool SelectionNearContextBoundary(SelLevInfo[] top, SelLevInfo[] bottom)
		{
			int headingFlid = (int)ScrSection.ScrSectionTags.kflidHeading;
			int contentFlid = (int)ScrSection.ScrSectionTags.kflidContent;
			if (top[1].ihvo != bottom[1].ihvo)
				return false; // Applies only within an StText

			StText curText = new StText(m_cache, top[1].hvo);
			return
				// Top within first paragraph of heading of first section
				((top[1].tag == headingFlid & true) ||
				// Bottom within last paragraph of context
				(bottom[1].tag == contentFlid & true));
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Reset Paragraph Style menu/toolbar command.
		///
		/// Resets the style of the paragraph to be the default style for the current context.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void ResetParagraphStyle()
		{
			CheckDisposed();

			if (ParagraphHvo == 0)
				return;
			// Reset paragraph style only active when selection is in a single
			// paragraph, so can just get selected paragraph from anchor
			ScrTxtPara para = new ScrTxtPara(m_cache, ParagraphHvo);
			ApplyStyle(para.DefaultStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override for the special semantics method. Treat verse and chapter numbers as
		/// special sematics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool SpecialSemanticsCharacterStyle(string name)
		{
			CheckDisposed();

			if (name == ScrStyleNames.VerseNumber || name == ScrStyleNames.ChapterNumber)
				return true;
			return false;
		}
		#endregion

		#region Selection Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <param name="scrollOption">Where to scroll the selection</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int tag, int book, int section, int para,
			int character, bool fAssocPrev, VwScrollSelOpts scrollOption)
		{
			CheckDisposed();

			return SelectRangeOfChars(book, section, tag, para, character, character,
				true, true, fAssocPrev, scrollOption);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual SelectionHelper SetInsertionPoint(int tag, int book, int section, int para,
			int character, bool fAssocPrev)
		{
			CheckDisposed();

			return SelectRangeOfChars(book, section, tag, para, character, character,
				true, true, fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int book, int section, int para,
			int character, bool fAssocPrev)
		{
			CheckDisposed();

			return SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent, book,
				section, para, character, fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of any Scripture element: Title, Section Head, or Section
		/// Content.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBook.ScrBookTags.kflidTitle"/>
		/// </param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int tag, int book, int section)
		{
			CheckDisposed();

			return SetInsertionPoint(tag, book, section, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of any Scripture element: Title, Section Head, or Section
		/// Content.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBook.ScrBookTags.kflidTitle"/>
		/// </param>
		/// <param name="paragraph">The 0-based index of the paragraph which to put the
		/// insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int tag, int book, int section, int paragraph)
		{
			CheckDisposed();

			return SelectRangeOfChars(book, section, tag, paragraph, 0, 0, true, true, false,
				VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the text in the specified paragraph in the specified scripture book at
		/// the specified offsets.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SelectRangeOfChars(int iBook, StTxtPara para, int ichStart, int ichEnd)
		{
			// The text still exists at the same position, so highlight it.
			StText text = para.Owner as StText;
			int iSection = -1;
			if (text.OwningFlid != (int)ScrBook.ScrBookTags.kflidTitle)
				iSection = text.Owner.IndexInOwner;

			SelectRangeOfChars(iBook, iSection, text.OwningFlid, para.IndexInOwner,
				ichStart, ichEnd, true, true, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection in this DraftView which is
		/// "installed" and scrolled into view.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// <remarks>This method is only used for tests</remarks>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int iPara,
			int startCharacter, int endCharacter)
		{
			CheckDisposed();

			return SelectRangeOfChars(iBook, iSection, (int)ScrSection.ScrSectionTags.kflidContent,
				iPara, startCharacter, endCharacter, true, true, true, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection in this DraftView which is
		/// "installed", scrolled into view using the default scroll option.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="tag">Indicates whether selection should be made in the section
		/// Heading or Content or in the book title</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="fAssocPrev"></param>
		/// <returns>The selection helper</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int tag,
			int iPara, int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			bool fAssocPrev)
		{
			CheckDisposed();

			return SelectRangeOfChars(iBook, iSection, tag, iPara, startCharacter, endCharacter,
				fInstall, fMakeVisible, fAssocPrev, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the actual workhorse for all the above methods that allows a selection to
		/// be created.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="tag">Indicates whether selection should be made in the section
		/// Heading or Content or in the book title</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="fAssocPrev">If an insertion point, does it have the properties of the
		/// previous character?</param>
		/// <param name="scrollOption">Where to scroll the selection</param>
		/// <returns>The selection helper</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int tag,
			int iPara, int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			bool fAssocPrev, VwScrollSelOpts scrollOption)
		{
			Debug.Assert(ContentType != StVc.ContentTypes.kctSegmentBT, "isegment arg required for segment BT SelectRangeOfChars");
			return SelectRangeOfChars(iBook, iSection, tag, iPara, 0, startCharacter, endCharacter, fInstall, fMakeVisible,
									  fAssocPrev, scrollOption);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the actual workhorse for all the above methods that allows a selection to
		/// be created.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// selection</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// selection</param>
		/// <param name="tag">Indicates whether selection should be made in the section
		/// Heading or Content or in the book title</param>
		/// <param name="isegment">If ContentType == segmentBT, the index of the segment
		/// in which to place the selection; otherwise ignored.</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="fAssocPrev">If an insertion point, does it have the properties of the
		/// previous character?</param>
		/// <param name="scrollOption">Where to scroll the selection</param>
		/// <returns>The selection helper</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int iBook, int iSection, int tag,
			int iPara, int isegment, int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			bool fAssocPrev, VwScrollSelOpts scrollOption)
		{
			CheckDisposed();

			if (Callbacks == null || Callbacks.EditedRootBox == null)
				return null;  // can't make a selection

			Debug.Assert(tag == (int)ScrSection.ScrSectionTags.kflidHeading ||
				tag == (int)ScrSection.ScrSectionTags.kflidContent ||
				tag == (int)ScrBook.ScrBookTags.kflidTitle);

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = ((ITeView)Control).LocationTracker.GetLevelCount(tag);
			int levelForPara = LocationTrackerImpl.GetLevelIndexForTag((int)StText.StTextTags.kflidParagraphs,
				m_contentType);

			selHelper.LevelInfo[levelForPara].ihvo = iPara;
			selHelper.LevelInfo[levelForPara + 1].tag = tag;

			((ITeView)Control).LocationTracker.SetBookAndSection(selHelper,
				SelectionHelper.SelLimitType.Anchor, iBook,
				tag == (int)ScrBook.ScrBookTags.kflidTitle ? -1 : iSection);

			if (ContentType == StVc.ContentTypes.kctSimpleBT)
			{
				int levelForBT = LocationTrackerImpl.GetLevelIndexForTag((int)StTxtPara.StTxtParaTags.kflidTranslations,
					m_contentType);
				selHelper.LevelInfo[levelForBT].tag = -1;
				selHelper.LevelInfo[levelForBT].ihvo = 0;
				selHelper.LevelInfo[levelForPara].tag = (int)StText.StTextTags.kflidParagraphs;
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
					(int)CmTranslation.CmTranslationTags.kflidTranslation);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
					(int)CmTranslation.CmTranslationTags.kflidTranslation);
			}
			else if (ContentType == StVc.ContentTypes.kctSegmentBT)
			{
				// In all segment BT views, under the paragraph there is a segment, and under that
				// an object which is the free translation itself.
				selHelper.LevelInfo[2].tag = (int) StText.StTextTags.kflidParagraphs; // JohnT: why don't we need this for non-BT??
				selHelper.LevelInfo[1].ihvo = isegment;
				selHelper.LevelInfo[1].tag = StTxtPara.SegmentsFlid(Cache);
				selHelper.LevelInfo[0].ihvo = 0; // not a sequence.
				selHelper.LevelInfo[0].tag = StTxtPara.SegmentFreeTranslationFlid(Cache);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
					(int)CmAnnotation.CmAnnotationTags.kflidComment);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
					(int)CmAnnotation.CmAnnotationTags.kflidComment);
			}
			// else	selHelper.LevelInfo[0].tag is set automatically by SelectionHelper class

			selHelper.AssocPrev = fAssocPrev;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selHelper.LevelInfo);

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = startCharacter;
			selHelper.IchEnd = endCharacter;

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = selHelper.SetSelection(Callbacks.EditedRootBox.Site, fInstall,
				fMakeVisible, scrollOption);

			// If the selection fails, then try selecting the user prompt.
			if (vwsel == null)
			{
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, SimpleRootSite.kTagUserPrompt);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, SimpleRootSite.kTagUserPrompt);
				vwsel = selHelper.SetSelection(Callbacks.EditedRootBox.Site, fInstall, fMakeVisible,
					scrollOption);
			}

			if (vwsel == null)
			{
				Debug.WriteLine("SetSelection failed in TeEditinHelper.SelectRangeOfChars()");
			}

			return selHelper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in a picture caption.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="iSection">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="tag">Indicates whether the picture ORC is in the section
		/// Heading or Content, the book title</param>
		/// <param name="iPara">The 0-based index of the paragraph containing the ORC</param>
		/// <param name="ichOrcPos">The character position of the orc in the paragraph.</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <exception cref="Exception">Requested selection could not be made in the picture
		/// caption.</exception>
		/// ------------------------------------------------------------------------------------
		public void MakeSelectionInPictureCaption(int iBook, int iSection, int tag,
			int iPara, int ichOrcPos, int startCharacter, int endCharacter)
		{
			CheckDisposed();

			if (Callbacks == null || Callbacks.EditedRootBox == null)
				throw new Exception("Requested selection could not be made in the picture caption.");

			Debug.Assert(tag == (int)ScrSection.ScrSectionTags.kflidHeading ||
				tag == (int)ScrSection.ScrSectionTags.kflidContent ||
				tag == (int)ScrBook.ScrBookTags.kflidTitle);
			Debug.Assert(!IsBackTranslation, "ENHANCE: This code not designed to make a selection in the BT of a picture caption");

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = ((ITeView)Control).LocationTracker.GetLevelCount(tag) + 1;
			int levelForPara = LocationTrackerImpl.GetLevelIndexForTag(
				(int)StText.StTextTags.kflidParagraphs, StVc.ContentTypes.kctNormal) + 1;
			int levelForCaption = LocationTrackerImpl.GetLevelIndexForTag(
				(int)CmPicture.CmPictureTags.kflidCaption, StVc.ContentTypes.kctNormal);
			selHelper.LevelInfo[levelForCaption].ihvo = -1;
			selHelper.LevelInfo[levelForCaption].ich = ichOrcPos;
			selHelper.LevelInfo[levelForCaption].tag = (int)StTxtPara.StTxtParaTags.kflidContents;
			selHelper.Ws = m_cache.DefaultVernWs;
			selHelper.LevelInfo[levelForPara].tag = (int)StText.StTextTags.kflidParagraphs;
			selHelper.LevelInfo[levelForPara].ihvo = iPara;
			selHelper.LevelInfo[levelForPara + 1].tag = tag;

			((ITeView)Control).LocationTracker.SetBookAndSection(selHelper,
				SelectionHelper.SelLimitType.Anchor, iBook,
				tag == (int)ScrBook.ScrBookTags.kflidTitle ? -1 : iSection);

			selHelper.AssocPrev = true;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selHelper.LevelInfo);

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = startCharacter;
			selHelper.IchEnd = endCharacter;
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, (int)CmPicture.CmPictureTags.kflidCaption);
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, (int)CmPicture.CmPictureTags.kflidCaption);

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = selHelper.SetSelection(Callbacks.EditedRootBox.Site, true,
				true, VwScrollSelOpts.kssoDefault);

			if (vwsel == null)
				throw new Exception("Requested selection could not be made in the picture caption.");

			Application.DoEvents(); // REVIEW: Do we need this? Why?
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a simple text selection.
		/// </summary>
		/// <param name="newLevInfo">The new selection level info.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ich">The character offset where the IP should be placed.</param>
		/// ------------------------------------------------------------------------------------
		private void MakeSimpleTextSelection(SelLevInfo[] newLevInfo, int tag, int ich)
		{
			Callbacks.EditedRootBox.MakeTextSelection(
				0, // arbitrarily assume only one root.
				newLevInfo.Length, newLevInfo, // takes us to the paragraph
				tag, // select this property
				0, // first (and only) occurence of contents
				ich, ich, // starts and ends at this char index
				0, // ws is irrelevant (not multilingual)
				true, // associate with previous character...pretty arbitrary
				-1, // all in that one paragraph
				null, // don't impose any special properties for next char typed
				true); // install the new selection.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle TE specific requirements on selection change.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		/// ------------------------------------------------------------------------------------
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			// selection change is being done by this routine, don't need to do processing
			// second time.
			if (m_selectionUpdateInProcess)
				return;

			// Make sure we don't try to use the old selection (TE-5009).
			m_viewSelection = null;

			SelectionHelper helper = SelectionHelper.Create(vwselNew, prootb.Site);

			UpdateGotoPassageControl(); // update the verse reference to the new selection
			SetInformationBarForSelection(); // update title bar with section reference range

			if (MainTransVc != null)
				ProcessBTSelChange(helper);

			bool updatedSelection = false;

			int hvoSelObj = 0;
			if (helper != null && helper.LevelInfo.Length > 0)
				hvoSelObj = helper.LevelInfo[0].hvo;

			IGetTeStVc getVc = prootb.Site as IGetTeStVc;
			TeStVc vc = null;
			if (getVc != null)
				vc = getVc.Vc;

			if (vc != null && vc.SuppressCommentPromptHvo != hvoSelObj)
			{
				vc.SuppressCommentPromptHvo = 0;
				// Enhance JohnT: do a Propchanged (possibly delayed until idle) on hvo.Comment to make the prompt reappear.
			}

			// If the selection is in a user prompt then extend the selection to cover the
			// entire prompt.
			if (IsSelectionInPrompt(helper))
			{
				if (vc != null)
					vc.SuppressCommentPromptHvo = hvoSelObj;

				// If we're not really showing the prompt, but just an incomplete composition that was typed
				// over it, we do NOT want to select all of it all the time! (TE-8267).
				if (!prootb.IsCompositionInProgress)
					vwselNew.ExtendToStringBoundaries();
				if (!vwselNew.IsEditable && helper != null)
				{
					// We somehow got an IP that associates with the non-editable spacer next to the prompt.
					// We need to extend in the opposite direction.
					helper.AssocPrev = !helper.AssocPrev;
					IVwSelection sel = helper.SetSelection(EditedRootBox.Site, false, false);
					// Make sure that the new selection is editable before we install it. This keeps us
					// from getting here again and again (recursively). (TE-8763)
					if (sel.IsEditable)
						sel.Install();
					return; // We have already been called again as the new selection is installed.
				}
				SetKeyboardForSelection(vwselNew);
			}

			// This isn't ideal but it's one of the better of several bad options for dealing
			// with simplifying the selection changes in footnote views.
			if ((m_viewType & TeViewType.FootnoteView) != 0 ||
				(m_viewType & TeViewType.NotesDataEntryView) != 0 || helper == null)
			{
				// This makes sure the writing system and styles combos get updated.
				base.SelectionChanged(prootb, vwselNew);
				return;
			}

			// If selection is IP, don't allow it to be associated with a verse number run.
			bool fRangeSelection = vwselNew.IsRange;
			if (!fRangeSelection)
				PreventIPAssociationWithVerseRun(vwselNew, prootb, ref updatedSelection);

			// Need to do this at end since selection may be changed by this method.
			// Doing this at top can also cause value of style in StylesComboBox to flash
			base.SelectionChanged(prootb, vwselNew);

			// If we changed the selection in this method we want to set the viewSelection to
			// null so that the next time it is gotten it will have the correct selection.
			if (updatedSelection)
				m_viewSelection = null;

			// Make sure the selection is in a valid Scripture element.
			int tagSelection;
			int hvoSelection;
			if (!vwselNew.IsValid || !GetSelectedScrElement(out tagSelection, out hvoSelection))
			{
				m_sPrevSelectedText = null;
				return;
			}

			// Determine whether or not the selection changed but is in a different reference.
			bool fInSameRef = (m_oldReference == CurrentStartRef);
			if (fInSameRef && !fRangeSelection)
			{
				m_sPrevSelectedText = null;
				if (m_syncHandler != null)
					m_syncHandler.SyncToScrLocation(this, this, true);

				return;
			}

			ScrReference curStartRef = CurrentStartRef;
			if (curStartRef.Chapter == 0)
				curStartRef.Chapter = 1;

			m_oldReference = CurrentStartRef;

			string selectedText = null;
			if (fRangeSelection)
			{
				try
				{
					ITsString tssSelectedText = GetCleanSelectedText();
					selectedText = (tssSelectedText != null ? tssSelectedText.Text : null);
				}
				catch
				{
					selectedText = null;
				}
			}

			bool fSameSelectedText = (m_sPrevSelectedText == selectedText);
			m_sPrevSelectedText = selectedText;

			if (m_syncHandler != null && (!fInSameRef || !fSameSelectedText ||
				InBookTitle || InSectionHead || InIntroSection))
			{
				m_syncHandler.SyncToScrLocation(this, this, fInSameRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resends a synchronized scrolling message for the current Scripture reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResendScriptureReference()
		{
			if (m_syncHandler != null && m_oldReference != null)
				m_syncHandler.SyncToScrLocation(this, this, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the BT sel change. (This method probably needs a better name.)
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessBTSelChange(SelectionHelper helper)
		{
			if (helper != null && helper.NumberOfLevels > 2 && helper.LevelInfo[0].tag == StTxtPara.SegmentFreeTranslationFlid(Cache))
			{
				// We're in a segmented back translation...try to highlight the main translation item.
				int hvoSeg = helper.LevelInfo[1].hvo;
				CmBaseAnnotation seg = CmObject.CreateFromDBObject(Cache, hvoSeg) as CmBaseAnnotation;
				StTxtPara para = seg.BeginObjectRA as StTxtPara;
				MainTransVc.SetupOverrides(para, seg.BeginOffset, seg.EndOffset,
					 delegate(ref DispPropOverride prop)
						{
							prop.chrp.clrBack =
							(uint) ColorTranslator.ToWin32(TeResourceHelper.ReadOnlyTextBackgroundColor);
						},
					 Callbacks.EditedRootBox);
			}
			else
			{
				// Not in segmented BT; remove any override. It's still important to pass the edited root box,
				// so the PropChanged doesn't affect us. We might still have a selection in the same paragraph,
				// but (e.g.) in a verse number.
				MainTransVc.SetupOverrides(null, 0, 0, null, Callbacks.EditedRootBox);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prevents IP-only selections from being associated with a verse number run.
		/// </summary>
		/// <param name="vwselNew">The new selection.</param>
		/// <param name="prootb">The root box.</param>
		/// <param name="updatedSelection"></param>
		/// ------------------------------------------------------------------------------------
		private void PreventIPAssociationWithVerseRun(IVwSelection vwselNew, IVwRootBox prootb,
			ref bool updatedSelection)
		{
			// Need to commit any editing in progress - otherwise attempt to read
			// paragraph that follows may get stale data.  This follows the example in
			// EditingHelper.SelectionChanged(). Also verify selection is still valid.
			Commit(vwselNew);
			if (!vwselNew.IsValid)
				return;

			SelectionHelper helper = SelectionHelper.Create(vwselNew, prootb.Site);

			// Get the tss that the IP is in
			ITsString tssSelectedScrPara = null;

			if (helper.LevelInfo.Length > 0)
			{
				switch (m_cache.GetClassOfObject(helper.LevelInfo[0].hvo))
				{
					case CmTranslation.kclsidCmTranslation:
						int hvo = helper.LevelInfo[0].hvo;
						int btWs = Callbacks.GetWritingSystemForHvo(hvo);
						CmTranslation trans = new CmTranslation(m_cache, hvo);
						tssSelectedScrPara = trans.Translation.GetAlternative(btWs).UnderlyingTsString;
						break;

					case StTxtPara.kclsidStTxtPara:
						StTxtPara para = new StTxtPara(m_cache, helper.LevelInfo[0].hvo);
						tssSelectedScrPara = para.Contents.UnderlyingTsString;
						break;
					case CmPicture.kclsidCmPicture:
					case StFootnote.kclsidStFootnote:
					default:
						// Not sure what it is, but it probably doesn't contain verse text...
						break;
				}
			}

			// Following code includes checking zero-length paragraphs for association with
			// the VerseNumber style so that empty paras will use the default character style
			if (tssSelectedScrPara == null)
				return;

			// Get the text props and run info of the run the IP is associating with
			int charPos = helper.IchAnchor;

			if (helper.AssocPrev && charPos > 0)
				charPos -= 1;

			if (charPos > tssSelectedScrPara.Length) // workaround for TE-5561
				charPos = tssSelectedScrPara.Length;

			TsRunInfo tri;
			ITsTextProps ttp = tssSelectedScrPara.FetchRunInfoAt(charPos, out tri);

			// These are the boundary conditions that require our intervention
			// regarding verse numbers.
			bool fEdgeOfTss = (helper.IchAnchor == 0 ||
			helper.IchAnchor == tssSelectedScrPara.Length);
			bool fBeginOfRun = (helper.IchAnchor == tri.ichMin);
			bool fEndOfRun = (helper.IchAnchor == tri.ichLim);

			if (!fEdgeOfTss && !fBeginOfRun && !fEndOfRun)
				return;

			// If the IP is associated with a verse number style run
			if (ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) != ScrStyleNames.VerseNumber)
				return;

			try
			{
				m_selectionUpdateInProcess = true; //set our semaphore

				// We must disassociate the IP from the verse number style run...

				// If IP is at beginning or end of paragraph, need to reset selection to
				// default paragraph chars (null style).
				if (fEdgeOfTss)
				{
					// REVIEW: Should we be re-getting this, or just using the one we have?
					vwselNew = prootb.Selection;
					ITsPropsBldr bldr = ttp.GetBldr();
					bldr.SetStrPropValue((int) FwTextPropType.ktptNamedStyle, null);
					vwselNew.SetIpTypingProps(bldr.GetTextProps());
				}
				else
				{
					// Else make the selection be associated with the other adjacent run.
					helper.AssocPrev = fBeginOfRun;
					helper.SetSelection(true);
				}

				updatedSelection = true;
			}
			finally
			{
				m_selectionUpdateInProcess = false; // reset semaphore
			}
		}

		#endregion

		#region GotoVerse/GetVerseText methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to set the selection immediately following the desired verse reference.
		/// This version of GotoVerse sends a synch message at the end, so it should not be
		/// called from within code that is already responding to a synch message.
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>true if the selection is changed (to the requested verse or one nearby);
		/// false otherwise</returns>
		/// <remarks>
		/// Searching will start at the current selection location and wrap around if necessary.
		/// If the verse reference is not included in the range of any sections, then the
		/// selection will not be changed. If the reference does not exist but it is in the
		/// range of a section, then a best guess will be done.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool GotoVerse(ScrReference targetRef)
		{
			bool retVal = GotoVerse_WithoutSynchMsg(targetRef);

			if (m_syncHandler != null)
				m_syncHandler.SyncToScrLocation(this, this, false);

			return retVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to set the selection immediately following the desired verse reference.
		/// This version of GotoVerse does not issue a synch message, so it is suitable for
		/// calling from the
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>true if the selection is changed (to the requested verse or one nearby);
		/// false otherwise</returns>
		/// <remarks>
		/// Searching will start at the current selection location and wrap around if necessary.
		/// If the verse reference is not included in the range of any sections, then the
		/// selection will not be changed. If the reference does not exist but it is in the
		/// range of a section, then a best guess will be done.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private bool GotoVerse_WithoutSynchMsg(ScrReference targetRef)
		{
			CheckDisposed();

			bool origIgnoreAnySyncMessages = false;
			if (m_syncHandler != null)
			{
				origIgnoreAnySyncMessages = m_syncHandler.IgnoreAnySyncMessages;
				m_syncHandler.IgnoreAnySyncMessages = true;
			}

			try
			{
				ScrBook bookToLookFor = BookFilter.GetBookByOrd(targetRef.Book);
				if (bookToLookFor == null)
					return false;

				int iBook = BookFilter.GetBookIndex(bookToLookFor.Hvo);

				if (targetRef.IsBookTitle)
				{
					SelectRangeOfChars(iBook, -1, (int)ScrBook.ScrBookTags.kflidTitle, 0, 0, 0,
						true, true, false, VwScrollSelOpts.kssoNearTop);
					return true;
				}

				// If the book has no sections, then don't look for any references
				Debug.Assert(bookToLookFor.SectionsOS.Count > 0);
				if (bookToLookFor.SectionsOS.Count == 0)
					return false;

				int startingSectionIndex = 0;
				int startingParaIndex = 0;
				int startingCharIndex = 0;

				// Get the current selection
				if (CurrentSelection != null)
				{
					ILocationTracker tracker = ((ITeView)Control).LocationTracker;
					// If the selection is in the desired book, we start there.
					// Otherwise start at the beginning of the book
					if (tracker.GetBookHvo(CurrentSelection,
						SelectionHelper.SelLimitType.Anchor) == bookToLookFor.Hvo)
					{
						int tmpSectionIndex = tracker.GetSectionIndexInBook(
							CurrentSelection, SelectionHelper.SelLimitType.Anchor);

						if (tmpSectionIndex >= 0)
						{
							startingSectionIndex = tmpSectionIndex;
							SelLevInfo paraInfo;
							if (CurrentSelection.GetLevelInfoForTag(
								(int)StText.StTextTags.kflidParagraphs,	out paraInfo))
							{
								startingParaIndex = paraInfo.ihvo;
								// Start looking 1 character beyond the current selection.
								startingCharIndex = CurrentSelection.IchEnd + 1;
							}
						}
					}
				}

				ScrSection startingSection = bookToLookFor[startingSectionIndex];
				ScrSection section;
				int paraIndex;
				int ichVerseStart;

				// Decide which section to start with. If the current selection
				// is at the end of the section then start with the next section
				StTxtPara lastParaOfSection = startingSection.LastContentParagraph;
				if (startingParaIndex >= startingSection.ContentParagraphCount - 1 &&
					startingCharIndex >= lastParaOfSection.Contents.Length)
				{
					startingSection = (startingSection.NextSection ?? bookToLookFor.FirstSection);
					startingParaIndex = 0;
					startingCharIndex = 0;
				}

				if (bookToLookFor.GetRefStartFromSection(targetRef, false, startingSection, startingParaIndex,
					startingCharIndex, out section, out paraIndex, out ichVerseStart))
				{
					// We found an exact match, so go there.
					GoToPosition(targetRef, iBook, section, paraIndex, ichVerseStart);
				}
				else
				{
					// We didn't find an exact match, so go somewhere close.
					GotoClosestPrecedingRef(targetRef, bookToLookFor);
				}

				return true;
			}
			finally
			{
				if (m_syncHandler != null)
					m_syncHandler.IgnoreAnySyncMessages = origIgnoreAnySyncMessages;
			}
		}

		#region class VerseTextSubstring
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simple class to keep track of pieces of a verse text which are split across
		/// different paragraphs and/or sections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class VerseTextSubstring
		{
			private ITsString m_tssText;
			private int m_iSection;
			private int m_iPara;
			private int m_tag;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="VerseTextSubstring"/> class.
			/// </summary>
			/// <param name="text">The text.</param>
			/// <param name="iSection">The index of the section containing this piece of verse
			/// text.</param>
			/// <param name="iPara">The index of the paragraph containing this piece of verse
			/// text.</param>
			/// <param name="tag">The tag of the text.</param>
			/// --------------------------------------------------------------------------------
			public VerseTextSubstring(ITsString text, int iSection, int iPara, int tag)
			{
				m_tssText = text;
				m_iSection = iSection;
				m_iPara = iPara;
				m_tag = tag;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the length of this substring.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int Length
			{
				get { return m_tssText.Length; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the verse text.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string Text
			{
				get { return m_tssText.Text; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the verse text.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ITsString Tss
			{
				get { return m_tssText; }
				set { m_tssText = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the section.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int SectionIndex
			{
				get { return m_iSection; }
				set { m_iSection = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the index of the paragraph.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int ParagraphIndex
			{
				get { return m_iPara; }
				set { m_iPara = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the tag.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int Tag
			{
				get { return m_tag; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns a <see cref="T:System.String"/> that represents this
			/// <see cref="VerseTextSubstring"/>.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public override string ToString()
			{
				return Text;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Implicit conversion of a <see cref="VerseTextSubstring"/> to a string
			/// </summary>
			/// <param name="verseTextSubstring">The <see cref="VerseTextSubstring"/> to be cast</param>
			/// <returns>A string containing the verse text of this portion of the verse</returns>
			/// ------------------------------------------------------------------------------------
			public static implicit operator string(VerseTextSubstring verseTextSubstring)
			{
				return verseTextSubstring.Text;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a verse and return the text of the verse in one or more
		/// <see cref="VerseTextSubstring"/> objects.
		/// </summary>
		/// <param name="scr">The scripture.</param>
		/// <param name="targetRef">The verse reference to look for</param>
		/// <param name="ichStart">The starting character where the (first part of the) verse
		/// text is located within the (first) containing paragraph</param>
		/// <returns>
		/// A list of <see cref="VerseTextSubstring"/> objects, each representing
		/// one paragraph worth of verse text (e.g., to deal with poetry)
		/// </returns>
		/// <remarks>Verses would not normally be split across sections, but there are a few
		/// places, such as the end of I Cor. 12, where it can happen.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public static List<VerseTextSubstring> GetVerseText(IScripture scr, ScrReference targetRef,
			out int ichStart)
		{
			ichStart = -1;

			if (scr.Versification != targetRef.Versification)
				targetRef = new ScrReference(targetRef, scr.Versification);

			List<VerseTextSubstring> verseText = new List<VerseTextSubstring>();

			// Find the book that the reference is in
			IScrBook book = ScrBook.FindBookByID(scr, targetRef.Book);
			if (book == null)
				return verseText;

			if (targetRef.IsBookTitle)
			{
				foreach (StTxtPara para in book.TitleOA.ParagraphsOS)
				{
					verseText.Add(new VerseTextSubstring(para.Contents.UnderlyingTsString, -1,
						para.IndexInOwner, (int)ScrBook.ScrBookTags.kflidTitle));
					ichStart = 0;
				}
				return verseText;
			}

			int iSection = 0;
			// Look through the sections for the target reference
			foreach (ScrSection section in book.SectionsOS)
			{
				if (!section.ContainsReference(targetRef))
				{
					if (verseText.Count > 0)
						return verseText;
				}
				else
				{
					int iPara = 0;
					// Look through each paragraph in the section
					foreach (StTxtPara para in section.ContentOA.ParagraphsOS)
					{
						// Search for target reference in the verses in the paragraph
						ScrTxtPara scrPara = new ScrTxtPara(scr.Cache, para.Hvo);
						ScrVerseSet verseSet = new ScrVerseSet(scrPara);
						foreach (ScrVerse verse in verseSet)
						{
							if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
							{
								// If the paragraph has a chapter number, the verse iterator
								// returns this as a separate string with the same reference
								// as the following verse.
								// We want to return the verse string, not the chapter number
								// run, so we skip a string that has only numeric characters.
								ITsString verseTextInPara = verse.Text;
								if (verse.Text.RunCount > 0)
								{
									string styleName = verse.Text.get_PropertiesAt(0).GetStrPropValue(
										(int)FwTextPropType.ktptNamedStyle);
									if (styleName == ScrStyleNames.VerseNumber)
										verseTextInPara = StringUtils.Substring(verseTextInPara, verse.Text.get_LimOfRun(0));
								}
								if (!IsNumber(verseTextInPara.Text)) // skip chapter number strings
								{
									if (verseText.Count == 0)
										ichStart = verse.TextStartIndex;
									verseText.Add(new VerseTextSubstring(verseTextInPara, iSection,
										iPara, (int)ScrSection.ScrSectionTags.kflidContent));
									break;
								}
							}
							else if (verseText.Count > 0)
								return verseText;
						}
						iPara++;
					}
				}
				iSection++;
			}
			return verseText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look for the given picture's ORC in the given verse.
		/// </summary>
		/// <param name="targetRef">The verse reference to look for</param>
		/// <param name="hvoPict">The hvo of the picture to look for.</param>
		/// <param name="iSection">The index of the section where the ORC was found.</param>
		/// <param name="iPara">The index of the para where the ORC was found.</param>
		/// <param name="ichOrcPos">The character position of the ORC in the paragraph.</param>
		/// <returns>
		/// 	<c>true</c> if the given picture is found in the given verse.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool FindPictureInVerse(ScrReference targetRef, int hvoPict, out int iSection,
			out int iPara, out int ichOrcPos)
		{
			CheckDisposed();

			iSection = iPara = ichOrcPos = -1;

			// Find the book that the reference is in
			IScrBook book = ScrBook.FindBookByID(m_scr, targetRef.Book);
			if (book == null)
				return false;

			iSection = 0;
			// Look through the sections for the target reference
			foreach (ScrSection section in book.SectionsOS)
			{
				if (section.ContainsReference(targetRef))
				{
					iPara = 0;
					// Look through each paragraph in the section
					foreach (StTxtPara para in section.ContentOA.ParagraphsOS)
					{
						// Search for target reference in the verses in the paragraph
						ScrTxtPara scrPara = new ScrTxtPara(m_cache, para.Hvo);
						ScrVerseSet verseSet = new ScrVerseSet(scrPara);
						foreach (ScrVerse verse in verseSet)
						{
							if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
							{
								// If the paragraph has a chapter number, the verse iterator
								// returns this as a separate string with the same reference
								// as the following verse.
								// We want to return the verse string, not the chapter number
								// run, so we skip a string that has only numeric characters.
								ITsString tssVerse = verse.Text;
								for (int iRun = 0; iRun < tssVerse.RunCount; iRun++)
								{
									string sRun = tssVerse.get_RunText(iRun);
									if (sRun.Length == 1 && sRun[0] == StringUtils.kchObject)
									{
										string str = tssVerse.get_Properties(iRun).GetStrPropValue(
											(int)FwTextPropType.ktptObjData);

										if (!String.IsNullOrEmpty(str) && str[0] == (char)(int)FwObjDataTypes.kodtGuidMoveableObjDisp)
										{
											Guid guid = MiscUtils.GetGuidFromObjData(str.Substring(1));
											if (m_cache.GetIdFromGuid(guid) == hvoPict)
											{
												ichOrcPos = tssVerse.get_MinOfRun(iRun) + verse.VerseStartIndex;
												return true;
											}
										}
									}
								}
							}
						}
						iPara++;
					}
				}
				iSection++;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects text in the given verse.
		/// </summary>
		/// <param name="scrRef">The Scripture reference of the verse.</param>
		/// <param name="text">The specific text within the verse to look for (<c>null</c> to
		/// select the text of the entire verse.</param>
		/// <remarks>
		/// REVIEW (TE-4218): Do we need to add a parameter to make it possible to do a case-
		/// insensitive match?
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SelectVerseText(ScrReference scrRef, ITsString text)
		{
			SelectVerseText(scrRef, text, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects text in the given verse.
		/// </summary>
		/// <param name="scrRef">The Scripture reference of the verse.</param>
		/// <param name="text">The specific text within the verse to look for (<c>null</c> to
		/// select the text of the entire verse.</param>
		/// <param name="fSynchScroll">if set to <c>true</c> then use the flavor of GotoVerse
		/// that will send a synch. message. Otherwise, the other flavor is used.</param>
		/// <remarks>
		/// REVIEW (TE-4218): Do we need to add a parameter to make it possible to do a case-
		/// insensitive match?
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SelectVerseText(ScrReference scrRef, ITsString text, bool fSynchScroll)
		{
			if (scrRef.Versification != m_scr.Versification)
				scrRef = new ScrReference(scrRef, m_scr.Versification);

			if (!(fSynchScroll ? GotoVerse(scrRef) : GotoVerse_WithoutSynchMsg(scrRef)))
				return;

			bool fOrigIgnoreAnySyncMessages = false;
			if (m_syncHandler != null)
			{
				fOrigIgnoreAnySyncMessages = m_syncHandler.IgnoreAnySyncMessages;
				m_syncHandler.IgnoreAnySyncMessages = !fSynchScroll;
			}

			try
			{
				// We successfully navigated to the verse (or somewhere close) so attempt to make a
				// selection there.
				int ichStart, ichEnd;
				if (text == null || text.Text == null)
				{
					int verseStart;
					List<VerseTextSubstring> verseTexts = GetVerseText(m_scr, scrRef, out verseStart);

					if (verseTexts.Count == 0)
						return; // Must not have found the exact verse

					SelectionHelper helper = CurrentSelection;
					helper.IchEnd = (verseTexts.Count == 1) ? verseStart + verseTexts[0].Length :
						verseTexts[verseTexts.Count - 1].Length;
					if (verseTexts[verseTexts.Count - 1].SectionIndex != verseTexts[0].SectionIndex)
					{
						SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
						int iParaLev = LocationTrackerImpl.GetLevelIndexForTag((int)StText.StTextTags.kflidParagraphs,
							StVc.ContentTypes.kctNormal);
						int iSectLev = LocationTrackerImpl.GetLevelIndexForTag((int)ScrBook.ScrBookTags.kflidSections,
							StVc.ContentTypes.kctNormal);
						levInfo[iParaLev].ihvo = verseTexts[verseTexts.Count - 1].ParagraphIndex;
						levInfo[iSectLev].ihvo += (verseTexts[verseTexts.Count - 1].SectionIndex - verseTexts[0].SectionIndex);
						helper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
					}
					else if (verseTexts.Count > 1)
					{
						SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
						int iParaLev = LocationTrackerImpl.GetLevelIndexForTag((int)StText.StTextTags.kflidParagraphs,
							StVc.ContentTypes.kctNormal);
						levInfo[iParaLev].ihvo = verseTexts[verseTexts.Count - 1].ParagraphIndex;
						helper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
					}
					helper.SetSelection(true);
				}
				else
				{
					int iSection, iPara;
					if (FindTextInVerse(m_scr, text, scrRef, false, out iSection, out iPara, out ichStart, out ichEnd))
					{
						// We found the text in the verse.
						if (!InBookTitle)
							SelectRangeOfChars(BookIndex, iSection, iPara, ichStart, ichEnd);
						else
						{
							SelectRangeOfChars(BookIndex, iSection,
								(int)ScrBook.ScrBookTags.kflidTitle, iPara,
								ichStart, ichEnd, true, true, false);
						}
					}
				}
			}
			finally
			{
				if (m_syncHandler != null)
					m_syncHandler.IgnoreAnySyncMessages = fOrigIgnoreAnySyncMessages;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given string contains only digits.
		/// </summary>
		/// <param name="str">given string</param>
		/// <returns>true, if string only contains digits; false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsNumber(string str)
		{
			if (String.IsNullOrEmpty(str))
				return false;
			foreach (char character in str)
			{
				// Non-numeric character found?
				if (!char.IsDigit(character))
					return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the verse reference in the back translation paragraph or the closest
		/// location to where it would occur
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void GotoVerseBT(ScrReference targetRef, int bookIndex,
			ScrSection section, int paraIndex, ScrTxtPara para)
		{
			ScrVerseSet verseSet = new ScrVerseSetBT(para, ViewConstructorWS);

			// Look for an exact match of the target verse number in the BT para
			foreach (ScrVerse verse in verseSet)
			{
				if (!verse.VerseNumberRun)
					continue;

				if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
				{
					// set the IP here now
					SetInsertionPoint(bookIndex, section.IndexInBook, paraIndex,
						verse.TextStartIndex, false);
					return;
				}
			}

			// An exact match was not found, so look for a best guess spot for it
			foreach (ScrVerse verse in verseSet)
			{
				if (!verse.VerseNumberRun)
					continue;

				if (verse.StartRef >= targetRef)
				{
					// set the IP here now
					SetInsertionPoint(bookIndex, section.IndexInBook, paraIndex, verse.VerseStartIndex, false);
					return;
				}
			}

			// A best guess spot was not found so put the selection at the end of the paragraph
			SetIpAtEndOfPara(bookIndex, section.IndexInBook, paraIndex, para);
		}

		private const int kflidComment = (int) CmAnnotation.CmAnnotationTags.kflidComment;
		/// <summary>
		/// Go to the indicated verse in the BT. We've already determined the index of the book, the section,
		/// the paragraph, and the character index in the vernacular. We want to figure the corresponding
		/// position in the BT, which should be the non-label segment closest to (hopefully containing) the
		/// vernacular position, and select it.
		/// </summary>
		/// <param name="targetRef"></param>
		/// <param name="bookIndex"></param>
		/// <param name="section"></param>
		/// <param name="paraIndex"></param>
		/// <param name="para"></param>
		/// <param name="ichMainPosition">The position where this verse occurs in the main paragraph.</param>
		protected virtual void GotoVerseBtSeg(ScrReference targetRef, int bookIndex,
			ScrSection section, int paraIndex, ScrTxtPara para, int ichMainPosition)
		{
			int isegTarget = GetBtSegIndexForVernChar(para, ichMainPosition, ViewConstructorWS);
			// Select the appropriate segment (or if nothing matched, the last place we can edit).
			if (isegTarget < 0)
				return; // pathological.
			SelectRangeOfChars(bookIndex, section.IndexInBook, (int)ScrSection.ScrSectionTags.kflidContent, paraIndex, isegTarget,
							   0, 0, true, true, false, VwScrollSelOpts.kssoDefault);
		}

		/// <summary>
		/// Given a character position in the contents of an ScrTxtPara, return the index of the corresponding segment
		/// (or the closest editable one).
		/// </summary>
		static public int GetBtSegIndexForVernChar(ScrTxtPara para, int ichMainPosition, int btWs)
		{
			StTxtPara.LoadSegmentFreeTranslations(new int[] { para.Hvo }, para.Cache, btWs);
			int ktagParaSegments = StTxtPara.SegmentsFlid(para.Cache);
			int cseg = para.Cache.GetVectorSize(para.Hvo, ktagParaSegments);
			int kflidFT = StTxtPara.SegmentFreeTranslationFlid(para.Cache);
			ISilDataAccess sda = para.Cache.MainCacheAccessor;
			int isegTarget = -1;
			for (int iseg = 0; iseg < cseg; iseg++)
			{
				int hvoSeg = sda.get_VecItem(para.Hvo, ktagParaSegments, iseg);
				CmBaseAnnotation seg = CmObject.CreateFromDBObject(para.Cache, hvoSeg) as CmBaseAnnotation;
				// If it's a 'label' segment, it's not where we want to put the IP.
				ITsString tssSeg = seg.TextAnnotated;
				if (SegmentBreaker.HasLabelText(tssSeg, 0, tssSeg.Length))
					continue;
				isegTarget = iseg;
				if (ichMainPosition <= seg.EndOffset)
					break; // don't consider any later segment
			}
			return isegTarget;
		}

		// This is the start of an alternative implementation of GotoVerseBtSeg, actually searching the text.
		//BCVRef m_startRef;
			//BCVRef m_endRef;
			//para.GetBCVRefAtPosition(0, out m_startRef, out m_endRef);
			//int ktagParaSegments = StTxtPara.SegmentsFlid(Cache);
			//int cseg = Cache.GetVectorSize(para.Hvo, ktagParaSegments);
			//int kflidFT = StTxtPara.SegmentFreeTranslationFlid(Cache);
			//ISilDataAccess sda = Cache.MainCacheAccessor;
			//int btWs = ViewConstructorWS;
			//for (int iseg = 0; iseg < cseg; iseg++)
			//{
			//    int hvoSeg = sda.get_VecItem(para.Hvo, ktagParaSegments, iseg);
			//    if (!sda.get_IsPropInCache(hvoSeg, kflidFT, (int)CellarModuleDefns.kcptReferenceAtom, 0))
			//    {
			//        StTxtPara.LoadSegmentFreeTranslations(new int[] {para.Hvo}, Cache, btWs);
			//    }
			//    int hvoFt = sda.get_ObjectProp(hvoSeg, kflidFT);
			//    ITsString tssTrans = sda.get_MultiStringAlt(hvoFt, kflidComment, btWs);
			//    int crun = tssTrans.RunCount;
			//    for (int irun = 0; irun < crun; irun++)
			//    {
			//        ITsTextProps ttpRun = tssTrans.get_Properties(irun);
			//        if (StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber))
			//        {
			//            string sVerseNum = tssTrans.get_RunText(irun);
			//            int nVerseStart, nVerseEnd;
			//            ScrReference.VerseToInt(sVerseNum, out nVerseStart, out nVerseEnd);
			//            m_startRef.Verse = nVerseStart;
			//            m_endRef.Verse = nVerseEnd;
			//        }
			//        else if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
			//        {
			//            string sChapterNum = tssTrans.get_RunText(irun);
			//            int nChapter = ScrReference.ChapterToInt(sChapterNum);
			//            m_startRef.Chapter = m_endRef.Chapter = nChapter;
			//            // Set the verse number to 1, since the first verse number after a
			//            // chapter is optional. If we happen to get a verse number in the
			//            // next run, this '1' will be overridden (though it will probably
			//            // still be a 1).
			//            m_startRef.Verse = m_endRef.Verse = 1;
			//        }
			//    }

					/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the IP and scrolls it to near the top of the view. Used for going to a
		/// location in the BT or vernacular where a verse starts (or as close as possible).
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="bookIndex">index of book to look in</param>
		/// <param name="section">section to search</param>
		/// <param name="paraIndex">paragraph to look in</param>
		/// <param name="ichPosition">starting character index to look at</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void GoToPosition(ScrReference targetRef, int bookIndex,
			ScrSection section, int paraIndex, int ichPosition)
		{
			if (ContentType == StVc.ContentTypes.kctSimpleBT)
			{
				GotoVerseBT(targetRef, bookIndex, section, paraIndex,
					 new ScrTxtPara(m_cache, section[paraIndex].Hvo));
			}
			else if (ContentType == StVc.ContentTypes.kctSegmentBT)
			{
				GotoVerseBtSeg(targetRef, bookIndex, section, paraIndex,
					 new ScrTxtPara(m_cache, section[paraIndex].Hvo), ichPosition);
			}
			else
			{
				SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
					bookIndex, section.IndexInBook, paraIndex, ichPosition, false,
					VwScrollSelOpts.kssoNearTop);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the IP to the end of the section. Check for vernacular or BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetIpAtEndOfSection(int bookIndex, ScrSection section)
		{
			SetIpAtEndOfPara(bookIndex, section.IndexInBook,
				section.ContentParagraphCount - 1, section.LastContentParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the IP to the end of the section. Check for vernacular or BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetIpAtEndOfPara(int bookIndex, int sectionIndex, int paraIndex, StTxtPara para)
		{
			int paraLength;
			if (IsBackTranslation)
			{
				ICmTranslation trans = para.GetOrCreateBT();
				paraLength = trans.Translation.GetAlternative(ViewConstructorWS).UnderlyingTsString.Length;
			}
			else
				paraLength = para.Contents.Length;

			SetInsertionPoint(bookIndex, sectionIndex, paraIndex, paraLength, true);
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Goes to the closest match in the given section or following section starting at the
		///// given paragraph and character offsets and ending at the given paragraph and character
		///// offsets
		///// </summary>
		///// <param name="targetRef">ScrReference to find</param>
		///// <param name="bookIndex">index of book to look in</param>
		///// <param name="section">section to search</param>
		///// <param name="startingParaIndex">starting paragraph to look in</param>
		///// <param name="startingCharIndex">starting character index to look at</param>
		///// <param name="endingParaIndex">last paragraph to look in (-1 for end)</param>
		///// <param name="endingCharIndex">ending character index to look at (-1 for end)</param>
		///// <returns><c>false</c> if we can't go to the closest match </returns>
		///// ------------------------------------------------------------------------------------
		//protected virtual bool GotoClosestMatch(ScrReference targetRef,
		//    int bookIndex, ScrSection section, int startingParaIndex, int startingCharIndex,
		//    int endingParaIndex, int endingCharIndex)
		//{
		//    int paraCount = section.ContentParagraphCount;

		//    if (startingParaIndex >= paraCount)
		//        return false;

		//    if (endingParaIndex == -1)
		//        endingParaIndex = paraCount - 1;

		//    if (endingCharIndex == -1)
		//        endingCharIndex = section.LastContentParagraph.Contents.Length;

		//    // only process this section if we have content to check
		//    if (startingParaIndex == endingParaIndex && startingCharIndex == endingCharIndex)
		//        return false;

		//    // Indicator to look for the min of a section.
		//    bool findMin = false;

		//    // If the section does not contain this reference, then look to see if we want to
		//    // put the selection between this section and the next one.
		//    if (!section.ContainsReference(targetRef))
		//    {
		//        // If there is no previous section and the reference is less than the
		//        // min of this section, then place the IP at the start of this section
		//        if (section.VerseRefMin > targetRef && section.PreviousSection == null)
		//        {
		//            SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
		//                bookIndex, section.IndexInBook, 0, 0, true, VwScrollSelOpts.kssoNearTop);
		//            return true;
		//        }
		//        ScrSection nextSection = section.NextSection;
		//        if (nextSection == null)
		//        {
		//            // If there is no following section and the reference is larger then the
		//            // max of this section, then place it at the end of this section
		//            if (section.VerseRefMax <= targetRef)
		//            {
		//                SetIpAtEndOfSection(bookIndex, section);
		//                return true;
		//            }
		//            else
		//                return false;
		//        }
		//        // If the reference falls between the max of this section and the max of the next
		//        // section, then the IP will either be at the end of this section or in the next section.
		//        if (section.VerseRefMax <= targetRef && targetRef <= nextSection.VerseRefMax)
		//        {
		//            // If the reference falls between the two sections, then place it at the edge of the section
		//            // that has a reference closest to the target reference.
		//            if (targetRef <= nextSection.VerseRefMin || section.VerseRefEnd > nextSection.VerseRefStart)
		//            {
		//                if (targetRef.ClosestTo(section.VerseRefMax, nextSection.VerseRefMin) == 0)
		//                {
		//                    // set selection to the end of this section
		//                    SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
		//                        bookIndex, section.IndexInBook, paraCount - 1,
		//                        section.LastContentParagraph.Contents.Length, true,
		//                        VwScrollSelOpts.kssoNearTop);
		//                    return true;
		//                }
		//                else
		//                {
		//                    // Place the selection at the min reference of the next section, so
		//                    // set the target reference to the min reference so it will be
		//                    // found below.
		//                    targetRef.BBCCCVVV = nextSection.VerseRefMin;
		//                    findMin = true;
		//                }
		//            }
		//            section = nextSection;
		//            // reset index limits for changed section
		//            paraCount = section.ContentParagraphCount;
		//            startingParaIndex = 0;
		//            endingParaIndex = paraCount - 1;
		//            endingCharIndex = startingCharIndex = -1; // ADDED 8-7-2008 TLB
		//        }
		//        else
		//            return false;
		//    }

		//    ScrVerse prevVerse = null; // REVIEW: This might need to go outside this for loop
		//    // The reference goes somewhere in this section, so look for the spot to put it
		//    for (int paraIndex = startingParaIndex; paraIndex <= endingParaIndex; ++paraIndex)
		//    {
		//        ScrTxtPara para = new ScrTxtPara(m_cache, section[paraIndex].Hvo);
		//        ScrVerseSet verseSet = new ScrVerseSet(para);
		//        int currentEndingCharIndex = (paraIndex == endingParaIndex && endingCharIndex != -1)? endingCharIndex: para.Contents.Length - 1;
		//        foreach (ScrVerse verse in verseSet)
		//        {
		//            if (verse.VerseStartIndex >= currentEndingCharIndex) // past the end
		//                break;
		//            // If we haven't gotten to the beginning position where we want to start
		//            // looking or we're looking for the minimum reference in the section, and
		//            // this is not it, then continue looking.
		//            if (verse.VerseStartIndex >= startingCharIndex ||
		//                (findMin && verse.StartRef == section.VerseRefMin))
		//            {
		//                // When the target reference is found, set the IP
		//                if (verse.StartRef >= targetRef)
		//                {
		//                    // REVIEW: what to do when prevVerse is null
		//                    int ich = verse.VerseStartIndex;
		//                    if (verse.StartRef > targetRef && prevVerse != null)
		//                    {
		//                        ich = prevVerse.TextStartIndex;
		//                        if (prevVerse.HvoPara != para.Hvo)
		//                            paraIndex--;

		//                    }
		//                    // set the IP here now
		//                    GoToPosition(targetRef, bookIndex, section, paraIndex, ich);
		//                    return true;
		//                }
		//            }
		//            prevVerse = verse;
		//        }

		//        // after the first paragraph, start looking at 0
		//        startingCharIndex = 0;
		//    }
		//    return false;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the closest match in the given book.
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="book">index of book to look in</param>
		/// <returns><c>false</c> if we can't go to the closest match </returns>
		/// ------------------------------------------------------------------------------------
		protected virtual void GotoClosestPrecedingRef(ScrReference targetRef, ScrBook book)
		{
			Debug.Assert(book != null);
			Debug.Assert(book.SectionsOS.Count > 0);
			ScrSection section = null;

			// Move backward through the sections in the book to find the one
			// whose start reference is less than the one we're looking for.
			for (int iSection = book.SectionsOS.Count - 1; iSection >= 0; iSection--)
			{
				section = book[iSection];

				// If the reference we're looking for is greater than the current
				// section's start reference, then get out of the loop because we've
				// found the section in which we need to place the IP.
				if (targetRef >= section.VerseRefStart)
					break;
			}

			// At this point, we know we have the section in which we think the
			// IP should be located.
			int iBook = BookFilter.GetBookIndex(book.Hvo);

			// If the reference we're looking for is before the section's start reference,
			// then we need to put the IP at the beginning of the book's first section,
			// but after a chapter number if the sections begins with one.
			if (targetRef < section.VerseRefStart)
			{
				GoToFirstChapterInSection(iBook, section);
				return;
			}

			int paraCount = section.ContentParagraphCount;

			// If there are no paragraphs in the section, then we're out of luck.
			Debug.Assert(paraCount > 0);

			// Go through the paragraphs and find the one in which we
			// think the IP should be located.
			for (int iPara = paraCount - 1; iPara >= 0; iPara--)
			{
				ScrTxtPara para = new ScrTxtPara(m_cache, section[iPara].Hvo);
				ScrVerseList verses = new ScrVerseList(para);

				// Go backward through the verses in the paragrah, looking for the
				// first one that is less than the reference we're looking for.
				for (int iVerse = verses.Count - 1; iVerse >= 0; iVerse--)
				{
					// If the current reference is before (i.e. less) the one we're looking
					// for,	then put the IP right after the it's verse number.
					if (verses[iVerse].StartRef <= targetRef)
					{
						GoToPosition(targetRef, iBook, section,
							iPara, verses[iVerse].TextStartIndex);

						return;
					}
				}
			}

			// At this point, we have failed to find a good location for the IP.
			// Therefore, just place it at the beginning of the section.
			GoToSectionStart(iBook, section.IndexInBook);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of the text of the first (implicit or explicit) chapter
		/// in the first paragraph in the specified scripture section. If a chapter isn't
		/// found then the IP is put at the beginning of the content paragraph.
		/// </summary>
		/// <param name="iBook">0-based index of the book (in this view)</param>
		/// <param name="section">The section (in the given book, as displayed in this view)
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void GoToFirstChapterInSection(int iBook, ScrSection section)
		{
			int ichChapterStart = 0;
			ScrVerseSet verseSet = new ScrVerseSet(new ScrTxtPara(m_cache, section[0].Hvo));
			if (verseSet.MoveNext())
			{
				ScrVerse verse = (ScrVerse)verseSet.Current;
				if (!verse.VerseNumberRun)
					ichChapterStart = verse.TextStartIndex;
			}

			ScrReference scrRef = new ScrReference(section.VerseRefStart, m_scr.Versification);
			GoToPosition(scrRef, iBook, section, 0, ichChapterStart);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine chapter before current reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference GetPrevChapter()
		{
			CheckDisposed();

			ScrReference curStartRef = CurrentStartRef;
			if (curStartRef.Book == 0)
				return ScrReference.Empty;

			if (curStartRef.Chapter == 0)
			{
				// Hmmm... we're probably at the beginning of the book already, and in the
				// title or intro area.  We should move into the previous book.
				if (BookIndex > 0)
				{
					ScrBook book = BookFilter.GetBook(BookIndex - 1);
					int chapter = 0;
					foreach (ScrSection section in book.SectionsOS)
					{
						ScrReference minRef = new ScrReference(section.VerseRefMin, m_scr.Versification);
						ScrReference maxRef = new ScrReference(section.VerseRefMax, m_scr.Versification);

						if (minRef.Valid && maxRef.Valid)
						{
							for (int i = BCVRef.GetChapterFromBcv(section.VerseRefMin); i <= BCVRef.GetChapterFromBcv(section.VerseRefMax); i++)
							{
								if (i > chapter)
									chapter = i;
							}
						}
					}

					return new ScrReference((short)book.CanonicalNum, chapter, 1, m_scr.Versification);
				}

				if (BookIndex == 0)
				{
					// Guess what, we're at the beginning, just move to the first
					// chapter/verse in the book.
					ScrBook book = BookFilter.GetBook(0);
					return new ScrReference((short)book.CanonicalNum, 1, 1, m_scr.Versification);
				}
			}
			else
			{
				// Standard yum-cha situation here... move up to the previous chapter
				// and be happy.
				if (BookIndex >= 0 && SectionIndex >= 0)
				{
					ScrBook book = BookFilter.GetBook(BookIndex);
					if (curStartRef.Chapter > 1)
					{
						return new ScrReference((short)book.CanonicalNum,
							curStartRef.Chapter - 1, 1, m_scr.Versification);
					}

					if (BookIndex > 0)
					{
						// We are at the first chapter of the book so move to the last chapter in
						// the previous book.
						book = BookFilter.GetBook(BookIndex - 1);
						int chapter = 0;
						if (book.SectionsOS.Count > 0)
							chapter = BCVRef.GetChapterFromBcv(book.LastSection.VerseRefMax);

						return new ScrReference((short)book.CanonicalNum, chapter, 1,
							m_scr.Versification);
					}

					return new ScrReference((short)book.CanonicalNum, 1, 1, m_scr.Versification);
				}
			}

			// Splode?
			return ScrReference.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines next chapter following current reference
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference GetNextChapter()
		{
			CheckDisposed();

			ScrReference curEndRef = CurrentEndRef;
			if (curEndRef.Book == 0)
			{
				// REVIEW BobA(TimS): Something went wrong, let's move to the first verse
				// we know of... Alternative is to say oh well, and do nothing at all.
				return ScrReference.StartOfBible(curEndRef.Versification);
			}

			if (curEndRef.Chapter == 0)
			{
				// Hmmm... we're probably at the beginning of the book already, and in the
				// title or intro area. We should move into the previous book.
				if (BookIndex >= 0)
				{
					// We are in the intro section area, we should move to the
					// first verse of the first chapter
					ScrBook book = BookFilter.GetBook(BookIndex);
					return new ScrReference((short)book.CanonicalNum, 1, 1, m_scr.Versification);
				}
			}
			else
			{
				// Standard yum-cha situation here... move up to the next chapter
				// and be happy.
				if (BookIndex >= 0 && SectionIndex >= 0)
				{
					ScrBook book = BookFilter.GetBook(BookIndex);
					int lastChapter = 0;
					foreach (ScrSection section in book.SectionsOS)
					{
						ScrReference maxSectionRef =
							new ScrReference(section.VerseRefMax, m_scr.Versification);
						ScrReference minSectionRef =
							new ScrReference(section.VerseRefMin, m_scr.Versification);

						if (minSectionRef.Valid && maxSectionRef.Valid &&
							maxSectionRef.Chapter > lastChapter)
						{
							lastChapter = maxSectionRef.Chapter;
						}
					}

					if (BookIndex == BookFilter.BookCount - 1 &&
						curEndRef.Chapter == lastChapter)
					{
						// We're at the end of the book, nowhere to go...
						return new ScrReference((short)book.CanonicalNum, lastChapter, 1,
							m_scr.Versification);
					}

					if (curEndRef.Chapter == lastChapter)
					{
						// We're at the end of the book, nowhere to go...
						book = BookFilter.GetBook(BookIndex + 1);
						return new ScrReference((short)book.CanonicalNum, 1, 1,
							m_scr.Versification);
					}

					// Otherwise, move on.... we were bored of this part anyhow...
					return new ScrReference((short)book.CanonicalNum, curEndRef.Chapter + 1,
						1, m_scr.Versification);
				}
			}

			// Splode?
			return ScrReference.StartOfBible(curEndRef.Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates reference for the last chapter of the currently referenced book.
		/// </summary>
		/// <returns>a Scripture reference for the last chapter of the book</returns>
		/// ------------------------------------------------------------------------------------
		public ScrReference GetLastChapter()
		{
			CheckDisposed();

			ScrBook book = BookFilter.GetBook(BookIndex);
			int chapter = 0;
			if (book.SectionsOS.Count > 0)
				chapter = BCVRef.GetChapterFromBcv(book.LastSection.VerseRefMax);

			return new ScrReference((short)book.CanonicalNum, chapter, 1, m_scr.Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the text referenced in the ScrScriptureNote. If it does not find the text,
		/// it makes the closest selection to the referenced text that it can.
		/// </summary>
		/// <param name="note">the note containing the Scripture reference to find</param>
		/// ------------------------------------------------------------------------------------
		public void GoToScrScriptureNoteRef(IScrScriptureNote note)
		{
			GoToScrScriptureNoteRef(note, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the text referenced in the ScrScriptureNote. If it does not find the text,
		/// it makes the closest selection to the referenced text that it can.
		/// </summary>
		/// <param name="note">the note containing the Scripture reference to find</param>
		/// <param name="sendSyncMsg"><c>true</c> to not send a focus sychronization message
		/// when the selection is changed by going to the scripture ref.</param>
		/// ------------------------------------------------------------------------------------
		public void GoToScrScriptureNoteRef(IScrScriptureNote note, bool sendSyncMsg)
		{
			// TODO (TE-1729): Use this method correctly from Annotations view.

			ScrReference scrRef = new ScrReference(note.BeginRef, m_scr.Versification);
			ScrBook book = BookFilter.GetBookByOrd(scrRef.Book);
			if (book == null)
				return;

			int iBook = BookFilter.GetBookIndex(book.Hvo);
			bool fOrigIgnoreAnySyncMessages = false;
			if (m_syncHandler != null)
			{
				fOrigIgnoreAnySyncMessages = m_syncHandler.IgnoreAnySyncMessages;
				m_syncHandler.IgnoreAnySyncMessages = true;
			}

			try
			{
				if (note.Flid == (int)CmPicture.CmPictureTags.kflidCaption)
				{
					SelectCitedTextInPictureCaption(iBook, note);
					return;
				}

				int ichStart, ichEnd;
				string citedText = note.CitedText;
				ITsString citedTextTss = note.CitedTextTss;
				StTxtPara para = note.BeginObjectRA as StTxtPara;
				if (para != null && m_cache.GetOwnerOfObjectOfClass(para.Hvo, ScrDraft.kClassId) == 0)
				{
					if (para.Owner is StFootnote)
					{
						// Make selection in footnote.
						if (TextAtExpectedLoc(para.Contents.Text, citedText, note.BeginOffset, note.EndOffset))
						{
							// Select text in footnote.
							StFootnote footnote = para.Owner as StFootnote;
							if (footnote == null)
								return;

							SelectionHelper selHelper = new SelectionHelper();
							selHelper.AssocPrev = false;
							selHelper.NumberOfLevels = 3;
							selHelper.LevelInfo[2].tag = BookFilter.Tag;
							selHelper.LevelInfo[2].ihvo = iBook;
							selHelper.LevelInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
							selHelper.LevelInfo[1].ihvo = footnote.IndexInOwner;
							selHelper.LevelInfo[0].ihvo = 0;

							// Prepare to move the IP to the specified character in the paragraph.
							selHelper.IchAnchor = note.BeginOffset;
							selHelper.IchEnd = note.EndOffset;

							// Now that all the preparation to set the IP is done, set it.
							selHelper.SetSelection(Callbacks.EditedRootBox.Site, true, true);
						}
						return;
					}

					// Make selection in Scripture text
					if (TextAtExpectedLoc(para.Contents.Text, citedText, note.BeginOffset, note.EndOffset))
					{
						SelectRangeOfChars(iBook, para, note.BeginOffset, note.EndOffset);
						return;
					}

					if (scrRef.Verse == 0)
					{
						// Either a missing chapter number or something in intro material.
						// Not much chance of finding it by reference (even if the chapter number
						// has been added, we never find the 0th verse, and 99% of the time that
						// chapter number would have been added to the same paragraph where it
						// was missing in the first place), so just try to find the text in the
						// paragraph, if it still exists.
						if (string.IsNullOrEmpty(citedText))
						{
							SelectRangeOfChars(iBook, para, note.BeginOffset, note.BeginOffset);
							return;
						}

						// The text may be null if the paragraph only contains the prompt. TE-8315
						if (para.Contents.Text != null)
						{
							int i = para.Contents.Text.IndexOf(citedText);
							if (i >= 0)
							{
								SelectRangeOfChars(iBook, para, i, i + citedText.Length);
								return;
							}
						}
					}
				}


				// A selection could not be made at the specified location. Attempt to go to
				// the specified verse and then try to find the text.

				// REVIEW (TimS): Why do we call GotoVerse here when we select the characters
				// down below? We might consider doing this only if we fail down below.
				if (sendSyncMsg)
					GotoVerse(scrRef);
				else
					GotoVerse_WithoutSynchMsg(scrRef);

				int iSection, iPara;
				if (citedText != null && FindTextInVerse(m_scr, citedTextTss, scrRef, false, out iSection,
					out iPara, out ichStart, out ichEnd))
				{
					// We found the text in the verse at a different character offset.
					SelectRangeOfChars(iBook, iSection, iPara, ichStart, ichEnd);
				}
				else if (note.BeginOffset > 0 && para != null &&
					IsOffsetValidLoc(para.Contents.Text, note.BeginOffset))
				{
					// We couldn't find the cited text at the specified offset, nor anywhere
					// in the paragraph. Therefore, just set the IP at the begin offset.
					SelectRangeOfChars(iBook, para, note.BeginOffset, note.BeginOffset);
				}
			}
			finally
			{
				if (m_syncHandler != null)
					m_syncHandler.IgnoreAnySyncMessages = fOrigIgnoreAnySyncMessages;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the specified text is in expected location in the paragraph.
		/// </summary>
		/// <param name="searchText">The text to search for in the paragraph.</param>
		/// <param name="paraText">The text in the paragraph.</param>
		/// <param name="beginOffset">expected beginning offset of searchText</param>
		/// <param name="endOffset">expected ending offset of searchText</param>
		/// <returns><c>true</c> if searchText found at specified offset, <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool TextAtExpectedLoc(string paraText, string searchText, int beginOffset,
			int endOffset)
		{
			return (paraText != null && paraText.Length >= endOffset &&
				paraText.Length > beginOffset && beginOffset < endOffset &&
				paraText.Substring(beginOffset, endOffset - beginOffset) ==	(searchText ?? string.Empty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the specified offset is less than or equal to the length of the
		/// specified text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsOffsetValidLoc(string paraText, int offset)
		{
			return (!string.IsNullOrEmpty(paraText) && offset <= paraText.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the given note's cited text in a picture caption.
		/// </summary>
		/// <param name="iBook">The index of the book in the filter (this is passed as a
		/// convenience -- it could be recalculated).</param>
		/// <param name="note">The note.</param>
		/// ------------------------------------------------------------------------------------
		private void SelectCitedTextInPictureCaption(int iBook, IScrScriptureNote note)
		{
			try
			{
				CmPicture picture = new CmPicture(Cache, note.BeginObjectRAHvo);
				int iSection, iPara, ichOrcPos;
				ITsString citedText = note.CitedTextTss;
				if (!FindPictureInVerse(new ScrReference(note.BeginRef, m_scr.Versification),
					picture.Hvo, out iSection, out iPara, out ichOrcPos))
				{
					throw new Exception("Picture ORC not found in verse"); // See catch
				}
				int ichStart, ichEnd;
				ITsString sCaptionText = picture.Caption.VernacularDefaultWritingSystem.UnderlyingTsString;
				if (sCaptionText.Length >= note.EndOffset &&
					StringUtils.Substring(sCaptionText, note.BeginOffset,
					note.EndOffset - note.BeginOffset) == citedText)
				{
					ichStart = note.BeginOffset;
					ichEnd = note.EndOffset;
				}
				else
				{
					// Search for word form in caption
					if (!StringUtils.FindWordFormInString(citedText, sCaptionText,
						m_cache.LanguageWritingSystemFactoryAccessor, out ichStart, out ichEnd))
					{
						ichStart = ichEnd = 0;
					}
				}
				MakeSelectionInPictureCaption(iBook, iSection,
					(int)ScrSection.ScrSectionTags.kflidContent, iPara,
					ichOrcPos, ichStart, ichEnd);
			}
			catch
			{
				// Picture or caption no longer exists. Go to the start of the specified verse.
				GotoVerse(new ScrReference(note.BeginRef, m_scr.Versification));
				return;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to find the specified text in the given verse and return the character
		/// offsets (relative to the whole paragraph) to allow it to be selected.
		/// </summary>
		/// <param name="scr">The scripture.</param>
		/// <param name="tssTextToFind">The text to search for</param>
		/// <param name="scrRef">The reference of the verse in which to look</param>
		/// <param name="fMatchWholeWord">True to match to a whole word, false to just look
		/// for the specified text</param>
		/// <param name="iSection">Index of the section where the text was found.</param>
		/// <param name="iPara">Index of the para in the section contents.</param>
		/// <param name="ichStart">if found, the character offset from the start of the para to
		/// the start of the sought text</param>
		/// <param name="ichEnd">if found, the character offset from the start of the para to
		/// the end of the sought text</param>
		/// <returns>
		/// 	<c>true</c> if found; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool FindTextInVerse(IScripture scr, ITsString tssTextToFind, ScrReference scrRef,
			bool fMatchWholeWord, out int iSection, out int iPara, out int ichStart, out int ichEnd)
		{
			// Get verse text
			int verseStart;
			List<VerseTextSubstring> verseTextList = GetVerseText(scr, scrRef, out verseStart);
			foreach (VerseTextSubstring verseText in verseTextList)
			{
				// Search for word form in verse text
				if (StringUtils.FindTextInString(tssTextToFind, verseText.Tss,
						scr.Cache.LanguageWritingSystemFactoryAccessor, fMatchWholeWord, out ichStart, out ichEnd))
				{
					ichStart += verseStart;
					ichEnd += verseStart;
					iSection = verseText.SectionIndex;
					iPara = verseText.ParagraphIndex;
					return true;
				}
				verseStart = 0;
			}

			ichStart = ichEnd = iSection = iPara = -1;
			return false;
		}
		#endregion

		#region Goto Book methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToFirstBook()
		{
			CheckDisposed();

			BookIndex = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToPrevBook()
		{
			CheckDisposed();

			BookIndex--;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToNextBook()
		{
			CheckDisposed();

			BookIndex++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToLastBook()
		{
			CheckDisposed();

			BookIndex = BookFilter.BookCount - 1;
		}
		#endregion

		#region Goto Section methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture section in the current book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToFirstSection()
		{
			CheckDisposed();

			GoToSectionStart(BookIndex, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToPrevSection()
		{
			CheckDisposed();

			try
			{
				SelectionHelper.ReduceSelectionToIp(Callbacks.EditedRootBox.Site,
					SelectionHelper.SelLimitType.Top, false);

				if (SectionIndex > 0)
				{
					GoToSectionStart(BookIndex,	SectionIndex - 1);
				}
				else
				{
					// If the insertion point is in a book title or we're in the first section
					// of a book then go to the last section of the preceeding book.
					if (BookIndex > 0)
					{
						int iBook = BookIndex - 1;
						ScrBook book = BookFilter.GetBook(iBook);

						if (book.SectionsOS.Count > 0)
							GoToSectionStart(iBook, book.SectionsOS.Count - 1);

						else // This book doesn't have any sections, so just go to the title
							SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, iBook, 0);
					}
				}
			}
			catch
			{
				// Must have been in the very first section already or something.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToNextSection()
		{
			CheckDisposed();

			try
			{
				SelectionHelper.ReduceSelectionToIp(Callbacks.EditedRootBox.Site,
					SelectionHelper.SelLimitType.Bottom, false);

				int iBook = BookIndex;
				ScrBook book = BookFilter.GetBook(iBook);
				if (SectionIndex < book.SectionsOS.Count - 1)
					GoToSectionStart(iBook, SectionIndex + 1);
				else if (BookIndex < BookFilter.BookCount - 1)
				{
					// If the insertion point is in the last section of a book then go to the
					// first section of the following book.
					int iBookNew = BookIndex + 1;
					book = BookFilter.GetBook(iBookNew);

					if (book.SectionsOS.Count > 0)
						GoToSectionStart(iBookNew, 0);
					else
					{
						// The book doesn't have any sections, so just go to the title
						SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle,iBookNew, 0);
					}
				}
			}
			catch
			{
				// Must have been in the very last section already or something.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture section in the current book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GoToLastSection()
		{
			CheckDisposed();

			try
			{
				int iBook = BookIndex;
				ScrBook book = BookFilter.GetBook(iBook);
				GoToSectionStart(iBook, book.SectionsOS.Count - 1);
			}
			catch
			{
				// Something strange happened, but just sigh and get on with life.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of the specified Scripture section.
		/// </summary>
		/// <param name="iBook">0-based index of the book (in this view)</param>
		/// <param name="iSection">0-based index of the section (in the given book, as
		/// displayed in this view)</param>
		/// <remarks>In an unfiltered view using the default sort, these indexes will
		/// correspond to the order of the books and sections in the DB model</remarks>
		/// ------------------------------------------------------------------------------------
		protected void GoToSectionStart(int iBook, int iSection)
		{
			if (iBook >= 0 && iSection >= 0 && iBook < BookFilter.BookCount)
			{
				ScrBook book = BookFilter.GetBook(iBook);
				if (iSection < book.SectionsOS.Count)
				{
					ScrSection section = book[iSection];
					SetInsertionPoint(section.HeadingParagraphCount > 0 ?
						(int)ScrSection.ScrSectionTags.kflidHeading :
						(int)ScrSection.ScrSectionTags.kflidContent, iBook,	iSection);
				}
			}
		}

		#endregion

		#region Goto Footnote methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to next footnote.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ScrFootnote GoToNextFootnote()
		{
			CheckDisposed();

			if (CurrentSelection == null)
			{
				Debug.Fail("Selection required");
				return null;
			}
			// Get the information needed from the current selection
			int paraLev = CurrentSelection.GetLevelForTag(
				(int)StText.StTextTags.kflidParagraphs);
			SelLevInfo[] levels = CurrentSelection.LevelInfo;
			int iBook = ((ITeView)Control).LocationTracker.GetBookIndex(
				CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			int tag = levels[paraLev + 1].tag;
			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			int iPara = levels[paraLev].ihvo;
			int ich = CurrentSelection.IchAnchor;

			// Look for the next footnote
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(m_cache,
				BookFilter.GetBook(iBook),
				ref iSection, ref iPara, ref ich, ref tag);

			// If we didn't find a footnote in the current book then go through the rest of the
			// books until we find one or we run out of books.
			while (footnote == null && iBook < BookFilter.BookCount - 1)
			{
				tag = (int)ScrBook.ScrBookTags.kflidTitle;
				iBook++;
				iPara = iSection = ich = 0;
				footnote = ScrFootnote.FindNextFootnote(m_cache,
					BookFilter.GetBook(iBook), ref iSection, ref iPara, ref ich,
					ref tag);
			}

			// Set the IP
			if (footnote != null)
				SetInsertionPoint(tag, iBook, iSection, iPara, ich, false);
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to previous footnote.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ScrFootnote GoToPreviousFootnote()
		{
			CheckDisposed();

			if (CurrentSelection == null)
			{
				Debug.Fail("Selection required");
				return null;
			}
			// Get the information needed from the current selection
			int paraLev = CurrentSelection.GetLevelForTag(
				(int)StText.StTextTags.kflidParagraphs);
			SelLevInfo[] levels = CurrentSelection.LevelInfo;
			int iBook = ((ITeView)Control).LocationTracker.GetBookIndex(
				CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			int tag = levels[paraLev + 1].tag;
			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				CurrentSelection, SelectionHelper.SelLimitType.Anchor);
			int iPara = levels[paraLev].ihvo;
			int ich = CurrentSelection.IchAnchor;

			// Look for the previous footnote
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(m_cache,
				BookFilter.GetBook(iBook),
				ref iSection, ref iPara, ref ich, ref tag);

			// If we didn't find a footnote in the current book then go through the rest of the
			// books until we find one or we run out of books.
			while (footnote == null && iBook > 0)
			{
				tag = (int)ScrSection.ScrSectionTags.kflidContent;
				iBook--;
				ScrBook book = BookFilter.GetBook(iBook);
				iSection = book.SectionsOS.Count - 1;
				iPara = -1;
				ich = -1;

				footnote = ScrFootnote.FindPreviousFootnote(m_cache,
					book, ref iSection, ref iPara, ref ich, ref tag);
			}

			// Set the IP
			if (footnote != null)
				SetInsertionPoint(tag, iBook, iSection, iPara, ich, true);
			return footnote;
		}
		#endregion

		#region Misc public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style name that is the default style to use for the given context (this is
		/// the static version)
		/// </summary>
		/// <param name="context">the context</param>
		/// <param name="fCharStyle">set to <c>true</c> for character styles; otherwise
		/// <c>false</c>.</param>
		/// <returns>
		/// Name of the style that is the default for the context
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static string GetDefaultStyleForContext(ContextValues context, bool fCharStyle)
		{
			switch (context)
			{
				case ContextValues.Annotation:
					return ScrStyleNames.Remark;
				case ContextValues.Intro:
					return ScrStyleNames.IntroParagraph;
				case ContextValues.Note:
					return ScrStyleNames.NormalFootnoteParagraph;
				case ContextValues.Text:
					return ScrStyleNames.NormalParagraph;
				case ContextValues.Title:
					return ScrStyleNames.MainBookTitle;
				case ContextValues.General:
					if (fCharStyle)
					{
						// The current style is a character style. It is appropriate for a
						// character style to be the General context. The default style for
						// character styles is "Default Paragraph Characters" which is
						// represented by string.Empty (TE-5875)
						return string.Empty;
					}
					// we shouldn't get here, ever, but if the user has figured out a way to
					// create a style with no context (i.e. a context of general) we need to
					// return something to keep from crashing.
					Debug.Fail("Shouldn't try to get the default style for the General Context.");
					return ScrStyleNames.NormalParagraph;
				default:
					throw new ArgumentException("Unexpected context");
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the current book as a string.
		/// </summary>
		/// <param name="selLimitType">Specify Top or Bottom</param>
		/// <returns>The name of the current book, or <c>string.Empty</c> if we don't have a
		/// selection or the selection is in a place we don't know how to handle.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual string CurrentBook(SelectionHelper.SelLimitType selLimitType)
		{
			CheckDisposed();

			// if there is no selection then there can be no book
			if (CurrentSelection == null || m_cache == null)
				return string.Empty;

			int iBook = ((ITeView)Control).LocationTracker.GetBookIndex(
				CurrentSelection, selLimitType);

			if (iBook < 0)
				return string.Empty;

			ScrBook book = BookFilter.GetBook(iBook);
			string sBook = book.Name.UserDefaultWritingSystem;
			if (sBook != null)
				return sBook;
			MultilingScrBooks multiScrBooks = new MultilingScrBooks((IScrProjMetaDataProvider)m_scr);
			multiScrBooks.InitializeWritingSystems(m_cache.LanguageWritingSystemFactoryAccessor);
			return multiScrBooks.GetBookName(book.CanonicalNum);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the flid and hvo corresponding to the current Scripture element (e.g.,
		/// section heading, section contents, or title) selected.
		/// </summary>
		/// <param name="tag">The flid of the selected owning element</param>
		/// <param name="hvoSel">The hvo of the selected owning element (hvo of either section
		/// or book)</param>
		/// <returns>True, if a known element is found at this current selection</returns>
		/// -----------------------------------------------------------------------------------
		public virtual bool GetSelectedScrElement(out int tag, out int hvoSel)
		{
			CheckDisposed();

			hvoSel = 0;
			tag = 0;
			if (CurrentSelection == null)
				return false;
			try
			{
				SelectionHelper helper = CurrentSelection;
				SelLevInfo selLevInfo;
				if (!helper.GetLevelInfoForTag((int)ScrBook.ScrBookTags.kflidTitle,
					 SelectionHelper.SelLimitType.Top, out selLevInfo))
				{
					if (!helper.GetLevelInfoForTag((int)ScrSection.ScrSectionTags.kflidHeading,
						 SelectionHelper.SelLimitType.Top, out selLevInfo))
					{
						if (!helper.GetLevelInfoForTag((int)ScrSection.ScrSectionTags.kflidContent,
							 SelectionHelper.SelLimitType.Top, out selLevInfo))
						{
							return false;
						}
					}
				}
				tag = selLevInfo.tag;
				if (selLevInfo.hvo > 0 && m_cache.IsValidObject(selLevInfo.hvo)) // Tests can't depend on throwing for bad HVO value.
					hvoSel = m_cache.GetOwnerOfObject(selLevInfo.hvo);
			}
			catch
			{
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust a paragraph index to a word boundary. Does not move the index if in a verse
		/// number.
		/// </summary>
		/// <param name="tss">the structured string of the paragraph or translation</param>
		/// <param name="ich">the given index</param>
		/// <returns>adjusted character index</returns>
		/// ------------------------------------------------------------------------------------
		public int MoveToWordBoundary(ITsString tss, int ich)
		{
			CheckDisposed();

			string paraText = tss.Text;
			if (paraText != null)
			{
				// Advance to the next word boundary if appropriate
				while (ich < paraText.Length)
				{
					// if the current character is space...
					if (UnicodeCharProps.get_IsSeparator(paraText[ich]))
						ich++;

					// if word-final punctuation...
					else if (UnicodeCharProps.get_IsPunctuation(paraText[ich]) && ich > 0 &&
					!UnicodeCharProps.get_IsSeparator(paraText[ich - 1]))
					{
						ich++; //advance
					}

					else
						break;
				}

				// NEVER move backward if at the end of the paragraph.
				if (ich < paraText.Length)
				{
					// While the insertion point is in the middle of a word then back up to the
					// start of the word or the start of a paragraph.
					while (ich > 0 &&
						!UnicodeCharProps.get_IsSeparator(paraText[ich - 1]) &&
						!UnicodeCharProps.get_IsNumber(paraText[ich - 1]))
						// REVIEW: If the tss contains a number that is plain text,
						// not a verse number, it's going to stop at the text number.
						// e.g. Gideon had 300 warriors.
						// For now, checking the text props instead would seem to cost more
						// than it would benefit.
					{
						ich--;
					}
				}
			}
			return ich;
		}
		#endregion

		#region Paste-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle insertion of paragraphs (i.e., from clipboard) with properties that don't
		/// match the properties of the paragraph where they are being inserted. This gives us
		/// the opportunity to create/modify the DB structure to recieve the paragraphs being
		/// inserted and to reject certain types of paste operations (such as attempting to
		/// paste a book).
		/// </summary>
		/// <param name="rootBox">the sender</param>
		/// <param name="ttpDest">properties of destination paragraph</param>
		/// <param name="cPara">number of paragraphs to be inserted</param>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="tssTrailing">Text of an incomplete paragraph to insert at end (with
		/// the properties of the destination paragraph.</param>
		/// <returns>One of the following:
		/// kidprDefault - causes the base implementation to insert the material as part of the
		/// current StText in the usual way;
		/// kidprFail - indicates that we have decided that this text should not be pasted at
		/// this location at all, causing entire operation to roll back;
		/// kidprDone - indicates that we have handled the paste ourselves, inserting the data
		/// whereever it ought to go and creating any necessary new structure.</returns>
		/// ------------------------------------------------------------------------------------
		public VwInsertDiffParaResponse InsertDiffParas(IVwRootBox rootBox,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrcArray, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			Debug.Assert(!IsBackTranslation);

			if (cPara != ttpSrcArray.Length || cPara != tssParas.Length || ttpDest == null ||
				IsBackTranslation)
				return VwInsertDiffParaResponse.kidprFail;

			// Get the context of the style we are inserting into
			string destStyleName =
				ttpDest.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);
			IStStyle destStyle = null;
			if (destStyleName != null)
				destStyle = m_scr.FindStyle(destStyleName);
			if (destStyle == null)
				return VwInsertDiffParaResponse.kidprFail;
			ContextValues destContext = destStyle.Context;
			StructureValues destStructure = destStyle.Structure;

			// If pasted data came from a non-FW app, all elements in the source props array
			// will be null. In this case, the default behavior will work fine.
			if (ttpSrcArray[0] == null)
			{
				int i;
				for (i = 1; i < ttpSrcArray.Length; i++)
				{
					if (ttpSrcArray[i] != null)
						break;
				}
				if (i >= ttpSrcArray.Length)
					return VwInsertDiffParaResponse.kidprDefault;
			}

			// Look through the source data (being inserted) to see if there is a conflict.
			ContextValues srcContext;
			StructureValues srcStructure;
			if (!GetSourceContextAndStructure(ttpSrcArray, out srcContext, out srcStructure))
			{
				MiscUtils.ErrorBeep();
				return VwInsertDiffParaResponse.kidprFail;
			}

			// If context of the style is Title then throw out the entire insert.
			// REVIEW: Should this allow insertion of a title para within an existing title?
			if (srcContext == ContextValues.Title)
			{
				MiscUtils.ErrorBeep();
				return VwInsertDiffParaResponse.kidprFail;
			}

			// Set a flag if the src context/structure is different from the dest context/structure
			bool foundMismatch = (srcContext != destContext) || (srcStructure != destStructure);

			if (!foundMismatch)
				// let the views handle it!
				return VwInsertDiffParaResponse.kidprDefault;

			bool noTrailingText =
				(tssTrailing == null || tssTrailing.Length == 0);

			// If insertion point is at beginning of paragraph and there is no trailing text
			if (CurrentSelection.IchAnchor == 0 && noTrailingText)
			{
				if (InBookTitle)
				{
					if (InsertParagraphsBeforeBook(ttpSrcArray, tssParas, srcContext))
						return VwInsertDiffParaResponse.kidprDone;
				}
				else if (InSectionHead)
				{
					if (SectionIndex > 0 && ParagraphIndex == 0 &&
						InsertParagraphsBeforeSection(ttpSrcArray, tssParas, srcContext))
						return VwInsertDiffParaResponse.kidprDone;
				}
				else if (srcContext == destContext && InsertParagraphsInSection(ttpSrcArray, tssParas))
					return VwInsertDiffParaResponse.kidprDone;
			}

			// The contexts don't match and we don't handle it so fail.
			// Case not handled - beep and return failure
			MiscUtils.ErrorBeep();
			return VwInsertDiffParaResponse.kidprFail;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the context and structure of the paragraphs to be inserted
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="srcContext">The context of the paragraphs to be inserted, if they
		/// are all the same; undefined if this method returns false.</param>
		/// <param name="srcStructure">The structure of the paragraphs to be inserted, if they
		/// are all the same; StructureValues.Undefined if there is a mix.</param>
		/// <returns>false if any of the paragraphs to be inserted have null properties OR if
		/// there is a mix of different contexts in the paragraphs</returns>
		/// ------------------------------------------------------------------------------------
		private bool GetSourceContextAndStructure(ITsTextProps[] ttpSrcArray,
			out ContextValues srcContext, out StructureValues srcStructure)
		{
			srcContext = ContextValues.General;
			srcStructure = StructureValues.Undefined;

			for(int i = 0; i < ttpSrcArray.Length; i++)
			{
				string srcStyleName = null;
				if (ttpSrcArray[i] != null)
					srcStyleName = ttpSrcArray[i].GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);

				if (srcStyleName == null)
					continue;

				IStStyle srcStyle = m_scr.FindStyle(srcStyleName);
				if (srcStyle == null)
				{
					return false;
					// TODO: Handle case where stuff being pasted from non-FW app begins with a new-line
					//					foundBogusStyle = true;
					//					if (i == 0)
					//					{
					//						ttpSrcArray[0] = ttpDest;
					//						srcStyle = destStyle;
					//					}
					//					else
					//					{
					//						ttpSrcArray[i] = ttpSrcArray[i - 1];
					//						srcStyleName =
					//							ttpSrcArray[i].GetStrPropValue((int)FwTextStringProp.kstpNamedStyle);
					//						srcStyle = m_scr.FindStyle(srcStyleName);
					//					}
				}
				if (i > 0 && srcContext != srcStyle.Context)
					return false;

				srcContext = srcStyle.Context;
				srcStructure = (i > 0 && srcStructure != srcStyle.Structure ?
					StructureValues.Undefined : srcStyle.Structure);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="srcContext">Context of paragraphs to be inserted</param>
		/// <returns>False if we can't allow the insertion</returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertParagraphsBeforeSection(ITsTextProps[] ttpSrcArray,
			ITsString[] tssParas, ContextValues srcContext)
		{
			// save indices, not sure if insertion point will be valid after paragraphs have
			// been added.
			int bookIndex = BookIndex;
			int sectionIndex = SectionIndex;
			ScrBook book = BookFilter.GetBook(bookIndex);
			IScrSection section = book.SectionsOS[sectionIndex - 1];

			if (section.Context != srcContext)
				return false;

			int cAddedSections;
			if (!InsertParagraphsAtSectionEnd(book, section, ttpSrcArray, tssParas,
				sectionIndex, out cAddedSections))
				return false;

			SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, bookIndex,
				sectionIndex + cAddedSections);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts paragraphs into the content of the previous visible book.
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="srcContext">Context of paragraphs to be inserted</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertParagraphsBeforeBook(ITsTextProps[] ttpSrcArray,
			ITsString[] tssParas, ContextValues srcContext)
		{
			int prevBook = BookIndex - 1;
			if (prevBook < 0)
				return false;

			ScrBook book = BookFilter.GetBook(prevBook);
			IScrSection section = book.SectionsOS[book.SectionsOS.Count - 1];

			if (section.Context != srcContext)
				return false;

			int cAddedSections;
			return InsertParagraphsAtSectionEnd(book, section, ttpSrcArray, tssParas,
				book.SectionsOS.Count, out cAddedSections);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts one or more paragraphs at the end of a section.  New sections for the book
		/// may also be created.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="section"></param>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="sectionIndex"></param>
		/// <param name="cAddedSections"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertParagraphsAtSectionEnd(ScrBook book, IScrSection section,
			ITsTextProps[] ttpSrcArray, ITsString[] tssParas, int sectionIndex, out int cAddedSections)
		{
			cAddedSections = 0;
			bool isIntro = false;

			for (int i = 0; i < ttpSrcArray.Length; i++)
			{
				IStStyle style = m_scr.FindStyle(ttpSrcArray[i]);
				if (style.Structure == StructureValues.Heading)
				{
					// If content has been added to section, create a new section.  Otherwise,
					// add the new paragraph to the end of the current section heading.
					if (section.ContentOA.ParagraphsOS.Count > 0)
					{
						isIntro = (style.Context == ContextValues.Intro);
						// Create a new section and add the current paragraph to the heading
						section = ScrSection.CreateEmptySection(book,
							sectionIndex + cAddedSections);
						CreateParagraph(section.HeadingOA, -1, ttpSrcArray[i], tssParas[i]);
						// Need additional prop changed event to get screen to refresh properly
						m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, book.Hvo,
							(int)ScrBook.ScrBookTags.kflidSections,
							sectionIndex + cAddedSections, 1, 0);
						cAddedSections++;
					}
					else
					{
						// Create heading paragraph at end of section heading
						CreateParagraph(section.HeadingOA, -1, ttpSrcArray[i], tssParas[i]);
					}
				}
				else
				{
					// Create content paragraph for the current section
					CreateParagraph(section.ContentOA, -1, ttpSrcArray[i], tssParas[i]);
				}
			}

			// create an empty paragraph if section content is empty
			if (section.ContentOA.ParagraphsOS.Count == 0)
			{
				string styleName =
					isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph;
				StTxtParaBldr bldr = new StTxtParaBldr(m_cache);
				bldr.ParaProps = StyleUtils.ParaStyleTextProps(styleName);
				ITsTextProps charProps = StyleUtils.CharStyleTextProps(styleName,
					m_cache.DefaultVernWs);
				bldr.AppendRun(string.Empty, charProps);
				bldr.CreateParagraph(section.ContentOAHvo);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts one or more paragraphs in the middle of a section.
		/// </summary>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertParagraphsInSection(ITsTextProps[] ttpSrcArray, ITsString[] tssParas)
		{
			// save indices, not sure if insertion point will be valid after paragraphs have
			// been added.
			int bookIndex = BookIndex;
			int sectionIndex = SectionIndex;
			int paragraphIndex = ParagraphIndex;
			ScrBook book = BookFilter.GetBook(bookIndex);
			IScrSection section = book.SectionsOS[sectionIndex];
			int cAddedSections = 0;
			int cAddedParagraphs = 0;	// number of paragraphs added in current section

			for (int i = 0; i < ttpSrcArray.Length; i++)
			{
				IStStyle style = m_scr.FindStyle(ttpSrcArray[i]);
				if (style.Structure == StructureValues.Heading)
				{
					// If not at first paragraph of content, create a new section.  Otherwise
					// add the paragraph to the end of the section heading.
					if (paragraphIndex + cAddedParagraphs > 0)
					{
						cAddedSections++;
						// Create a new section and add the current paragraph to the heading
						IScrSection newSection = ScrSection.CreateEmptySection(book,
							sectionIndex + cAddedSections);
						CreateParagraph(newSection.HeadingOA, -1, ttpSrcArray[i], tssParas[i]);

						// Copy paragraphs after the last added paragraph to the new section.
						StText.MoveTextParagraphs(section.ContentOA, newSection.ContentOA,
							paragraphIndex + cAddedParagraphs, false);
						section = newSection;

						// Need additional prop changed event to get screen to refresh properly
						m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, book.Hvo,
							(int)ScrBook.ScrBookTags.kflidSections,
							sectionIndex + cAddedSections, 1, 0);
						cAddedParagraphs = 0;
						paragraphIndex = 0;
					}
					else
					{
						// Create another heading paragraph for the current section
						CreateParagraph(section.HeadingOA, -1, ttpSrcArray[i], tssParas[i]);
					}
				}
				else
				{
					// Create content paragraph for the current section
					CreateParagraph(section.ContentOA, paragraphIndex + cAddedParagraphs,
						ttpSrcArray[i], tssParas[i]);
					cAddedParagraphs++;
				}
			}

			// Update Insertion point
			SetInsertionPoint(bookIndex, sectionIndex + cAddedSections,
				paragraphIndex + cAddedParagraphs, 0, false);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new paragraph at the end of an StText.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="paragraphIndex"></param>
		/// <param name="ttp"></param>
		/// <param name="tss"></param>
		/// ------------------------------------------------------------------------------------
		private void CreateParagraph(IStText text, int paragraphIndex, ITsTextProps ttp,
			ITsString tss)
		{
			StTxtPara para = new StTxtPara();
			if (paragraphIndex < 0)
			{
				paragraphIndex = text.ParagraphsOS.Count;
				text.ParagraphsOS.Append(para);
			}
			else
				text.ParagraphsOS.InsertAt(para, paragraphIndex);
			para.StyleRules = ttp;
			para.Contents.UnderlyingTsString = tss;

			// Need additional prop changed event to get screen to refresh properly
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, text.Hvo,
				(int)StText.StTextTags.kflidParagraphs, paragraphIndex, 1, 0);
		}
		#endregion

		#region Insert Section
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the Insert Section menu, if IP at the end of a section, inserts a new section.
		/// Sets the IP in the new para of the new section.
		/// </summary>
		/// <param name="insertIntroSection">True to make the created section an intro section,
		/// false otherwise</param>
		/// <returns><c>true</c> if we handle this.</returns>
		/// ------------------------------------------------------------------------------------
		public bool CreateSection(bool insertIntroSection)
		{
			CheckDisposed();

			if (CurrentSelection == null)
				return false;

			// REVIEW: What should be done if user has a range selection?
			if (CurrentSelection.Selection.IsRange)
			{
				SelectionHelper.ReduceSelectionToIp(Callbacks.EditedRootBox.Site,
					SelectionHelper.SelLimitType.Top, true);
			}

			// if someone is updating data don't do the insertion
			if (DataUpdateMonitor.IsUpdateInProgress(m_cache.MainCacheAccessor))
				return true;

			Debug.Assert(EditedRootBox != null);
			if (m_cache == null)
				return false;

			int iSection = SectionIndex;
			int iBook = BookIndex;

			// Verify that IP is in valid location for new section
			if (iSection == -1 && !InBookTitle)
			{
				if (!InTestMode)
				{
					MessageBox.Show(Control,
						TeResourceHelper.GetResourceString("kstidInsertSectionNotAllowed"),
						TeResourceHelper.GetResourceString("kstidApplicationName"));
				}

				return true;
			}

			IVwRootSite rootSite = EditedRootBox.Site;
			// make sure noone else can update the data
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertSection", out undo, out redo);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(rootSite, undo, redo, true))
			using (new DataUpdateMonitor(Control, m_cache.MainCacheAccessor, rootSite, "Insert Section"))
			using (new WaitCursor(Control))
			{
				try
				{
					ScrBook book = BookFilter.GetBook(iBook);
					IScrSection section = null;
					if (iSection >= 0)
						section = book[iSection];
					int ichIP = CurrentSelection.IchAnchor;
					int iPara = CurrentSelection.LevelInfo[0].ihvo;
					StTxtPara para = null;

					if (InBookTitle)
					{
						iSection = 0;
						InsertSectionAtIndex(book, iSection, insertIntroSection);
					}
					else if (CurrentSelection.LevelInfo[1].tag ==
						(int)ScrSection.ScrSectionTags.kflidContent)
					{
						// if IP is in a content paragraph
						para = (StTxtPara)section.ContentOA.ParagraphsOS[iPara];
						if (ichIP > para.Contents.Length)
							ichIP = para.Contents.Length;

						// we insert the new section after the current one, so the current section
						// moves down
						iSection++;

						// Now insert the new section
						// if IP is at the end of the last paragraph of the section...
						if (iPara == section.ContentOA.ParagraphsOS.Count - 1 &&
							(para.Contents.Text == null ||
							ichIP == para.Contents.Length))
						{
							InsertSectionAtIndex(book, iSection, insertIntroSection);
						}
						else
						{
							// Need to create a new section and split the section content between
							// the new section and the existing section
							IScrSection newSection =
								ScrSection.CreateSectionWithHeadingPara(book, iSection, insertIntroSection);
							StText.MovePartialContents(section.ContentOA,
								newSection.ContentOA, iPara, ichIP, false);
						}
					}
					else if (CurrentSelection.LevelInfo[1].tag ==
						(int)ScrSection.ScrSectionTags.kflidHeading)
					{
						para = (StTxtPara)section.HeadingOA.ParagraphsOS[iPara];
						if (ichIP > para.Contents.Length)
							ichIP = para.Contents.Length;

						if (iPara == 0 && ichIP == 0)
						{
							// Insert an empty section before the current section
							InsertSectionAtIndex(book, iSection, insertIntroSection);
						}
						else
						{
							// Need to create a new section and split the section heading between
							// the new section and the existing section
							IScrSection newSection =
								ScrSection.CreateSectionWithContentPara(book, iSection, insertIntroSection);
							StText.MovePartialContents(section.HeadingOA,
								newSection.HeadingOA, iPara, ichIP, true);
						}
					}

					m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
						book.Hvo, (int)ScrBook.ScrBookTags.kflidSections,
						iSection, 1, 0);

					// Set the insertion point at the beg of the new section contents
					if (iBook > -1)
					{
						ScrSection newSection = new ScrSection(m_cache, book[iSection].Hvo);
						SelectionHelper selHelperIP = null;
						StTxtPara firstPara = newSection.FirstContentParagraph;

						// if dividing an existing section...
						if (firstPara != null && firstPara.Contents.Length > 0)
						{
							// otherwise, set the IP in the section heading
							selHelperIP = SetInsertionPoint(
								(int)ScrSection.ScrSectionTags.kflidHeading, iBook, iSection);
						}
						else
						{
							// set the IP in the paragraph content
							selHelperIP = SetInsertionPoint(iBook, iSection, 0, 0, false);
						}

						// Don't set the character props if the selection is a range
						// selection. This most likely is the case only if the new
						// selection is in a user prompt. The user prompt consists of
						// 2 runs which makes the below code fail because we only give
						// it enough props to replace 1 run.
						if (selHelperIP != null && selHelperIP.Selection != null &&
							!selHelperIP.Selection.IsRange)
						{
							//TODO: Only do this if we're creating a brand new section, not if
							// we're splitting one
							ITsTextProps[] rgttp = new ITsTextProps[] {
										StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs) };
							selHelperIP.Selection.SetSelectionProps(1, rgttp);
						}
					}

					return true;
				}
				catch (Exception)
				{
					undoTaskHelper.EndUndoTask = false;
					if (FwApp.App != null) // can be null when testing
						FwApp.App.RefreshAllViews(m_cache);
					throw;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new section at the given position. The new section will include an empty
		/// paragraph.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="iSection"></param>
		/// <param name="isIntroSection"></param>
		/// ------------------------------------------------------------------------------------
		protected internal IScrSection InsertSectionAtIndex(ScrBook book, int iSection,
			bool isIntroSection)
		{
			if (m_cache == null)
				return null;

			return ScrSection.CreateSectionWithEmptyParas(book, iSection, isIntroSection);
		}
		#endregion

		#region Insert Book
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert Scripture book in view.
		/// </summary>
		/// <param name="bookNum">Ordinal number of the book to insert (i.e. one-based book
		/// number).</param>
		/// <returns>The new book.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook InsertBook(int bookNum)
		{
			CheckDisposed();

			if (m_cache == null)
				return null;

			IVwRootSite rootSite = EditedRootBox.Site;
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertBook", out undo, out redo);
			using (new UndoTaskHelper(rootSite, undo, redo, true))
			using (new DataUpdateMonitor(Control, m_cache.MainCacheAccessor, rootSite, "InsertBook"))
			{
				m_cache.ActionHandlerAccessor.AddAction(new UndoWithSyncAction(m_cache,
					SyncMsg.ksyncReloadScriptureControl));

				UndoInsertBookAction undoInsertBookAction = new UndoInsertBookAction(m_cache,
					BookFilter, bookNum);

				IScrBook newBook = undoInsertBookAction.Do();

				// Add another action to update the scripture control so it will get done
				// at both ends of the undo/redo
				m_cache.ActionHandlerAccessor.AddAction(new UndoWithSyncAction(m_cache,
					SyncMsg.ksyncReloadScriptureControl));
				m_cache.ActionHandlerAccessor.AddAction(undoInsertBookAction);

				// Set the insertion point at the end of the new section contents
				IVwRootSite rootsite = Callbacks.EditedRootBox.Site;
				if (rootsite != null && rootsite.RootBox != null)
				{
					SelectionHelper selHelper = SetInsertionPoint(0, 0, 0, 1, false);
					ITsTextProps[] rgttp = new ITsTextProps[] {
							StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs) };

					if (selHelper != null && selHelper.Selection != null)
						selHelper.Selection.SetSelectionProps(1, rgttp);
				}

				return newBook;
			}
		}

		#endregion

		#region Problem Deletion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a problem deletion - a complex selection crossing sections or other
		/// difficult cases such as BS/DEL at boundaries.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="dpt"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();

			// Problem deletions are not permitted in a back translation text.
			if (IsBackTranslation)
			{
				MiscUtils.ErrorBeep();
				return VwDelProbResponse.kdprAbort;
			}

			SelectionHelper helper = SelectionHelper.GetSelectionInfo(sel,
				Callbacks.EditedRootBox.Site);

			if (helper == null)
			{
				MiscUtils.ErrorBeep();
				throw new NotImplementedException();
			}

			// There should only be one scripture object as the root of the view.
			// If this changes, enhance this method to fail if they are not equal.
			Debug.Assert(helper.GetIhvoRoot(SelectionHelper.SelLimitType.Top) == 0);
			Debug.Assert(helper.GetIhvoRoot(SelectionHelper.SelLimitType.Bottom) == 0);

			// Handle a BS/DEL at the boundary of a paragraph
			if (dpt == VwDelProbType.kdptBsAtStartPara ||
				dpt == VwDelProbType.kdptDelAtEndPara)
			{
				using (new WaitCursor(Control))
				{
					if (HandleBsOrDelAtTextBoundary(helper, dpt))
						return VwDelProbResponse.kdprDone;
				}
			}

				// handle a complex selection range where the deletion will require restructuring
				// paragraphs or sections.
			else if (dpt == VwDelProbType.kdptComplexRange)
			{
				using (new WaitCursor(Control))
				{
					if (HandleComplexDeletion(helper))
						return VwDelProbResponse.kdprDone;
					else
					{
						// Don't want default behavior for complex deletions since it
						// only will delete a single paragraph and this is probably worse
						// than doing nothing.
						MiscUtils.ErrorBeep();
						return VwDelProbResponse.kdprAbort;
					}
				}
			}
			// If it's not a case we know how to deal with, treat as not implemented and accept
			// the default behavior.

			MiscUtils.ErrorBeep();
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to do something about an IP selection deletion that is at the start or end of an
		/// StText. If successful return true, otherwise false.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="dpt"></param>
		/// <returns><c>true</c> if we successfully handled the deletion.</returns>
		/// ------------------------------------------------------------------------------------
		internal bool HandleBsOrDelAtTextBoundary(SelectionHelper helper, VwDelProbType dpt)
		{
			CheckDisposed();

			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);

			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			// bail out if we are not in a paragraph within a scripture section
			if (levInfo.Length !=
				tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent) ||
				tracker.GetSectionIndexInView(helper, SelectionHelper.SelLimitType.Anchor) < 0 ||
				levInfo[0].tag != (int)StText.StTextTags.kflidParagraphs)
			{
				// Assume we are in a book title
				SelLevInfo dummyInfo;
				if (helper.GetLevelInfoForTag((int)ScrBook.ScrBookTags.kflidTitle, out dummyInfo))
					return MergeParasInTable(helper, dpt);
				return false;
			}

			// Level 1 will have tags showing which field of section is selected
			int iLevelSection = helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidHeading);
			if (iLevelSection >= 0)
			{
				if (levInfo[0].ihvo == 0 && dpt == VwDelProbType.kdptBsAtStartPara)
				{
					// first paragraph of section head
					return HandleBackspaceAfterEmptyContentParagraph(helper);
				}
				else if (levInfo[0].ihvo == 0 && helper.IchAnchor == 0)
				{
					// Delete was pressed in an empty section head - try to combine with previous
					// return DeleteSectionHead(helper, false, false);
					if (dpt == VwDelProbType.kdptBsAtStartPara)
						return HandleBackspaceAfterEmptySectionHeadParagraph(helper);
					return HandleDeleteBeforeEmptySectionHeadParagraph(helper);
				}
				// NOTE: we check the vector size for the parent of the paragraph (levInfo[1].hvo)
				// but with our own tag (levInfo[0].tag)!
				else if (levInfo[0].ihvo == m_cache.GetVectorSize(levInfo[iLevelSection].hvo,
					levInfo[0].tag) - 1
					&& dpt == VwDelProbType.kdptDelAtEndPara)
				{
					// last paragraph of section head
					return HandleDeleteBeforeEmptySectionContentParagraph(helper);
				}
				else
				{
					// other problem deletion: e.g. delete in BT side-by-side view. Because
					// we're displaying the paragraphs in a table with two columns, the views
					// code can't handle that. We have to merge the two paragraphs manually.
					return MergeParasInTable(helper, dpt);
				}
			}
			else if (helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidContent) >= 0)
			{
				iLevelSection = helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidContent);
				if (levInfo[0].ihvo == 0 && dpt == VwDelProbType.kdptBsAtStartPara)
				{
					// first paragraph of section
					return HandleBackspaceAfterEmptySectionHeadParagraph(helper);
				}
				else if (levInfo[0].ihvo == 0 && helper.IchAnchor == 0)
				{
					// Delete was pressed in an empty section content - try to combine with previous
					if (dpt == VwDelProbType.kdptBsAtStartPara)
						return HandleBackspaceAfterEmptyContentParagraph(helper);
					return HandleDeleteBeforeEmptySectionContentParagraph(helper);
				}
				// NOTE: we check the vector size for the parent of the paragraph (levInfo[1].hvo)
				// but with our own tag (levInfo[0].tag)!
				else if (levInfo[0].ihvo == m_cache.GetVectorSize(levInfo[iLevelSection].hvo,
					levInfo[0].tag) - 1 && dpt == VwDelProbType.kdptDelAtEndPara)
				{
					// last paragraph of section
					return HandleDeleteBeforeEmptySectionHeadParagraph(helper);
				}
				else
				{
					// other problem deletion: e.g. delete in BT side-by-side view. Because
					// we're displaying the paragraphs in a table with two columns, the views
					// code can't handle that. We have to merge the two paragraphs manually.
					return MergeParasInTable(helper, dpt);
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the paras in table.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="dpt">The problem deletion type.</param>
		/// <returns><c>true</c> if we merged the paras, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		internal protected bool MergeParasInTable(SelectionHelper helper, VwDelProbType dpt)
		{
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			if (levInfo[0].tag != (int)StText.StTextTags.kflidParagraphs)
				return false;

			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			IScrBook book = new ScrBook(m_cache, tracker.GetBookHvo(
				helper, SelectionHelper.SelLimitType.Anchor));

			SelLevInfo tmpInfo;
			IStText text;
			if (helper.GetLevelInfoForTag((int)ScrBook.ScrBookTags.kflidTitle, out tmpInfo))
				text = book.TitleOA;
			else
			{
				IScrSection section = book.SectionsOS[tracker.GetSectionIndexInBook(
					helper,	SelectionHelper.SelLimitType.Anchor)];

				text = (levInfo[1].tag == (int)ScrSection.ScrSectionTags.kflidHeading ?
					section.HeadingOA :	text = section.ContentOA);
			}

			int iPara = helper.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs).ihvo;
			StTxtPara currPara = (StTxtPara)text.ParagraphsOS[iPara];
			ITsStrBldr bldr;

			// Backspace at beginning of paragraph
			if (dpt == VwDelProbType.kdptBsAtStartPara)
			{
				if (iPara <= 0)
				{
					MiscUtils.ErrorBeep();
					return false;
				}

				StTxtPara prevPara = (StTxtPara)text.ParagraphsOS[iPara - 1];
				int prevParaLen = prevPara.Contents.Length;

				// Need to make sure we move the back translations
				AboutToDelete(helper, currPara.Hvo, text.Hvo,
					(int)StText.StTextTags.kflidParagraphs, iPara, false);

				bldr = prevPara.Contents.UnderlyingTsString.GetBldr();
				bldr.ReplaceTsString(prevPara.Contents.Length, prevPara.Contents.Length,
					currPara.Contents.UnderlyingTsString);
				prevPara.Contents.UnderlyingTsString = bldr.GetString();
				text.ParagraphsOS.RemoveAt(iPara);
				helper.SetIch(SelectionHelper.SelLimitType.Top, prevParaLen);
				helper.SetIch(SelectionHelper.SelLimitType.Bottom, prevParaLen);
				levInfo[0].ihvo = iPara - 1;
				helper.SetLevelInfo(SelectionHelper.SelLimitType.Top, levInfo);
				helper.SetLevelInfo(SelectionHelper.SelLimitType.Bottom, levInfo);
				helper.SetSelection(true);
				return true;
			}
			// delete at end of a paragraph
			int cParas = text.ParagraphsOS.Count;
			if (iPara + 1 >= cParas)
				return false; // We don't handle merging across StTexts

			StTxtPara nextPara = (StTxtPara)text.ParagraphsOS[iPara + 1];

			// Need to make sure we move the back translations
			AboutToDelete(helper, nextPara.Hvo, text.Hvo,
				(int)StText.StTextTags.kflidParagraphs, iPara + 1, false);

			bldr = currPara.Contents.UnderlyingTsString.GetBldr();
			bldr.ReplaceTsString(currPara.Contents.Length, currPara.Contents.Length,
				nextPara.Contents.UnderlyingTsString);
			currPara.Contents.UnderlyingTsString = bldr.GetString();
			text.ParagraphsOS.RemoveAt(iPara + 1);
			helper.SetSelection(true);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletions where the selection is not contained within paragraphs of a
		/// single section.
		/// </summary>
		/// <param name="helper"></param>
		/// <returns><code>true</code> if deletion was handled</returns>
		/// ------------------------------------------------------------------------------------
		internal protected bool HandleComplexDeletion(SelectionHelper helper)
		{
			SelLevInfo[] topInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			SelLevInfo[] bottomInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom);

			// Determine if whole section head was deleted
			if (IsSectionHeadDeletion(helper, topInfo, bottomInfo))
			{
				return DeleteSectionHead(helper, true, false);
			}
			else if (IsSectionDeletion(helper, topInfo, bottomInfo))
			{
				return DeleteSections(helper, topInfo, bottomInfo);
			}
			else if (IsMultiSectionContentSelection(helper, topInfo, bottomInfo) &&
				SectionsHaveSameContext(helper, topInfo, bottomInfo))
			{
				return DeleteMultiSectionContentRange(helper);
			}
			else if (IsBookTitleSelection(helper, topInfo, bottomInfo))
			{
				return DeleteBookTitle();
			}
			else if (IsBookSelection(helper, topInfo, bottomInfo))
			{
				// Don't use BookIndex, since it will use anchor which may be wrong book.
				RemoveBook(((ITeView)Control).LocationTracker.GetBookIndex(
					helper, SelectionHelper.SelLimitType.Top));
				return true;
			}
			// TODO: Add complex deletion when deleting a range selection that includes two
			// partial sections.
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a single section is wholly selected or the selection goes from the top of one
		/// section to the top of the following section, then the selected section may be
		/// deleted.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSectionDeletion(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			bool sectionDeletion = false;
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			int clev = tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent);
			int levelText = helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidContent);
			if (levelText < 0)
				levelText = helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidHeading);
			int levelPara = helper.GetLevelForTag((int)StText.StTextTags.kflidParagraphs);
			// the selection must be the right number of levels deep.
			if (topInfo.Length == clev && bottomInfo.Length == clev && levelText >= 0 && levelPara >= 0)
			{
				// if the selection is in the same scripture and the same book...
				if (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					// not selecting something weird
					topInfo[levelText - 1].tag == (int)StText.StTextTags.kflidParagraphs &&
					bottomInfo[levelText - 1].tag == (int)StText.StTextTags.kflidParagraphs)
				{
					// Selection top is in one section and bottom is in a following one
					if (tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) <=
						tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) - 1 &&
						// if the selection begins and ends in section heading
						topInfo[levelText].tag == (int)ScrSection.ScrSectionTags.kflidHeading &&
						bottomInfo[levelText].tag == (int)ScrSection.ScrSectionTags.kflidHeading &&
						topInfo[levelText].ihvo == 0 &&
						bottomInfo[levelText].ihvo == 0 &&
						// and the selection begins and ends in the first paragraph of those headings
						topInfo[levelPara].ihvo == 0 &&
						bottomInfo[levelPara].ihvo == 0)
					{
						// Does selection go from the beginning of one heading to the beginning of another?
						sectionDeletion = (helper.IchAnchor == 0 && helper.IchEnd == 0);
					}
					// Selection starts at beginning of one section heading and ends
					// at end of content of another section.
					else if (tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) <=
						tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) &&
						// Selection top is in section heading and bottom is in content
						topInfo[levelText].tag == (int)ScrSection.ScrSectionTags.kflidHeading &&
						bottomInfo[levelText].tag == (int)ScrSection.ScrSectionTags.kflidContent &&
						topInfo[levelText].ihvo == 0 &&
						bottomInfo[levelText].ihvo == 0 &&
						// Top of selection is in first heading paragraph
						topInfo[levelPara].ihvo == 0)
					{
						int ichTop = helper.GetIch(SelectionHelper.SelLimitType.Top);
						sectionDeletion =
							ichTop == 0 && IsSelectionAtEndOfSection(helper, bottomInfo, clev);
					}
				}
			}
			return sectionDeletion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selections at end of section.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="bottomInfo">The bottom info.</param>
		/// <param name="clev">The clev.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSelectionAtEndOfSection(SelectionHelper helper, SelLevInfo[] bottomInfo,
			int clev)
		{
			bool atEndOfSection = false;
			// Is the bottom of the selection in the last para of the section?
			ScrSection bottomSection = new ScrSection(m_cache,
				((ITeView)Control).LocationTracker.GetSectionHvo(
				helper, SelectionHelper.SelLimitType.Bottom));

			IStText bottomContent = bottomSection.ContentOA;
			int levelPara = helper.GetLevelForTag((int)StText.StTextTags.kflidParagraphs);
			if (bottomInfo[levelPara].ihvo == bottomContent.ParagraphsOS.Count - 1)
			{
				StTxtPara lastPara = new StTxtPara(m_cache, bottomInfo[levelPara].hvo);
				ITsString lastParaContents = lastPara.Contents.UnderlyingTsString;
				int ichBottom = helper.GetIch(SelectionHelper.SelLimitType.Bottom);
				atEndOfSection = ichBottom == lastParaContents.Length;
			}
			return atEndOfSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether this is a selection that goes from the content of one section to
		/// the content of another.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsMultiSectionContentSelection(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			bool multiSectionSelection = false;
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int sectionTag = (int)ScrSection.ScrSectionTags.kflidContent;
			int clev = tracker.GetLevelCount(sectionTag);

			if (topInfo.Length == clev && bottomInfo.Length == clev)
			{
				int paraTag = (int)StText.StTextTags.kflidParagraphs;
				// if selection starts in content of section in one book and goes to content
				// of another section in the same book
				int sectionLevelTop = helper.GetLevelForTag(sectionTag, SelectionHelper.SelLimitType.Top);
				if (sectionLevelTop == -1)
					return false;

				if (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) <
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) &&
					sectionLevelTop ==
					helper.GetLevelForTag(sectionTag, SelectionHelper.SelLimitType.Bottom) &&
					helper.GetLevelForTag(paraTag, SelectionHelper.SelLimitType.Top) ==
					helper.GetLevelForTag(paraTag, SelectionHelper.SelLimitType.Bottom))
				{
					multiSectionSelection = true;
				}
			}

			return multiSectionSelection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the selection starts at the beginning of the section head and ends at
		/// the beginning of the section content.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsSectionHeadDeletion(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			int levelText = helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidContent);
			if (levelText < 0)
				levelText = helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidHeading);
			if (levelText < 0)
				return false;

			bool sectionHeadDeletion = false;
			int levelPara = helper.GetLevelForTag((int)StText.StTextTags.kflidParagraphs);
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int clev = tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent);

			if (topInfo.Length == clev && bottomInfo.Length == clev && levelPara >= 0)
			{
				if (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) &&
					topInfo[levelText].tag == (int)ScrSection.ScrSectionTags.kflidHeading &&
					bottomInfo[levelText].tag == (int)ScrSection.ScrSectionTags.kflidContent &&
					topInfo[levelText].ihvo == 0 &&
					bottomInfo[levelText].ihvo == 0 &&
					topInfo[levelPara].tag == (int)StText.StTextTags.kflidParagraphs &&
					bottomInfo[levelPara].tag == (int)StText.StTextTags.kflidParagraphs &&
					topInfo[levelPara].ihvo == 0 &&
					bottomInfo[levelPara].ihvo == 0)
				{
					sectionHeadDeletion = (helper.IchAnchor == 0 && helper.IchEnd == 0);
					if (sectionHeadDeletion)
					{
						StTxtPara para = new StTxtPara(m_cache, bottomInfo[levelPara].hvo);
						sectionHeadDeletion = (para.Contents.Text != null);
					}
				}
			}

			return sectionHeadDeletion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the selection starts at the beginning of the book title and ends
		/// at the beginning of the first section heading of the book.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsBookTitleSelection (SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			if (topInfo.Length != tracker.GetLevelCount((int)ScrBook.ScrBookTags.kflidTitle) ||
				bottomInfo.Length != tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent))
			{
				return false;
			}

			return (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
				tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
				topInfo[1].tag == (int)ScrBook.ScrBookTags.kflidTitle &&
				topInfo[1].ihvo == 0 &&
				topInfo[0].tag == (int)StText.StTextTags.kflidParagraphs &&
				topInfo[0].ihvo == 0 &&
				tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom) == 0 &&
				bottomInfo[1].tag == (int)ScrSection.ScrSectionTags.kflidHeading &&
				bottomInfo[1].ihvo == 0 &&
				bottomInfo[0].tag == (int)StText.StTextTags.kflidParagraphs &&
				bottomInfo[0].ihvo == 0 &&
				helper.IchAnchor == 0 &&
				helper.IchEnd == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the selection is an entire book
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool IsBookSelection (SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int bookTitleLevelCount = tracker.GetLevelCount((int)ScrBook.ScrBookTags.kflidTitle);
			if (topInfo.Length == bookTitleLevelCount && bottomInfo.Length == bookTitleLevelCount)
			{
				// Verify that selection goes from beginning of book title to
				// beginning of title in next book.
				return (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) - 1 &&
					topInfo[1].tag == (int)ScrBook.ScrBookTags.kflidTitle &&
					topInfo[1].ihvo == 0 &&
					topInfo[0].tag == (int)StText.StTextTags.kflidParagraphs &&
					topInfo[0].ihvo == 0 &&
					bottomInfo[1].tag == (int)ScrBook.ScrBookTags.kflidTitle &&
					bottomInfo[1].ihvo == 0 &&
					bottomInfo[0].tag == (int)StText.StTextTags.kflidParagraphs &&
					bottomInfo[0].ihvo == 0 &&
					helper.IchAnchor == 0 &&
					helper.IchEnd == 0);
			}
			else if (topInfo.Length == bookTitleLevelCount && bottomInfo.Length ==
				tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent))
			{
				// Check that selection is at start of title
				bool bookSel = (tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
					tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom) &&
					topInfo[1].tag == (int)ScrBook.ScrBookTags.kflidTitle &&
					topInfo[1].ihvo == 0 &&
					topInfo[0].tag == (int)StText.StTextTags.kflidParagraphs &&
					topInfo[0].ihvo == 0 &&
					helper.GetIch(SelectionHelper.SelLimitType.Top) == 0);

				if (bookSel)
				{
					// Get information about last paragraph of the book
					ScrBook book = new ScrBook(m_cache,
						tracker.GetBookHvo(helper, SelectionHelper.SelLimitType.Top));
					int iSection = book.SectionsOS.Count - 1;
					IScrSection section = book.SectionsOS[iSection];
					int iPara = section.ContentOA.ParagraphsOS.Count - 1;
					StTxtPara para = (StTxtPara) section.ContentOA.ParagraphsOS[iPara];
					int ichEnd = para.Contents.Length;

					// Check that selection is at end of last paragraph of book
					bookSel = (tracker.GetSectionIndexInBook(helper,
						SelectionHelper.SelLimitType.Bottom) == iSection &&
						bottomInfo[1].tag == (int)ScrSection.ScrSectionTags.kflidContent &&
						bottomInfo[0].ihvo == iPara &&
						bottomInfo[0].tag == (int)StText.StTextTags.kflidParagraphs &&
						helper.GetIch(SelectionHelper.SelLimitType.Bottom) == ichEnd);
				}

				return bookSel;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete all footnotes between firstFootnoteToDelete and lastFootnoteToDelete.
		/// </summary>
		/// <remarks>Use this version of DeleteFootnotes() when there is no need to clean up
		/// ORCs in corresponding BTs, such as when the deletion being performed is deleting
		/// only entire paragraphs.</remarks>
		/// <param name="book">Book that contains the sections to delete</param>
		/// <param name="firstFootnoteToDelete">First footnote that will be deleted
		/// (might be null).</param>
		/// <param name="lastFootnoteToDelete">Last footnote that will be deleted
		/// (might be null).</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnotes(IScrBook book, ScrFootnote firstFootnoteToDelete,
			ScrFootnote lastFootnoteToDelete)
		{
			DeleteFootnotes(book, firstFootnoteToDelete, lastFootnoteToDelete, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete all footnotes between firstFootnoteToDelete and lastFootnoteToDelete. Clean
		/// up BT ORCs for the paragraphs specified.
		/// </summary>
		/// <param name="book">Book that contains the sections to delete</param>
		/// <param name="firstFootnoteToDelete">First footnote that will be deleted
		/// (might be null).</param>
		/// <param name="lastFootnoteToDelete">Last footnote that will be deleted
		/// (might be null).</param>
		/// <param name="hvoFirstPara">First (possibly partial) paragraph being deleted that
		/// might contain ORCs for the first footnotes. Any footnotes being deleted whose ORCs
		/// are in this paragraph will have their corresponding BT ORCs removed as well.
		/// If zero, we don't bother to remove any corresponding BT ORCs in the first para.</param>
		/// <param name="hvoLastPara">Last (possibly partial) paragraph being deleted that
		/// might contain ORCs for the last footnotes. Any footnotes being deleted whose ORCs
		/// are in this paragraph will have their corresponding BT ORCs removed as well.
		/// If zero, we don't bother to remove any corresponding BT ORCs in the last para.</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnotes(IScrBook book, ScrFootnote firstFootnoteToDelete,
			ScrFootnote lastFootnoteToDelete, int hvoFirstPara, int hvoLastPara)
		{
			Debug.Assert((firstFootnoteToDelete == null && lastFootnoteToDelete == null)
				|| (firstFootnoteToDelete != null && lastFootnoteToDelete != null));

			if (firstFootnoteToDelete != null)
			{
				bool fDeleteFootnote = false;
				int[] hvos = book.FootnotesOS.HvoArray;
				for (int i = hvos.Length - 1; i >= 0; i--)
				{
					if (hvos[i] == lastFootnoteToDelete.Hvo)
						fDeleteFootnote = true; // we are now in the range of footnotes to delete

					if (fDeleteFootnote)
					{
						// This code fixes TE-4882.
						ScrFootnote footnote = new ScrFootnote(m_cache, hvos[i]);
						int hvoPara = footnote.ContainingParagraphHvo;
						Debug.Assert(hvoPara > 0);
						if ((hvoPara == hvoFirstPara || hvoPara == hvoLastPara) && hvoPara > 0)
						{
							StTxtPara para = new StTxtPara(m_cache, hvoPara);
							para.DeleteAnyBtMarkersForFootnote(footnote.Guid);
						}
						m_cache.DeleteObject(hvos[i]);
					}

					if (hvos[i] == firstFootnoteToDelete.Hvo)
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the sections that are entirely spanned by the given selection. Selection is
		/// assumed to start a heading of one section and either end at the end of the content
		/// of a section or end at the beginning of another section heading. All selected
		/// sections will be deleted.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteSections(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;

			// Verify some of the assumptions
			int sectionLevelCount = tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent);
			Debug.Assert(topInfo.Length == sectionLevelCount);
			Debug.Assert(bottomInfo.Length == sectionLevelCount);
			Debug.Assert(topInfo[0].tag == (int)StText.StTextTags.kflidParagraphs &&
				bottomInfo[0].tag == (int)StText.StTextTags.kflidParagraphs);
			Debug.Assert(tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top) ==
				tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom));

			int ihvoBook = tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top);
			IScrBook book = BookFilter.GetBook(ihvoBook);

			int ihvoSectionStart = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			int ihvoSectionEnd = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom);
			// if selection ends in heading, delete stops at end of previous section.
			if (bottomInfo[1].tag == (int)ScrSection.ScrSectionTags.kflidHeading)
				ihvoSectionEnd--;
			int cDeletedSections = ihvoSectionEnd - ihvoSectionStart + 1;

			// Delete footnotes that are in the sections to delete
			FdoOwningSequence<IScrSection> sections = book.SectionsOS;
			ScrFootnote firstFootnoteToDelete = ScrFootnote.FindFirstFootnoteInSectionRange(sections,
				ihvoSectionStart, ihvoSectionEnd);
			ScrFootnote lastFootnoteToDelete = firstFootnoteToDelete != null ?
				ScrFootnote.FindLastFootnoteInSectionRange(sections, ihvoSectionStart, ihvoSectionEnd) :
				null;
			DeleteFootnotes(book, firstFootnoteToDelete, lastFootnoteToDelete);

			// If all sections in the book are being deleted, then delete all except one
			// section and then delete the header and contents of the last remaining
			// section. This will leave a skeleton structure of a single section in place.
			if (sections.Count == cDeletedSections)
			{
				for (int i = 1; i < cDeletedSections; i++)
					sections.RemoveAt(0);

				IScrSection section = sections[0];

				FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
				while (paras.Count > 1)
					paras.RemoveAt(paras.Count - 1);
				StTxtPara para = (StTxtPara)paras[0];
				ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
				bldr.Clear();
				para.Contents.UnderlyingTsString = bldr.GetString();

				paras = section.ContentOA.ParagraphsOS;
				while (paras.Count > 1)
					paras.RemoveAt(paras.Count - 1);
				para = (StTxtPara)paras[0];
				bldr = para.Contents.UnderlyingTsString.GetBldr();
				bldr.Clear();
				para.Contents.UnderlyingTsString = bldr.GetString();
			}
			else
			{
				for (int i = ihvoSectionEnd; i >= ihvoSectionStart; i--)
					sections.RemoveAt(i);
			}

			if (ihvoSectionStart < sections.Count)
				SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, ihvoBook,
					ihvoSectionStart);
			else
			{
				// Need to set position at end of new last section when last section has been
				// deleted
				IScrSection lastSection = sections[sections.Count - 1];
				FdoOwningSequence<IStPara> lastSectionParas = lastSection.ContentOA.ParagraphsOS;
				StTxtPara lastParaInSection =
					(StTxtPara)lastSectionParas[lastSectionParas.Count - 1];
				int lastPosition = lastParaInSection.Contents.Length;
				SetInsertionPoint(ihvoBook, sections.Count - 1, lastSectionParas.Count - 1,
					lastPosition, helper.AssocPrev);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the title of the currently selected book and makes the empty paragraph
		/// have "Main Title" style.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteBookTitle()
		{
			int iBook = BookIndex;
			ScrBook book = BookFilter.GetBook(iBook);

			// remove all except first paragraph of book title
			for (int i = book.TitleOA.ParagraphsOS.Count - 1; i > 0; i--)
				book.TitleOA.ParagraphsOS.RemoveAt(i);

			// Clear contents of remaining paragraph
			StTxtPara para = (StTxtPara) book.TitleOA.ParagraphsOS[0];
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle);
			ITsTextProps ttp = para.Contents.UnderlyingTsString.get_Properties(0);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			// Set the writing system to the previously used writing system, if valid.
			// Otherwise, use the default vernacular writing system.
			int dummy;
			int titleWs = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
			para.Contents.UnderlyingTsString = tsf.MakeString(string.Empty,
				titleWs > 0 ? titleWs : RootVcDefaultWritingSystem);

			// Set selection into title
			SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, iBook, 0);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the given selection, removing any spanned sections. Selection is assumed to
		/// start in content of one section and end in content of another section.
		/// </summary>
		/// <param name="helper"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteMultiSectionContentRange(SelectionHelper helper)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int ihvoBook = tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top);
			int ihvoSectionStart =
				tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			int ihvoSectionEnd =
				tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Bottom);
			if (ihvoBook < 0 || ihvoSectionStart < 0 || ihvoSectionEnd < 0)
			{
				// Something changed catastrophically. Maybe we added introductory material?
				Debug.Assert(false);
				return false;
			}
			ScrBook book = BookFilter.GetBook(ihvoBook);
			IScrSection sectionStart = book.SectionsOS[ihvoSectionStart];
			IScrSection sectionEnd = book.SectionsOS[ihvoSectionEnd];
			int ihvoParaStart = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo;
			StTxtPara paraStart = (StTxtPara) sectionStart.ContentOA.ParagraphsOS[ihvoParaStart];
			int ihvoParaEnd = helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom)[0].ihvo;
			StTxtPara paraEnd = (StTxtPara) sectionEnd.ContentOA.ParagraphsOS[ihvoParaEnd];
			int ichStart = helper.GetIch(SelectionHelper.SelLimitType.Top);
			int ichEnd = helper.GetIch(SelectionHelper.SelLimitType.Bottom);

			if (helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].tag !=
				(int)StText.StTextTags.kflidParagraphs)
			{
				// Something changed catastrophically. StText has something in it other than paragraphs!
				Debug.Assert(false);
				return false;
			}

			// Remove footnotes of sections that will be deleted entirely
			int iSection = ihvoSectionStart;
			int iPara = ihvoParaStart;
			int ich = ichStart;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote firstFootnoteToDelete = ScrFootnote.FindNextFootnote(m_cache,
				book, ref iSection, ref iPara, ref ich, ref tag, false);
			if (iSection < ihvoSectionEnd || (iSection == ihvoSectionEnd &&
				(iPara < ihvoParaEnd || (iPara == ihvoParaEnd && ich <= ichEnd))) ||
				tag != (int)ScrSection.ScrSectionTags.kflidContent)
			{
				iSection = ihvoSectionEnd;
				iPara = ihvoParaEnd;
				ich = ichEnd;
				tag = (int)ScrSection.ScrSectionTags.kflidContent;
				ScrFootnote lastFootnoteToDelete = ScrFootnote.FindPreviousFootnote(m_cache,
					book, ref iSection, ref iPara, ref ich, ref tag, true);
				if (iSection > ihvoSectionStart || (iSection == ihvoSectionStart &&
					(iPara > ihvoParaStart || (iPara == ihvoParaStart && ich >= ichStart))) ||
					tag != (int)ScrSection.ScrSectionTags.kflidContent)
				{
					DeleteFootnotes(book, firstFootnoteToDelete, lastFootnoteToDelete,
						paraStart.Hvo, paraEnd.Hvo);
				}
			}

			// Remove any trailing portion of the start section that is selected
			IStText content = sectionStart.ContentOA;
			for (iPara = content.ParagraphsOS.Count - 1; iPara > ihvoParaStart; iPara--)
				content.ParagraphsOS.RemoveAt(iPara);

			ITsStrBldr startContentBldr = paraStart.Contents.UnderlyingTsString.GetBldr();
			startContentBldr.Replace(ichStart, startContentBldr.Length, string.Empty, null);

			// Copy any trailing portion (not selected) of the end section to the start section.
			int ihvoMoveStart;
			// Move whole ending paragraph of the selection if selection ends at the beginning
			// of the paragraph
			if (ichEnd == 0)
				ihvoMoveStart = ihvoParaEnd;
			else
				ihvoMoveStart = ihvoParaEnd + 1;
			if (ihvoMoveStart < sectionEnd.ContentOA.ParagraphsOS.Count)
				m_cache.MoveOwningSequence(sectionEnd.ContentOAHvo,
					(int)StText.StTextTags.kflidParagraphs, ihvoMoveStart,
					sectionEnd.ContentOA.ParagraphsOS.Count - 1, sectionStart.ContentOAHvo,
					(int)StText.StTextTags.kflidParagraphs,
					sectionStart.ContentOA.ParagraphsOS.Count);

			// Copy end of last paragraph of selection to end of first paragraph of selection
			if (ichEnd > 0)
			{
				AboutToDelete(helper, paraEnd.Hvo, sectionEnd.ContentOA.Hvo,
					(int)StText.StTextTags.kflidParagraphs, ihvoParaEnd, false);

				ITsStrBldr endContentBldr = paraEnd.Contents.UnderlyingTsString.GetBldr();
				endContentBldr.Replace(0, ichEnd, string.Empty, null);
				int curEnd = startContentBldr.Length;
				startContentBldr.ReplaceTsString(curEnd, curEnd, endContentBldr.GetString());

				paraStart.StyleRules = paraEnd.StyleRules;
			}
			paraStart.Contents.UnderlyingTsString = startContentBldr.GetString();

			// Remove whole sections between start and end of selection, including the
			// ending section since content we want from it has been moved to the
			// starting section.
			for (iSection = ihvoSectionEnd; iSection > ihvoSectionStart; iSection--)
				book.SectionsOS.RemoveAt(iSection);
			// if selection starts at beginning of section head remove the whole beginning
			// section
			if (helper.IchAnchor == 0 &&
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[1].tag ==
				(int)ScrSection.ScrSectionTags.kflidHeading)
			{
				book.SectionsOS.RemoveAt(ihvoSectionStart);
				//set the IP to the end of the previous section
				if (ihvoSectionStart > 0)
				{
					ihvoSectionStart--;
					IScrSection section = book.SectionsOS[ihvoSectionStart];
					ihvoParaStart = section.ContentOA.ParagraphsOS.Count - 1;
					StTxtPara para = (StTxtPara) section.ContentOA.ParagraphsOS[ihvoParaStart];
					ichStart = para.Contents.Length;
				}
			}
			// Set the insertion point.
			SetInsertionPoint(ihvoBook, ihvoSectionStart, ihvoParaStart, ichStart,
				helper.AssocPrev);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section heading paragraph on delete key being pressed at
		/// text boundary.
		/// </summary>
		/// <param name="helper"></param>
		/// <returns><c>true</c> if we handled the deletion, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleDeleteBeforeEmptySectionHeadParagraph(SelectionHelper helper)
		{
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			ScrBook book = new ScrBook(m_cache, ((ITeView)Control).LocationTracker.GetBookHvo(
				helper, SelectionHelper.SelLimitType.Top));

			// Delete problem deletion will be at end of last paragraph of content,
			// need to check following section head (if available)

			// Get next section of book.
			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				helper, SelectionHelper.SelLimitType.Top);

			bool positionAtEnd = false;
			if (!InSectionHead)
			{
				iSection++;
				if (iSection > book.SectionsOS.Count - 1)
					return false;
				positionAtEnd = true;
			}
			else if (iSection == 0)
				return false;

			IScrSection section = book.SectionsOS[iSection];

			if (section.HeadingOA == null || section.HeadingOA.ParagraphsOS.Count == 0)
			{
				// don't crash if database is corrupt - allow user to merge the two
				// sections (TE-4869)
				return MergeWithPreviousSectionIfInSameContext(helper, book,
					section, iSection, positionAtEnd);
			}
			else
			{
				// if there are more than one paragraph in heading, check to see if first
				// paragraph can be deleted.
				if (section.HeadingOA.ParagraphsOS.Count > 1)
				{
					StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
					if (para.Contents.Length == 0)
					{
						section.HeadingOA.ParagraphsOS.RemoveAt(0);
						return true;
					}
				}
				// If we are at end of content before an empty section head,
				// we should merge sections.
				else
				{
					StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
					if (para.Contents.Length == 0)
					{
						return MergeWithPreviousSectionIfInSameContext(helper, book,
							section, iSection, positionAtEnd);
					}
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section heading paragraph on backspace key being pressed
		/// at text boundary.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <returns><c>true</c> if we handled the deletion, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleBackspaceAfterEmptySectionHeadParagraph(SelectionHelper helper)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			ScrBook book = new ScrBook(m_cache, tracker.GetBookHvo(helper,
				SelectionHelper.SelLimitType.Top));
			ScrSection section = new ScrSection(m_cache, tracker.GetSectionHvo(helper,
				SelectionHelper.SelLimitType.Top));
			int iSection = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			int cParas = section.HeadingOA != null ? section.HeadingOA.ParagraphsOS.Count : 0;
			if (cParas > 0)
			{
				// If we are the beginning of content before a multi-paragraph heading
				// and the last paragraph is empty, we delete the last paragraph of
				// the heading.
				if (cParas > 1)
				{
					StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[cParas - 1];
					if (para.Contents.Length == 0)
					{
						section.HeadingOA.ParagraphsOS.RemoveAt(cParas - 1);
						return true;
					}
				}
				// If we are at beginning of content before an empty section head and
				// not in the first section, we should merge sections.
				else if (iSection > 0)
				{
					StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[cParas - 1];
					if (para.Contents.Length == 0)
					{
						return MergeWithPreviousSectionIfInSameContext(helper, book, section,
							iSection, false);
					}
				}
				return false;	// heading is not empty
			}

			// don't crash if database is corrupt - allow user to merge with
			// previous section (TE-4869)
			if (iSection > 0)
			{
				return MergeWithPreviousSectionIfInSameContext(helper, book,
					section, iSection, false);
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the content of the current section with the previous section if in the
		/// sections have the same context.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <param name="book">The current book.</param>
		/// <param name="section">The current section.</param>
		/// <param name="iSection">The index of the current section.</param>
		/// <param name="fPositionAtEnd">If true position of Selection is placed at end of
		/// paragraph, else at the beginning.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool MergeWithPreviousSectionIfInSameContext(SelectionHelper helper, ScrBook book,
			IScrSection section, int iSection, bool fPositionAtEnd)
		{
			IScrSection prevSection = book.SectionsOS[iSection - 1];
			if (SectionsHaveSameContext(prevSection, section))
			{
				MergeContentWithPreviousSection(helper, book, section, iSection,
					fPositionAtEnd);
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section content paragraph on delete key being pressed at
		/// text boundary.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleDeleteBeforeEmptySectionContentParagraph(SelectionHelper helper)
		{
			// delete problem deletion will occur at end of section heading
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			ScrBook book = new ScrBook(m_cache, tracker.GetBookHvo(helper,
				SelectionHelper.SelLimitType.Top));
			int iSection = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			IScrSection section = book.SectionsOS[iSection];
			int cParas = section.ContentOA != null ? section.ContentOA.ParagraphsOS.Count : 0;
			if (cParas > 0)
			{
				// If we are the end of heading before a multi-paragraph content
				// and the first paragraph is empty, we delete the first paragraph of
				// the content.
				if (cParas > 1)
				{
					StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
					if (para.Contents.Length == 0)
					{
						section.ContentOA.ParagraphsOS.RemoveAt(0);
						return true;
					}
				}
				// If we are at end of section heading or beginning of section content
				// and not in the last section, we should merge sections.
				else
				{
					if (iSection == book.SectionsOS.Count - 1)
						return false;
					StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
					if (para.Contents.Length == 0)
					{
						return MergeWithFollowingSectionIfInSameContext(helper, book, iSection,
							section, true);
					}
				}
			}
			else
			{
				return MergeWithFollowingSectionIfInSameContext(helper, book, iSection,
					section, true);
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the section the with following section if they are in the same context.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <param name="book">The book.</param>
		/// <param name="iSection">The index of the current section.</param>
		/// <param name="section">The current section.</param>
		/// <param name="fDeleteForward">if set to <c>true</c> the user pressed the delete key,
		/// otherwise the backspace key.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool MergeWithFollowingSectionIfInSameContext(SelectionHelper helper, ScrBook book,
			int iSection, IScrSection section, bool fDeleteForward)
		{
			IScrSection nextSection = book.SectionsOS[iSection + 1];
			// Merge the sections if they have the same context.
			if (SectionsHaveSameContext(nextSection, section))
			{
				bool wasInHeading = InSectionHead;
				int lastParaIndex = section.HeadingOA.ParagraphsOS.Count - 1;
				MergeHeadingWithFollowingSection(helper, book, section, iSection);
				// Now we have to re-establish a selection
				if (wasInHeading && fDeleteForward)
				{
					// delete key was pressed at end of section head, place IP at
					// end of what was the original heading
					section = book.SectionsOS[iSection];
					StTxtPara lastPara =
						(StTxtPara)section.HeadingOA.ParagraphsOS[lastParaIndex];
					SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
						BookFilter.GetBookIndex(book.Hvo), iSection, lastParaIndex);
					CurrentSelection.IchAnchor = lastPara.Contents.Length;
					CurrentSelection.SetSelection(true);
				}
				else
				{
					// delete key was pressed in empty section content, place IP
					// at beginning of what was following section heading
					SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
						BookFilter.GetBookIndex(book.Hvo), iSection, lastParaIndex + 1);
				}
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles deletion of empty section content paragraph on backspace key being pressed
		/// at text boundary.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <returns><c>true</c> if we merged the sections, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleBackspaceAfterEmptyContentParagraph(SelectionHelper helper)
		{
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			ScrBook book = new ScrBook(m_cache, tracker.GetBookHvo(helper, SelectionHelper.SelLimitType.Top));
			int iSection = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			if (iSection == 0)
				return false;
			IScrSection prevSection = book.SectionsOS[iSection - 1];
			int cParas = prevSection.ContentOA != null ? prevSection.ContentOA.ParagraphsOS.Count : 0;
			if (cParas > 0)
			{
				// If we are the beginning of heading before a multi-paragraph content
				// and the last paragraph is empty, we delete the last paragraph of
				// the content.
				if (cParas > 1)
				{
					StTxtPara para = (StTxtPara)prevSection.ContentOA.ParagraphsOS[cParas - 1];
					if (para.Contents.Length == 0)
					{
						prevSection.ContentOA.ParagraphsOS.RemoveAt(cParas - 1);
						return true;
					}
				}
				// If we are at beginning of heading before an empty section content and
				// not in the first section, we should merge sections.
				else
				{
					StTxtPara para = (StTxtPara)prevSection.ContentOA.ParagraphsOS[cParas - 1];
					if (para.Contents.Length == 0)
					{
						return MergeWithFollowingSectionIfInSameContext(helper, book,
							iSection - 1, prevSection, false);
					}
				}
			}
			else
			{
				return MergeWithFollowingSectionIfInSameContext(helper, book, iSection - 1,
					prevSection, false);
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a section heading.
		/// </summary>
		/// <param name="helper">Selection information</param>
		/// <param name="allowHeadingText">if <code>true</code> section head will be deleted
		/// even if heading contains text</param>
		/// <param name="positionAtEnd">if <code>true</code> IP will be at end of paragraph
		/// before top point of current selection</param>
		/// <returns><code>true</code> if deletion was done</returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteSectionHead(SelectionHelper helper, bool allowHeadingText,
			bool positionAtEnd)
		{
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			if (helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) !=
				tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent))
				return false;
			int tag = helper.GetTextPropId(SelectionHelper.SelLimitType.Top);
			if (tag != (int)StTxtPara.StTxtParaTags.kflidContents &&
				tag != SimpleRootSite.kTagUserPrompt)
			{
				// Currently this is the only possible leaf property in draft view;
				// if this changes somehow, we'll probably need to enhance this code.
				Debug.Assert(false);
				return false;
			}
			int hvoBook = tracker.GetBookHvo(helper, SelectionHelper.SelLimitType.Top);
			if (hvoBook < 0)
			{
				Debug.Assert(false);
				return false;
			}
			ScrBook book = new ScrBook(m_cache, hvoBook);

			int iSection = tracker.GetSectionIndexInBook(helper, SelectionHelper.SelLimitType.Top);
			if (iSection < 0)
			{
				// Something changed catastrophically. Book has something in it other than sections.
				Debug.Assert(false);
				return false;
			}
			IScrSection section = book.SectionsOS[iSection];

			if (helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].tag !=
				(int)StText.StTextTags.kflidParagraphs)
			{
				// Something changed catastrophically. StText has something in it other than paragraphs!
				Debug.Assert(false);
				return false;
			}
			// For now we just handle the heading.
			if (helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[1].tag !=
				(int)ScrSection.ScrSectionTags.kflidHeading)
			{
				// Add code here if desired to handle bsp/del at boundary of body.
				return false;
			}

			// OK, we're dealing with a change at the boundary of a section heading
			// (in a paragraph of an StText that is the heading of an ScrSection in an ScrBook).
			IStText text = section.HeadingOA;
			int ihvoPara = helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo;
			IStTxtPara para = (IStTxtPara)(text.ParagraphsOS[ihvoPara]);

			if (!allowHeadingText && para.Contents.Length > 0)
			{
				// The current heading paragraph has something in it!
				// For now we won't try to handle this.
				// (The problem is knowing what to do with the undeleted header text...
				// make it part of the previous section? The previous header? Delete it?)
				return false;
			}
			if (text.ParagraphsOS.Count != 1)
			{
				// Backspace at start or delete at end of non-empty section, and the paragraph is
				// empty. Do nothing.
				return false;

				// Other options:
				// - delete the section? But what do we do with the rest of its heading?
				// - delete the empty paragraph? That's easy...just
				//		text.ParagraphsOS.RemoveAt(ihvoPara);
				// But where do we put the IP afterwards? At the end of the previous body,
				// for bsp, or the start of our own body, for del? Or keep it in the heading?
			}
			// OK, we're in a completely empty section heading.
			// If it's the very first section of the book, we can't join it to the previous
			// section, so do nothing. (May eventually enhance to join the two books...
			// perhaps after asking for confirmation!)
			if (iSection == 0)
				return false;
			// Finally...we know we're going to merge the two sections.
			MergeContentWithPreviousSection(helper, book, section, iSection, positionAtEnd);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges content of given section into the content of the previous section and then
		/// deletes the given section.
		/// </summary>
		/// <param name="helper"> </param>
		/// <param name="book"></param>
		/// <param name="section"></param>
		/// <param name="ihvoSection"></param>
		/// <param name="fPositionAtEnd">If true position of Selection is placed at end of
		/// paragraph, else at the beginning.</param>
		/// ------------------------------------------------------------------------------------
		private void MergeContentWithPreviousSection(SelectionHelper helper, ScrBook book,
			IScrSection section, int ihvoSection, bool fPositionAtEnd)
		{
			//REVIEW: Can the methods that call this be refactored
			//to use (a refactored?) ScrSection.MergeWithPreviousSection?
			//
			// Get the previous section and move the paragraphs.
			IScrSection sectionPrev = book.SectionsOS[ihvoSection - 1];
			IStText textPrev = sectionPrev.ContentOA;
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int iBook = tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top);
			int cparaPrev = 0;
			if (textPrev == null)
			{
				// Prevent crash when dealing with corrupt database (TE-4869)
				// Since the previous section doesn't have a text, we simply move the entire text
				// object from the current section to the previous section.
				m_cache.ChangeOwner(section.ContentOAHvo, sectionPrev.Hvo,
					(int)ScrSection.ScrSectionTags.kflidContent);
			}
			else
			{
				cparaPrev = textPrev.ParagraphsOS.Count;
				IStText textOldContents = section.ContentOA;
				m_cache.MoveOwningSequence(textOldContents.Hvo, (int)StText.StTextTags.kflidParagraphs,
					0, textOldContents.ParagraphsOS.Count - 1,
					textPrev.Hvo, (int)StText.StTextTags.kflidParagraphs, cparaPrev);
			}
			// protected for some reason...textPrev.ParagraphsOS.Append(text.ParagraphsOS.HvoArray);
			book.SectionsOS.RemoveAt(ihvoSection);
			// Now we have to re-establish a selection. Whatever happens, it will be in the
			// same book as before, and the previous section, and in the body.
			if (InSectionHead || !fPositionAtEnd)
			{
				tracker.SetBookAndSection(helper, SelectionHelper.SelLimitType.Top, iBook,
					ihvoSection - 1);
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[1].tag =
					(int)ScrSection.ScrSectionTags.kflidContent;
			}
			Debug.Assert(helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[1].tag ==
				(int)ScrSection.ScrSectionTags.kflidContent);

			if (fPositionAtEnd)
			{
				// we want selection at end of last paragraph of old previous section.
				// (That is, at the end of paragraph cparaPrev - 1.)
				Debug.Assert(cparaPrev > 0);
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo = cparaPrev - 1;
				StTxtPara paraPrev = (StTxtPara)(textPrev.ParagraphsOS[cparaPrev - 1]);

				int cchParaPrev = paraPrev.Contents.Length;
				helper.IchAnchor = cchParaPrev;
				helper.IchEnd = cchParaPrev;
				helper.AssocPrev = true;
			}
			else
			{
				// want selection at start of old first paragraph of deleted section.
				// (That is, at the start of paragraph cparaPrev.)
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[0].ihvo = cparaPrev;
				helper.IchAnchor = 0;
				helper.IchEnd = 0;
				helper.AssocPrev = false;
			}
			helper.SetLevelInfo(SelectionHelper.SelLimitType.Bottom,
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top));
			helper.SetSelection(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges heading of given section into the heading of the follwing section and then
		/// deletes the given section. We assume that the content of the current section is
		/// empty.
		/// </summary>
		/// <param name="helper"> </param>
		/// <param name="book"></param>
		/// <param name="section"></param>
		/// <param name="ihvoSection"></param>
		/// ------------------------------------------------------------------------------------
		private void MergeHeadingWithFollowingSection(SelectionHelper helper, ScrBook book,
			IScrSection section, int ihvoSection)
		{
			// Get the following section and move the paragraphs.
			IScrSection sectionNext = book.SectionsOS[ihvoSection + 1];
			IStText textNext = sectionNext.HeadingOA;
			if (textNext == null)
			{
				// Prevent crash when dealing with corrupt database (TE-4869)
				// Since the next section doesn't have a heading text, we simply move the entire
				// text object from the current section head to the next section.
				m_cache.ChangeOwner(section.HeadingOAHvo, sectionNext.Hvo,
					(int)ScrSection.ScrSectionTags.kflidHeading);
			}
			else
			{
				IStText textOldHeading = section.HeadingOA;
				int cOldHeadingParas = textOldHeading.ParagraphsOS.Count;
				m_cache.MoveOwningSequence(textOldHeading.Hvo, (int)StText.StTextTags.kflidParagraphs,
					0, cOldHeadingParas - 1,
					textNext.Hvo, (int)StText.StTextTags.kflidParagraphs, 0);
			}
			// protected for some reason...textNext.ParagraphsOS.Append(text.ParagraphsOS.HvoArray);
			book.SectionsOS.RemoveAt(ihvoSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the beginning and the end of the selection are within the same type
		/// of paragraph (e.g. all in intro material or all in scripture paragraphs).
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="topInfo"></param>
		/// <param name="bottomInfo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool SectionsHaveSameContext(SelectionHelper helper, SelLevInfo[] topInfo,
			SelLevInfo[] bottomInfo)
		{
			if (topInfo.Length != bottomInfo.Length)
				return false;

			// Make sure the beginning and end of the selection are in the same book.
			ILocationTracker tracker = ((ITeView)Control).LocationTracker;
			int iBook = tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Top);
			Debug.Assert(iBook == tracker.GetBookIndex(helper, SelectionHelper.SelLimitType.Bottom));
			Debug.Assert(topInfo.Length ==
				tracker.GetLevelCount((int)ScrSection.ScrSectionTags.kflidContent));

			ScrBook book = BookFilter.GetBook(iBook);
			IScrSection section1 = book.SectionsOS[tracker.GetSectionIndexInBook(helper,
				SelectionHelper.SelLimitType.Top)];
			IScrSection section2 = book.SectionsOS[tracker.GetSectionIndexInBook(helper,
				SelectionHelper.SelLimitType.Bottom)];

			return SectionsHaveSameContext(section1, section2);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the beginning and the end of the selection are within the same type
		/// of paragraph (e.g. all in intro material or all in scripture paragraphs).
		/// </summary>
		/// <param name="section1">The section1.</param>
		/// <param name="section2">The section2.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------------
		private bool SectionsHaveSameContext(IScrSection section1, IScrSection section2)
		{
			// Must both be true or both be false: negated exclusive OR.
			return !(section1.IsIntro ^ section2.IsIntro);
		}
		#endregion

		#region Book Deletion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the selected book.
		/// </summary>
		/// <param name="iBook">Index of book in the filter</param>
		/// ------------------------------------------------------------------------------------
		public virtual void RemoveBook(int iBook)
		{
			CheckDisposed();

			IVwRootSite rootSite = Callbacks.EditedRootBox.Site;
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRemoveBook", out undo, out redo);
			using (new UndoTaskHelper(rootSite, undo, redo, true))
			using (new DataUpdateMonitor(Control, m_cache.MainCacheAccessor, rootSite, "RemoveBook"))
			{
				m_cache.ActionHandlerAccessor.AddAction(new UndoRemoveBookAction(m_cache, BookFilter,
					BookFilter.GetBook(iBook).CanonicalNum));

				m_cache.ActionHandlerAccessor.AddAction(new UndoWithSyncAction(m_cache,
					SyncMsg.ksyncReloadScriptureControl));

				int realBook = BookFilter.GetUnfilteredIndex(iBook);

				// Remove from filter first - filter may update automatically when real book is removed
				int bookHvo = BookFilter.GetBook(iBook).Hvo;
				m_scr.ScriptureBooksOS.RemoveAt(realBook);

				// Add another action to update the scripture control so it will get done
				// at both ends of the undo/redo
				m_cache.ActionHandlerAccessor.AddAction(new UndoWithSyncAction(m_cache,
					SyncMsg.ksyncReloadScriptureControl));

				// During tests main window may not be available.
				if (FwApp.App != null)
				{
					FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncScriptureDeleteBook, bookHvo,
						(int)Scripture.ScriptureTags.kflidScriptureBooks), m_cache);
					FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncReloadScriptureControl, 0, 0), m_cache);
				}
				else
				{
					IRootSite site = Callbacks.EditedRootBox.Site as IRootSite;
					if (site != null)
						site.RefreshDisplay();
				}

				// Reset IP if any books are left.
				if (BookFilter.BookCount > 0)
				{
					// need to adjust index if last book was deleted.
					iBook = Math.Min(iBook, BookFilter.BookCount - 1);
					// put IP at beginning of book title
					SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, iBook, 0);
					Control.Focus();
				}
			}
		}
		#endregion

		#region Picture Deletion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the currently selected picture, if there is one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeletePicture()
		{
			CheckDisposed();

			if (IsPictureReallySelected)
			{
				string undo;
				string redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidDeletePicture", out undo, out redo);
				using (new UndoTaskHelper(Callbacks.EditedRootBox.Site, undo, redo, false))
				{
					DeletePicture(CurrentSelection, CurrentSelection.LevelInfo[0].hvo);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the picture specified by the hvo.
		/// </summary>
		/// <param name="helper">Selection containing the picture.</param>
		/// <param name="hvoPic">hvo of picture.</param>
		/// ------------------------------------------------------------------------------------
		protected void DeletePicture(SelectionHelper helper, int hvoPic)
		{
			int paraHvo = helper.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs).hvo;
			Debug.Assert(paraHvo != 0);

			int iBook, iSection;
			iBook = ((ITeView)Control).LocationTracker.GetBookIndex(helper,
				SelectionHelper.SelLimitType.Anchor);
			iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(helper,
				SelectionHelper.SelLimitType.Anchor);

			StTxtPara para = new StTxtPara(m_cache, paraHvo);

			// Find the ORC and delete it from the paragraph
			ITsString contents = para.Contents.UnderlyingTsString;
			int startOfRun = 0;
			for(int i = 0; i < contents.RunCount; i++)
			{
				string str = contents.get_Properties(i).GetStrPropValue(
					(int)FwTextPropType.ktptObjData);

				if (str != null)
				{
					Guid guid = MiscUtils.GetGuidFromObjData(str.Substring(1));
					int hvo = m_cache.GetIdFromGuid(guid);
					if (hvo == hvoPic)
					{
						ITsStrBldr bldr = contents.GetBldr();
						startOfRun = contents.get_MinOfRun(i);
						bldr.Replace(startOfRun, contents.get_LimOfRun(i),
							string.Empty, null);
						para.Contents.UnderlyingTsString = bldr.GetString();
						break;
					}
				}
			}

			// TODO (TE-4967): do a prop change that actually works.
			//			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
			//				para.Hvo, (int)StTxtPara.StTxtParaTags.kflidContents,
			//				startOfRun, 0, 1);
			if (FwApp.App != null)
				FwApp.App.RefreshAllViews(m_cache);
			m_cache.DeleteObject(hvoPic);

			// TODO (TimS): This code to create a selection in the paragraph the picture was
			// in probably won't work when deleting a picture in back translation
			// material (which isn't possible to insert yet).

			((ITeView)Control).LocationTracker.SetBookAndSection(helper,
				SelectionHelper.SelLimitType.Anchor, iBook, iSection);
			helper.RemoveLevel((int)StTxtPara.StTxtParaTags.kflidContents);

			if (Callbacks != null && Callbacks.EditedRootBox != null) // may not exist in tests.
			{
				try
				{
					MakeSimpleTextSelection(helper.LevelInfo,
						(int)StTxtPara.StTxtParaTags.kflidContents, startOfRun);
				}
				catch
				{
					// If we couldn't make the selection in the contents, it's probably because
					// we are in a user prompt, so try that instead.
					MakeSimpleTextSelection(helper.LevelInfo, (int)SimpleRootSite.kTagUserPrompt,
						startOfRun);
				}
				Callbacks.EditedRootBox.Site.ScrollSelectionIntoView(Callbacks.EditedRootBox.Selection,
					VwScrollSelOpts.kssoNearTop);
			}
		}
		#endregion

		#region Insert Chapter Number
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a chapter number at the current location
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertChapterNumber()
		{
			CheckDisposed();

			Debug.Assert(CanInsertNumberInElement);

			// If selection is a range, we reduce it to an IP (top)
			SelectionHelper selHelper =
				SelectionHelper.ReduceSelectionToIp(Callbacks.EditedRootBox.Site,
				SelectionHelper.SelLimitType.Top, false);

			if (selHelper == null)
				return;

			selHelper.SetSelection(false);

			IVwRootSite rootSite = Callbacks.EditedRootBox.Site;
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertChapterNumber", out undo, out redo);
			using (new UndoTaskHelper(rootSite, undo, redo, true))
			using (new DataUpdateMonitor(Control, m_cache.MainCacheAccessor, rootSite, "InsertChapterNumber"))
			{
				// get relevant information about the selection.
				ITsString tss;
				int ich;
				bool fAssocPrev;
				int hvoObj;
				int wsAlt; //the WS of the multiString alt, if selection is in a back translation
				int propTag;
				CurrentSelection.Selection.TextSelInfo(true, out tss, out ich, out fAssocPrev,
					out hvoObj, out propTag, out wsAlt);

				string chapterNumber = null; // String representation of chapter number to insert
				int ichLimNew;

				// Establish whether we are in a vernacular paragraph or a back translation,
				//  depending on the propTag of the given selection
				if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents)
				{
					// selection is in a vernacular paragraph
					Debug.Assert(wsAlt == 0, "wsAlt should be 0 for a bigString");
					wsAlt = 0; // Some code depends on this being zero to indicate we are in vernacular
					// Adjust the insertion position to the beginning of a word - not in the middle
					ich = MoveToWordBoundary(tss, ich);

					// Make a reference for the book and chapter + 1, validate it and get the
					// string equivalent.
					ScrReference curStartRef = CurrentStartRef;
					curStartRef.Chapter += 1;
					curStartRef.MakeValid();
					chapterNumber = m_scr.ConvertToString(curStartRef.Chapter);

					// If we are at the beginning of the first section, then use chapter 1.
					int paraIndex = CurrentSelection.GetLevelInfoForTag(
						(int)StText.StTextTags.kflidParagraphs).ihvo;
					if (paraIndex == 0 && ich == 0 && IsFirstScriptureSection())
						chapterNumber = m_scr.ConvertToString(1);

					ITsTextProps ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
						m_cache.DefaultVernWs);

					// Insert the chapter number into the tss, and update the cache
					ReplaceInParaOrBt(hvoObj, propTag, wsAlt, chapterNumber, ttp, ich, ich,
						ref tss, out ichLimNew);
				}
				else
				{
					if (propTag == (int)CmTranslation.CmTranslationTags.kflidTranslation)
					{
						// selection is in a back translation
						Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
					}
					else if (propTag == RootSite.kTagUserPrompt)
					{
						// selection is a user prompt
						// by TE design this happens only in a back translation
						int nVar; //dummy for out params
						// get writing system from zero-width space in run 0 of the user prompt
						wsAlt = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs,
							out nVar);
						Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
						Debug.Assert(
							tss.get_Properties(0).GetIntPropValues(SimpleRootSite.ktptUserPrompt, out nVar) == 1);

						// Replace the TextSelInfo with stuff we can use
						ITsStrFactory tssFactory = TsStrFactoryClass.Create();
						tss = tssFactory.MakeString(string.Empty, wsAlt);
						ich = 0; // This should already be the case
						propTag = (int)CmTranslation.CmTranslationTags.kflidTranslation;
					}
					else
						Debug.Assert(false, "Unexpected propTag");

					ich = MoveToWordBoundary(tss, ich);

					// Figure out the corresponding chapter number in the vernacular, if any.
					if (!InsertNextChapterNumberInBt(hvoObj, propTag, CurrentSelection,
						wsAlt, ich, ref tss, out ichLimNew))
					{
						MiscUtils.ErrorBeep(); // No verse number inserted or updated
						return;
					}
				}

				// set the cursor at the end of the chapter number
				if (CurrentSelection != null)
				{
					CurrentSelection.IchAnchor = ichLimNew;
					CurrentSelection.SetSelection(true);
				}

				// Issue property change event for inserted chapter number.
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoObj,
					StTxtPara.ktagVerseNumbers, 0, 1, 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the current section is the first section of the book that contains
		/// scripture.
		/// </summary>
		/// <returns>true if it is the first section with scripture</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsFirstScriptureSection()
		{
			CheckDisposed();

			ScrBook book = BookFilter.GetBook(BookIndex);
			// Look at all of the previous sections to see if any are scripture sections
			for (int i = SectionIndex - 1; i >= 0; i--)
			{
				if (!((ScrSection)book.SectionsOS[i]).IsIntro)
					return false;
			}
			return true;
		}
		#endregion

		#region Insert Verse Number
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the InsertVerseNumbers request.
		/// </summary>
		/// <param name="currentStateEnabled">current state of the insert verse numbers
		/// mode</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessInsertVerseNumbers(bool currentStateEnabled)
		{
			CheckDisposed();

			if (currentStateEnabled)
				EndInsertVerseNumbers();
			else
			{
				// Enter InsertVerseNumbers mode.
				m_restoreCursor = DefaultCursor;
				DefaultCursor = TeResourceHelper.InsertVerseCursor;
				// don't re-add a message filter if verse number mode was already on
				if (!InsertVerseActive)
				{
					m_InsertVerseMessageFilter = new InsertVerseMessageFilter(Control, this);
					Application.AddMessageFilter(m_InsertVerseMessageFilter);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the inserting verse numbers mode
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndInsertVerseNumbers()
		{
			CheckDisposed();

			DefaultCursor = m_restoreCursor;
			Application.RemoveMessageFilter(m_InsertVerseMessageFilter);
			m_InsertVerseMessageFilter = null;
			SelectionChanged(Callbacks.EditedRootBox, Callbacks.EditedRootBox.Selection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert verse number at the current insertion point, with undo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InsertVerseNumber()
		{
			CheckDisposed();

			// If selection is a range, we reduce it to an IP (top)
			SelectionHelper selHelper =
				SelectionHelper.ReduceSelectionToIp(Callbacks.EditedRootBox.Site,
				SelectionHelper.SelLimitType.Top, false);

			if (selHelper == null)
			{
				MiscUtils.ErrorBeep();
				return;
			}
			IVwSelection vwselNew = selHelper.SetSelection(false);

			// If selection is not in ScrSection Content (vern or BT), quit
			if (!CanInsertNumberInElement || vwselNew == null)
			{
				MiscUtils.ErrorBeep();
				return;
			}

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertVerseNumber", out undo, out redo);
			using(UndoTaskHelper undoTaskHelper =
					  new UndoTaskHelper(Callbacks.EditedRootBox.Site, undo, redo, true))
			{
				InsertVerseNumber(selHelper);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Low-level implementation of insert verse number.
		/// </summary>
		/// <param name="selHelper">the given SelectionHelper</param>
		/// ------------------------------------------------------------------------------------
		public void InsertVerseNumber(SelectionHelper selHelper)
		{
			CheckDisposed();
			Debug.Assert(selHelper != null);
			Debug.Assert(!selHelper.IsRange);

			// Get the details about the current selection
			int ichSelOrig; //the character offset of the selection in the ITsString
			int hvoObj; //the id of the object the selection is in (StTxtPara or CmTranslation)
			int propTag; //property tag of object
			ITsString tssSel; //ITsString containing the selection
			int wsAlt; //the WS of the multiString alt, if selection is in a back translation
			ichSelOrig = GetSelectionInfo(selHelper, out hvoObj, out propTag, out tssSel, out wsAlt);

			// If we're at the start of a paragraph and the first run is a chapter number,
			// we need to jump past it to insert the verse number otherwise it will insert
			// it before the chapter number, which for some reason feels brain dead.
			if (ichSelOrig == 0)
			{
				ITsTextProps ttp = tssSel.get_Properties(0);
				if (ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) ==
					ScrStyleNames.ChapterNumber)
				{
					ichSelOrig = tssSel.get_LimOfRun(0);
				}
			}

			// Adjust the insertion position to the beginning of a word - not in the middle
			//  (may move to an existing verse number too)
			int ichWord = MoveToWordBoundary(tssSel, ichSelOrig);

			//			TomB and MarkB have decided we won't do this, at least for now
			//			// If the start of the Bt does not match the vernacular, adjust it if required.
			//			if (ichWord > 0)
			//				cInsDel = SetVerseAtStartOfBtIfNeeded();
			//			ichWord += cInsDel; //adjust

			// some key variables set by Update or Insert methods, etc
			string sVerseNumIns = null; // will hold the verse number string we inserted; could be
			//   a simple number, a verse bridge, or the end number added to a bridge
			string sChapterNumIns = null; // will hold chapter number string inserted or null if none
			int ichLimIns = -1; //will hold the end of the new chapter/verse numbers we update or insert

			// Is ichWord in or next to a verse number? (if so, get its ich range)
			bool fCheckForChapter = (wsAlt != 0); //check for chapter in BT only
			int ichMin; // min of the verse number run, if we are on one
			int ichLim; // lim of the verse number run, if we are on one
			bool fFoundExistingRef =
				InReference(tssSel, ichWord, fCheckForChapter, out ichMin, out ichLim);

			// If we moved the selection forward (over spaces or punctuation) to an
			//  existing verse number ...
			if (fFoundExistingRef && (ichSelOrig < ichWord))
			{
				//Attempt to insert a verse number at the IP, if one is missing there.
				// if selection is in vernacular...
				if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents)
				{
					// Insert missing verse number in vernacular
					InsertMissingVerseNumberInVern(hvoObj, propTag, selHelper, ichSelOrig,
						ichWord, ref tssSel, out sVerseNumIns, out ichLimIns);
				}
				//else
				//{
				//    InsertMissingVerseNumberInBt(hvoObj, propTag, selHelper, wsAlt,
				//        ichSelOrig, ichWord, ref tssSel, ref ichLim,
				//        out sVerseNumIns, out sChapterNumIns);
				//}
				// if a verse number was not inserted, sVerseNumIns is null
			}

			// If no verse number inserted yet...
			if (sVerseNumIns == null)
			{
				if (fFoundExistingRef)
				{
					//We must update the existing verse number at ichWord
					// is selection in vern or BT?
					if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents)
					{
						// Update verse number in vernacular
						UpdateExistingVerseNumberInVern(hvoObj, propTag, selHelper,
							ichMin, ichLim, ref tssSel, out sVerseNumIns, out ichLimIns);
					}
					else
					{
						//Update verse number in back translation
						UpdateExistingVerseNumberInBt(hvoObj, propTag, selHelper, wsAlt,
							ichMin, ichLim, ref tssSel, out sVerseNumIns, out sChapterNumIns,
							out ichLimIns);
					}
				}
				else
				{
					// We're NOT on an existing verse number, so insert the next appropriate one.
					// is selection in vern or BT?
					if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents)
					{
						InsertNextVerseNumberInVern(hvoObj, propTag, selHelper, ichWord,
							ref tssSel, out sVerseNumIns, out ichLimIns);
					}
					else
					{
						InsertNextVerseNumberInBt(hvoObj, propTag, selHelper, wsAlt, ichWord,
							ref tssSel, out	sVerseNumIns, out sChapterNumIns, out ichLimIns);
						selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
							(int)CmTranslation.CmTranslationTags.kflidTranslation);
						selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
							(int)CmTranslation.CmTranslationTags.kflidTranslation);
					}
				}
			}

			if (sVerseNumIns == null)
			{
				MiscUtils.ErrorBeep(); // No verse number inserted or updated
				return;
			}

			// set new IP behind the verse number
			selHelper.IchAnchor = ichLimIns;
			selHelper.IchEnd = ichLimIns;
			selHelper.AssocPrev = true;
			selHelper.SetSelection(true);

			// Remove any duplicate chapter/verse numbers following the new verse number.
			RemoveDuplicateVerseNumbers(hvoObj, propTag, tssSel, wsAlt, sChapterNumIns,
				sVerseNumIns, ichLimIns);

			// Issue property change event for inserted verse.
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoObj,
				StTxtPara.ktagVerseNumbers, 0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get information about the selection including the selection, the property tag,
		/// and the writing system.
		/// </summary>
		/// <param name="selHelper">selection helper. This is intended to be called with a
		/// selection helper which represents an insertion point, but if this is a range
		/// selection, the information returned will be for the anchor point.
		/// </param>
		/// <param name="hvoObj">out: the id of the object the selection is in (StTxtPara or
		/// CmTranslation)</param>
		/// <param name="propTag">out: tag/flid of the field that contains the TsString (e.g.,
		/// for vernacular text, this will be StTxtPara.StTxtParaTags.kflidContents)</param>
		/// <param name="tssSel">out: ITsString containing selection (this is the full string,
		/// not just the selected portion)</param>
		/// <param name="wsAlt">out: the WS id of the multiString alt, if selection is in a back
		/// translation; otherwise 0</param>
		/// <returns>the character offset (in the ITsString) represented by the anchor of the
		/// given selection helper.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private int GetSelectionInfo(SelectionHelper selHelper, out int hvoObj, out int propTag,
			out ITsString tssSel, out int wsAlt)
		{
			Debug.Assert(selHelper != null);
			Debug.Assert(selHelper.Selection != null);

			// get relevant information about the selection.
			int ichSel;
			bool fAssocPrev;
			selHelper.Selection.TextSelInfo(true, out tssSel, out ichSel, out fAssocPrev, out hvoObj,
				out propTag, out wsAlt);

			// Establish whether we are in a vernacular paragraph or a back translation,
			//  depending on the propTag of the given selection
			if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents)
			{
				// selection is in a vernacular paragraph
				Debug.Assert(wsAlt == 0, "wsAlt should be 0 for a bigString");
				wsAlt = 0; // actually, some code depends on this being zero to indicate we are in vernacular
			}
			else
			{
				if (propTag == (int)CmTranslation.CmTranslationTags.kflidTranslation)
				{
					// selection is in a back translation
					Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
				}
				else if (propTag == RootSite.kTagUserPrompt)
				{
					// selection is a user prompt
					// by TE design this happens only in a back translation
					int nVar; //dummy for out params
					// get writing system from zero-width space in run 0 of the user prompt
					wsAlt = tssSel.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs,
						out nVar);
					Debug.Assert(wsAlt > 0, "wsAlt should be a valid WS for Translation multiBigString alt");
					Debug.Assert(
						tssSel.get_Properties(0).GetIntPropValues(SimpleRootSite.ktptUserPrompt, out nVar) == 1);

					// Replace the TextSelInfo with stuff we can use
					ITsStrFactory tssFactory = TsStrFactoryClass.Create();
					tssSel = tssFactory.MakeString(string.Empty, wsAlt);
					ichSel = 0; // This should already be the case
					propTag = (int)CmTranslation.CmTranslationTags.kflidTranslation;
				}
				else
					Debug.Assert(false, "Unexpected propTag");
			}
			return ichSel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an IP selection in a vernacular paragraph, and a following verse number run,
		/// determine if a verse number is missing and if so insert it at the IP.
		/// </summary>
		/// <param name="hvoObj">The id of the paragraph being modified</param>
		/// <param name="propTag">The flid (i.e. Contents)</param>
		/// <param name="selHelper">The selection helper, the IP where the insert verse command
		/// was made</param>
		/// <param name="ichIp">The character position of the selHelper</param>
		/// <param name="ichMinNextVerse">The character offset at the beginning of the following
		/// verse number run</param>
		/// <param name="tss">The given string, in which we will update the verse number</param>
		/// <param name="sVerseNumIns">output: String representation of the new verse number
		/// inserted, or null if none inserted</param>
		/// <param name="ichLimInserted">output: set to the end of the inserted verse number
		/// run, or -1 if none inserted </param>
		/// ------------------------------------------------------------------------------------
		private void InsertMissingVerseNumberInVern(int hvoObj, int propTag,
			SelectionHelper selHelper, int ichIp, int ichMinNextVerse,
			ref ITsString tss, out string sVerseNumIns, out int ichLimInserted)
		{
			sVerseNumIns = null;
			ichLimInserted = -1;

			// get info on the following verse number run
			string sVerseNumNext = tss.get_RunText(tss.get_RunAt(ichMinNextVerse));
			if (sVerseNumNext == null) //should never be null, but just in case, we check
				return;
			int startVerseNext = ScrReference.VerseToIntStart(sVerseNumNext);

			// Determine the appropriate verse number string to consider inserting at the IP
			sVerseNumIns = GetVernVerseNumberToInsert(tss, ichIp, selHelper, false);
			if (sVerseNumIns == null)
				return;
			int startVerseInsert = ScrReference.VerseToIntStart(sVerseNumIns);

			// Is number to insert less than the next existing verse number?
			if (startVerseInsert < startVerseNext)
			{
				// If so, the  number to insert is missing! We need to insert it at the IP.

				// Add space, if needed, before verse is inserted.
				int ichIns = ichIp;
				InsertSpaceIfNeeded(hvoObj, propTag, ichIns, 0, ref tss, ref ichIns);

				// Now insert the new verse number
				ITsTextProps ttpVerse = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
					m_cache.DefaultVernWs);
				ReplaceInParaOrBt(hvoObj, propTag, m_cache.DefaultVernWs, sVerseNumIns, ttpVerse,
					ichIns, ichIns, ref tss, out ichLimInserted);
			}
			else
				sVerseNumIns = null; // nothing inserted
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a space if the character before the ichIp is not white space and not in a
		/// chapter number run.
		/// This method is used when the insert is to be at the original IP, not when the ich
		/// was moved to a "word" boundary.
		/// </summary>
		/// <param name="hvoObj">The id of the paragraph or translation being modified</param>
		/// <param name="propTag">The flid (i.e. Contents or Translation)</param>
		/// <param name="ichIp">the ich into the tss at the IP</param>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt</param>
		/// <param name="tss">ref: the tss that will have a space added, if needed</param>
		/// <param name="ichLimInserted">ref: the end of the inserted verse number run;
		/// not changed if nothing inserted</param>
		/// ------------------------------------------------------------------------------------
		private void InsertSpaceIfNeeded(int hvoObj, int propTag, int ichIp, int wsAlt,
			ref ITsString tss, ref int ichLimInserted)
		{
			int ichIns = ichIp;
			if (ichIns > 0)
			{
				//If previous character is neither white space nor a chapter number...
				ITsTextProps ttp = tss.get_PropertiesAt(ichIns - 1);
				if (!UnicodeCharProps.get_IsSeparator(tss.Text[ichIns - 1]) &&
					!StStyle.IsStyle(ttp, ScrStyleNames.ChapterNumber))
				{
					//add a space.
					ReplaceInParaOrBt(hvoObj, propTag, wsAlt, " ", tss.get_PropertiesAt(ichIns),
						ichIns, ichIns, ref tss, out ichLimInserted);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a verse number run in a vernacular paragraph string, create or extend a verse
		/// bridge.
		/// </summary>
		/// <param name="hvoObj">The id of the paragraph being modified</param>
		/// <param name="propTag">The flid (i.e., Contents)</param>
		/// <param name="selHelper">The selection helper</param>
		/// <param name="ichMin">The character offset at the beginning of the verse number
		/// run</param>
		/// <param name="ichLim">the end of the verse number run </param>
		/// <param name="tss">The given string, in which we will update the verse number</param>
		/// <param name="sVerseNumIns">output: String representation of the new end number appended
		/// to verse bridge</param>
		/// <param name="ichLimIns">output: the end offset of the updated verse number run</param>
		/// <returns><c>true</c> if we updated a verse number/bridge; false if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool UpdateExistingVerseNumberInVern(int hvoObj, int propTag,
			SelectionHelper selHelper, int ichMin, int ichLim, ref ITsString tss,
			out string sVerseNumIns, out int ichLimIns)
		{
			Debug.Assert(ichMin < ichLim);
			ichLimIns = -1;

			// Determine the appropriate verse number string to insert here
			sVerseNumIns = GetVernVerseNumberToInsert(tss, ichMin, selHelper, true);
			if (sVerseNumIns == null)
			{
				MiscUtils.ErrorBeep();
				return false;
			}

			int iRun = tss.get_RunAt(ichMin);
			string sCurrVerseNumber = tss.get_RunText(iRun);
			string bridgeJoiner = m_scr.BridgeForWs(m_cache.DefaultVernWs);
			int bridgePos = sCurrVerseNumber.IndexOf(bridgeJoiner, StringComparison.Ordinal);
			int ichInsAt;
			ITsTextProps ttpVerse = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				m_cache.DefaultVernWs);
			if (bridgePos != -1)
			{
				// Add to existing verse bridge (add an additional pylon or two)
				ichInsAt = ichMin + bridgePos + bridgeJoiner.Length;
				ReplaceInParaOrBt(hvoObj, propTag, 0, sVerseNumIns, ttpVerse, ichInsAt, ichLim,
					ref tss, out ichLimIns);
			}
			else
			{
				// Create a verse bridge by adding to the existing number
				ichInsAt = ichLim;
				ReplaceInParaOrBt(hvoObj, propTag, 0, bridgeJoiner + sVerseNumIns, ttpVerse,
					ichInsAt, ichInsAt, ref tss, out ichLimIns);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the beginning and ending reference for a given paragraph.
		/// </summary>
		/// <param name="para">given paragraph</param>
		/// <param name="startRef">out: reference at start of paragraph</param>
		/// <param name="endRef">out: reference at end of paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void FindParaRefRange(ScrTxtPara para, out BCVRef startRef, out BCVRef endRef)
		{
			BCVRef notUsed;
			para.GetBCVRefAtPosition(0, false, out startRef, out notUsed);
			para.GetBCVRefAtPosition(para.Contents.Length,
				out notUsed, out endRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scan for previous chapter number run in a given ITsString.
		/// </summary>
		/// <param name="tss">given ITsString</param>
		/// <param name="ichMin">starting character index</param>
		/// <returns>value of previous chapter in tss, or 0 if no valid chapter found</returns>
		/// ------------------------------------------------------------------------------------
		private int GetPreviousChapter(ITsString tss, int ichMin)
		{
			for (int iRun = tss.get_RunAt(ichMin) - 1; iRun >= 0; iRun--)
			{
				ITsTextProps ttp = tss.get_Properties(iRun);
				if (ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) ==
					ScrStyleNames.ChapterNumber)
				{
					return ScrReference.ChapterToInt(tss.get_RunText(iRun));
				}
			}

			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a verse number run in the back translation string, try to locate the
		/// corresponding verse in the vernacular, and update the verse number in the BT.
		/// </summary>
		/// <param name="hvoObj">The id of the translation being modified</param>
		/// <param name="propTag">The flid (i.e. Translation)</param>
		/// <param name="selHelper">The selection helper</param>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ichMin">The character offset at the beginning of the BT verse number
		/// run</param>
		/// <param name="ichLim">the end of the verse number run </param>
		/// <param name="tssBt">The given BT string, in which we will update the verse number</param>
		/// <param name="sVerseNumIns">output: String representation of the new end number appended
		/// to verse bridge</param>
		/// <param name="sChapterNumIns">output: String containing the inserted chapter number,
		/// or null if no chapter number inserted</param>
		/// <param name="ichLimIns">output: the end offset of the updated chapter/verse numbers</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateExistingVerseNumberInBt(int hvoObj, int propTag,
			SelectionHelper selHelper, int wsAlt, int ichMin, int ichLim,
			ref ITsString tssBt, out string sVerseNumIns, out string sChapterNumIns,
			out int ichLimIns)
		{
			if (ichMin == 0)
			{
				// We are at start of BT para: get the first verse number in the vernacular.
				sVerseNumIns = GetBtVerseNumberFromVern(selHelper, tssBt, 0, wsAlt, false,
					out sChapterNumIns);
			}
			else
			{
				// We are within the BT para.
				// our preferred algorithm: get previous verse reference from back translation,
				// find its match in the vernacular, then get the next verse num following it
				sVerseNumIns = GetBtVerseNumberFromVern(selHelper, tssBt, ichMin-1, wsAlt, true,
					out sChapterNumIns);
				// an alternate algorithm: get the matching verse num in the vernacular
				//sVerseNumIns = GetBtVerseNumberFromVern(selHelper, tss, ichMin, wsAlt, false);
			}

			ReplaceRangeInBt(hvoObj, propTag, wsAlt, ichMin, ichLim,
				ref sChapterNumIns, ref sVerseNumIns, ref tssBt, out ichLimIns);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a vernacular paragraph string, insert the next verse number.
		/// </summary>
		/// <param name="hvoObj">The id of the paragraph being modified</param>
		/// <param name="propTag">The flid (i.e., Contents)</param>
		/// <param name="selHelper">The selection helper</param>
		/// <param name="ich">The character position at which to insert verse number</param>
		/// <param name="tss">The given string, in which we will insert the verse number</param>
		/// <param name="sVerseNumIns">The s verse num ins.</param>
		/// <param name="ichLim">Gets set to the end of the new verse number run</param>
		/// <returns>
		/// 	<c>true</c> if we inserted a verse number; <c>false</c> if not
		/// </returns>
		/// ------------------------------------------------------------------------------------------
		private bool InsertNextVerseNumberInVern(int hvoObj, int propTag,
			SelectionHelper selHelper, int ich,
			ref ITsString tss, out string sVerseNumIns, out int ichLim)
		{
			// Insert next verse in vernacular
			sVerseNumIns = GetVernVerseNumberToInsert(tss, ich, selHelper, false);

			if (sVerseNumIns == null)
			{
				// nothing to insert
				ichLim = ich;
				return false;
			}

			// Insert the verse number
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				m_cache.DefaultVernWs);
			ReplaceInParaOrBt(hvoObj, propTag, 0, sVerseNumIns, ttp, ich, ich,
				ref tss, out ichLim);

			return true;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a back translation string, try to locate the corresponding
		/// verse in the vernacular, and insert the verse number in the BT.
		/// </summary>
		/// <param name="hvoObj">The id of the translation being modified</param>
		/// <param name="propTag">The flid (i.e. Translation)</param>
		/// <param name="selHelper">The selection helper</param>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ich">The character position at which to insert verse number</param>
		/// <param name="tssBt">The given BT string, in which we will insert the verse number</param>
		/// <param name="sVerseNumIns">output: String containing the inserted verse number,
		/// or null if no verse number inserted</param>
		/// <param name="sChapterNumIns">output: String containing the inserted chapter number,
		/// or null if no chapter number inserted</param>
		/// <param name="ichLimIns">output: Gets set to the end of the new BT chapter/verse numbers
		/// inserted</param>
		/// <returns>
		/// 	<c>true</c> if we inserted a verse number/bridge; <c>false</c> if not
		/// </returns>
		/// ------------------------------------------------------------------------------------------
		private bool InsertNextVerseNumberInBt(int hvoObj, int propTag,
			SelectionHelper selHelper, int wsAlt, int ich,
			ref ITsString tssBt, out string sVerseNumIns, out string sChapterNumIns, out int ichLimIns)
		{
			// Get the corresponding verse number from the vernacular
			sVerseNumIns = GetBtVerseNumberFromVern(selHelper, tssBt, ich, wsAlt, true,
				out sChapterNumIns);

			ReplaceRangeInBt(hvoObj, propTag, wsAlt, ich, ich, ref sChapterNumIns, ref sVerseNumIns,
					ref tssBt, out ichLimIns);

			// Remove any chapter numbers not in the chapter range of the vernacular para that owns this BT
			ScrTxtPara vernPara = new ScrTxtPara(m_cache, selHelper.LevelInfo[1].hvo);
			BCVRef startRef;
			BCVRef endRef;
			FindParaRefRange(vernPara, out startRef, out endRef);
			CleanChapterInBtPara(hvoObj, wsAlt, ref tssBt, startRef, endRef);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a back translation string, try to locate the corresponding
		/// chapter in the vernacular and insert it in the BT
		/// (or update the existing reference in the BT).
		/// </summary>
		/// <param name="hvoObj">The id of the translation being modified</param>
		/// <param name="propTag">The flid (i.e. Translation)</param>
		/// <param name="selHelper">The selection helper</param>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ich">The character position at which to insert the chapter number</param>
		/// <param name="tssBt">ref: The given BT string</param>
		/// <param name="ichLimIns">output: set to the end of the new BT chapter number run</param>
		/// <returns><c>true</c> if a chapter number was inserted; <c>false</c> otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertNextChapterNumberInBt(int hvoObj, int propTag,
			SelectionHelper selHelper, int wsAlt, int ich, ref ITsString tssBt, out int ichLimIns)
		{
			ichLimIns = -1; //default output, if nothing inserted

			int ichMinRefBt, ichLimRefBt; // min and lim of the chapter/verse number runs, if we are on one

			// Are we in or next to a chapter/verse number?
			if (InReference(tssBt, ich, true, out ichMinRefBt, out ichLimRefBt))
			{
				// move our focus to the start of the chapter/verse numbers
				ich = ichMinRefBt;
			}

			// Get the corresponding chapter number from the vernacular
			string sVerseNumIns, sChapterNumIns;
			if (ich == 0)
			{
				// We are at the start of BT para--get the first chapter number in the
				//     vernacular para
				sVerseNumIns = GetBtVerseNumberFromVern(selHelper, tssBt, 0, wsAlt, false,
					out sChapterNumIns);
			}
			else
			{
				sVerseNumIns = GetBtVerseNumberFromVern(selHelper, tssBt, ich - 1, wsAlt, true,
					out sChapterNumIns);
				// an idea:
				//if no matching chapter number found for our current ich,
				// consider attempting to get a chapter number at the start of the vern, and
				// insert/update at the start of the BT
			}

			if (sChapterNumIns == null)
				return false;

			// Insert the chapter number into the tssBt, and update the cache
			int ichLimReplace = ichLimRefBt > 0 ? ichLimRefBt : ich;
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, wsAlt);
			ReplaceInParaOrBt(hvoObj, propTag, wsAlt, sChapterNumIns, ttp, ich, ichLimReplace,
				ref tssBt, out ichLimIns);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position and selection in a vernacular paragraph string, use the current
		/// reference and context to build the appropriate verse number string to insert at the
		/// ich position. Ususally the current verse reference + 1.
		/// </summary>
		/// <param name="tss">The string</param>
		/// <param name="ich">The character position we would like to insert a verse number at.
		/// This is either on the current selection, or moved to a nearby "word" boundary.
		/// </param>
		/// <param name="selHelper">The selection helper on or near the ich</param>
		/// <param name="fInVerse">true if ich is at an existing verse number</param>
		/// <returns>The verse number string to insert, or null if the verse number would be
		/// out of range</returns>
		/// ------------------------------------------------------------------------------------
		private string GetVernVerseNumberToInsert(ITsString tss, int ich, SelectionHelper selHelper,
			bool fInVerse)
		{

			//Get the BCV end ref at the current selection
			ScrReference refToInsert = CurrentEndRef;

			// Note that our ich may be at a "word" boundary near the selection.
			// If the ich is on a verse number, update refToInsert.Verse to its end value.
			if (fInVerse)
			{
				string sVerseRun = tss.get_RunText(tss.get_RunAt(ich));
				refToInsert.Verse = ScrReference.VerseToIntEnd(sVerseRun);
			}

			//  If verse number is already at the end of this chapter (or beyond), quit now!
			if (refToInsert.Verse >= refToInsert.LastVerse)
				return null;

			// Calculate the default next verse number: current verse ref + 1
			string sVerseNum = m_scr.ConvertToString(refToInsert.Verse + 1);

			// If we are already in a verse, we are done; this is the usual case for a verse num update
			if (fInVerse)
				return sVerseNum;

			// we are inserting in text at ich...

			// If we are at the beginning of the first scripture section, we insert verse 1.
			if (selHelper.LevelInfo[0].ihvo == 0 && ich == 0 &&
				IsFirstScriptureSection())
			{
				sVerseNum = m_scr.ConvertToString(1);
			}

			// If we directly follow a chapter number, we insert verse 1!
			if (ich > 0)
			{
				ITsTextProps ttpPrev = tss.get_PropertiesAt(ich - 1);
				if (ttpPrev.GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle)	==
					ScrStyleNames.ChapterNumber)
				{
					sVerseNum = m_scr.ConvertToString(1);
				}
			}
			return sVerseNum;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the next verse number or verse bridge from the vernacular
		/// for inserting (or updating) the back translation at or following ich.
		/// Also a string for the chapter number, if found in the vernacular.
		/// Result strings are in lower ASCII digits.
		/// </summary>
		/// <param name="selHelper">The selection helper in the BT para</param>
		/// <param name="tssBt">The string of the BT para</param>
		/// <param name="ichBt">The character offset in the BT paragraph</param>
		/// <param name="wsBt">The writing system of the BT</param>
		/// <param name="fGetNext">if false, we get the associated verse number from the
		/// vernacular); if true, we get the next verse number after the associated one</param>
		/// <param name="sChapNumberBt">the chapter number string in lower ASCII digits,
		/// if one was found just before the verse number we found</param>
		/// <returns>The verse number string in lower ASCII digits, or null if there is no
		/// corresponding verse number in the vernacular.</returns>
		/// ------------------------------------------------------------------------------------
		private string GetBtVerseNumberFromVern(SelectionHelper selHelper, ITsString tssBt,
			int ichBt, int wsBt, bool fGetNext, out string sChapNumberBt)
		{
			// Get our vernacular parent paragraph
			StTxtPara vernPara = new StTxtPara(m_cache, selHelper.LevelInfo[1].hvo);
			ITsString vernTss = vernPara.Contents.UnderlyingTsString;

			// Get matching verse and/or chapter strings from the vernacular
			sChapNumberBt = null;
			string sVerseRunVern; // verse number run we'll get from the vernacular
			string sChapterRunVern; // chapter number run we may get from the vernacular
			if (ichBt == 0)
			{
				// We are at the start of the BT para--get the first verse number in the
				//     vernacular para
				sVerseRunVern = GetNextVerseNumberRun (vernTss, 0, out sChapterRunVern);
			}
			else
			{
				// Get the desired verse number in the vernacular
				sVerseRunVern = GetMatchingVerseNumberFromVern(tssBt, ichBt, wsBt, vernTss,
					fGetNext, out sChapterRunVern);
			}

			// Adjust the vernacular verse/chapter run selections for special cases.
			int ichLimRef_IfRefAtVernStart = GetIchLimRef_IfIchInRefAtParaStart(vernTss, 0);
			int ichLimRef_IfIchInRefAtBtStart = GetIchLimRef_IfIchInRefAtParaStart(tssBt, ichBt);
			// -1 means paragraph doesn't begin with a chapter/verse number run, or the ich wasn't in it.

			// if the vernacular begins with a reference (chapter/verse) run...
			if (ichLimRef_IfRefAtVernStart != -1)
			{
				// get that first reference in vern
				string sDummy;
				string sFirstVerseVern = GetNextVerseNumberRun(vernTss, 0, out sDummy);
				// Design behavior: if we are in the middle of the BT, we don't want to insert
				// a verse number that's at the start of the vern, even if it matches our
				// current BCV reference in the BT. If it does match, we instead we want to get the
				// following verse number from the vernacular.

				// If the ichBt wasn't in chapter/verse reference at start of BT.
				// And, the vernacular paragraph begins with a reference that is the same as
				// the reference selected as a matching verse.
				// And our ichBt is not at the beginning of the BT para...
				if (ichLimRef_IfIchInRefAtBtStart == -1 && sFirstVerseVern == sVerseRunVern &&
					ichBt != 0)
				{
					// IP is not at the start of the back translation, so find the
					// next verse in the vernacular after the begining reference.
					sVerseRunVern = GetNextVerseNumberRun(vernTss, ichLimRef_IfRefAtVernStart,
						out sChapterRunVern);
				}
			}
			// else the vernacular does NOT begin with a reference
			else if (ichBt == 0 || ichLimRef_IfIchInRefAtBtStart != -1)
			{
				// Trying to insert (or update) a verse number at the start of a BT para when the
				// vernacular does not begin with a reference. This is illegal.
				sVerseRunVern = null;
				sChapterRunVern = null;
			}

			// Convert chapter number to lower ASCII for back translation
			if (sChapterRunVern != null)
			{
				try
				{
					int chapNum = ScrReference.ChapterToInt(sChapterRunVern);
					sChapNumberBt = chapNum.ToString();
				}
				catch (ArgumentException)
				{
					// ignore runs with invalid Chapter numbers
					sChapNumberBt = null;
				}
			}

			// Convert verse number to lower ASCII for back translation
			if (sVerseRunVern != null)
			{
				// convert verse number to lower ASCII
				int startVerse, endVerse;
				ScrReference.VerseToInt(sVerseRunVern, out startVerse, out endVerse);
				string nextVerseStringBT = startVerse.ToString();
				// TODO: If we support right to left languages in a back translation, then
				// we need to fix the bridge character to include an RTL mark on either side
				// of it.
				if (endVerse > startVerse)
					nextVerseStringBT += "-" + endVerse.ToString();

				return nextVerseStringBT;
			}

			// no verse number found to insert
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the paragraph starts with chapter/verse run and the ich is within one
		/// of these runs.
		/// </summary>
		/// <param name="tss">paragraph string</param>
		/// <param name="ich">character offset</param>
		/// <returns>ich after the reference or -1 if the paragraph does not begin with a
		/// chapter/verse run</returns>
		/// ------------------------------------------------------------------------------------
		private static int GetIchLimRef_IfIchInRefAtParaStart(ITsString tss, int ich)
		{
			int iRun = tss.get_RunAt(ich);
			ITsTextProps firstTtp = tss.get_Properties(0);
			ITsTextProps ttp = tss.get_PropertiesAt(ich);

			if ((StStyle.IsStyle(ttp, ScrStyleNames.VerseNumber) ||
				StStyle.IsStyle(ttp, ScrStyleNames.ChapterNumber)) && iRun == 0)
			{
				return GetLimOfReference(tss, iRun, ref ttp);
			}
			else if (StStyle.IsStyle(firstTtp, ScrStyleNames.ChapterNumber) &&
				StStyle.IsStyle(ttp, ScrStyleNames.VerseNumber) && iRun == 1)
			{
				// The first run is a chapter and the next is a verse number run.
				// Return the lim of the verse number (current) run.
				return tss.get_LimOfRun(iRun);
			}
			else if (iRun == 0 && IsBlank(tss, iRun))
			{
				// The first run contains only white space.
				// Ignore this run and check the following runs.
				if (tss.RunCount > 1)
				{
					ttp = tss.get_Properties(iRun + 1);
					if (StStyle.IsStyle(ttp, ScrStyleNames.VerseNumber) ||
						StStyle.IsStyle(ttp, ScrStyleNames.ChapterNumber))
					{
						return GetLimOfReference(tss, iRun + 1, ref ttp);
					}
				}
			}
			// Paragraph doesn't begin with a chapter/verse number run (or the ich
			// wasn't in it).
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ending character index of reference run--or runs if a chapter and verse
		/// number are adjacent.
		/// </summary>
		/// <param name="tss">The ITsString.</param>
		/// <param name="iRun">The index of the run.</param>
		/// <param name="ttp">The text properties of the run.</param>
		/// <returns>character index at the end of the reference runs(s)</returns>
		/// ------------------------------------------------------------------------------------
		private static int GetLimOfReference(ITsString tss, int iRun, ref ITsTextProps ttp)
		{
			// The first run is either a verse or chapter number run.
			if (tss.RunCount > iRun + 1)
			{
				// There are more runs so determine if the next run is a verse number.
				ttp = tss.get_Properties(iRun + 1);
				// If so, get the lim of this next run.
				if (StStyle.IsStyle(ttp, ScrStyleNames.VerseNumber))
					return tss.get_LimOfRun(iRun + 1);
			}
			return tss.get_LimOfRun(iRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified run in the ITsString contains only whitespace.
		/// </summary>
		/// <param name="tss">The ITsString.</param>
		/// <param name="iRun">The index of the run into the tss.</param>
		/// <returns>
		/// 	<c>true</c> if the specified run in the ITsString is blank; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsBlank(ITsString tss, int iRun)
		{
			if (tss.RunCount > iRun)
			{
				string runText = tss.get_RunText(iRun);
				return runText == null || runText.Trim().Length == 0;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a BT string and ich, find the matching verse number string in the given
		/// vernacular string (or the next one if fGetNext is true).
		/// </summary>
		/// <param name="tssBt">given BT string</param>
		/// <param name="ich">character offset in BT string</param>
		/// <param name="wsBt">writing system of the BT</param>
		/// <param name="vernTss">given vernacular string</param>
		/// <param name="fGetNext">if false, we get the associated verse number from the
		/// vernacular; if true, we get the next verse number after the associated one</param>
		/// <param name="sChapterRunVern">the string of a chapter number run,
		/// if one was found just before the verse number we found; else null</param>
		/// <returns>The verse number string found in the vernacular, or null if none</returns>
		/// <remarks>If associated verse number match in vernacular is within bridge,
		/// its usefulness depends on fGetNext and whether we match the beginning or the end.
		/// Details are documented in the code. Unuseable matches will return null.</remarks>
		/// ------------------------------------------------------------------------------------
		private string GetMatchingVerseNumberFromVern(ITsString tssBt, int ich, int wsBt,
			ITsString vernTss, bool fGetNext, out string sChapterRunVern)
		{
			string sVerseRunVern = null;
			sChapterRunVern = null;
			BCVRef curRefStartBt;
			BCVRef curRefEndBt;

			// Get the current verse/chapter in the BT at ich
			ChapterVerseFound CvFoundBt = ScrTxtPara.GetBCVRefAtPosWithinTss(tssBt, ich,
				true, out curRefStartBt, out curRefEndBt);

			// If we found a verse...
			if (CvFoundBt == ChapterVerseFound.Verse
				|| CvFoundBt == (ChapterVerseFound.Chapter | ChapterVerseFound.Verse))
			{
				// Find the associated verse number in the vernacular para
				int ichLimVern;
				// First, find the same verse number in the vernacular para
				//   note: param chapterFoundVern is set true because we assume our parent
				//   paragraph is in the right chapter.
				bool chapterFoundVern = true;
				bool verseFoundVern;
				int verseStartVern, verseEndVern;
				verseFoundVern = FindVerseNumberInTss(curRefStartBt, vernTss, false,
					ref chapterFoundVern, out ichLimVern, out verseStartVern, out verseEndVern);


				// Did we find matching verse number run in the vernacular para?
				if (verseFoundVern)
				{
					// ichLimVern is already at the Lim of the associated verse number
					Debug.Assert(ichLimVern > 0);
					if (fGetNext)
					{
						// we want to get the following verse
						// if we found a bridge, we can use it only if we match the end of bridge
						if (verseStartVern != verseEndVern && verseEndVern != curRefEndBt.Verse)
						{
							// the end references do not match-- this is not a useful match.
							sVerseRunVern = null;
						}
						else // most common case - get the following verse number
						{
							sVerseRunVern = GetNextVerseNumberRun (vernTss, ichLimVern,
								out sChapterRunVern);
						}
					}
					else
					{
						// get the one we found
						sVerseRunVern = vernTss.get_RunText(vernTss.get_RunAt(ichLimVern - 1));
						// if we found a bridge, we can use it only if we match the start of bridge
						ScrReference.VerseToInt(sVerseRunVern, out verseStartVern, out verseEndVern);
						if (verseStartVern != curRefStartBt.Verse)
						{
							// our match is deep within a bridged verse number; we can't use it
							sVerseRunVern = null;
						}
					}
				}
				else if (ichLimVern > 0)
				{
					// We did not find curRefStartBt in the vernacular, but we did find a
					//   higher verse number.
					Debug.Assert(false, "FindVerseNumberInTss should never find a greater verse");
				}
				else
				{
					// No match and no larger verse number in the vernacular.
					// In this case, user should begin inserting at start of BT para to synchronize
					//   with the vernacular translation.
					sVerseRunVern = null;
				}
			}
			else if (CvFoundBt == ChapterVerseFound.Chapter)
			{
				MiscUtils.ErrorBeep(); // TODO TE-2278: Need to implement this scenario
				return "400 CHAPTER FOUND, NO VERSE";
			}
			else // No chapter or verse found in the back translation
			{
				// Get first verse reference from vernacular, if available
				sVerseRunVern = GetNextVerseNumberRun(vernTss, 0, out sChapterRunVern);
			}

			return sVerseRunVern;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a structured string, this method searches for a given target reference. If
		/// found, it returns the position just after the verse number found in that paragraph.
		/// If fStopIfGreaterVerseFound is true and we encounter a greater verse number,
		/// we return false but with information about the greater verse number encountered.
		/// </summary>
		/// <param name="targetRef">The reference being sought</param>
		/// <param name="tss">structured string to search for reference</param>
		/// <param name="fStopIfGreaterVerseFound">true if we want to return false immediately
		/// if we find a greater verse number</param>
		/// <param name="fChapterFound">at call: true if beginning of this tss is already in the
		/// target chapter; upon return: set to true if we encountered the target chapter in
		/// this tss</param>
		/// <param name="ichLim">The index immediately following the verse number found,
		/// or 0 if desired verse number not found.</param>
		/// <param name="startVerseOut">starting verse of the verse number run found,
		/// or 0 if desired verse number not found.</param>
		/// <param name="endVerseOut">ending verse of the verse number run found,
		/// or 0 if desired verse number not found.</param>
		/// <returns>true if matching verse number found, otherwise false</returns>
		/// <remarks>if fStopIfGreaterVerseFound is true and we in fact encounter a greater one,
		/// we return false immediately and the 3 output params provide info about the greater
		/// verse number found</remarks>
		/// ------------------------------------------------------------------------------------
		protected bool FindVerseNumberInTss(BCVRef targetRef, ITsString tss,
			bool fStopIfGreaterVerseFound, ref bool fChapterFound, out int ichLim,
			out int startVerseOut, out int endVerseOut)
		{
			ichLim = 0; // default values if not found
			startVerseOut = 0;
			endVerseOut = 0;

			if (tss.Text == null)
				return false;

			TsRunInfo tsi;
			ITsTextProps ttpRun;
			int ich = 0;
			bool fFoundChapterNumHere = false;
			int iRun = 0;
			while (ich < tss.Length)
			{
				// Get props of current run.
				ttpRun = tss.FetchRunInfoAt(ich, out tsi);
				// If we are already in our target chapter
				if (fChapterFound)
				{
					// See if run is our verse number style.
					if (StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber))
					{
						// The whole run is the verse number. Extract it.
						string sVerseNum = tss.get_RunText(tsi.irun);
						int startVerse, endVerse;
						ScrReference.VerseToInt(sVerseNum, out startVerse, out endVerse);
						if (startVerse <= targetRef.Verse && endVerse >= targetRef.Verse)
						{
							ichLim = tsi.ichLim; //end of the verse number run
							startVerseOut = startVerse;
							endVerseOut = endVerse;
							return true;
						}
						else if (fStopIfGreaterVerseFound && startVerse > targetRef.Verse)
						{	// we found a greater verse number and we want to stop on it
							ichLim = tsi.ichLim; //end of the verse number run
							startVerseOut = startVerse;
							endVerseOut = endVerse;
							return false;
						}
					}
					else if (targetRef.Verse == 1 && fFoundChapterNumHere)
					{
						ichLim = tsi.ichMin; //end of the verse number run
						startVerseOut = endVerseOut = 1;
						return true;
					}
				}

				// See if run is our chapter number style.
				if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
				{
					try
					{
						// Assume the whole run is the chapter number. Extract it.
						string sChapterNum = tss.get_RunText(tsi.irun);
						int nChapter = ScrReference.ChapterToInt(sChapterNum);
						// Is this our target chapter number?
						fFoundChapterNumHere = fChapterFound =
							(nChapter == targetRef.Chapter || fChapterFound);
					}
					catch (ArgumentException)
					{
						// ignore runs with invalid Chapter numbers
					}
				}
				ich = tsi.ichLim;
				iRun++;
			}

			// Verse was not found in the tss
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove any duplicate verse numbers following the new verse number in the following
		/// text in the current as well as the following section, if any.
		/// </summary>
		/// <param name="hvoObj">The id of the para or translation being modified</param>
		/// <param name="propTag">The flid (Contents or Translation)</param>
		/// <param name="tss">The structured string or the para or trans with the new verse
		/// number</param>
		/// <param name="wsAlt">The writing system, if a back trans multiString alt</param>
		/// <param name="chapterToRemove">A string representation of the duplicate chapter number
		/// to remove.</param>
		/// <param name="verseRangeToRemove">A string representation of the duplicate verse number to
		/// remove. This may also be a verse bridge, in which case we will remove all verse
		/// numbers up to the end value of the bridge</param>
		/// <param name="ich">The character offset after which we start looking for dups</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveDuplicateVerseNumbers(int hvoObj, int propTag, ITsString tss, int wsAlt,
			string chapterToRemove, string verseRangeToRemove, int ich)
		{
			// Determine the verse number we will remove up to
			int removeUpToVerse = ScrReference.VerseToIntEnd(verseRangeToRemove);

			bool inBackTrans = (m_cache.GetClassOfObject(hvoObj) == CmTranslation.kClassId);

			// Get my current StText, ScrSection and ScrBook
			int hvoCurrPara = inBackTrans ? (new CmTranslation(m_cache, hvoObj)).OwnerHVO : hvoObj;
			ScrTxtPara currentPara = new ScrTxtPara(m_cache, hvoCurrPara);

			// Determine the last chapter reference to remove.
			int removeChapter = (chapterToRemove != null && chapterToRemove.Length > 0) ?
				ScrReference.ChapterToInt(chapterToRemove) : 0;

			// First look in the paragraph where the verse was inserted.
			if (RemoveDuplicateVerseNumbersInPara(hvoObj, propTag, tss, wsAlt, removeChapter,
				removeUpToVerse, ich))
			{
				return;
			}

			// Search through current and subsequent section (if any) for duplicate verse numbers.
			StText text = new StText(m_cache, currentPara.OwnerHVO);
			ScrSection currentSection = new ScrSection(m_cache, text.OwnerHVO);
			IScrBook currentBook = currentSection.OwningBook;

			// First look through successive paragraphs for duplicate verse numbers, and then
			// try the next section if necessary.
			if (!RemoveDuplicateVerseNumbersInText(text, currentPara.IndexInOwner + 1, inBackTrans,
				propTag, wsAlt, removeChapter, removeUpToVerse))
			{
				ScrSection nextSection = currentSection.NextSection;
				if (nextSection != null)
				{
					RemoveDuplicateVerseNumbersInText(nextSection.ContentOA, 0,
						inBackTrans, propTag, wsAlt, removeChapter, removeUpToVerse);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the duplicate verse numbers in text.
		/// </summary>
		/// <param name="text">The text (either the remainder of the text following the
		/// paragraph where the insertion ocurred or in the next section)</param>
		/// <param name="iParaStart">The index of the para to start searching for dups.</param>
		/// <param name="inBackTrans">Indicates whether to search in back trans.</param>
		/// <param name="propTag">The flid (Contents or Translation)</param>
		/// <param name="wsAlt">The writing system, if a back trans multiString alt</param>
		/// <param name="chapterToRemove">The duplicate chapter number to remove.</param>
		/// <param name="removeUpToVerse">The last duplicate verse number to remove.</param>
		/// <returns><c>true</c> if all remaining duplicates have been removed; <c>false</c>
		/// if caller should re-call this method with the next section (or just give up)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool RemoveDuplicateVerseNumbersInText(IStText text, int iParaStart,
			bool inBackTrans, int propTag, int wsAlt, int chapterToRemove, int removeUpToVerse)
		{
			ITsString tss;
			int[] paraHvos = text.ParagraphsOS.HvoArray;
			for (int iPara = iParaStart; iPara < paraHvos.Length; iPara++)
			{
				// Get hvo and tss for this para or translation
				StTxtPara para = new StTxtPara(m_cache, paraHvos[iPara]);
				int hvoObj = 0;
				if (inBackTrans)
				{
					ICmTranslation trans = para.GetBT();
					if (trans == null)
						continue;
					hvoObj = trans.Hvo;
					tss = trans.Translation.GetAlternative(wsAlt).UnderlyingTsString;
				}
				else
				{
					hvoObj = para.Hvo;
					tss = para.Contents.UnderlyingTsString;
				}

				// Remove any duplicate verse number in this para or translation
				if (RemoveDuplicateVerseNumbersInPara(hvoObj, propTag, tss, wsAlt,
					chapterToRemove, removeUpToVerse, 0))
				{
					return true; // removal is complete
				}
			}
			return false; // removal isn't complete
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove chapters in back translation paragraph (btTss) that do not exist in the
		/// vernacular paragraph.
		/// </summary>
		/// <param name="hvoObj">The id of the para or translation being modified</param>
		/// <param name="btTss">back translation tss</param>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt</param>
		/// <param name="startRef">starting reference of vernacular paragraph</param>
		/// <param name="endRef">ending reference of vernacular paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void CleanChapterInBtPara(int hvoObj, int wsAlt, ref ITsString btTss,
			BCVRef startRef, BCVRef endRef)
		{
			int iRun = 0;
			while (iRun < btTss.RunCount)
			{
				RemoveOutOfRangeChapterRun(iRun, hvoObj, wsAlt, startRef, endRef, ref btTss);
				iRun++;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove the current chapter run if it is not within the specified reference range.
		/// </summary>
		/// <param name="iRun">run index</param>
		/// <param name="hvoObj">the id of the paragraph of translation being modified</param>
		/// <param name="wsAlt">the writing system, if a back translation the multiString alt</param>
		/// <param name="startRef">minimum reference allowed in tss</param>
		/// <param name="endRef">maximum reference allowed in tss</param>
		/// <param name="tss">ref: given structured string from which out of range chapters will
		/// be removed</param>
		/// <returns>true if chapter number run removed, otherwise false</returns>
		/// <remarks>If the specified run is not a chapter run, it will not be removed.</remarks>
		/// ------------------------------------------------------------------------------------
		private bool RemoveOutOfRangeChapterRun(int iRun, int hvoObj, int wsAlt,
			BCVRef startRef, BCVRef endRef, ref ITsString tss)
		{
			Debug.Assert(iRun >= 0 && iRun < tss.RunCount, "Out of range run index");

			ITsTextProps ttp = tss.get_Properties(iRun);
			// Is run is a chapter number?
			if (ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) ==
				ScrStyleNames.ChapterNumber)
			{
				string runText = tss.get_RunText(iRun);
				int chapterNum = ScrReference.ChapterToInt(runText);
				if (chapterNum < startRef.Chapter || chapterNum > endRef.Chapter)
				{
					// chapter number is out of range. Remove!
					int dummy;
					int cchDel= runText.Length;
					int ich = tss.get_MinOfRun(iRun);
					ReplaceInParaOrBt(hvoObj, (int)CmTranslation.CmTranslationTags.kflidTranslation,
						wsAlt, null, null, ich, ich + cchDel, ref tss, out dummy);
					return true;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove any duplicate chapter/verse number(s) following the new verse number, in the
		/// given paragraph or back translation.
		/// </summary>
		/// <param name="hvoObj">The id of the para or translation being modified</param>
		/// <param name="propTag">The flid (Contents or Translation)</param>
		/// <param name="tss">The structured string or the para or trans to remove in</param>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt</param>
		/// <param name="removeChapter">The chapter number to remove, or 0 if none</param>
		/// <param name="removeUpToVerse">The maximum verse number to remove</param>
		/// <param name="ich">The character offset at which we start looking for dups</param>
		/// <returns><c>true</c> if we're all done removing, <c>false</c> if caller needs to
		/// check subsequent paragraphs
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool RemoveDuplicateVerseNumbersInPara(int hvoObj, int propTag, ITsString tss,
			int wsAlt, int removeChapter, int removeUpToVerse, int ich)
		{
			bool fAllDone = false;

			while (ich < tss.Length)
			{
				int iRun = tss.get_RunAt(ich);
				ITsTextProps ttp = tss.get_Properties(iRun);
				if (removeChapter > 0 &&
					ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) ==
					ScrStyleNames.ChapterNumber)
				{
					// Process this chapter number run
					string chapterNumberText = tss.get_RunText(iRun);
					int chapterNum = ScrReference.ChapterToInt(chapterNumberText);

					if (chapterNum == removeChapter)
					{
						// Target chapter found. Remove!
						int cchDel = chapterNumberText.Length;
						int dummy;
						ReplaceInParaOrBt(hvoObj, propTag, wsAlt, null, null,
							ich, ich + cchDel, ref tss, out dummy);

						// since we removed an entire run, we must adjust our current iRun
						if (iRun > 0)
							--iRun;
					}
					//If we found a chapter beyond our target chapter, we are done.
					else if (chapterNum > removeChapter)
						return true;
				}
				else if (ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) ==
					ScrStyleNames.VerseNumber)
				{
					// Process this verse number run
					string verseNumberText = tss.get_RunText(iRun);
					string bridgeJoiner = m_scr.BridgeForWs(wsAlt);

					// get binary values of this verse number run
					int startNumber, endNumber; //numbers in this verse number run
					ScrReference.VerseToInt(verseNumberText, out startNumber, out endNumber);
					// and the bridge-joiner index, if any
					int bridgeJoinerIndex = verseNumberText.IndexOf(bridgeJoiner, StringComparison.Ordinal);

					if (startNumber <= removeUpToVerse)
					{
						// Target verse(s) found. Remove!
						int cchDel;
						int dummy;

						if (endNumber <= removeUpToVerse)
						{
							//remove all of verseNumberText
							cchDel = verseNumberText.Length;
							ReplaceInParaOrBt(hvoObj, propTag, wsAlt, null, null,
								ich, ich + cchDel,
								ref tss, out dummy);

							// since we removed an entire run, we must adjust our current iRun
							if (iRun > 0)
								--iRun;

							if (endNumber == removeUpToVerse)
								fAllDone = true;
						}
						else if (endNumber == removeUpToVerse + 1)
						{
							// reduce to a single verse (ending verse)
							Debug.Assert(bridgeJoinerIndex > -1);
							cchDel = bridgeJoinerIndex + bridgeJoiner.Length;
							ReplaceInParaOrBt(hvoObj, propTag, wsAlt, null, null,
								ich, ich + cchDel,
								ref tss, out dummy);

							fAllDone = true;
						}
						else // endNumber > removeUpToVerse + 1
						{
							// set beginning of bridge to max+1
							Debug.Assert(bridgeJoinerIndex > -1);
							cchDel = bridgeJoinerIndex;
							string maxPlusOne = m_scr.ConvertToString(removeUpToVerse + 1);
							ReplaceInParaOrBt(hvoObj, propTag, wsAlt, maxPlusOne, ttp,
								ich, ich + cchDel,
								ref tss, out dummy);

							fAllDone = true;
						}
					}
					else // startNumber > removeUpToVerse
					{
						fAllDone = true; //we are done looking.
						//  we assume verse numbers are in order
					}

					if (fAllDone)
						return true;
				}
				// we are not looking to remove a chapter, and the current run is not a verse
				else if (ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) ==
					ScrStyleNames.ChapterNumber)
				{
					string runText = tss.get_RunText(iRun);
					if (ScrReference.ChapterToInt(runText) != 1)
					{
						// quit when a chapter number is found that is not one.
						return true;
					}
					else
					{
						// chapter 1 found after inserted verse number. Remove!
						int cchDel = runText.Length;
						int dummy;
						ReplaceInParaOrBt(hvoObj, propTag, wsAlt, null, null,
							ich, ich + cchDel, ref tss, out dummy);

						// since we removed an entire run, we must adjust our current iRun
						if (iRun > 0)
							--iRun;
					}
				}
				ich = tss.get_LimOfRun(iRun);
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text of the next verse number run in a given ITsString after the given
		/// character position.
		/// </summary>
		/// <param name="tss">the given ITsString</param>
		/// <param name="ich">the given character position</param>
		/// <param name="sChapterNumberRun">the string of a chapter number run,
		/// if one was found just before the verse number we found; else null</param>
		/// <returns>the string of the next verse number after the given ich, or null if not
		/// found</returns>
		/// ------------------------------------------------------------------------------------
		private string GetNextVerseNumberRun(ITsString tss, int ich,
			out string sChapterNumberRun)
		{
			sChapterNumberRun = null;

			if (tss.Text == null)
				return null;

			TsRunInfo tsi;
			ITsTextProps ttpRun;
			while (ich < tss.Length)
			{
				// Get props of current run.
				ttpRun = tss.FetchRunInfoAt(ich, out tsi);
				// See if run is our verse number style.
				if (StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber))
				{
					// The whole run is the verse number. Extract it.
					string sVerseNumberRun = tss.get_RunText(tsi.irun);

					// Also extract a preceeding chapter number run, if present.
					if (tsi.ichMin > 0)
					{
						// Get props of previous run.
						ttpRun = tss.FetchRunInfoAt(tsi.ichMin - 1, out tsi);
						// See if run is chapter number style; get its text.
						if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
							sChapterNumberRun = tss.get_RunText(tsi.irun);
					}
					return sVerseNumberRun;
				}

				// if this is a chapter number, check the next run to see if it is a
				// verse number (perhaps it is implied).
				if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber) &&
					tsi.irun < tss.RunCount - 1)
				{
					ITsTextProps ttpNextRun = tss.get_Properties(tsi.irun + 1);
					if (!StStyle.IsStyle(ttpNextRun, ScrStyleNames.VerseNumber))
					{
						// verse 1 is implied; get the chapter number
						sChapterNumberRun = tss.get_RunText(tsi.irun);
						return null;
					}
				}

				ich = tsi.ichLim; // get ready for next run
			}

			// no verse number found
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If selection is a simple IP, check current run or runs on either side (if between
		/// runs). If run(s) is/are a verse number run, return true.
		/// </summary>
		/// <param name="vwsel">selection to check</param>
		/// <returns>True if IP is in or next to a verse number run</returns>
		/// ------------------------------------------------------------------------------------
		private bool SelIPInVerseNumber(IVwSelection vwsel)
		{
			if (vwsel.IsRange)
				return false;
			ITsString tss;
			int ich, hvo, tag, ws, ichMin, ichLim;
			bool fAssocPrev;
			vwsel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			return InReference(tss, ich, false, out ichMin, out ichLim);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given character position is next to or in a chapter/verse number
		/// run in the given ITsString. This overload provides less detail.
		/// </summary>
		/// <param name="tss">The string to examine</param>
		/// <param name="ich">character index to look at</param>
		/// <param name="fCheckForChapter">true if we want to include searching for chapters</param>
		/// <param name="ichMin">Gets set to the beginning of the chapter/verse number run if
		/// we find a verse number</param>
		/// <param name="ichLim">Gets set to the Lim of the chapter/verse number run if
		/// we find a verse number</param>
		/// <returns><c>true</c> if we find a verse number/bridge (and/or chapter);
		/// <c>false</c> if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool InReference(ITsString tss, int ich, bool fCheckForChapter, out int ichMin,
			out int ichLim)
		{
			int ichBetweenChapterAndVerse;
			bool fFoundVerse;
			return InReference(tss, ich, fCheckForChapter, out ichMin, out ichLim,
				out ichBetweenChapterAndVerse, out fFoundVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given character position is in or next to a chapter/verse number
		/// run in the given ITsString.
		/// If we are, check the run beyond so we can locate the entire chapt
		/// </summary>
		/// <param name="tss">The string to examine</param>
		/// <param name="ich">character index to look at</param>
		/// <param name="fCheckForChapter">true if we want to include searching for chapters</param>
		/// <param name="ichMin">output: Gets set to the beginning of the chapter/verse number
		/// run if we find a verse/chapter number; otherwise -1</param>
		/// <param name="ichLim">output: Gets set to the Lim of the chapter/verse number run if
		/// we find a verse/chapter number; otherwise -1</param>
		/// <param name="ichBetweenChapterAndVerse">NO LONGER USED
		/// Was output: set to the position between the
		/// chapter and verse number runs if we find a verse number and chapter number run
		/// at (near) ich; otherwise -1</param>
		/// <param name="fVerseAtIch">output: true if ich is in or next to a verse number</param>
		/// <returns><c>true</c> if we find a verse number/bridge (and/or chapter);
		/// <c>false</c> if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool InReference(ITsString tss, int ich, bool fCheckForChapter, out int ichMin,
			out int ichLim, out int ichBetweenChapterAndVerse, out bool fVerseAtIch)
		{
			ichMin = -1;
			ichLim = -1;
			ichBetweenChapterAndVerse = -1;
			fVerseAtIch = false;

			if (tss.Length == 0)
				return false;

			// Check the run to the right of ich
			int iRun = tss.get_RunAt(ich);
			if (FindRefRunMinLim(tss, iRun, fCheckForChapter,
				out ichMin, out ichLim, out fVerseAtIch))
			{
				// We found a verse (or chapter) run. Check following run too.
				if (tss.RunCount > iRun + 1)
				{
					bool dummy;
					int ichMinNext, ichLimNext;
					if (FindRefRunMinLim(tss, iRun + 1, fCheckForChapter,
						out ichMinNext, out ichLimNext, out dummy))
					{
						// found another reference run. extend the ichLim.
						ichLim = ichLimNext;
					}
				}
			}

			// if iRun is the first run: we are done, return our result
			if (iRun == 0)
				return (ichMin > -1);

			// Is the char to the left of ich in the same run as ich??
			int iRunPrevChar = tss.get_RunAt(ich - 1);
			if (iRunPrevChar == iRun)
				return (ichMin > -1); //no adjacent ref to the left, so we are done

			// Check the run to the left of ich
			bool dummy2;
			int ichMinPrev, ichLimPrev;
			if (FindRefRunMinLim(tss, iRunPrevChar, fCheckForChapter,
				out ichMinPrev, out ichLimPrev, out dummy2))
			{
				// We found a verse (or chapter) run.
				ichMin = ichMinPrev; // set or extend the ichMin
				if (ichLim == -1) // reference not found after ich
					ichLim = ichLimPrev;
				// check an additional previous run, too
				if (iRunPrevChar >= 1)
				{
					if (FindRefRunMinLim(tss, iRunPrevChar - 1, fCheckForChapter,
						out ichMinPrev, out ichLimPrev, out dummy2))
					{
						// found a second previous reference run. extend the ichMin.
						ichMin = ichMinPrev;
					}
				}
			}

			return (ichMin > -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the extent of the current run if it is a chapter (optionally) or verse
		/// number.
		/// </summary>
		/// <param name="tss">paragraph</param>
		/// <param name="iRun">run index</param>
		/// <param name="fCheckForChapter">true if we want to include searching for chapters
		/// </param>
		/// <param name="ichMin">output: index at beginning of run</param>
		/// <param name="ichLim">output: index at limit of run</param>
		/// <param name="fIsVerseNumber">output: <c>true</c> if this run is a verse number run
		/// </param>
		/// <returns><c>true</c> if this run is a verse number/bridge (or chapter, if checking
		/// for that); <c>false</c> if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool FindRefRunMinLim(ITsString tss, int iRun, bool fCheckForChapter,
			out int ichMin, out int ichLim, out bool fIsVerseNumber)
		{
			ichMin = -1;
			ichLim = -1;

			// Check current run to see if it's a verse (or chapter) number.
			ITsTextProps ttp = tss.get_Properties(iRun);
			fIsVerseNumber = StStyle.IsStyle(ttp, ScrStyleNames.VerseNumber);
			bool fIsChapterNumber = false;
			if (!fIsVerseNumber && fCheckForChapter)
				fIsChapterNumber = StStyle.IsStyle(ttp, ScrStyleNames.ChapterNumber);

			if (fIsVerseNumber || fIsChapterNumber)
			{
				ichMin = tss.get_MinOfRun(iRun);
				ichLim = tss.get_LimOfRun(iRun);
			}
			return (fIsVerseNumber || fIsChapterNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert/replace the range in the given back translation with the given chapter and verse.
		/// </summary>
		/// <param name="hvoObj">The id of the translation being modified</param>
		/// <param name="propTag">The flid (i.e. Translation)</param>
		/// <param name="wsAlt">The writing system of the back translation multiString alt</param>
		/// <param name="ichMin">character offset Min at which we will replace in the tss
		///  </param>
		/// <param name="ichLim">end of the range at which we will replace in the tss </param>
		/// <param name="sChapterNumIns">The chapter number string to be inserted, if any</param>
		/// <param name="sVerseNumIns">The verse number string to be inserted, if any</param>
		/// <param name="tssBt">ref: The given structured string from the BT, in which we will
		/// replace</param>
		/// <param name="ichLimIns">output: gets set to the end of what we inserted</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceRangeInBt(int hvoObj, int propTag, int wsAlt, int ichMin, int ichLim,
			ref string sChapterNumIns, ref string sVerseNumIns, ref ITsString tssBt, out int ichLimIns)
		{
			ichLimIns = -1;

			// Insert the chapter number, if defined
			if (sChapterNumIns != null)
			{
				string oldText = tssBt.get_RunText(tssBt.get_RunAt(ichMin));
				ITsTextProps oldProps = tssBt.get_PropertiesAt(ichMin);

				if (oldText == sChapterNumIns && oldProps.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle) == ScrStyleNames.ChapterNumber)
				{
					ichMin += sChapterNumIns.Length;
					sChapterNumIns = null;
				}
				else
				{
					ITsTextProps ttpChap = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, wsAlt);
					ReplaceInParaOrBt(hvoObj, propTag, wsAlt, sChapterNumIns, ttpChap, ichMin, ichLim,
						ref tssBt, out ichLimIns);
					// adjust range for verse insert
					ichMin += sChapterNumIns.Length;
					ichLim = ichMin;
				}
			}

			// Insert the verse number, if defined. If the text has not changed, then do not make
			// the change so an undo task will not be created.
			if (sVerseNumIns != null)
			{
				string oldText = tssBt.GetChars(ichMin, ichLim);
				ITsTextProps oldProps = tssBt.get_PropertiesAt(ichMin);

				if (oldText == sVerseNumIns && oldProps.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle) == ScrStyleNames.VerseNumber)
					sVerseNumIns = null;
				else
				{
					ITsTextProps ttpVerse = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, wsAlt);
					ReplaceInParaOrBt(hvoObj, propTag, wsAlt, sVerseNumIns, ttpVerse,
						ichMin, ichLim, ref tssBt, out ichLimIns);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a structured string in a paragraph or back translation, replaces the given
		/// range with a given string and given run props. Updates the cache.
		/// </summary>
		/// <param name="hvoObj">The id of the paragraph or translation being modified</param>
		/// <param name="propTag">The flid (i.e. Contents or Translation)</param>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt</param>
		/// <param name="str">the string to be inserted; if null, we remove the range</param>
		/// <param name="ttp">The text props for the string being inserted</param>
		/// <param name="ichMin">character offset Min at which we will replace in the tss
		///  </param>
		/// <param name="ichLim">end of the range at which we will replace in the tss </param>
		/// <param name="tss">The given structured string from the para or BT, in which we will
		/// replace</param>
		/// <param name="ichLimNew">gets set to the end of what we inserted</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceInParaOrBt(int hvoObj, int propTag, int wsAlt, string str,
			ITsTextProps ttp,  int ichMin, int ichLim, ref ITsString tss, out int ichLimNew)
		{
			Debug.Assert(tss != null);

			// Insert in the given string in place of the existing range
			int cchIns = (str == null ? 0 : str.Length);
			int cchDel = ichLim - ichMin;
			ITsStrBldr tsb = tss.GetBldr();
			tsb.Replace(ichMin, ichLim, str, ttp);
			tss = tsb.GetString();

			// Update the cache with the new tss...
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents) //vernacular para
			{
				// Record information, as if we had a selection in the paragraph we are about to modify,
				// that will allow us to adjust annotations.
				m_annotationAdjuster.OnAboutToModify(m_cache, hvoObj);
				sda.SetString(hvoObj, propTag, tss);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoObj,
					propTag, ichMin, cchIns, cchDel);
				m_annotationAdjuster.OnFinishedEdit();
			}
			else // translation
			{
				Debug.Assert(propTag == (int)CmTranslation.CmTranslationTags.kflidTranslation);
				Debug.Assert(wsAlt > 0);
				sda.SetMultiStringAlt(hvoObj, propTag, wsAlt, tss);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoObj,
					propTag, wsAlt, 1, 1);
			}

			// calculate the end of what was inserted
			ichLimNew = ichMin + cchIns;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prevents window events from ending insert verse mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PreventEndInsertVerseNumbers()
		{
			CheckDisposed();

			if (m_InsertVerseMessageFilter != null)
				m_InsertVerseMessageFilter.PreventEndInsertVerseNumbers();
		}

		//		// TomB and MarkB have decided not to do this, at least for now
		//		//  but it's a unique bunch of logic that we may need later
		//		private int SetVerseAtStartOfBtIfNeeded(ITsString tssBt, int ichSel, ITsString tssVern)
		//		{
		//			if (ichSel == 0)
		//				return 0; // Insert/Update methods will handle start of BT
		//
		//			bool fShouldCheckStartOfBt = false;
		//
		//			// Search for a verse number that preceeds the current run
		//			TsRunInfo tsiBt;
		//			tssBt.FetchRunInfoAt(ichSel, out tsiBt);
		//			int ich = tsiBt.ichMin;
		//			//			if (ich == 0) // if we're at the first run, no need to continue search
		//			//				fShouldCheckStartOfBt = true;
		//			//			else
		//			if (ich > 0)
		//				--ich;
		//
		//			ITsTextProps ttpBt;
		//			while (ich >= 0)
		//			{
		//				// Get props of current run.
		//				ttpBt = tss.FetchRunInfoAt(ich, out tsiBt);
		//				// See if it is our verse number style.
		//				if (StStyle.IsStyle(ttpBt, ScrStyleNames.VerseNumber))
		//				{
		//					if (tsiBt.irun > 0)
		//						return 0; //this verse number is in mid para; no need to update start of BT
		//				}
		//
		//				// move index (going backwards) to the Min of the run we just looked at
		//				ich = tsi.ichMin;
		//				// then decrement index so it's in the previous run, if any
		//				ich--;
		//			}
		//
		//			// We now have information about the first run of the BT.
		//			// Now we need to get information about the first run of the vernacular.
		//			TsRunInfo tsiVern;
		//			ITsTextProps ttpVern;
		//			ttpVern = tssVern.FetchRunInfo(0, out tsiVern);
		//
		//			int charToInsert;
		//			bool vernStartsWithVerse = StStyle.IsStyle(ttpVern, ScrStyleNames.VerseNumber);
		//			bool btStartsWithVerse = StStyle.IsStyle(ttpBt, ScrStyleNames.VerseNumber);
		//			if (!vernStartsWithVerse && !btStartsWithVerse)
		//			{
		//				// Both vernacular and BT do not start with verse numbers.
		//				// No need to update.
		//				return 0;
		//			}
		//			else if (vernStartsWithVerse)
		//			{
		//				// get verse from vernacular and insert into back translation
		//				string sVerseNumIns = tssVern.get_RunAt(0);
		//				return (sVerseNumIns.Length);
		//			}
		//
		//			// Are there previous verse number runs in the BT para?
		//
		//			string btRun, vernRun; ...
		//		}

		#endregion

		#region Insert Footnote
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote at the current selection
		/// </summary>
		/// <param name="styleName">style name for created footnote</param>
		/// <param name="iFootnote">out: If selHelper is in vernacular para, the ihvo of the
		/// footnote just inserted. If selHelper is in back trans, the ihvo of the footnote
		/// corresponding to the ref ORC just inserted in the BT, or -1 no corresponding</param>
		/// <returns>The created/corresponding footnote</returns>
		/// ------------------------------------------------------------------------------------
		public ScrFootnote InsertFootnote(string styleName, out int iFootnote)
		{
			CheckDisposed();

			return InsertFootnote(CurrentSelection, styleName, out iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote at the given selection
		/// </summary>
		/// <param name="selHelper">Current selection information</param>
		/// <param name="styleName">style name for created footnote</param>
		/// <param name="iFootnote">out: If selHelper is in vernacular para, the ihvo of the
		/// footnote just inserted. If selHelper is in back trans, the ihvo of the footnote
		/// corresponding to the ref ORC just inserted in the BT, or -1 no corresponding</param>
		/// <returns>The created/corresponding footnote</returns>
		/// ------------------------------------------------------------------------------------
		public virtual ScrFootnote InsertFootnote(SelectionHelper selHelper, string styleName,
			out int iFootnote)
		{
			CheckDisposed();

			// Get any selected text.
			ITsString tssSelected;
			IVwSelection vwsel = selHelper.Selection;
			if (IsSelectionInOneEditableProp(vwsel))
				vwsel.GetSelectionString(out tssSelected, string.Empty);
			else
				tssSelected = StringUtils.MakeTss(string.Empty, m_cache.DefaultVernWs);

			int hvoObj;
			ITsString tssPara;
			int propTag;
			int ws;
			selHelper.ReduceToIp(SelectionHelper.SelLimitType.Bottom, false, false);
			int ichSel = GetSelectionInfo(selHelper, out hvoObj, out propTag, out tssPara, out ws);

			if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents)
				ws = Cache.DefaultVernWs;

			// get book info
			IScrBook book = GetCurrentBook(m_cache);
			// get paragraph info
			int paraHvo = selHelper.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs).hvo;
			StTxtPara para = new StTxtPara(m_cache, paraHvo);

			if (tssSelected.Length > 0)
			{
				tssSelected = StringUtils.RemoveORCsAndStylesFromTSS(tssSelected,
					new List<string>(new string[] {ScrStyleNames.ChapterNumber, ScrStyleNames.VerseNumber}),
					false, m_cache.LanguageWritingSystemFactoryAccessor);
				if (tssSelected.Length > 0)
				{
					ITsStrBldr bldr = tssSelected.GetBldr();
					bldr.SetStrPropValue(0, bldr.Length, (int) FwTextPropType.ktptNamedStyle, ScrStyleNames.ReferencedText);
					bldr.ReplaceRgch(bldr.Length, bldr.Length, " ", 1, StyleUtils.CharStyleTextProps(null, ws));
					tssSelected = bldr.GetString();
				}
			}

			ScrFootnote footnote = null;
			string undo;
			string redo;
			if (styleName == ScrStyleNames.CrossRefFootnoteParagraph)
				TeResourceHelper.MakeUndoRedoLabels("kstidInsertCrossReference", out undo, out redo);
			else
				TeResourceHelper.MakeUndoRedoLabels("kstidInsertFootnote", out undo, out redo);
			using (UndoTaskHelper undoTaskHelper =
					  new UndoTaskHelper(Callbacks.EditedRootBox.Site, undo, redo, true))
			{
				try
				{
					if (propTag == (int)StTxtPara.StTxtParaTags.kflidContents)
					{
						// Inserting footnote into the vernacular paragraph
						iFootnote = FindFootnotePosition(book, selHelper);
						ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
						// create the footnote and insert its marker into the paragraph's string
						// builder.
						footnote = ScrFootnote.InsertFootnoteAt(book, styleName, iFootnote, tsStrBldr, ichSel);

						// BEFORE we insert the ORC in the paragraph, we need to insert an empty
						// paragraph into the new StFootnote, because the para style is needed to
						// determine the footnote marker type.
						StTxtPara footnotePara = new StTxtPara();
						footnote.ParagraphsOS.Append(footnotePara);
						// If we wait for this to be created by the VC, its creation won't be part of the
						// Undo task, and we won't be able to Undo creating the footnote, because the paragraph
						// will own something that Undo doesn't know to delete (TE-7988).
						footnotePara.GetOrCreateBT();
						ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
							styleName);
						footnotePara.StyleRules = propsBldr.GetTextProps();

						// Record information, as if we were typing the footnote caller, that allows
						// segment boundaries to be adjusted properly.
						OnAboutToEdit();

						// update the paragraph contents to include the footnote marker
						para.Contents.UnderlyingTsString = tsStrBldr.GetString();

						// Finish off any necessary annotation adjustments.
						m_annotationAdjuster.OnFinishedEdit();

						// Insert the selected text (or an empty run) into the footnote paragraph.
						footnotePara.Contents.UnderlyingTsString = tssSelected;

						// Do a prop change to get the footnote updated in all views
						m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, book.Hvo,
							(int)ScrBook.ScrBookTags.kflidFootnotes, iFootnote, 1, 0);
					}
					else
					{
						// Inserting footnote reference ORC into a back translation
						ICmTranslation btParaTrans = para.GetOrCreateBT();
						ITsString btTss = btParaTrans.Translation.GetAlternative(ws).UnderlyingTsString;
						footnote = FindVernParaFootnote(ichSel, btTss, para);
						if (footnote != null)
						{
							// Insert footnote reference ORC into back translation paragraph.
							ITsStrBldr tssBldr = btTss.GetBldr();
							//if reference to footnote already somewhere else in para, delete it first
							int ichDel = StTxtPara.DeleteBtFootnoteMarker(tssBldr, footnote.Guid);
							if (ichDel >= 0 && ichSel > ichDel)
								ichSel -= footnote.FootnoteMarker.Length;

							footnote.InsertRefORCIntoTrans(tssBldr, ichSel, ws);
							btParaTrans.Translation.SetAlternative(tssBldr.GetString(), ws);
							iFootnote = footnote.IndexInOwner;

							if (tssSelected.Length > 0)
							{
								ICmTranslation btFootnoteTrans = ((StTxtPara) footnote.ParagraphsOS[0]).GetOrCreateBT();
								ITsString btFootnoteTss = btFootnoteTrans.Translation.GetAlternative(ws).UnderlyingTsString;

								// Insert any selected text into this back translation for the footnote paragraph.
								btFootnoteTrans.Translation.SetAlternative(tssSelected, ws);
							}
						}
						else
						{
							iFootnote = -1;
							MiscUtils.ErrorBeep(); // No footnote reference ORC inserted
						}
					}
				}
				catch
				{
					undoTaskHelper.EndUndoTask = false;
					throw; // rethrow the original exception
				}
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds position in footnote list (0-based index) where new footnote should be
		/// inserted.
		/// </summary>
		/// <param name="book">book where footnote will be inserted</param>
		/// <param name="helper"></param>
		/// <returns>index of the footnote in the book</returns>
		/// ------------------------------------------------------------------------------------
		private int FindFootnotePosition(IScrBook book, SelectionHelper helper)
		{
			if (book.FootnotesOS.Count == 0)
				return 0;

			StFootnote prevFootnote = FindFootnoteNearSelection(helper, book, false);
			if (prevFootnote == null)
				return 0;

			return prevFootnote.IndexInOwner + 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the vernacular footnote which corresponds to the selection position in the
		/// back translation. If no footnote in the vernacular corresponds to the one in the
		/// back translation, then return null.
		/// </summary>
		/// <param name="ichBtSel">the character offset in the back translation paragraph</param>
		/// <param name="btTss">back translation ITsString</param>
		/// <param name="vernPara">owning vernacular paragraph</param>
		/// <returns>corresponding vernacular footnote, or null if no corresponding footnote
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote FindVernParaFootnote(int ichBtSel, ITsString btTss, StTxtPara vernPara)
		{
			// Scan through the back translation paragraph to see if there are any footnotes
			int iBtFootnote = FindBtFootnoteIndex(ichBtSel, btTss);

			// Scan through the owning paragraph to see if there are enough footnotes to allow
			// the insertion of another footnote in the back translation
			return FindFootnoteAtIndex(vernPara, iBtFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the zero-based index within a paragraph of a footnote to be inserted in
		/// the back translation.</summary>
		/// <param name="ichBtSel">the character offset in the back translation paragraph</param>
		/// <param name="btTss">back translation ITsString</param>
		/// <returns>zero-based index of a footnote to be inserted</returns>
		/// ------------------------------------------------------------------------------------
		private int FindBtFootnoteIndex(int ichBtSel, ITsString btTss)
		{
			// Scan through the back translation paragraph to find the index of the footnote
			// to insert
			int iRun = 0;
			int iBtFootnote = 0;
			int runLim = btTss.get_RunAt(ichBtSel);
			TsRunInfo tri;
			ITsTextProps ttp;
			FwObjDataTypes odt;
			Set<FwObjDataTypes> desiredOrcTypes = new Set<FwObjDataTypes>(2);
			desiredOrcTypes.Add(FwObjDataTypes.kodtNameGuidHot);
			desiredOrcTypes.Add(FwObjDataTypes.kodtOwnNameGuidHot); // We have to look for this kind of ORC too because of a past bug that caused BT footnotes to get imported with the wrong type of ORC

			while (iRun < btTss.RunCount && btTss.get_LimOfRun(iRun) <= ichBtSel)
			{
				if (StringUtils.GetGuidFromRun(btTss, iRun, out odt, out tri, out ttp, desiredOrcTypes)
					!= Guid.Empty)
				{
					// Found footnote ORC in back translation
					iBtFootnote++;
				}
				iRun++;
			}

			return iBtFootnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a footnote in the vernacular paragraph at the specified index within the para.
		/// </summary>
		/// <param name="vernPara">owning vernacular paragraph</param>
		/// <param name="index">index within the vernPara of footnote to find</param>
		/// <returns>footnote in vernacular, if found</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote FindFootnoteAtIndex(StTxtPara vernPara, int index)
		{
			ITsString vernTss = vernPara.Contents.UnderlyingTsString;

			// Scan through the vernacular paragraph to find the corresponding footnote
			Guid guid;
			FwObjDataTypes odt;
			int iRun = 0;
			int iVernFootnote = 0;
			while (iRun < vernTss.RunCount)
			{
				guid = StringUtils.GetOwnedGuidFromRun(vernTss, iRun, out odt);

				if (odt == FwObjDataTypes.kodtOwnNameGuidHot)
				{
					if (index == iVernFootnote)
					{
						// Found a footnote in the vernacular that is the same index in the paragraph
						// as the one we are trying to insert into the back translation
						int hvoFootnote = m_cache.GetIdFromGuid(guid);
						return new ScrFootnote(m_cache, hvoFootnote);
					}
					iVernFootnote++; // Found a footnote, but it was before the one we are looking for
				}
				iRun++;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the nearest footnote around the given selection. Searches backward and
		/// forward.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public StFootnote FindFootnoteNearSelection(SelectionHelper helper)
		{
			CheckDisposed();

			if (CurrentSelection == null)
				return null;

			return FindFootnoteNearSelection(helper, GetCurrentBook(m_cache), true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the nearest footnote before the given selection.
		/// </summary>
		/// <param name="helper">The selection helper.</param>
		/// <param name="book">The book that owns the footnote collection.</param>
		/// <param name="fSearchForward">True to also search forward within the current paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private StFootnote FindFootnoteNearSelection(SelectionHelper helper, IScrBook book,
			bool fSearchForward)
		{
			CheckDisposed();

			if (helper == null)
				helper = CurrentSelection;
			if (helper == null || book == null)
				return null;

			SelLevInfo[] levels = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);

			int iParagraph = -1;
			int tag = 0;
			int ich = helper.IchAnchor;
			int paraLev = helper.GetLevelForTag((int)StText.StTextTags.kflidParagraphs);
			int contentLev = helper.GetLevelForTag((int)StTxtPara.StTxtParaTags.kflidContents);

			Debug.Assert(paraLev != -1, "Need a paragraph for this method");
			iParagraph = levels[paraLev].ihvo;

			int iSection = ((ITeView)Control).LocationTracker.GetSectionIndexInBook(
				helper, SelectionHelper.SelLimitType.Anchor);

			if (iSection < 0)
				tag = (int)ScrBook.ScrBookTags.kflidTitle;
			else
			{
				tag = (helper.GetLevelForTag((int)ScrSection.ScrSectionTags.kflidContent) >= 0 ?
					(int)ScrSection.ScrSectionTags.kflidContent :
					(int)ScrSection.ScrSectionTags.kflidHeading);
			}

			// Special case: if we're in the caption of a picture, get the ich from
			// the first level instead of the anchor. (TE-4696)
			if (contentLev >= 0 && levels[contentLev].ihvo == -1)
				ich = levels[contentLev].ich;

			ScrFootnote prevFootnote = null;
			if (fSearchForward) // look first at our current position, if we are searching foward
			{
				prevFootnote = ScrFootnote.FindCurrentFootnote(m_cache, book, iSection,
					iParagraph, ich, tag);
			}

			if (prevFootnote == null)
			{
				StTxtPara para = new StTxtPara(m_cache, levels[paraLev].hvo);
				ITsString tss = para.Contents.UnderlyingTsString;
				if (ich != 0)
				{
					// look backwards in our current paragraph
					prevFootnote = ScrFootnote.FindLastFootnoteInString(m_cache, tss, ref ich, true);
				}
				else if (iParagraph > 0)
				{
					// look at the previous paragraph for a footnote at the end
					StText text = new StText(m_cache, levels[paraLev + 1].hvo);
					StTxtPara prevPara = (StTxtPara)text.ParagraphsOS[iParagraph - 1];
					ITsString prevTss = prevPara.Contents.UnderlyingTsString;
					int ichTmp = -1;
					prevFootnote = ScrFootnote.FindLastFootnoteInString(m_cache, prevTss, ref ichTmp, false);
					if (prevFootnote != null)
					{
						if (ichTmp == prevTss.Length - 1)
							ich = ichTmp;
						else
							prevFootnote = null;
					}
					// ENHANCE: Look across contexts.
				}
			}
			if (prevFootnote == null && fSearchForward)
			{
				// look ahead in the same paragraph
				StTxtPara para = new StTxtPara(m_cache, levels[paraLev].hvo);
				ITsString tss = para.Contents.UnderlyingTsString;
				prevFootnote = ScrFootnote.FindFirstFootnoteInString(m_cache, tss, ref ich, true);
			}
			if (prevFootnote == null)
			{
				// just go back until we find one
				prevFootnote = ScrFootnote.FindPreviousFootnote(m_cache,
					book, ref iSection, ref iParagraph, ref ich, ref tag);
			}

			return prevFootnote;
		}
		#endregion

		#region Methods to support inserting annotations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets info about the current selection needed to insert an annotation.
		/// </summary>
		/// <param name="beginObj">The begin object referenced by the annotation.</param>
		/// <param name="endObj">The end object referenced by the annotation.</param>
		/// <param name="wsSelector">The writing system selector.</param>
		/// <param name="tssQuote">The quote in the annotation (referenced text).</param>
		/// <param name="startRef">The starting reference in the selection.</param>
		/// <param name="endRef">The end reference in the selection.</param>
		/// <param name="startOffset">The starting character offset in the beginObj.</param>
		/// <param name="endOffset">The ending character offset in the endObj.</param>
		/// <exception cref="InvalidOperationException">When the current selection is not in
		/// a paragraph at all</exception>
		/// ------------------------------------------------------------------------------------
		public void GetAnnotationLocationInfo(out CmObject beginObj, out CmObject endObj,
			out int wsSelector, out int startOffset, out int endOffset, out ITsString tssQuote,
			out BCVRef startRef, out BCVRef endRef)
		{
			CheckDisposed();

			int beginObjHvo = 0;
			int endObjHvo = 0;
			beginObj = null;
			endObj = null;
			if (CurrentSelection != null)
			{
				// Get the Scripture reference information that the note will apply to
				SelLevInfo[] startSel = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Top);
				SelLevInfo[] endSel = CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Bottom);
				if (startSel.Length > 0 && endSel.Length > 0)
				{
					beginObjHvo = startSel[0].hvo;
					endObjHvo = endSel[0].hvo;

					// Get the objects at the beginning and end of the selection.
					beginObj = GetNoteTargetObject(beginObjHvo);
					endObj = GetNoteTargetObject(endObjHvo);
				}
			}

			if (beginObj == null || endObj == null)
			{
				// No selection, or selection is in unexpected object.
				throw new InvalidOperationException("No translation or paragraph in current selection levels");
			}

			int wsStart = GetCurrentBtWs(SelectionHelper.SelLimitType.Top);
			int wsEnd = GetCurrentBtWs(SelectionHelper.SelLimitType.Bottom);

			ScrReference[] startScrRef = GetCurrentRefRange(CurrentSelection, SelectionHelper.SelLimitType.Top);
			ScrReference[] endScrRef = GetCurrentRefRange(CurrentSelection, SelectionHelper.SelLimitType.Bottom);

			if (wsStart == wsEnd && beginObjHvo == endObjHvo)
			{
				// The selection range does not include more than one para or type of translation.
				wsSelector = wsStart;
				tssQuote = GetCleanSelectedText(out startOffset, out endOffset);
			}
			else
			{
				wsSelector = -1;
				startOffset = 0;
				endOffset = 0;
				tssQuote = null;
			}

			startRef = startScrRef[0];
			endRef = endScrRef[1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cleaned-up selected text (no ORCS, chapter/verse numbers, or
		/// leading/trailing space).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString GetCleanSelectedText()
		{
			int startOffset, endOffset;
			return GetCleanSelectedText(out startOffset, out endOffset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cleaned-up selected text (no ORCS, chapter/verse numbers, or
		/// leading/trailing space).
		/// </summary>
		/// <param name="startOffset">The start offset.</param>
		/// <param name="endOffset">The end offset.</param>
		/// ------------------------------------------------------------------------------------
		public ITsString GetCleanSelectedText(out int startOffset, out int endOffset)
		{
			if (CurrentSelection == null)
			{
				Debug.Fail("Should not have null selection.");
				startOffset = endOffset = -1;
				return null;
			}
			ITsString tssQuote;
			startOffset = CurrentSelection.GetIch(SelectionHelper.SelLimitType.Top);
			endOffset = CurrentSelection.GetIch(SelectionHelper.SelLimitType.Bottom);
			CurrentSelection.Selection.GetSelectionString(out tssQuote, string.Empty);
			return GetCleanText(tssQuote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cleaned-up the specified text (no ORCS, chapter/verse numbers,
		/// or leading/trailing space).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString GetCleanText(ITsString tss)
		{
			// Remove chapter/verse numbers and footnotes from ITsString.
			List<string> stylesToRemove = new List<string>();
			stylesToRemove.Add(ScrStyleNames.ChapterNumber);
			stylesToRemove.Add(ScrStyleNames.VerseNumber);
			return StringUtils.RemoveORCsAndStylesFromTSS(tss, stylesToRemove, false,
				m_cache.LanguageWritingSystemFactoryAccessor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to find a complete word the selection corresponds to. If it is a range,
		/// then the first word in the range is returned. Before the word is returned, it
		/// is cleaned of ORCs, chapter/verse numbers, and leading/trailing spaces.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CleanSelectedWord
		{
			get
			{
				if (CurrentSelection == null || CurrentSelection.SelectedWord == null)
					return null;

				ITsString tssWord = GetCleanText(CurrentSelection.SelectedWord);
				return (tssWord == null ? null : tssWord.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an instance of the derived type of CmObject for an annotation.
		/// </summary>
		/// <remarks>REVIEW: This seems totally pointless since all the caller wants is a plain
		/// CmObject. All this really does is makes sure the target object is of one the types
		/// we consider valid.</remarks>
		/// <param name="hvo">The hvo of the object.</param>
		/// ------------------------------------------------------------------------------------
		CmObject GetNoteTargetObject(int hvo)
		{
			switch (m_cache.GetClassOfObject(hvo))
			{
				case CmTranslation.kclsidCmTranslation:
					return new CmTranslation(m_cache, hvo);
				case StTxtPara.kclsidStTxtPara:
					return new StTxtPara(m_cache, hvo);
				case CmIndirectAnnotation.kclsidCmIndirectAnnotation:
					return new CmIndirectAnnotation(m_cache, hvo);
					// JohnT: currently this is possible if the selection is in a verse number in a segmented BT.
					// That may stop being possible as we introduce translations of verse numbers.
				case CmBaseAnnotation.kclsidCmBaseAnnotation:
					return new CmBaseAnnotation(m_cache, hvo);
				case CmPicture.kclsidCmPicture:
					return new CmPicture(m_cache, hvo);
				default:
					Debug.Fail("Unexpected class of object: " + m_cache.GetClassOfObject(hvo));
					return null;
			}
		}
		#endregion

		#region "Focus-sharing" methods/property (for synchronizing with Paratext, TW, etc.)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sync handler (i.e. FocusMessageHandling).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FocusMessageHandling SyncHandler
		{
			get { return m_syncHandler; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes focus Windows messages.
		/// </summary>
		/// <param name="msg">The Windows Message to process.</param>
		/// ------------------------------------------------------------------------------------
		public void WndProc(Message msg)
		{
			if (m_syncHandler != null && msg.Msg == SantaFeFocusMessageHandler.FocusMsg)
				m_syncHandler.ReceiveFocusMessage(msg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables or disables this editing helper's ability to respond to synchronizing to
		/// the notes window or external applications (e.g. Libronix, Paratext, etc.). Each
		/// view has its own TeEditingHelper and this is called whenever the active view
		/// changes. When a view becomes active, this method of its editing helper is called
		/// to enable responding and vice versa when the view becomes inactive... in case it
		/// isn't obvious.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RespondToSyncScrollingMsgs(bool enable)
		{
			if (enable)
			{
				m_syncHandler.ReferenceChanged += ScrollToReference;
				m_syncHandler.AnnotationChanged += ScrollToCitedText;
				if (m_projectSettings.ReceiveSyncMessages)
					m_syncHandler.EnableLibronixLinking = true; // turn on the connection
			}
			else
			{
				m_syncHandler.ReferenceChanged -= ScrollToReference;
				m_syncHandler.AnnotationChanged -= ScrollToCitedText;
				m_syncHandler.EnableLibronixLinking = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls to reference.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="scrRef">The Scripture reference.</param>
		/// <param name="tssSelectedText">The selected TS String.</param>
		/// ------------------------------------------------------------------------------------
		private void ScrollToReference(object sender, ScrReference scrRef, ITsString tssSelectedText)
		{
			CheckDisposed();
			if (sender != this)
				SelectVerseText(scrRef, tssSelectedText, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls to cited text.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="annotation">The annotation whose reference and cited text we should
		/// sync. to.</param>
		/// ------------------------------------------------------------------------------------
		void ScrollToCitedText(object sender, IScrScriptureNote annotation)
		{
			CheckDisposed();
			if (sender != this && annotation != null)
				GoToScrScriptureNoteRef(annotation, false);
		}

		#endregion

		#region Information Bar methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Uses current selection to update information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetInformationBarForSelection()
		{
			CheckDisposed();

			int tagSelection;
			int hvoSelection;
			if (GetSelectedScrElement(out tagSelection, out hvoSelection))
				SetInformationBarForSelection(tagSelection, hvoSelection);
			else if (TheMainWnd != null)
				TheMainWnd.InformationBarText = string.Empty;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Update the text in a main window's caption bar based on the selection. If the
		/// selection is a scripture passage, the BCV reference is appended.
		/// </summary>
		/// <param name="tag">The flid of the selected element</param>
		/// <param name="hvoSel">The hvo of the selected element</param>
		/// -----------------------------------------------------------------------------------
		public void SetInformationBarForSelection(int tag, int hvoSel)
		{
			CheckDisposed();

			if (TheMainWnd == null || TheClientWnd == null)
				return;

			string sEditRef = null;
			try
			{
				sEditRef = GetPassageAsString(tag, hvoSel);
			}
			catch {}

			if (sEditRef == null)
				TheMainWnd.InformationBarText = TheClientWnd.BaseInfoBarCaption;
			else
			{
				TheMainWnd.InformationBarText = string.Format(
					TeResourceHelper.GetResourceString("kstidCaptionWRefFormat"),
					TheClientWnd.BaseInfoBarCaption, sEditRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use to update the GoTo Reference control that is part of the
		/// information toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateGotoPassageControl()
		{
			CheckDisposed();

			if (TheMainWnd == null || TheMainWnd.Mediator == null)
				return;

			if (BookIndex != -1)
			{
				try
				{
					ScrReference currStartRef = CurrentStartRef;
					if (currStartRef.Verse < 1)
						currStartRef.Verse = 1;
					if (currStartRef.Chapter < 1)
						currStartRef.Chapter = 1;

					TheMainWnd.Mediator.SendMessage("SelectedPassageChanged", currStartRef);
				}
				catch
				{
					// You can't update the GotoReferenceControl properly if there's no text
					// hanging around to make a selection.  Yeah.
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a string that describes the Scripture passage based on the selection.
		/// </summary>
		/// <param name="tag">The flid of the selected element</param>
		/// <param name="hvoSel">The hvo of the selected element, either a ScrSection (usually)
		/// or ScrBook (if in a title)</param>
		/// <returns>String that describes the Scripture passage or null if the selection
		/// can't be interpreted as a book and/or section reference.</returns>
		/// -----------------------------------------------------------------------------------
		public string GetPassageAsString(int tag, int hvoSel)
		{
			CheckDisposed();

			return GetPassageAsString(tag, hvoSel, false);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a string that describes the Scripture passage based on the selection.
		/// </summary>
		/// <param name="tag">The flid of the selected element</param>
		/// <param name="hvoSel">The hvo of the selected element, either a ScrSection (usually)
		/// or ScrBook (if in a title)</param>
		/// <param name="fSimpleFormat">Gets a simple, standardized reference (uses SIL 3-letter
		/// codes and no verse bridges)</param>
		/// <returns>String that describes the Scripture passage or null if the selection
		/// can't be interpreted as a book and/or section reference.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual string GetPassageAsString(int tag, int hvoSel, bool fSimpleFormat)
		{
			CheckDisposed();

			if (m_cache == null)
				return null;

			string sEditRef = null; // Title/reference/etc of text being edited in the draft pane
			switch (tag)
			{
				case (int)ScrSection.ScrSectionTags.kflidHeading:
				case (int)ScrSection.ScrSectionTags.kflidContent:
				{
					// ENHANCE TomB: might want to use low-level methods to get
					// cached values directly instead of creating the FDO objects
					// and having them reread the info from the DB. Also, may want
					// to cache the hvoSel in a method static variable so that when
					// the selection hasn't really changed to a new section or book,
					// we stop here.
					ScrSection section = new ScrSection(m_cache, hvoSel, false, false);
					BCVRef startRef;
					BCVRef endRef;
					section.GetDisplayRefs(out startRef, out endRef);
					if (fSimpleFormat)
						sEditRef = startRef.AsString;
					else
					{
						sEditRef = ScrReference.MakeReferenceString(section.OwningBook.BestUIName,
							startRef, endRef, m_scr.ChapterVerseSepr, m_scr.Bridge);
					}
					break;
				}
				case (int)ScrBook.ScrBookTags.kflidTitle:
				{
					ScrBook book = new ScrBook(m_cache, hvoSel, false, false);
					sEditRef = (fSimpleFormat ? (book.BookId + " 0:0") : book.BestUIName);
					break;
				}
				default:
					return null;
			}

			// Add the back translation writing system info to the output string, if needed
			if (IsBackTranslation)
			{
				LgWritingSystem ws = new LgWritingSystem(m_cache, ViewConstructorWS);
				sEditRef = string.Format(
					TeResourceHelper.GetResourceString("kstidCaptionInBackTrans"),
					sEditRef, ws.Name.UserDefaultWritingSystem);
			}

			return (sEditRef != null && sEditRef.Length != 0) ? sEditRef : null;
		}
		#endregion

		#region Back Translation methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the current section and generates a template for the back translation with
		/// corresponding chapter or verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void GenerateTranslationCVNumsForSection()
		{
			CheckDisposed();

			Debug.Assert(BookIndex != -1);
			Debug.Assert(SectionIndex != -1);

			ScrBook book = BookFilter.GetBook(BookIndex);
			IScrSection section = book.SectionsOS[SectionIndex];

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidGenerateSectionTemplate", out undo, out redo);
			using(new UndoTaskHelper(Callbacks.EditedRootBox.Site, undo, redo, true))
			{
				int wsTrans = RootVcDefaultWritingSystem;

				foreach(StTxtPara para in section.ContentOA.ParagraphsOS)
				{
					ICmTranslation trans = para.GetOrCreateBT();

					// If there is already text in the translation then skip this para
					if (trans.Translation.GetAlternative(wsTrans).Text != null)
						continue;

					// create the Chapter-Verse template in the translation
					ITsString tssTrans = GenerateTranslationCVNums(para.Contents.UnderlyingTsString,
						wsTrans);
					if (tssTrans != null)
						trans.Translation.GetAlternative(wsTrans).UnderlyingTsString = tssTrans;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an ITsString from a scripture paragraph, generate matching chapter and verse
		/// numbers in an ITsString for the translation.
		/// </summary>
		/// <param name="tssParentPara">content of the parent scripture paragraph</param>
		/// <param name="wsTrans">writing system of the translation</param>
		/// <returns>ITsString for the translation, or null if there are no chapter or verse
		/// numbers in the parent paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GenerateTranslationCVNums(ITsString tssParentPara, int wsTrans)
		{
			CheckDisposed();

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			bool PrevRunIsVerseNumber = false;

			for (int iRun = 0; iRun < tssParentPara.RunCount; iRun++)
			{
				string styleName = tssParentPara.get_Properties(iRun).GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);

				if (styleName == ScrStyleNames.ChapterNumber ||
					styleName == ScrStyleNames.VerseNumber)
				{
					if (styleName == ScrStyleNames.VerseNumber)
					{
						// Previous token is verse number, add space with default paragraph style
						if (PrevRunIsVerseNumber)
							AddRunToStrBldr(strBldr, " ", wsTrans, null);

						PrevRunIsVerseNumber = true;
					}
					else
						PrevRunIsVerseNumber = false;

					string number = ConvertVerseChapterNumForBT(tssParentPara.get_RunText(iRun));

					// Add corresponding chapter or verse number to string builder
					AddRunToStrBldr(strBldr, number, wsTrans, styleName);
				}
			}

			// return null if there are no chapter or verse numbers
			if (strBldr.Length == 0)
				return null;

			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vernVerseChapterText">Verse or chapter number text. This may be
		/// <c>null</c>.</param>
		/// <returns>The converted verse or chapter number, or empty string if
		/// <paramref name="vernVerseChapterText"/> is <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected string ConvertVerseChapterNumForBT(string vernVerseChapterText)
		{
			return m_scr.ConvertVerseChapterNumForBT(vernVerseChapterText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vernVerseChapterText">Verse or chapter number text. This may be
		/// <c>null</c>.</param>
		/// <param name="scr"></param>
		/// <returns>The converted verse or chapter number, or empty string if
		/// <paramref name="vernVerseChapterText"/> is <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public static string ConvertVerseChapterNumForBT(string vernVerseChapterText, Scripture scr)
		{
			if (vernVerseChapterText == null)
				return string.Empty; // empty verse/chapter number run.

			StringBuilder btVerseChapterText = new StringBuilder(vernVerseChapterText.Length);
			char baseChar = scr.UseScriptDigits ? (char)scr.ScriptDigitZero : '0';
			for (int i = 0; i < vernVerseChapterText.Length; i++)
			{
				if (char.IsDigit(vernVerseChapterText[i]))
				{
					btVerseChapterText.Append((char)('0' + (vernVerseChapterText[i] - baseChar)));
				}
				else
				{
					btVerseChapterText.Append(vernVerseChapterText[i]);
				}
			}
			return btVerseChapterText.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function appends a run to the given string builder
		/// </summary>
		/// <param name="strBldr"></param>
		/// <param name="text"></param>
		/// <param name="ws"></param>
		/// <param name="charStyle"></param>
		/// ------------------------------------------------------------------------------------
		private void AddRunToStrBldr(ITsStrBldr strBldr, string text, int ws, string charStyle)
		{
			strBldr.Replace(strBldr.Length, strBldr.Length, text,
				StyleUtils.CharStyleTextProps(charStyle, ws));
		}
		#endregion
	}

	#region Message filter class for inserting verses
	/// ------------------------------------------------------------------------------------
	/// <summary>Message filter for detecting events that may turn off
	/// the insert verse numbers mode</summary>
	/// ------------------------------------------------------------------------------------
	public class InsertVerseMessageFilter : IMessageFilter
	{
		/// <summary></summary>
		private Control m_control;
		private TeEditingHelper m_helper;
		private int m_cMessageTimeBomb;
		private bool m_fCanEndInsertVerseNumbers = true;
		private bool m_fInsertVerseInProgress = false;

		/// <summary>Constructor for filter object</summary>
		public InsertVerseMessageFilter(Control ctrl, TeEditingHelper helper)
		{
			m_control = ctrl;
			m_helper = helper;
			m_cMessageTimeBomb = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary></summary>
		/// <param name="m">message to be filtered</param>
		/// <returns>true if the message is consumed, false to pass it on.</returns>
		/// --------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			const int WM_TURNOFFINSERTVERSE = (int)Win32.WinMsgs.WM_APP + 0x123;
			const int CHAR_ESC = 0x001b;
			const int HTMENU = 0x05;

			//Debug.WriteLine("Msg: " + (Win32.WinMsgs)m.Msg);

			switch ((Win32.WinMsgs)m.Msg)
			{
				case Win32.WinMsgs.WM_SYSKEYDOWN:
				{
					// WM_SYSKEYDOWN will result from a system key that pops up a menu.
					// Popping up a menu will exit insert verse numbers mode.
					m_helper.EndInsertVerseNumbers();
					return false;
				}
				case Win32.WinMsgs.WM_COMMAND:
				{
					// WM_COMMAND results from clicking in the menu bar and selecting a menu
					// item. We need to catch WM_COMMAND rather than exiting insert verse
					// number mode on a WM_NCLCLICK so that we can stay in the mode on an
					// undo.
					//Debug.WriteLine("WM_COMMAND");
					Win32.PostMessage(m_control.Handle, WM_TURNOFFINSERTVERSE, 0, 0);
					return false;
				}
				case Win32.WinMsgs.WM_NCLBUTTONDOWN:
				case Win32.WinMsgs.WM_NCLBUTTONUP:
				{
					// Handle any non-client mouse left button activity.
					// Non-client areas include the title bar, menu bar, window borders,
					// and scroll bars.
					// Debug.WriteLine("NC LClick; hit-test value:" + m.WParam);
					if (m.WParam.ToInt32() == HTMENU)
					{
						// user clicked menu - handled when WM_COMMAND comes
						return false;
					}
					Control c = Control.FromHandle(m.HWnd);
					// Clicking on the scroll bar in the draft view should NOT exit verse
					// insert mode. Clicking on other non-client areas will exit the insert
					// verse numbers mode.
					if (c != m_control)
						m_helper.EndInsertVerseNumbers();
					return false;
				}
				case Win32.WinMsgs.WM_LBUTTONDOWN:
				case Win32.WinMsgs.WM_LBUTTONUP:
				{
					// Handle client area LEFT mouse click activity
					// If a down click is detected outside the draft view, then wait for the
					// up click message.  When the second message comes in then post a
					// message to turn off the insert verse mode.  The PostMessage will
					// allow the insert verse toolbar button a chance to handle the click
					// before turning off insert verse mode.
					//Debug.WriteLine("LClick");
					Control c = Control.FromHandle(m.HWnd);
					if (c != m_control)
					{
						if (++m_cMessageTimeBomb > 1)
						{
							// Debug.WriteLine("Time Bomb inc: " + m_cMessageTimeBomb);
							Win32.PostMessage(m_control.Handle, WM_TURNOFFINSERTVERSE, 0, 0);
						}
					}
					else if (m_cMessageTimeBomb > 0)
					{
						// Any second mouse click will turn off insert mode.  This can happen if the down
						// click was outside the draft view but the up click was in the draft view.
						m_helper.EndInsertVerseNumbers();
					}
					else if ((Win32.WinMsgs)m.Msg == Win32.WinMsgs.WM_LBUTTONUP && InsertVerseInProgress)
					{
						// Left Button up ends the current verse insertion
						InsertVerseInProgress = false;
					}
					return false;
				}
				case Win32.WinMsgs.WM_KEYDOWN:
				{
					// Handle key presses.
					//Debug.WriteLine("KeyDown: VK: " + ((int)m.WParam).ToString("x"));
					switch((int)m.WParam)
					{
							// Navigation keys will be passed through to allow
							// keyboard navigation during verse insert mode.
						case (int)Win32.VirtualKeycodes.VK_CONTROL:
						case (int)Win32.VirtualKeycodes.VK_PRIOR:
						case (int)Win32.VirtualKeycodes.VK_NEXT:
						case (int)Win32.VirtualKeycodes.VK_END:
						case (int)Win32.VirtualKeycodes.VK_HOME:
						case (int)Win32.VirtualKeycodes.VK_LEFT:
						case (int)Win32.VirtualKeycodes.VK_UP:
						case (int)Win32.VirtualKeycodes.VK_RIGHT:
						case (int)Win32.VirtualKeycodes.VK_DOWN:
							return false;

							// the escape key will be passed on and consumed in the WM_CHAR message
						case (int)Win32.VirtualKeycodes.VK_ESCAPE:
							return false;

							// other keys will terminate the insert verse mode
						default:
							Win32.PostMessage(m_control.Handle, WM_TURNOFFINSERTVERSE, 0, 0);
							return false;
					}
				}
				case Win32.WinMsgs.WM_CHAR:
				{
					// Handle WM_CHAR messages.  We only handle the ESC key at this point.
					//Debug.WriteLine("WM_CHAR: " + ((int)m.WParam).ToString("x"));
					m_helper.EndInsertVerseNumbers();
					// consume an escape key
					if ((int)m.WParam == CHAR_ESC)
						return true;

					// let all other characters pass through
					return false;
				}
				case Win32.WinMsgs.WM_RBUTTONDOWN:
				case Win32.WinMsgs.WM_RBUTTONUP:
				case Win32.WinMsgs.WM_RBUTTONDBLCLK:
				case Win32.WinMsgs.WM_MBUTTONDOWN:
				case Win32.WinMsgs.WM_MBUTTONUP:
				case Win32.WinMsgs.WM_NCRBUTTONDOWN:
				case Win32.WinMsgs.WM_NCMBUTTONUP:
					// Handle right or middle button mouse clicks in or out of the draft
					// view. This will end insert verse mode.
					m_helper.EndInsertVerseNumbers();
					return false;
				case Win32.WinMsgs.WM_MOUSEMOVE:
					// Mouse movements will be ignored if verse insert is in progress.  This
					// will prevent selections from being created
					return InsertVerseInProgress;
				default:
				{
					if (m.Msg == WM_TURNOFFINSERTVERSE)
					{
						// Handle the message that was posted to turn off insert verse mode.
						if (m_fCanEndInsertVerseNumbers)
							m_helper.EndInsertVerseNumbers();
						else
							m_fCanEndInsertVerseNumbers = true;
					}
					return false;
				}
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Prevents ending the insert verse numbers mode. This gets called
		/// if user clicks on Undo button and prevents exit from InsertVerseMode.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void PreventEndInsertVerseNumbers()
		{
			m_fCanEndInsertVerseNumbers = false;
			m_cMessageTimeBomb = 0;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Property indicating that verse number insert is in progress.  Mouse movements
		/// will be ignored so that selection is not created.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public bool InsertVerseInProgress
		{
			get {return m_fInsertVerseInProgress;}
			set {m_fInsertVerseInProgress = value;}
		}
	}
	#endregion

	/// --------------------------------------------------------------------------------
	/// <summary>
	/// Class for enabling the dialog to change multiple mis-spelled words to be invoked.
	/// </summary>
	/// --------------------------------------------------------------------------------
	public class ChangeSpellingInfo
	{
		string m_word; // the word(form) we want to change occurrences of.
		int m_ws;
		FdoCache m_cache;
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="word"></param>
		/// <param name="ws"></param>
		/// <param name="cache"></param>
		public ChangeSpellingInfo(string word, int ws, FdoCache cache)
		{
			m_word = word;
			m_cache = cache;
			m_ws = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the command...that is, run the change spelling dialog on the specified wordform.
		/// </summary>
		/// <param name="site">The rootsite.</param>
		/// ------------------------------------------------------------------------------------
		public void DoIt(IVwRootSite site)
		{
			IWfiWordform wf = null;
			using (new SuppressSubTasks(m_cache))
			{
				wf = WfiWordform.CreateFromDBObject(m_cache,
					WfiWordform.FindOrCreateWordform(m_cache, m_word, m_ws, true));
			}
			string stUndo, stRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidChangeSpellingItem", out stUndo,
				out stRedo);
			using (new UndoTaskHelper(site, stUndo, stRedo, false))
			{
				object respellerDlg = ReflectionHelper.CreateObject("MorphologyEditorDll.dll",
					"SIL.FieldWorks.XWorks.MorphologyEditor.RespellerDlg", new object[0]);
				try
				{
					ReflectionHelper.CallMethod(respellerDlg, "SetDlgInfo", new object[] { wf });
					ReflectionHelper.CallMethod(respellerDlg, "ShowDialog", new object[] { Form.ActiveForm });
					ReflectionHelper.CallMethod(respellerDlg, "SaveSettings", new object[] { });
				}
				finally
				{
					ReflectionHelper.CallMethod(respellerDlg, "Dispose", new object[] { });
				}
			}
		}
	}

	/// <summary>
	/// Interface implemented by TE rootsites to provide access to the VC.
	/// </summary>
	public interface IGetTeStVc
	{
		/// <summary>
		/// Get the Vc!
		/// </summary>
		TeStVc Vc { get; }
	}
}
