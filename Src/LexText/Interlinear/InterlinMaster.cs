using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using System.IO;
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.FwUtils;


namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// InterlinMaster is a master control for the main pane of an interlinear view.
	/// It holds and information bar ("Information"), a TitleContents pane,
	/// another information bar ("Text"/"Interlinear Text") with a label button
	/// ("Show Interlinear"/"Show Raw Text") and then either a RawTextPane or an
	/// InterlinDocChild. Eventually it may also show a SandBox, and perhaps a
	/// segment of a lexicon! This comment is way out-of-date!
	/// </summary>
	public class InterlinMaster : RecordView
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		// Controls
		protected TitleContentsPane m_tcPane; // Parent is 'this'.
		IVwStylesheet m_styleSheet;

		protected System.Windows.Forms.TabControl m_tabCtrl; // Parent is 'this'.
		protected System.Windows.Forms.TabPage m_tpInfo; // Parent is m_tabCtrl.
		protected System.Windows.Forms.TabPage m_tpRawText; // Parent is m_tabCtrl.
		protected System.Windows.Forms.TabPage m_tpGloss; // Parent is m_tabCtrl.
		protected System.Windows.Forms.TabPage m_tpInterlinear; // Parent is m_tabCtrl.
		protected System.Windows.Forms.TabPage m_tpTagging; // Parent is m_tabCtrl.
		protected System.Windows.Forms.TabPage m_tpPrintView; // Parent is m_tabCtrl; holds PrintView.
		protected System.Windows.Forms.TabPage m_tpCChart; // Parent is m_tabCtrl; holds constituent chart.
		protected InfoPane m_infoPane; // Parent is m_tpInfo.
		protected RawTextPane m_rtPane; // Parent is m_tpRawText.
		// Panel m_panelInterlin holds m_idcPane.
		protected Panel m_panelInterlin; // Parent is m_tpInterlinear.
		// Not created until needed; may be null!
		protected InterlinDocChild m_idcPane; // Parent is m_panelInterlin.
		protected Panel m_panelTagging; // Parent is m_tpTagging.
		protected InterlinTaggingChild m_taggingViewPane; // Parent is m_panelTagging.
		protected Panel m_panelPrintView; // Parent is m_tpPrintView.
		protected InterlinPrintChild m_printViewPane; // Parent is m_panelPrintView m_tpPrintView.

		/// <summary>
		/// This variable is the main constituent chart pane, SIL.FieldWorks.Discourse.ConstituentChart.
		/// Because of problems with circular references, we define it just as a UserControl, and
		/// create it (and call a couple of key methods) by reflection).
		/// Parent is m_tpCChart.
		/// </summary>
		internal UserControl m_constChartPane;

		protected int m_hvoStText;  // HVO of the main Text object we're showing.
		public InterAreaBookmark m_bookmark;
		protected int m_tabIndex = -1; // remember the tab control index
		private bool m_fParsedTextDuringSave = false;
		private bool m_fSkipNextParse = false;
		private bool m_fRefreshOccurred = false;

		// true (typically used as concordance 3rd pane) to suppress autocreating a text if the
		// clerk has no current object.
		protected bool m_fSuppressAutoCreate;
		/// <summary>
		/// Numbers identifying the main tabs in the interlinear text.
		/// </summary>
		public enum TabPageSelection
		{
			Info = 0,
			RawText = 1,
			Gloss = 2,
			Interlinearizer = 3,
			TaggingView = 4,
			PrintView = 5,
			ConstituentChart = 6
		}

		protected class ITextTabControl : TabControl
		{
			internal ITextTabControl() : base()
			 { }

			protected override void OnGotFocus(EventArgs e)
			{
				base.OnGotFocus(e);
				// pass Focus to control in tab page.
				if (this.SelectedTab != null)
				{
					if (this.SelectedTab.Controls.Count > 0)
						this.SelectedTab.Controls[0].Focus();
					else
						this.SelectedTab.Focus();
				}
			}

			protected override void OnLostFocus(EventArgs e)
			{
				base.OnLostFocus(e);
			}
		}

		// These constants allow us to use a switch statement in SaveBookMark()
		const int ktpsInfo = (int)TabPageSelection.Info;
		const int ktpsRawText = (int)TabPageSelection.RawText;
		const int ktpsGloss = (int)TabPageSelection.Gloss;
		const int ktpsAnalyze = (int)TabPageSelection.Interlinearizer;
		const int ktpsTagging = (int)TabPageSelection.TaggingView;
		const int ktpsPrint = (int)TabPageSelection.PrintView;
		const int ktpsCChart = (int)TabPageSelection.ConstituentChart;

		const int kflidParagraphs = (int)StText.StTextTags.kflidParagraphs;
		const int kflidBeginObject = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject;
		const int kflidAnnotationType = (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType;
		const int kflidInstanceOf = (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf;

		public InterlinMaster()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		internal bool ParsedDuringSave
		{
			get
			{
				CheckDisposed();
				return m_fParsedTextDuringSave;
			}
			set
			{
				CheckDisposed();
				m_fParsedTextDuringSave = value;
			}
		}

		protected int GetWidth(string text, Font fnt)
		{
			int width = 0;
			using (Graphics g = Graphics.FromHwnd(Handle))
			{
				width = (int)g.MeasureString(text, fnt).Width + 1;
			}
			return width;
		}

		void SetStyleSheetFor(SimpleRootSite site)
		{
			if (m_styleSheet == null)
				GetStyleSheetFromForm();
			if (site != null)
				site.StyleSheet = m_styleSheet;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			if (m_styleSheet == null)
			{
				GetStyleSheetFromForm();
				if (m_styleSheet != null)
				{
					SetStyleSheetFor(m_tcPane);
					SetStyleSheetFor(m_rtPane);
					SetStyleSheetFor(m_idcPane);
					SetStyleSheetFor(m_taggingViewPane);
					SetStyleSheetFor(m_printViewPane);
					SetStyleSheetForConstChart();
				}
			}
			base.OnHandleCreated (e);
			// re-select our annotation if we're in the raw text pane, since
			// initialization subsequent to ShowRecord() loses our selection.
			if (this.m_tabIndex == ktpsRawText)
				this.SelectAnnotation();
		}

		/// <summary>
		/// Override method to add other content to main control.
		/// </summary>
		protected override void AddPaneBar()
		{
			try
			{
				GetStyleSheetFromForm();

				base.AddPaneBar();
				// This stuff is usually in InitializeComponent, but because the components here
				// are custom and somewhat flaky working with the designer, I just put them
				// here.
				SuspendLayout();
				// Note: The two things we want docked at the top are added in reverse
				// order, since the one added last is at the top of the Z order which puts it
				// really at the top.

				Controls.Add(m_tabCtrl);
				Controls.Add(m_tcPane);

				m_tabCtrl.ResumeLayout(false);
				m_tpInfo.ResumeLayout(false);
				m_tpRawText.ResumeLayout(false);
				m_tpGloss.ResumeLayout(false);
				m_tpInterlinear.ResumeLayout(false);
				m_tpTagging.ResumeLayout(false);
				m_tpPrintView.ResumeLayout(false);
				m_tpCChart.ResumeLayout(false);
				ResumeLayout();
			}
			catch (ApplicationException)
			{
				//m_informationBar = new ImageHolder(); //something to show at design time
			}
		}

		protected override void SetInfoBarText()
		{
			if (m_informationBar != null && m_configurationParameters != null)
			{
				string sAltTitle = XmlUtils.GetAttributeValue(m_configurationParameters, "altTitleId");
				if (!String.IsNullOrEmpty(sAltTitle))
				{
					string sTitle = StringTbl.GetString(sAltTitle, "AlternativeTitles");
					if (!String.IsNullOrEmpty(sTitle))
					{
						((IPaneBar)m_informationBar).Text = sTitle;
						return;
					}
				}
			}
			base.SetInfoBarText();
		}

		private void MakeTabControlAndPages()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InterlinMaster));
			//
			//  create the TabControl and TabPages
			//
			m_tabCtrl = new InterlinMaster.ITextTabControl();
			m_tabCtrl.SuspendLayout();
			m_tpInfo = new System.Windows.Forms.TabPage();
			m_tpInfo.SuspendLayout();
			m_tpRawText = new System.Windows.Forms.TabPage();
			m_tpRawText.SuspendLayout();
			m_tpGloss = new System.Windows.Forms.TabPage();
			m_tpGloss.SuspendLayout();
			m_tpInterlinear = new System.Windows.Forms.TabPage();
			m_tpInterlinear.SuspendLayout();
			m_tpTagging = new System.Windows.Forms.TabPage();
			m_tpTagging.SuspendLayout();
			m_tpPrintView = new System.Windows.Forms.TabPage();
			m_tpPrintView.SuspendLayout();
			m_tpCChart = new System.Windows.Forms.TabPage();
			m_tpCChart.SuspendLayout();
			//
			// Finish defining the TabControl.
			//
			resources.ApplyResources(this.m_tabCtrl, "m_tabCtrl");
			m_tabCtrl.AccessibleDescription = "tab control for Text/Interlinear Text";
			m_tabCtrl.AccessibleName = "m_tabCtrl.AccessibleName";
			m_tabCtrl.Appearance = System.Windows.Forms.TabAppearance.Normal;
			//The order of these must match TabPageSelection order
			m_tabCtrl.Controls.Add(m_tpInfo);
			m_tabCtrl.Controls.Add(m_tpRawText);
			m_tabCtrl.Controls.Add(m_tpGloss);
			m_tabCtrl.Controls.Add(m_tpInterlinear);
			m_tabCtrl.Controls.Add(m_tpTagging);
			m_tabCtrl.Controls.Add(m_tpPrintView);
			m_tabCtrl.Controls.Add(m_tpCChart);
			m_tabCtrl.Dock = System.Windows.Forms.DockStyle.Fill;
			//m_tabCtrl.Location = new System.Drawing.Point(8, m_tcPane.Height + 8);
			m_tabCtrl.Name = "m_tabCtrl";
			m_tabCtrl.Padding = new System.Drawing.Point(6,3);
			m_tabCtrl.RightToLeft = System.Windows.Forms.RightToLeft.Inherit;
			m_tabCtrl.SelectedIndex = ktpsRawText;
			m_tabCtrl.SelectedIndexChanged += new System.EventHandler(m_tabCtrl_SelectedIndexChanged);
			//
			//  Finish defining m_tpInfo.
			//
			resources.ApplyResources(this.m_tpInfo, "m_tpInfo");
			m_tpInfo.AccessibleDescription = "tab page for Comments";
			m_tpInfo.AccessibleName = "m_tpInfo.AccessibleName";
			m_tpInfo.Name = "m_tpInfo";
			m_tpInfo.RightToLeft = System.Windows.Forms.RightToLeft.Inherit;
			//
			//  m_tpRawText
			//
			resources.ApplyResources(this.m_tpRawText, "m_tpRawText");
			m_tpRawText.AccessibleDescription = "tab page for Text";
			m_tpRawText.AccessibleName = "m_tpRawText.AccessibleName";
			m_tpRawText.Name = "m_tpRawText";
			m_tpRawText.RightToLeft = System.Windows.Forms.RightToLeft.Inherit;
			//
			//  m_tpInterlinear & m_tpGloss
			//
			m_panelInterlin = new Panel();
			m_panelInterlin.Dock = DockStyle.Fill;
			InitializeInterlinearTabPage(m_tpGloss, "m_tpGloss");
			InitializeInterlinearTabPage(m_tpInterlinear,"m_tpInterlinear");
			// First add the interlin panel to the gloss tab.
			m_tpGloss.Controls.Add(m_panelInterlin);
			//
			//  m_tpTaggingView
			//
			m_panelTagging = new Panel();
			m_panelTagging.Dock = DockStyle.Fill;
			resources.ApplyResources(this.m_tpTagging, "m_tpTagging");
			m_tpTagging.AccessibleDescription = "tab page for Tagging View";
			m_tpTagging.AccessibleName = "m_tpTagging.AccessibleName";
			m_tpTagging.Name = "m_tpTagging";
			m_tpTagging.RightToLeft = System.Windows.Forms.RightToLeft.Inherit;
			m_tpTagging.Controls.Add(m_panelTagging);
			//
			//  m_tpPrintView
			//
			m_panelPrintView = new Panel();
			m_panelPrintView.Dock = DockStyle.Fill;
			resources.ApplyResources(this.m_tpPrintView, "m_tpPrintView");
			m_tpPrintView.AccessibleDescription = "tab page for Print View";
			m_tpPrintView.AccessibleName = "m_tpPrintView.AccessibleName";
			m_tpPrintView.Name = "m_tpPrintView";
			m_tpPrintView.RightToLeft = System.Windows.Forms.RightToLeft.Inherit;
			m_tpPrintView.Controls.Add(m_panelPrintView);
			//
			//  Finish defining m_tpCChart.
			//
			resources.ApplyResources(this.m_tpCChart, "m_tpCChart");
			m_tpCChart.AccessibleDescription = "tab page for Constituent Charts";
			m_tpCChart.AccessibleName = "m_tpCChart.AccessibleName";
			m_tpCChart.Name = "m_tpCChart";
			m_tpCChart.RightToLeft = System.Windows.Forms.RightToLeft.Inherit;
		}

		private void InitializeInterlinearTabPage(System.Windows.Forms.TabPage tpInterlinear, string name)
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InterlinMaster));
			//
			//  Finish defining tpInterlinear.
			//
			resources.ApplyResources(tpInterlinear, name);
			tpInterlinear.AccessibleDescription = "tab page for Interlinear Text";
			tpInterlinear.AccessibleName = name+".AccessibleName";
			tpInterlinear.RightToLeft = System.Windows.Forms.RightToLeft.Inherit;
			tpInterlinear.Name = name;
		}

		private void MakeRawTextPane()
		{
			//
			//  Finish defining m_tpRawText.
			//
			m_rtPane = new RawTextPane();
			m_rtPane.Name = "m_rtPane";
			m_rtPane.StyleSheet = m_styleSheet;
			m_rtPane.Dock = DockStyle.Fill;
			m_rtPane.Cache = Cache;
			m_rtPane.Init(m_mediator, m_configurationParameters);
			bool fEditable = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParameters, "editable", true);
			if (!fEditable)
				m_tpRawText.ToolTipText = String.Format(ITextStrings.ksBaseLineNotEditable);
			m_rtPane.SetRoot(m_hvoStText);
		}

		private void MakePrintViewPane()
		{
			if (m_printViewPane != null)
				return;
			m_printViewPane = new InterlinPrintChild();
			m_printViewPane.Name = "m_printViewPane";
			m_printViewPane.ForEditing = false;
			m_printViewPane.Dock = DockStyle.Fill;
			m_printViewPane.BackColor = Color.FromKnownColor(KnownColor.Window);
			this.SetStyleSheetFor(m_printViewPane);

			m_printViewPane.Visible = false;
			m_panelPrintView.Controls.Add(m_printViewPane);

			// If these don't happen now they will happen later in SetupDataContext.
			if (Cache != null)
				m_printViewPane.Cache = Cache;
			if (m_mediator != null)
				m_printViewPane.Init(m_mediator, m_configurationParameters);
		}

		private void MakeTaggingPane()
		{
			if (m_taggingViewPane != null)
				return;
			m_taggingViewPane = new InterlinTaggingChild();
			m_taggingViewPane.Name = "m_taggingViewPane";
			m_taggingViewPane.ForEditing = false;
			m_taggingViewPane.Dock = DockStyle.Fill;
			m_taggingViewPane.BackColor = Color.FromKnownColor(KnownColor.Window);
			this.SetStyleSheetFor(m_taggingViewPane);

			m_taggingViewPane.Visible = false;
			m_panelTagging.Controls.Add(m_taggingViewPane);

			// If these don't happen now they will happen later in SetupDataContext.
			if (Cache != null)
				m_taggingViewPane.Cache = Cache;
			if (m_mediator != null)
				m_taggingViewPane.Init(m_mediator, m_configurationParameters);
		}

		private void MakeTitleContentsPane()
		{
			m_tcPane = new TitleContentsPane();
			m_tcPane.Dock = System.Windows.Forms.DockStyle.Top;
			// 55 holds two lines plus borders; should really compute from contents.
			m_tcPane.Height = 55; // tentative estimate usually correct.
			m_tcPane.StyleSheet = m_styleSheet;
		}

		private void GetStyleSheetFromForm()
		{
			IMainWindowDelegatedFunctions containingForm = this.FindForm() as IMainWindowDelegatedFunctions;
			if (containingForm != null)
				m_styleSheet = containingForm.StyleSheet;
		}

		#region IxCoreCtrlTabProvider implementation

		public override Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			Control ctrlHasFocus = this;
			if (m_tcPane != null && m_tcPane.Visible && !m_tcPane.ReadOnlyView)
			{
				targetCandidates.Add(m_tcPane);
				if (m_tcPane.ContainsFocus)
					ctrlHasFocus = m_tcPane;
			}
			if (m_tabCtrl != null)
			{
				if (m_tabCtrl.ContainsFocus)
					ctrlHasFocus = m_tabCtrl;
				targetCandidates.Add(m_tabCtrl);
			}
			return ContainsFocus ? ctrlHasFocus : null;
		}

		#endregion  IxCoreCtrlTabProvider implementation

		private void m_tabCtrl_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// Switching tabs, usual precaution against being able to Undo things you can no longer see.
			// When we do this because we're switching objects because we deleted one, and the new
			// object is empty, we're inside the ShowRecord where it is suppressed.
			// If the tool is just starting up (in Init, before InitBase is called), we may not
			// have configuration parameters. Usually, then, there won't be a transaction open,
			// but play safe, because the call to get the Clerk will crash if we don't have configuration
			// params.
			if (m_configurationParameters != null && !Cache.DatabaseAccessor.IsTransactionOpen())
				Clerk.SaveOnChangeRecord();
			bool fParsedTextDuringSave = false;
			// Pane-individual updates
			if (m_idcPane != null &&
				(m_tabIndex == ktpsAnalyze || m_tabIndex == ktpsGloss))
			{
				// save any edits in the interlinear pane.
				m_idcPane.UpdateRealFromSandbox();
				// explicitly set Visibility to false, so that FocusBox will not become prematurely visible
				// when we re-enter InterlinearMode, otherwise FocusBox.OnVisibleChanged may try to run logic based upon
				// invalid annotation information (LT-6555).
				m_idcPane.Visible = false;
			}
			if (m_bookmark != null) // This is out here to save bookmarks set in Chart, Print and Edit views too.
			{
				m_bookmark.Save();
				fParsedTextDuringSave = this.ParsedDuringSave;
			}
			InterlinearTab = (TabPageSelection)m_tabCtrl.SelectedIndex;
			if (IsPersistedForAnInterlinearTabPage && fParsedTextDuringSave)
				m_fSkipNextParse = true;
			m_tabIndex = m_tabCtrl.SelectedIndex;	// save the new state.
			// If we're just starting up (setting it from saved state) we don't need to do anything to change it.
			if (m_rtPane != null || m_idcPane != null || m_constChartPane != null || m_infoPane != null
				|| m_taggingViewPane != null || m_printViewPane != null)
			{
				// In order to prevent crashes caused by PropChanges affecting non-visible tabs (e.g. Undo/Redo, LT-9078),
				// dispose of any existing panes including interlinDoc child based controls,
				// and recreate them as needed. Most of the time is spend Reconstructing a display,
				// not initializing the controls.
				// NOTE: (EricP) tried to dispose of the RawTextPane as well, but for some reason
				// we'd lose our cursor when switching texts or from a different tab. Instead,
				// just dispose of the interlinDocChild panes, since those are the ones that
				// are likely to crash during an intermediate state during Undo/Redo.
				DisposeInterlinDocPanes();
				ClearInterlinDocPaneVariables();
				// don't want to re-enter ShowMainView if we're changing the index from there.
				if (!m_fInShowMainView)
					ShowMainView();
			}
			m_fSkipNextParse = false;
		}

		/// <summary>
		/// Sets m_bookmark to what is currently selected and persists it.
		/// </summary>
		internal void SaveBookMark()
		{
			CheckDisposed();

			if (m_tabIndex == ktpsInfo) // Info tab shouldn't call this. Go away!
				return;
			// No text active, so nothing to save here, just reset.
			if (m_hvoStText <= 0)
			{
				m_bookmark.Reset();
				return;
			}
			int hvoAnn = 0;
			switch (m_tabIndex)
			{
				case ktpsAnalyze:
				case ktpsGloss:
					if (m_idcPane == null) // Can this really happen? Perhaps if !m_fullyinitialized?
					{
						if (m_rtPane != null) // Not the one, but the other? Odd.
						{
							if (SaveBookmarkFromRootBox(m_rtPane.RootBox))
								return;
						}
					} else
						hvoAnn = m_idcPane.AnnotationHvoClosestToSelection();
					break;

				case ktpsCChart:
					if (m_constChartPane == null)
						return; // e.g., right after creating a new database, when previous one was open in chart pane.
					// Call CChart.GetUnchartedAnnForBookmark() by reflection
					System.Type type = m_constChartPane.GetType();
					MethodInfo info = type.GetMethod("GetUnchartedAnnForBookmark");
					Debug.Assert(info != null);
					hvoAnn = (int)info.Invoke(m_constChartPane, null);
					if (hvoAnn < 1) // This result means the Chart doesn't want to save a bookmark
						return;
					break;

				case ktpsTagging:
					if (m_taggingViewPane != null)
						hvoAnn = m_taggingViewPane.AnnotationContainingSelection();
					break;

				case ktpsPrint:
					if (m_printViewPane != null)
						hvoAnn = m_printViewPane.AnnotationContainingSelection();
					break;

				case ktpsRawText:
					// Find the annotation we were working on.
					if (m_rtPane != null)
					{
						if (SaveBookmarkFromRootBox(m_rtPane.RootBox))
							return;
					}
					break;

				default:
					Debug.Fail("Unhandled tab index.");
					break;
			}

			if (hvoAnn == 0 && !m_fullyInitialized)
			{
				// See if we can get our annotation from the record clerk.
				if (Clerk.CurrentObject != null)
				{
					int hvo = Clerk.CurrentObject.Hvo;
					int clsid = Cache.GetClassOfObject(hvo);
					if (clsid == CmBaseAnnotation.kClassId)
						hvoAnn = hvo;
				}
			}
			m_bookmark.Save(hvoAnn, true);
		}

		private bool SaveBookmarkFromRootBox(IVwRootBox rb)
		{
			if (rb == null || rb.Selection == null)
				return false;
			// There may be pictures in the text, and the selection may be on a picture or its
			// caption.  Therefore, getting the TextSelInfo is not enough.  See LT-7906.
			// Unfortunately, the bookmark for a picture or its caption can only put the user
			// back in the same paragraph, it can't fully reestablish the exact same position.
			int iPara = -1;
			SelectionHelper helper = SelectionHelper.GetSelectionInfo(rb.Selection, rb.Site);
			int ichAnchor = helper.IchAnchor;
			int ichEnd = helper.IchEnd;
			int hvoParaAnchor = 0, hvoParaEnd = 0;
			SelLevInfo[] sliAnchor = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			SelLevInfo[] sliEnd = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			if (sliAnchor.Length != sliEnd.Length)
				ichEnd = ichAnchor;
			for (int i = 0; i < sliAnchor.Length; ++i)
			{
				if (sliAnchor[i].tag == (int)StText.StTextTags.kflidParagraphs)
				{
					hvoParaAnchor = sliAnchor[i].hvo;
					break;
				}
			}
			for (int i = 0; i < sliEnd.Length; ++i)
			{
				if (sliEnd[i].tag == (int)StText.StTextTags.kflidParagraphs)
				{
					hvoParaEnd = sliEnd[i].hvo;
					break;
				}
			}
			if (hvoParaAnchor != 0)
			{
				iPara = Cache.GetObjIndex(Cache.GetOwnerOfObject(hvoParaAnchor),
					(int)StText.StTextTags.kflidParagraphs, hvoParaAnchor);
				if (hvoParaAnchor != hvoParaEnd)
					ichEnd = ichAnchor;
				if (ichAnchor == -1)
					ichAnchor = 0;
				if (ichEnd == -1)
					ichEnd = 0;
			}
			if (iPara >= 0)
			{
				m_bookmark.Save(iPara, Math.Min(ichAnchor, ichEnd), Math.Max(ichAnchor, ichEnd), true);
				return true;
			}
			else
			{
				return false;
			}
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (m_styleSheet == null)
				return;		// cannot display properly without style sheet, so don't try.
			base.OnLayout (levent);
			if (m_tcPane != null)
			{
				// we adjust the height of the title pane after we layout, so that it can use the correct width
				if (m_tcPane.AdjustHeight())
					// if the title pane changed height, we need to relayout
					base.OnLayout(levent);
			}
			if (m_idcPane != null && IsPersistedForAnInterlinearTabPage && m_idcPane.ExistingFocusBox != null)
			{
				// (LT-3836) move the FocusBox to its appropriate place after the layout has changed.
				// it seems more stable to do the adjustment here, rather than
				// in InterlinDocChild, because it can get multiple OnSizeChanges
				// and OnLayout() calls, and the views PrepareToDraw code seems to
				// get confused when we call it multiple times during those events.
				// see crash LT-5932.
				m_idcPane.MoveFocusBoxIntoPlace();
			}
		}

		bool m_fNeedShowAddWordsToLexiconDlgAfterInitialization = false;
		private void ShowAddWordsToLexiconDlg()
		{
			// don't try to show the dialog if we're not completely setup yet.
			if (!m_fullyInitialized)
			{
				// indicate we may still to bring this dialog up at a later time.
				m_fNeedShowAddWordsToLexiconDlgAfterInitialization = true;
				return;
			}
			// show the dialog if we're in the gloss tab and the user hasn't already clicked 'OK' on the dialog.
			if (CanShowAddWordsToLexiconDlg())
			{
				// first check to see that we want to show this dialog.
				bool fShowDlg = m_mediator.PropertyTable.GetBoolProperty(ksPropertyShowAddWordsToLexiconDlg, true);
				if (fShowDlg)
				{
					using (AddWordsToLexiconDlg dlg = new AddWordsToLexiconDlg())
					{
						dlg.UserWantsToAddWordsToLexicon = InModeForAddingGlossedWordsToLexicon;
						// if dialog returns OK, then set the AddWordsToLexicon property
						// and indicate that we don't want to show this dialog anymore.
						if (dlg.ShowDialog(this) == DialogResult.OK)
						{
							m_mediator.PropertyTable.SetProperty(ksPropertyAddWordsToLexicon, dlg.UserWantsToAddWordsToLexicon);
							// the user only needs to see this dialog until they hit 'OK'
							m_mediator.PropertyTable.SetProperty(ksPropertyShowAddWordsToLexiconDlg, false);
						}
					}
				}
			}
		}

		/// <summary>
		/// indicate when the app window has finished setting itself up.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnIdle(object argument)
		{
			CheckDisposed();
			if (m_fNeedShowAddWordsToLexiconDlgAfterInitialization)
			{
				// we handled this.
				m_fNeedShowAddWordsToLexiconDlgAfterInitialization = false;
				ShowAddWordsToLexiconDlg();
			}
			return false;
		}

		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch (name)
			{
				case ksPropertyAddWordsToLexicon:
					if (m_idcPane != null && m_idcPane.LineChoices != null)
					{
						// whenever we change this mode, we may also
						// need to show the proper line choice labels, so put the lineChoices in the right mode.
						InterlinLineChoices.InterlinMode newMode = GetLineMode();
						if (m_idcPane.LineChoices.Mode != newMode)
						{
							m_idcPane.TryHideFocusBox();
							m_idcPane.LineChoices.Mode = newMode;
							// the following reconstruct will destroy any valid selection (e.g. in Free line).
							// is there anyway to do a less drastic refresh (e.g. via PropChanged?)
							// that properly adjusts things?
							m_idcPane.ReconstructAndRecreateSandbox(false);
						}
					}
					break;
				case "InterlinearTab":
					if (m_tabCtrl.SelectedIndex != (int)InterlinearTab)
						ShowMainView();
					break;
				case "ShowMorphBundles":
					// This helps make sure the notification gets through even if the pane isn't
					// in focus (maybe the Sandbox or TC pane is) and so isn't an xCore target.
					if (m_idcPane != null)
						m_idcPane.OnPropertyChanged(name);
					break;
			}
		}

		void MakeInterlinPane()
		{
			if (m_idcPane != null)
				return;
			m_idcPane = new InterlinDocChild();
			m_idcPane.Name = "m_idcPane";
			m_idcPane.ForEditing = true;
			m_idcPane.Dock = DockStyle.Fill;
			m_idcPane.BackColor = Color.FromKnownColor(KnownColor.Window);
			this.SetStyleSheetFor(m_idcPane);
			// This isn't adequate now we're saving the active pane, this routine gets called before
			// we have our style sheet.
			//m_idcPane.StyleSheet = m_styleSheet;
			m_idcPane.AnnnotationSelected += new AnnotationSelectedEventHandler(
				m_idcPane_AnnnotationSelected);
			m_idcPane.Visible = false;
			m_panelInterlin.Controls.Add(m_idcPane);
			// If these don't happen now they will happen later in SetupDataContext.
			if (Cache != null)
				m_idcPane.Cache = Cache;
			if (m_mediator != null)
				m_idcPane.Init(m_mediator, m_configurationParameters);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				SuspendLayout();	// don't want do trigger OnLayout() when removing controls!
				DestroyTitleContentsPane();
				if (m_tabCtrl != null)
					m_tabCtrl.SelectedIndexChanged -= new System.EventHandler( m_tabCtrl_SelectedIndexChanged);
				DisposeInterlinDocPanes();
				DisposeIfParentNull(m_panelInterlin);
				DisposeIfParentNull(m_rtPane);
				DisposeIfParentNull(m_infoPane);

				if(components != null)
				{
					components.Dispose();
				}
				// LT-5702
				// The Find / Replace dlg can currently only exist in this view, so
				// remove it when the view changes.  This will have to be expanded
				// when the dlg can search and operate on more than one view in Flex
				// as it does in TE.
				if (FwApp.App != null)
					FwApp.App.RemoveFindReplaceDialog();
			}

			m_tcPane = null;
			m_infoPane = null;
			m_rtPane = null;
			m_constChartPane = null;
			ClearInterlinDocPaneVariables();
			m_panelInterlin = null;
			m_panelTagging = null;
			m_panelPrintView = null;
			m_bookmark = null;

			base.Dispose( disposing );
		}

		/// <summary>
		/// Dispose of panes using InterlinDocView based controls when switching tabs, to
		/// avoid hidden panes trying to handle PropChanges (e.g. Undo/Redo) that can lead to crash (LT-9078)
		/// </summary>
		private void DisposeInterlinDocPanes()
		{
			if (m_idcPane != null) // InterlinDocChild
			{
				m_idcPane.AnnnotationSelected -= new AnnotationSelectedEventHandler(m_idcPane_AnnnotationSelected);
				RemoveFromContainerDispose(m_idcPane, m_panelInterlin);
			}
			RemoveFromContainerDispose(m_taggingViewPane, m_panelTagging);
			RemoveFromContainerDispose(m_printViewPane, m_panelPrintView);

			DisposeIfParentNull(m_constChartPane);

			// now make sure future InterlinDocChild based controls
			// also get removed/disposed and cleared.
			DebugAssertAllInterlinDocChildControlsDisposed(this.Controls);
		}

		/// <summary>
		/// If you get caught by this DebugAssert,
		/// also make sure any member variable gets cleared by ClearInterlinDocPaneVariables()
		/// </summary>
		/// <param name="collection"></param>
		private void DebugAssertAllInterlinDocChildControlsDisposed(ControlCollection collection)
		{
			foreach (Control c in collection)
			{
				Debug.Assert(!(c is InterlinDocChild || c is InterlinDocView),
							 String.Format("We can crash if we don't remove InterlinDoc controls ({0}) that can receive " +
										   "PropChanges during Undo/Redo(LT-9078)", c.Name));
				DebugAssertAllInterlinDocChildControlsDisposed(c.Controls);
			}
		}

		private void ClearInterlinDocPaneVariables()
		{
			m_idcPane = null;
			m_taggingViewPane = null;
			m_printViewPane = null;
		}

		private void RemoveFromContainerDispose(Control item, Control holder)
		{
			if (item != null)
			{
				if (holder != null)
					holder.Controls.Remove(item);
				item.Dispose();
			}
		}

		private void DisposeIfParentNull(Control con)
		{
			if (con != null && con.Parent == null)
				con.Dispose();
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// InterlinMaster
			//
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigureInterlinDialog));
			Name = "InterlinMaster";
			//Size = new System.Drawing.Size(448, 248);
			resources.ApplyResources(this, "$this");
		}
		#endregion

		/// <summary>
		/// Enable if there's anything to select.  This is needed so that the toolbar button is
		/// disabled when there's nothing to look up.  Otherwise, crashes can result when it's
		/// clicked but there's nothing there to process!  It's misleading to the user if
		/// nothing else.  We leave the button visible so that the user doesn't get nauseated
		/// from the buttons appearing and disappearing rapidly.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true</returns>
		public bool OnDisplayLexiconLookup(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = true;
			if (m_tabCtrl.SelectedIndex != ktpsRawText)
				display.Enabled = false;
			else
			{
				//LT-6904 : exposed this case where the m_rtPane was null
				// (another case of toolbar processing being done at an unepxected time)
				if (m_rtPane == null)
					display.Enabled = false;
				else
					display.Enabled = m_rtPane.LexiconLookupEnabled();
			}
			return true;
		}

		internal int RootHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoStText;
			}
		}

		internal int TextListFlid
		{
			get
			{
				CheckDisposed();
				return Clerk.VirtualFlid;
			}
		}

		internal int TextListIndex
		{
			get
			{
				CheckDisposed();
				return Clerk.CurrentIndex;
			}
		}

		bool m_fInShowMainView = false;
		protected void ShowMainView()
		{
			m_fInShowMainView = true;
			try
			{
				m_tabCtrl.SelectedIndex = (int) InterlinearTab; // set the persisted tab setting.
				RefreshPaneBar();
				// JohnT: don't want to switch mode just because concordance selects no text.
				//			if (m_hvoStText == 0)
				//				IsPersistedForAnInterlinearTabPage = false;
				bool fDidParse = m_fSkipNextParse;
				if ((InterlinearTab != TabPageSelection.RawText &&
					 InterlinearTab != TabPageSelection.Info)
					&& m_hvoStText != 0 && !m_fSkipNextParse)
				{
					if (!ParseText(out fDidParse))
					{
						// Didn't get anything...switch to raw (probably empty text).
						m_tabCtrl.SelectedIndex = ktpsRawText;
					}
					// currently we assume that if we had something to parse, then the rootbox
					// is invalid and needs to be resync'd. In the future, we'll want to
					// base this decision more exactly on whether the text has actually been
					// updated since the last time we set the root.
					// We need to do this even if we're not switching to that pane now, because
					// when we later do, we may not need to reparse, but it could still be remembering.
					if (m_idcPane != null)
						m_idcPane.InvalidateRootBox();
					if (m_taggingViewPane != null)
						m_taggingViewPane.InvalidateRootBox();
					if (m_printViewPane != null)
						m_printViewPane.InvalidateRootBox();
				}

				if (IsPersistedForAnInterlinearTabPage && InterlinearTabPageIsSelected())
				{
#if DEBUG
					//TimeRecorder.Begin("switch to interlin view");
#endif
					this.SuspendLayout();
#if DEBUG
					//TimeRecorder.Begin("MakeInterlinPane");
#endif
					MakeInterlinPane();
#if DEBUG
					//TimeRecorder.End("MakeInterlinPane");
					//TimeRecorder.Begin("ParseTextM");
					//TimeRecorder.End("ParseTextM");
					//TimeRecorder.Begin("Add and front");
#endif
					// Pass the interlinear document to which ever tab wants to use it.
					// (Controls.Add() may seem a little weird, but
					// .NET does automatically remove the control from a previous parent)
					if (m_tabCtrl.SelectedIndex == ktpsGloss)
						m_tpGloss.Controls.Add(m_panelInterlin);
					else if (m_tabCtrl.SelectedIndex == ktpsAnalyze)
						m_tpInterlinear.Controls.Add(m_panelInterlin);
					// The only current difference in functionality between the interliear tabs
					// (Gloss and Analyze) is the interlinear lines configured for the document.
					// So pass that (context sensitive) property to the interlinear doc control.
					m_idcPane.ConfigPropName = this.ConfigPropName;
					m_panelInterlin.BringToFront();
#if DEBUG
					//TimeRecorder.End("Add and front");
					//TimeRecorder.Begin("ResumeLayout");
#endif
					this.ResumeLayout();
#if DEBUG
					//TimeRecorder.End("ResumeLayout");
					//TimeRecorder.Begin("SetRoot");
#endif

					// Somewhat obsolete comment? (JohnT 1 Jan 2008: it says to do the SetRoot AFTER
					// making visible, but we don't.
					// This causes a reconstruct if the root box has been constructed, forcing it to
					// reload everything to be consistent with the results of the reparse.  At the
					// time of writing, we can't use Reconstruct for this, because it fails if the
					// root box has not yet been constructed.  A fix for this has already been made
					// in another branch, I believe.  We need to do this AFTER the pane has been
					// made visible, so that a DoUpdates call can really draw it, in order to get
					// data loaded during SetRoot.

					// This ensures that its root has been constructed and it's in a valid state
					// for things like setting an annotation and making the focus box and scrolling
					// to show it. Also that all layout that happens in the process happens at the
					// correct width (and height...this helps us position the focus box sensibly).
					if (m_idcPane.Width != m_tabCtrl.SelectedTab.Width)
						m_idcPane.Width = m_tabCtrl.SelectedTab.Width;
					if (m_idcPane.Height != m_tabCtrl.SelectedTab.Height)
						m_idcPane.Height = m_tabCtrl.SelectedTab.Height;
					// SetupLineChoices is resets the interlin doc control to display using line choices
					// appropriate to the tab (Gloss or Analyze).
					SetupLineChoices();
					m_idcPane.SetRoot(m_hvoStText);
					SelectAnnotation();
					UpdateContextHistory();
					m_idcPane.Visible = true;
					ShowAddWordsToLexiconDlg();
					return;
#if DEBUG
					//TimeRecorder.End("SetRoot");
					//TimeRecorder.Begin("Paint");
#endif
					//m_idcPane.Update();
#if DEBUG
					//TimeRecorder.End("Paint");
					//TimeRecorder.End("switch to interlin view");
					//TimeRecorder.Report();
#endif
				}
				else if (m_tabCtrl.SelectedIndex == ktpsTagging)
				{
					this.SuspendLayout();
					if (m_taggingViewPane == null)
					{
						MakeTaggingPane();
					}
					SetPaneSizeAndRoot(m_taggingViewPane);
				}
				else if (m_tabCtrl.SelectedIndex == ktpsPrint)
				{
					this.SuspendLayout();
					if (m_printViewPane == null)
					{
						MakePrintViewPane();
					}
					SetPaneSizeAndRoot(m_printViewPane);
				}
				else if (m_tabCtrl.SelectedIndex == ktpsRawText)
				{
					if (m_rtPane == null)
					{
						MakeRawTextPane();
					}
					if (!this.m_tpRawText.Controls.Contains(m_rtPane))
					{
						this.SuspendLayout();
						this.m_tpRawText.Controls.Add(m_rtPane);
						this.ResumeLayout();
					}
					m_rtPane.Focus();
					// Creating the selection must be done after setting the focus on the raw text
					// pane.  Otherwise, the writing system combobox in the toolbar is not updated
					// (and neither is the keyboard).  See the later comments in LT-6692.
					if (m_rtPane.RootBox != null && m_rtPane.RootBox.Selection == null && m_hvoStText != 0)
						m_rtPane.RootBox.MakeSimpleSel(true, false, false, true);
				}
				else if (InterlinearTabPageIsSelected())
				{
					// for some reason, the property table doesn't have our selected index
					// so go back to RawText tab page.
					m_tabCtrl.SelectedIndex = ktpsRawText;
					return;
				}
				else if (m_tabCtrl.SelectedIndex == ktpsCChart)
				{
					if (m_constChartPane == null)
					{
						m_constChartPane = (UserControl) DynamicLoader.CreateObject("Discourse.dll",
																					"SIL.FieldWorks.Discourse.ConstituentChart",
																					new object[] {Cache});
						(m_constChartPane as IxCoreColleague).Init(m_mediator, m_configurationParameters);
						m_constChartPane.Dock = DockStyle.Fill;
						m_tpCChart.Controls.Add(m_constChartPane);
						if (m_styleSheet != null)
							SetStyleSheetForConstChart();
					}
					if (m_hvoStText == 0)
						m_constChartPane.Enabled = false;
					else
					{
						// LT-7733 Warning dialog for Text Chart
						XCore.XMessageBoxExManager.Trigger("TextChartNewFeature");
						m_constChartPane.Enabled = true;
					}
					SetConstChartRoot(m_hvoStText);
					m_constChartPane.Focus();
				}
				else if (m_tabCtrl.SelectedIndex == ktpsInfo)
				{
					if (m_infoPane == null)
					{
						m_infoPane = new InfoPane(Cache, m_mediator, Clerk);
						m_infoPane.Dock = DockStyle.Fill;
						m_tpInfo.Controls.Add(m_infoPane);
					}
					m_infoPane.Enabled = (m_hvoStText != 0);
					if (m_infoPane.Enabled)
					{
						m_infoPane.BackColor = System.Drawing.SystemColors.Control;
						m_infoPane.Focus();
					}
					else
					{
						m_infoPane.BackColor = System.Drawing.Color.White;
					}
				}
				SelectAnnotation();
				UpdateContextHistory();
			}
			finally
			{
				m_fInShowMainView = false;
			}
		}

		private void SetPaneSizeAndRoot(InterlinDocChild pane)
		{
			this.ResumeLayout();

			// This ensures that its root has been constructed and it's in a valid state
			// for things like setting an annotation and making the focus box and scrolling
			// to show it. Also that all layout that happens in the process happens at the
			// correct width (and height...this helps us position the focus box sensibly).
			if (pane.Width != m_tabCtrl.SelectedTab.Width)
				pane.Width = m_tabCtrl.SelectedTab.Width;
			if (pane.Height != m_tabCtrl.SelectedTab.Height)
				pane.Height = m_tabCtrl.SelectedTab.Height;

			// If the suspendLayout was not used then there were sometimes significant delays
			// when switching from other tabs to the PrintView tab.
			pane.SuspendLayout();
			pane.SetRoot(m_hvoStText);
			pane.ResumeLayout();

			pane.Visible = true;
		}

		private void SetStyleSheetForConstChart()
		{
			if (m_constChartPane != null)
			{
				PropertyInfo pi = m_constChartPane.GetType().GetProperty("StyleSheet",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				pi.SetValue(m_constChartPane, m_styleSheet, null);
			}
		}

		private void RefreshPaneBar()
		{
			// if we're in the context of a PaneBar, refresh the bar so the menu items will
			// reflect the current tab.
			if (this.Parent != null && this.Parent is PaneBarContainer)
				(this.Parent as PaneBarContainer).RefreshPaneBar();
		}

		internal static bool ParseText(FdoCache cache, int hvoStText, Mediator mediator, out bool fDidParse)
		{
			using (ProgressState progress = FwXWindow.CreateMilestoneProgressState(mediator))
			{
				return ParagraphParser.ParseText(new StText(cache, hvoStText), progress, out fDidParse);
			}
		}

		private bool ParseText(out bool fDidParse)
		{
			return ParseText(Cache, m_hvoStText, m_mediator, out fDidParse);
		}

		/// <summary>
		/// Required override for RecordView subclass.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public override void Init(XCore.Mediator mediator,
			System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

			// Do this BEFORE calling InitBase, which calls ShowRecord, whose correct behavior
			// depends on the suppressAutoCreate flag.
			bool fHideTitlePane = XmlUtils.GetBooleanAttributeValue(configurationParameters, "hideTitleContents");
			if (fHideTitlePane)
			{
				// When used as the third pane of a concordance, we don't want the
				// title/contents stuff.
				DestroyTitleContentsPane();
			}
			m_fSuppressAutoCreate = XmlUtils.GetBooleanAttributeValue(configurationParameters,
				"suppressAutoCreate");

			// InitBase will do this, but we need it in place for testing IsPersistedForAnInterlinearTabPage.
			m_mediator = mediator;

			// Making the tab control currently requires this first...
			if (!fHideTitlePane)
				MakeTitleContentsPane();
			// We need to create this so we can set the tab index.
			MakeTabControlAndPages();
			// Set the appropriate tab index BEFORE calling InitBase, since that calls
			// RecordView.InitBase, which calls ShowRecord, which calls ShowMainRecord,
			// which will unnecessarily create the wrong pane, if the tab index is wrong.
			m_tabIndex = m_tabCtrl.SelectedIndex;	// save initial tab index
			// If the Record Clerk has remembered we're IsPersistedForAnInterlinearTabPage,
			// and we haven't already switched to that tab page, do so now.
			if (this.Visible && m_tabCtrl.SelectedIndex != (int)InterlinearTab)
			{
				// Switch to the persisted tab page index.
				m_tabCtrl.SelectedIndex = (int)InterlinearTab;
			}
			// Do NOT do this, it raises an exception.
			//base.Init (mediator, configurationParameters);
			// Instead do this.
			InitBase(mediator, configurationParameters);
			m_fullyInitialized = true;
			RefreshPaneBar();
		}

		private void DestroyTitleContentsPane()
		{
			if (m_tcPane != null)
			{
				Controls.Remove(m_tcPane);
				m_tcPane.Dispose();
				m_tcPane = null;
			}
		}

		/// <summary>
		/// From IxCoreContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public override bool PrepareToGoAway()
		{
			CheckDisposed();

			// let's save our position before going further. Otherwise we might loose our annotation/analysis information altogether.
			// even if PrepareToGoAway creates a new analysis or annotation, it still should be at the same location.
			this.SuspendLayout();

			//LT-6904 : exposed this case where the m_bookmark is null
			if (m_bookmark != null)
				m_bookmark.Save();
			if (m_idcPane != null && !m_idcPane.PrepareToGoAway())
				return false;
			if (!base.PrepareToGoAway())
				return false;
			this.Visible = false;
			return true;
		}

		public bool OnPrepareToRefresh(object args)
		{
			CheckDisposed();

			// flag that a refresh was triggered.
			m_fRefreshOccurred = true;
			(Cache.LangProject.WordformInventoryOA as IDummy).OnPrepareToRefresh(args);
			return false; // other things may wish to prepare too.
		}

		/// <summary>
		/// Determine if this is the correct place for handling the 'New Text' command.
		/// NOTE: in PrepareToGoAway(), the mediator may have been switched to a new area.
		/// </summary>
		internal bool InTextsArea
		{
			get
			{
				CheckDisposed();

				string desiredArea = "textsWords";
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice",
					null);
				if (areaChoice != null && areaChoice == desiredArea)
					return true;
				else
					return false;
			}
		}

		protected override void SetupDataContext()
		{
			base.SetupDataContext ();
			if (m_tcPane != null)
			{
				m_tcPane.Cache = Cache;
				// Init it as xCoreColleague.
				m_tcPane.Init(m_mediator, m_configurationParameters);
			}
			if (m_idcPane != null)
			{
				// If these don't happen now they will happen later in MakeInterlinPane().
				m_idcPane.Cache = Cache;
				m_idcPane.Init(m_mediator, m_configurationParameters);
			}
			if (m_rtPane != null)
			{
				m_rtPane.Cache = Cache;
				m_rtPane.Init(m_mediator, m_configurationParameters);
			}
			if (m_taggingViewPane != null)
			{
				m_taggingViewPane.Cache = Cache;
				m_taggingViewPane.Init(m_mediator, m_configurationParameters);
			}
			if (m_printViewPane != null)
			{
				m_printViewPane.Cache = Cache;
				m_printViewPane.Init(m_mediator, m_configurationParameters);
			}
		}

		/// <summary>
		/// The record index of the currently selected text.
		/// </summary>
		internal int IndexOfTextRecord
		{
			get
			{
				CheckDisposed();

				if (Clerk.CurrentObject != null)
				{
					int hvoRoot = Clerk.CurrentObject.Hvo;
					int clsid = Cache.GetClassOfObject(hvoRoot);
					if (Cache.GetClassOfObject(hvoRoot) == FDO.Cellar.StText.kclsidStText)
						return Clerk.CurrentIndex;
				}

				return -1;
			}
		}

		internal string TitleOfTextRecord
		{
			get
			{
				CheckDisposed();

				if (Clerk.CurrentObject != null)
				{
					int hvoRoot = Clerk.CurrentObject.Hvo;
					int clsid = Cache.GetClassOfObject(hvoRoot);
					if (Cache.GetClassOfObject(hvoRoot) == FDO.Ling.Text.kclsidText)
					{
						FDO.IText text = new Text(Cache, hvoRoot);
						return text.Name.AnalysisDefaultWritingSystem;
					}
				}
				return string.Empty;
			}
		}

		protected override void ShowRecord(RecordNavigationInfo rni)
		{
			base.ShowRecord(rni);

			// independent of whether base.ShowRecord(rni) skips ShowRecord()
			// we still want to try to put the focus in our control.
			if (InterlinearTabPageIsSelected())
				m_idcPane.Focus();
			else
				this.Focus();
		}

		protected override void ShowRecord()
		{
			base.ShowRecord ();
			if (Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
				return;
			if (m_bookmark == null)
				m_bookmark = new InterAreaBookmark(this, m_mediator, Cache);
			if (IsPersistedForAnInterlinearTabPage) // can be true from work in another instance
				MakeInterlinPane(); // does nothing if already made.
			// It's important not to do this if there is a filter, as there's a good chance the new
			// record doesn't pass the filter and we get into an infinite loop. Also, if the user
			// is filtering, he probably just wants to see that there are no matching texts, not
			// make a new one.
			if (Clerk.CurrentObject == null && !m_fSuppressAutoCreate && !Clerk.ShouldNotModifyList
				&& Clerk.Filter == null)
			{
				// first clear the views of their knowledge of the previous text.
				// otherwise they could crash trying to access information that is no longer valid. (LT-10024)
				SwitchText(0);

				// Presumably because there are none..make one.
				// This is invisible to the user so it should not be undoable; that is particularly
				// important if the most recent action was to delete the last text, which will
				// not be undoable if we are now showing 'Undo insert text'.
				using (new SuppressSubTasks(Cache))
				{
					bool fWasSuppressed = Clerk.SuppressSaveOnChangeRecord;
					try
					{
						// We don't want to force a Save here if we just deleted the last text;
						// we want to be able to Undo deleting it!
						Clerk.SuppressSaveOnChangeRecord = true;
						Clerk.InsertItemInVector("StText");
					}
					finally
					{
						Clerk.SuppressSaveOnChangeRecord = fWasSuppressed;
					}
				}
			}
			if (Clerk.CurrentObject == null)
			{
				SwitchText(0);		// We no longer have a text.
				return;				// We get another call when there is one.
			}
			int hvoRoot = Clerk.CurrentObject.Hvo;
			int clsid = Cache.GetClassOfObject(hvoRoot);
			int hvoStText = 0;
			if (clsid == CmBaseAnnotation.kClassId)	// RecordClerk is tracking the annotation
			{
				// This pane, as well as knowing how to work with a record list of Texts, knows
				// how to work with one of CmBaseAnnotations, that is, a list of occurrences of
				// a word.
				int annHvo = hvoRoot;
				if (!m_fRefreshOccurred)
					m_bookmark.Save(annHvo, false);
				int hvoPara = Cache.MainCacheAccessor.get_ObjectProp(annHvo, kflidBeginObject);
				hvoStText = hvoRoot = Cache.GetOwnerOfObject(hvoPara);
				if (m_rtPane != null)
					m_rtPane.SetRoot(hvoRoot);
				if (m_constChartPane != null)
					SetConstChartRoot(hvoRoot);
			}
			else
			{
				//FDO.IText text = new Text(Cache, hvoRoot);
				//// If the text is empty...typically newly created...make it an StText and an
				//// empty paragraph in the right WS.
				//if (text.ContentsOA == null)
				//    text.ContentsOA = new StText();
				IStText stText = new StText(Cache, hvoRoot);
				if (stText.ParagraphsOS.Count == 0)
				{
					IStTxtPara txtPara = new StTxtPara();
					stText.ParagraphsOS.Append(txtPara);
					int wsText = (Clerk as InterlinearTextsRecordClerk).PrevTextWs;
					if (wsText != 0)
					{
						// Establish the writing system of the new text by filling its first paragraph with
						// an empty string in the proper writing system.
						if (Cache.LangProject.VernWssRC.Count > 1 && !Cache.AddAllActionsForTests)
						{
							using (ChooseTextWritingSystemDlg dlg = new ChooseTextWritingSystemDlg())
							{
								dlg.Initialize(Cache, wsText);
								dlg.ShowDialog();
								wsText = dlg.TextWs;
							}
						}
						(Clerk as InterlinearTextsRecordClerk).PrevTextWs = 0;
					}
					else
					{
						wsText = Cache.DefaultVernWs;
					}
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					Cache.MainCacheAccessor.SetString(txtPara.Hvo,
						(int)StTxtPara.StTxtParaTags.kflidContents,
						tsf.MakeString("", wsText));
					// since we have a new text, we should switch to the Baseline tab.
					// ShowMainView() will adjust the tab control appropriately.
					this.InterlinearTab = TabPageSelection.RawText;
				}

				if (m_tcPane != null)
					m_tcPane.SetRoot(hvoRoot);
				if (m_rtPane != null)
					m_rtPane.SetRoot(hvoRoot);

				if (m_hvoStText == 0)
				{
					// we've just now entered the area, so try to restore a bookmark.
					m_bookmark.Restore();
				}
				else if (m_hvoStText != hvoRoot)
				{
					// we've switched texts, so reset our bookmark.
					m_bookmark.Reset();
				}
			}

			if (m_hvoStText != hvoRoot)
			{
				SwitchText(hvoRoot);
			}
			else
				SelectAnnotation(); // select an annotation in the current text.

			// This takes a lot of time, and the view is never visible by now, and it gets done
			// again when made visible! So don't do it!
			//m_idcPane.SetRoot(hvoRoot);

			// If we're showing the raw text pane make sure it has a selection.
			if (Controls.IndexOf(m_rtPane) >= 0 && m_rtPane.RootBox.Selection == null)
				m_rtPane.RootBox.MakeSimpleSel(true, false, false, true);

			UpdateContextHistory();
			m_fRefreshOccurred = false;	// reset our flag that a refresh occurred.
		}

		private void SwitchText(int hvoRoot)
		{
			// We've switched text, so clear the Undo stack redisplay it.
			// This method will clear the Undo stack UNLESS we're changing record
			// because we inserted or deleted one, which ought to be undoable.
			Clerk.SaveOnChangeRecord();
			m_hvoStText = hvoRoot; // one way or another it's the Text by now.
			if (hvoRoot == 0)
			{
				m_bookmark.Reset();
				if (m_rtPane != null)
					m_rtPane.SetRoot(0);
				if (m_tcPane != null)
					m_tcPane.SetRoot(0);
				if (m_constChartPane != null)
					SetConstChartRoot(0);
				//if (m_printViewPane != null)
				//    m_printViewPane.SetRoot(0);
			}
			ShowMainView();
		}

		private void SetConstChartRoot(int hvoRoot)
		{
			// It's important for the chart's width to be valid, and sometimes they are not
			// when switching from another window.
			if (this.Width != m_tabCtrl.Width)
				m_tabCtrl.Width = this.Width;
			// Use reflection, we don't 'know' it has a SetRoot method.
			System.Type type = m_constChartPane.GetType();
			MethodInfo info = type.GetMethod("SetRoot");
			Debug.Assert(info != null);
			info.Invoke(m_constChartPane, new object[] { hvoRoot });
		}

		// If the Clerk's object is an annotation, select the corresponding thing in whatever pane
		// is active. Or, if we have a bookmark, restore it.
		private void SelectAnnotation()
		{
			if (Clerk.CurrentObject == null || Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
				return;
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int annHvo = 0;
			// Use a bookmark, if we've set one.
			if (m_bookmark.IndexOfParagraph >= 0)
			{
				// Interlinear and Chart SelectAnnotation() must take TWFIC, but RawTextPane can
				// also handle text segments.
				// Chart and PrintView SelectAnnotation() don't need to worry about Sandbox.
				switch (m_tabCtrl.SelectedIndex)
				{
					case ktpsAnalyze:
					case ktpsGloss:
						annHvo = HandleBookmark(m_idcPane);
						break;

					case ktpsTagging:
						annHvo = HandleBookmark(m_taggingViewPane);
						break;

					case ktpsPrint:
						annHvo = HandleBookmark(m_printViewPane);
						break;

					case ktpsCChart:
						// Call Constituent Chart SelectBookmark() by reflection
						System.Type type = m_constChartPane.GetType();
						MethodInfo info = type.GetMethod("SelectAndScrollToBookmark");
						Debug.Assert(info != null, "Couldn't find 'SelectAndScrollToBookmark()' in CChart");
						info.Invoke(m_constChartPane, new object[] { m_bookmark });
						break;

					case ktpsRawText:
						m_rtPane.SelectBookMark(m_bookmark);
						break;

					case ktpsInfo:
						break;

					default:
						Debug.Fail("Unhandled tab index.");
						break;
				}
			}
			else
			{
				// This pane, as well as knowing how to work with a record list of Texts, knows
				// how to work with one of CmBaseAnnotations, that is, a list of occurrences of
				// a word.
				int clsid = Cache.GetClassOfObject(Clerk.CurrentObject.Hvo);
				if (clsid == CmBaseAnnotation.kClassId)
					annHvo = Clerk.CurrentObject.Hvo;
			}

			if (IsPersistedForAnInterlinearTabPage && InterlinearTabPageIsSelected())
			{
				if (annHvo == 0)
				{
					// Select the first word needing attention.
					AdvanceWordArgs args = new AdvanceWordArgs(0, 0);
					m_idcPane.AdvanceWord(this, args, true);
					if (args.Annotation != 0)
						annHvo = args.Annotation;
					else
						return;		// Can't select nothing, so return.
				}

				// Try our best not to select an annotation we know won't work.
				int twficType = CmAnnotationDefn.Twfic(Cache).Hvo;
				int annoType = sda.get_ObjectProp(annHvo, kflidAnnotationType);
				int annInstanceOfRAHvo = sda.get_ObjectProp(annHvo, kflidInstanceOf);
				if (annInstanceOfRAHvo == 0 || annoType != twficType)
				{
					// if we didn't set annHvo by our marker or Clerk.CurrentObject,
					// then we must have already tried the first word.
					if (m_bookmark.IndexOfParagraph < 0 && Clerk.CurrentObject.Hvo == 0)
						return;
					// reset our marker and return to avoid trying to reuse an "orphan annotation"
					// resulting from an Undo(). (cf. LT-2663).
					m_bookmark.Reset();

					// See if we can at least select the first word.
					AdvanceWordArgs args = new AdvanceWordArgs(0, 0);
					m_idcPane.AdvanceWord(this, args, false);
					if (args.Annotation != 0)
						annHvo = args.Annotation;
					else
						return;		// Can't select nothing, so return.

					annoType = sda.get_ObjectProp(annHvo, kflidAnnotationType);
					annInstanceOfRAHvo = sda.get_ObjectProp(annHvo, kflidInstanceOf);
					// If we still haven't found a good analysis, return.
					if (annInstanceOfRAHvo == 0 || annoType != twficType)
						return;
				}
				m_idcPane.SelectAnnotation(annHvo);
			}
			else if (m_tabCtrl.SelectedIndex == ktpsRawText)
			{
				if (annHvo == 0)
					return;		// Can't select nothing, so return;

				// Select something in the RawTextPane
				m_bookmark.Save(annHvo, false);
				int hvoPara = sda.get_ObjectProp(annHvo, kflidBeginObject);
				int hvoStText = Cache.GetOwnerOfObject(hvoPara);
				m_rtPane.SelectAnnotation(hvoStText, hvoPara, annHvo);
			}
			else if (m_tabCtrl.SelectedIndex == ktpsTagging)
			{
				if (annHvo == 0)
					return;		// Can't select nothing, so return;

				TrySelectAnnotation(sda, m_taggingViewPane, annHvo);
			}
			else if (m_tabCtrl.SelectedIndex == ktpsPrint)
			{
				if (annHvo == 0)
					return;		// Can't select nothing, so return;

				TrySelectAnnotation(sda, m_printViewPane, annHvo);
			}
		}

		private void TrySelectAnnotation(ISilDataAccess sda, InterlinDocChild pane, int annHvo)
		{
			// Try our best not to select an annotation we know won't work.
			int annoType = sda.get_ObjectProp(annHvo, kflidAnnotationType);
			int annInstanceOfRAHvo = sda.get_ObjectProp(annHvo, kflidInstanceOf);
			if (annInstanceOfRAHvo == 0 || annoType != CmAnnotationDefn.Twfic(Cache).Hvo)
			{
				// if we didn't set annHvo by our marker or Clerk.CurrentObject,
				// then we must have already tried the first word.
				if (m_bookmark.IndexOfParagraph < 0 && Clerk.CurrentObject.Hvo == 0)
					return;
				// reset our marker and return to avoid trying to reuse an "orphan annotation"
				// resulting from an Undo(). (cf. LT-2663).
				m_bookmark.Reset();

				return;		// Can't select nothing, so return.
			}
			pane.SelectAnnotation(annHvo);
		}

		private int HandleBookmark(InterlinDocChild pane)
		{
			int hvoResult = 0;
			int cchPara = -2; // won't match any begin offset!
			if (m_bookmark.IndexOfParagraph == pane.RawStText.ParagraphsOS.Count - 1)
			{
				// bookmark in last paragraph, see if after last character.
				IStTxtPara para = pane.RawStText.ParagraphsOS[m_bookmark.IndexOfParagraph] as IStTxtPara;
				cchPara = para.Contents.Length;
			}
			if (m_bookmark.BeginCharOffset == m_bookmark.EndCharOffset && m_bookmark.BeginCharOffset == cchPara)
			{
				// Bookmark is an IP at the end of the text, don't try to match it.
				// If we're in Analyze or Gloss, default is to select first thing that needs annotation.
				// Otherwise, do nothing for now.
				if (m_tabCtrl.SelectedIndex == ktpsAnalyze || m_tabCtrl.SelectedIndex == ktpsGloss)
					hvoResult = SelectFirstThingNeedingAnnotation(pane);
			}
			else
			{
				// bookmark is not an IP at end of text, try to select the corresponding thing.
				hvoResult = RawTextPane.AnnotationHvo(Cache, pane.RawStText, m_bookmark, true);
			}
			return hvoResult;
		}

		private int SelectFirstThingNeedingAnnotation(InterlinDocChild pane)
		{
			int hvoAnn = 0;
			AdvanceWordArgs args = new AdvanceWordArgs(0, 0);
			pane.AdvanceWord(this, args, true);
			if (args.Annotation != 0)
				hvoAnn = args.Annotation;
			return hvoAnn;
		}

		/// <summary>
		/// Most Interlinear document logic in InterlinMaster can be shared between
		/// the these tab pages (Analyze (Interlinearizer) and Gloss).
		/// So you can use this to test whether our tab control is in one of those tabs.
		/// </summary>
		/// <returns></returns>
		internal bool InterlinearTabPageIsSelected()
		{
			return m_tabCtrl.SelectedIndex == ktpsAnalyze ||
					m_tabCtrl.SelectedIndex == ktpsGloss;
		}

		/// <summary>
		/// We can't switch the interlinear view in successfully until the window actually
		/// shows, mainly because the handles need to be created so the Sandbox has a root box
		/// and can figure its size. So when it becomes visible, if it is in the wrong mode, fix
		/// it.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged (e);
			if (this.Visible && IsPersistedForAnInterlinearTabPage && m_idcPane == null)
				ShowMainView();
		}

		/// <summary>
		/// Make the subpanes message targets, especially so the interlin doc child can enable
		/// the Insert Free Translation menu items.
		/// </summary>
		/// <returns></returns>
		protected override XCore.IxCoreColleague[] GetMessageAdditionalTargets()
		{
			// added this to list for processing the ShowMorph menu item
			if (m_tabCtrl.SelectedIndex == ktpsInfo && m_tcPane != null)
			{
				return new IxCoreColleague[] { m_tcPane, this };
			}
			else if (m_tabCtrl.SelectedIndex == ktpsTagging && m_taggingViewPane != null)
			{
				return new IxCoreColleague[] { m_taggingViewPane, this };
			}
			else if (m_tabCtrl.SelectedIndex == ktpsPrint && m_printViewPane != null)
			{	// this enabled the Print View pane to handle the Print command
				return new IxCoreColleague[] { m_printViewPane, this };
			}
			else if (m_idcPane != null && InterlinearTabPageIsSelected())
			{
				return new IxCoreColleague[] { m_idcPane, this };
			}
			else if (m_tabCtrl.SelectedIndex == ktpsRawText && m_rtPane != null)
			{
				//Debug.WriteLine("raw text pane is a colleague");
				return new IxCoreColleague[] { m_rtPane, this };
			}
			else if (m_tabCtrl.SelectedIndex == ktpsCChart && m_constChartPane is IxCoreColleague)
			{
				return new IxCoreColleague[] { m_constChartPane as IxCoreColleague, this };
			}
			// Neither active pane has focus, so return neither of them...something else should
			// receive cut/copy/paste etc.
			return new IxCoreColleague[] { this };
		}

		#region free translation stuff
		/// <summary>
		/// Enable the "Add Freeform Annotation" command if the idcPane is visible and wants to
		/// do it.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddFreeTrans(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_idcPane != null && InterlinearTabPageIsSelected())
				m_idcPane.OnDisplayAddFreeTrans(commandObject, ref display);
			else
				display.Enabled = false;
			return true;
		}

		public bool OnDisplayFindAndReplaceText(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			bool fVisible = m_rtPane != null && (m_tabCtrl.SelectedIndex == (int)TabPageSelection.RawText) && InTextsArea
				&& toolName != "wordListConcordance";
			display.Visible = fVisible;

			if (fVisible && m_rtPane.RootBox != null)
			{
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				m_rtPane.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				display.Enabled = hvoRoot != 0;
			}
			else
				display.Enabled = false;

			// Although it's a modal dialog, it's dangerous for it to be visible in contexts where it
			// could not be launched, presumably because it doesn't apply to that view, and may do
			// something dangerous to another view (cf LT-7961).
			if (!display.Enabled)
				FwApp.App.RemoveFindReplaceDialog();
			return true;
		}

		public void OnFindAndReplaceText(object argument)
		{
			CheckDisposed();

			FwApp.App.ShowFindReplaceDialog(false, m_rtPane);
		}

		/// <summary>
		/// Enable the "Add Literal Annotation" command if the idcPane is visible and wants to
		/// do it.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddLitTrans(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_idcPane != null && InterlinearTabPageIsSelected())
				m_idcPane.OnDisplayAddLitTrans(commandObject, ref display);
			else
				display.Enabled = false;
			return true;
		}

		/// <summary>
		/// Enable the "Add Note" command if the idcPane is visible and wants to do it.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddNote(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_idcPane != null && InterlinearTabPageIsSelected())
				m_idcPane.OnDisplayAddNote(commandObject, ref display);
			else
				display.Enabled = false;
			return true;
		}

		/// <summary>
		/// Delegate this command to the idcPane. (It isn't enabled unless one exists.)
		/// </summary>
		/// <param name="argument"></param>
		public void OnAddFreeTrans(object argument)
		{
			CheckDisposed();
			using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(Cache, argument as Command))
			{
				m_idcPane.OnAddFreeTrans1(argument);
			}
		}

		/// <summary>
		/// Delegate this command to the idcPane. (It isn't enabled unless one exists.)
		/// </summary>
		/// <param name="argument"></param>
		public void OnAddLitTrans(object argument)
		{
			CheckDisposed();
			using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(Cache, argument as Command))
			{
				m_idcPane.OnAddLitTrans1(argument);
			}
		}

		/// <summary>
		/// Delegate this command to the idcPane. (It isn't enabled unless one exists.)
		/// </summary>
		/// <param name="argument"></param>
		public void OnAddNote(object argument)
		{
			CheckDisposed();
			using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(Cache, argument as Command))
			{
				m_idcPane.OnAddNote1(argument);
			}
		}
