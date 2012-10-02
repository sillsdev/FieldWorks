// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BasicEntryInfoDlg.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implementation of:
//		InsertEntryDlg - Dialog for adding basic information of new entries.
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
using System.Xml;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for InsertEntryDlg.
	/// </summary>
	public class InsertEntryDlg : Form, IFWDisposable
	{
		public enum MorphTypeFilterType
		{
			prefix,
			suffix,
			any
		}

		#region Data members

		private FdoCache m_cache;
		private IPersistenceProvider m_persistProvider;
		private Mediator m_mediator;
		private int m_entryID;
		private int m_hvoNewSense = 0;		// set if we actually create a new entry.
		private IMoMorphType m_morphType;
		private ILexEntryType m_complexType = null;
		private bool m_fComplexForm = false;
		private int m_morphClsid;
		private MoMorphTypeCollection m_types;
		private bool m_fNewlyCreated = false;
		private string m_oldForm = "";
		private bool m_skipCheck = false;
		private ListBox.ObjectCollection m_MGAGlossListBoxItems;

		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label2;
		private SIL.FieldWorks.Common.Widgets.FwTextBox tbLexicalForm;	// text box used if one vernacular ws
		private SIL.FieldWorks.Common.Widgets.FwTextBox tbGloss; // text box used if one analysis ws
		private SIL.FieldWorks.Common.Widgets.LabeledMultiStringControl msLexicalForm; // multistring text box used for multiple vernacular ws
		private SIL.FieldWorks.Common.Widgets.LabeledMultiStringControl msGloss; // multistring text box used for multiple analysis ws.
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.Label label1;
		private FwOverrideComboBox cbMorphType;
		private FwOverrideComboBox cbComplexFormType;
		private System.Windows.Forms.GroupBox groupBox2;
		private MatchingEntries matchingEntries;
		private System.Windows.Forms.ToolTip m_toolTipSlotCombo;
		private MSAGroupBox m_msaGroupBox;
		private System.ComponentModel.IContainer components;

		private string s_helpTopic = "khtpInsertEntry";
		private LinkLabel linkSimilarEntry;
		private ImageList imageList1;
		private Label labelArrow;
		private System.Windows.Forms.HelpProvider helpProvider;
		protected SIL.FieldWorks.Resources.SearchingAnimation m_searchAnimtation;
		/// <summary>
		/// Remember how much we adjusted the height for the lexical form and gloss
		/// text boxes.
		/// </summary>
		private int m_delta = 0;
		private GroupBox groupBox1;
		private Label label3;

		// These are used to identify the <Not Complex> and <Unknown Complex Form>
		// entries in the combobox list.
		int m_idxNotComplex;
		int m_idxUnknownComplex;
		private GroupBox groupBox3;
		private LinkLabel m_lnkAssistant;

		// These are used for maintaining focus in either tbLexicalForm box or tbGloss.
		private bool m_fLexicalFormChanged = false;
		private bool m_fLexicalFormInitialFocus = true;
		#endregion // Data members


		/// <summary>
		/// This class allows a dummy LexEntryType replacement for "&lt;Unknown&gt;".
		/// </summary>
		internal class DummyEntryType
		{
			private string m_sName;
			bool m_fIsComplexForm;

			internal DummyEntryType(string sName, bool fIsComplexForm)
			{
				m_sName = sName;
				m_fIsComplexForm = fIsComplexForm;
			}

			public bool IsComplexForm
			{
				get { return m_fIsComplexForm; }
			}

			public override string ToString()
			{
				return m_sName;
			}
		}

		#region Properties

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Registry key for settings for this Dialog.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\LingCmnDlgs");
			}
		}

		private string Form
		{
			get
			{
				string sForm = null;
				if (msLexicalForm == null)
				{
					sForm = tbLexicalForm.Text;
				}
				else
				{
					ITsString tss = msLexicalForm.Value(m_cache.DefaultVernWs);
					if (tss != null)
						sForm = tss.Text;
				}
				return TrimOrGetEmptyString(sForm);
			}
			set
			{
				if (msLexicalForm == null)
					tbLexicalForm.Text = value.Trim();
				else
					msLexicalForm.SetValue(m_cache.DefaultVernWs, value.Trim());
			}
		}

		private ITsString TssForm
		{
			// REVIEW: trim?
			get
			{
				CheckDisposed();
				if (msLexicalForm == null)
					return tbLexicalForm.Tss;
				else
					return msLexicalForm.Value(m_cache.DefaultVernWs);
			}
			set
			{
				CheckDisposed();
				if (msLexicalForm == null)
				{
					tbLexicalForm.Tss = value;
				}
				else
				{
					int wsForm;
					bool fVern = IsFormWsInCurrentVernWs(m_cache, value, out wsForm);
					msLexicalForm.SetValue(fVern ? wsForm : m_cache.DefaultVernWs, value);
				}
			}
		}

		private string BestForm
		{
			get
			{
				if (msLexicalForm == null)
					return Form;
				for (int i = 0; i < msLexicalForm.NumberOfWritingSystems; ++i)
				{
					int ws;
					ITsString tss = msLexicalForm.ValueAndWs(i, out ws);
					if (tss != null && tss.Length > 0)
						return tss.Text.Trim();
				}
				return String.Empty;
			}
			set
			{
				if (msLexicalForm == null)
				{
					Form = value;
				}
				else
				{
					for (int i = 0; i < msLexicalForm.NumberOfWritingSystems; ++i)
					{
						int ws;
						ITsString tss = msLexicalForm.ValueAndWs(i, out ws);
						if (tss != null && tss.Length > 0)
						{
							msLexicalForm.SetValue(ws, value);
							return;
						}
					}
					Form = value;
				}
			}
		}

		private ITsString BestTssForm
		{
			get
			{
				if (msLexicalForm == null)
					return TssForm;
				for (int i = 0; i < msLexicalForm.NumberOfWritingSystems; ++i)
				{
					int ws;
					ITsString tss = msLexicalForm.ValueAndWs(i, out ws);
					if (tss != null && tss.Length > 0)
						return tss;
				}
				return null;
			}
		}

		private ITsString SelectedOrBestGlossTss
		{
			get
			{
				ITsString tssBestGloss = null;
				if (msGloss == null)
				{
					tssBestGloss = tbGloss.Tss;
				}
				else
				{
					int wsGloss = LangProject.kwsFirstAnal;
					// if there is a selection in the MultiStringControl
					// use the anchor ws from that selection.
					TextSelInfo tsi = new TextSelInfo(msGloss.RootBox);
					if (tsi.Selection != null)
						wsGloss = tsi.WsAltAnchor;
					tssBestGloss = msGloss.Value(wsGloss);
				}
				if (tssBestGloss != null && tssBestGloss.Length > 0)
					return tssBestGloss;
				return null;
			}
		}

		private string Gloss
		{
			get
			{
				string glossAnalysis = null;
				if (msGloss == null)
				{
					glossAnalysis = tbGloss.Text;
				}
				else
				{
					ITsString tssAnal = msGloss.Value(m_cache.DefaultAnalWs);
					if (tssAnal != null)
						glossAnalysis = tssAnal.Text;
				}
				return TrimOrGetEmptyString(glossAnalysis);
			}
			set
			{
				if (msGloss == null)
					tbGloss.Text = value.Trim();
				else
					msGloss.SetValue(m_cache.DefaultAnalWs, value.Trim());
			}
		}

		private string TrimOrGetEmptyString(string s)
		{
			if (String.IsNullOrEmpty(s))
				return String.Empty;
			else
				return s.Trim();
		}

		public ITsString TssGloss
		{
			// REVIEW: trim?
			get
			{
				CheckDisposed();
				if (msGloss == null)
					return tbGloss.Tss;
				else
					return msGloss.Value(m_cache.DefaultAnalWs);
			}
			set
			{
				CheckDisposed();
				if (msGloss == null)
				{
					tbGloss.Tss = value;
				}
				else
				{
					int wsGloss = StringUtils.GetWsAtOffset(value, 0);
					bool fAnal = false;
					foreach (int hvoWs in m_cache.LangProject.CurAnalysisWssRS.HvoArray)
					{
						if (hvoWs == wsGloss)
						{
							fAnal = true;
							break;
						}
					}
					msGloss.SetValue(fAnal ? wsGloss : m_cache.DefaultAnalWs, value);
				}
			}
		}

		/// <summary>
		/// Used to initialize other WSs of the gloss line during startup.
		/// Only works for WSs that are displayed (current analysis WSs).
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="tss"></param>
		public void SetInitialGloss(int ws, ITsString tss)
		{
			if (msGloss == null)
			{
				if (ws == m_cache.DefaultAnalWs)
					tbGloss.Tss = tss;
			}
			else
			{
				msGloss.SetValue(ws, tss);
			}
		}

		public int Pos
		{
			set
			{
				CheckDisposed();
				m_msaGroupBox.StemPOSHvo = value;
			}
		}

		public MsaType MsaType
		{
			get
			{
				CheckDisposed();

				if (m_msaGroupBox != null)
					return m_msaGroupBox.MSAType;
				else
					return MsaType.kStem;
			}
			set
			{
				CheckDisposed();

				if (m_msaGroupBox != null)
					m_msaGroupBox.MSAType = value;
			}
		}
		public IMoInflAffixSlot Slot
		{
			get
			{
				CheckDisposed();

				if (m_msaGroupBox != null)
					return m_msaGroupBox.Slot;
				else
					return null;
			}
			set
			{
				CheckDisposed();

				if (m_msaGroupBox != null)
					m_msaGroupBox.Slot = value;
			}
		}

		#endregion // Properties

		#region Construction and Initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		public InsertEntryDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Figure out where to locate the dlg.
			object obj = SettingsKey.GetValue("InsertX");
			if (obj != null)
			{
				int x = (int)obj;
				int y = (int)SettingsKey.GetValue("InsertY");
				int width = (int)SettingsKey.GetValue("InsertWidth", Width);
				int height = (int)SettingsKey.GetValue("InsertHeight", Height);
				Rectangle rect = new Rectangle(x, y, width, height);
				ScreenUtils.EnsureVisibleRect(ref rect);
				DesktopBounds = rect;
				StartPosition = FormStartPosition.Manual;
			}

			helpProvider = new System.Windows.Forms.HelpProvider();
			if (FwApp.App != null)
			{
				helpProvider.HelpNamespace = FwApp.App.HelpFile;
				helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
			}
			else
			{
				btnHelp.Enabled = false;
			}
			helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);

			m_searchAnimtation = new SIL.FieldWorks.Resources.SearchingAnimation();
			// We will add it to controls when we want to show it.
			matchingEntries.SearchingChanged += new EventHandler(matchingEntries_SearchingChanged);

			AdjustWidthForLinkLabelGroupBox();
		}

		/// <summary>
		/// Adjust the width of the group box containing the LinkLabel to allow longer
		/// translated labels to be visible (if possible).
		/// </summary>
		private void AdjustWidthForLinkLabelGroupBox()
		{
			int maxWidth = groupBox2.Width;
			int needWidth = m_lnkAssistant.Location.X + m_lnkAssistant.Width + 2;
			if (needWidth > groupBox3.Width || groupBox3.Width > maxWidth)
				groupBox3.Width = Math.Min(needWidth, maxWidth);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			AdjustWidthForLinkLabelGroupBox();
		}

		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for this dialog because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			Size size = this.Size;
			base.OnLoad (e);
			if (this.Size != size)
				this.Size = size;
		}


		bool m_fInitialized = false;
		/// <summary>
		/// This shouldn't be needed, but without it, the dialog can start up with the focus
		/// in the lexical form text box, but the keyboard set to the analysis writing system
		/// instead of the vernacular writing system.  See LT-4719.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			if (!m_fInitialized)
			{
				if (m_fLexicalFormInitialFocus)
				{
					if (msLexicalForm == null)
						tbLexicalForm.Focus();
					else
						msLexicalForm.Focus();
					// Set the m_fLexicalFormChanged flag, so that if an UpdateMatches
					// is currently running, it will shift the focus to the lexical form.
					m_fLexicalFormChanged = true;
				}
				else
				{
					if (msGloss == null)
						tbGloss.Focus();
					else
						msGloss.Focus();
					// Set the m_fLexicalFormChanged flag, so that if an UpdateMatches
					// is currently running, it will shift the focus to the gloss.
					m_fLexicalFormChanged = false;
				}
				m_fInitialized = true;
			}
		}

		void matchingEntries_SearchingChanged(object sender, EventArgs e)
		{
			this.Searching = matchingEntries.Searching;
			CheckIfGoto();
		}

		/// <summary>
		/// Initialize the dialog.
		/// </summary>
		/// <param name="cache">The FDO cache to use.</param>
		/// <param name="morphType">The morpheme type</param>
		/// <remarks>All other variations of SetDlgInfo should eventually call this one.</remarks>
		protected void SetDlgInfo(FdoCache cache, IMoMorphType morphType)
		{
			SetDlgInfo(cache, morphType, 0, MorphTypeFilterType.any);
		}

		protected void SetDlgInfo(FdoCache cache, IMoMorphType morphType, int wsVern, MorphTypeFilterType filter)
		{
			ReplaceMatchingEntriesControl();
			IVwStylesheet stylesheet = null;
			if (m_mediator != null)
			{
				stylesheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
				if (matchingEntries != null)
					matchingEntries.Initialize(cache, stylesheet, m_mediator);
			}
			m_cache = cache;

			m_fNewlyCreated = false;
			m_oldForm = "";

			if (m_types == null)
				m_types = new MoMorphTypeCollection(cache);

			// Set fonts for the two edit boxes.
			if (stylesheet != null)
			{
				tbLexicalForm.StyleSheet = stylesheet;
				tbGloss.StyleSheet = stylesheet;
			}

			// Set writing system factory and code for the two edit boxes.
			tbLexicalForm.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			if (wsVern <= 0)
				wsVern = cache.LangProject.DefaultVernacularWritingSystem;
			tbLexicalForm.WritingSystemCode = wsVern;
			tbLexicalForm.AdjustStringHeight = false;

			tbGloss.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			tbGloss.WritingSystemCode = cache.LangProject.DefaultAnalysisWritingSystem;
			tbGloss.AdjustStringHeight = false;

			// initialize to empty TsStrings
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			//we need to use the weVern so that tbLexicalForm is sized correctly for the font size.
			//In Interlinear text the baseline can be in any of the vernacular writing systems, not just
			//the defaultVernacularWritingSystem.
			TssForm = tsf.MakeString("", wsVern);
			TssGloss = tsf.MakeString("", cache.LangProject.DefaultAnalysisWritingSystem);
			((System.ComponentModel.ISupportInitialize)(this.tbLexicalForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.tbGloss)).EndInit();


			int cVern = LabeledMultiStringView.GetWritingSystemList(m_cache, LangProject.kwsVerns, false).Length;
			if (cVern > 1)
			{
				msLexicalForm = ReplaceTextBoxWithMultiStringBox(tbLexicalForm, LangProject.kwsVerns, stylesheet);
				msLexicalForm.TextChanged += new EventHandler(tbLexicalForm_TextChanged);
			}
			else
			{
				// See if we need to adjust the height of the lexical form
				AdjustTextBoxAndDialogHeight(tbLexicalForm);
			}

			// JohnT addition: if multiple analysis writing systems, replace tbGloss with msGloss
			int cWritingSystem = LabeledMultiStringView.GetWritingSystemList(m_cache, LangProject.kwsAnals, false).Length;
			if (cWritingSystem > 1)
			{
				msGloss = ReplaceTextBoxWithMultiStringBox(tbGloss, LangProject.kwsAnals, stylesheet);
				m_lnkAssistant.Top = msGloss.Bottom - m_lnkAssistant.Height;
				msGloss.TextChanged += new System.EventHandler(this.tbGloss_TextChanged);
			}
			else
			{
				// See if we need to adjust the height of the gloss
				AdjustTextBoxAndDialogHeight(tbGloss);
			}

			m_msaGroupBox.Initialize(cache, m_mediator, m_lnkAssistant, this);
			// See if we need to adjust the height of the MSA group box.
			int oldHeight = m_msaGroupBox.Height;
			int newHeight = Math.Max(m_msaGroupBox.PreferredHeight, oldHeight);
			GrowDialogAndAdjustControls(newHeight - oldHeight, m_msaGroupBox);
			m_msaGroupBox.AdjustInternalControlsAndGrow();

			Text = GetTitle();
			m_lnkAssistant.Enabled = false;

			// Set font for the combobox.
			cbMorphType.Font =
				new Font(cache.LangProject.DefaultAnalysisWritingSystemFont, 10);

			// Populate morph type combo.
			// first Fill ComplexFormType combo, since cbMorphType controls
			// whether it gets enabled and which index is selected.
			cbComplexFormType.Font =
				new Font(cache.LangProject.DefaultAnalysisWritingSystemFont, 10);
			List<ICmPossibility> rgComplexTypes = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities.ToArray());
			rgComplexTypes.Sort();
			m_idxNotComplex = cbComplexFormType.Items.Count;
			cbComplexFormType.Items.Add(new DummyEntryType(LexTextControls.ksNotApplicable, false));
			m_idxUnknownComplex = cbComplexFormType.Items.Count;
			cbComplexFormType.Items.Add(new DummyEntryType(LexTextControls.ksUnknownComplexForm, true));
			for (int i = 0; i < rgComplexTypes.Count; ++i)
			{
				ILexEntryType type = (ILexEntryType)rgComplexTypes[i];
				cbComplexFormType.Items.Add(type);
			}
			cbComplexFormType.SelectedIndex = 0;
			cbComplexFormType.Visible = true;
			cbComplexFormType.Enabled = true;
			// Convert from Set to List, since the Set can't sort.

			List<ICmPossibility> al = new List<ICmPossibility>();
			foreach (ICmPossibility mType in m_cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities)
			{
				switch (filter)
				{
					case MorphTypeFilterType.prefix:
						if (MoMorphType.IsPrefixishType(m_cache, mType.Hvo))
							al.Add(mType);
						break;

					case MorphTypeFilterType.suffix:
						if (MoMorphType.IsSuffixishType(m_cache, mType.Hvo))
							al.Add(mType);
						break;

					case MorphTypeFilterType.any:
						al.Add(mType);
						break;
				}
			}
			al.Sort();
			for (int i = 0; i < al.Count; ++i)
			{
				IMoMorphType type = (IMoMorphType)al[i];

				cbMorphType.Items.Add(type);
				//previously had "if (type == morphType)" which was always false
				if (type.Equals(morphType))
					cbMorphType.SelectedIndex = i;
			}

			m_morphType = morphType; // Is this still needed?
			m_msaGroupBox.MorphTypePreference = m_morphType;
			// Now position the searching animation
			/*
			 * This position put the animation over the Glossing Assistant button. LT-9146
			m_searchAnimtation.Top = groupBox2.Top - m_searchAnimtation.Height - 5;
			m_searchAnimtation.Left = groupBox2.Right - m_searchAnimtation.Width - 10;
			 */
			/* This position puts the animation over the top left corner, but will that
			 * look okay with right-to-left?
			m_searchAnimtation.Top = groupBox2.Top + 40;
			m_searchAnimtation.Left = groupBox2.Left + 10;
			 */
			// This position puts the animation close to the middle of the list.
			m_searchAnimtation.Top = groupBox2.Top + (groupBox2.Top / 2);
			m_searchAnimtation.Left = groupBox2.Left + (groupBox2.Right / 2);
		}

		private LabeledMultiStringControl ReplaceTextBoxWithMultiStringBox(FwTextBox tb, int wsType,
			IVwStylesheet stylesheet)
		{
			tb.Hide();
			LabeledMultiStringControl ms = new LabeledMultiStringControl(m_cache, wsType, stylesheet);
			ms.Location = tb.Location;
			ms.Width = tb.Width;
			ms.Anchor = tb.Anchor;

			int oldHeight = tb.Parent.Height;
			FontHeightAdjuster.GrowDialogAndAdjustControls(tb.Parent, ms.Height - tb.Height, ms);
			tb.Parent.Controls.Add(ms);

			// Grow the dialog and move all lower controls down to make room.
			GrowDialogAndAdjustControls(tb.Parent.Height - oldHeight, tb.Parent);
			ms.TabIndex = tb.TabIndex;	// assume the same tab order as the single ws control
			return ms;
		}

		private void AdjustTextBoxAndDialogHeight(FwTextBox tb)
		{
			int oldHeight = tb.Parent.Height;
			int tbNewHeight = Math.Max(tb.PreferredHeight, tb.Height);
			FontHeightAdjuster.GrowDialogAndAdjustControls(tb.Parent, tbNewHeight - tb.Height, tb);
			tb.Height = tbNewHeight;

			GrowDialogAndAdjustControls(tb.Parent.Height - oldHeight, tb.Parent);
		}

		protected virtual void ReplaceMatchingEntriesControl()
		{
			XmlNode xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			XmlNode xnControl = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingEntries\"]");
			if (xnControl != null)
			{
				// Replace the current matchingEntries object with the one specified in the XML.
				MatchingEntries newME = DynamicLoader.CreateObject(xnControl) as MatchingEntries;
				if (newME != null)
				{
					ReplaceMatchingEntriesControl(newME);
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="newME">the new control to use. if null, just remove the existing control.</param>
		protected void ReplaceMatchingEntriesControl(MatchingEntries newME)
		{
			if (newME != null)
			{
				newME.Location = matchingEntries.Location;
				newME.Size = matchingEntries.Size;
				newME.Name = matchingEntries.Name;
				newME.AccessibleName = matchingEntries.AccessibleName;
				newME.TabStop = matchingEntries.TabStop;
				newME.TabIndex = matchingEntries.TabIndex;
				newME.Anchor = matchingEntries.Anchor;
			}
			this.groupBox2.Controls.Remove(matchingEntries);
			matchingEntries.SelectionChanged -= new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
			bool fAddSearchingChanged = false;
			if (matchingEntries.HasSearchingChanged)
			{
				fAddSearchingChanged = true;
				matchingEntries.SearchingChanged -= new EventHandler(matchingEntries_SearchingChanged);
			}
			matchingEntries.RestoreFocus -= new EventHandler(matchingEntries_RestoreFocus);
			matchingEntries.Dispose();
			matchingEntries = newME;
			if (matchingEntries != null)
			{
				this.groupBox2.Controls.Add(matchingEntries);
				matchingEntries.SelectionChanged += new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
				if (fAddSearchingChanged)
					matchingEntries.SearchingChanged += new EventHandler(matchingEntries_SearchingChanged);
				matchingEntries.RestoreFocus += new EventHandler(matchingEntries_RestoreFocus);
			}
		}

		// Grow the dialog's height by delta.
		// Adjust any controls that need it.
		private void GrowDialogAndAdjustControls(int delta, Control grower)
		{
			if (delta == 0)
				return;
			m_delta += delta;
			FontHeightAdjuster.GrowDialogAndAdjustControls(this, delta, grower);
		}

		/// <summary>
		/// Initialize an InsertEntryDlg from something like an "Insert Major Entry menu".
		/// </summary>
		/// <param name="cache">The FDO cache to use.</param>
		/// <param name="tssForm">The initial form to use.</param>
		/// <param name="mediator">The XCore.Mediator to use.</param>
		public void SetDlgInfo(FdoCache cache, ITsString tssForm, Mediator mediator)
		{
			CheckDisposed();

			m_mediator = mediator;
			if (m_types == null)
				m_types = new MoMorphTypeCollection(cache);
			string form = tssForm.Text;
			int clsidForm;
			IMoMorphType mmt;

			// Check whether the incoming form is vernacular or analysis.
			// (See LT-4074 and LT-7240.)
			int wsForm;
			bool fVern = IsFormWsInCurrentVernWs(cache, tssForm, out wsForm);
			// If form is empty (cf. LT-1621), use stem
			if (tssForm.Length == 0 || !fVern)
				mmt = m_types.Item(MoMorphType.kmtStem);
			else
				mmt = MoMorphType.FindMorphType(cache, m_types, ref form, out clsidForm);
			int wsVern = fVern ? wsForm : cache.DefaultVernWs;
			SetDlgInfo(cache, mmt, wsVern, MorphTypeFilterType.any);
			if (fVern)
			{
				TssForm = tssForm;
				TssGloss = cache.MakeAnalysisTss("");
				// The lexical form is already set, so shift focus to the gloss when
				// the form is activated.
				m_fLexicalFormInitialFocus = false;
			}
			else
			{
				TssForm = cache.MakeVernTss("");
				TssGloss = tssForm;
				// The gloss is already set, so shift the focus to the lexical form
				// when the form is activated.
				m_fLexicalFormInitialFocus = m_fLexicalFormChanged = true;
			}

			UpdateMatches();
		}

		/// <summary>
		/// look through current vern wss and see if the the ws for tssForm is in that list.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssForm"></param>
		/// <param name="wsForm">ws at offset 0 of tssForm</param>
		/// <returns></returns>
		private static bool IsFormWsInCurrentVernWs(FdoCache cache, ITsString tssForm, out int wsForm)
		{
			bool fVern = false;
			wsForm = StringUtils.GetWsAtOffset(tssForm, 0);
			foreach (int hvoWs in cache.LangProject.CurVernWssRS.HvoArray)
			{
				if (wsForm == hvoWs)
				{
					fVern = true;
					break;
				}
			}
			return fVern;
		}

		/// <summary>
		/// Initialize an InsertEntryDlg from something like an "Insert Major Entry menu".
		/// </summary>
		/// <param name="cache">The FDO cache to use.</param>
		/// <param name="mediator">The XCore.Mediator to use.</param>
		/// <param name="persistProvider">The persistence provider to use.</param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, IPersistenceProvider persistProvider)
		{
			CheckDisposed();

			Debug.Assert(mediator != null);
			Debug.Assert(persistProvider != null);
			m_persistProvider = persistProvider;
			m_mediator = mediator;

			SetDlgInfo(cache);
		}

		/// <summary>
		/// Initialize the dialog.
		/// </summary>
		/// <param name="cache">The FDO cache to use.</param>
		/// <param name="morphType">The morpheme type</param>
		/// <param name="msaType">The type of msa</param>
		/// <param name="slot">The default slot of the inflectional affix msa to</param>
		public void SetDlgInfo(FdoCache cache, IMoMorphType morphType,
			MsaType msaType, IMoInflAffixSlot slot, Mediator mediator, MorphTypeFilterType filter)
		{
			CheckDisposed();

			Debug.Assert(mediator != null);
			m_mediator = mediator;

			SetDlgInfo(cache, morphType, 0, filter);
			m_msaGroupBox.MSAType = msaType;
			Slot = slot;
		}

		/// <summary>
		/// Disable these two controls (for use when creating an entry for a particular slot)
		/// </summary>
		public void DisableAffixTypeMainPosAndSlot()
		{
			CheckDisposed();

			m_msaGroupBox.DisableAffixTypeMainPosAndSlot();
		}

		/// <summary>
		/// Initialize an InsertEntryDlg from something like an "Insert Major Entry menu".
		/// </summary>
		/// <param name="cache">The FDO cache to use.</param>
		protected void SetDlgInfo(FdoCache cache)
		{
			if (m_types == null)
				m_types = new MoMorphTypeCollection(cache);
			SetDlgInfo(cache, m_types.Item(MoMorphType.kmtStem));
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_cache = null;

			base.Dispose(disposing);
		}

		#endregion Construction and Initialization

		#region Other methods

		/// <summary>
		/// Get the results from the dlg.
		/// </summary>
		/// <param name="entryID">entry ID for entry.</param>
		/// <param name="newlyCreated">true if entry was just created, otherwise false.</param>
		public void GetDialogInfo(out int entryID, out bool newlyCreated)
		{
			CheckDisposed();

			entryID = m_entryID;
			newlyCreated = m_fNewlyCreated;
		}

		/// <summary>
		/// Return the id of the sense created for the new entry.  This is valid only if
		/// m_fNewlyCreated is true.
		/// </summary>
		public int NewSenseId
		{
			get
			{
				CheckDisposed();
				return m_hvoNewSense;
			}
		}

		private string GetTitle()
		{
			string sTitle;
			if (m_mediator == null)
			{
				sTitle = LexText.Controls.LexTextControls.ksNewEntry;
			}
			else
				sTitle = m_mediator.StringTbl.GetStringWithXPath("CreateEntry", "/group[@id=\"DialogTitles\"]/");
			return sTitle;
		}

		protected virtual void UpdateMatches()
		{
			List<ExtantEntryInfo> filters = new List<ExtantEntryInfo>();
			string form = MoForm.EnsureNoMarkers(Form, m_cache);
			ITsString tssGloss = SelectedOrBestGlossTss;
			string gloss = null;
			int wsGloss = m_cache.DefaultAnalWs;
			if (tssGloss != null)
			{
				gloss = TrimOrGetEmptyString(tssGloss.Text);
				if (gloss.Length > 0)
					wsGloss = StringUtils.GetWsAtOffset(tssGloss, 0);
			}
			matchingEntries.ResetSearch(m_cache, m_entryID,
				false,
				StringUtils.GetWsAtOffset(TssForm, 0),
				form, // Citation form
				form, // underlying form
				form, // allomorph form
				wsGloss, // analysis ws
				gloss, // gloss
				filters);
		}

		/// <summary>
		/// Set the class and morph type.
		/// </summary>
		/// <param name="mmt"></param>
		/// <param name="clsid"></param>
		private void SetMorphType(IMoMorphType mmt, int clsid)
		{
			if (!cbMorphType.Items.Contains(mmt))
				return;

			m_morphClsid = clsid;
			m_morphType = mmt;
			m_msaGroupBox.MorphTypePreference = mmt;
			m_skipCheck = true;
			cbMorphType.SelectedItem = mmt;
			m_skipCheck = false;
			EnableComplexFormTypeCombo();
		}

		private void UseExistingEntry()
		{
			DialogResult = DialogResult.Yes;
			Close();
		}

		/// <summary>
		/// Changes the text of "Use Similar Entry" link to indicate that in this context it will lead
		/// to adding an allomorph to the similar entry (unless it already has an appropriate one, of course).
		/// </summary>
		public void ChangeUseSimilarToCreateAllomorph()
		{
			linkSimilarEntry.Text = LexTextControls.ksAddAllomorphToSimilarEntry;
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			CheckDisposed();

			s_helpTopic = helpTopic;
			if (FwApp.App != null)
			{
				helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
				btnHelp.Enabled = true;
			}
		}

		#endregion Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InsertEntryDlg));
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.tbLexicalForm = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.tbGloss = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.btnHelp = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.cbMorphType = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.cbComplexFormType = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.labelArrow = new System.Windows.Forms.Label();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.linkSimilarEntry = new System.Windows.Forms.LinkLabel();
			this.matchingEntries = new SIL.FieldWorks.LexText.Controls.MatchingEntries();
			this.m_toolTipSlotCombo = new System.Windows.Forms.ToolTip(this.components);
			this.m_msaGroupBox = new SIL.FieldWorks.LexText.Controls.MSAGroupBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.m_lnkAssistant = new System.Windows.Forms.LinkLabel();
			((System.ComponentModel.ISupportInitialize)(this.tbLexicalForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.tbGloss)).BeginInit();
			this.groupBox2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// tbLexicalForm
			//
			this.tbLexicalForm.AdjustStringHeight = true;
			this.tbLexicalForm.AllowMultipleLines = false;
			this.tbLexicalForm.BackColor = System.Drawing.SystemColors.Window;
			this.tbLexicalForm.controlID = null;
			resources.ApplyResources(this.tbLexicalForm, "tbLexicalForm");
			this.tbLexicalForm.HasBorder = true;
			this.tbLexicalForm.Name = "tbLexicalForm";
			this.tbLexicalForm.SelectionLength = 0;
			this.tbLexicalForm.SelectionStart = 0;
			this.tbLexicalForm.TabStop = true;
			this.tbLexicalForm.TextChanged += new System.EventHandler(this.tbLexicalForm_TextChanged);
			//
			// tbGloss
			//
			this.tbGloss.AdjustStringHeight = true;
			this.tbGloss.AllowMultipleLines = false;
			this.tbGloss.BackColor = System.Drawing.SystemColors.Window;
			this.tbGloss.controlID = null;
			resources.ApplyResources(this.tbGloss, "tbGloss");
			this.tbGloss.HasBorder = true;
			this.tbGloss.Name = "tbGloss";
			this.tbGloss.SelectionLength = 0;
			this.tbGloss.SelectionStart = 0;
			this.tbGloss.TabStop = true;
			this.tbGloss.TextChanged += new System.EventHandler(this.tbGloss_TextChanged);
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// cbMorphType
			//
			this.cbMorphType.AllowSpaceInEditBox = false;
			this.cbMorphType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbMorphType, "cbMorphType");
			this.cbMorphType.Name = "cbMorphType";
			this.cbMorphType.SelectedIndexChanged += new System.EventHandler(this.cbMorphType_SelectedIndexChanged);
			//
			// cbComplexFormType
			//
			this.cbComplexFormType.AllowSpaceInEditBox = false;
			this.cbComplexFormType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbComplexFormType, "cbComplexFormType");
			this.cbComplexFormType.Name = "cbComplexFormType";
			this.cbComplexFormType.SelectedIndexChanged += new System.EventHandler(this.cbComplexFormType_SelectedIndexChanged);
			//
			// groupBox2
			//
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Controls.Add(this.labelArrow);
			this.groupBox2.Controls.Add(this.linkSimilarEntry);
			this.groupBox2.Controls.Add(this.matchingEntries);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			//
			// labelArrow
			//
			resources.ApplyResources(this.labelArrow, "labelArrow");
			this.labelArrow.ImageList = this.imageList1;
			this.labelArrow.Name = "labelArrow";
			this.labelArrow.Click += new System.EventHandler(this.btnSimilarEntry_Click);
			//
			// imageList1
			//
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList1.Images.SetKeyName(0, "GoToArrow.bmp");
			//
			// linkSimilarEntry
			//
			resources.ApplyResources(this.linkSimilarEntry, "linkSimilarEntry");
			this.linkSimilarEntry.Name = "linkSimilarEntry";
			this.linkSimilarEntry.TabStop = true;
			this.linkSimilarEntry.Click += new System.EventHandler(this.btnSimilarEntry_Click);
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			this.matchingEntries.Name = "matchingEntries";
			this.matchingEntries.TabStop = false;
			this.matchingEntries.SelectionChanged += new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
			//
			// m_toolTipSlotCombo
			//
			this.m_toolTipSlotCombo.AutoPopDelay = 5000;
			this.m_toolTipSlotCombo.InitialDelay = 250;
			this.m_toolTipSlotCombo.ReshowDelay = 100;
			this.m_toolTipSlotCombo.ShowAlways = true;
			//
			// m_msaGroupBox
			//
			resources.ApplyResources(this.m_msaGroupBox, "m_msaGroupBox");
			this.m_msaGroupBox.MSAType = SIL.FieldWorks.FDO.MsaType.kNotSet;
			this.m_msaGroupBox.Name = "m_msaGroupBox";
			this.m_msaGroupBox.Slot = null;
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.cbMorphType);
			this.groupBox1.Controls.Add(this.cbComplexFormType);
			this.groupBox1.Controls.Add(this.tbLexicalForm);
			this.groupBox1.Controls.Add(this.label2);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// groupBox3
			//
			this.groupBox3.Controls.Add(this.m_lnkAssistant);
			this.groupBox3.Controls.Add(this.tbGloss);
			resources.ApplyResources(this.groupBox3, "groupBox3");
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.TabStop = false;
			//
			// m_lnkAssistant
			//
			resources.ApplyResources(this.m_lnkAssistant, "m_lnkAssistant");
			this.m_lnkAssistant.Name = "m_lnkAssistant";
			this.m_lnkAssistant.TabStop = true;
			this.m_lnkAssistant.VisitedLinkColor = System.Drawing.Color.Blue;
			this.m_lnkAssistant.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkAssistant_LinkClicked);
			//
			// InsertEntryDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.m_msaGroupBox);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "InsertEntryDlg";
			this.ShowInTaskbar = false;
			this.Load += new System.EventHandler(this.InsertEntryDlg_Load);
			this.Closed += new System.EventHandler(this.InsertEntryDlg_Closed);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.InsertEntryDlg_Closing);
			((System.ComponentModel.ISupportInitialize)(this.tbLexicalForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.tbGloss)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Event Handlers

		private void InsertEntryDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			switch (DialogResult)
			{
				default:
				{
					Debug.Assert(false, "Unexpected DialogResult.");
					break;
				}
				case DialogResult.Yes:
				{
					// Exiting via existing entry selection.
					DialogResult = DialogResult.OK;
					m_fNewlyCreated = false;
					m_entryID = matchingEntries.SelectedEntryID();
					break;
				}
				case DialogResult.Cancel:
				{
					break;
				}
				case DialogResult.OK:
				{
					// In the beginning, Larry specified the gloss to not be required.
					// Then, Andy changed it to be required.
					// As of LT-518, it is again not required.
					// I'll leave it in, but blocked, in case it changes again. :-)
					//&& tbGloss.Text.Length > 0
					// As of LT-832, categories are all optional.
					if (!LexFormNotEmpty())
					{
						e.Cancel = true;
						MessageBox.Show(this, LexText.Controls.LexTextControls.ksFillInLexForm,
							LexText.Controls.LexTextControls.ksMissingInformation,
							MessageBoxButtons.OK, MessageBoxIcon.Information);
						return;
					}
					if (!CheckMorphType())
					{
						e.Cancel = true;
						MessageBox.Show(this, LexText.Controls.LexTextControls.ksInvalidLexForm,
							LexText.Controls.LexTextControls.ksMissingInformation,
							MessageBoxButtons.OK, MessageBoxIcon.Information);
						return;
					}
					if (CircumfixProblem())
					{
						e.Cancel = true;
						MessageBox.Show(this, LexText.Controls.LexTextControls.ksCompleteCircumfix,
							LexText.Controls.LexTextControls.ksMissingInformation,
							MessageBoxButtons.OK, MessageBoxIcon.Information);
						return;
					}
					CreateNewEntry();
					break;
				}
			}
		}

		private bool CheckMorphType()
		{
			string form = BestForm;
			int clsid;
			IMoMorphType mmt = MoMorphType.FindMorphType(m_cache, m_types, ref form, out clsid);
			switch (m_morphType.Guid.ToString())
			{
				// these cases are not handled by FindMorphType
				case MoMorphType.kguidMorphCircumfix:
				case MoMorphType.kguidMorphPhrase:
				case MoMorphType.kguidMorphDiscontiguousPhrase:
				case MoMorphType.kguidMorphStem:
				case MoMorphType.kguidMorphRoot:
				case MoMorphType.kguidMorphParticle:
				case MoMorphType.kguidMorphClitic:
					return mmt.Guid.ToString() == MoMorphType.kguidMorphStem || mmt.Guid.ToString() == MoMorphType.kguidMorphPhrase;

				case MoMorphType.kguidMorphBoundRoot:
					return mmt.Guid.ToString() == MoMorphType.kguidMorphBoundStem;

				case MoMorphType.kguidMorphSuffixingInterfix:
					return mmt.Guid.ToString() == MoMorphType.kguidMorphSuffix;

				case MoMorphType.kguidMorphPrefixingInterfix:
					return mmt.Guid.ToString() == MoMorphType.kguidMorphPrefix;

				case MoMorphType.kguidMorphInfixingInterfix:
					return mmt.Guid.ToString() == MoMorphType.kguidMorphInfix;

				default:
					return mmt.Equals(m_morphType);
			}
		}

		/// <summary>
		/// Answer true if we are trying to create a circumfix and the data is not in a state that allows that.
		/// </summary>
		/// <returns></returns>
		private bool CircumfixProblem()
		{
			if (m_morphType.Guid.ToString() != MoMorphType.kguidMorphCircumfix)
				return false; // not a circumfix at all.
			if (msLexicalForm == null)
			{
				ITsString tss = TssForm;
				string left, right;
				if (!LexEntry.GetCircumfixLeftAndRightParts(m_cache, tss, out left, out right))
					return true;
			}
			else // multiple WSS to check.
			{
				// Check all other writing systems.
				for (int i = 0; i < msLexicalForm.NumberOfWritingSystems; i++)
				{
					int ws;
					ITsString tss = msLexicalForm.ValueAndWs(i, out ws);
					if (tss != null && tss.Text != null)
					{
						string left, right;
						if (!LexEntry.GetCircumfixLeftAndRightParts(m_cache, tss, out left, out right))
							return true;
					}
				}
			}
			return false;
		}

		private bool LexFormNotEmpty()
		{
			return BestForm.Length > 0;
		}

		/// <summary>
		/// Create a new entry based upon the state of the Dialog.
		/// </summary>
		public void CreateNewEntry()
		{
			bool okToClose = LexFormNotEmpty();
			if (!okToClose)
				throw new ArgumentException("lexical form field should not be empty.");
			Cursor = Cursors.WaitCursor;
			// We don't want the parser to see the new entry till we get done.
			// Note (from RandyR): This probably won't do what you think,
			// because it won't shut down the connection the parser has,
			// so there could still be timeouts or locks.
			// It probably would be a good idea if the parser didn't get excited about reloading,
			// until the entry is completely done being created here, however.
			ParserScheduler parser = null;
			if (ParserFactory.HasParser(m_cache.ServerName, m_cache.DatabaseName,
				m_cache.LangProject.Name.AnalysisDefaultWritingSystem))
			{
				// Getting the parser can fail with an internal error message something like
				//	Object '/8b9d17e1_bb1e_4fb3_b84a_1ac50b02c4ed/gm6vzmwmfhwbcnsyu085vinz_105.rem' has been disconnected or does not exist at the server
				// See LT-8704
				try
				{
					parser = ParserFactory.GetDefaultParser(m_cache.ServerName, m_cache.DatabaseName,
						m_cache.LangProject.Name.AnalysisDefaultWritingSystem);
					if (parser.IsPaused)
						parser = null; // nothing to do when closed
					else
						if (!parser.AttemptToPause())
							Debug.Fail("Could not pause parser.");
				}
				catch
				{
					parser = null;
					Debug.WriteLine("UpdateRealFromSandbox(): ParserFactory.GetDefaultParser() threw an error?!");
				}
			}

			ILexEntry entry = LexEntry.CreateEntry(m_cache, m_morphType, BestTssForm, Gloss,
				m_msaGroupBox.DummyMSA);
			m_entryID = entry.Hvo;
			m_fNewlyCreated = true;
			ILexSense sense = entry.SensesOS.FirstItem;
			m_hvoNewSense = sense.Hvo;
			if (msLexicalForm != null)
			{
				// Save the other writing systems.
				for (int i = 0; i < msLexicalForm.NumberOfWritingSystems; i++)
				{
					int ws;
					ITsString tss = msLexicalForm.ValueAndWs(i, out ws);
					if (tss != null && tss.Text != null)
						entry.SetLexemeFormAlt(ws, tss);
				}
			}
			if (msGloss != null)
			{
				// Save the other writing systems.
				for (int i = 0; i < msGloss.NumberOfWritingSystems; i++)
				{
					int ws;
					ITsString tss = msGloss.ValueAndWs(i, out ws);

					m_cache.MainCacheAccessor.SetMultiStringAlt(sense.Hvo, (int)LexSense.LexSenseTags.kflidGloss, ws, tss);
				}
			}

			HandleAnyFeatures(entry);

			if (m_fComplexForm)
			{
				ILexEntryRef ler = new LexEntryRef();
				entry.EntryRefsOS.Append(ler);
				if (m_complexType != null)
					ler.ComplexEntryTypesRS.Append(m_complexType);
				ler.RefType = 1;
				// This is not wanted anywhere (at the moment).  See LT-10289.
				//string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
				//if (toolName != "lexiconEdit")
				//{
				//    using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
				//    {
				//        dlg.SetDlgInfo(m_cache, m_mediator, entry);
				//        dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense");
				//        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				//        {
				//            ler.ComponentLexemesRS.Append(dlg.SelectedID);
				//            ler.PrimaryLexemesRS.Append(dlg.SelectedID);
				//        }
				//    }
				//}
			}
			// If we paused the parser, restart it now.
			if (parser != null)
				parser.Resume();

			// Not redrawing the list in the GUI and not finding later, so added the following to cause
			// the display to update and find the item.  (LT-3101,2983)
			// The following section was copied from InsertEntryDlgListener::OnDialogInsertItemInVector.
			// Begin copied code

			// Note: This call will cause the RecordClerk to reload,
			// since it won't know anything about the new entry
			int hvoLexDb = m_cache.LangProject.LexDbOAHvo;
			int tagEntries = (int)FDO.Ling.LexDb.LexDbTags.kflidEntries;
			int ihvo = 0;
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			int chvo = sda.get_VecSize(hvoLexDb, tagEntries);
			for (; ihvo < chvo && sda.get_VecItem(hvoLexDb, tagEntries, ihvo) != m_entryID; ihvo++)
				;
			Debug.Assert(ihvo < chvo); // We should find the new entry!
			sda.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				hvoLexDb,
				tagEntries,
				ihvo, // The position it is in the current version of the property.
				1, // inserted 1
				0); // deleted 0

			// End copied code

			Cursor = Cursors.Default;
		}
		private void HandleAnyFeatures(ILexEntry entry)
		{
			if (m_MGAGlossListBoxItems == null)
				return;
			foreach (MGA.GlossListBoxItem xn in m_MGAGlossListBoxItems)
			{
				FsFeatureSystem.AddFeatureAsXml(m_cache, xn.XmlNode);
				foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
				{
					IMoInflAffMsa infl = msa as IMoInflAffMsa;
					if (infl != null)
					{
						if (infl.InflFeatsOA == null)
						{
							infl.InflFeatsOA = new FDO.Cellar.FsFeatStruc();
						}
						infl.InflFeatsOA.AddFeatureFromXml(m_cache, xn.XmlNode);
						// if there is a POS, add features to topmost pos' inflectable features
						IPartOfSpeech pos = infl.PartOfSpeechRA;
						if (pos != null)
						{
							IPartOfSpeech topPos = PartOfSpeech.CreateFromDBObject(m_cache, pos.GetHvoOfHighestPartOfSpeech(pos));
							topPos.AddInflectableFeatsFromXml(m_cache, xn.XmlNode);
						}
					}
				}
			}
		}
		private void InsertEntryDlg_Closed(object sender, System.EventArgs e)
		{
			// Save location.
			SettingsKey.SetValue("InsertX", Location.X);
			SettingsKey.SetValue("InsertY", Location.Y);
			SettingsKey.SetValue("InsertWidth", Width);
			// We want to save the default height, without the growing we did
			// to make room for multiple gloss writing systems or a large font
			// in the lexical form box.
			SettingsKey.SetValue("InsertHeight", Height - m_delta);
		}

		/// <summary>
		/// This is triggered also if msGloss has been created, and its text has changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tbGloss_TextChanged(object sender, System.EventArgs e)
		{
			if (m_skipCheck)
				return;

			m_fLexicalFormChanged = false;

			UpdateMatches();
		}

		/// <summary>
		/// Controls appearance of animation window.
		/// </summary>
		internal bool Searching
		{
			get
			{
				CheckDisposed();
				return Controls.Contains(m_searchAnimtation);
			}
			set
			{
				CheckDisposed();

				if (value && !Controls.Contains(m_searchAnimtation))
				{
					Controls.Add(m_searchAnimtation);
					m_searchAnimtation.BringToFront();
				}
				else if (!value && Controls.Contains(m_searchAnimtation))
					Controls.Remove(m_searchAnimtation);
			}
		}

		private void tbLexicalForm_TextChanged(object sender, System.EventArgs e)
		{
			if (m_skipCheck)
				return;
			//TODO?
			Debug.Assert(BestForm != null);

			m_fLexicalFormChanged = true;

			if (BestForm == String.Empty)
			{
				// Set it back to stem, since there are no characters.
				SetMorphType(m_types.Item(MoMorphType.kmtStem), MoStemAllomorph.kclsidMoStemAllomorph);
				m_oldForm = BestForm;
				UpdateMatches();
				return;
			}

			string newForm = BestForm;
			int clsid = MoAffixAllomorph.kClassId; // default;
			string sAdjusted;
			IMoMorphType mmt = m_types.GetTypeIfMatchesPrefix(newForm, out sAdjusted);
			if (mmt != null)
			{
				if (newForm != sAdjusted)
				{
					m_skipCheck = true;
					BestForm = sAdjusted;
					if (msLexicalForm == null)
					{
						tbLexicalForm.SelectionLength = 0;
						tbLexicalForm.SelectionStart = newForm.Length;
					}
					else
					{
						// TODO?
					}
					m_skipCheck = false;
				}
			}
			else if (newForm.Length == 1)
			{
				mmt = m_types.Item(MoMorphType.kmtStem);
				clsid = MoStemAllomorph.kclsidMoStemAllomorph;
			}
			else // Longer than one character.
			{
				try
				{
					mmt = MoMorphType.FindMorphType(m_cache, m_types, ref newForm, out clsid);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, LexText.Controls.LexTextControls.ksInvalidForm,
						MessageBoxButtons.OK);
					m_skipCheck = true;
					BestForm = m_oldForm;
					UpdateMatches();
					m_skipCheck = false;
					return;
				}
			}
			if (mmt != null && mmt != m_morphType)
				SetMorphType(mmt, clsid);
			m_oldForm = BestForm;
			UpdateMatches();
		}

		private void cbMorphType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (m_skipCheck)
				return;

			m_morphType = (IMoMorphType)cbMorphType.SelectedItem;
			m_msaGroupBox.MorphTypePreference = m_morphType;
			m_skipCheck = true;
			if (m_morphType.Guid.ToString() != MoMorphType.kguidMorphCircumfix)
			{ // since circumfixes can be a combination of prefix, infix, and suffix, just leave it as is
				BestForm = m_morphType.FormWithMarkers(BestForm);
			}
			m_skipCheck = false;

			EnableComplexFormTypeCombo();
		}

		private void EnableComplexFormTypeCombo()
		{
			int mtIdx = MoMorphType.FindMorphTypeIndex(m_cache, m_morphType);
			switch (mtIdx)
			{
				case MoMorphType.kmtBoundRoot:
				case MoMorphType.kmtRoot:
					cbComplexFormType.SelectedIndex = 0;
					cbComplexFormType.Enabled = false;
					break;
				case MoMorphType.kmtDiscontiguousPhrase:
				case MoMorphType.kmtPhrase:
					cbComplexFormType.Enabled = true;
					// default to "Unknown" for "phrase"
					if (cbComplexFormType.SelectedIndex == m_idxNotComplex)
						cbComplexFormType.SelectedIndex = m_idxUnknownComplex;
					break;
				default:
					cbComplexFormType.SelectedIndex = 0;
					cbComplexFormType.Enabled = true;
					break;
			}
		}


		private void cbComplexFormType_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_complexType = cbComplexFormType.SelectedItem as ILexEntryType;
			m_fComplexForm = m_complexType != null;
			if (!m_fComplexForm)
			{
				DummyEntryType dum = cbComplexFormType.SelectedItem as DummyEntryType;
				Debug.Assert(dum != null);
				m_fComplexForm = dum.IsComplexForm;
			}
		}

		private void InsertEntryDlg_Load(object sender, System.EventArgs e)
		{
			ITsString tss = BestTssForm;
			if (tss != null && tss.Length > 0)
			{
				string text = tss.Text;
				int ws = StringUtils.GetWsAtOffset(tss, 0);
				if (text == "-")
				{
					// is either prefix or suffix
					int wsEng = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
					if ("prefix" == m_morphType.Name.GetAlternative(wsEng))
					{
						// is prefix so set cursor to beginning (before the hyphen)
						if (msLexicalForm == null)
							tbLexicalForm.Select(0, 0);
						else
							msLexicalForm.Select(ws, 0, 0);
					}
					else
					{
						// is not prefix, so set cursor to end (after the hyphen)
						if (msLexicalForm == null)
							tbLexicalForm.Select(1, 0);
						else
							msLexicalForm.Select(ws, 1, 0);
					}
				}
				else
				{
					if (msLexicalForm == null)
						tbLexicalForm.Select(text.Length, 0);
					else
						msLexicalForm.Select(ws, text.Length, 0);
				}
			}
			else
			{
				if (msLexicalForm == null)
					tbLexicalForm.Select();
				else
					msLexicalForm.Select();
			}
		}

		private void matchingEntries_SelectionChanged(object sender, SIL.FieldWorks.Common.Utils.FwObjectSelectionEventArgs e)
		{
			CheckIfGoto();
		}

		private void CheckIfGoto()
		{
			bool fEnable = (matchingEntries.SelectedEntryID() > 0);
			linkSimilarEntry.TabStop = fEnable;
			labelArrow.Enabled = linkSimilarEntry.Enabled = fEnable;
		}

		private void btnSimilarEntry_Click(object sender, System.EventArgs e)
		{
			UseExistingEntry();
		}

		private void lnkAssistant_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (MsaType == MsaType.kInfl)
			{
				using (MGA.MGADialog dlg = new SIL.FieldWorks.LexText.Controls.MGA.MGADialog(this.m_cache, this.m_mediator, tbLexicalForm.Text))
				{
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						Gloss = dlg.Result;
						m_MGAGlossListBoxItems = dlg.Items;
					}
				}
			}
			else if (MsaType == MsaType.kDeriv)
			{
				MessageBox.Show(LexText.Controls.LexTextControls.ksNoAssistForDerivAffixes,
					LexText.Controls.LexTextControls.ksNotice,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "FLExHelpFile", s_helpTopic);
		}

		void matchingEntries_RestoreFocus(object sender, EventArgs e)
		{
			// Note: due to Keyman/TSF interactions in Indic scripts, do not set focus
			// If it is already set, or we can lose typed characters (e.g., typing poM in
			// Kannada Keyman script causes everything to disappear on M)
			if (m_fLexicalFormChanged)
			{
				if (msLexicalForm == null)
				{
					if (!tbLexicalForm.Focused)
						tbLexicalForm.Focus();
				}
				else
				{
					if (!msLexicalForm.Focused)
						msLexicalForm.Focus();
				}
			}
			else
			{
				if (msGloss == null)
				{
					if (!tbGloss.Focused)
						tbGloss.Focus();
				}
				else
				{
					if (!msGloss.Focused)
						msGloss.Focus();
				}
			}
		}

		#endregion Event Handlers
	}
}
