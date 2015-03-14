// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BasicEntryInfoDlg.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implementation of:
//		InsertEntryDlg - Dialog for adding basic information of new entries.
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using SIL.Collections;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls.MGA;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for InsertEntryDlg.
	/// </summary>
	public class InsertEntryDlg : Form, IFWDisposable
	{
		public enum MorphTypeFilterType
		{
			Prefix,
			Suffix,
			Any
		}

		#region Data members

		private FdoCache m_cache;
		private Mediator m_mediator;
		private ILexEntry m_entry;
		private IMoMorphType m_morphType;
		private ILexEntryType m_complexType;
		private bool m_fComplexForm;
		private bool m_fNewlyCreated;
		private string m_oldForm = "";
		private SimpleMonitor m_updateTextMonitor;
		private ListBox.ObjectCollection m_MGAGlossListBoxItems;

		private Button m_btnOK;
		private Button m_btnCancel;
		private Label m_formLabel;
		private FwTextBox m_tbLexicalForm;	// text box used if one vernacular ws
		private FwTextBox m_tbGloss; // text box used if one analysis ws
		private LabeledMultiStringControl msLexicalForm; // multistring text box used for multiple vernacular ws
		private LabeledMultiStringControl msGloss; // multistring text box used for multiple analysis ws.
		private Button m_btnHelp;
		private Label m_morphTypeLabel;
		private FwOverrideComboBox m_cbMorphType;
		private FwOverrideComboBox m_cbComplexFormType;
		protected GroupBox m_matchingEntriesGroupBox;
		private MatchingObjectsBrowser m_matchingObjectsBrowser;
		private ToolTip m_toolTipSlotCombo;
		private MSAGroupBox m_msaGroupBox;
		private IContainer components;

		private string s_helpTopic = "khtpInsertEntry";
		private LinkLabel m_linkSimilarEntry;
		private ImageList m_imageList;
		private Label m_labelArrow;
		private readonly HelpProvider m_helpProvider;
		protected SearchingAnimation m_searchAnimation;
		/// <summary>
		/// Remember how much we adjusted the height for the lexical form and gloss
		/// text boxes.
		/// </summary>
		private int m_delta;
		private GroupBox m_propsGroupBox;
		private Label m_complexTypeLabel;

		// These are used to identify the <Not Complex> and <Unknown Complex Form>
		// entries in the combobox list.
		int m_idxNotComplex;
		int m_idxUnknownComplex;
		private GroupBox m_glossGroupBox;
		private LinkLabel m_lnkAssistant;

		private bool m_fLexicalFormInitialFocus = true;
		#endregion // Data members


		/// <summary>
		/// This class allows a dummy LexEntryType replacement for "&lt;Unknown&gt;".
		/// </summary>
		internal class DummyEntryType
		{
			private readonly string m_sName;
			private readonly bool m_fIsComplexForm;

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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				using (var regKey = FwRegistryHelper.FieldWorksRegistryKey)
				{
					return regKey.CreateSubKey("LingCmnDlgs");
				}
			}
		}

		private string Form
		{
			get
			{
				string sForm = null;
				if (msLexicalForm == null)
				{
					sForm = m_tbLexicalForm.Text;
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
					m_tbLexicalForm.Text = value.Trim();
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
				return msLexicalForm == null ? m_tbLexicalForm.Tss : msLexicalForm.Value(m_cache.DefaultVernWs);
			}
			set
			{
				CheckDisposed();
				if (msLexicalForm == null)
				{
					m_tbLexicalForm.Tss = value;
				}
				else
				{
					int wsForm = TsStringUtils.GetWsAtOffset(value, 0);
					bool fVern = m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Contains(wsForm);
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
				ITsString tssBestGloss;
				// FWNX-260: added 'msGloss.RootBox.Selection == null'
				if (msGloss == null || msGloss.RootBox.Selection == null)
				{
					tssBestGloss = m_tbGloss.Tss;
				}
				else
				{
					int wsGloss = WritingSystemServices.kwsFirstAnal;
					// if there is a selection in the MultiStringControl
					// use the anchor ws from that selection.
					var tsi = new TextSelInfo(msGloss.RootBox);
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
					glossAnalysis = m_tbGloss.Text;
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
					m_tbGloss.Text = value.Trim();
				else
					msGloss.SetValue(m_cache.DefaultAnalWs, value.Trim());
			}
		}

		private static string TrimOrGetEmptyString(string s)
		{
			return String.IsNullOrEmpty(s) ? String.Empty : s.Trim();
		}

		public ITsString TssGloss
		{
			// REVIEW: trim?
			get
			{
				CheckDisposed();
				return msGloss == null ? m_tbGloss.Tss : msGloss.Value(m_cache.DefaultAnalWs);
			}
			set
			{
				CheckDisposed();
				if (msGloss == null)
				{
					m_tbGloss.Tss = value;
				}
				else
				{
					int wsGloss = TsStringUtils.GetWsAtOffset(value, 0);
					bool fAnal = false;
					foreach (CoreWritingSystemDefinition ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						if (ws.Handle == wsGloss)
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
				if (ws == m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle)
					m_tbGloss.Tss = tss;
			}
			else
			{
				msGloss.SetValue(ws, tss);
			}
		}

		public IPartOfSpeech POS
		{
			set
			{
				CheckDisposed();
				m_msaGroupBox.StemPOS = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the mediator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Mediator Mediator
		{
			set
			{
				Debug.Assert(value != null);
				m_mediator = value;
				if (m_mediator.HelpTopicProvider != null)
				{
					m_helpProvider.HelpNamespace = m_mediator.HelpTopicProvider.HelpFile;
					m_helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(s_helpTopic));
				}
				m_btnHelp.Enabled = (m_mediator.HelpTopicProvider != null);
			}
		}

		public MsaType MsaType
		{
			get
			{
				CheckDisposed();

				return m_msaGroupBox != null ? m_msaGroupBox.MSAType : MsaType.kStem;
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

				return m_msaGroupBox != null ? m_msaGroupBox.Slot : null;
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
			AccessibleName = GetType().Name;

			// Figure out where to locate the dlg.
			using (var regKey = SettingsKey)
			{
				object obj = regKey.GetValue("InsertX");
				if (obj != null)
				{
					var x = (int)obj;
					var y = (int)regKey.GetValue("InsertY");
					var width = (int)regKey.GetValue("InsertWidth", Width);
					var height = (int)regKey.GetValue("InsertHeight", Height);
					var rect = new Rectangle(x, y, width, height);
					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}

			m_helpProvider = new HelpProvider();
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

			m_updateTextMonitor = new SimpleMonitor();

			m_searchAnimation = new SearchingAnimation();
			AdjustWidthForLinkLabelGroupBox();
		}

		/// <summary>
		/// Adjust the width of the group box containing the LinkLabel to allow longer
		/// translated labels to be visible (if possible).
		/// </summary>
		private void AdjustWidthForLinkLabelGroupBox()
		{
			int maxWidth = m_matchingEntriesGroupBox.Width;
			int needWidth = m_lnkAssistant.Location.X + m_lnkAssistant.Width + 2;
			if (needWidth > m_glossGroupBox.Width || m_glossGroupBox.Width > maxWidth)
				m_glossGroupBox.Width = Math.Min(needWidth, maxWidth);
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
			Size size = Size;
			base.OnLoad(e);
			if (Size != size)
				Size = size;
#if __MonoCS__
			// Mono doesn't seem to fire the Activated event, so call the method here.
			// This fixes FWNX-783, setting the focus in the gloss textbox.
			SetInitialFocus();
#endif
		}

		bool m_fInitialized;
		/// <summary>
		/// Set the initial focus to either the lexical form or the gloss.
		/// </summary>
		void SetInitialFocus()
		{
			if (!m_fInitialized)
			{
				if (m_fLexicalFormInitialFocus)
				{
					if (msLexicalForm == null)
						m_tbLexicalForm.Select();
					else
						msLexicalForm.Select();
				}
				else
				{
					if (msGloss == null)
						m_tbGloss.Select();
					else
						msGloss.Select();
				}
				m_fInitialized = true;
			}
		}

		/// <summary>
		/// This shouldn't be needed, but without it, the dialog can start up with the focus
		/// in the lexical form text box, but the keyboard set to the analysis writing system
		/// instead of the vernacular writing system.  See LT-4719.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			SetInitialFocus();
		}

#if __MonoCS__
		/// <summary>
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			// FWNX-520: fix some focus issues.
			// By the time this message is processed, the popup form (PopupTree) may need to be the
			// active window, so ignore WM_ACTIVATE.
			if (m.Msg == 0x6 /*WM_ACTIVATE*/ && System.Windows.Forms.Form.ActiveForm == this)
			{
				return;
			}

			base.WndProc(ref m);
		}
#endif

		/// <summary>
		/// Initialize the dialog.
		/// </summary>
		/// <param name="cache">The FDO cache to use.</param>
		/// <param name="morphType">The morpheme type</param>
		/// <remarks>All other variations of SetDlgInfo should eventually call this one.</remarks>
		protected void SetDlgInfo(FdoCache cache, IMoMorphType morphType)
		{
			SetDlgInfo(cache, morphType, 0, MorphTypeFilterType.Any);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "searchEngine is disposed by the mediator.")]
		protected void SetDlgInfo(FdoCache cache, IMoMorphType morphType, int wsVern, MorphTypeFilterType filter)
		{
			try
			{
				IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
				var xnWindow = (XmlNode) m_mediator.PropertyTable.GetValue("WindowConfiguration");
				XmlNode configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingEntries\"]/parameters");

				SearchEngine searchEngine = SearchEngine.Get(m_mediator, "InsertEntrySearchEngine", () => new InsertEntrySearchEngine(cache));

				m_matchingObjectsBrowser.Initialize(cache, stylesheet, m_mediator, configNode,
					searchEngine);

				m_cache = cache;

				m_fNewlyCreated = false;
				m_oldForm = "";

				// Set fonts for the two edit boxes.
				if (stylesheet != null)
				{
					m_tbLexicalForm.StyleSheet = stylesheet;
					m_tbGloss.StyleSheet = stylesheet;
				}

				// Set writing system factory and code for the two edit boxes.
				IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
				CoreWritingSystemDefinition defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
				CoreWritingSystemDefinition defVernWs = wsContainer.DefaultVernacularWritingSystem;
				m_tbLexicalForm.WritingSystemFactory = cache.WritingSystemFactory;
				m_tbGloss.WritingSystemFactory = cache.WritingSystemFactory;

				m_tbLexicalForm.AdjustStringHeight = false;
				m_tbGloss.AdjustStringHeight = false;

				if (wsVern <= 0)
					wsVern = defVernWs.Handle;
				// initialize to empty TsStrings
				ITsStrFactory tsf = cache.TsStrFactory;
				//we need to use the wsVern so that tbLexicalForm is sized correctly for the font size.
				//In Interlinear text the baseline can be in any of the vernacular writing systems, not just
				//the defaultVernacularWritingSystem.
				ITsString tssForm = tsf.MakeString("", wsVern);
				ITsString tssGloss = tsf.MakeString("", defAnalWs.Handle);

				using (m_updateTextMonitor.Enter())
				{
					m_tbLexicalForm.WritingSystemCode = wsVern;
					m_tbGloss.WritingSystemCode = defAnalWs.Handle;

					TssForm = tssForm;
					TssGloss = tssGloss;
				}

				// start building index
				m_matchingObjectsBrowser.SearchAsync(BuildSearchFieldArray(tssForm, tssGloss));

				((ISupportInitialize)(m_tbLexicalForm)).EndInit();
				((ISupportInitialize)(m_tbGloss)).EndInit();

				if (WritingSystemServices.GetWritingSystemList(m_cache, WritingSystemServices.kwsVerns, false).Count > 1)
				{
					msLexicalForm = ReplaceTextBoxWithMultiStringBox(m_tbLexicalForm, WritingSystemServices.kwsVerns, stylesheet);
					msLexicalForm.TextChanged += tbLexicalForm_TextChanged;
				}
				else
				{
					// See if we need to adjust the height of the lexical form
					AdjustTextBoxAndDialogHeight(m_tbLexicalForm);
				}

				// JohnT addition: if multiple analysis writing systems, replace tbGloss with msGloss
				if (WritingSystemServices.GetWritingSystemList(m_cache, WritingSystemServices.kwsAnals, false).Count > 1)
				{
					msGloss = ReplaceTextBoxWithMultiStringBox(m_tbGloss, WritingSystemServices.kwsAnals, stylesheet);
					m_lnkAssistant.Top = msGloss.Bottom - m_lnkAssistant.Height;
					msGloss.TextChanged += tbGloss_TextChanged;
				}
				else
				{
					// See if we need to adjust the height of the gloss
					AdjustTextBoxAndDialogHeight(m_tbGloss);
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
				m_cbMorphType.Font = new Font(defAnalWs.DefaultFontName, 10);

				// Populate morph type combo.
				// first Fill ComplexFormType combo, since cbMorphType controls
				// whether it gets enabled and which index is selected.
				m_cbComplexFormType.Font = new Font(defAnalWs.DefaultFontName, 10);
				var rgComplexTypes = new List<ICmPossibility>(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities.ToArray());
				rgComplexTypes.Sort();
				m_idxNotComplex = m_cbComplexFormType.Items.Count;
				m_cbComplexFormType.Items.Add(new DummyEntryType(LexTextControls.ksNotApplicable, false));
				m_idxUnknownComplex = m_cbComplexFormType.Items.Count;
				m_cbComplexFormType.Items.Add(new DummyEntryType(LexTextControls.ksUnknownComplexForm, true));
				for (int i = 0; i < rgComplexTypes.Count; ++i)
				{
					var type = (ILexEntryType)rgComplexTypes[i];
					m_cbComplexFormType.Items.Add(type);
				}
				m_cbComplexFormType.SelectedIndex = 0;
				m_cbComplexFormType.Visible = true;
				m_cbComplexFormType.Enabled = true;
				// Convert from Set to List, since the Set can't sort.

				var al = new List<IMoMorphType>();
				foreach (IMoMorphType mType in m_cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities.Cast<IMoMorphType>())
				{
					switch (filter)
					{
						case MorphTypeFilterType.Prefix:
							if (mType.IsPrefixishType)
								al.Add(mType);
							break;

						case MorphTypeFilterType.Suffix:
							if (mType.IsSuffixishType)
								al.Add(mType);
							break;

						case MorphTypeFilterType.Any:
							al.Add(mType);
							break;
					}
				}
				al.Sort();
				for (int i = 0; i < al.Count; ++i)
				{
					m_cbMorphType.Items.Add(al[i]);
					if (al[i] == morphType)
						m_cbMorphType.SelectedIndex = i;
				}

				m_morphType = morphType; // Is this still needed?
				m_msaGroupBox.MorphTypePreference = m_morphType;
				// Now position the searching animation
				/*
					* This position put the animation over the Glossing Assistant button. LT-9146
				m_searchAnimation.Top = groupBox2.Top - m_searchAnimation.Height - 5;
				m_searchAnimation.Left = groupBox2.Right - m_searchAnimation.Width - 10;
					*/
				/* This position puts the animation over the top left corner, but will that
					* look okay with right-to-left?
				m_searchAnimation.Top = groupBox2.Top + 40;
				m_searchAnimation.Left = groupBox2.Left + 10;
					*/
				// This position puts the animation close to the middle of the list.
				m_searchAnimation.Top = m_matchingEntriesGroupBox.Top + (m_matchingEntriesGroupBox.Height / 2) - (m_searchAnimation.Height / 2);
				m_searchAnimation.Left = m_matchingEntriesGroupBox.Left + (m_matchingEntriesGroupBox.Width / 2) - (m_searchAnimation.Width / 2);
			}
			catch(Exception e)
			{
				MessageBox.Show(e.ToString());
				MessageBox.Show(e.StackTrace);
			}
		}

		private LabeledMultiStringControl ReplaceTextBoxWithMultiStringBox(FwTextBox tb, int wsType,
			IVwStylesheet stylesheet)
		{
			tb.Hide();
			var ms = new LabeledMultiStringControl(m_cache, wsType, stylesheet)
			{
				Location = tb.Location,
				Width = tb.Width,
				Anchor = tb.Anchor
			};

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

			Mediator = mediator;
			var morphComponents = MorphServices.BuildMorphComponents(cache, tssForm, MoMorphTypeTags.kguidMorphStem);
			var morphType = morphComponents.MorphType;
			IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
			int wsForm = TsStringUtils.GetWsAtOffset(tssForm, 0);
			bool fVern = wsContainer.CurrentVernacularWritingSystems.Contains(wsForm);
			int wsVern = fVern ? wsForm : wsContainer.DefaultVernacularWritingSystem.Handle;
			SetDlgInfo(cache, morphType, wsVern, MorphTypeFilterType.Any);
			if (fVern)
			{
				TssForm = tssForm;

				TssGloss = TsStringUtils.MakeTss("", wsContainer.DefaultAnalysisWritingSystem.Handle);
				// The lexical form is already set, so shift focus to the gloss when
				// the form is activated.
				m_fLexicalFormInitialFocus = false;
			}
			else
			{
				TssForm = TsStringUtils.MakeTss("", wsContainer.DefaultVernacularWritingSystem.Handle);
				TssGloss = tssForm;
				// The gloss is already set, so shift the focus to the lexical form
				// when the form is activated.
				m_fLexicalFormInitialFocus = true;
			}

			if (tssForm.Length > 0)
				UpdateMatches();
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

			Debug.Assert(persistProvider != null);
			Mediator = mediator;

			SetDlgInfo(cache);
		}

		/// <summary>
		/// Initialize the dialog.
		/// </summary>
		/// <param name="cache">The FDO cache to use.</param>
		/// <param name="morphType">The morpheme type</param>
		/// <param name="msaType">The type of msa</param>
		/// <param name="slot">The default slot of the inflectional affix msa to</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="filter">The filter.</param>
		public void SetDlgInfo(FdoCache cache, IMoMorphType morphType,
			MsaType msaType, IMoInflAffixSlot slot, Mediator mediator, MorphTypeFilterType filter)
		{
			CheckDisposed();

			Mediator = mediator;

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
			SetDlgInfo(cache, cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem));
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
		/// <param name="entry">The entry.</param>
		/// <param name="newlyCreated">true if entry was just created, otherwise false.</param>
		public void GetDialogInfo(out ILexEntry entry, out bool newlyCreated)
		{
			CheckDisposed();

			entry = m_entry;
			newlyCreated = m_fNewlyCreated;
		}

		private string GetTitle()
		{
			return m_mediator == null ? LexText.Controls.LexTextControls.ksNewEntry
				: m_mediator.StringTbl.GetStringWithXPath("CreateEntry", "/group[@id=\"DialogTitles\"]/");
		}

		protected virtual void UpdateMatches()
		{
// This timing was useful for checking fixes for LT-11547.
//#if DEBUG
//            var dtStart = DateTime.Now;
//#endif
			ITsString tssForm = TssForm;
			int vernWs = TsStringUtils.GetWsAtOffset(tssForm, 0);
			string form = MorphServices.EnsureNoMarkers(tssForm.Text, m_cache);
			tssForm = m_cache.TsStrFactory.MakeString(form, vernWs);

			ITsString tssGloss = SelectedOrBestGlossTss;

			if (!Controls.Contains(m_searchAnimation))
			{
				Controls.Add(m_searchAnimation);
				m_searchAnimation.BringToFront();
			}

			m_matchingObjectsBrowser.SearchAsync(BuildSearchFieldArray(tssForm, tssGloss));
//#if DEBUG
//            var dtEnd = DateTime.Now;
//            var diff = dtEnd - dtStart;
//            Debug.WriteLine(String.Format("InsertEntryDlg.UpdateMatches took {0}", diff));
//#endif
		}

		private SearchField[] BuildSearchFieldArray(ITsString tssForm, ITsString tssGloss)
		{
			var fields = new List<SearchField>();

			if(m_matchingObjectsBrowser.IsVisibleColumn("EntryHeadword") || m_matchingObjectsBrowser.IsVisibleColumn("CitationForm"))
				fields.Add(new SearchField(LexEntryTags.kflidCitationForm, tssForm));
			if (m_matchingObjectsBrowser.IsVisibleColumn("EntryHeadword") || m_matchingObjectsBrowser.IsVisibleColumn("LexemeForm"))
				fields.Add(new SearchField(LexEntryTags.kflidLexemeForm, tssForm));
			if (m_matchingObjectsBrowser.IsVisibleColumn("Allomorphs"))
				fields.Add(new SearchField(LexEntryTags.kflidAlternateForms, tssForm));

			if (tssGloss != null && m_matchingObjectsBrowser.IsVisibleColumn("Glosses"))
				fields.Add(new SearchField(LexSenseTags.kflidGloss, tssGloss));

			return fields.ToArray();
		}

		/// <summary>
		/// Set the class and morph type.
		/// </summary>
		/// <param name="mmt">The morph type.</param>
		private void SetMorphType(IMoMorphType mmt)
		{
			if (!m_cbMorphType.Items.Contains(mmt))
				return;

			m_morphType = mmt;
			m_msaGroupBox.MorphTypePreference = mmt;
			using (m_updateTextMonitor.Enter())
				m_cbMorphType.SelectedItem = mmt;
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
			m_linkSimilarEntry.Text = LexTextControls.ksAddAllomorphToSimilarEntry;
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			CheckDisposed();

			s_helpTopic = helpTopic;
			if (m_mediator.HelpTopicProvider != null)
			{
				m_helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(s_helpTopic));
				m_btnHelp.Enabled = true;
			}
		}

		#endregion Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InsertEntryDlg));
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_formLabel = new System.Windows.Forms.Label();
			this.m_tbLexicalForm = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_tbGloss = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_morphTypeLabel = new System.Windows.Forms.Label();
			this.m_cbMorphType = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_cbComplexFormType = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_matchingEntriesGroupBox = new System.Windows.Forms.GroupBox();
			this.m_labelArrow = new System.Windows.Forms.Label();
			this.m_imageList = new System.Windows.Forms.ImageList(this.components);
			this.m_linkSimilarEntry = new System.Windows.Forms.LinkLabel();
			this.m_matchingObjectsBrowser = new SIL.FieldWorks.Common.Controls.MatchingObjectsBrowser();
			this.m_toolTipSlotCombo = new System.Windows.Forms.ToolTip(this.components);
			this.m_msaGroupBox = new SIL.FieldWorks.LexText.Controls.MSAGroupBox();
			this.m_propsGroupBox = new System.Windows.Forms.GroupBox();
			this.m_complexTypeLabel = new System.Windows.Forms.Label();
			this.m_glossGroupBox = new System.Windows.Forms.GroupBox();
			this.m_lnkAssistant = new System.Windows.Forms.LinkLabel();
			((System.ComponentModel.ISupportInitialize)(this.m_tbLexicalForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_tbGloss)).BeginInit();
			this.m_matchingEntriesGroupBox.SuspendLayout();
			this.m_propsGroupBox.SuspendLayout();
			this.m_glossGroupBox.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// m_formLabel
			//
			resources.ApplyResources(this.m_formLabel, "m_formLabel");
			this.m_formLabel.Name = "m_formLabel";
			//
			// m_tbLexicalForm
			//
			this.m_tbLexicalForm.AdjustStringHeight = true;
			this.m_tbLexicalForm.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbLexicalForm.controlID = null;
			resources.ApplyResources(this.m_tbLexicalForm, "m_tbLexicalForm");
			this.m_tbLexicalForm.HasBorder = true;
			this.m_tbLexicalForm.Name = "m_tbLexicalForm";
			this.m_tbLexicalForm.SelectionLength = 0;
			this.m_tbLexicalForm.SelectionStart = 0;
			this.m_tbLexicalForm.TextChanged += new System.EventHandler(this.tbLexicalForm_TextChanged);
			//
			// m_tbGloss
			//
			this.m_tbGloss.AdjustStringHeight = true;
			this.m_tbGloss.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbGloss.controlID = null;
			resources.ApplyResources(this.m_tbGloss, "m_tbGloss");
			this.m_tbGloss.HasBorder = true;
			this.m_tbGloss.Name = "m_tbGloss";
			this.m_tbGloss.SelectionLength = 0;
			this.m_tbGloss.SelectionStart = 0;
			this.m_tbGloss.TextChanged += new System.EventHandler(this.tbGloss_TextChanged);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// m_morphTypeLabel
			//
			resources.ApplyResources(this.m_morphTypeLabel, "m_morphTypeLabel");
			this.m_morphTypeLabel.Name = "m_morphTypeLabel";
			//
			// m_cbMorphType
			//
			this.m_cbMorphType.AllowSpaceInEditBox = false;
			this.m_cbMorphType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbMorphType, "m_cbMorphType");
			this.m_cbMorphType.Name = "m_cbMorphType";
			this.m_cbMorphType.SelectedIndexChanged += new System.EventHandler(this.cbMorphType_SelectedIndexChanged);
			//
			// m_cbComplexFormType
			//
			this.m_cbComplexFormType.AllowSpaceInEditBox = false;
			this.m_cbComplexFormType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbComplexFormType, "m_cbComplexFormType");
			this.m_cbComplexFormType.Name = "m_cbComplexFormType";
			this.m_cbComplexFormType.SelectedIndexChanged += new System.EventHandler(this.cbComplexFormType_SelectedIndexChanged);
			//
			// m_matchingEntriesGroupBox
			//
			resources.ApplyResources(this.m_matchingEntriesGroupBox, "m_matchingEntriesGroupBox");
			this.m_matchingEntriesGroupBox.Controls.Add(this.m_labelArrow);
			this.m_matchingEntriesGroupBox.Controls.Add(this.m_linkSimilarEntry);
			this.m_matchingEntriesGroupBox.Controls.Add(this.m_matchingObjectsBrowser);
			this.m_matchingEntriesGroupBox.Name = "m_matchingEntriesGroupBox";
			this.m_matchingEntriesGroupBox.TabStop = false;
			//
			// m_labelArrow
			//
			resources.ApplyResources(this.m_labelArrow, "m_labelArrow");
			this.m_labelArrow.ImageList = this.m_imageList;
			this.m_labelArrow.Name = "m_labelArrow";
			this.m_labelArrow.Click += new System.EventHandler(this.btnSimilarEntry_Click);
			//
			// m_imageList
			//
			this.m_imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList.ImageStream")));
			this.m_imageList.TransparentColor = System.Drawing.Color.Fuchsia;
			this.m_imageList.Images.SetKeyName(0, "GoToArrow.bmp");
			//
			// m_linkSimilarEntry
			//
			resources.ApplyResources(this.m_linkSimilarEntry, "m_linkSimilarEntry");
			this.m_linkSimilarEntry.Name = "m_linkSimilarEntry";
			this.m_linkSimilarEntry.TabStop = true;
			this.m_linkSimilarEntry.Click += new System.EventHandler(this.btnSimilarEntry_Click);
			//
			// m_matchingObjectsBrowser
			//
			resources.ApplyResources(this.m_matchingObjectsBrowser, "m_matchingObjectsBrowser");
			this.m_matchingObjectsBrowser.Name = "m_matchingObjectsBrowser";
			this.m_matchingObjectsBrowser.TabStop = false;
			this.m_matchingObjectsBrowser.SelectionChanged += new FwSelectionChangedEventHandler(this.m_matchingObjectsBrowser_SelectionChanged);
			this.m_matchingObjectsBrowser.SelectionMade += new FwSelectionChangedEventHandler(this.m_matchingObjectsBrowser_SelectionMade);
			this.m_matchingObjectsBrowser.SearchCompleted += new EventHandler(this.m_matchingObjectsBrowser_SearchCompleted);
			this.m_matchingObjectsBrowser.ColumnsChanged += new EventHandler(this.m_matchingObjectsBrowser_ColumnsChanged);
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
			// m_propsGroupBox
			//
			this.m_propsGroupBox.Controls.Add(this.m_complexTypeLabel);
			this.m_propsGroupBox.Controls.Add(this.m_morphTypeLabel);
			this.m_propsGroupBox.Controls.Add(this.m_cbMorphType);
			this.m_propsGroupBox.Controls.Add(this.m_cbComplexFormType);
			this.m_propsGroupBox.Controls.Add(this.m_tbLexicalForm);
			this.m_propsGroupBox.Controls.Add(this.m_formLabel);
			resources.ApplyResources(this.m_propsGroupBox, "m_propsGroupBox");
			this.m_propsGroupBox.Name = "m_propsGroupBox";
			this.m_propsGroupBox.TabStop = false;
			//
			// m_complexTypeLabel
			//
			resources.ApplyResources(this.m_complexTypeLabel, "m_complexTypeLabel");
			this.m_complexTypeLabel.Name = "m_complexTypeLabel";
			//
			// m_glossGroupBox
			//
			this.m_glossGroupBox.Controls.Add(this.m_lnkAssistant);
			this.m_glossGroupBox.Controls.Add(this.m_tbGloss);
			resources.ApplyResources(this.m_glossGroupBox, "m_glossGroupBox");
			this.m_glossGroupBox.Name = "m_glossGroupBox";
			this.m_glossGroupBox.TabStop = false;
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
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_glossGroupBox);
			this.Controls.Add(this.m_propsGroupBox);
			this.Controls.Add(this.m_msaGroupBox);
			this.Controls.Add(this.m_matchingEntriesGroupBox);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "InsertEntryDlg";
			this.ShowInTaskbar = false;
			this.Load += new System.EventHandler(this.InsertEntryDlg_Load);
			this.Closed += new System.EventHandler(this.InsertEntryDlg_Closed);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.InsertEntryDlg_Closing);
			((System.ComponentModel.ISupportInitialize)(this.m_tbLexicalForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_tbGloss)).EndInit();
			this.m_matchingEntriesGroupBox.ResumeLayout(false);
			this.m_matchingEntriesGroupBox.PerformLayout();
			this.m_propsGroupBox.ResumeLayout(false);
			this.m_glossGroupBox.ResumeLayout(false);
			this.m_glossGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Event Handlers

		private void InsertEntryDlg_Closing(object sender, CancelEventArgs e)
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
					m_entry = (ILexEntry)m_matchingObjectsBrowser.SelectedObject;
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
			string originalForm = form;
			int clsid;
			IMoMorphType mmt = MorphServices.FindMorphType(m_cache, ref form, out clsid);
			bool result;
			switch (m_morphType.Guid.ToString())
			{
				// these cases are not handled by FindMorphType
				case MoMorphTypeTags.kMorphCircumfix:
				case MoMorphTypeTags.kMorphPhrase:
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
				case MoMorphTypeTags.kMorphStem:
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphParticle:
				case MoMorphTypeTags.kMorphClitic:
					result = mmt.Guid == MoMorphTypeTags.kguidMorphStem || mmt.Guid == MoMorphTypeTags.kguidMorphPhrase;
					break;

				case MoMorphTypeTags.kMorphBoundRoot:
					result = mmt.Guid == MoMorphTypeTags.kguidMorphBoundStem;
					break;

				case MoMorphTypeTags.kMorphSuffixingInterfix:
					result = mmt.Guid == MoMorphTypeTags.kguidMorphSuffix;
					break;

				case MoMorphTypeTags.kMorphPrefixingInterfix:
					result = mmt.Guid == MoMorphTypeTags.kguidMorphPrefix;
					break;

				case MoMorphTypeTags.kMorphInfixingInterfix:
					result = mmt.Guid == MoMorphTypeTags.kguidMorphInfix;
					break;

				default:
					result = mmt.Equals(m_morphType);
					break;
			}
			if (result)
				return true; // all is well.
			// Pathologically the user may have changed the markers so that we cannot distinguish things that
			// are normally distinct (e.g., LT-12378).
			var expected = mmt.Prefix + form + mmt.Postfix;
			if (expected == originalForm)
				return true; // predicted form does not match, but the one the user chose would look the same.

			return result;
		}

		/// <summary>
		/// Answer true if we are trying to create a circumfix and the data is not in a state that allows that.
		/// </summary>
		/// <returns></returns>
		private bool CircumfixProblem()
		{
			if (m_morphType.Guid != MoMorphTypeTags.kguidMorphCircumfix)
				return false; // not a circumfix at all.
			if (msLexicalForm == null)
			{
				ITsString tss = TssForm;
				string left, right;
				if (!StringServices.GetCircumfixLeftAndRightParts(m_cache, tss, out left, out right))
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
						if (!StringServices.GetCircumfixLeftAndRightParts(m_cache, tss, out left, out right))
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
			using (new WaitCursor(this))
			{

				ILexEntry newEntry = null;
				UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateEntry, LexTextControls.ksRedoCreateEntry,
											m_cache.ServiceLocator.GetInstance<IActionHandler>(),
											() => { newEntry = CreateNewEntryInternal(); });
				m_entry = newEntry;
				m_fNewlyCreated = true;
			}
		}

		private ILexEntry CreateNewEntryInternal()
		{
			var entryComponents = BuildEntryComponentsDTO();
			ILexEntry newEntry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);
			if (m_fComplexForm)
			{
				ILexEntryRef ler = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				newEntry.EntryRefsOS.Add(ler);
				if (m_complexType != null)
					ler.ComplexEntryTypesRS.Add(m_complexType);
				ler.RefType = LexEntryRefTags.krtComplexForm;
			}
			return newEntry;
		}

		private LexEntryComponents BuildEntryComponentsDTO()
		{
			var entryComponents = new LexEntryComponents();
			entryComponents.MorphType = m_morphType;
			CollectValuesFromMultiStringControl(msLexicalForm, entryComponents.LexemeFormAlternatives, BestTssForm);
			CollectValuesFromMultiStringControl(msGloss, entryComponents.GlossAlternatives,
				TsStringUtils.MakeTss(Gloss, m_cache.DefaultAnalWs));
			entryComponents.MSA = m_msaGroupBox.SandboxMSA;
			if (m_MGAGlossListBoxItems != null)
			{
				foreach (GlossListBoxItem xn in m_MGAGlossListBoxItems)
					entryComponents.GlossFeatures.Add(xn.XmlNode);
			}
			return entryComponents;
		}

		private void CollectValuesFromMultiStringControl(LabeledMultiStringControl lmsControl,
			IList<ITsString> alternativesCollector, ITsString defaultIfNoMultiString)
		{
			var bldr = m_cache.TsStrFactory;
			if (lmsControl == null)
			{
				alternativesCollector.Add(defaultIfNoMultiString);
			}
			else
			{
				// Save all the writing systems.
				for (var i = 0; i < lmsControl.NumberOfWritingSystems; i++)
				{
					int ws;
					ITsString tss = lmsControl.ValueAndWs(i, out ws);
					if (tss != null && tss.Text != null)
					{
						// In the case of copied text, sometimes the string had the wrong ws attached to it. (LT-11950)
						alternativesCollector.Add(bldr.MakeString(tss.Text, ws));
					}
				}
			}
		}

		private void InsertEntryDlg_Closed(object sender, EventArgs e)
		{
			if (IsDisposed)
				return; // Prevent interaction w/ Paratext from causing crash here (LT-13582)

			// Save location.
			using (var regKey = SettingsKey)
			{
				regKey.SetValue("InsertX", Location.X);
				regKey.SetValue("InsertY", Location.Y);
				regKey.SetValue("InsertWidth", Width);
				// We want to save the default height, without the growing we did
				// to make room for multiple gloss writing systems or a large font
				// in the lexical form box.
				regKey.SetValue("InsertHeight", Height - m_delta);
			}
		}

		/// <summary>
		/// This is triggered also if msGloss has been created, and its text has changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tbGloss_TextChanged(object sender, EventArgs e)
		{
			if (m_updateTextMonitor.Busy)
				return;

			UpdateMatches();
		}

		private void tbLexicalForm_TextChanged(object sender, EventArgs e)
		{
			if (m_updateTextMonitor.Busy)
				return;

			//TODO?
			Debug.Assert(BestForm != null);

			if (BestForm == String.Empty)
			{
				// Set it back to stem, since there are no characters.
				SetMorphType(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem));
				m_oldForm = BestForm;
				UpdateMatches();
				return;
			}

			string newForm = BestForm;
			string sAdjusted;
			var mmt = MorphServices.GetTypeIfMatchesPrefix(m_cache, newForm, out sAdjusted);
			if (mmt != null)
			{
				if (newForm != sAdjusted)
				{
					using (m_updateTextMonitor.Enter())
					{
						BestForm = sAdjusted;
						if (msLexicalForm == null)
						{
							m_tbLexicalForm.SelectionLength = 0;
							m_tbLexicalForm.SelectionStart = newForm.Length;
						}
						// TODO: how do we handle multiple writing systems?
					}
				}
			}
			else if (newForm.Length == 1)
			{
				mmt = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			}
			else // Longer than one character.
			{
				try
				{
					int clsid;
					mmt = MorphServices.FindMorphType(m_cache, ref newForm, out clsid);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, LexText.Controls.LexTextControls.ksInvalidForm,
						MessageBoxButtons.OK);
					using (m_updateTextMonitor.Enter())
					{
						BestForm = m_oldForm;
						UpdateMatches();
					}
					return;
				}
			}
			if (mmt != null && mmt != m_morphType)
				SetMorphType(mmt);
			m_oldForm = BestForm;
			UpdateMatches();
		}

		private void cbMorphType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_updateTextMonitor.Busy)
				return;

			m_morphType = (IMoMorphType)m_cbMorphType.SelectedItem;
			m_msaGroupBox.MorphTypePreference = m_morphType;
			if (m_morphType.Guid != MoMorphTypeTags.kguidMorphCircumfix)
			{ // since circumfixes can be a combination of prefix, infix, and suffix, just leave it as is
				using (m_updateTextMonitor.Enter())
					BestForm = m_morphType.FormWithMarkers(BestForm);
			}

			EnableComplexFormTypeCombo();
		}

		private void EnableComplexFormTypeCombo()
		{
			switch (m_morphType.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphBoundRoot:
				case MoMorphTypeTags.kMorphRoot:
					m_cbComplexFormType.SelectedIndex = 0;
					m_cbComplexFormType.Enabled = false;
					break;
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
				case MoMorphTypeTags.kMorphPhrase:
					m_cbComplexFormType.Enabled = true;
					// default to "Unknown" for "phrase"
					if (m_cbComplexFormType.SelectedIndex == m_idxNotComplex)
						m_cbComplexFormType.SelectedIndex = m_idxUnknownComplex;
					break;
				default:
					m_cbComplexFormType.SelectedIndex = 0;
					m_cbComplexFormType.Enabled = true;
					break;
			}
		}


		private void cbComplexFormType_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_complexType = m_cbComplexFormType.SelectedItem as ILexEntryType;
			m_fComplexForm = m_complexType != null;
			if (!m_fComplexForm)
			{
				var dum = (DummyEntryType)m_cbComplexFormType.SelectedItem;
				m_fComplexForm = dum.IsComplexForm;
			}
		}

		private void InsertEntryDlg_Load(object sender, EventArgs e)
		{
			ITsString tss = BestTssForm;
			if (tss != null && tss.Length > 0)
			{
				string text = tss.Text;
				int ws = TsStringUtils.GetWsAtOffset(tss, 0);
				if (text == "-")
				{
					// is either prefix or suffix
					int wsEng = m_cache.WritingSystemFactory.GetWsFromStr("en");
					if ("prefix" == m_morphType.Name.get_String(wsEng).Text)
					{
						// is prefix so set cursor to beginning (before the hyphen)
						if (msLexicalForm == null)
							m_tbLexicalForm.Select(0, 0);
						else
							msLexicalForm.Select(ws, 0, 0);
					}
					else
					{
						// is not prefix, so set cursor to end (after the hyphen)
						if (msLexicalForm == null)
							m_tbLexicalForm.Select(1, 0);
						else
							msLexicalForm.Select(ws, 1, 0);
					}
				}
				else
				{
					if (msLexicalForm == null)
						m_tbLexicalForm.Select(text.Length, 0);
					else
						msLexicalForm.Select(ws, text.Length, 0);
				}
			}
			else
			{
				if (msLexicalForm == null)
					m_tbLexicalForm.Select();
				else
					msLexicalForm.Select();
			}
		}

		private void m_matchingObjectsBrowser_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			CheckIfGoto();
		}

		private void m_matchingObjectsBrowser_SelectionMade(object sender, FwObjectSelectionEventArgs e)
		{
			DialogResult = DialogResult.Yes;
			Close();
		}

		private void m_matchingObjectsBrowser_SearchCompleted(object sender, EventArgs e)
		{
			CheckIfGoto();
			if (Controls.Contains(m_searchAnimation))
				Controls.Remove(m_searchAnimation);
		}

		private void m_matchingObjectsBrowser_ColumnsChanged(object sender, EventArgs e)
		{
			UpdateMatches();
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void CheckIfGoto()
		{
			bool fEnable = m_matchingObjectsBrowser.SelectedObject != null;
			m_linkSimilarEntry.TabStop = fEnable;
			m_labelArrow.Enabled = m_linkSimilarEntry.Enabled = fEnable;
		}

		private void btnSimilarEntry_Click(object sender, EventArgs e)
		{
			UseExistingEntry();
		}

		private void lnkAssistant_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (MsaType == MsaType.kInfl)
			{
				MGADialog dlg;
				// Get a wait cursor by setting the LinkLabel to use a wait cursor. See FWNX-700.
				// Need to use a wait cursor while creating dialog, but not when showing it.
				using (new WaitCursor(m_lnkAssistant))
					dlg = new MGAHtmlHelpDialog(m_cache, m_mediator, m_tbLexicalForm.Text);

				using (dlg)
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
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, "FLExHelpFile", s_helpTopic);
		}

		#endregion Event Handlers
	}
}