#endregion

		protected void m_idcPane_AnnnotationSelected(object sender, AnnotationSelectedArgs e)
		{
			m_bookmark.Save();
		}

		/// <summary>
		/// Gets/Sets the property table state for the selected tab page.
		/// </summary>
		internal TabPageSelection InterlinearTab
		{
			get
			{
				if (m_mediator == null || m_mediator.PropertyTable == null)
					return TabPageSelection.RawText;
				string val = m_mediator.PropertyTable.GetStringProperty("InterlinearTab", TabPageSelection.RawText.ToString());
				TabPageSelection tabSelection;
				try
				{
					tabSelection = (TabPageSelection)Enum.Parse(typeof(TabPageSelection), val);
				}
				catch
				{
					tabSelection = TabPageSelection.RawText;
					InterlinearTab = tabSelection;
				}
				return tabSelection;
			}

			set
			{
				m_mediator.PropertyTable.SetProperty("InterlinearTab", value.ToString());
			}
		}

		/// <summary>
		/// Note: This property only makes sense in the context of Gloss or Analyze (Interlinearize) tab
		/// It is used to establish the line options for the document used in that tab.
		/// </summary>
		private string ConfigPropName
		{
			get { return "InterlinConfig_Edit_" + InterlinearTab.ToString(); }
		}

		/// <summary>
		/// setup the line choices for the interlinear document based upon
		/// the tab context (Analyze or Gloss).
		/// </summary>
		/// <returns></returns>
		private InterlinLineChoices SetupLineChoices()
		{
			if (m_hvoStText == 0)
				return null;
			InterlinLineChoices lineChoices = null ;
			if (m_idcPane != null)
				lineChoices = m_idcPane.SetupLineChoices(ConfigPropName, GetLineMode());
			return lineChoices;
		}

		internal InterlinLineChoices.InterlinMode GetLineMode()
		{
			if (m_tabCtrl.SelectedIndex == ktpsGloss)
			{
				return m_mediator.PropertyTable.GetBoolProperty(ksPropertyAddWordsToLexicon, false) ?
					InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon : InterlinLineChoices.InterlinMode.Gloss;
			}
			else if (m_tabCtrl.SelectedIndex == ktpsAnalyze)
			{
				return InterlinLineChoices.InterlinMode.Analyze;
			}
			return InterlinLineChoices.InterlinMode.Analyze;
		}
		const string ksPropertyAddWordsToLexicon = "ITexts_AddWordsToLexicon";
		const string ksPropertyShowAddWordsToLexiconDlg = "ITexts_ShowAddWordsToLexiconDlg";


		/// <summary>
		/// (LT-7807) determine whether we're in the context/state where we we want to add
		/// monomorphemic glosses to lexicon.
		/// </summary>
		internal bool InModeForAddingGlossedWordsToLexicon
		{
			get
			{
				return GetLineMode() == InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon;
			}
		}

		/// <summary>
		/// Enable the "Configure Interlinear" command. Can be done any time this view is a target.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayConfigureInterlinear(object commandObject,
			ref UIItemDisplayProperties display)
		{
			bool fDisplay = m_idcPane != null && InterlinearTabPageIsSelected() && IsPersistedForAnInterlinearTabPage;
			display.Visible = fDisplay;
			display.Enabled = fDisplay;
			return true;
		}

		/// <summary>
		///  Launch the Configure interlinear dialog and deal with the results
		/// </summary>
		/// <param name="argument"></param>
		public bool OnConfigureInterlinear(object argument)
		{
			m_idcPane.OnConfigureInterlinear(argument);
			return true; // We handled this
		}

		/// <summary>
		/// Use this to determine whether the last selected tab page which was
		/// persisted in the PropertyTable, pertains to an interlinear document.
		/// Currently, two tabs share an interlinear document (Gloss and Interlinearizer (Analyze)).
		/// </summary>
		protected bool IsPersistedForAnInterlinearTabPage
		{
			get
			{
				if (m_mediator == null || m_mediator.PropertyTable == null)
					return false; // apparently not quite setup to determine true or false.
				return InterlinearTab == TabPageSelection.Interlinearizer ||
					InterlinearTab == TabPageSelection.Gloss;
			}
		}

		/// <summary>
		/// create and register a URL describing the current context, for use in going backwards
		/// and forwards
		/// </summary>
		/// <remarks> We need an override in order to store the state of the "mode".</remarks>
		protected override void UpdateContextHistory()
		{
			// are we the dominant pane? The thinking here is that if our clerk is controlling
			// the record tree bar, then we are.
			if (Clerk.IsControllingTheRecordTreeBar)
			{
				//add our current state to the history system
				string toolName =
					m_mediator.PropertyTable.GetStringProperty("currentContentControl","");
				int hvo=-1;
				if (Clerk. CurrentObject!= null)
					hvo = Clerk. CurrentObject.Hvo;
				FdoCache cache = Cache;
				FwLink link = FwLink.Create(toolName, cache.GetGuidFromId(hvo),
					cache.ServerName, cache.DatabaseName, InterlinearTab.ToString());
				link.PropertyTableEntries.Add(new XCore.Property("InterlinearTab",
					InterlinearTab.ToString()));
				m_mediator.SendMessage("AddContextToHistory", link, false);
			}
		}

		/// <summary>
		/// determine if this is the correct place [it's the only one that handles the message, and
		/// it defaults to false, so it should be]
		/// </summary>
		protected  bool InFriendlyArea
		{
			get
			{
				string desiredArea = "textsWords";

				// see if it's the right area
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
				return areaChoice != null && areaChoice == desiredArea;
			}
		}

		/// <summary>
		/// determine if we're in the (given) tool
		/// </summary>
		/// <param name="desiredTool"></param>
		/// <returns></returns>
		protected bool InFriendlyTool(string desiredTool)
		{
			string toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_textsWords", null);
			return toolChoice != null && toolChoice == desiredTool;
		}

		/// <summary>
		/// Mode for populating the Lexicon with monomorphemic glosses
		/// </summary>
		/// <param name="argument"></param>
		public bool OnDisplayITexts_AddWordsToLexicon(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool fCanDisplayAddWordsToLexiconPanelBarButton = InterlinearTab == TabPageSelection.Gloss;
			display.Visible = fCanDisplayAddWordsToLexiconPanelBarButton;
			display.Enabled = fCanDisplayAddWordsToLexiconPanelBarButton;
			return true;
		}

		private bool CanShowAddWordsToLexiconDlg()
		{
			return (m_idcPane != null && m_idcPane.RawStText != null && InterlinearTab == TabPageSelection.Gloss && m_fullyInitialized);
		}

		/// <summary>
		/// handle the message to see if the menu item should be displayed
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayShowMorphBundles(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea && InFriendlyTool("interlinearEdit");
			return true; //we've handled this
		}
		public bool OnDisplayCreateTextObjects(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			string sToolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			string sTabName = m_mediator.PropertyTable.GetStringProperty("InterlinearTab", "");
			display.Enabled = (InFriendlyArea &&
							   ((sToolName == "interlinearEdit") &&
							   ((sTabName == "Interlinearizer") || (sTabName == "RawText"))));
			display.Visible = display.Enabled;
			return true;	//we handled this.
		}

		public bool OnCreateTextObjects(object cmd)
		{
			IStText text = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject", null) as IStText;
			if (text != null)
			{
				DateTime start = new DateTime();
				DateTime finish = new DateTime();
				start = DateTime.Now;
				text.CreateTextObjects();
				finish = DateTime.Now;
				Trace.WriteLine("CreateTextObjects took " + (finish - start));
			}
			return true; //we've handled this
		}

	}

	public class InterlinearTextsRecordClerk : RecordClerk
	{
		InterlinearTextsVirtualHandler m_interlinearTextsVh = null;

		// The following is used in the process of selecting the ws for a new text.  See LT-6692.
		private int m_wsPrevText = 0;
		public int PrevTextWs
		{
			get { return m_wsPrevText; }
			set { m_wsPrevText = value; }
		}

		public override void Init(Mediator mediator, XmlNode viewConfiguration)
		{
			base.Init(mediator, viewConfiguration);
			TryGetInterlinearTextsVirtualHandler(out m_interlinearTextsVh);
			CanAccessScriptureIds();	// cache ability to access scripture ids.
		}

		/// <summary>
		/// Get the list of currently selected Scripture section ids.
		/// </summary>
		/// <returns></returns>
		public List<int> GetScriptureIds()
		{
			if (m_interlinearTextsVh != null)
				return m_interlinearTextsVh.GetScriptureIds();
			else
				return new List<int>();
		}

		/// <summary>
		/// Enable the "Add Scripture" command if TE is installed.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddScripture(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			display.Enabled = IsActiveClerk && CanAccessScriptureIds();
			display.Visible = display.Enabled;
			return true;
		}

		/// <summary>
		/// Indicated whether TE is installed or not;
		/// </summary>
		bool m_fCanAccessScripture = false;
		bool m_fCanAccessScriptureCached = false;
		private bool CanAccessScriptureIds()
		{
			if (!m_fCanAccessScriptureCached)
			{
				Form mainWindow = (Form)m_mediator.PropertyTable.GetValue("window") as Form;
				FwXWindow activeWnd = mainWindow as FwXWindow;
				m_fCanAccessScripture = m_interlinearTextsVh != null && FwXApp.IsTEInstalled;
				if (m_interlinearTextsVh != null)
					m_interlinearTextsVh.CanAccessScriptureIds = m_fCanAccessScripture;
				m_fCanAccessScriptureCached = true;
			}
			return m_fCanAccessScripture;
		}


		bool TryGetInterlinearTextsVirtualHandler(out InterlinearTextsVirtualHandler itvh)
		{
			itvh = null;
			List<int> flids;
			IVwVirtualHandler vh;
			// first try our record list
			if (Cache.TryGetVirtualHandler(m_list.Flid, out vh) && vh is InterlinearTextsVirtualHandler)
			{
				itvh = vh as InterlinearTextsVirtualHandler;
			}
			else if (Cache.TryGetDependencies(m_list.Flid, out flids))
			{
				// try our dependencies.
				Set<int> uniqueflids = new Set<int>(flids);
				foreach (int flid in uniqueflids)
				{
					if (Cache.TryGetVirtualHandler(flid, out vh) && vh is InterlinearTextsVirtualHandler)
					{
						itvh = vh as InterlinearTextsVirtualHandler;
					}
				}
			}
			return itvh != null;
		}

		protected bool OnAddScripture(object args)
		{
			CheckDisposed();
			// get saved scripture choices
			if (m_interlinearTextsVh != null)
			{
				List<int> savedHvos = m_interlinearTextsVh.GetScriptureIds();
				// Ensure we get a current view of the data.
				Cache.VwOleDbDaAccessor.UpdatePropIfCached(Cache.LangProject.TranslatedScriptureOAHvo,
					(int)Scripture.ScriptureTags.kflidScriptureBooks,
					(int)CellarModuleDefns.kcptOwningSequence, Cache.DefaultVernWs);
				using (FilterScrSectionDialog dlg = new FilterScrSectionDialog(Cache, savedHvos.ToArray()))
				{
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						int[] hvosScriptureToAdd = dlg.GetListOfIncludedSections();
						m_interlinearTextsVh.UpdateList(hvosScriptureToAdd);
					}
				}
			}
			return true;
		}

		private string GetRecordListContext()
		{
			string recordListContext = "???";
			if (this.InDesiredArea("textsWords"))
			{
				recordListContext = ITextStrings.ksTexts;
			}
			else if (this.InDesiredTool("concordance"))
			{
				recordListContext = ITextStrings.ksConcordance;
			}
			return recordListContext;
		}

		/// <summary>
		/// Always enable the 'InsertInterlinText' command by default for this class, but allow
		/// subclasses to override this behavior.
		/// </summary>
		public virtual bool OnDisplayInsertInterlinText(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = IsActiveClerk && InDesiredArea("textsWords");

			RecordClerk clrk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
			if (clrk.Id == "interlinearTexts")
			{
				display.Enabled = true;
				return true;
			}
			display.Enabled = false;
			return true;
		}

		/// <summary>
		/// We use a unique method name for inserting a text, which could otherwise be handled simply
		/// by letting the Clerk handle InsertItemInVector, because after it is inserted we may
		/// want to switch tools.
		/// The argument should be the XmlNode for <parameters className="Text"/>.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnInsertInterlinText(object argument)
		{
			// Get the default writing system for the new text.  See LT-6692.
			m_wsPrevText = Cache.DefaultVernWs;
			if (CurrentObject != null && Cache.LangProject.VernWssRC.Count > 1)
			{
				m_wsPrevText = Cache.LangProject.ActualWs(LangProject.kwsVernInParagraph,
					CurrentObject.Hvo, (int)StText.StTextTags.kflidParagraphs);
			}
			if (m_list.Filter != null)
			{
				// Tell the user we're turning off the filter, and then do it.
				MessageBox.Show(ITextStrings.ksTurningOffFilter, ITextStrings.ksNote, MessageBoxButtons.OK);
				m_mediator.SendMessage("RemoveFilters", this);
				m_activeMenuBarFilter = null;
			}
			m_mediator.SendMessageToAllNow("InsertItemInVector", argument);
			if (CurrentObject == null || CurrentObject.Hvo == 0)
				return false;
			if (!InDesiredTool("interlinearEdit"))
				m_mediator.SendMessage("FollowLink", FwLink.Create("interlinearEdit", Cache.GetGuidFromId(CurrentObject.Hvo), Cache.ServerName, Cache.DatabaseName));
			// This is a workable alternative (where link is the one created above), but means this code has to know about the FwXApp class.
			//(FwXApp.App as FwXApp).OnIncomingLink(link);
			// This alternative does NOT work; it produces a deadlock...I think the remote code is waiting for the target app
			// to return to its message loop, but it never does, because it is the same app that is trying to send the link, so it is busy
			// waiting for 'Activate' to return!
			//link.Activate();
			return true;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_interlinearTextsVh = null;
			}
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// "SIL.FieldWorks.IText.InterlinearTextsVirtualHandler"
	/// </summary>
	public class InterlinearTextsVirtualHandler : FDOSequencePropertyTableVirtualHandler, IAddItemToVirtualProperty
	{
		public InterlinearTextsVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
		}

		protected override List<int> GetListForCache()
		{
			List<int> scriptures = GetScriptureIds();
			// (int)TextTags.kflidContents = 5054008
			// (int)ScrSection.ScrSectionTags.kflidHeading = 3005001
			// (int)ScrSection.ScrSectionTags.kflidContent = 3005002
			string sql = string.Format("select id from StText_ where OwnFlid$={0}", (int)Text.TextTags.kflidContents);
			List<int> texts = DbOps.ReadIntsFromCommand(Cache, sql, null);
			texts.AddRange(scriptures);
			return texts;
		}

		/// <summary>
		/// True if TE is installed.
		/// </summary>
		/// <returns></returns>
		bool m_fCanAccessScriptureIds = false;
		public bool CanAccessScriptureIds
		{
			get { return m_fCanAccessScriptureIds; }
			set { m_fCanAccessScriptureIds = value; }
		}

		/// <summary>
		/// Return the list of ids we stored in our property table.
		/// (Filter to remove texts that have been archived.)
		/// </summary>
		/// <returns></returns>
		internal List<int> GetScriptureIds()
		{
			if (CanAccessScriptureIds)
			{
				List<int> result = PropertyTableList();
				// Filter any saved items which belong to a ScrDraft.
				for (int i = 0; i < result.Count;)
				{
					if (m_cache.GetOwnerOfObjectOfClass(result[i],
						ScrDraft.kclsidScrDraft) != 0)
					{
						// owned by a draft...don't return it
						result.RemoveAt(i);
						// and don't increment i, the next one is now at position i.
					}
					else
					{
						i++;
					}
				}
				return result;
			}
			else
				return new List<int>();
		}

		/// <summary>
		/// This is invoked when TE (or some other program) invokes a link, typically to a Scripture Section text not in our filter.
		/// If possible, add it to the filter and return its index. Otherwise, return -1.
		/// </summary>
		public bool Add(int hvoListOwner, int hvoItem)
		{
			int targetPosition = TextPosition(hvoItem);
			if (targetPosition < 0)
				return false; // not a text in current Scripture.
			List<int> ids = GetScriptureIds();
			int index;
			for(index = 0; index < ids.Count; index++)
			{
				if (TextPosition(ids[index]) > targetPosition)
				{
					break;
				}
			}
			ids.Insert(index, hvoItem);
			// Also insert the other text in the same section
			int hvoSection = m_cache.GetOwnerOfObject(hvoItem);
			IScrSection sec = CmObject.CreateFromDBObject(m_cache, hvoSection) as IScrSection;
			if (sec != null) // paranoia
			{
				if (hvoItem == sec.ContentOA.Hvo && sec.HeadingOAHvo != 0)
				{
					if (index == 0 || ids[index - 1] != sec.HeadingOA.Hvo)
						ids.Insert(index, sec.HeadingOAHvo);
					else
						index--; // move index to point at heading
				}
				else if (sec.ContentOAHvo != 0)
				{
					if (index >= ids.Count - 1 || ids[index + 1] != sec.ContentOAHvo)
						ids.Insert(index + 1, sec.ContentOAHvo);
				}
				// At this point the heading and contents of the section for the inserted text
				// are at index. We look for adjacent sections in the same chapter and if necessary
				// add them too.
				int indexAfter = index + 1;
				if (sec.ContentOAHvo != 0 && sec.HeadingOAHvo != 0)
					indexAfter++;
				// It would be nicer to use ScrReference, but not worth adding a whole project reference.
				int chapMax = sec.VerseRefMax/1000;
				int chapMin = sec.VerseRefMin/1000;
				int hvoBook = sec.OwnerHVO;
				IScrBook book = CmObject.CreateFromDBObject(m_cache, hvoBook) as IScrBook;
				int csec = book.SectionsOS.Count;
				int isecCur = m_cache.GetObjIndex(hvoBook, (int) ScrBook.ScrBookTags.kflidSections, hvoSection);
				for (int isec = isecCur + 1; isec < csec; isec++)
				{
					IScrSection secNext = book.SectionsOS[isec];
					if (secNext.VerseRefMin/1000 != chapMax)
						break; // different chapter.
					indexAfter = AddAfter(ids, indexAfter, secNext.HeadingOAHvo);
					indexAfter = AddAfter(ids, indexAfter, secNext.ContentOAHvo);
				}
				for (int isec = isecCur - 1; isec >= 0; isec--)
				{
					IScrSection secPrev = book.SectionsOS[isec];
					if (secPrev.VerseRefMax/1000 != chapMin)
						break;
					index = AddBefore(ids, index, secPrev.ContentOAHvo);
					index = AddBefore(ids, index, secPrev.HeadingOAHvo);
				}
			}
			UpdateList(ids.ToArray());
			return true;
		}

		private int AddBefore(List<int> ids, int index, int hvoAdd)
		{
			if (hvoAdd == 0)
				return index; // nothing to add
			if (index == 0 || ids[index - 1] != hvoAdd)
			{
				// Not present, add it.
				ids.Insert(index, hvoAdd);
				return index; // no change, things moved up.
			}
			return index - 1;
		}

		private int AddAfter(List<int> ids, int indexAfter, int hvoAdd)
		{
			if (hvoAdd == 0)
				return indexAfter; // nothing to add
			if (indexAfter >= ids.Count - 1 || ids[indexAfter] != hvoAdd)
			{
				// Not already present, add it.
				ids.Insert(indexAfter, hvoAdd);
			}
			return indexAfter + 1; // in either case next text goes after this one.
		}

		/// <summary>
		/// Return an index we can use to order StTexts in Scripture.
		/// Take the book index * 10,000.
		/// if not in the title, add (section index + 1)*2.
		/// If in contents add 1.
		/// </summary>
		/// <param name="hvoText"></param>
		/// <returns></returns>
		int TextPosition(int hvoText)
		{
			int hvoOwner = m_cache.GetOwnerOfObject(hvoText);
			int flid = m_cache.GetOwningFlidOfObject(hvoText);
			if (flid != (int) ScrSection.ScrSectionTags.kflidContent &&
				flid != (int) ScrSection.ScrSectionTags.kflidHeading
				&& flid != (int) ScrBook.ScrBookTags.kflidTitle)
			{
				return -1;
			}
			if (flid == (int) ScrBook.ScrBookTags.kflidTitle)
				return BookPosition(hvoOwner);
			int hvoSection = hvoOwner;
			int hvoBook = m_cache.GetOwnerOfObject(hvoSection);
			return BookPosition(hvoBook)
				   + m_cache.GetObjIndex(hvoBook, (int) ScrBook.ScrBookTags.kflidSections, hvoSection)*2 + 2
				   + (flid == (int) ScrSection.ScrSectionTags.kflidContent ? 1 : 0);
		}

		private int BookPosition(int hvoBook)
		{
			return m_cache.GetObjIndex(m_cache.LangProject.TranslatedScriptureOAHvo,
									   (int) Scripture.ScriptureTags.kflidScriptureBooks, hvoBook)*10000;
		}
	}

	/// <summary>
	/// Virtual handler for figuring titles for StText
	/// </summary>
	public class StTextTitleVirtualHandler : MultiStringVirtualHandler
	{
		FdoCache m_cache = null;

		public StTextTitleVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
			m_cache = cache;
		}

		public override void Load(int hvoStText, int vtagStTextTitle, int ws, IVwCacheDa cda)
		{
			ITsString tssTitle = null;
			StText stText = new StText(Cache, hvoStText);
			if (Scripture.IsResponsibleFor(stText))
			{
				// it's important that we try to scripture more dynamically than during initialization
				// of the virtual handler, since we may not have created the Scripture object with TE
				// until after we've created the FLEX project (LT-8123) and it might be possible that
				// the scripture for that project could get deleted subsequently(?).
				Scripture scripture = Cache.LangProject.TranslatedScriptureOA as Scripture;
				if (scripture != null)
				{
					tssTitle = scripture.BookChapterVerseBridgeAsTss(stText, ws);
					if (stText.OwningFlid == (int)ScrSection.ScrSectionTags.kflidHeading)
					{
						string sFmt = ITextStrings.ksSectionHeading;
						int iMin = sFmt.IndexOf("{0}");
						if (iMin < 0)
						{
							tssTitle = m_cache.MakeUserTss(sFmt);
						}
						else
						{
							ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
							if (iMin > 0)
								tisb.AppendTsString(m_cache.MakeUserTss(sFmt.Substring(0, iMin)));
							tisb.AppendTsString(tssTitle);
							if (iMin + 3 < sFmt.Length)
								tisb.AppendTsString(m_cache.MakeUserTss(sFmt.Substring(iMin + 3)));
							tssTitle = tisb.GetString();
						}
					}
				}
			}
			else if (stText.OwningFlid == (int)Text.TextTags.kflidContents)
			{
				Text text = stText.Owner as Text;
				tssTitle = text.Name.GetAlternativeTss(ws);
			}
			else
			{
				// throw?
			}
			if (tssTitle == null)
				tssTitle = Cache.MakeAnalysisTss("");
			cda.CacheStringAlt(hvoStText, vtagStTextTitle, ws, tssTitle);
		}

		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}
	}


	public class StTextIsTranslationVirtualHandler : FDOBooleanPropertyVirtualHandler
	{
		public StTextIsTranslationVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
		}

		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			bool fIsTranslation = false;
			StText stText = new StText(Cache, hvo);
			if (Scripture.IsResponsibleFor(stText))
			{
				// we'll consider everything but footnotes a translation.
				fIsTranslation = stText.OwningFlid != (int)ScrBook.ScrBookTags.kflidFootnotes;
			}
			else if (stText.OwningFlid == (int)Text.TextTags.kflidContents)
			{
				Text text = stText.Owner as Text;
				fIsTranslation = text.IsTranslated;
			}
			else
			{
				// throw?
			}
			cda.CacheBooleanProp(hvo, tag, fIsTranslation);
		}
	}

	/// <summary>
	/// Helper for keeping track of our location in the text when switching from and back to the
	/// Texts area (cf. LT-1543).  It also serves to keep our place when switching between
	/// RawTextPane (Baseline), GlossPane, AnalyzePane(Interlinearizer), TaggingPane, PrintPane and ConstChartPane.
	/// </summary>
	public class InterAreaBookmark
	{
		InterlinMaster m_interlinMaster;
		XCore.Mediator m_mediator;
		FdoCache m_cache = null;
		bool m_fInTextsArea = false;
		int m_iParagraph = -1;
		int m_BeginOffset = -1;
		int m_EndOffset = -1;

		internal InterAreaBookmark()
		{
		}

		internal InterAreaBookmark(InterlinMaster interlinMaster, Mediator mediator, FdoCache cache)	// For restoring
		{
			Init(interlinMaster, mediator, cache);
			this.Restore();
		}

		internal void Init(InterlinMaster interlinMaster, Mediator mediator, FdoCache cache)
		{
			Debug.Assert(interlinMaster != null);
			Debug.Assert(mediator != null);
			Debug.Assert(cache != null);
			m_interlinMaster = interlinMaster;
			m_mediator = mediator;
			m_cache = cache;
			m_fInTextsArea = m_interlinMaster.InTextsArea;
			if (!m_fInTextsArea)
			{
				// We may be switching areas to the Texts from somewhere else, which isn't yet
				// reflected in the value of "areaChoice", but is reflected in the value of
				// "currentContentControlParameters" if we dig a little bit.
				System.Xml.XmlElement x =
					m_mediator.PropertyTable.GetValue("currentContentControlParameters", null)
					as System.Xml.XmlElement;
				if (x != null)
				{
					System.Xml.XmlNodeList xnl = x.GetElementsByTagName("parameters");
					foreach (System.Xml.XmlNode xn in xnl)
					{
						System.Xml.XmlElement xe = xn as System.Xml.XmlElement;
						if (xe != null)
						{
							string sVal = xe.GetAttribute("area");
							if (sVal != null && sVal == "textsWords")
							{
								m_fInTextsArea = true;
								break;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Saves and persists the current selected annotation (or string) in the InterlinMaster.
		/// </summary>
		public void Save()
		{
			m_interlinMaster.SaveBookMark();
		}

		const int kflidBeginOffset = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset;
		const int kflidEndOffset = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset;

		/// <summary>
		/// Saves the given annotation in the InterlinMaster.
		/// </summary>
		/// <param name="ann">annotation</param>
		/// <param name="fPersistNow">if true, this annotation will persist.</param>
		public void Save(int annHvo, bool fPersistNow)
		{
			if (annHvo == -1 || annHvo == 0)
			{
				this.Reset(); // let's just reset for an empty location.
				return;
			}
			int iPara = RawTextPane.GetParagraphIndexForAnnotation(m_cache, annHvo);
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			Save(iPara, sda.get_IntProp(annHvo, kflidBeginOffset),
				sda.get_IntProp(annHvo, kflidEndOffset), fPersistNow);
		}

		/// <summary>
		/// Saves the current selected annotation in the InterlinMaster.
		/// </summary>
		/// <param name="fPersistNow">if true, this annotation will persist.</param>
		public void Save(bool fPersistNow)
		{
			if (fPersistNow)
				this.SavePersisted();
		}

		internal void Save(int paragraphIndex, int beginCharOffset, int endCharOffset, bool fPersistNow)
		{
			m_iParagraph = paragraphIndex;
			m_BeginOffset = beginCharOffset;
			m_EndOffset = endCharOffset;

			this.Save(fPersistNow);
		}

		private void Save(int annHvo)
		{
			Save(annHvo, true);
		}

		private string BookmarkNamePrefix
		{
			get
			{
				return "ITexts-Bookmark-";
			}
		}

		internal string RecordIndexBookmarkName
		{
			get
			{
				return BookmarkPropertyName("IndexOfRecord");
			}
		}

		private string BookmarkPropertyName(string attribute)
		{
			return BookmarkNamePrefix + attribute;
		}

		private void SavePersisted()
		{
			// Currently, we only support persistence for the Texts area, since the Words
			// area Record Clerk keeps track of the current CmBaseAnnotation for us.
			// This will help prevent us from saving over or loading something persisted
			// for another area.  We should make this class inherit from IPersistAsXml if we want
			// to store information for identifying which record clerk we are saving for.
			if (!m_fInTextsArea)
				return;
			Debug.Assert(m_mediator != null);
			// TODO: store clerk identifier in property. For now, just do the index.
			// to make this more strict, we could match on the title, but let's do that later.
			int recordIndex = m_interlinMaster.IndexOfTextRecord;
			// string recordTitle = m_interlinMaster.TitleOfTextRecord;
			// m_mediator.PropertyTable.SetProperty(pfx + "Title", recordTitle, false);
			m_mediator.PropertyTable.SetProperty(RecordIndexBookmarkName, recordIndex, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("IndexOfParagraph"), m_iParagraph, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("CharBeginOffset"), m_BeginOffset, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("CharEndOffset"), m_EndOffset, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(RecordIndexBookmarkName, true, PropertyTable.SettingsGroup.LocalSettings);
			// m_mediator.PropertyTable.SetPropertyPersistence(pfx + "Title", true);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("IndexOfParagraph"), true, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("CharBeginOffset"), true, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("CharEndOffset"), true, PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Restore the InterlinMaster bookmark to its previously saved state.
		/// </summary>
		public void Restore()
		{
			// Currently, we only support persistence for the Texts area, since the Words
			// area Record Clerk keeps track of the current CmBaseAnnotation for us.
			// This will help prevent us from us from saving over or loading something persisted
			// for another area.  We should make this class inherit from IPersistAsXml if we want
			// to store information for identifying which record clerk we are saving for.
			if (!m_fInTextsArea)
				return;
			Debug.Assert(m_mediator != null);
			// verify we're restoring to the right text. Is there a better way to verify this?
			int restoredRecordIndex = m_mediator.PropertyTable.GetIntProperty(RecordIndexBookmarkName, -1, PropertyTable.SettingsGroup.LocalSettings);
			if (m_interlinMaster.IndexOfTextRecord != restoredRecordIndex)
				return;
			m_iParagraph = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("IndexOfParagraph"), -1, PropertyTable.SettingsGroup.LocalSettings);
			m_BeginOffset = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("CharBeginOffset"), -1, PropertyTable.SettingsGroup.LocalSettings);
			m_EndOffset = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("CharEndOffset"), -1, PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Reset the bookmark to its default values.
		/// </summary>
		public void Reset()
		{
			m_iParagraph = -1;
			m_BeginOffset = -1;
			m_EndOffset = -1;

			this.SavePersisted();
		}

		public int IndexOfParagraph	{ get { return m_iParagraph; } }
		public int BeginCharOffset  { get { return m_BeginOffset; } }
		public int EndCharOffset	{ get { return m_EndOffset; } }
	}
}
