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
// File: DummyFootnoteViewForm.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	#region class DummyFootnoteView
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy <see cref="FootnoteView"/> for testing purposes that allows accessing protected
	/// members.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFootnoteView : FootnoteView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public bool m_updatedDisplayMarker;
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public bool m_updatedDisplayReference;
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public string m_updatedFootnoteMarker;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyFootnoteView(FdoCache cache) : base(cache, 0, null, -1)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the selected footnote.
		/// </summary>
		/// <param name="tag">The flid of the selected footnote</param>
		/// <param name="hvoSel">The hvo of the selected footnote</param>
		/// <returns>True, if a footnote is found at the current selection</returns>
		/// -----------------------------------------------------------------------------------
		public new bool GetSelectedFootnote(out int tag, out int hvoSel)
		{
			CheckDisposed();

			return base.GetSelectedFootnote(out tag, out hvoSel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the displayed text for a footnote.
		/// </summary>
		/// <param name="iBook">Index of the book the footnote is in</param>
		/// <param name="iFootnote">Index of the footnote</param>
		/// <param name="footnote">The footnote object</param>
		/// <returns>The TsString representing the text of the footnote, including any displayed
		/// marker, reference, etc.</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetDisplayedTextForFootnote(int iBook, int iFootnote,
			StFootnote footnote)
		{
			SelectionHelper helper = new SelectionHelper();

			// Create selection in footnote marker
			SelLevInfo[] anchorLevInfo = new SelLevInfo[4];
			anchorLevInfo[3].tag = BookFilter.Tag;
			anchorLevInfo[3].ihvo = iBook;
			anchorLevInfo[2].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
			anchorLevInfo[2].ihvo = iFootnote;
			anchorLevInfo[1].tag = (int)StText.StTextTags.kflidParagraphs;
			anchorLevInfo[1].ihvo = 0;
			anchorLevInfo[0].tag = -1;
			anchorLevInfo[0].ihvo = 0;
			helper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, anchorLevInfo);
			helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
				(int)VwSpecialAttrTags.ktagGapInAttrs);
			helper.IchAnchor = 0;

			SelLevInfo[] endLevInfo = new SelLevInfo[3];
			endLevInfo[2].tag = BookFilter.Tag;
			endLevInfo[2].ihvo = iBook;
			endLevInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
			endLevInfo[1].ihvo = iFootnote;
			endLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			endLevInfo[0].ihvo = 0;
			helper.SetLevelInfo(SelectionHelper.SelLimitType.End, endLevInfo);
			helper.SetTextPropId(SelectionHelper.SelLimitType.End,
				(int)StTxtPara.StTxtParaTags.kflidContents);
			string footnoteText = ((StTxtPara)footnote.ParagraphsOS[0]).Contents.Text;
			helper.IchEnd = footnoteText.Length;

			helper.SetSelection(this, true, true);

			IVwSelection sel = RootBox.Selection;
			ITsString tss;
			sel.GetSelectionString(out tss, string.Empty);
			return tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to any footnote, given a book index and a footnote index.
		/// </summary>
		/// <param name="iBook">The 0-based index of the Scripture book containing the footnote
		/// to seek.</param>
		/// <param name="iFootnote">The 0-based index of the Footnote in which to put the
		/// insertion point.</param>
		/// ------------------------------------------------------------------------------------
		public void SetInsertionPoint(int iBook, int iFootnote)
		{
			CheckDisposed();

			//			base.SetInsertionPoint(iBook, iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the OnKeyDown method
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public new void OnKeyDown(KeyEventArgs e)
		{
			CheckDisposed();

			base.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the OnKeyPress method
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public new void OnKeyPress(KeyPressEventArgs e)
		{
			CheckDisposed();

			base.OnKeyPress(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for presence of proper paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoAnchor">[out] Start index of selection</param>
		/// <param name="ihvoEnd">[out] End index of selection</param>
		/// <returns>Return <c>false</c> if neither selection nor paragraph property. Otherwise
		/// return <c>true</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int tagText, out IVwPropertyStore[] vqvps, out int ihvoAnchor, out int ihvoEnd)
		{
			CheckDisposed();

			vwsel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoAnchor = 0;
			ihvoEnd = 0;

			return EditingHelper.IsParagraphProps(out vwsel, out hvoText, out tagText, out vqvps,
				out ihvoAnchor, out ihvoEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the view selection and paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoFirst">[out] Start index of selection</param>
		/// <param name="ihvoLast">[out] End index of selection</param>
		/// <param name="vqttp">[out] The style rules</param>
		/// <returns>Return false if there is neither a selection nor a paragraph property.
		/// Otherwise return true.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GetParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int tagText, out IVwPropertyStore[] vqvps, out int ihvoFirst, out int ihvoLast,
			out ITsTextProps[] vqttp)
		{
			CheckDisposed();

			vwsel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoFirst = 0;
			ihvoLast = 0;
			vqttp = null;
			return EditingHelper.GetParagraphProps(out vwsel, out hvoText, out tagText, out vqvps,
				out ihvoFirst, out ihvoLast, out vqttp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes OnDeleteFootnote to testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeleteFootnote()
		{
			CheckDisposed();

			base.OnDeleteFootnote(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes FindNearestFootnote to testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallFindNearestFootnote(ref int iBook, ref int iFootnote)
		{
			CheckDisposed();

			base.FindNearestFootnote(ref iBook, ref iFootnote);
		}
	}
	#endregion

	#region class DummyFootnoteViewForm
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy form for a <see cref="FootnoteView"/>, so that we can create a view
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFootnoteViewForm : Form, IFWDisposable, ISettings
	{
		private System.ComponentModel.IContainer components;
		private DummyFootnoteView m_footnoteView;
		private SIL.FieldWorks.Common.Controls.Persistence m_Persistence;
		private FdoCache m_cache;

		#region Constructor, Dispose, Generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyFootnoteViewForm"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyFootnoteViewForm()
		{
			InitializeComponent();
		}

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool fDisposing )
		{
			if (fDisposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
				if (m_footnoteView != null)
				{
					// Remove m_footnoteView and dispose it here, so the call to base.Dispose doesn't crash.
					// It tries to do the save settings in the Persistence object,
					// which just went belly up
					// in the above components.Dispose call.
					Controls.Remove(m_footnoteView);
					if (m_footnoteView != null)
						m_footnoteView.Dispose();
				}
				// No. since we don't own it.
				// "The Lord gaveth, and the Lord hath to taketh away."
				// Cache.Dispose();
			}
			m_footnoteView = null;
			Cache = null;

			base.Dispose( fDisposing );
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_Persistence = new SIL.FieldWorks.Common.Controls.Persistence(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_Persistence)).BeginInit();
			//
			// m_Persistence
			//
			this.m_Persistence.DefaultKeyPath = "Software\\SIL\\FieldWorks\\FootnoteViewTest";
			this.m_Persistence.Parent = this;
			//
			// DummyFootnoteViewForm
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Name = "DummyFootnoteViewForm";
			this.Text = "DummyFootnoteViewForm";
			((System.ComponentModel.ISupportInitialize)(this.m_Persistence)).EndInit();

		}
		#endregion
		#endregion

		#region ISettings methods
		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the parent's SettingsKey if parent implements ISettings, otherwise null.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public Microsoft.Win32.RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				return Registry.CurrentUser.CreateSubKey(m_Persistence.DefaultKeyPath);
			}
		}

		///***********************************************************************************
		/// <summary>
		/// Gets a window creation option.
		/// </summary>
		/// <value>By default, returns false</value>
		///***********************************************************************************
		[Browsable(false)]
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();

				return false;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Save the persisted settings now.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

			m_Persistence.SaveSettingsNow(this);
		}

		#endregion

		#region Properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets and Sets the Fdo cache
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
			set
			{
				CheckDisposed();
				m_cache = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyFootnoteView FootnoteView
		{
			get
			{
				CheckDisposed();
				return m_footnoteView;
			}
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the server and name to be used for establishing a database connection. First
		/// try to get this info based on the command-line arguments. If not given, try using
		/// the info in the registry. Otherwise, just get the first FW database on the local
		/// server.
		/// </summary>
		/// <returns>A new FdoCache, based on options, or null, if not found.</returns>
		/// <remarks>This method was originally taken from FwApp.cs</remarks>
		/// -----------------------------------------------------------------------------------
		private FdoCache GetCache()
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("c", MiscUtils.LocalServerName);
			cacheOptions.Add("db", "TestLangProj");
			FdoCache cache = null;
			cache = FdoCache.Create(cacheOptions);
			return cache;	// After all of this, it may be still be null, so watch out.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateFootnoteView()
		{
			CheckDisposed();

			CreateFootnoteView(GetCache());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and opens a <see cref="DummyFootnoteView"/> in a form. Loads scripture
		/// footnotes from the DB.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void CreateFootnoteView(FdoCache cache)
		{
			CheckDisposed();

			Cache = cache;
			FwStyleSheet styleSheet = new FwStyleSheet();

			ILangProject lgproj = Cache.LangProject;
			IScripture scripture = lgproj.TranslatedScriptureOA;
			styleSheet.Init(Cache, scripture.Hvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			m_footnoteView = new DummyFootnoteView(Cache);
			m_footnoteView.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			m_footnoteView.Dock = DockStyle.Fill;
			m_footnoteView.Name = "footnoteView";
			// make sure book filter is created before view constructor is created.
			int nbook = m_footnoteView.BookFilter.BookCount;
			m_footnoteView.MakeRoot();
			m_footnoteView.Visible = true;
			m_footnoteView.StyleSheet = styleSheet;
			Controls.Add(m_footnoteView);
			m_footnoteView.ActivateView();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Delete the registry subkey to allow for clean test
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void DeleteRegistryKey()
		{
			CheckDisposed();

			try
			{
				RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\SIL\\FieldWorks");
				key.DeleteSubKeyTree("FootnoteViewTest");
			}
			catch(Exception)
			{
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the FootnoteView to the end.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void ScrollToEnd()
		{
			CheckDisposed();

			m_footnoteView.GoToEnd();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get visibility of IP
		/// </summary>
		/// <returns>Returns <c>true</c> if selection is visible</returns>
		/// -----------------------------------------------------------------------------------
		public bool IsSelectionVisible()
		{
			CheckDisposed();

			return m_footnoteView.IsSelectionVisible(null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the Y positon off the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public int YPosition
		{
			get
			{
				CheckDisposed();
				return m_footnoteView.AutoScrollPosition.Y;
			}
		}
	}
	#endregion
}
