// Copyright (c) 2002-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.ComponentModel;
using System.Threading;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region BtFootnoteBldrInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Info needed for batching up footnote string builders when doing non-interleaved Back
	/// Translations
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct BtFootnoteBldrInfo
	{
		/// <summary>The writing system of the BT</summary>
		public readonly int ws;
		/// <summary>The builder containing the guts of the footnote</summary>
		public readonly ITsStrBldr bldr;
		/// <summary>The para style of the footnote para</summary>
		public readonly string styleId;
		/// <summary>The character offset where this footnote belongs in the "owning" para</summary>
		public readonly int ichOffset;
		/// <summary>The Scripture reference of the footnote</summary>
		public readonly BCVRef reference;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="bldr"></param>
		/// <param name="styleId"></param>
		/// <param name="ichOffset"></param>
		/// <param name="reference"></param>
		/// ------------------------------------------------------------------------------------
		public BtFootnoteBldrInfo(int ws, ITsStrBldr bldr, string styleId, int ichOffset,
			BCVRef reference)
		{
			this.ws = ws;
			this.bldr = bldr;
			this.styleId = styleId;
			this.ichOffset = ichOffset;
			this.reference = new BCVRef(reference);
		}
	}
	#endregion

	#region BTPictureInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains information about the back translation of a picture. These are accumulated
	/// for a paragraph and matched up to the pictures in the vernacular paragraph when
	/// the paragraph is completed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BTPictureInfo
	{
		/// <summary>Back translation text for the picture caption</summary>
		public ITsIncStrBldr m_strbldrCaption;
		/// <summary>Writing system for this back translation</summary>
		public int m_ws;
		/// <summary>Current Filename at the time the BT picture segment was encountered (if known).
		/// Used only for error reporting purposes</summary>
		public string m_filename;
		/// <summary>Current file line number at the time the BT picture segment was encountered (if known).
		/// Used only for error reporting purposes</summary>
		public int m_lineNumber;
		/// <summary>The BT picture segment (marker + segment text).
		/// Used only for error reporting purposes</summary>
		public string m_segment;
		/// <summary>Current Scripture reference at the time the BT picture segment was encountered.
		/// Used only for error reporting purposes</summary>
		public readonly BCVRef m_ref;
		/// <summary>Publishable information about the copyright that should appear on the
		/// copyright page of the publication.</summary>
		public string m_copyright;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="captionText">The BT caption text (can be null).</param>
		/// <param name="sCopyright">The BT copyright text (can be null).</param>
		/// <param name="ws">The writing system of the BT.</param>
		/// <param name="filename">The name of the file being imported.</param>
		/// <param name="lineNumber">The line number of the first BT segment encountered for
		/// this picture.</param>
		/// <param name="segment">The first BT segment encountered for this picture.</param>
		/// <param name="reference">The Scripture reference.</param>
		/// ------------------------------------------------------------------------------------
		public BTPictureInfo(string captionText, string sCopyright, int ws, string filename,
			int lineNumber, string segment, BCVRef reference)
		{
			m_strbldrCaption = TsIncStrBldrClass.Create();
			m_strbldrCaption.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			if (!String.IsNullOrEmpty(captionText))
				m_strbldrCaption.Append(captionText);
			m_copyright = sCopyright;
			m_ws = ws;
			m_filename = filename;
			m_lineNumber = lineNumber;
			m_segment = segment;
			m_ref = new BCVRef(reference);
		}
	}
	#endregion

	#region TeImporter class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class handles importing of Scripture, back translations, and notes.
	/// </summary>
	/// <remarks>Note: this class runs on a background thread. It can't call any UI methods
	/// directly!</remarks>
	/// ----------------------------------------------------------------------------------------
	public abstract class TeImporter : IFWDisposable
	{
		#region class ToolboxPictureInfo
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Simple class to hold picture parameters as they're being read in, with a few convenient
		/// methods for interpreting the data from the SF field.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public class ToolboxPictureInfo
		{
			/// <summary>String builder for the picture caption.</summary>
			public ITsIncStrBldr Caption;
			/// <summary>picture filename</summary>
			public string PictureFilename;
			/// <summary>Illustration description. This is not published (at least in printed publications).</summary>
			public Dictionary<int, string> Description;
			/// <summary>Indication of where in the column/page the picture is to be laid out.</summary>
			public string LayoutPos;
			/// <summary>Percentage by which picture is grown or shrunk.</summary>
			public string ScaleFactor;
			/// <summary>Indicates the type of data contained in LocationMin and LocationMax.</summary>
			public PictureLocationRangeType LocationRangeType = PictureLocationRangeType.AfterAnchor;
			/// <summary>The range of Scripture references or paragraphs in which this picture
			/// can be laid out.</summary>
			public string LocationRange;
			/// <summary>Publishable information about the copyright that should appear on the
			/// copyright page of the publication.</summary>
			public string Copyright;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the caption as a Ts String.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ITsString TssCaption
			{
				get { return (Caption == null) ? null : Caption.GetString(); }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether we have a Description specified for the given writing system.
			/// </summary>
			/// <param name="ws">The writing system ID.</param>
			/// --------------------------------------------------------------------------------
			public bool HasDescriptionForWs(int ws)
			{
				if (Description == null)
					return false;
				return Description.ContainsKey(ws);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Adds the description variant.
			/// </summary>
			/// <param name="description">The description.</param>
			/// <param name="ws">The ID of the writing system.</param>
			/// --------------------------------------------------------------------------------
			public void AddDescriptionVariant(string description, int ws)
			{
				Debug.Assert(!HasDescriptionForWs(ws));
				if (Description == null)
					Description = new Dictionary<int, string>();
				Description[ws] = description;
			}
		}
		#endregion

		#region Member Variables
		/// <summary>The cache</summary>
		protected FdoCache m_cache;
		/// <summary>Stylesheet used for importing</summary>
		protected FwStyleSheet m_styleSheet;

		/// <summary>The data model Scripture object</summary>
		protected IScripture m_scr;

		/// <summary>Info needed to undo, including the archived draft where we save existing books</summary>
		protected UndoImportManager m_undoManager;

		/// <summary>
		/// The description to use if the user wants to make a saved version.
		/// </summary>
		protected string m_savedVersionDescription;

		/// <summary>UI Callbacks</summary>
		protected TeImportUi m_importCallbacks;

		/// <summary>The writing system we use for all vernacular text.</summary>
		protected int m_wsVern;
		/// <summary>The writing system we use for all non-vernacular text.</summary>
		protected int m_wsAnal;
		/// <summary>
		/// Scripture Section Head paragraph style proxy
		/// </summary>
		protected ImportStyleProxy m_ScrSectionHeadParaProxy;
		/// <summary>
		/// Background Section Head paragraph style proxy
		/// </summary>
		protected ImportStyleProxy m_DefaultIntroSectionHeadParaProxy;
		/// <summary>
		/// Default Scripture paragraph style proxy
		/// </summary>
		protected ImportStyleProxy m_DefaultScrParaProxy;
		/// <summary>
		/// Default Background paragraph style proxy
		/// </summary>
		protected ImportStyleProxy m_DefaultIntroParaProxy;
		/// <summary>String builder to construct paragraph strings.</summary>
		protected StTxtParaBldr m_ParaBldr;
		/// <summary>String builders to construct back-trans paragraph strings.</summary>
		protected Dictionary<int, ITsStrBldr> m_BTStrBldrs = new Dictionary<int, ITsStrBldr>();
		/// <summary>
		/// Used to save and restore an "outer" string builder when creating "nested"
		/// paragraphs, such as footnotes.
		/// </summary>
		protected StTxtParaBldr m_SavedParaBldr;
		/// <summary>Gets set to true if a book is found during the import</summary>
		protected bool m_fFoundABook;
		/// <summary>Indicates whether the current section being worked on is an intro section</summary>
		protected bool m_fCurrentSectionIsIntro = true;
		// Status information as segements are imported
		/// <summary>We have begun processing actual Scripture text for the
		/// current book (as opposed to background materials, etc.) If true, the current
		/// section is a scripture text section. One implication of this distinction is that
		/// for Scripture sections, the caption bar will display the range of references.
		/// </summary>
		protected bool m_fInScriptureText;
		/// <summary>Currently processing a book title</summary>
		protected bool m_fInBookTitle;
		/// <summary>Currently processing a section heading</summary>
		protected bool m_fInSectionHeading;
		/// <summary>Currently processing a paragraph whose context is ContextValues.Text
		/// and structure is StructureValues.Body.</summary>
		protected bool m_fInVerseTextParagraph;
		/// <summary>Currently processing a footnote</summary>
		protected bool m_fInFootnote;
		/// <summary>The index of the next footnote to insert (for the current book)</summary>
		protected int m_iCurrFootnote;
		/// <summary>The current footnote; null if <see cref="m_fInFootnote"/> is both false.
		/// </summary>
		protected IScrFootnote m_CurrFootnote;
		/// <summary>Text props for chapter number character style</summary>
		protected ITsTextProps m_ttpChapterNumber;

		/// <summary>Previous book number (in a string)</summary>
		protected string m_sPrevBook;
		/// <summary>Current book number, based on cannonical order (e.g., Gen = 1)</summary>
		protected int m_nBookNumber;

		/// <summary>Book we are currently adding to</summary>
		protected IScrBook m_scrBook;
		/// <summary>ID of title</summary>
		protected IStText m_Title;
		/// <summary>Current section we are adding to</summary>
		protected IScrSection m_currSection;
		/// <summary>StText that is the section heading</summary>
		protected IStText m_sectionHeading;
		/// <summary>StText that is the section content</summary>
		protected IStText m_sectionContent;
		/// <summary>Reference of current segment</summary>
		protected BCVRef m_currentRef = new BCVRef();
		/// <summary>Last (previous) reference found (typically equal to m_currentRef only at the start of the import process)</summary>
		protected BCVRef m_prevRef = new BCVRef();
		/// <summary>Index of current section (0-based).</summary>
		protected int m_iCurrSection = -1;
		/// <summary>Information about the current picture being processed (for Toolbox-style markup)</summary>
		protected ToolboxPictureInfo m_currPictureInfo = null;
		/// <summary>Information about the current BT picture being processed (for Toolbox-style markup)</summary>
		protected BTPictureInfo m_currBtPictureInfo = null;

		/// <summary>the first imported reference</summary>
		protected ScrReference m_firstImportedRef = ScrReference.Empty;
		/// <summary>the set of annotations for the current book existing before import</summary>
		protected Dictionary<ScrNoteKey, IScrScriptureNote> m_existingAnnotations;

		private readonly ManualResetEvent m_pauseEvent = new ManualResetEvent(true);
		#endregion

		#region Methods shared by subclasses

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gives initial default for Saved Version description.
		/// Enhance JohnT: Maybe add some text, "Import"? or source name? Compare OXES override.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string DefaultSvDescription
		{
			get
			{
				return TeResourceHelper.GetResourceString("kstidStandardFormatImportSvDesc");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the message in the progress dialog box to reflect the book being imported
		/// </summary>
		/// <param name="stid">The resource id for the format string to use</param>
		/// ------------------------------------------------------------------------------------
		protected void UpdateProgressDlgForBook(string stid)
		{
			m_importCallbacks.StatusMessage = string.Format(TeResourceHelper.GetResourceString(stid),
				ScriptureServices.GetUiBookName(m_nBookNumber));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set everything up for processing a book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void StartBookTitle()
		{
			m_fInBookTitle = true;
			m_fInVerseTextParagraph = false;
			m_fInSectionHeading = false;

			// Blow away any existing title so that we don't append this one to the previous
			// one. This can happen if there is an intro book title, followed later by a real
			// book title. In this case, we don't need to keep both. The intro book title
			// can be generated for the view (or for publishing) from the book title. They
			// should always be the same.
			int cTitleParas = m_Title.ParagraphsOS.Count;
			for (int i = 0; i < cTitleParas; i++)
				m_Title.ParagraphsOS.RemoveAt(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets importer state variables for the new book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ResetStateVariablesForNewBook()
		{
			m_currSection = null;
			m_iCurrSection = -1;
			m_iCurrFootnote = 0;
			m_fInSectionHeading = false;
			m_fInScriptureText = false;
			m_fCurrentSectionIsIntro = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new book having the "canonical" book number m_nBookNumber. The new book is
		/// stored in the saved version. It also creates a title object for the book and puts
		/// he hvo of the title in m_hvoTitle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void PrepareToImportNewBook()
		{
			UpdateProgressDlgForBook("kstidImportingBook");
			m_scrBook = m_undoManager.AddNewBook(m_nBookNumber, SavedVersionDescription, out m_Title);
			ResetStateVariablesForNewBook();
			SetBookAnnotations();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description of the saved version; gets the default desciption if not set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string SavedVersionDescription
		{
			get
			{
				if (String.IsNullOrEmpty(m_savedVersionDescription))
					m_savedVersionDescription = DefaultSvDescription;
				return m_savedVersionDescription;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TeSfmImporter needs to do something for this, but TeXmlImporter doesn't.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void EndFootnote()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a section, with empty heading and contents.
		/// Sets the following members: m_currSection, m_hvoSectionHeading, m_hvoSectionContent
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeSection()
		{
			// Finalize the previous section, if there is one.
			FinalizePrevSection();

			m_currSection = m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateEmptySection(CurrentBook, ++m_iCurrSection);
			m_sectionHeading = m_currSection.HeadingOA;
			m_sectionContent = m_currSection.ContentOA;

			// Default new section refs to current segment's ref (or range)
			// in case the new section lacks verse number and verse text segments.
			m_currSection.VerseRefEnd = m_currentRef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizes section references and checks if the current section has any heading text
		/// and content. If not, a single blank paragraph is written for whatever is missing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void FinalizePrevSection()
		{
			if (m_currSection == null || !m_currSection.IsValidObject)
				return;

			int bcvRef = (m_firstImportedRef.IsEmpty) ? m_currSection.VerseRefMin :
				Math.Min(m_firstImportedRef, m_currSection.VerseRefMin);
			m_firstImportedRef = new ScrReference(bcvRef, m_scr.Versification);

			if (InMainImportDomain)
				return;

			// First, check if there is heading content. If not, add a blank paragraph.
			if (m_currSection.HeadingOA.ParagraphsOS.Count == 0)
			{
				StTxtParaBldr paraBldr = new StTxtParaBldr(m_cache);
					paraBldr.ParaStylePropsProxy =
						(m_fInScriptureText ? m_ScrSectionHeadParaProxy : m_DefaultIntroSectionHeadParaProxy);
					paraBldr.StringBuilder.SetIntPropValues(0, 0, (int)FwTextPropType.ktptWs,
						(int)FwTextPropVar.ktpvDefault, m_wsVern);
					paraBldr.CreateParagraph(m_sectionHeading);
				}

			// Now, check if there is content. If not, add a blank paragraph.
			if (m_currSection.ContentOA.ParagraphsOS.Count == 0)
			{
				StTxtParaBldr paraBldr = new StTxtParaBldr(m_cache);
					paraBldr.ParaStylePropsProxy =
						(m_fInScriptureText ? m_DefaultScrParaProxy : m_DefaultIntroParaProxy);
					paraBldr.StringBuilder.SetIntPropValues(0, 0, (int)FwTextPropType.ktptWs,
						(int)FwTextPropVar.ktpvDefault, m_wsVern);
					paraBldr.CreateParagraph(m_sectionContent);
				}

			m_fInScriptureText = !m_fCurrentSectionIsIntro;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the set of annotations that exist for the given book before importing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetBookAnnotations()
		{
			IFdoOwningSequence<IScrScriptureNote> curBookNotes =
				m_scr.BookAnnotationsOS[m_nBookNumber - 1].NotesOS;
			m_existingAnnotations =
				new Dictionary<ScrNoteKey, IScrScriptureNote>(curBookNotes.Count);
			foreach (IScrScriptureNote ann in curBookNotes)
			{
				if (!m_existingAnnotations.ContainsKey(ann.Key))
					m_existingAnnotations.Add(ann.Key, ann);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds or creates an annotation.
		/// </summary>
		/// <param name="info">The information about a annotation being imported.</param>
		/// <param name="annotatedObj">The annotated object (a book or paragraph).</param>
		/// <returns>
		/// The annotation (whether created or found)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected IScrScriptureNote FindOrCreateAnnotation(ScrAnnotationInfo info,
			ICmObject annotatedObj)
		{
			IScrScriptureNote ann;
			// If an identical note is not found...
			if (!m_existingAnnotations.TryGetValue(info.Key, out ann))
			{
				// insert this note.
				ann = m_undoManager.InsertNote(info.startReference, info.endReference,
					annotatedObj, GetAnnotDiscussionParaBldr(info), info.guidAnnotationType);

				ann.BeginOffset = ann.EndOffset = info.ichOffset;
				if (ann.CitedText != null)
					ann.EndOffset += ann.CitedText.Length;
			}

			return ann;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a paragraph builder for a simple, single-paragraph Scripture annotation
		/// discussion field. The base implementation just returns null, but this is virtual to
		/// allow subclasses to return something useful if they don't handle complex annotations.
		/// </summary>
		/// <param name="info">The information about a annotation being imported.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual StTxtParaBldr GetAnnotDiscussionParaBldr(ScrAnnotationInfo info)
		{
			return null;
		}

		/// <summary>
		/// Updates the previous book name.
		/// </summary>
		protected void StartingNewBook()
		{
			if (m_scrBook != null)
			{
				lock (this)
					m_sPrevBook = m_scrBook.BestUIName;
			}
		}

		/// <summary>
		/// Checks to see if a pause has been requested.
		/// </summary>
		protected void CheckPause()
		{
			m_pauseEvent.WaitOne();
		}
		#endregion

		/// <summary>
		/// Pauses the import.
		/// </summary>
		internal void Pause()
		{
			m_pauseEvent.Reset();
		}

		/// <summary>
		/// Resumes the import.
		/// </summary>
		internal void Resume()
		{
			m_pauseEvent.Set();
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current book being imported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal IScrBook CurrentBook
		{
			get { return m_scrBook; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the prev book. This property is called from TeImportUi.
		/// </summary>
		/// <value>The prev book.</value>
		/// ------------------------------------------------------------------------------------
		internal string PrevBook
		{
			get
			{
				lock (this)
					return m_sPrevBook;
			}
		}

		/// <summary>
		/// Check whether we're importing from the main import source.  Defaults to true.
		/// </summary>
		protected virtual bool InMainImportDomain
		{
			get { return true; }
		}
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		protected bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~TeImporter()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				var disposable = m_pauseEvent as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_ParaBldr = null;
			m_cache = null;
			m_styleSheet = null;
			m_scr = null;
			m_undoManager = null;
			m_importCallbacks = null;
			m_ScrSectionHeadParaProxy = null;
			m_DefaultIntroSectionHeadParaProxy = null;
			m_DefaultScrParaProxy = null;
			m_DefaultIntroParaProxy = null;
			m_BTStrBldrs = null;
			m_SavedParaBldr = null;
			m_CurrFootnote = null;
			m_ttpChapterNumber = null;
			m_sPrevBook = null;
			m_scrBook = null;
			m_currSection = null;

			m_isDisposed = true;
		}
		#endregion IDisposable & Co. implementation
	}
	#endregion

	#region TeSfmImporter class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This subclass handles importing from a Standard Format file (includes Paratext).
	/// </summary>
	/// <remarks>
	/// Note: this runs on a background thread. It can't call any UI methods directly!
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class TeSfmImporter : TeImporter
	{
		private static readonly char[] kControlCharacters = {'\u0000', '\u0001', '\u0002', '\u0003',
			'\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C',
			'\u000D', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015',
			'\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B','\u001C', '\u001D', '\u001E', '\u001F' } ;
		#region Member Variables
		private const int kMaxParaSizeForChapterBrk = 5000;
		private const int kMaxParaSizeForVerseAndPuncBrk = 20000;
		private const string kHardLineBreak = "\u2028";

		/// <summary>
		/// Settings object that represent the Paratext or SF settings for importing
		/// </summary>
		protected IScrImportSet m_settings;
		/// <summary>Wrapper object for TE, Paratext scripture objects. Normally use the
		/// SOWrapper property to access this.</summary>
		protected ScrObjWrapper m_SOWrapper;

		/// <summary>Text contents of the current segment.</summary>
		protected string m_sSegmentText;
		/// <summary>The marker of the tag (includes backslash).</summary>
		protected string m_sMarker;
		/// <summary>The begin marker used to open the current character style (if any).</summary>
		protected string m_sCharStyleBeginMarker;
		/// <summary>The begin marker used to open the current footnote (if any).</summary>
		protected string m_sFootnoteBeginMarker;
		/// <summary>The style proxy to be used for the processing of current segment</summary>
		protected ImportStyleProxy m_styleProxy;
		/// <summary>The context of the current segment</summary>
		protected ContextValues m_context;
		/// <summary>The style proxy used for processing previous paragraph segment</summary>
		protected ImportStyleProxy m_vernParaStyleProxy;
		/// <summary>The current domain (unchanged by in-line markers)</summary>
		protected MarkerDomain m_currDomain;

		/// <summary>Vernacular Text Properties.</summary>
		protected ITsTextProps m_vernTextProps;
		/// <summary>Analysis Text Properties.</summary>
		protected ITsTextProps m_analTextProps;
		/// <summary>The writing system used by the current paragraph segment.</summary>
		protected int m_wsPara = 0;

		/// <summary>Dictionary of string (import marker/tag) to import style proxy. For end markers,
		/// the key is EndMarker\UFFFBeginMarker.</summary>
		protected Dictionary<string, ImportStyleProxy> m_styleProxies = new Dictionary<string, ImportStyleProxy>();
		/// <summary>List of markers that we encounter during import that did not have mappings
		/// (currently we create mappings on the fly, but if the fly complains too much we might change that)</summary>
		protected List<string> m_unmappedMarkers = new List<string>();
		/// <summary>Dictionary of string (import marker/tag) to import style proxy used for
		/// Annotations domain.  For end markers, the key is EndMarker\UFFFBeginMarker.</summary>
		protected Dictionary<string, ImportStyleProxy> m_notesStyleProxies = new Dictionary<string, ImportStyleProxy>();

		/// <summary>The last paragraph that was created</summary>
		protected IScrTxtPara m_lastPara;

		// Special paragraph style proxies, used as fallbacks in case import data lacks a
		//   para style:
		/// <summary>
		/// Book Title paragraph style proxy
		/// </summary>
		protected ImportStyleProxy m_BookTitleParaProxy;
		/// <summary>Default Footnote paragraph style proxy</summary>
		protected ImportStyleProxy m_DefaultFootnoteParaProxy;
		/// <summary>Default Annotation style proxy</summary>
		protected ImportStyleProxy m_DefaultAnnotationStyleProxy;

		/// <summary>Factory for creating TsStrings.</summary>
		protected ITsStrFactory m_TsStringFactory = TsStrFactoryClass.Create();
		/// <summary>The writing system of the current back trans para being processed --
		/// needed to be able to stick BT character runs into the proper BT para</summary>
		protected int m_wsCurrBtPara;
		/// <summary>
		/// String builder for creating back translations of footnotes
		/// </summary>
		protected ITsStrBldr m_BTFootnoteStrBldr;
		/// <summary>
		/// List of pictures in current paragraph in the vernacular
		/// </summary>
		protected List<ICmPicture> m_CurrParaPictures = new List<ICmPicture>();
		/// <summary>
		/// List of footnotes in current paragraph
		/// </summary>
		protected List<FootnoteInfo> m_CurrParaFootnotes = new List<FootnoteInfo>();
		/// <summary>
		/// List of accumulated picture caption back translations for the current paragraph
		/// </summary>
		protected List<BTPictureInfo> m_BTPendingPictures = new List<BTPictureInfo>();
		/// <summary>
		/// Footnote whose BT is currently being imported. Null whenever a footnote BT is not being imported.
		/// Used only for interleaved Back Translations.
		/// </summary>
		protected IStFootnote m_CurrBTFootnote;
		/// <summary>
		/// This is the style of the current BT footnote para. Undefined if we don't happen to be processing
		/// a BT of a footnote.
		/// </summary>
		string m_sBtFootnoteParaStyle;
		/// <summary>
		/// Collection of <see cref="BtFootnoteBldrInfo"/> thingies for Back Translation footnotes.
		/// Used only for non-interleaved Back Translations.
		/// </summary>
		protected List<BtFootnoteBldrInfo> m_BtFootnoteStrBldrs = new List<BtFootnoteBldrInfo>();
		/// <summary>
		/// Collection of ScrAnnotationInfo objects with information about
		/// imported notes that are waiting to be merged.
		/// </summary>
		protected List<ScrAnnotationInfo> m_PendingAnnotations = new List<ScrAnnotationInfo>();
		/// <summary>
		/// Dictionary of indexes to the current footnote in the back translation(s) (one entry for
		/// each WS).
		/// </summary>
		protected Dictionary<int, int> m_BTfootnoteIndex = new Dictionary<int, int>();
		/// <summary>
		/// If true, this member indicates that when we encounter a footnote, we should attempt to interpret
		/// it as a USFM-style footnote and set the database settings accordingly.
		/// </summary>
		protected bool m_fInterpretFootnoteSettings;
		/// <summary>True if chapter number is ready to be inserted at a proper location.</summary>
		protected bool m_fChapterNumberPending;
		/// <summary>Domain of the current import stream. Main for the primary Scripture
		/// stream or if the BT and/or notes segment is being imported interleaved with
		/// the vernacular data stream.</summary>
		protected ImportDomain m_importDomain = ImportDomain.Main;
		/// <summary>The HVO of the writing system of the import stream of the previous marker.</summary>
		protected int m_wsOfPrevImportStream = -1;
		/// <summary>The domain of the import stream of the previous marker.</summary>
		protected ImportDomain m_prevImportDomain = ImportDomain.Main;
		/// <summary>True if verse number is ready to be inserted into the back translation(s).</summary>
		protected bool m_fBackTransVerseNumPending;
		/// <summary>List of BTs which have already had the "pending" verse number added. This is
		/// cleared every time m_fBackTransVerseNumPending is set to true.</summary>
		protected List<int> m_btPendingVerseNumAdded = new List<int>();
		/// <summary>True if chapter number is ready to be inserted into the back translation(s).</summary>
		protected bool m_fBackTransChapterNumPending;
		/// <summary>List of BTs which have already had the "pending" chapter number added. This is
		/// cleared every time m_fBackTransChapterNumPending is set to true.</summary>
		protected List<int> m_btPendingChapterNumAdded = new List<int>();
		/// <summary>In a char style with an end marker</summary>
		protected bool m_fInCharStyle;
		/// <summary>If processing a footnote, this tells whether we've hit a Note Marker segment</summary>
		protected bool m_fGotFootnoteMarker;
		/// <summary>End marker for current character style</summary>
		protected string m_sCharStyleEndMarker;
		/// <summary>End marker for current footnote</summary>
		protected string m_sFootnoteEndMarker;
#pragma warning disable 414
		/// <summary>Previous book number, based on cannonical order (or -1 for first book)</summary>
		private int m_nPrevBookNumber = -1;
#pragma warning restore 414
		/// <summary>The book number to skip (or -1 if none so far)</summary>
		private int m_nSkipBookNumber = -1;

		/// <summary>Current chapter number, based on actual text in incoming data</summary>
		protected int m_nChapter = 1; //default to 1 to provide chapter 1 ref for intro matl

		private bool m_foundAChapter;

		/// <summary>Index of next BT paragraph to locate (0-based).</summary>
		protected int m_iNextBtPara;
		/// <summary>Annotation type for translator notes</summary>
		private ICmAnnotationDefn m_scrTranslatorAnnotationDef;

		/// <summary>cached copy of the ICU character properties</summary>
		private ILgCharacterPropertyEngine m_cpe;

		/// <summary> segment to be evaluated to determine whether it is a marker or footnote
		/// text (see TE-5002)</summary>
		private string m_sPendingFootnoteText;

		// True if we are importing (typically BT only) to the main, current version of Scripture,
		// as opposed to the typical case of importing to an archive.
		private bool m_fImportingToMain;
		#endregion

		#region Import, the only public (static) method (2 overloads)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This static method imports Scripture (This overload used only for testing w/o merge
		/// or overwrite dialog.)
		/// </summary>
		/// <param name="settings">Import settings object (filled in by wizard)</param>
		/// <param name="cache">The cache used to import to and get misc. info. from.</param>
		/// <param name="styleSheet">Stylesheet from which to get scripture styles.</param>
		/// <param name="undoManager">The undo import manager (which is responsible for creating
		/// and maintaining the archive of original books being overwritten and maintaining
		/// the book filter).</param>
		/// <returns>The reference of the first thing that was imported</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrReference Import(IScrImportSet settings, FdoCache cache,
			FwStyleSheet styleSheet, UndoImportManager undoManager)
		{
			return Import(settings, cache, styleSheet, undoManager, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call this static method to Import Scripture
		/// </summary>
		/// <param name="settings">Import settings object (filled in by wizard)</param>
		/// <param name="cache">The cache used to import to and get misc. info. from.</param>
		/// <param name="styleSheet">Stylesheet from which to get scripture styles.</param>
		/// <param name="undoManager">The undo import manager (which is responsible for creating
		/// and maintaining the archive of original books being overwritten and maintaining
		/// the book filter).</param>
		/// <param name="importCallbacks">UI callbacks</param>
		/// <returns>
		/// The Scripture reference of the first thing that was imported
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static ScrReference Import(IScrImportSet settings, FdoCache cache,
			FwStyleSheet styleSheet, UndoImportManager undoManager, TeImportUi importCallbacks)
		{
			using (TeSfmImporter importer = new TeSfmImporter(settings, cache, styleSheet, undoManager,
				importCallbacks))
			{
				importer.Import();
				return importer.m_firstImportedRef;
			}	// Dispose() releases any hold on ICU character properties.
		}
		#endregion

		#region Constructor (protected)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the TeSfmImporter class
		/// </summary>
		/// <param name="settings">Import settings object (filled in by wizard)</param>
		/// <param name="cache">The cache used to import to and get misc. info. from.</param>
		/// <param name="styleSheet">Stylesheet from which to get scripture styles.</param>
		/// <param name="undoManager">The undo import manager(which is responsible for creating
		/// and maintainging the archive of original books being overwritten).</param>
		/// <param name="importCallbacks">UI callbacks</param>
		/// ------------------------------------------------------------------------------------
		protected TeSfmImporter(IScrImportSet settings, FdoCache cache,
			FwStyleSheet styleSheet, UndoImportManager undoManager, TeImportUi importCallbacks)
		{
			Debug.Assert(cache != null);
			Debug.Assert(styleSheet != null);

			m_settings = settings;
			m_cache = cache;
			m_styleSheet = styleSheet;
			m_undoManager = undoManager;
			m_importCallbacks = importCallbacks;
			m_importCallbacks.Importer = this;

			Debug.Assert(m_settings.BasicSettingsExist);
			// ENHANCE (TomB): Make it possible to start importing in the middle
			// of a book.
			m_settings.StartRef = new BCVRef(m_settings.StartRef.Book, 1, 0);
		}
		#endregion

		#region Internal (protected) methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the actual workhorse to Import Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Import()
		{
			DateTime beg = DateTime.Now;
			try
			{
				try
				{
					Initialize();
					m_fFoundABook = false;

					// Get and process each scripture segment
					string sText, sMarker;
					ImportDomain domain;
					while (SOWrapper.GetNextSegment(out sText, out sMarker, out domain))
					{
						CheckPause();
						//				Trace.WriteLine(sMarker + " " + sText);
						m_sSegmentText = RemoveControlCharacters(sText);
						m_sMarker = sMarker;
						m_importDomain = domain;
						ProcessSegment();
					}
				}
				catch (EncodingConverterException e)
				{
					m_importCallbacks.ErrorMessage(e);
					StopImport();
				}

				// If no book was imported then tell the user that the books they specified didn't
				// import properly.
				if (!m_fFoundABook)
				{
					if (!MiscUtils.RunningTests) // If we're not running the tests
					{
						string message = string.Format(TeResourceHelper.GetResourceString(
							"kstidImportNoBookError"),
							m_settings.StartRef.AsString,
							m_settings.EndRef.AsString);
						m_importCallbacks.ErrorMessage(message);
					}
					else
					{
						throw new ArgumentException("Found no book to import");
					}
				}

				FinalizeImport();
			}
			catch
			{
				try
				{
					FinalizeImport();
				}
// ReSharper disable EmptyGeneralCatchClause Justification: want to throw the exception from the outer try/catch block
				catch {}
// ReSharper restore EmptyGeneralCatchClause
				throw;
			}
			finally
			{
				m_importCallbacks.Position = m_importCallbacks.Maximum;
				if (SOWrapper != null)
					SOWrapper.Cleanup();
			}
			Debug.WriteLine("import time: " + (DateTime.Now - beg));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove control characters from the given string, and return the result.
		/// Tab characters are replaced by a space.
		/// Need to use this because control characters are not valid in XML unless they are quoted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string RemoveControlCharacters(string sText)
		{
			if (sText.IndexOfAny(kControlCharacters) == -1)
				return sText;

			StringBuilder bldr = new StringBuilder(sText.Length);
			foreach (var ch in sText)
			{
				if (ch >= ' ')
					bldr.Append(ch);
				else if (ch == '\u0009')
					bldr.Append(' ');
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the scripture importer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Initialize()
		{
			m_wsAnal = m_cache.DefaultAnalWs;
			m_wsVern = m_cache.DefaultVernWs;
			m_wsPara = m_cache.DefaultVernWs;

			m_scr = m_cache.LangProject.TranslatedScriptureOA;

			InitInterpretFootnoteSettings();

			// ENHANCE (TomB): Might want to make it possible to end importing in the middle
			// of a book someday.
			ScrReference endRef = new ScrReference(m_settings.EndRef, m_scr.Versification);
			ScrReference startRef = new ScrReference(m_settings.StartRef, m_scr.Versification);
			m_nBookNumber = m_settings.StartRef.Book;
			m_settings.EndRef = endRef = endRef.LastReferenceForBook;


			// Initialize scripture object
			InitScriptureObject();

			// Load the scripture text project & enum
			LoadScriptureProject();

			// Display progress if one was supplied
			if (m_importCallbacks.IsDisplayingUi)
			{
				int cChapters = ScrReference.GetNumberOfChaptersInRange(SOWrapper.BooksPresent,
					startRef, endRef);

				int nMax = m_settings.ImportTranslation ? cChapters : 0;
				if (SOWrapper.HasNonInterleavedBT && m_settings.ImportBackTranslation)
					nMax += cChapters;
				if (SOWrapper.HasNonInterleavedNotes && m_settings.ImportAnnotations)
					nMax += cChapters;

				m_importCallbacks.Maximum = nMax;
			}

			// Init our set of style proxies
			LoadImportMappingProxies();

			// Init member vars special paragraph style proxies, used as fallbacks in case
			// import data lacks a paragraph style.
			// For now we always use the default vernacular writing system. This may change
			// when we are able to import paratext project proxies with multiple
			// domains (vern, back transl, notes)
			m_BookTitleParaProxy = new ImportStyleProxy(ScrStyleNames.MainBookTitle,
				StyleType.kstParagraph, m_wsVern, ContextValues.Title, m_styleSheet);
			Debug.Assert(m_BookTitleParaProxy.Context == ContextValues.Title);

			m_ScrSectionHeadParaProxy = new ImportStyleProxy(ScrStyleNames.SectionHead,
				StyleType.kstParagraph, m_wsVern, ContextValues.Text, m_styleSheet);
			m_DefaultIntroSectionHeadParaProxy = new ImportStyleProxy(ScrStyleNames.IntroSectionHead,
				StyleType.kstParagraph, m_wsVern, ContextValues.Intro, m_styleSheet);

			m_DefaultScrParaProxy = new ImportStyleProxy(ScrStyleNames.NormalParagraph,
				StyleType.kstParagraph, m_wsVern, ContextValues.Text, m_styleSheet);

			m_DefaultIntroParaProxy = new ImportStyleProxy(ScrStyleNames.IntroParagraph,
				StyleType.kstParagraph, m_wsVern, ContextValues.Intro, m_styleSheet);

			m_DefaultFootnoteParaProxy = new ImportStyleProxy(ScrStyleNames.NormalFootnoteParagraph,
				StyleType.kstParagraph, m_wsVern, ContextValues.Note, m_styleSheet);

			m_DefaultAnnotationStyleProxy = new ImportStyleProxy(ScrStyleNames.Remark,
				StyleType.kstParagraph, m_wsAnal, ContextValues.Annotation, m_styleSheet);

			// Make a paragraph builder. We will keep re-using this every time we build a paragraph.
			m_ParaBldr = new StTxtParaBldr(m_cache);

			// Handle the case where the very first marker (after the \id line) is a
			// character style.
			m_ParaBldr.ParaStylePropsProxy = m_DefaultIntroParaProxy;

			// Build generic character props for use with different runs of text and analysis
			// character properties
			ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
			// analysis character properties
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsAnal);
			m_analTextProps = tsPropsBldr.GetTextProps();
			// vernacular character properties
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsVern);
			m_vernTextProps = tsPropsBldr.GetTextProps();

			// Get a reference to the annotation definition of translator notes (to use as default note type)
			m_scrTranslatorAnnotationDef =
				m_cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(CmAnnotationDefnTags.kguidAnnTranslatorNote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the m_fInterpretFootnoteSettings flag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitInterpretFootnoteSettings()
		{
			m_fInterpretFootnoteSettings = true;
			if (!m_scr.HasDefaultFootnoteSettings)
			{
				m_fInterpretFootnoteSettings = false;
			}
			else
			{
				foreach (IScrBook book in m_scr.ScriptureBooksOS)
					if (book.FootnotesOS.Count > 0)
						m_fInterpretFootnoteSettings = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the scripture object wrapper.
		/// </summary>
		/// <remarks>Virtual for testing override purposes.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitScriptureObject()
		{
			SOWrapper = new ScrObjWrapper();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the scripture project and enumerator, preparing us to read the data files.
		/// </summary>
		/// <remarks>Virtual for testing override purposes.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadScriptureProject()
		{
			SOWrapper.LoadScriptureProject(m_settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throw an exception to end the import process.
		/// </summary>
		/// <exception cref="CancelException">If not skipping the current book; this will cause
		/// the import to roll back the current book, leaving the last whole book (if any)
		/// imported.</exception>
		/// ------------------------------------------------------------------------------------
		protected void StopImport()
		{
			throw new CancelException("Import canceled by user.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether the previous run is Verse Number.
		/// </summary>
		/// <param name="bldr">string builder</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool PrevRunIsVerseNumber(ITsStrBldr bldr)
		{
			CheckDisposed();

			if (bldr == null)
				return false;

			if (bldr.Length == 0) // Nothing has been added to builder yet
				return false;

			ITsTextProps propsOfLastRun = bldr.get_Properties(bldr.RunCount - 1);

			return (propsOfLastRun.GetStrPropValue((int)FwTextPropType.ktptNamedStyle)
				== ScrStyleNames.VerseNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process this scripture text segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ProcessSegment()
		{
			if (m_importCallbacks.CancelImport)
				StopImport();

			m_styleProxy = GetStyleProxy();

			// Excluded markers are thrown away, but if it is an inline marker, we
			// need to look for the end marker.
			if (m_styleProxy.Excluded ||
				(m_styleProxy.Function == FunctionValues.StanzaBreak &&
				(m_vernParaStyleProxy == null ||
				m_vernParaStyleProxy.Function != FunctionValues.Line)))
			{
				// REVIEW: Should we call SetInCharStyle here?
				if (m_styleProxy.EndMarker != null)
					m_fInCharStyle = true; // treat this like a character style
				return;
			}

			// Confirm that we have a book at this point if we are ready to start importing data or
			// that we are currently processing the \id line to get a book
			// Needed to add a specific check to allow annotations when there isn't a book since new
			// books are imported to saved versions and won't be found.
			if (m_styleProxy.Context == ContextValues.Book)
			{
				// processing /id line
			}
			else if (m_nBookNumber == 0 && m_importDomain == ImportDomain.Annotations)
			{
				throw new ScriptureUtilsException(SUE_ErrorCode.UnexcludedDataBeforeIdLine,
					SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber, m_sMarker + " " + m_sSegmentText,
					string.Empty, string.Empty, string.Empty);
			}
			else if (m_scrBook == null && m_settings.ImportTranslation && m_settings.ImportBackTranslation)
			{
				throw new ScriptureUtilsException(SUE_ErrorCode.UnexcludedDataBeforeIdLine,
					SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber, m_sMarker + " " + m_sSegmentText,
					string.Empty, string.Empty, string.Empty);
			}

			if (HandleSpecialTargets())
				return;

			CheckForPendingFootnoteText();

			#region Footnote Target Refs
			if (m_styleProxy.StyleId == ScrStyleNames.FootnoteTargetRef)
			{
				if (m_fInFootnote && m_CurrFootnote != null && m_fInterpretFootnoteSettings)
				{
					Debug.Assert(m_ParaBldr != null);
					ImportStyleProxy proxy = (ImportStyleProxy)m_ParaBldr.ParaStylePropsProxy;
					m_scr.SetDisplayFootnoteReference(proxy.StyleId, true);
				}
				// else... TODO: Maybe write an error out to the error log (if we had one)
				//				if (m_styleProxy.EndMarker != null)
				//				{
				//				}
				SetInCharacterStyle(); // treat this like a character style
				return;
			}
			#endregion

			GetSegmentInfo();

			// Assume we need to insert the text at the end of this method
			bool fInsertSegment = true;

			if (HandleStartOfBook() || SkipThisBook)
				return;

			// Handle the case of non-interleaved and interleaved notes.
			if (m_importDomain == ImportDomain.Annotations || m_currDomain == MarkerDomain.Note)
			{
				HandleNoteDomain();
				return;
			}

			if (SkipIntroMaterial())
				return;

			#region Back translation domain handling
			// Handle the case of non-interleaved and interleaved back translations.
			if (m_importDomain == ImportDomain.BackTrans || (m_currDomain & MarkerDomain.BackTrans) != 0)
			{
				ProcessBackTransSegment();
				return;
			}
			if (!m_settings.ImportTranslation)
			{
				if (!m_fInScriptureText && m_styleProxy.Context == ContextValues.Text &&
					m_styleProxy.Structure == StructureValues.Body)
				{
					PrepareForFirstScriptureSection();
					m_iNextBtPara = 0;
				}
				else
				{
					if (ProcessingParagraphStart)
					{
						FinalizePrevParagraph();
						if (m_vernParaStyleProxy == null || m_vernParaStyleProxy.Structure != m_styleProxy.Structure)
							m_iNextBtPara = 0;
					}
					HandleSectionHead(ref fInsertSegment);
				}
			}
			// Remember the paragraph style for future BT processing.
			if (ProcessingParagraphStart)
			{
				m_vernParaStyleProxy = (m_styleProxy.StyleType == StyleType.kstParagraph) ?
					m_styleProxy : m_BookTitleParaProxy;
				m_fBackTransVerseNumPending = false;
			}
			#endregion

			#region End Marker
			if (m_context == ContextValues.EndMarker)
			{
				if (ProcessEndMarker())
					return;
			}
			else
			{
				// This segment implicitly terminates any char style run
				EndCharStyle();
			}
			#endregion

			#region Start of Chapter
			if (m_styleProxy.Function == FunctionValues.Chapter)
			{
				ProcessStartOfChapter();
				// don't insert this segment into the body text because it's already been handled.
				fInsertSegment = false;
			}
			#endregion

			if (m_settings.ImportTranslation)
			{
				HandleBookTitle(ref fInsertSegment);
				HandleSectionHead(ref fInsertSegment);
				HandleFootnotes(ref fInsertSegment);

				#region Make an Implicit Scripture Section if necessary
				// Prepare for the switch from book title or background material to real
				// "Scripture" text.
				if (!m_fInScriptureText && m_styleProxy.Context == ContextValues.Text &&
					m_styleProxy.Structure == StructureValues.Body)
				{
					PrepareForFirstScriptureSection();
				}
				#endregion
			}

			bool isVerse = HandleVerseNumbers();

			if (!m_settings.ImportTranslation)
			{
				if (m_styleProxy.StyleType == StyleType.kstCharacter)
					SetInCharacterStyle();
				return;
			}

			if (!isVerse)
				HandleOtherParagraphStyles();

			if (fInsertSegment)
				InsertSegment();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the state for a character style run. This can be a real character style or
		/// a special target (e.g., Footnote Target Ref) that is treated as a char style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetInCharacterStyle()
		{
			m_fInCharStyle = true;
			m_sCharStyleEndMarker = m_styleProxy.EndMarker;
			if (!string.IsNullOrEmpty(m_sCharStyleEndMarker))
				m_sCharStyleBeginMarker = m_sMarker;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the character style run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EndCharStyle()
		{
			m_fInCharStyle = false;
			m_sCharStyleEndMarker = null;
			m_sCharStyleBeginMarker = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the state for a footnote run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetInFootnote()
		{
			m_fInFootnote = true;
			m_fGotFootnoteMarker = false;
			m_sFootnoteEndMarker = m_styleProxy.EndMarker;
			if (!string.IsNullOrEmpty(m_sFootnoteEndMarker))
				m_sFootnoteBeginMarker = m_sMarker;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for pending footnote text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckForPendingFootnoteText()
		{
			if (m_sPendingFootnoteText != null)
			{
				if ((m_currDomain != MarkerDomain.Footnote || m_sMarker != m_sFootnoteEndMarker) &&
					m_styleProxy.Context != ContextValues.Text)
				{
					if (m_fInterpretFootnoteSettings && m_sPendingFootnoteText.Trim().Length == 1)
					{
						m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
						m_scr.FootnoteMarkerSymbol = m_sPendingFootnoteText.Trim();
					}
					// Discard the data if we aren't interpreting the footnote settings
				}
				else
				{
					AddTextToPara(m_sPendingFootnoteText);
				}
				m_sPendingFootnoteText = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a style proxy for processing a segment
		/// </summary>
		/// <returns>the style proxy</returns>
		/// ------------------------------------------------------------------------------------
		private ImportStyleProxy GetStyleProxy()
		{
			Dictionary<string, ImportStyleProxy> styleProxies =
				(m_importDomain == ImportDomain.Annotations ? m_notesStyleProxies : m_styleProxies);

			ImportStyleProxy proxy;
			if (styleProxies.TryGetValue(m_sMarker, out proxy))
				return proxy;
			if (m_fInCharStyle && styleProxies.TryGetValue(m_sMarker + "\uFEFF" + m_sCharStyleBeginMarker, out proxy))
				return proxy; // Character style proxy
			if (m_fInFootnote && styleProxies.TryGetValue(m_sMarker + "\uFEFF" + m_sFootnoteBeginMarker, out proxy))
				return proxy; // Footnote proxy

			// Didn't find a proxy for the marker, so create a new one based off the marker
			// and the current import domain
			MarkerDomain domain = MarkerDomain.Default;
			switch (m_importDomain)
			{
				case ImportDomain.Main:
					domain = MarkerDomain.Default;
					break;
				case ImportDomain.BackTrans:
					domain = MarkerDomain.BackTrans;
					break;
				case ImportDomain.Annotations:
					domain = MarkerDomain.Note;
					break;
			}

			m_unmappedMarkers.Add(m_sMarker);
			return styleProxies[m_sMarker] = new ImportStyleProxy(
				m_sMarker, StyleType.kstParagraph, GetWsForImportDomain(),
				m_fInScriptureText ? ContextValues.Text : ContextValues.Intro,
				domain, m_styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this segment is a special target then handle it
		/// </summary>
		/// <returns>true if the segment was a special target</returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleSpecialTargets()
		{
			bool fFoundSpecialTarget = false;
			bool fProcessingPicture = false;
			switch(m_styleProxy.MappingTarget)
			{
				case MappingTargetType.TEStyle:
					// TEStyle is not a special target
					break;

				case MappingTargetType.Figure:
					m_sSegmentText = EnsurePictureFilePathIsRooted(m_sSegmentText);
					if (m_importDomain == ImportDomain.BackTrans ||
						(m_styleProxy.Domain & MarkerDomain.BackTrans) != 0)
					{
						// Save information about this picture caption back translation which will
						// be added at the end of the paragraph.
						m_BTPendingPictures.Add(new BTPictureInfo(m_sSegmentText, null, BackTransWS,
							SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber,
							m_sMarker + " " + m_sSegmentText, m_currentRef));
					}
					else
					{
						try
						{
							if (m_settings.ImportTranslation)
								InsertPicture();
						}
						catch (ArgumentException e)
						{
							throw new ScriptureUtilsException(SUE_ErrorCode.InvalidPictureParameters,
								SOWrapper.CurrentFileName, e);
						}
						if (m_styleProxy.EndMarker != null)
						{
							SetInCharacterStyle(); // treat this like a character style
						}
					}
					fFoundSpecialTarget = true;
					break;

				case MappingTargetType.FigureCaption:
					if (m_importDomain == ImportDomain.BackTrans ||
						(m_styleProxy.Domain & MarkerDomain.BackTrans) != 0)
					{
						// Save information about this picture caption back translation which will
						// be added at the end of the paragraph.
						if (m_currBtPictureInfo == null)
						{
							m_currBtPictureInfo = new BTPictureInfo(m_sSegmentText, null, BackTransWS,
								SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber,
								m_sMarker + " " + m_sSegmentText, m_currentRef);
							m_BTPendingPictures.Add(m_currBtPictureInfo);
						}
						else
						{
							m_currBtPictureInfo.m_strbldrCaption.Append(m_sSegmentText);
						}
					}
					else
					{
						if (m_currPictureInfo != null && m_currPictureInfo.Caption != null)
							InsertPicture(); // Already found a caption for the current picture; treat this as a new picture.
						if (m_currPictureInfo == null)
							m_currPictureInfo = new ToolboxPictureInfo();
						m_currPictureInfo.Caption = TsIncStrBldrClass.Create();
						m_currPictureInfo.Caption.SetIntPropValues((int)FwTextPropType.ktptWs,
							(int)FwTextPropVar.ktpvDefault, m_wsVern);
						m_currPictureInfo.Caption.Append(m_sSegmentText);
					}
					fProcessingPicture = fFoundSpecialTarget = true;
					break;

				case MappingTargetType.FigureCopyright:
					if (m_importDomain == ImportDomain.BackTrans ||
						(m_styleProxy.Domain & MarkerDomain.BackTrans) != 0)
					{
						// Save information about this picture copyright back translation which will
						// be added at the end of the paragraph.
						if (m_currBtPictureInfo == null)
						{
							m_currBtPictureInfo = new BTPictureInfo(null, m_sSegmentText, BackTransWS,
								SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber,
								m_sMarker + " " + m_sSegmentText, m_currentRef);
							m_BTPendingPictures.Add(m_currBtPictureInfo);
						}
						else
						{
							m_currBtPictureInfo.m_copyright = m_sSegmentText;
						}
					}
					else
					{
						if (m_currPictureInfo != null && !string.IsNullOrEmpty(m_currPictureInfo.Copyright))
							InsertPicture(); // Already found a copyright for the current picture; treat this as a new picture.
						if (m_currPictureInfo == null)
							m_currPictureInfo = new ToolboxPictureInfo();
						m_currPictureInfo.Copyright = m_sSegmentText;
					}
					fProcessingPicture = fFoundSpecialTarget = true;
					break;

				case MappingTargetType.FigureDescription:
					{
						int ws = m_styleProxy.WritingSystem <= 0 ? m_wsAnal : m_styleProxy.WritingSystem;
						if (m_currPictureInfo != null && m_currPictureInfo.HasDescriptionForWs(ws))
							InsertPicture(); // Already found a description for the current picture; treat this as a new picture.
						if (m_currPictureInfo == null)
							m_currPictureInfo = new ToolboxPictureInfo();
						m_currPictureInfo.AddDescriptionVariant(m_sSegmentText, ws);
						fProcessingPicture = fFoundSpecialTarget = true;
					}
					break;

				case MappingTargetType.FigureFilename:
					if (m_currPictureInfo != null && !string.IsNullOrEmpty(m_currPictureInfo.PictureFilename))
						InsertPicture(); // Already found a filename for the current picture; treat this as a new picture.
					if (m_currPictureInfo == null)
						m_currPictureInfo = new ToolboxPictureInfo();

					if (!FileUtils.IsFilePathValid(m_sSegmentText))
					{
						throw new ScriptureUtilsException(SUE_ErrorCode.InvalidPictureFilename, SOWrapper.CurrentFileName,
							SOWrapper.CurrentLineNumber, m_sMarker + " " + m_sSegmentText, m_currentRef);
					}
					m_currPictureInfo.PictureFilename = EnsurePictureFilePathIsRooted(m_sSegmentText);
					fProcessingPicture = fFoundSpecialTarget = true;
					break;

				case MappingTargetType.FigureLayoutPosition:
					if (m_currPictureInfo != null && !string.IsNullOrEmpty(m_currPictureInfo.LayoutPos))
						InsertPicture(); // Already found a layout position for the current picture; treat this as a new picture.
					if (m_currPictureInfo == null)
						m_currPictureInfo = new ToolboxPictureInfo();
					m_currPictureInfo.LayoutPos = m_sSegmentText;
					fProcessingPicture = fFoundSpecialTarget = true;
					break;

				case MappingTargetType.FigureRefRange:
					if (m_currPictureInfo != null && !string.IsNullOrEmpty(m_currPictureInfo.LocationRange))
						InsertPicture(); // Already found a location range for the current picture; treat this as a new picture.
					if (m_currPictureInfo == null)
						m_currPictureInfo = new ToolboxPictureInfo();
					m_currPictureInfo.LocationRange = m_sSegmentText;
					fProcessingPicture = fFoundSpecialTarget = true;
					break;

				case MappingTargetType.FigureScale:
					if (m_currPictureInfo != null && !string.IsNullOrEmpty(m_currPictureInfo.ScaleFactor))
						InsertPicture(); // Already found a scale factor for the current picture; treat this as a new picture.
					if (m_currPictureInfo == null)
						m_currPictureInfo = new ToolboxPictureInfo();
					m_currPictureInfo.ScaleFactor = m_sSegmentText;
					fProcessingPicture = fFoundSpecialTarget = true;
					break;

				case MappingTargetType.TitleShort:
					{
						int ws = m_styleProxy.WritingSystem;

						if (ws == -1)
							ws = GetWsForImportDomain();

						if (m_scrBook != null)
							m_scrBook.Name.set_String(ws, TsStringUtils.MakeTss(m_sSegmentText.Trim(), ws));

						// REVIEW: Should we call SetInCharStyle here?
						if (m_styleProxy.EndMarker != null)
							m_fInCharStyle = true; // treat this like a character style

						fFoundSpecialTarget = true;
					}
					break;

				case MappingTargetType.ChapterLabel:
					// TODO: TE-867 Handle chapter label pseudo style
					// REVIEW: Should we call SetInCharStyle here?
					if (m_styleProxy.EndMarker != null)
						m_fInCharStyle = true; // treat this like a character style

					fFoundSpecialTarget = true;
					break;
			}

			if (!fProcessingPicture)
			{
				if (m_currPictureInfo != null)
				{
					// We just finished collecting all the info we're going to get for the current
					// picture, so insert it.
					// ENHANCE: Need to account for the possibility of a writing system (or char
					// style?) run in the picture caption.
					InsertPicture();
				}
				m_currBtPictureInfo = null; // Don't worry, it'll get hooked up later
			}

			return fFoundSpecialTarget;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the text representation of a picture for a folder. If it doesn't include the
		/// folder name, then add it.
		/// </summary>
		/// <param name="textRep">text representation of the picture</param>
		/// <returns>text representation with folder added, if necessary (or original text
		/// representation, if not)</returns>
		/// ------------------------------------------------------------------------------------
		private string EnsurePictureFilePathIsRooted(string textRep)
		{
			// Determine file name string.
			int iFileNameStart = textRep.IndexOf('|');
			if (iFileNameStart < 0 || textRep.Length <= iFileNameStart)
			{
				// text representation is either invalid or the file name is not contained within vertical bars
				return GetRootedPath(textRep);
			}

			string pictureTextRep = textRep.Substring(iFileNameStart + 1);
			int iFileNameEnd = pictureTextRep.IndexOf('|');
			if (iFileNameEnd <= 0)
				return textRep; // text representation invalid

			string fileName = pictureTextRep.Substring(0, iFileNameEnd);

			// Picture text representation already has folder information for file.
			return textRep.Substring(0, iFileNameStart + 1) + GetRootedPath(fileName) +
						pictureTextRep.Substring(iFileNameEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rooted path.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetRootedPath(string fileName)
		{
			string fullPath = null;
#if !__MonoCS__
			if (fileName.Length < 2 || !Char.IsLetter(fileName[0]) || fileName[1] != Path.VolumeSeparatorChar)
#else
			if (fileName[0] != Path.DirectorySeparatorChar) // is fileName a relative Path
#endif
			{
				try
				{
					if (Path.IsPathRooted(fileName))
					{
						fullPath = Path.GetFullPath(fileName);
						if (!FileUtils.FileExists(fullPath))
						{
							fullPath = null;
							fileName = fileName.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
						}
					}
					if (fullPath == null)
					{
						Debug.Assert(SOWrapper.ExternalPictureFolders.Count > 0);
						foreach (string folder in SOWrapper.ExternalPictureFolders)
						{
							fullPath = Path.Combine(folder, fileName);
							if (FileUtils.FileExists(fullPath))
								break;
							fullPath = null;
						}
					}
					if (fullPath == null)
						fullPath = Path.Combine(SOWrapper.ExternalPictureFolders[0], fileName);
					return fullPath;
				}
				catch (ArgumentException)
				{
					// filename probably has invalid characters in it, this should be caught a reported later
				}
			}
			return fileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the context and paragraph text properties for this segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetSegmentInfo()
		{
			m_context = m_styleProxy.Context;

			// Determine the WS/Domain of the paragraph
			if (m_styleProxy.StyleType == StyleType.kstParagraph)
			{
				m_wsPara = m_styleProxy.WritingSystem;
				if (m_wsPara < 0)
					m_wsPara = 0;

				if (m_styleProxy.Domain == MarkerDomain.Default)
				{
					switch (m_importDomain)
					{
						case ImportDomain.Main:
							m_currDomain = MarkerDomain.Default;
							break;
						case ImportDomain.BackTrans:
							m_currDomain = MarkerDomain.BackTrans;
							break;
						case ImportDomain.Annotations:
							m_currDomain = MarkerDomain.Note;
							break;
					}
				}
				else
				{
					m_currDomain = m_styleProxy.Domain;
				}
			}
			else if ((m_importDomain == ImportDomain.Main ||
				(m_importDomain == ImportDomain.BackTrans && m_currDomain == MarkerDomain.Note)) &&
				(m_styleProxy.Function == FunctionValues.Chapter ||
				 m_styleProxy.Function == FunctionValues.Verse))
			{
				// Any chapter or verse number encountered while processing the main (i.e., Scripture)
				// import domain pops us out of any BT or Note paragraph back into the vernacular.
				m_wsPara = m_wsVern;
				m_currDomain = MarkerDomain.Default;
			}

			if (m_styleProxy.Domain != MarkerDomain.Default &&
				(m_styleProxy.Domain != MarkerDomain.Footnote ||
				(m_currDomain & MarkerDomain.Footnote) == 0))
			{
				m_currDomain = m_styleProxy.Domain;
			}
			else if (m_styleProxy.Domain == MarkerDomain.Default &&
				m_styleProxy.Context == ContextValues.EndMarker &&
				(!m_fInCharStyle || m_sCharStyleEndMarker == null))
			{
				m_currDomain = m_styleProxy.Domain;
			}

			// General character styles should be processed as Verse Text if they occur within
			// a verse text paragraph.
			if (m_context == ContextValues.General && m_fInVerseTextParagraph)
				m_context = ContextValues.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the start of book segments
		/// </summary>
		/// <returns><c>true if this segment was a book marker</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleStartOfBook()
		{
			// Start of Book (or possibly just another file with more of the same book)
			if (m_context != ContextValues.Book)
				return false;

			int wsCurrentStream = SOWrapper.CurrentWs(m_wsOfPrevImportStream);
			// If this is the first book, a new book, or a new domain...
			if (!m_fFoundABook || m_nBookNumber != SOWrapper.SegmentFirstRef.Book ||
				m_prevImportDomain != m_importDomain ||	m_wsOfPrevImportStream != wsCurrentStream)
			{
				ProcessStartOfBook();
				m_nPrevBookNumber = m_nBookNumber;
				m_fFoundABook = true;
				if (m_prevImportDomain != m_importDomain)
				{
					// if we are starting a new domain, remove "rogue" import style proxies that have
					// not been used.
					foreach (string marker in m_unmappedMarkers)
						if (m_styleProxies[marker].Style == null)
							m_styleProxies.Remove(marker);
					m_unmappedMarkers.Clear();
				}
			}
			m_prevImportDomain = m_importDomain;
			m_wsOfPrevImportStream = wsCurrentStream;
			if (m_importDomain == ImportDomain.BackTrans)
			{
				m_fBackTransChapterNumPending = m_fBackTransVerseNumPending = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if this segment should be skipped as part of introductory material
		/// </summary>
		/// <returns>true if the segment is part of introductory material and it
		/// should be skipped</returns>
		/// ------------------------------------------------------------------------------------
		private bool SkipIntroMaterial()
		{
			// Never skip chapter and verse numbers
			if (m_styleProxy.Function == FunctionValues.Chapter ||
				m_styleProxy.Function == FunctionValues.Verse ||
				m_styleProxy.Context == ContextValues.Title)
				return false;

			// If we are still in the introduction and this segment is a character style and
			// we are skipping intro stuff, then skip this segment
			if (m_fCurrentSectionIsIntro && !m_settings.ImportBookIntros
				&& m_styleProxy.StyleType == StyleType.kstCharacter)
			{
				if (m_styleProxy.Context != ContextValues.EndMarker)
					SetInCharacterStyle();
				return true;
			}

			// If introductions are not being imported then ignore.
			if (m_context == ContextValues.Intro)
			{
				if (!m_settings.ImportBookIntros)
					return true;

				// Make sure that this intro section is not occurring after a scripture section.
				if (m_fInSectionHeading && !m_fCurrentSectionIsIntro
					&& m_styleProxy.Structure == StructureValues.Heading)
				{
					m_fInScriptureText = true;
				}
				if (m_fInScriptureText)
				{
					int verse = SOWrapper.SegmentFirstRef.Verse;
#pragma warning disable 219
					string formatString =
						TeResourceHelper.GetResourceString("kstidImportIntroWithinScripture");
#pragma warning restore 219
					throw new ScriptureUtilsException(SUE_ErrorCode.IntroWithinScripture,
						SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber,
						m_sMarker + " " + m_sSegmentText,
						m_fFoundABook ? ScrReference.NumberToBookCode(m_nBookNumber) : null,
						m_foundAChapter ? m_nChapter.ToString(): "1",
						(verse > 0) ? verse.ToString() : "1");
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a segment in the Note (Annotation) domain. This method doesn't actually
		/// create an annotation. It just adds info about it to an array, to be added later by
		/// <see cref="AddPendingAnnotations"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleNoteDomain()
		{
			//if (m_fInFootnote)
			//    EndFootnote();

			if (!m_settings.ImportAnnotations)
				return;

			if (m_styleProxy.Function == FunctionValues.Chapter)
			{
				ProcessStartOfChapter();
				return;
			}
			if (m_styleProxy.Function == FunctionValues.Verse)
			{
				ProcessVerseNumbers();
				m_sSegmentText = m_sSegmentText.Trim();
				if (m_sSegmentText != null && m_sSegmentText != string.Empty)
					AddNewAnnotation();
				return;
			}

			if (m_styleProxy.StyleType == StyleType.kstParagraph)
				AddNewAnnotation();
			else
			{
				SetInCharacterStyle();
				if (m_PendingAnnotations.Count == 0)
				{
					ITsTextProps runProps = m_styleProxy.TsTextProps;
					if (m_styleProxy.WritingSystem <= 0)
					{
						ITsPropsBldr tpb = runProps.GetBldr();
						tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
							m_SOWrapper.CurrentWs(m_wsAnal));
						runProps = tpb.GetTextProps();
					}
					AddNewAnnotation(m_DefaultAnnotationStyleProxy, runProps);
				}
				else
				{
					ScrAnnotationInfo info = m_PendingAnnotations[m_PendingAnnotations.Count - 1];
					Debug.Assert(info.bldrsDiscussion != null && info.bldrsDiscussion.Count > 0);
					AddTextToPara(m_sSegmentText, m_styleProxy.TsTextProps, info.bldrsDiscussion[0].StringBuilder);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new annotation to the pending list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddNewAnnotation()
		{
			AddNewAnnotation(m_styleProxy, GetTextPropsWithWS(m_styleProxy.WritingSystem, null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new annotation to the pending list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddNewAnnotation(ImportStyleProxy paraProxy, ITsTextProps runProps)
		{
			StTxtParaBldr paraBldr = new StTxtParaBldr(m_cache);
			paraBldr.ParaStylePropsProxy = paraProxy;
			paraBldr.AppendRun(m_sSegmentText, runProps);
			Guid guidAnnotationType = Guid.Empty;
			try
			{
				if (paraProxy.NoteType != null)
					guidAnnotationType = paraProxy.NoteType.Guid;
				else if (m_SOWrapper.CurrentAnnotationType > 0)
				{
					guidAnnotationType =
						m_cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(m_SOWrapper.CurrentAnnotationType).Guid;
				}
			}
			catch
			{
			}
			finally
			{
				if (guidAnnotationType == Guid.Empty)
					guidAnnotationType = m_scrTranslatorAnnotationDef.Guid;
			}
			BCVRef lastRef = SOWrapper.SegmentLastRef;
			// If we haven't come across any scripture text (still in title or intro), then make
			// sure that the chapter number is set right.
			if (lastRef.Chapter == 0)
				lastRef.Chapter = 1;

			m_PendingAnnotations.Add(new ScrAnnotationInfo(guidAnnotationType, paraBldr,
				(m_ParaBldr.Length > 0 ? m_ParaBldr.Length - 1 : 0),
				m_currentRef.BBCCCVVV, lastRef.BBCCCVVV));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add any pending annotations for the last paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddPendingAnnotations()
		{
			foreach (ScrAnnotationInfo info in m_PendingAnnotations)
			{
				if (m_importDomain == ImportDomain.Annotations)
				{
					// TODO: Try to hook up Intro annotations to the correct para if we have enough info
					if (m_currentRef.Verse == 0)
						m_lastPara = null;
					else
						// ENHANCE: eventually handle the end reference if needed
						m_lastPara = FindCorrespondingVernParaForAnnotation(info.startReference);
				}

				info.bldrsDiscussion[0].TrimTrailingSpaceInPara();

				FindOrCreateAnnotation(info,
					(m_lastPara != null) ? (ICmObject)m_lastPara : (ICmObject)CurrentBook);
			}
			m_PendingAnnotations.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a paragraph builder for a simple, single-paragraph Scripture annotation
		/// discussion field. This is virtual to allow subclasses to return NULL if they can't
		/// deal with this much simplicity.
		/// </summary>
		/// <param name="info">The information about a annotation being imported.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override StTxtParaBldr GetAnnotDiscussionParaBldr(ScrAnnotationInfo info)
		{
			if (info.bldrsDiscussion == null || info.bldrsDiscussion.Count == 0)
				return null;
			Debug.Assert(info.bldrsDiscussion.Count == 1);
			return info.bldrsDiscussion[0];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the start and end of book title segments
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleBookTitle(ref bool fInsertSegment)
		{
			if (m_context == ContextValues.Title)
			{
				if (!m_fInBookTitle)
					ProcessBookTitleStart();
				else
				{
					// We have another segment to add to the current title
					AddBookTitleSegment();
					// don't insert this segment into the body text because it's already been handled.
					fInsertSegment = false;
				}
				SetBookName();
			}
			else if (m_fInBookTitle)
			{
				// Want to process general character styles as additions to current paragraph
				// which is the default processing at the end of this method.
				if (m_styleProxy.StyleType == StyleType.kstCharacter &&
					m_context == ContextValues.General)
					fInsertSegment = true;
				else if (PrepareForNextPara())
					FinalizePrevTitle();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle section head segments (begin or end)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleSectionHead(ref bool fInsertSegment)
		{
			if (m_styleProxy.Structure == StructureValues.Heading)
			{
				if (!m_fInSectionHeading)
					ProcessSectionHeadStart();
				else
				{
					// We have another segment to add to the current section head
					AddSectionHeadSegment();
					// don't insert this segment into the body text because it's already been handled.
					fInsertSegment = false;
				}
			}
			else if (m_fInSectionHeading) // section head para is complete
			{
				// Want to process general character styles as additions to current paragraph
				// which is the default processing at the end of this method.
				if (m_styleProxy.StyleType == StyleType.kstCharacter &&
					m_context == ContextValues.General)
					fInsertSegment = true;
				else if (PrepareForNextPara())
					m_fInSectionHeading = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle footnote processing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleFootnotes(ref bool fInsertSegment)
		{
			if (m_currDomain == MarkerDomain.Footnote)
			{
				// Any current footnote is terminated if this is a new footnote paragraph or
				// a second note marker (which can't happen) for this footnote.
				if (m_fInFootnote &&
					(m_styleProxy.StyleType == StyleType.kstParagraph ||
					(m_fGotFootnoteMarker && m_styleProxy.StyleId == ScrStyleNames.FootnoteMarker)))
				{
					EndFootnote();
				}
				if (!m_fInFootnote)
					BeginFootnote(); //sets m_fInFootnote

				if (m_styleProxy.StyleId == ScrStyleNames.FootnoteMarker)
				{
					fInsertSegment = false;
					m_fGotFootnoteMarker = true;
					SetInCharacterStyle();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if current segment is a verse number, and if so process the verse number part
		/// of it. Also prepare to handle any text following the number (vernacular stream
		/// only).
		/// </summary>
		/// <returns>true if the segment was processed as a verse number</returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleVerseNumbers()
		{
			if (m_styleProxy.Function != FunctionValues.Verse)
				return false;

			ProcessVerseNumbers();

			// Prepare to insert the verse text into the paragraph as plain vernacular text.
			m_sSegmentText = m_sSegmentText.TrimStart();

			// When we go to add the actual text, we want the character properties to
			// contain only the writing system, not the character properties for the verse
			// number style. The resulting run will inherit its character formatting from
			// whatever paragraph style is in effect. To achieve this, we arbitrarily pick
			// a vernacular paragraph style here and get its proxy. Its character properties
			// are guaranteed to contain only a writing system.
			m_styleProxy = m_DefaultScrParaProxy;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process verse number segments
		/// </summary>
		/// <returns>true if the segment was processed as a verse number</returns>
		/// ------------------------------------------------------------------------------------
		private void ProcessVerseNumbers()
		{
			if (m_settings.ImportTranslation)
			{
				if (m_fInFootnote)
					EndFootnote(); // any current footnote is terminated

				if (m_importDomain == ImportDomain.Main)
				{
					// If appropriate, insert drop-cap chapter number
					if (m_fChapterNumberPending)
						AddDropChapterNumToPara(m_ParaBldr.StringBuilder, 0);

					if (m_ParaBldr.Length > kMaxParaSizeForVerseAndPuncBrk) // See TE-2753
						FinalizePrevParagraph(); // finish up any accumulated paragraph, start a new one
					else if (PrevRunIsVerseNumber(m_ParaBldr.StringBuilder))
					{
						// if the last token was a verse number, add a space before the verse number
						// using no style (default paragraph characters).
						AddTextToPara(" ", m_vernTextProps);
					}

					// Now insert the verse number into the paragraph.
					string sVerseRef = GetVerseRefAsString(0);
					AddTextToPara(sVerseRef);
				}
			}
			else if (m_settings.ImportBackTranslation)
			{
				if (m_fInFootnote)
					EndFootnote(); // any current footnote is terminated
			}
			AddChapterAndVerseNumsToBackTranslations();

			m_prevRef = new BCVRef(m_currentRef);
			m_currentRef.Verse = SOWrapper.SegmentFirstRef.Verse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this segment is any other kind of paragraph besides the special cases already
		/// handled, then finish off the previous para and prepare for this new one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleOtherParagraphStyles()
		{
			if (m_styleProxy.StyleType == StyleType.kstParagraph &&
				m_context != ContextValues.Book &&
				m_context != ContextValues.Title &&
				m_styleProxy.Structure != StructureValues.Heading &&
				m_context != ContextValues.Note)
			{
				if (m_fInFootnote)
					EndFootnote(); // any current footnote is terminated

				// finish up any accumulated paragraph, start a new one
				FinalizePrevParagraph();

				CheckForSectionHeadInIntroMaterial();

				// set paragraph style property for new para
				m_ParaBldr.ParaStylePropsProxy = m_styleProxy;
				// set VerseTextParagraph flag
				m_fInVerseTextParagraph = (m_context == ContextValues.Text &&
					m_styleProxy.Structure == StructureValues.Body);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We've processed paragraph start and inserted specially marked text segments.
		/// Insert other marked text, from the current segment, into our paragraph (or
		/// footnote).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InsertSegment()
		{
			if (m_importDomain == ImportDomain.Main && m_settings.ImportTranslation)
			{
				// If appropriate, insert drop-cap chapter number
				if (m_fChapterNumberPending && m_context == ContextValues.Text &&
					m_styleProxy.Structure == StructureValues.Body && !m_fInFootnote)
				{
					AddDropChapterNumToPara(m_ParaBldr.StringBuilder, 0);
				}

				while (m_ParaBldr.Length + m_sSegmentText.Length > kMaxParaSizeForVerseAndPuncBrk)
				{
					bool fFoundPunctuation = false;

					for (int ich = kMaxParaSizeForVerseAndPuncBrk - m_ParaBldr.Length;
						ich < m_sSegmentText.Length;
						ich++)
					{
						if (UnicodeCharProps.get_IsPunctuation(m_sSegmentText[ich]))
						{
							string sSave = m_sSegmentText.Substring(ich + 1);
							m_sSegmentText = m_sSegmentText.Substring(0, ich + 1);
							AddTextToPara(m_sSegmentText);
							FinalizePrevParagraph();
							m_sSegmentText = sSave;
							fFoundPunctuation = true;
							break;
						}
					}

					if (!fFoundPunctuation)
						break;
				}

				// Here we add the basic segment text to our paragraph builder
				AddTextToPara(m_sSegmentText);
			}

			// if we're using a character style other than a footnote marker, we'll watch
			// for a character style end marker. (Footnotes handled separately)
			if (m_styleProxy.StyleType == StyleType.kstCharacter && m_sMarker != m_sFootnoteBeginMarker)
			{
				// If the proxy has no specific end marker, this will be null, and any
				// marker which maps to default paragraph characters will end the styled run.
				SetInCharacterStyle();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return text props that can be used for a run of "Default Paragraph Characters"
		/// having only a WS.
		/// </summary>
		/// <param name="ws">The explicit writing system to use, or 0 to use the default
		/// vernacular, or -1 to infer from the context</param>
		/// <param name="currBldr">The string builder, from which the WS might be inferred if
		/// not specified</param>
		/// ------------------------------------------------------------------------------------
		private ITsTextProps GetTextPropsWithWS(int ws, ITsStrBldr currBldr)
		{
			if (ws == m_wsVern || ws == 0)
				return m_vernTextProps;
			else if (ws == m_wsAnal)
				return m_analTextProps;
			if (ws < 0)
			{
				ws = GetWsForContext(currBldr);
			}
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);
			return propsBldr.GetTextProps();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a picture, either using the current segment (USFM-style) or the pending
		/// picture in m_currPictureInfo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void InsertPicture()
		{
			ICmPicture picture;
			ICmPictureFactory picFactory = m_cache.ServiceLocator.GetInstance<ICmPictureFactory>();
			if (m_currPictureInfo == null)
			{
				string[] tokens = m_sSegmentText.Split('|');
				if (tokens.Length < 6)
					throw new ArgumentException("The USFM format for a Picture was invalid");
				string sDescription = tokens[0];
				string srcFilename = tokens[1];
				if (String.IsNullOrEmpty(srcFilename))
					srcFilename = Path.Combine(FwDirectoryFinder.CodeDirectory, "MissingPictureInImport.bmp");
				string sLayoutPos = tokens[2];
				string sLocationRange = tokens[3];
				string sCopyright = tokens[4];
				string sCaption = tokens[5];
				picture = picFactory.Create(CmFolderTags.DefaultPictureFolder, m_currentRef.BBCCCVVV,
					m_scr as IPictureLocationBridge, sDescription, srcFilename, sLayoutPos,
					sLocationRange, sCopyright, sCaption, PictureLocationRangeType.ReferenceRange, "100");
			}
			else
			{
				string srcFilename = m_currPictureInfo.PictureFilename;
				if (String.IsNullOrEmpty(srcFilename))
					srcFilename = Path.Combine(m_cache.LanguageProject.LinkedFilesRootDir, "MissingPictureInImport.bmp");
				picture = picFactory.Create(CmFolderTags.DefaultPictureFolder, m_currentRef.BBCCCVVV,
					m_scr as IPictureLocationBridge, m_currPictureInfo.Description, srcFilename,
					m_currPictureInfo.LayoutPos, m_currPictureInfo.LocationRange ?? "",
					m_currPictureInfo.Copyright, m_currPictureInfo.TssCaption,
					PictureLocationRangeType.ReferenceRange, m_currPictureInfo.ScaleFactor);
				m_currPictureInfo = null;
			}
			picture.InsertORCAt(m_ParaBldr.StringBuilder, m_ParaBldr.Length);
			m_CurrParaPictures.Add(picture);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a back translation segment (duh)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ProcessBackTransSegment()
		{
			if (m_styleProxy.Function == FunctionValues.Chapter)
			{
				ProcessStartOfChapter();
				return;
			}

			if (!m_settings.ImportBackTranslation)
			{
				if (m_styleProxy.Function == FunctionValues.Verse)
					m_currentRef.Verse = SOWrapper.SegmentFirstRef.Verse;
				return;
			}

			if (CurrentBook == null)
			{
				// An interleaved BT paragraph is being imported for a book that
				// doesn't exist.
				throw new ScriptureUtilsException(SUE_ErrorCode.BackTransMissingVernBook,
					null, null, ScrReference.NumberToBookCode(m_nBookNumber), null, null, true);
			}

			// Any non-end marker ends a character style
			if (m_context != ContextValues.EndMarker)
				EndCharStyle();

			ITsTextProps ttpBtSeg;
			bool fThisSegmentEndsAFootnote = false;
			if (m_importDomain == ImportDomain.Main)
			{
				int wsBT = BackTransWS;
				ttpBtSeg = GetBTProps(wsBT);
				if (wsBT != m_wsCurrBtPara &&
					(m_styleProxy.Domain != MarkerDomain.Default ||
					 m_styleProxy.StyleType == StyleType.kstParagraph))
				{
					// We're about to change which WS is being processed, so end any open character
					// style and/or footnote.
					EndCharStyle();
					if (m_fInFootnote)
					{
						EndFootnote();
						fThisSegmentEndsAFootnote = (m_currDomain & MarkerDomain.Footnote) == 0;
					}
					m_wsCurrBtPara = wsBT;
				}
				else if (m_context == ContextValues.EndMarker)
					fThisSegmentEndsAFootnote = ProcessBTEndMarker();
				else if (m_fInFootnote && (m_currDomain & MarkerDomain.Footnote) != 0 &&
					m_styleProxy.StyleType == StyleType.kstParagraph)
				{
					EndFootnote();
					fThisSegmentEndsAFootnote = (m_currDomain & MarkerDomain.Footnote) == 0;
				}
			}
			else
			{
				ttpBtSeg = GetBTProps(m_wsCurrBtPara);
				if (m_context == ContextValues.EndMarker)
					fThisSegmentEndsAFootnote = ProcessBTEndMarker();
			}

			if (m_importDomain == ImportDomain.BackTrans && ProcessingParagraphStart)
			{
				if (m_fInFootnote)
				{
					EndFootnote();
					fThisSegmentEndsAFootnote = (m_currDomain & MarkerDomain.Footnote) == 0;
				}

				AddBackTranslations();
			}

			if (!m_fInScriptureText && m_styleProxy.Context == ContextValues.Text &&
				m_styleProxy.Structure == StructureValues.Body)
			{
				// If this BT segment is a scripture body segment and we don't already have
				// a paragraph started, this file must have an implicit paragraph start,
				// so we initialize the member that holds the para style proxy for the
				// corresponding vernacular para so we'll have something reasonable to
				// look for when we go to find the corresponding para.
				if (m_BTStrBldrs.Count == 0 && m_importDomain == ImportDomain.BackTrans)
					m_vernParaStyleProxy = m_DefaultScrParaProxy;
				m_fInScriptureText = true;
				m_iNextBtPara = 0;
			}

			ITsStrBldr strbldr;

			if (m_BTStrBldrs.ContainsKey(m_wsCurrBtPara))
			{
				// We continue to use the existing BT para builder (for this WS) until the
				// vernacular paragraph -- and hence any BT paragraph(s) -- get written.
				// This means that if the BT contains spurious paragraph markers, their
				// segments will just be appended to the one-and-only BT paragraph (for this
				// WS) already being built.
				// TODO: Generate an error annotation.
				Debug.Assert(m_wsCurrBtPara != 0);
				strbldr = m_BTStrBldrs[m_wsCurrBtPara];
			}
			else
			{
				if (ProcessingParagraphStart)
				{
					if (m_importDomain == ImportDomain.Main)
					{
						if (m_vernParaStyleProxy == null || m_vernParaStyleProxy.StyleId == null)
						{
							// Got an unexpected BT paragraph segment. A BT paragraph came before
							// any vernacular paragraph in the import stream.
							int verse = SOWrapper.SegmentFirstRef.Verse;
							throw new ScriptureUtilsException(SUE_ErrorCode.BackTransMissingVernPara,
								SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber,
								m_sMarker + " " + m_sSegmentText,
								m_fFoundABook ? CurrentBook.BookId : null,
								m_foundAChapter ? m_nChapter.ToString() : null,
								(verse > 0) ? verse.ToString() : null,
								true);
						}
						if (m_vernParaStyleProxy.StyleId != m_styleProxy.StyleId &&
							m_styleProxy.Style.Type != StyleType.kstCharacter)
						{
							// Got an unexpected BT paragraph segment. The paragraph style of the BT
							// paragraph doesn't match the style of the corresponding (i.e.,
							// preceding) vernacular paragraph.
							int verse = SOWrapper.SegmentFirstRef.Verse;
							throw new ScriptureUtilsException(SUE_ErrorCode.BackTransStyleMismatch,
								m_sMarker + " " + m_sSegmentText, string.Format(
								TeResourceHelper.GetResourceString("kstidBTStyleMismatchDetails"),
								m_styleProxy.StyleId, m_vernParaStyleProxy.StyleId),
								m_fFoundABook ? CurrentBook.BookId : null,
								m_foundAChapter ? m_nChapter.ToString() : null,
								(verse > 0) ? verse.ToString() : null, true);
						}
					}
					// Non-interleaved BT
					else
					{
						ProcessBtParaStart();
					}
				}
				strbldr = TsStringUtils.MakeTss("", m_wsCurrBtPara).GetBldr();
				Debug.Assert(m_wsCurrBtPara != 0);
				m_BTStrBldrs[m_wsCurrBtPara] = strbldr;
			}

			if (m_context == ContextValues.Title)
			{
				SetBookName();
				if (m_fInBookTitle)
				{
					// We have another segment to add to the current title
					AddBookTitleSegment(strbldr, m_vernParaStyleProxy, ttpBtSeg);
					return;
				}
				m_fInBookTitle = true;
			}

			AddPendingVerseAndChapterNumsToBackTrans(GetVerseRefAsString(m_wsCurrBtPara), m_wsCurrBtPara, strbldr);

			if ((m_currDomain & MarkerDomain.Footnote) != 0 && !fThisSegmentEndsAFootnote)
			{
				if (m_BTFootnoteStrBldr == null)
				{
					if (m_fInFootnote)
					{
						// This BT footnote ends the vernacular footnote being built
						EndFootnote();
					}
					// remember that we are now processing a footnote
					SetInFootnote();
					CheckDataForFootnoteMarker();
					m_BTFootnoteStrBldr  = TsStrBldrClass.Create();
					m_sBtFootnoteParaStyle = (m_styleProxy.StyleType == StyleType.kstCharacter) ?
						m_DefaultFootnoteParaProxy.StyleId : m_styleProxy.StyleId;
					if (m_importDomain == ImportDomain.Main)
					{
						// If we aren't importing the vernacular... (TE-7445)
						if (!m_settings.ImportTranslation)
						{
							// attempt to find an existing vernacular paragraph that corresponds
							// to the back translation para.
							bool append;
							m_lastPara = FindCorrespondingVernParaForSegment(m_vernParaStyleProxy.Style,
								m_currentRef, out append);
							m_CurrParaFootnotes = (m_lastPara != null ?
								m_lastPara.GetFootnotes() : null);
						}

						m_CurrBTFootnote = FindCorrespondingFootnote(m_sBtFootnoteParaStyle);
						if (m_CurrBTFootnote == null)
						{
							int verse = SOWrapper.SegmentFirstRef.Verse;
							throw new ScriptureUtilsException(SUE_ErrorCode.BackTransMissingVernFootnote,
								SOWrapper.CurrentFileName, SOWrapper.CurrentLineNumber,
								m_sMarker + " " + m_sSegmentText,
								m_fFoundABook ? CurrentBook.BookId : null,
								m_foundAChapter ? m_nChapter.ToString(): null,
								(verse > 0) ? verse.ToString() : null,
								true);
						}
					}
					else
					{
						m_CurrBTFootnote = null;
					}
				}
				strbldr = m_BTFootnoteStrBldr;
				if (m_styleProxy.StyleType == StyleType.kstCharacter &&
					m_styleProxy.Context != ContextValues.EndMarker)
				{
					SetInCharacterStyle();
				}
			}
			else if (m_styleProxy.StyleType == StyleType.kstCharacter &&
				m_styleProxy.Function != FunctionValues.Verse)
			{
				SetInCharacterStyle();
			}

			// Add a line break between section head segments if we're processing
			// interleaved back translation section heading segments.
			if (m_fInSectionHeading && strbldr.Length > 0 &&
				ProcessingParagraphStart && m_importDomain == ImportDomain.Main)
			{
				AddTextToPara(kHardLineBreak, ttpBtSeg, strbldr);
			}

			AddTextToPara(m_sSegmentText, ttpBtSeg, strbldr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether a verse number is pending for the Back Translation for the given
		/// writing system.
		/// </summary>
		/// <param name="ws">Writing system ID</param>
		/// <returns>true or false</returns>
		/// ------------------------------------------------------------------------------------
		private bool BtVerseNumPending(int ws)
		{
			return m_fBackTransVerseNumPending && !m_btPendingVerseNumAdded.Contains(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the chapter number is pending for the Back Translation for the given
		/// writing system.
		/// </summary>
		/// <param name="ws">Writing system ID</param>
		/// <returns>true or false</returns>
		/// ------------------------------------------------------------------------------------
		private bool BtChapterNumPending(int ws)
		{
			return m_fBackTransChapterNumPending && !m_btPendingChapterNumAdded.Contains(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this book should be skipped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool SkipThisBook
		{
			get { return m_nSkipBookNumber == m_nBookNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the writing system to use for the back translation paragraph
		/// </summary>
		/// <returns>back translation writing system</returns>
		/// ------------------------------------------------------------------------------------
		private int BackTransWS
		{
			get
			{
				// If the proxy has an explicit writing system other than the vernacular, use it
				// as the writing system for the CmTranslation
				if (m_styleProxy.WritingSystem > 0 && m_styleProxy.WritingSystem != m_wsVern)
					return m_styleProxy.WritingSystem;

				// If we couldn't get the writing system from the proxy and we're not already
				// in the middle of processing a BT para, just use the default analysis WS.
				return (m_wsCurrBtPara <= 0) ? m_wsAnal : m_wsCurrBtPara;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the correct text props to use for the current BT segment. This method returns
		/// the properties of the current proxy, doped up with the given writing system if
		/// necessary.
		/// </summary>
		/// <param name="wsBT">back translation writing system</param>
		/// <returns>a text props to use with the WS set</returns>
		/// ------------------------------------------------------------------------------------
		private ITsTextProps GetBTProps(int wsBT)
		{
			Debug.Assert(wsBT > 0);
			ITsTextProps ttpBtSeg = m_styleProxy.TsTextProps;

			// if the proxy doesn't have an explicit WS, use the WS of the current BT
			if (m_styleProxy.WritingSystem <= 0 || m_styleProxy.Function == FunctionValues.Verse)
			{
				ITsPropsBldr tpb = ttpBtSeg.GetBldr();
				if (m_styleProxy.WritingSystem <= 0)
					tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsBT);
				if (m_styleProxy.Function == FunctionValues.Verse)
				{
					if (m_importDomain == ImportDomain.BackTrans)
						ProcessVerseNumbers();
					else
					{
						// For this "Verse Number" segment, the verse number will get put in the
						// BT stringbuilder as a side-effect of processing the vernacular data stream,
						// so discard the first numeric token in the string and add any remaining text as
						// default paragraph characters.
						string sLiteralVerse, sRemainingText;
						BCVRef firstRef = 0;
						BCVRef lastRef = 0;
						if (BCVRef.VerseToScrRef(m_sSegmentText, out sLiteralVerse,
							out sRemainingText, ref firstRef, ref lastRef))
						{
							m_sSegmentText = sRemainingText;
						}
					}
					m_sSegmentText = m_sSegmentText.TrimStart();
					tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
				}
				ttpBtSeg = tpb.GetTextProps();
			}

			return ttpBtSeg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if current segment is a paragraph (but not a footnote para). Note that
		/// this also handles the special case of a title which starts with a Title Secondary or
		/// Tertiary character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool ProcessingParagraphStart
		{
			get
			{
				return ((m_styleProxy.StyleType == StyleType.kstParagraph &&
					(m_currDomain & MarkerDomain.Footnote) == 0 &&
					(m_context != ContextValues.Title || !m_fInBookTitle)) ||
					(m_context == ContextValues.Title && !m_fInBookTitle));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remember info about this paragraph so we can try to find a corresponding paragraph
		/// later.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ProcessBtParaStart()
		{
			if (m_vernParaStyleProxy == null ||
				m_vernParaStyleProxy.Context != m_styleProxy.Context)
			{
				m_iNextBtPara = 0;
				m_iCurrSection = 0;
			}
			else if (m_vernParaStyleProxy.Structure != m_styleProxy.Structure)
			{
				m_iNextBtPara = 0;
				if (m_styleProxy.Structure == StructureValues.Heading)
				{
					// If structure is going from content to heading, this is a new section,
					// so increment the index of the section where we should look for the matching
					// vern para.
					m_iCurrSection++;
				}
			}

			m_vernParaStyleProxy = (m_styleProxy.StyleType == StyleType.kstParagraph) ?
				m_styleProxy : m_BookTitleParaProxy;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph containing the specified reference.
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>The corrersponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrTxtPara FindCorrespondingVernParaForAnnotation(BCVRef targetRef)
		{
			m_lastPara = null;
			if (m_scrBook != null)
				m_lastPara = m_scrBook.FindPara(null, targetRef, 0, ref m_iCurrSection);

			if (m_lastPara == null)
				m_lastPara = m_scr.FindPara(null, targetRef, 0, ref m_iCurrSection);
			return m_lastPara;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph with the specified style id, containing the specified verse number,
		/// if specified, and having the correct sequence number. Sets all appropriate state
		/// variables before returning.
		/// </summary>
		/// <param name="style">style of paragraph to find. If the style is null, then
		/// disregard the style of the paragraph. Just get first paragraph with the given
		/// reference.</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="fAppend">Indicates to caller that the found paragraph already has
		/// been used previously, so new (BT) text should be appended to the existing back
		/// translation (with a hard line break separating the two parts). This is needed to
		/// support special logic we do in section headers when two paragraphs with the same
		/// style are imported in succession.</param>
		/// <returns>The corrersponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrTxtPara FindCorrespondingVernParaForSegment(IStStyle style,
			BCVRef targetRef, out bool fAppend)
		{
			fAppend = false;

			int iCurrSectionTemp = m_iCurrSection;
			m_lastPara = m_scrBook.FindPara(style, targetRef, m_iNextBtPara, ref m_iCurrSection);

			if (m_lastPara == null && style.Structure == StructureValues.Heading && m_iNextBtPara > 0)
			{
				m_iCurrSection = iCurrSectionTemp;
				m_lastPara = m_scrBook.FindPara(style, targetRef, --m_iNextBtPara, ref m_iCurrSection);
				fAppend = (m_lastPara != null);
			}

			if (m_lastPara != null)
			{
				m_CurrParaFootnotes = m_lastPara.GetFootnotes();
				m_CurrParaPictures = m_lastPara.GetPictures();
			}
			m_currSection = null;
			m_fCurrentSectionIsIntro = false;
			m_fInBookTitle = false;
			m_fInSectionHeading = false;
			m_fInVerseTextParagraph = false;
			m_sectionContent = null;
			m_sectionHeading = null;
			m_Title = CurrentBook.TitleOA;
			Debug.Assert(m_ParaBldr.Length == 0);

			return m_lastPara;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the next picture
		/// </summary>
		/// <returns>found picture, or null if the next picture is not found</returns>
		/// ------------------------------------------------------------------------------------
		protected ICmPicture FindCorrespondingPicture(int index)
		{
			if (index >= m_CurrParaPictures.Count)
				return null;
			return m_CurrParaPictures[index];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a footnote with the specified style id
		/// </summary>
		/// <param name="styleId">style of footnote to find</param>
		/// <returns>found footnote, or null if corrersponding footnote of styleId is not
		/// found</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrFootnote FindCorrespondingFootnote(string styleId)
		{
			if (!m_BTfootnoteIndex.ContainsKey(m_wsCurrBtPara))
				m_BTfootnoteIndex[m_wsCurrBtPara] = 0;
			int index = m_BTfootnoteIndex[m_wsCurrBtPara];
			IScrFootnote footnote = FindCorrespondingFootnote(styleId, index);
			if (footnote != null)
				m_BTfootnoteIndex[m_wsCurrBtPara] = index + 1;
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a footnote with the specified style id
		/// </summary>
		/// <param name="styleId">style of footnote to find</param>
		/// <param name="index">Index of footnote within the current paragraph</param>
		/// <returns>found footnote, or null if corresponding footnote of styleId is not
		/// found</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrFootnote FindCorrespondingFootnote(string styleId, int index)
		{
			try
			{
				FootnoteInfo finfo = m_CurrParaFootnotes[index];
				return (finfo.paraStylename == styleId) ? finfo.footnote : null;
			}
			catch
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalize the BT of the footnote, if any.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void EndBTFootnote()
		{
			Debug.Assert(m_fInFootnote);
			Debug.Assert(m_BTFootnoteStrBldr != null);
			ITsStrBldr strbldr = m_BTStrBldrs[m_wsCurrBtPara];
			TrimTrailingSpace(m_BTFootnoteStrBldr);
			// If the last character in the paragraph is a separator, then insert the footnote
			// marker before it. (see TE-2431)
			int ichMarker = strbldr.Length;
			if (ichMarker > 0)
			{
				string s = strbldr.GetChars(ichMarker - 1, ichMarker);
				if (UnicodeCharProps.get_IsSeparator(s[0]))
					ichMarker--;
			}
			if (m_CurrBTFootnote != null)
			{
				// Don't support importing multi-para footnotes
				IScrTxtPara para = (IScrTxtPara)m_CurrBTFootnote.ParagraphsOS[0];

				ICmTranslation transl = para.GetOrCreateBT();
				ITsString btTss = m_BTFootnoteStrBldr.Length == 0 ?
					m_TsStringFactory.MakeString(string.Empty, m_wsCurrBtPara) :
					m_BTFootnoteStrBldr.GetString();
				transl.Translation.set_String(m_wsCurrBtPara, btTss);
				m_CurrBTFootnote.InsertRefORCIntoTrans(strbldr, ichMarker, m_wsCurrBtPara);
				m_CurrBTFootnote = null;
			}
			else
			{
				m_BtFootnoteStrBldrs.Add(new BtFootnoteBldrInfo(m_wsCurrBtPara, m_BTFootnoteStrBldr,
					m_sBtFootnoteParaStyle, ichMarker, m_currentRef));
			}
			m_fInFootnote = false;
			m_BTFootnoteStrBldr = null;
			if ((m_styleProxy.Domain & MarkerDomain.Footnote) == 0)
				m_currDomain &= ~MarkerDomain.Footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add any pending chapter number and/or the current verse number as a run to each Back
		/// Translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddChapterAndVerseNumsToBackTranslations()
		{
			m_fBackTransVerseNumPending = true;
			m_btPendingVerseNumAdded.Clear();

			foreach (KeyValuePair<int, ITsStrBldr> kvp in m_BTStrBldrs)
				AddPendingVerseAndChapterNumsToBackTrans(GetVerseRefAsString(kvp.Key), kvp.Key, kvp.Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add any pending verse or chapter number as a run to the Back Translation for the
		/// given writing system.
		/// </summary>
		/// <param name="sVerseRef"></param>
		/// <param name="ws">Writing system ID</param>
		/// <param name="bldr">String builder for the BT</param>
		/// ------------------------------------------------------------------------------------
		private void AddPendingVerseAndChapterNumsToBackTrans(string sVerseRef, int ws,
			ITsStrBldr bldr)
		{
			if (BtChapterNumPending(m_wsCurrBtPara) &&
				m_vernParaStyleProxy != null &&
				((m_vernParaStyleProxy.Context == ContextValues.Text && m_vernParaStyleProxy.Structure == StructureValues.Body) ||
				m_vernParaStyleProxy.Context == ContextValues.EndMarker) &&
				!m_fInFootnote)
			{
				AddDropChapterNumToPara(bldr, ws);
			}

			if (BtVerseNumPending(ws))
			{
				// If previous marker is a verse number, add space between verse markers
				ITsStrBldr prevBldr = null;
				if (m_BTStrBldrs.ContainsKey(m_wsCurrBtPara))
					prevBldr = m_BTStrBldrs[m_wsCurrBtPara];
				if (PrevRunIsVerseNumber(prevBldr))
				{
					AddTextToPara(" ", StyleUtils.CharStyleTextProps(null, ws), bldr);
				}
				AddTextToPara(sVerseRef,
					StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, ws), bldr);
				m_btPendingVerseNumAdded.Add(ws);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process char style or footnote end marker in a back translation
		/// </summary>
		/// <returns><c>true</c> if this segment ends a footnote; <c>false</c> otherwise
		/// </returns>
		/// <remarks> Note: This logic is similar (and probably needs to be kept in synch with)
		/// ProcessEndMarker, but we can't use that method, because the details differ.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private bool ProcessBTEndMarker()
		{
			if (m_fInCharStyle &&
				(m_sMarker == m_sCharStyleEndMarker || m_sCharStyleEndMarker == null))
			{
				EndCharStyle(); // terminate the char style, we don't want to end the footnote yet.
				return false;
			}
			if (m_fInFootnote && (m_sMarker == m_sFootnoteEndMarker ||
				m_sFootnoteEndMarker == null ||
				(m_styleProxy.Domain & MarkerDomain.Footnote) == 0))
			{
				EndCharStyle();
				// If we don't have an explicit end marker, stay in footnote domain.
				if (m_sFootnoteEndMarker != null || (m_styleProxy.Domain & MarkerDomain.Footnote) == 0)
				{
					EndFootnote(); // terminate footnote, clear m_fInFootnote
					return true;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process char style or footnote end marker in the vernacular textF
		/// </summary>
		/// <returns><c>true</c> if any text associated with the end marker was added to the
		/// appropriate text (OR discarded because we're not actually importing the
		/// translation); <c>false</c> otherwise (specifically, this "end marker" is
		/// actually the start of a new footnote)</returns>
		/// <remarks> Note: This logic is similar (and probably needs to be kept in synch with)
		/// ProcessBTEndMarker, but we can't use that method, because the details differ.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private bool ProcessEndMarker()
		{
			if ((m_styleProxy.Domain & MarkerDomain.Footnote) != 0 && !m_fInFootnote)
			{
				// This is a special case of a marker (such as \ft) that is an "end marker"
				// (because it is mapped to Default Pargraph Characters) which is expected
				// to occur inside a footnote but doesn't. We let later code create a
				// new footnote for this.
				return false;
			}

			// Process footnote end marker
			if (m_fInFootnote && (m_sMarker == m_sFootnoteEndMarker ||
				m_sFootnoteEndMarker == null ||
				(m_styleProxy.Domain & MarkerDomain.Footnote) == 0) &&
				(!m_fInCharStyle || m_sMarker != m_sCharStyleEndMarker || m_sCharStyleEndMarker == null))
			{
				EndCharStyle();
				// We don't have an explicit end marker, so stay in footnote domain.
				if (m_sFootnoteEndMarker != null || (m_styleProxy.Domain & MarkerDomain.Footnote) == 0)
					EndFootnote(); // terminate footnote, clear m_fInFootnote
				// Add the text to the current paragraph
				//TODO BryanW: Use WS from proxy
				if (m_settings.ImportTranslation)
					AddTextToPara(m_sSegmentText, m_vernTextProps);
				return true;
			}

			// Process char style end marker
			if (m_fInCharStyle &&
				(m_sMarker == m_sCharStyleEndMarker || m_sCharStyleEndMarker == null) &&
				(m_sMarker != m_sFootnoteEndMarker ||
				(m_fInFootnote && m_sFootnoteEndMarker == m_sCharStyleEndMarker)))
			{
				EndCharStyle();
				//Add the text to the current paragraph or footnote, without the char style props,
				// BUT... if this is a footnote segment and we're not already in a footnote, don't
				// do this. We need to let subsequent code actually create a footnote.
				//TODO BryanW: Use WS from proxy
				if (m_fInFootnote || (m_styleProxy.Domain & MarkerDomain.Footnote) == 0)
				{
					if (m_settings.ImportTranslation)
						AddTextToPara(m_sSegmentText, m_vernTextProps);
					return true;
				}
				return false;
			}

			if (m_settings.ImportTranslation)
			{
				// if end marker does not match either one we're looking for, don't worry about it --
				// just spew the text out with the requested WS.
				// If we get here, we might be at the start of a paragraph whose first verse
				// text segment is NOT a verse number, but which IS at the start of a paragraph.
				// (This is unusual, but it could happen: \c \s \p \vt) Any pending chapter # must be
				// written out.
				if (m_fChapterNumberPending && !m_fInFootnote)
					AddDropChapterNumToPara(m_ParaBldr.StringBuilder, 0);

				// Check for the special case of an "end-marker" that is really a begin marker
				// that maps to default paragraph characters but actually has an end-marker of
				// its own. This could be an \ft...\ft* (footnote text) inside a \f (footnote).
				if (!string.IsNullOrEmpty(m_styleProxy.EndMarker))
					SetInCharacterStyle();

				AddTextToPara(m_sSegmentText, m_styleProxy.TsTextProps);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process segment which is the beginning of a new book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessStartOfBook()
		{
			FinalizeBook();
			StartingNewBook();
			//m_currSection = null; // Done with the previous section
			//m_iCurrSection = -1;

			// Determine current Book number. For now we rely on a ContextValues.Book
			// tag to come from the TextEnum as the first tag of every book.
			// m_nBookNumber should be set before we call PrepareToImportNewBook().
			m_nBookNumber = SOWrapper.SegmentFirstRef.Book;

			Logger.WriteEvent(string.Format("Importing book {0}", m_nBookNumber));
			m_prevRef = new BCVRef(m_currentRef);
			m_currentRef = new BCVRef(m_nBookNumber, 1, 0);

			if (m_importDomain != ImportDomain.Main || !m_settings.ImportTranslation)
			{
				// We may have imported the vernacular to the imported version in a previous
				// pass. If so, we want to use that version (i.e., import our BT into that or
				// associate annotations with the objects in it).
				// If not, then we use the book in the current version. Errors are reported
				// (mostly elsewhere) if we don't HAVE a vernacular.

				UpdateProgressDlgForBook(m_importDomain == ImportDomain.BackTrans ?
					"kstidImportingBackTranslation" : "kstidImportingAnnotations");

				// If we aren't importing either vernacular or BT, we aren't going to change it
				// (presumably only importing notes), so we don't need a copy.
				m_fImportingToMain = (m_importDomain == ImportDomain.BackTrans ||
					(m_importDomain == ImportDomain.Main && m_settings.ImportBackTranslation));
				m_scrBook = m_undoManager.PrepareBookNotImportingVern(m_nBookNumber, m_fImportingToMain);

				if (m_importDomain == ImportDomain.BackTrans && m_scrBook == null &&
					m_settings.ImportBackTranslation)
				{
					throw new ScriptureUtilsException(SUE_ErrorCode.BackTransMissingVernBook, null, null,
						ScrReference.NumberToBookCode(m_nBookNumber), null, null, false);
				}

				ResetStateVariablesForNewBook();
				SetBookAnnotations();
				if (m_importDomain == ImportDomain.BackTrans)
					m_wsCurrBtPara = SOWrapper.CurrentWs(m_wsAnal);
				SOWrapper.SetBookCheckSum(m_scrBook);
				return;
			}

			PrepareToImportNewBook();
			SOWrapper.SetBookCheckSum(m_scrBook);

			if (!m_settings.ImportTranslation)
				return;

			// Set the id text for the book, everything following the 3-letter book code.
			string idText = m_sSegmentText.Trim();
			int spacePos = idText.IndexOf(" ");
			if (spacePos == -1)
				CurrentBook.IdText = string.Empty;
			else
				CurrentBook.IdText = idText.Substring(spacePos + 1);

			// Set the title of the book in the UI language.
			CurrentBook.Name.UserDefaultWritingSystem =
				TsStringUtils.MakeTss(CurrentBook.BestUIName, m_cache.DefaultUserWs);

			// Create an empty title paragraph in case we don't get a Main Title.
			FinalizePrevTitle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process segment which is the beginning of a new chapter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessStartOfChapter()
		{
			m_importCallbacks.Step(0);

			if (m_fInFootnote)
				EndFootnote(); // any current footnote is terminated

			if (!m_fInScriptureText || m_ParaBldr.Length > kMaxParaSizeForChapterBrk) // See TE-2753
				FinalizePrevParagraph(); // finish up any accumulated paragraph, start a new one

			// Make token for the chapter number
			// TODO TeTeam(TomB): If/when TextSegment object is modified to return
			// actual chapter number as a separate text parameter, just use it and remove
			// the following code that attempts to rebuild the chapter number. This
			// existing approach won't handle weird chapter numbers (e.g., F)
			m_nChapter = SOWrapper.SegmentFirstRef.Chapter;
			m_prevRef = new BCVRef(m_currentRef);
			m_currentRef.Chapter = m_nChapter;
			m_currentRef.Verse = 1;
			m_foundAChapter = true;

			if (m_importDomain == ImportDomain.Annotations)
				return;

			m_ttpChapterNumber = m_styleProxy.TsTextProps;
			if (m_importDomain == ImportDomain.Main)
				m_fChapterNumberPending = true;
			m_fBackTransChapterNumPending = true;
			m_btPendingChapterNumAdded.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a book title. Note: they're always a new paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessBookTitleStart()
		{
			// A new book title is always a new paragraph, even if this segment is a character
			// style. Terminate any current footnote.
			if (m_fInFootnote)
				EndFootnote();

			// Finish up any accumulated paragraph, start a new one
			FinalizePrevParagraph();

			// anything after a book title should be in a new section
			FinalizePrevSection();
			m_currSection = null;

			// set paragraph style property for new para
			m_ParaBldr.ParaStylePropsProxy = (m_styleProxy.StyleType == StyleType.kstParagraph) ?
				m_styleProxy : m_BookTitleParaProxy;

			StartBookTitle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// if we are in an intro paragraph (i.e. not in scripture content) then
		/// make sure the section head has an intro section head style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckForSectionHeadInIntroMaterial()
		{
			if (!m_fInScriptureText && !m_fCurrentSectionIsIntro && m_sectionHeading != null)
			{
				m_fCurrentSectionIsIntro = true;
				foreach (IStTxtPara para in m_sectionHeading.ParagraphsOS)
				{
					if (para.StyleRules ==
						StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead))
					{
						para.StyleRules =
							StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroSectionHead);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a hard line break to separate this segment from the previous one. Then insert
		/// this segment as a new run. Use this version only for vernacular Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddBookTitleSegment()
		{
			AddBookTitleSegment(m_ParaBldr.StringBuilder, m_ParaBldr.ParaStylePropsProxy,
				m_vernTextProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a hard line break to separate this segment from the previous one. Then insert
		/// this segment as a new run.
		/// </summary>
		/// <param name="bldr">The STRING builder.</param>
		/// <param name="titleParaStyleProxy">The paragraph style proxy of the current title
		/// paragraph.</param>
		/// <param name="props">The text props to use for hard line break character and also
		/// paragraph text if current segment is a paragraph segment.</param>
		/// ------------------------------------------------------------------------------------
		private void AddBookTitleSegment(ITsStrBldr bldr,
			IParaStylePropsProxy titleParaStyleProxy, ITsTextProps props)
		{
			// Add a hard line break, if we have text
			if (bldr.Length > 0)
			{
				// First trim trailing space if necessary
				string s = bldr.Text;
				if (UnicodeCharProps.get_IsSeparator(s[s.Length - 1]))
					bldr.Replace(s.Length - 1, s.Length, null, null);

				AddTextToPara(kHardLineBreak, props, bldr);
			}
			if (m_styleProxy == titleParaStyleProxy)
				AddTextToPara(m_sSegmentText, props, bldr);
			else
				AddTextToPara(m_sSegmentText, m_styleProxy.TsTextProps, bldr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the Name property of the book if the current segment is the Main Title and the
		/// name has not already been set (i.e., by a Title Short segment).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetBookName()
		{
			string sTitle = m_sSegmentText.TrimEnd();
			int ws = m_styleProxy.WritingSystem;
			if (ws == -1)
				ws = GetWsForImportDomain();

			// ENHANCE (TE-5230): Probably want to have the Title Short, if any, always take
			// precedence over the main title for the purpose of setting the Name property.
			// To do that, we'd need to set a flag (on a per-WS basis) if the name is set
			// based on Title Short; if that flag is set, then this method does nothing.
			ITsString tssBookName = CurrentBook.Name.get_String(ws);
			if (m_styleProxy.StyleId == ScrStyleNames.MainBookTitle && sTitle.Length > 0 &&
				tssBookName != null && String.IsNullOrEmpty(tssBookName.Text))
			{
				CurrentBook.Name.set_String(ws, TsStringUtils.MakeTss(sTitle, ws));

				// To show the vernacular name in the progress dialog, enable this code
				//					if (m_progressDlg != null)
				//					{
				//						string sImportMsgFmt = TeResourceHelper.GetResourceString("kstidImportingBook");
				//						m_progressDlg.StatusMessage = string.Format(sImportMsgFmt, sTitle);
				//					}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a segment which initiates a new section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessSectionHeadStart()
		{
			// A new section heading is always a new paragraph, even if this segment is a
			// character style. Terminate any current footnote.
			if (m_fInFootnote)
				EndFootnote();

			// Finish up any accumulated paragraph, and start a new one.
			FinalizePrevParagraph();

			// Are we finished with intro sections and now doing Scripture sections?
			if (m_context != ContextValues.Intro)
				m_fCurrentSectionIsIntro = false;

			// Establish a new section unless we have an empty section waiting for us
			// (presumably created because of a preceding \c marker)
			if (m_settings.ImportTranslation && (m_currSection == null ||
				!m_currSection.IsValidObject ||
				m_currSection.ContentOA.ParagraphsOS.Count > 0))
			{
				MakeSection();
			}
			else if (!m_settings.ImportTranslation)
				m_iCurrSection++;

			m_iNextBtPara = 0;

			// set paragraph style property for new section head para
			if (m_styleProxy.StyleType == StyleType.kstParagraph)
				m_ParaBldr.ParaStylePropsProxy = m_styleProxy;
			else
			{
				// segment is a char style
				m_ParaBldr.ParaStylePropsProxy = m_fInScriptureText ?
				m_ScrSectionHeadParaProxy : m_DefaultIntroSectionHeadParaProxy;
			}

			m_fInVerseTextParagraph = false;
			m_fInSectionHeading = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a hard line break to separate this segment from the previous one. Then insert
		/// this segment as a new run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddSectionHeadSegment()
		{
			// Now process this segment. If it is a different paragraph style, finalize prev
			// para and start a new one. Otherwise, use a hard line break to separate this
			// segment from the previous one.
			if (m_styleProxy != m_ParaBldr.ParaStylePropsProxy &&
				m_styleProxy.StyleType == StyleType.kstParagraph)
			{
				FinalizePrevParagraph();
				// set paragraph style property for new para
				m_ParaBldr.ParaStylePropsProxy = m_styleProxy;
				// Create the paragraph and add this segment to it.
				if (m_settings.ImportTranslation)
					AddTextToPara(m_sSegmentText);
			}
			else if (m_settings.ImportTranslation)
			{
				m_ParaBldr.TrimTrailingSpaceInPara();
				if (m_ParaBldr.Length > 0)
				{
					AddTextToPara(kHardLineBreak, m_vernTextProps);
				}
				if (m_styleProxy == m_ParaBldr.ParaStylePropsProxy)
					AddTextToPara(m_sSegmentText, m_vernTextProps);
				else
					AddTextToPara(m_sSegmentText);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares a new Scripture section section (if needed). Used when the imported data
		/// switches from book title or background information to scripture data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PrepareForFirstScriptureSection()
		{
			FinalizePrevParagraph();

			// If there isn't a current section or there is and the section has content, then
			// create an implicit section break before chapter one, verse one of the scripture.
			// Otherwise an implicit section won't be created since it's assumed the current
			// section is the beginning of scripture data (as opposed to intro. or background
			// material.
			if (m_settings.ImportTranslation && m_importDomain == ImportDomain.Main &&
				(m_currSection == null || !m_currSection.IsValidObject ||
				m_currSection.ContentOA.ParagraphsOS.Count > 0))
			{
				MakeSection();
			}

			m_fInScriptureText = true;
			m_fCurrentSectionIsIntro = false;

			// The paragraph style is set to a default here, but may get reset later, in
			// ProcessSegment. We set the default here because it won't get reset later
			// if the current segment is marked with a character style.
			m_ParaBldr.ParaStylePropsProxy = m_vernParaStyleProxy = m_DefaultScrParaProxy;
			m_context = ContextValues.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a segment which may end a section head or book title. Prepare for a new
		/// paragraph by setting the m_ParaBldr.ParaStylePropsProxy and m_fInVerseTextParagraph
		/// members.
		/// </summary>
		/// <returns><c>false</c> if segment doesn't really end the current title or section
		/// head element. <c>true</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		private bool PrepareForNextPara()
		{
			if (m_context == ContextValues.Note)
			{
				// This is a footnote, so it doesn't really end the title or section head.
				return false;
			}

			// Current section head or book title para is complete, force a new para
			FinalizePrevParagraph();  // finish up any accumulated paragraph, start a new one

			// set paragraph style property for new para, also VerseTextParagraph flag
			if (m_styleProxy.StyleType == StyleType.kstParagraph)
			{
				m_ParaBldr.ParaStylePropsProxy = m_styleProxy;
				m_fInVerseTextParagraph = (m_context == ContextValues.Text);
			}
			else
			{
				// segment is a char style
				m_fInVerseTextParagraph = m_fInScriptureText;
				m_ParaBldr.ParaStylePropsProxy = (m_fInScriptureText)?
				m_DefaultScrParaProxy : m_DefaultIntroParaProxy;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Choose the correct default WS based on the current import domain
		/// </summary>
		/// <returns>HVO of WS to use</returns>
		/// ------------------------------------------------------------------------------------
		private int GetWsForImportDomain()
		{
			if (m_importDomain == ImportDomain.Main)
				return m_wsVern;
			else
				return m_SOWrapper.CurrentWs(m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean-up any last paragraphs and such
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void FinalizeImport()
		{
			if (m_fFoundABook)
			{
				m_importCallbacks.Position = m_importCallbacks.Maximum;
				m_importCallbacks.StatusMessage =
					TeResourceHelper.GetResourceString("kstidImportFinalizing");
			}

			FinalizeBook();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizes the current book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FinalizeBook()
		{
			if (m_sPendingFootnoteText != null)
				AddTextToPara(m_sPendingFootnoteText);

			if (m_fInFootnote)
				EndFootnote();

			if (m_currPictureInfo != null)
				InsertPicture();

			// Spew out the last bit of stuff in the builder, if any.
			FinalizePrevParagraph();
			FinalizePrevTitle();
			// Ensure we have at least one section for this book.
			if (m_currSection == null && m_settings.ImportTranslation && CurrentBook != null &&
				m_importDomain == ImportDomain.Main)
			{
				MakeSection();
			}
			FinalizePrevSection();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizes the title. If is empty, a single blank paragraph is written for
		/// whatever is missing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void FinalizePrevTitle()
		{
			if (m_cache == null || m_Title == null)
				return;

			// First, check if there is content. If not, add a blank paragraph.
			if (m_Title.ParagraphsOS.Count == 0)
			{
				StTxtParaBldr titleParaBldr = new StTxtParaBldr(m_cache);
					titleParaBldr.ParaStylePropsProxy = m_BookTitleParaProxy;
					titleParaBldr.CreateParagraph(m_Title);
				}

			m_fInBookTitle = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets importer state variables for the new book
		/// Need to reset a few more things than the base class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ResetStateVariablesForNewBook()
		{
			base.ResetStateVariablesForNewBook();
			m_nChapter = 1;
			m_foundAChapter = false;
			m_iNextBtPara = 0;
			m_vernParaStyleProxy = null;
			m_lastPara = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the verse part of the reference (as a string) from current Scripture segment
		/// </summary>
		/// <param name="wsBt">Writing system of the back translation for which this reference
		/// is being formatted or 0 if for the vernacular text.</param>
		/// <remarks>The string returned includes any sub-verse letters, etc. that would be
		/// hard to recreate from a numeric verse number (e.g., "2-6a")</remarks>
		/// ------------------------------------------------------------------------------------
		protected string GetVerseRefAsString(int wsBt)
		{
			BCVRef refFirst = SOWrapper.SegmentFirstRef;
			BCVRef refLast = SOWrapper.SegmentLastRef;

			string sVerseFirst;
			string sVerseLast;
			// convert verse numbers to proper decimal string
			if (wsBt == 0)
			{
				sVerseFirst = m_scr.ConvertToString(refFirst.Verse);
				sVerseLast = m_scr.ConvertToString(refLast.Verse);
			}
			else
			{
				sVerseFirst = refFirst.Verse.ToString();
				sVerseLast = refLast.Verse.ToString();
			}

			// get local copy of segment integers
			int nSegFirst = refFirst.Segment;
			int nSegLast = refLast.Segment;

			// build the reference string to return using first verse info
			string sRef;
			if (nSegFirst > 0)
			{
				// Convert nSeg to lowercase alpha
				// TODO: Need to deal with language-dependent sort orders
				sRef = string.Concat(sVerseFirst, Convert.ToChar(nSegFirst + 96));
			}
			else
			{
				// No segment letter
				sRef = sVerseFirst;
			}

			// if this is a bridged reference, append last verse info
			if (sVerseFirst != sVerseLast || nSegFirst != nSegLast)
			{
				// Convert nSeg to lowercase alpha if needed
				sRef = string.Concat(sRef, m_scr.BridgeForWs(wsBt), sVerseLast,
					(nSegLast > 0) ? Convert.ToChar(nSegLast + 96).ToString() : string.Empty);
			}
			return sRef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the given text (using the text props from the current proxy) as a new run to the
		/// end of the current paragraph.
		/// </summary>
		/// <param name="sText">Text to be appended to the paragraph being built</param>
		/// ------------------------------------------------------------------------------------
		protected void AddTextToPara(string sText)
		{
			AddTextToPara(sText, m_styleProxy.TsTextProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the given text and props as a new run to the end of the current paragraph.
		/// </summary>
		/// <param name="sText">Text to be appended to the paragraph being built</param>
		/// <param name="pttpProps">Properties (should contain only a named style) for the run
		/// of text to be added.</param>
		/// ------------------------------------------------------------------------------------
		protected void AddTextToPara(string sText, ITsTextProps pttpProps)
		{
			AddTextToPara(sText, pttpProps, m_ParaBldr.StringBuilder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the given text and props as a new run to the end of the specified paragraph
		/// under construction.
		/// </summary>
		/// <param name="sText">Text to be appended to the paragraph being built</param>
		/// <param name="pttpProps">Properties (should contain only a named style) for the run
		/// of text to be added.</param>
		/// <param name="strbldr">String builder of paragraph being built</param>
		/// ------------------------------------------------------------------------------------
		protected void AddTextToPara(string sText, ITsTextProps pttpProps, ITsStrBldr strbldr)
		{
			// Don't bother trying to add empty runs. Also don't add runs consisting of a single
			// space if processing a marker that maps to a paragraph style.
			if (sText.Length > 0 &&
				(sText != " " || m_styleProxy.StyleType == StyleType.kstCharacter))
			{
				// send the text and props directly to the Builder, after first ensuring that
				// a ws is specified.
				int var;
				int ws = pttpProps.GetIntPropValues((int)FwTextPropType.ktptWs,
					out var);
				if (ws == -1)
				{
					ws = GetWsForContext(strbldr);
					ITsPropsBldr tpb = pttpProps.GetBldr();
					tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);
					pttpProps = tpb.GetTextProps();
				}
				int cchLength = strbldr.Length;
				// Remove extra space.
				if (cchLength > 0 && UnicodeCharProps.get_IsSeparator(sText[0]))
				{
					string s = strbldr.GetChars(cchLength - 1, cchLength);
					if (UnicodeCharProps.get_IsSeparator(s[0]))
						sText = sText.Substring(1);
				}

				if (sText != string.Empty || cchLength == 0)
					strbldr.Replace(cchLength, cchLength, sText, pttpProps);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the writing system for the context. First use the WS of the para, if any
		/// (i.e., for BT paras); otherwise use the ws of the preceding segment, if any;
		/// otherwise use the default WS for the domain.
		/// </summary>
		/// <param name="strbldr">The string builder of the paragraph being built</param>
		/// <returns>HVO of writing system to be used</returns>
		/// ------------------------------------------------------------------------------------
		private int GetWsForContext(ITsStrBldr strbldr)
		{
			// Get the ws of the current paragraph.
			int ws = m_wsPara;
			if (ws <= 0)
			{
				// Get the ws from the preceding character(s).
				if (strbldr != null)
				{
					int cch = strbldr.Length;
					if (cch > 0)
					{
						ITsTextProps ttpCur = strbldr.get_PropertiesAt(cch - 1);
						int var;
						ws = ttpCur.GetIntPropValues((int)FwTextPropType.ktptWs,
							out var);
					}
				}
				if (ws <= 0)
				{
					// If all else fails, use the writing system appropriate for the
					// current IMPORT domain.
					ws = GetWsForImportDomain();
				}
			}
			return ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the pending chapter number string into the beginning of a new paragraph,
		/// in proper location as a drop-cap.
		/// </summary>
		/// <param name="bldr">The string builder of the paragraph being built</param>
		/// <param name="ws">HVO of writing system to be used for chapter number (and
		/// preceding space if this is an intra-paragraph chapter number). Can be 0 to use
		/// vernacular or negative to get the correct WS for the context.</param>
		/// ------------------------------------------------------------------------------------
		protected void AddDropChapterNumToPara(ITsStrBldr bldr, int ws)
		{
			Debug.Assert(ScrStyleNames.ChapterNumber ==
				m_ttpChapterNumber.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Compute the properties for the chapter number. If this is in BT, ws will be
			// non-zero; we use the specified ws.
			ITsTextProps chapterNumProps = m_ttpChapterNumber;
			bool fVernacular = (ws == 0);
			if (!fVernacular)
			{
				ITsPropsBldr propsBldr = chapterNumProps.GetBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);
				chapterNumProps = propsBldr.GetTextProps();
			}

			// If inserting this chapter number in the middle of an existing paragraph, put in a
			// space if necessary.
			int cchBldr = bldr.Length;
			if (cchBldr > 0 && bldr.GetChars(cchBldr - 1, cchBldr) != " ")
			{
				ITsTextProps spaceProps = GetTextPropsWithWS(ws, bldr);
				AddTextToPara(" ", spaceProps, bldr);
			}

			// now insert the chapter number
			string sChapterNumber;
			if (fVernacular)
				sChapterNumber = m_scr.ConvertToString(m_nChapter);
			else
				sChapterNumber = m_nChapter.ToString();

			AddTextToPara(sChapterNumber, chapterNumProps, bldr);

			// clean up
			if (fVernacular)
				m_fChapterNumberPending = false;
			else
				m_btPendingChapterNumAdded.Add(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we have any pending text in buffers or Para builder, finish making the paragraph
		/// and add it to the database. Add it to the current title or section (make a section
		/// if we need one and don't have one). Clean up the string builder to prepare for a
		/// new paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void FinalizePrevParagraph()
		{
			// If a footnote is in progress, then finalize it first
			if (m_fInFootnote)
				EndFootnote();

			// Now create the paragraph, if we have text.
			if (m_ParaBldr.Length > 0 && m_scrBook != null)
			{
				m_ParaBldr.TrimTrailingSpaceInPara();

				// Now append the paragraph in m_ParaBldr to the database
				if (m_fInBookTitle)
				{
					m_lastPara = (IScrTxtPara)m_ParaBldr.CreateParagraph(m_Title);
				}
				else
				{
					// Before we insert the paragraph, we have to make sure we have a section.
					if (m_currSection == null)
						MakeSection();

					m_lastPara = (IScrTxtPara)m_ParaBldr.CreateParagraph(
						m_fInSectionHeading ? m_sectionHeading : m_sectionContent);
				}
			}
			else if (((ImportStyleProxy)m_ParaBldr.ParaStylePropsProxy).Function == FunctionValues.StanzaBreak &&
				!m_fInBookTitle && m_currSection != null)
			{
				// Stanza Breaks are the exception to the rule. We expect these to be empty paragraphs!
				m_lastPara = (IScrTxtPara)m_ParaBldr.CreateParagraph(m_sectionContent);
			}
			AddPendingAnnotations();
			AddBackTranslations();
			if (m_importDomain != ImportDomain.BackTrans)
				m_wsCurrBtPara = 0;
			m_CurrParaFootnotes.Clear(); // REVIEW: Should this be part of AddBackTranslations instead?
			m_CurrParaPictures.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add any accumulated Back Translation(s) (one per WS) to the last paragraph created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddBackTranslations()
		{
			if ((m_vernParaStyleProxy == null || m_vernParaStyleProxy.Style == null) && m_lastPara == null)
			{
				if (m_BTStrBldrs.Count > 0)
					ReportBTTextNotPartOfPara();
				return;
			}

			if (!m_fChapterNumberPending && m_importDomain == ImportDomain.Main)
				m_fBackTransChapterNumPending = false;

			if (m_BTStrBldrs.Count > 0)
			{
				if (m_scrBook == null)
				{
					throw new ScriptureUtilsException(SUE_ErrorCode.BackTransMissingVernBook, null, null,
						ScrReference.NumberToBookCode(m_nBookNumber), null, null, false);
				}
				ICmTranslation trans = null;
				bool fFoundSomeText = false;
				bool fAppend = false;
				foreach (KeyValuePair<int, ITsStrBldr> kvp in m_BTStrBldrs)
				{
					int ws = kvp.Key;
					ITsStrBldr bldr = kvp.Value;
					int length = bldr.Length;
					if (length > 0 || m_BtFootnoteStrBldrs.Count > 0)
					{
						if (trans == null)
							trans = CreateCmTranslationForPendingBT(ws, bldr.Text, out fAppend);

						AddPendingBTFootnotes(ws, bldr);

						// check if the last char sent to the builder is a space
						TrimTrailingSpace(bldr);
						if (fAppend)
						{
							bldr.Replace(0, 0, kHardLineBreak, null);
							bldr.ReplaceTsString(0, 0, trans.Translation.get_String(ws));
						}
						trans.Translation.set_String(ws, bldr.GetString());
						fFoundSomeText = true;
					}
				}

				m_BTStrBldrs.Clear();
				// Only advance counter if paragraph had text or it is a stanza break.Empty paras
				// don't get created except for stanza break lines.
				if (fFoundSomeText || m_vernParaStyleProxy.Function == FunctionValues.StanzaBreak)
					m_iNextBtPara++;
			}
			m_BtFootnoteStrBldrs.Clear();
			m_BTfootnoteIndex.Clear();

			AddBTPictureCaptionsAndCopyrights();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throw an exception to report BT text that does not correspond to vernacular text
		/// </summary>
		/// <exception cref="ScriptureUtilsException">BT text not part of paragraph</exception>
		/// ------------------------------------------------------------------------------------
		private void ReportBTTextNotPartOfPara()
		{
			ITsStrBldr bldr = null;
			foreach (int key in m_BTStrBldrs.Keys)
			{
				bldr = m_BTStrBldrs[key];
				break;
			}
			Debug.Assert(bldr != null);
			string sCharStyle = bldr.get_Properties(0).GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle);

			if (sCharStyle == null)
				sCharStyle = StyleUtils.DefaultParaCharsStyleName;

			throw new ScriptureUtilsException(
				SUE_ErrorCode.BackTransTextNotPartOfParagraph,
				string.Format(TeResourceHelper.GetResourceString("kstidBTTextNotPartOfParaDetails"), bldr.Text, sCharStyle),
				m_SOWrapper.CurrentFileName, m_scrBook.BookId,
				null, null, (m_importDomain == ImportDomain.Main));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new CmTranslation to hold the BT in the given builder (and possibly BTs in
		/// other writing systems as well).
		/// </summary>
		/// <param name="ws">The writing system of the BT</param>
		/// <param name="btText">The text of the BT, from the string builder (for error
		/// reporting)</param>
		/// <param name="fAppend">Indicates to caller that the found paragraph already has
		/// been used previously, so new (BT) text should be appended to the existing back
		/// translation (with a hard line break separating the two parts). This is needed to
		/// support special logic we do in section headers when two paragraphs with the same
		/// style are imported in succession.</param>
		/// <returns>A new CmTranslation</returns>
		/// ------------------------------------------------------------------------------------
		private ICmTranslation CreateCmTranslationForPendingBT(int ws, string btText,
			out bool fAppend)
		{
			fAppend = false;
			ICmTranslation trans;
			if (m_prevImportDomain == ImportDomain.BackTrans ||
				(m_prevImportDomain == ImportDomain.Main && !m_settings.ImportTranslation))
			{
				BCVRef targetRef = (BtChapterNumPending(ws) &&
					m_vernParaStyleProxy.Style.Structure == StructureValues.Body) ?
					new BCVRef(m_prevRef) : new BCVRef(m_currentRef);
				if (m_vernParaStyleProxy.Style.Context != ContextValues.Intro &&
					targetRef.Verse == 0)
				{
					targetRef.Verse = 1;
				}

				// FindCorrespondingVernParaForSegment may change m_iSection.
				// However, if we're importing interleaved BT we want to keep
				// m_iSection at it's current value.
				int iSectionTmp = m_iCurrSection;
				if (FindCorrespondingVernParaForSegment(m_vernParaStyleProxy.Style,
					targetRef, out fAppend) == null)
				{
					// Got an unexpected BT paragraph segment. Can't find a corresponding
					// vernacular paragraph.
					string sParaContents = btText;
					if (sParaContents != null && sParaContents.Length > 120)
						sParaContents = sParaContents.Substring(0, 120) + "...";
					throw new ScriptureUtilsException(
						SUE_ErrorCode.BackTransParagraphMismatch,
						sParaContents, string.Format(TeResourceHelper.GetResourceString(
						"kstidBTNoCorrespondingParaDetails"),
						m_vernParaStyleProxy.StyleId, targetRef.AsString),
						null, null, null, false);
				}
				if (m_prevImportDomain == ImportDomain.Main)
					m_iCurrSection = iSectionTmp;
			}
			if (m_lastPara == null)
			{
				throw new ScriptureUtilsException(
					SUE_ErrorCode.BackTransTextNotPartOfParagraph,
					string.Format(TeResourceHelper.GetResourceString("kstidBTTextNotPartOfParaDetails"),
					btText, m_vernParaStyleProxy.Style.Name),
					m_SOWrapper.CurrentFileName, m_scrBook.BookId,
					null, null, (m_importDomain == ImportDomain.Main));
			}
			trans = m_lastPara.GetOrCreateBT();
			return trans;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add footnote callers to the string builder being built for the Back Translation and
		/// add BT to the footnotes themselves.
		/// </summary>
		/// <param name="ws">The writing system of the BT</param>
		/// <param name="bldr">The string builder for the back translation paragraph text
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void AddPendingBTFootnotes(int ws, ITsStrBldr bldr)
		{
			int iFootnote = 0;
			foreach (BtFootnoteBldrInfo info in m_BtFootnoteStrBldrs)
			{
				if (info.ws == ws)
				{
					IStFootnote footnote = FindCorrespondingFootnote(info.styleId, iFootnote);
					if (footnote == null)
					{
						throw new ScriptureUtilsException(
							SUE_ErrorCode.BackTransMissingVernFootnote,
							null, 0, info.bldr.Text,
							CurrentBook.BookId,
							info.reference.Chapter.ToString(),
							info.reference.Verse.ToString(),
							m_importDomain == ImportDomain.Main);
					}
					// Only allow one paragraph per footnote
					IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
					ICmTranslation transl = footnotePara.GetOrCreateBT();
					transl.Translation.set_String(ws, info.bldr.Length == 0 ?
						m_TsStringFactory.MakeString(string.Empty, ws) : info.bldr.GetString());

					footnote.InsertRefORCIntoTrans(bldr, info.ichOffset + iFootnote, ws);
					iFootnote++;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process any pending picture caption/copyright back translations.
		/// </summary>
		/// <exception cref="ScriptureUtilsException">BT missing vernacular picture</exception>
		/// ------------------------------------------------------------------------------------
		private void AddBTPictureCaptionsAndCopyrights()
		{
#pragma warning disable 219
			ITsStrFactory factory = TsStrFactoryClass.Create();
#pragma warning restore 219
			for (int index = 0; index < m_BTPendingPictures.Count; index++)
			{
				ICmPicture picture = FindCorrespondingPicture(index);
				BTPictureInfo pictureInfo = m_BTPendingPictures[index];
				if (picture == null)
				{
					// throw an error to indicate that there is not a matching picture
					throw new ScriptureUtilsException(SUE_ErrorCode.BackTransMissingVernPicture,
						pictureInfo.m_filename, pictureInfo.m_lineNumber,
						pictureInfo.m_segment, pictureInfo.m_ref, m_importDomain == ImportDomain.Main);
				}

				ITsString tssCaption = pictureInfo.m_strbldrCaption.GetString();
				if (tssCaption != null)
					picture.Caption.set_String(pictureInfo.m_ws, tssCaption);
				if (!String.IsNullOrEmpty(pictureInfo.m_copyright))
					picture.PictureFileRA.Copyright.set_String(pictureInfo.m_ws, pictureInfo.m_copyright);
			}
			m_BTPendingPictures.Clear();
			m_currBtPictureInfo = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Trims the last character from the given string builder if it's a separator
		/// </summary>
		/// <param name="bldr">The string builder</param>
		/// ------------------------------------------------------------------------------------
		private void TrimTrailingSpace(ITsStrBldr bldr)
		{
			int length = bldr.Length;
			if (length == 0)
				return;
			string s = bldr.GetChars(length - 1, length);
			if (UnicodeCharProps.get_IsSeparator(s[0]))
			{
				// remove the trailing space from the builder
				bldr.Replace(length - 1, length, null, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Begin a footnote: insert the footnote into the ScrBook and insert the footnote
		/// marker into the paragraph. Subsequent footnote segments will be added to the
		/// footnote paragraph until <see cref="EndFootnote"/> is called.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void BeginFootnote()
		{
			Debug.Assert(!m_fInFootnote);

			// Check to see if this footnote is a USFM style footnote or otherwise begins with a
			// single character that is likely to be a footnote marker. If so, strip the marker
			// from the data.
			CheckDataForFootnoteMarker();

			// If the footnote marker is the auto marker, then generate a marker for it
			ImportStyleProxy proxy =
				(m_styleProxy.StyleType == StyleType.kstParagraph) ? m_styleProxy :
				m_DefaultFootnoteParaProxy;

			string sFootnoteMarker = m_scr.GetFootnoteMarker(proxy.StyleId);

			// If the last character in the paragraph is a separator, then insert the footnote
			// marker before it. (TE-2431)
			ITsStrBldr strbldr = m_ParaBldr.StringBuilder;
			int ichMarker = m_ParaBldr.Length;
			bool fInsertSpaceAfterCaller = false;
			if (UnicodeCharProps.get_IsSeparator(m_ParaBldr.FinalCharInPara))
				ichMarker--;
			else if (m_settings.ImportTypeEnum == TypeOfImport.Other)
			{
				// Check to see if we are inserting this right after a verse.
				ITsTextProps propsOfLastRun = strbldr.get_Properties(strbldr.RunCount - 1);
				string sStyleOfLastRun = propsOfLastRun.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				if (sStyleOfLastRun == ScrStyleNames.VerseNumber)
					fInsertSpaceAfterCaller = true;
			}

			// When importing a whole book, we can call this version of InsertFootnoteAt and save
			// the time of trying to look for the previous footnote each time and checking to see
			// if we need to resequence following footnotes.
			// TODO (TE-920 and/or TE-431):
			m_CurrFootnote = m_scrBook.InsertFootnoteAt(m_iCurrFootnote, strbldr, ichMarker);

			if (fInsertSpaceAfterCaller)
				strbldr.Replace(strbldr.Length, strbldr.Length, " ", m_vernTextProps);

			// Set up the paragraph builder
			m_SavedParaBldr = m_ParaBldr;
			m_ParaBldr = new StTxtParaBldr(m_cache);
			m_ParaBldr.ParaStylePropsProxy = proxy;
			m_CurrParaFootnotes.Add(new FootnoteInfo(m_CurrFootnote, proxy.StyleId));



			// remember that we are now processing a footnote
			SetInFootnote();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the current footnote data is a USFM style footnote or otherwise
		/// begins with a single character that is likely to be a footnote marker. If so, strip
		/// the marker from the data. If we aren't sure, then we leave the data as is but let
		/// the caller know.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CheckDataForFootnoteMarker()
		{
			string[] segments = m_sSegmentText.TrimStart().Split(new char[] {' '}, 2);
			bool fSingleToken = (segments.Length == 1 || segments[1].Trim() == string.Empty);
			if (segments[0] == "+")
			{
				m_sSegmentText = fSingleToken ? string.Empty : segments[1];
				if (m_fInterpretFootnoteSettings)
				{
					m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
				}
			}
			else if (segments[0] == "-")
			{
				m_sSegmentText = fSingleToken ? string.Empty : segments[1];
				if (m_fInterpretFootnoteSettings)
				{
					m_scr.FootnoteMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
				}
			}
			else if (segments[0] == "*")
			{
				m_sSegmentText = fSingleToken ? string.Empty : segments[1];
				if (m_fInterpretFootnoteSettings)
				{
					m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
					m_scr.FootnoteMarkerSymbol = "*";
				}
			}
			else if (fSingleToken)
			{
				// Based on the next segment, we'll determine whether this token is data or
				// a marker.
				m_sPendingFootnoteText = m_sSegmentText;
				m_sSegmentText = string.Empty;
			}
		}

		// TODO BryanW: Someday handle additional paragraphs in a footnote
		// protected void AddParaToFootnote(string sText, byte[] rgbParaProps)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When finished adding all segments of the current footnote, call this to reset
		/// builder and other state variables to resume normal segment processing and increment
		/// the footnote counter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void EndFootnote()
		{
			if (m_BTFootnoteStrBldr != null)
			{
				EndBTFootnote();
				return;
			}
			Debug.Assert(m_fInFootnote);
			m_ParaBldr.TrimTrailingSpaceInPara();
			// Create the paragraph even if the contents are empty; otherwise user will have no
			// way to enter contents later.
			m_lastPara = (IScrTxtPara)m_ParaBldr.CreateParagraph(m_CurrFootnote);
			m_fInFootnote = false;
			m_sFootnoteBeginMarker = null;
			m_iCurrFootnote++; // increment for next footnote
			m_ParaBldr = m_SavedParaBldr;
			m_CurrFootnote = null;
			if ((m_styleProxy.Domain & MarkerDomain.Footnote) == 0)
				m_currDomain &= ~MarkerDomain.Footnote;
			// ENHANCE: For now, we just let the settings of the very first footnote dictate
			// what they will all look like. Later, we should do some statistical analysis.
			m_fInterpretFootnoteSettings = false;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get or set the wrapper object for TE, Paratext scripture objects to read in the
		/// data.
		/// </summary>
		/// <remarks>This class (TeSfmImporter) is responsible for disposing ScrObjWrapper!</remarks>
		/// ------------------------------------------------------------------------------------
		protected ScrObjWrapper SOWrapper
		{
			get
			{
				return m_SOWrapper;
			}
			set
			{
				if (m_SOWrapper != null)
					m_SOWrapper.Dispose();
				m_SOWrapper = value;
			}
		}

		/// <summary>
		/// Check whether we're importing from the main source.
		/// </summary>
		protected override bool InMainImportDomain
		{
			get { return m_prevImportDomain != ImportDomain.Main; }
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
					m_cpe = m_cache.ServiceLocator.UnicodeCharProps;
				return m_cpe;
			}
		}
		#endregion

		#region Methods to load import mappings to create style proxies
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load mapping proxies from m_settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void LoadImportMappingProxies()
		{
			Debug.Assert(m_settings != null);
#pragma warning disable 219
			bool fHasNonInterleavedBT = SOWrapper.HasNonInterleavedBT;
#pragma warning restore 219
			bool fHasNonInterleavedNotes = SOWrapper.HasNonInterleavedNotes;

			LoadImportMappingProxies(m_styleProxies, m_settings.Mappings(MappingSet.Main));

			if (fHasNonInterleavedNotes)
				LoadImportMappingProxies(m_notesStyleProxies, m_settings.Mappings(MappingSet.Notes));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load mapping proxies from the given enumeration of mappings
		/// </summary>
		/// <param name="styleProxies">Dictionary to add the proxy to</param>
		/// <param name="mappings"></param>
		/// ------------------------------------------------------------------------------------
		private void LoadImportMappingProxies(Dictionary<string, ImportStyleProxy> styleProxies,
			IEnumerable mappings)
		{
			// Iterate through the mappings and create style proxies
			foreach (ImportMappingInfo mapping in mappings)
			{
				if (mapping.BeginMarker == ScriptureServices.kMarkerBook)
					continue;

				AddImportStyleProxyForMapping(mapping, styleProxies);
			}

			// Map the book marker (\id) for processing purposes, but not to a real style.
			styleProxies[ScriptureServices.kMarkerBook] =
				new ImportStyleProxy(null,
				StyleType.kstParagraph,
				0, ContextValues.Book, m_styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an Import style proxy for the given mapping and include it in the hash map.
		/// </summary>
		/// <param name="mapping">The mapping for which the proxy entry is to be created</param>
		/// <param name="styleProxies">Dictionary to add the proxy to</param>
		/// <remarks>A second styleProxy is added if the mapping has an end marker.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected void AddImportStyleProxyForMapping(ImportMappingInfo mapping,
			Dictionary<string, ImportStyleProxy> styleProxies)
		{
			string styleName = mapping.StyleName;

			// If there is no style name and the target type is a style then don't
			// add a proxy entry for it.
			if (styleName == null && !mapping.IsExcluded &&
				mapping.MappingTarget == MappingTargetType.TEStyle)
			{
				return;
			}

			ImportStyleProxy proxy;
			int ws = 0;
			// add the new style proxy to the hash table
			if (mapping.WsId != null)
			{
				CoreWritingSystemDefinition wsObj;
				if (m_cache.ServiceLocator.WritingSystemManager.TryGet(mapping.WsId, out wsObj))
					ws = wsObj.Handle;
			}

			if (ws == 0)
			{
				// If domain is unspecified or if this is a BT marker, defer resolving the WS
				// until later, so it can be based on the context.
				if (mapping.Domain == MarkerDomain.Note)
					ws = m_wsAnal;
				else
					ws = -1;
			}
			if (styleName == StyleUtils.DefaultParaCharsStyleName && !mapping.IsExcluded)
			{
				// Any marker that maps to "Default Paragraph Characters" should be
				// treated like an end marker.
				proxy = new ImportStyleProxy(null, 0, ws, ContextValues.EndMarker, mapping.Domain,
					m_styleSheet);

				styleProxies[mapping.BeginMarker] = proxy;
				// Use same proxy for end marker, if it is defined. This will cause optional \ft*
				// marker to be processed correctly.
				if (!string.IsNullOrEmpty(mapping.EndMarker))
				{
					proxy.EndMarker = mapping.EndMarker;
					styleProxies[mapping.EndMarker] = proxy;
				}
				return;
			}
			else
			{
				// note that kstParagraph is overridden by real type for existing styles.
				proxy = new ImportStyleProxy(mapping, ws, m_styleSheet);
			}

			// make another proxy just for the end marker
			if (mapping.EndMarker != null && mapping.EndMarker.Length > 0)
			{
				proxy.EndMarker = mapping.EndMarker;

				int wsStuffAfterMarker = -1;
				MarkerDomain endMarkerDomain = MarkerDomain.Default;
				if (proxy.StyleType != StyleType.kstParagraph)
					endMarkerDomain = proxy.Domain;
				else if ((proxy.Domain & MarkerDomain.Footnote) == MarkerDomain.Footnote)
					endMarkerDomain = proxy.Domain ^ MarkerDomain.Footnote;
				styleProxies[mapping.EndMarker + "\uFEFF" + mapping.BeginMarker] = new ImportStyleProxy(
					null, //for an EndMarker, most params are irrelevant
					0, wsStuffAfterMarker, ContextValues.EndMarker,
					endMarkerDomain, m_styleSheet);
			}

			styleProxies[mapping.BeginMarker] = proxy;
		}
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~TeSfmImporter()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

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
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_SOWrapper != null)
					m_SOWrapper.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cpe = null;
			m_settings = null;
			m_SOWrapper = null;
			m_sSegmentText = null;
			m_sMarker = null;
			m_styleProxy = null;
			m_vernParaStyleProxy = null;
			m_vernTextProps = null;
			m_analTextProps = null;
			m_styleProxies = null;
			m_notesStyleProxies = null;
			m_lastPara = null;
			m_BookTitleParaProxy = null;
			m_DefaultFootnoteParaProxy = null;
			m_TsStringFactory = null;
			m_BTFootnoteStrBldr = null;
			m_CurrParaPictures = null;
			m_CurrParaFootnotes = null;
			m_BTPendingPictures = null;
			m_CurrBTFootnote = null;
			m_sBtFootnoteParaStyle = null;
			m_BtFootnoteStrBldrs = null;
			m_PendingAnnotations = null;
			m_BTfootnoteIndex = null;
			m_sCharStyleEndMarker = null;
			m_sFootnoteEndMarker = null;
			m_sCharStyleBeginMarker = null;
			m_sFootnoteBeginMarker = null;
			m_scrTranslatorAnnotationDef = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable & Co. implementation
	}
	#endregion
}
