// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Diagnostics;
using LingTree;
using Microsoft.Win32;
using System.Text;

namespace SIL.PcPatrBrowser
{
	/// <summary>
	/// Summary description for PcPatrBrowserApp.
	/// </summary>
	public class PcPatrBrowserApp : System.Windows.Forms.Form
	{
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Panel pnlInterlinear;
		private System.Windows.Forms.Splitter splitterBetweenInterlinearAndTreeFeat;
		private System.Windows.Forms.Panel pnlTreeFeat;
		private System.Windows.Forms.Panel pnlTree;
		private System.Windows.Forms.Panel pnlFeatureStructure;
		private System.Windows.Forms.Splitter splitterBetweenTreeAndFeat;
		private System.Windows.Forms.Panel pnlRule;
		private System.Windows.Forms.Splitter splitterBetweenTreeFeatAndRule;
		private System.Windows.Forms.RichTextBox richTextBox1;
		private System.Windows.Forms.StatusBar sbStatusBar;
		private System.Windows.Forms.StatusBarPanel sbpnlSentence;
		private System.Windows.Forms.StatusBarPanel sbpnlParse;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem miFileOpenAna;
		private System.Windows.Forms.MenuItem miFileOpenGrammar;
		private System.Windows.Forms.MenuItem miFile;
		private System.Windows.Forms.MenuItem miExit;
		private System.Windows.Forms.MenuItem miView;
		private System.Windows.Forms.MenuItem miSentence;
		private System.Windows.Forms.MenuItem miSentenceNext;
		private System.Windows.Forms.MenuItem miSentencePrevious;
		private System.Windows.Forms.MenuItem miSentenceLast;
		private System.Windows.Forms.MenuItem miSentenceFirst;
		private System.Windows.Forms.MenuItem miParse;
		private System.Windows.Forms.MenuItem miParseFirst;
		private System.Windows.Forms.MenuItem miParsePrevious;
		private System.Windows.Forms.MenuItem miParseNext;
		private System.Windows.Forms.MenuItem miParseLast;
		private System.Windows.Forms.MenuItem miSentenceGoTo;
		private System.Windows.Forms.MenuItem miUseThisParse;
		private System.Windows.Forms.MenuItem miCancelUsingThisParse;
		private WebBrowser wbFeatureStructure;
		private WebBrowser wbInterlinear;
		private string m_sStartUpPath;

		public static string m_sRegKey = "Software\\SIL\\PcPatrBrowser";
		const string m_ksLastLanguageFile = "LastTreeFile";
		const string m_ksLastLogFile = "LastLogFile";
		const string m_ksLocationX = "LocationX";
		const string m_ksLocationY = "LocationY";
		const string m_ksSizeHeight = "SizeHeight";
		const string m_ksSizeWidth = "SizeWidth";
		const string m_ksWindowState = "WindowState";
		const string m_ksSplitterBetweenInterAndTreeFeat = "SplitterBetweenInterAndTreeFeat";
		const string m_ksSplitterBetweenTreeAndFeat = "SplitterBetweenTreeAndFeat";
		const string m_ksSplitterBetweenTreeFeatAndRule = "SplitterBetweenTreeFeatAndRule";
		const string m_ksViewFeatStruct = "ViewFeatStruct";
		const string m_ksViewInterlinear = "ViewInterlinear";
		const string m_ksViewRule = "ViewRule";
		const string m_ksViewStatusBar = "ViewStatusBar";
		const string m_ksViewToolBar = "ViewToolBar";
		const string m_ksViewRightToLeft = "ViewRightToLeft";
		const string m_ksSaveForTreeTran = "SaveForTreeTran";
		private bool m_fNeedToSaveLanguageInfo;
		private Rectangle m_RectNormal;
		private string m_sLogOrAnaFileName;
		private string m_sGrammarFileName;
		private string m_sLanguageFileName;
		private const string m_strLanguageFileFilter =
			"PC-PATR Browser Language Info (*.pbl)|*.pbl|" + "All Files (*.*)|*.*";
		string m_sFSFile;
		string m_sInterFile;
		private PcPatrDocument m_doc;
		private PcPatrParse m_parse;
		private LingTreeTree m_tree;
		private XslCompiledTransform m_treeTransform;
		private XslCompiledTransform m_fsTransform;
		private XslCompiledTransform m_interlinearTransform;
		private string m_sInitFeatureMessagePath;
		private string m_sInitInterlinearMessagePath;
		private const string m_ksNoSentences = "No sentences";
		private LanguageInfo m_language;
		private System.Windows.Forms.MenuItem miViewStatusBar;
		private System.Windows.Forms.MenuItem miViewRuleFile;
		private System.Windows.Forms.MenuItem miViewFeatStruct;
		private System.Windows.Forms.ToolBar tbMain;
		private System.Windows.Forms.ToolBarButton tbbtnFirstSentence;
		private System.Windows.Forms.ToolBarButton tbbtnNextSentence;
		private System.Windows.Forms.ToolBarButton tbbtnPreviousSentence;
		private System.Windows.Forms.ToolBarButton tbbtnLastSentence;
		private System.Windows.Forms.ToolBarButton tbbtnFirstParse;
		private System.Windows.Forms.ToolBarButton tbbtnPreviousParse;
		private System.Windows.Forms.ToolBarButton tbbtnNextParse;
		private System.Windows.Forms.ToolBarButton tbbtnLastParse;
		private System.Windows.Forms.ToolBarButton tbbtnSeparator;
		private System.Windows.Forms.ToolBarButton tbbtnSeparator1;
		private System.Windows.Forms.ToolBarButton tbbtnOpenAnalysis;
		private System.Windows.Forms.ToolBarButton tbbtnUseThisParse;
		private System.Windows.Forms.ImageList ilToolBar;
		private System.Windows.Forms.ToolBarButton tbbtnSeparator2;
		private System.Windows.Forms.ToolBarButton tbbtnSeparator3;
		private System.Windows.Forms.MenuItem miViewToolBar;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem miParserGoTo;
		private System.Windows.Forms.MenuItem miViewInterlinear;
		private System.Windows.Forms.MenuItem menuItem5;
		private System.Windows.Forms.MenuItem miViewRightToLeft;
		private System.Windows.Forms.MenuItem miLanguage;
		private System.Windows.Forms.MenuItem menuItem7;
		private System.Windows.Forms.MenuItem menuItem9;
		private System.Windows.Forms.MenuItem menuItem10;
		private System.Windows.Forms.MenuItem miFileOpenLanguage;
		private System.Windows.Forms.MenuItem miFileSaveLanguage;
		private System.Windows.Forms.MenuItem miFileSaveLangAs;
		private const string m_ksNoParses = "No parses";
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem miViewRefresh;
		private System.Windows.Forms.ToolBarButton tbbtnRefresh;
		private System.Windows.Forms.ToolBarButton tbbtbSeparator4;
		private System.Windows.Forms.ToolBarButton tbbtbSeparator5;
		private ToolBarButton tbbtnSeparator6;
		private ToolBarButton tbbtnSaveForTreeTran;
		private MenuItem miSaveForTreeTran;
		private MenuItem miUseThisParseNextSentence;
		private const string m_ksGrammarMessage =
			"When you click on a node in the tree in the panel above and "
			+ "a grammar file has been loaded, the corresponding rule will show here.";

		public String[] PropertiesChosen { get; set; }
		public int[] ParsesChosen { get; set; }

		public event LingTreeNodeClickedEventHandler LingTreeNodeClicked;

		protected virtual void OnLingTreeNodeClicked(LingTreeNodeClickedEventArgs ltncea)
		{
			if (LingTreeNodeClicked != null)
				LingTreeNodeClicked(this, ltncea);
		}

		public PcPatrBrowserApp()
		{
			InitPcPatrBrowser(Application.StartupPath);
		}

		public PcPatrBrowserApp(string sStartupPath)
		{
			InitPcPatrBrowser(sStartupPath);
		}

		private void InitPcPatrBrowser(string sStartupPath)
		{
			m_sStartUpPath = sStartupPath;
			// Create temp files for Feature structure info and Interlinear
			m_sFSFile = Path.GetTempFileName();
			m_sInterFile = Path.GetTempFileName();

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			richTextBox1.Text = m_ksGrammarMessage;
			pnlRule.PreviewKeyDown += new PreviewKeyDownEventHandler(BrowserPreviewKeyDownHandler);

			InitToolBar();

			InitStatusBar();

			InitMenuItems();

			pnlTreeFeat.Controls.Add(pnlTree);
			pnlTreeFeat.Controls.Add(splitterBetweenTreeAndFeat);
			pnlTreeFeat.Controls.Add(pnlFeatureStructure);
			pnlTreeFeat.BorderStyle = BorderStyle.Fixed3D;

			pnlTree.Paint += new PaintEventHandler(TreePanelOnPaint);

			m_tree = new LingTreeTree();
			m_tree.m_LingTreeNodeClickedEvent += new LingTreeNodeClickedEventHandler(OnNodeClicked);
			pnlTree.Controls.Add(m_tree);

			m_treeTransform = new XslCompiledTransform();
			string beginPath = "";
			if (sStartupPath.Contains("x64"))
				beginPath = @"..\..\";
			m_sStartUpPath = Path.Combine(sStartupPath, beginPath);
			m_treeTransform.Load(Path.Combine(m_sStartUpPath, @"Transforms\PcPatrToLingTree.xsl"));

			m_fsTransform = new XslCompiledTransform();
			m_fsTransform.Load(Path.Combine(m_sStartUpPath, @"Transforms\ShowFS.xsl"));

			m_interlinearTransform = new XslCompiledTransform();
			m_interlinearTransform.Load(
				Path.Combine(m_sStartUpPath, @"Transforms\ShowInterlinear.xsl")
			);

			InitInterlinearBrowser();
			InitFeatureStructureBrowser();

			InitLanguageInfo();

			RetrieveRegistryInfo();

			EnableDisableItems();

			SetTitle();
			// set position on screen
			m_RectNormal = DesktopBounds;

			m_fNeedToSaveLanguageInfo = false;
		}

		public void AdjustUIForPcPatrFLEx()
		{
			miFileOpenAna.Visible = false;
			miFileOpenGrammar.Visible = false;
			miUseThisParse.Visible = true;
			miUseThisParseNextSentence.Visible = true;
			miCancelUsingThisParse.Visible = true;
			tbbtnOpenAnalysis.Visible = false;
		}

		private void InitLanguageInfo()
		{
			miUseThisParse.Visible = false;
			miUseThisParseNextSentence.Visible = false;
			miCancelUsingThisParse.Visible = false;
			MyFontInfo nt = new MyFontInfo(new Font("Times New Roman", 14.0f), Color.Black);
			MyFontInfo lex = new MyFontInfo(new Font("Courier New", 12.0f), Color.Blue);
			MyFontInfo gloss = new MyFontInfo(new Font("Times New Roman", 12.0f), Color.Green);
			m_language = new LanguageInfo("Default Language", nt, lex, gloss, false, "-");
		}

		private void InitMenuItems()
		{
#if DoesNotWorkForSomeReason
			miFileOpenAna.Shortcut = Shortcut.CtrlO;

			miSentenceFirst.Shortcut = Shortcut.CtrlF;
			miSentencePrevious.Shortcut = Shortcut.CtrlP;
			miSentenceNext.Shortcut = Shortcut.CtrlN;
			miSentenceLast.Shortcut = Shortcut.CtrlL;
			miSentenceGoTo.Shortcut = Shortcut.CtrlG;

			miParseFirst.Shortcut = Shortcut.CtrlShiftF;
			miParsePrevious.Shortcut = Shortcut.CtrlShiftP;
			miParseNext.Shortcut = Shortcut.CtrlShiftN;
			miParseLast.Shortcut = Shortcut.CtrlShiftL;
			miParserGoTo.Shortcut = Shortcut.CtrlShiftG;
#endif
		}

		private void InitToolBar()
		{
			tbbtnRefresh.Tag = miViewRefresh;
			tbbtnFirstParse.Tag = miParseFirst;
			tbbtnFirstSentence.Tag = miSentenceFirst;
			tbbtnLastParse.Tag = miParseLast;
			tbbtnLastSentence.Tag = miSentenceLast;
			tbbtnNextParse.Tag = miParseNext;
			tbbtnNextSentence.Tag = miSentenceNext;
			tbbtnOpenAnalysis.Tag = miFileOpenAna;
			tbbtnPreviousParse.Tag = miParsePrevious;
			tbbtnPreviousSentence.Tag = miSentencePrevious;
			tbbtnUseThisParse.Tag = miUseThisParse;
			tbbtnSaveForTreeTran.Tag = miSaveForTreeTran;
		}

		private void InitStatusBar()
		{
			sbStatusBar = new StatusBar();
			sbStatusBar.Parent = this;
			sbStatusBar.ShowPanels = true;

			sbpnlSentence = new StatusBarPanel();
			sbpnlSentence.Text = m_ksNoSentences;

			sbpnlParse = new StatusBarPanel();
			sbpnlParse.Text = m_ksNoParses;

			sbStatusBar.Panels.Add(sbpnlSentence);
			sbStatusBar.Panels.Add(sbpnlParse);
		}

		private void EnableDisableItems()
		{
			if (m_doc == null)
			{
				miParse.Enabled = false;
				miSentence.Enabled = false;
				miUseThisParse.Enabled = false;
				miUseThisParseNextSentence.Enabled = false;
				miCancelUsingThisParse.Enabled = false;
				tbbtnFirstParse.Enabled = false;
				tbbtnFirstSentence.Enabled = false;
				tbbtnLastParse.Enabled = false;
				tbbtnLastSentence.Enabled = false;
				tbbtnNextParse.Enabled = false;
				tbbtnNextSentence.Enabled = false;
				tbbtnOpenAnalysis.Enabled = true;
				tbbtnPreviousParse.Enabled = false;
				tbbtnPreviousSentence.Enabled = false;
				tbbtnUseThisParse.Enabled = false;
			}
			else
			{
				EnableDisableSentenceItems();
				EnableDisableParseItems();
				bool parsesExist = false;
				if (m_doc.CurrentSentence != null)
				{
					if (m_doc.CurrentSentence.NumberOfParses > 0)
						parsesExist = true;
				}
				miUseThisParse.Enabled = parsesExist;
				tbbtnUseThisParse.Enabled = parsesExist;
				miUseThisParseNextSentence.Enabled = parsesExist;
				miCancelUsingThisParse.Enabled = parsesExist;
			}
			if (m_tree == null)
				miViewRightToLeft.Enabled = false;
			else
				miViewRightToLeft.Enabled = true;
			if (miViewFeatStruct.Checked)
				pnlFeatureStructure.Visible = true;
			else
				pnlFeatureStructure.Visible = false;
			if (miViewInterlinear.Checked)
				pnlInterlinear.Visible = true;
			else
				pnlInterlinear.Visible = false;
			if (miViewRuleFile.Checked)
				pnlRule.Visible = true;
			else
				pnlRule.Visible = false;
			if (miViewStatusBar.Checked)
				sbStatusBar.Visible = true;
			else
				sbStatusBar.Visible = false;
			if (miViewToolBar.Checked)
				tbMain.Visible = true;
			else
				tbMain.Visible = false;
			tbbtnSaveForTreeTran.Pushed = miSaveForTreeTran.Checked;
		}

		private void EnableDisableSentenceItems()
		{
			miSentence.Enabled = true;
			if (m_doc.CurrentSentenceNumber == 1)
			{
				miSentenceFirst.Enabled = false;
				miSentencePrevious.Enabled = false;
				tbbtnFirstSentence.Enabled = false;
				tbbtnPreviousSentence.Enabled = false;
			}
			else
			{
				miSentenceFirst.Enabled = true;
				miSentencePrevious.Enabled = true;
				tbbtnFirstSentence.Enabled = true;
				tbbtnPreviousSentence.Enabled = true;
			}
			if (m_doc.CurrentSentenceNumber == m_doc.NumberOfSentences)
			{
				miSentenceNext.Enabled = false;
				miSentenceLast.Enabled = false;
				tbbtnNextSentence.Enabled = false;
				tbbtnLastSentence.Enabled = false;
			}
			else
			{
				miSentenceNext.Enabled = true;
				miSentenceLast.Enabled = true;
				tbbtnNextSentence.Enabled = true;
				tbbtnLastSentence.Enabled = true;
			}
		}

		private void EnableDisableParseItems()
		{
			if (m_doc.CurrentSentence.NumberOfParses == 0)
			{
				miParse.Enabled = false;
				EnableOrDisableSetFirstPreviousParse(false);
				EnableOrDisableSetNextLastParse(false);
			}
			else
			{
				miParse.Enabled = true;
				if (m_doc.CurrentSentence.CurrentParseNumber == 1)
					EnableOrDisableSetFirstPreviousParse(false);
				else
					EnableOrDisableSetFirstPreviousParse(true);
				if (
					m_doc.CurrentSentence.CurrentParseNumber == m_doc.CurrentSentence.NumberOfParses
				)
					EnableOrDisableSetNextLastParse(false);
				else
					EnableOrDisableSetNextLastParse(true);
			}
		}

		/// <summary>
		/// Either disable or enable the parse first/previous menu/toolbar items
		/// </summary>
		/// <param name="bValue">true to enable; false to diable</param>
		private void EnableOrDisableSetFirstPreviousParse(bool bValue)
		{
			miParseFirst.Enabled = bValue;
			miParsePrevious.Enabled = bValue;
			tbbtnFirstParse.Enabled = bValue;
			tbbtnPreviousParse.Enabled = bValue;
		}

		/// <summary>
		/// Either disable or enable the parse next/last menu/toolbar items
		/// </summary>
		/// <param name="bValue">true to enable; false to diable</param>
		private void EnableOrDisableSetNextLastParse(bool bValue)
		{
			miParseNext.Enabled = bValue;
			tbbtnLastParse.Enabled = bValue;
			tbbtnNextParse.Enabled = bValue;
			miParseLast.Enabled = bValue;
		}

		private void InitBrowser(WebBrowser browser)
		{
			//browser.AddressBar = false;
			//browser.Border3D = true;
			//browser.DisableBackSpace = true;
			//browser.DisableCtrlF = true;
			//browser.DisableCtrlN = true;
			//browser.DisableCtrlP = true;
			browser.Dock = System.Windows.Forms.DockStyle.Fill;
			//browser.EnableHtmlDocumentEventHandling = false;
			browser.IsWebBrowserContextMenuEnabled = false;
			//browser.FlatScrollBar = false;
			//browser.FullScreen = false;
			browser.Location = new System.Drawing.Point(176, 28);
			//browser.Offline = true;
			//browser.OpenInNewWindow = false;
			//browser.Options = Sloppycode.Controls.BrowserOptions.Images;
			//browser.RegisterAsBrowser = false;
			//browser.RegisterAsDropTarget = false;
			//browser.ScrollBarsVisible = true;
			browser.ScrollBarsEnabled = true;
			//browser.ShowWebsiteInDesigner = false;
			//browser.Silent = false;
			browser.Size = new System.Drawing.Size(552, 388);
			browser.TabIndex = 6;
			//browser.TheaterMode = false;
			browser.Url = null;
			browser.PreviewKeyDown += new PreviewKeyDownEventHandler(BrowserPreviewKeyDownHandler);
			//browser.XPThemed = false;
		}

		private void BrowserPreviewKeyDownHandler(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.Control && !e.Shift)
			{
				switch (e.KeyCode)
				{
					case Keys.Down:
						miSentenceNext_Click(sender, e);
						break;
					case Keys.Up:
						miSentencePrevious_Click(sender, e);
						break;
					case Keys.Right:
						miParseNext_Click(sender, e);
						break;
					case Keys.Left:
						miParsePrevious_Click(sender, e);
						break;
					case Keys.OemQuestion: // fall through
					case Keys.L:
						miUseThisParse_Click(sender, e);
						break;
				}
			}
			else if (e.Control && e.Shift && e.KeyCode == Keys.Down)
			{
				miUseThisParseNextSentence_Click(sender, e);
			}
		}

		private void InitFeatureStructureBrowser()
		{
			wbFeatureStructure = new WebBrowser();
			InitBrowser(wbFeatureStructure);
			wbFeatureStructure.Name = "wbFeatureStructure";
			pnlFeatureStructure.Controls.Add(wbFeatureStructure);
			m_sInitFeatureMessagePath = Path.Combine(m_sStartUpPath, @"Transforms\InitFeature.htm");
			wbFeatureStructure.Navigate(m_sInitFeatureMessagePath);
		}

		private void InitInterlinearBrowser()
		{
			wbInterlinear = new WebBrowser();
			InitBrowser(wbInterlinear);
			wbInterlinear.Name = "wbInterlinear";
			pnlInterlinear.Controls.Add(wbInterlinear);
			m_sInitInterlinearMessagePath = Path.Combine(
				m_sStartUpPath,
				@"Transforms\InitInterlinear.htm"
			);
			wbInterlinear.Navigate(m_sInitInterlinearMessagePath);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources =
				new System.ComponentModel.ComponentResourceManager(typeof(PcPatrBrowserApp));
			this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
			this.miFile = new System.Windows.Forms.MenuItem();
			this.miFileOpenAna = new System.Windows.Forms.MenuItem();
			this.menuItem7 = new System.Windows.Forms.MenuItem();
			this.miFileOpenGrammar = new System.Windows.Forms.MenuItem();
			this.menuItem10 = new System.Windows.Forms.MenuItem();
			this.miFileOpenLanguage = new System.Windows.Forms.MenuItem();
			this.miFileSaveLanguage = new System.Windows.Forms.MenuItem();
			this.miFileSaveLangAs = new System.Windows.Forms.MenuItem();
			this.miSaveForTreeTran = new System.Windows.Forms.MenuItem();
			this.menuItem9 = new System.Windows.Forms.MenuItem();
			this.miExit = new System.Windows.Forms.MenuItem();
			this.miView = new System.Windows.Forms.MenuItem();
			this.miViewFeatStruct = new System.Windows.Forms.MenuItem();
			this.miViewInterlinear = new System.Windows.Forms.MenuItem();
			this.miViewRuleFile = new System.Windows.Forms.MenuItem();
			this.miViewStatusBar = new System.Windows.Forms.MenuItem();
			this.miViewToolBar = new System.Windows.Forms.MenuItem();
			this.menuItem5 = new System.Windows.Forms.MenuItem();
			this.miViewRightToLeft = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.miViewRefresh = new System.Windows.Forms.MenuItem();
			this.miSentence = new System.Windows.Forms.MenuItem();
			this.miSentenceFirst = new System.Windows.Forms.MenuItem();
			this.miSentencePrevious = new System.Windows.Forms.MenuItem();
			this.miSentenceNext = new System.Windows.Forms.MenuItem();
			this.miSentenceLast = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.miSentenceGoTo = new System.Windows.Forms.MenuItem();
			this.miParse = new System.Windows.Forms.MenuItem();
			this.miParseFirst = new System.Windows.Forms.MenuItem();
			this.miParsePrevious = new System.Windows.Forms.MenuItem();
			this.miParseNext = new System.Windows.Forms.MenuItem();
			this.miParseLast = new System.Windows.Forms.MenuItem();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.miParserGoTo = new System.Windows.Forms.MenuItem();
			this.miUseThisParse = new System.Windows.Forms.MenuItem();
			this.miUseThisParseNextSentence = new System.Windows.Forms.MenuItem();
			this.miCancelUsingThisParse = new System.Windows.Forms.MenuItem();
			this.miLanguage = new System.Windows.Forms.MenuItem();
			this.pnlTreeFeat = new System.Windows.Forms.Panel();
			this.splitterBetweenTreeFeatAndRule = new System.Windows.Forms.Splitter();
			this.pnlRule = new System.Windows.Forms.Panel();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.splitterBetweenInterlinearAndTreeFeat = new System.Windows.Forms.Splitter();
			this.pnlInterlinear = new System.Windows.Forms.Panel();
			this.pnlTree = new System.Windows.Forms.Panel();
			this.splitterBetweenTreeAndFeat = new System.Windows.Forms.Splitter();
			this.pnlFeatureStructure = new System.Windows.Forms.Panel();
			this.tbMain = new System.Windows.Forms.ToolBar();
			this.tbbtnOpenAnalysis = new System.Windows.Forms.ToolBarButton();
			this.tbbtnSeparator1 = new System.Windows.Forms.ToolBarButton();
			this.tbbtnSeparator2 = new System.Windows.Forms.ToolBarButton();
			this.tbbtnRefresh = new System.Windows.Forms.ToolBarButton();
			this.tbbtbSeparator4 = new System.Windows.Forms.ToolBarButton();
			this.tbbtbSeparator5 = new System.Windows.Forms.ToolBarButton();
			this.tbbtnFirstSentence = new System.Windows.Forms.ToolBarButton();
			this.tbbtnPreviousSentence = new System.Windows.Forms.ToolBarButton();
			this.tbbtnNextSentence = new System.Windows.Forms.ToolBarButton();
			this.tbbtnLastSentence = new System.Windows.Forms.ToolBarButton();
			this.tbbtnSeparator = new System.Windows.Forms.ToolBarButton();
			this.tbbtnSeparator3 = new System.Windows.Forms.ToolBarButton();
			this.tbbtnFirstParse = new System.Windows.Forms.ToolBarButton();
			this.tbbtnPreviousParse = new System.Windows.Forms.ToolBarButton();
			this.tbbtnNextParse = new System.Windows.Forms.ToolBarButton();
			this.tbbtnLastParse = new System.Windows.Forms.ToolBarButton();
			this.tbbtnUseThisParse = new System.Windows.Forms.ToolBarButton();
			this.tbbtnSeparator6 = new System.Windows.Forms.ToolBarButton();
			this.tbbtnSaveForTreeTran = new System.Windows.Forms.ToolBarButton();
			this.ilToolBar = new System.Windows.Forms.ImageList(this.components);
			this.pnlRule.SuspendLayout();
			this.SuspendLayout();
			//
			// mainMenu1
			//
			this.mainMenu1.MenuItems.AddRange(
				new System.Windows.Forms.MenuItem[]
				{
					this.miFile,
					this.miView,
					this.miSentence,
					this.miParse,
					this.miLanguage
				}
			);
			//
			// miFile
			//
			this.miFile.Index = 0;
			this.miFile.MenuItems.AddRange(
				new System.Windows.Forms.MenuItem[]
				{
					this.miFileOpenAna,
					this.menuItem7,
					this.miFileOpenGrammar,
					this.menuItem10,
					this.miFileOpenLanguage,
					this.miFileSaveLanguage,
					this.miFileSaveLangAs,
					this.miSaveForTreeTran,
					this.menuItem9,
					this.miExit
				}
			);
			this.miFile.Text = "File";
			//
			// miFileOpenAna
			//
			this.miFileOpenAna.Index = 0;
			this.miFileOpenAna.Text = "&Open Log or Analysis file...";
			this.miFileOpenAna.Click += new System.EventHandler(this.FileOpenAna_Click);
			//
			// menuItem7
			//
			this.menuItem7.Index = 1;
			this.menuItem7.Text = "-";
			//
			// miFileOpenGrammar
			//
			this.miFileOpenGrammar.Index = 2;
			this.miFileOpenGrammar.Text = "Open &Grammar File...";
			this.miFileOpenGrammar.Click += new System.EventHandler(this.FileOpenGrammar_Click);
			//
			// menuItem10
			//
			this.menuItem10.Index = 3;
			this.menuItem10.Text = "-";
			//
			// miFileOpenLanguage
			//
			this.miFileOpenLanguage.Index = 4;
			this.miFileOpenLanguage.Text = "Open &Language File...";
			this.miFileOpenLanguage.Click += new System.EventHandler(this.miFileOpenLanguage_Click);
			//
			// miFileSaveLanguage
			//
			this.miFileSaveLanguage.Index = 5;
			this.miFileSaveLanguage.Text = "&Save Language File...";
			this.miFileSaveLanguage.Click += new System.EventHandler(this.miFileSaveLanguage_Click);
			//
			// miFileSaveLangAs
			//
			this.miFileSaveLangAs.Index = 6;
			this.miFileSaveLangAs.Text = "Save Language File &As...";
			this.miFileSaveLangAs.Click += new System.EventHandler(this.miFileSaveLangAs_Click);
			//
			// miSaveForTreeTran
			//
			this.miSaveForTreeTran.Checked = false;
			this.miSaveForTreeTran.Index = 7;
			this.miSaveForTreeTran.Text = "Save for TreeTran";
			this.miSaveForTreeTran.Click += new System.EventHandler(this.miSaveForTreeTran_Click);
			//
			// menuItem9
			//
			this.menuItem9.Index = 8;
			this.menuItem9.Text = "-";
			//
			// miExit
			//
			this.miExit.Index = 9;
			this.miExit.Text = "E&xit";
			this.miExit.Click += new System.EventHandler(this.miExit_Click);
			//
			// miView
			//
			this.miView.Index = 1;
			this.miView.MenuItems.AddRange(
				new System.Windows.Forms.MenuItem[]
				{
					this.miViewFeatStruct,
					this.miViewInterlinear,
					this.miViewRuleFile,
					this.miViewStatusBar,
					this.miViewToolBar,
					this.menuItem5,
					this.miViewRightToLeft,
					this.menuItem2,
					this.miViewRefresh
				}
			);
			this.miView.Text = "View";
			//
			// miViewFeatStruct
			//
			this.miViewFeatStruct.Checked = true;
			this.miViewFeatStruct.Index = 0;
			this.miViewFeatStruct.Text = "&Feature Structure Information";
			this.miViewFeatStruct.Click += new System.EventHandler(this.miViewFeatStruct_Click);
			//
			// miViewInterlinear
			//
			this.miViewInterlinear.Checked = true;
			this.miViewInterlinear.Index = 1;
			this.miViewInterlinear.Text = "Interlinear";
			this.miViewInterlinear.Click += new System.EventHandler(this.miViewInterlinear_Click);
			//
			// miViewRuleFile
			//
			this.miViewRuleFile.Checked = true;
			this.miViewRuleFile.Index = 2;
			this.miViewRuleFile.Text = "&Rule File";
			this.miViewRuleFile.Click += new System.EventHandler(this.miViewRuleFile_Click);
			//
			// miViewStatusBar
			//
			this.miViewStatusBar.Checked = true;
			this.miViewStatusBar.Index = 3;
			this.miViewStatusBar.Text = "&Status Bar";
			this.miViewStatusBar.Click += new System.EventHandler(this.miViewStatusBar_Click);
			//
			// miViewToolBar
			//
			this.miViewToolBar.Checked = true;
			this.miViewToolBar.Index = 4;
			this.miViewToolBar.Text = "&Tool Bar";
			this.miViewToolBar.Click += new System.EventHandler(this.miViewToolBar_Click);
			//
			// menuItem5
			//
			this.menuItem5.Index = 5;
			this.menuItem5.Text = "-";
			//
			// miViewRightToLeft
			//
			this.miViewRightToLeft.Index = 6;
			this.miViewRightToLeft.Text = "Right-to-left Orientation";
			this.miViewRightToLeft.Click += new System.EventHandler(this.miViewRightToLeft_Click);
			//
			// menuItem2
			//
			this.menuItem2.Index = 7;
			this.menuItem2.Text = "-";
			//
			// miViewRefresh
			//
			this.miViewRefresh.Enabled = false;
			this.miViewRefresh.Index = 8;
			this.miViewRefresh.Text = "Refresh";
			this.miViewRefresh.Click += new System.EventHandler(this.miViewRefresh_Click);
			//
			// miSentence
			//
			this.miSentence.Index = 2;
			this.miSentence.MenuItems.AddRange(
				new System.Windows.Forms.MenuItem[]
				{
					this.miSentenceFirst,
					this.miSentencePrevious,
					this.miSentenceNext,
					this.miSentenceLast,
					this.menuItem3,
					this.miSentenceGoTo
				}
			);
			this.miSentence.Text = "Sentence";
			//
			// miSentenceFirst
			//
			this.miSentenceFirst.Index = 0;
			this.miSentenceFirst.Text = "&First Sentence";
			this.miSentenceFirst.Click += new System.EventHandler(this.miSentenceFirst_Click);
			//
			// miSentencePrevious
			//
			this.miSentencePrevious.Index = 1;
			this.miSentencePrevious.Text = "&Previous Sentence";
			this.miSentencePrevious.Click += new System.EventHandler(this.miSentencePrevious_Click);
			//
			// miSentenceNext
			//
			this.miSentenceNext.Index = 2;
			this.miSentenceNext.Text = "&Next Sentence";
			this.miSentenceNext.Click += new System.EventHandler(this.miSentenceNext_Click);
			//
			// miSentenceLast
			//
			this.miSentenceLast.Index = 3;
			this.miSentenceLast.Text = "&Last Sentence";
			this.miSentenceLast.Click += new System.EventHandler(this.miSentenceLast_Click);
			//
			// menuItem3
			//
			this.menuItem3.Index = 4;
			this.menuItem3.Text = "-";
			//
			// miSentenceGoTo
			//
			this.miSentenceGoTo.Index = 5;
			this.miSentenceGoTo.Text = "&Go to Sentence...";
			this.miSentenceGoTo.Click += new System.EventHandler(this.miSentenceGoTo_Click);
			//
			// miParse
			//
			this.miParse.Index = 3;
			this.miParse.MenuItems.AddRange(
				new System.Windows.Forms.MenuItem[]
				{
					this.miParseFirst,
					this.miParsePrevious,
					this.miParseNext,
					this.miParseLast,
					this.menuItem4,
					this.miParserGoTo,
					this.miUseThisParse,
					this.miUseThisParseNextSentence,
					this.miCancelUsingThisParse
				}
			);
			this.miParse.Text = "Parse";
			//
			// miParseFirst
			//
			this.miParseFirst.Index = 0;
			this.miParseFirst.Text = "&First Parse";
			this.miParseFirst.Click += new System.EventHandler(this.miParseFirst_Click);
			//
			// miParsePrevious
			//
			this.miParsePrevious.Index = 1;
			this.miParsePrevious.Text = "&Previous Parse";
			this.miParsePrevious.Click += new System.EventHandler(this.miParsePrevious_Click);
			//
			// miParseNext
			//
			this.miParseNext.Index = 2;
			this.miParseNext.Text = "&Next Parse";
			this.miParseNext.Click += new System.EventHandler(this.miParseNext_Click);
			//
			// miParseLast
			//
			this.miParseLast.Index = 3;
			this.miParseLast.Text = "&Last Parse";
			this.miParseLast.Click += new System.EventHandler(this.miParseLast_Click);
			//
			// menuItem4
			//
			this.menuItem4.Index = 4;
			this.menuItem4.Text = "-";
			//
			// miParserGoTo
			//
			this.miParserGoTo.Index = 5;
			this.miParserGoTo.Text = "&Go to Parse";
			this.miParserGoTo.Click += new System.EventHandler(this.miParserGoTo_Click);
			//
			// miUseThisParse
			//
			this.miUseThisParse.Index = 6;
			this.miUseThisParse.Text = "&Use This Parse";
			this.miUseThisParse.Click += new System.EventHandler(this.miUseThisParse_Click);
			//
			// miUseThisParseNextSentence
			//
			this.miUseThisParseNextSentence.Index = 7;
			this.miUseThisParseNextSentence.Text = "Use This Parse && Go to Ne&xt Sentence";
			this.miUseThisParseNextSentence.Click += new System.EventHandler(
				this.miUseThisParseNextSentence_Click
			);
			//
			// miCancelUsingThisParse
			//
			this.miCancelUsingThisParse.Index = 8;
			this.miCancelUsingThisParse.Text = "&Cancel Using This Parse";
			this.miCancelUsingThisParse.Click += new System.EventHandler(
				this.miCancelUsingThisParse_Click
			);
			//
			// miLanguage
			//
			this.miLanguage.Index = 4;
			this.miLanguage.Text = "&Language";
			this.miLanguage.Click += new System.EventHandler(this.miLanguage_Click);
			//
			// pnlTreeFeat
			//
			this.pnlTreeFeat.AutoScroll = true;
			this.pnlTreeFeat.BackColor = System.Drawing.SystemColors.Window;
			this.pnlTreeFeat.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnlTreeFeat.Location = new System.Drawing.Point(0, 176);
			this.pnlTreeFeat.Name = "pnlTreeFeat";
			this.pnlTreeFeat.Size = new System.Drawing.Size(592, 125);
			this.pnlTreeFeat.TabIndex = 7;
			//
			// splitterBetweenTreeFeatAndRule
			//
			this.splitterBetweenTreeFeatAndRule.BackColor = System
				.Drawing
				.SystemColors
				.ActiveBorder;
			this.splitterBetweenTreeFeatAndRule.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.splitterBetweenTreeFeatAndRule.Location = new System.Drawing.Point(0, 301);
			this.splitterBetweenTreeFeatAndRule.Name = "splitterBetweenTreeFeatAndRule";
			this.splitterBetweenTreeFeatAndRule.Size = new System.Drawing.Size(592, 2);
			this.splitterBetweenTreeFeatAndRule.TabIndex = 8;
			this.splitterBetweenTreeFeatAndRule.TabStop = false;
			//
			// pnlRule
			//
			this.pnlRule.AutoScroll = true;
			this.pnlRule.BackColor = System.Drawing.SystemColors.Window;
			this.pnlRule.Controls.Add(this.richTextBox1);
			this.pnlRule.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlRule.Location = new System.Drawing.Point(0, 303);
			this.pnlRule.Name = "pnlRule";
			this.pnlRule.Size = new System.Drawing.Size(592, 146);
			this.pnlRule.TabIndex = 7;
			//
			// richTextBox1
			//
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Location = new System.Drawing.Point(0, 0);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ReadOnly = true;
			this.richTextBox1.Size = new System.Drawing.Size(592, 146);
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.Text = "";
			//
			// splitterBetweenInterlinearAndTreeFeat
			//
			this.splitterBetweenInterlinearAndTreeFeat.BackColor = System
				.Drawing
				.SystemColors
				.ActiveBorder;
			this.splitterBetweenInterlinearAndTreeFeat.Dock = System.Windows.Forms.DockStyle.Top;
			this.splitterBetweenInterlinearAndTreeFeat.Location = new System.Drawing.Point(0, 174);
			this.splitterBetweenInterlinearAndTreeFeat.Name =
				"splitterBetweenInterlinearAndTreeFeat";
			this.splitterBetweenInterlinearAndTreeFeat.Size = new System.Drawing.Size(592, 2);
			this.splitterBetweenInterlinearAndTreeFeat.TabIndex = 8;
			this.splitterBetweenInterlinearAndTreeFeat.TabStop = false;
			//
			// pnlInterlinear
			//
			this.pnlInterlinear.AutoScroll = true;
			this.pnlInterlinear.BackColor = System.Drawing.SystemColors.Window;
			this.pnlInterlinear.Dock = System.Windows.Forms.DockStyle.Top;
			this.pnlInterlinear.Location = new System.Drawing.Point(0, 28);
			this.pnlInterlinear.Name = "pnlInterlinear";
			this.pnlInterlinear.Size = new System.Drawing.Size(592, 146);
			this.pnlInterlinear.TabIndex = 7;
			//
			// pnlTree
			//
			this.pnlTree.AutoScroll = true;
			this.pnlTree.BackColor = System.Drawing.SystemColors.Window;
			this.pnlTree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnlTree.Location = new System.Drawing.Point(0, 0);
			this.pnlTree.Name = "pnlTree";
			this.pnlTree.Size = new System.Drawing.Size(400, 569);
			this.pnlTree.TabIndex = 7;
			//
			// splitterBetweenTreeAndFeat
			//
			this.splitterBetweenTreeAndFeat.BackColor = System.Drawing.SystemColors.ActiveBorder;
			this.splitterBetweenTreeAndFeat.Dock = System.Windows.Forms.DockStyle.Right;
			this.splitterBetweenTreeAndFeat.Location = new System.Drawing.Point(0, 0);
			this.splitterBetweenTreeAndFeat.Name = "splitterBetweenTreeAndFeat";
			this.splitterBetweenTreeAndFeat.Size = new System.Drawing.Size(3, 1);
			this.splitterBetweenTreeAndFeat.TabIndex = 8;
			this.splitterBetweenTreeAndFeat.TabStop = false;
			//
			// pnlFeatureStructure
			//
			this.pnlFeatureStructure.AutoScroll = true;
			this.pnlFeatureStructure.BackColor = System.Drawing.SystemColors.Control;
			this.pnlFeatureStructure.Dock = System.Windows.Forms.DockStyle.Right;
			this.pnlFeatureStructure.Location = new System.Drawing.Point(0, 481);
			this.pnlFeatureStructure.Name = "pnlFeatureStructure";
			this.pnlFeatureStructure.Size = new System.Drawing.Size(192, 569);
			this.pnlFeatureStructure.TabIndex = 0;
			//
			// tbMain
			//
			this.tbMain.Buttons.AddRange(
				new System.Windows.Forms.ToolBarButton[]
				{
					this.tbbtnOpenAnalysis,
					this.tbbtnSeparator1,
					this.tbbtnSeparator2,
					this.tbbtnRefresh,
					this.tbbtbSeparator4,
					this.tbbtbSeparator5,
					this.tbbtnFirstSentence,
					this.tbbtnPreviousSentence,
					this.tbbtnNextSentence,
					this.tbbtnLastSentence,
					this.tbbtnSeparator,
					this.tbbtnSeparator3,
					this.tbbtnFirstParse,
					this.tbbtnPreviousParse,
					this.tbbtnNextParse,
					this.tbbtnLastParse,
					this.tbbtnUseThisParse,
					this.tbbtnSeparator6,
					this.tbbtnSaveForTreeTran
				}
			);
			this.tbMain.DropDownArrows = true;
			this.tbMain.ImageList = this.ilToolBar;
			this.tbMain.Location = new System.Drawing.Point(0, 0);
			this.tbMain.Name = "tbMain";
			this.tbMain.ShowToolTips = true;
			this.tbMain.Size = new System.Drawing.Size(592, 28);
			this.tbMain.TabIndex = 0;
			this.tbMain.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(
				this.tbMain_ButtonClick
			);
			//
			// tbbtnOpenAnalysis
			//
			this.tbbtnOpenAnalysis.ImageIndex = 8;
			this.tbbtnOpenAnalysis.Name = "tbbtnOpenAnalysis";
			this.tbbtnOpenAnalysis.ToolTipText = "Open Log or Analysis File";
			//
			// tbbtnSeparator1
			//
			this.tbbtnSeparator1.Name = "tbbtnSeparator1";
			this.tbbtnSeparator1.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbtnSeparator2
			//
			this.tbbtnSeparator2.Name = "tbbtnSeparator2";
			this.tbbtnSeparator2.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbtnRefresh
			//
			this.tbbtnRefresh.Enabled = false;
			this.tbbtnRefresh.ImageIndex = 9;
			this.tbbtnRefresh.Name = "tbbtnRefresh";
			this.tbbtnRefresh.ToolTipText = "Refresh";
			//
			// tbbtbSeparator4
			//
			this.tbbtbSeparator4.Name = "tbbtbSeparator4";
			this.tbbtbSeparator4.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbtbSeparator5
			//
			this.tbbtbSeparator5.Name = "tbbtbSeparator5";
			this.tbbtbSeparator5.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbtnFirstSentence
			//
			this.tbbtnFirstSentence.ImageIndex = 0;
			this.tbbtnFirstSentence.Name = "tbbtnFirstSentence";
			this.tbbtnFirstSentence.ToolTipText = "First Sentence";
			//
			// tbbtnPreviousSentence
			//
			this.tbbtnPreviousSentence.ImageIndex = 1;
			this.tbbtnPreviousSentence.Name = "tbbtnPreviousSentence";
			this.tbbtnPreviousSentence.ToolTipText = "Previous Sentence";
			//
			// tbbtnNextSentence
			//
			this.tbbtnNextSentence.ImageIndex = 2;
			this.tbbtnNextSentence.Name = "tbbtnNextSentence";
			this.tbbtnNextSentence.ToolTipText = "Next Sentence";
			//
			// tbbtnLastSentence
			//
			this.tbbtnLastSentence.ImageIndex = 3;
			this.tbbtnLastSentence.Name = "tbbtnLastSentence";
			this.tbbtnLastSentence.ToolTipText = "Last Sentence";
			//
			// tbbtnSeparator
			//
			this.tbbtnSeparator.Name = "tbbtnSeparator";
			this.tbbtnSeparator.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbtnSeparator3
			//
			this.tbbtnSeparator3.Name = "tbbtnSeparator3";
			this.tbbtnSeparator3.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbtnFirstParse
			//
			this.tbbtnFirstParse.ImageIndex = 4;
			this.tbbtnFirstParse.Name = "tbbtnFirstParse";
			this.tbbtnFirstParse.ToolTipText = "First Parse";
			//
			// tbbtnPreviousParse
			//
			this.tbbtnPreviousParse.ImageIndex = 5;
			this.tbbtnPreviousParse.Name = "tbbtnPreviousParse";
			this.tbbtnPreviousParse.ToolTipText = "Previous Parse";
			//
			// tbbtnNextParse
			//
			this.tbbtnNextParse.ImageIndex = 6;
			this.tbbtnNextParse.Name = "tbbtnNextParse";
			this.tbbtnNextParse.ToolTipText = "Next Parse";
			//
			// tbbtnLastParse
			//
			this.tbbtnLastParse.ImageIndex = 7;
			this.tbbtnLastParse.Name = "tbbtnLastParse";
			this.tbbtnLastParse.ToolTipText = "Last Parse";
			//
			// tbbtnUseThisParse
			//
			this.tbbtnUseThisParse.ImageIndex = 10;
			this.tbbtnUseThisParse.Name = "tbbtnUseThisParse";
			this.tbbtnUseThisParse.ToolTipText = "Use This Parse";
			//
			// tbbtnSeparator6
			//
			this.tbbtnSeparator6.Name = "tbbtnSeparator6";
			this.tbbtnSeparator6.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			//
			// tbbtnSaveForTreeTran
			//
			this.tbbtnSaveForTreeTran.ImageIndex = 11;
			this.tbbtnSaveForTreeTran.Name = "tbbtnSaveForTreeTran";
			this.tbbtnSaveForTreeTran.Style = System.Windows.Forms.ToolBarButtonStyle.ToggleButton;
			this.tbbtnSaveForTreeTran.ToolTipText = "Save for TreeTran";
			//
			// ilToolBar
			//
			this.ilToolBar.ImageStream = (
				(System.Windows.Forms.ImageListStreamer)(
					resources.GetObject("ilToolBar.ImageStream")
				)
			);
			this.ilToolBar.TransparentColor = System.Drawing.Color.Fuchsia;
			this.ilToolBar.Images.SetKeyName(0, "");
			this.ilToolBar.Images.SetKeyName(1, "");
			this.ilToolBar.Images.SetKeyName(2, "");
			this.ilToolBar.Images.SetKeyName(3, "");
			this.ilToolBar.Images.SetKeyName(4, "");
			this.ilToolBar.Images.SetKeyName(5, "");
			this.ilToolBar.Images.SetKeyName(6, "");
			this.ilToolBar.Images.SetKeyName(7, "");
			this.ilToolBar.Images.SetKeyName(8, "");
			this.ilToolBar.Images.SetKeyName(9, "Refresh.bmp");
			this.ilToolBar.Images.SetKeyName(10, "yes.png");
			this.ilToolBar.Images.SetKeyName(11, "TreeTranOutput.png");
			//
			// PcPatrBrowserApp
			//
			//this.AutoScaleBaseSize = new System.Drawing.Size(8, 19);
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(592, 449);
			this.Controls.Add(this.pnlTreeFeat);
			this.Controls.Add(this.splitterBetweenInterlinearAndTreeFeat);
			this.Controls.Add(this.pnlInterlinear);
			this.Controls.Add(this.tbMain);
			this.Controls.Add(this.splitterBetweenTreeFeatAndRule);
			this.Controls.Add(this.pnlRule);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Menu = this.mainMenu1;
			this.Name = "PcPatrBrowserApp";
			this.Text = "PC-PATR Browser";
			this.pnlRule.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			Application.Run(new PcPatrBrowserApp());
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main(string[] rgArgs)
		{
			if (rgArgs.Length > 1)
			{
				string startupDirectory = rgArgs[0];
				string andFile = rgArgs[1];
				if (!Directory.Exists(startupDirectory))
				{
					Console.WriteLine("Could not find directory '" + startupDirectory + "'");
				}
				else
				{
					if (!File.Exists(andFile))
					{
						Console.WriteLine("Could not find file '" + andFile + "'");
					}
					else
					{
						PcPatrBrowserApp browserApp = new PcPatrBrowserApp(startupDirectory);
						browserApp.LoadAnaFile(andFile);
						Application.Run(browserApp);
						//Application.Run(new PcPatrBrowserApp(rgArgs[0]));
					}
				}
			}
			else
			{
				Console.WriteLine(
					"Usage: SIL.PcPatrBrowserExe 'FLEx program directory' 'Invoker.and file'"
				);
			}
		}

#if ItWouldBeNiceToHaveInsteadButICouldNotGetItToWorkAndAmNotGoingToSpendMoreTimeOnItNow
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Application entry point. If PcPatrBrowserApp isn't already running,
		/// an instance of the app is created.
		/// </summary>
		/// <param name="rgArgs">Command-line arguments</param>
		/// <returns>0</returns>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			// Create a semaphore to keep more than one instance of the application
			// from running at the same time.  If the semaphore is signalled, then
			// this instance can run.
			Win32.SecurityAttributes sa = new Win32.SecurityAttributes();
			IntPtr semaphore = Win32.CreateSemaphore(
				ref sa,
				1,
				1,
				Process.GetCurrentProcess().MainModule.ModuleName
			);
			switch (Win32.WaitForSingleObject(semaphore, 0))
			{
				case Win32.WAIT_OBJECT_0:
					// Using the 'using' gizmo will call Dispose on app,
					// which in turn will call Dispose for all FdoCache objects,
					// which will release all of the COM objects it connects to.
					using (PcPatrBrowserApp application = new PcPatrBrowserApp(rgArgs))
					{
						application.Run();
					}
					int previousCount;
					Win32.ReleaseSemaphore(semaphore, 1, out previousCount);
					break;

				case Win32.WAIT_TIMEOUT:
					// If the semaphore wait times out then another instance is running.
					// Try to get a handle to its window and activate it.  Then terminate
					// this process.
					try
					{
						IntPtr hWndMain = ExistingProcess.MainWindowHandle;
						if (hWndMain != (IntPtr)0)
							Win32.SetForegroundWindow(hWndMain);
					}
					catch
					{
						// The other instance does not have a window handle.  It is either in
						// the process of starting up or shutting down.
					}
					break;
			}

			return 0;
		}
#endif

		private void FileOpenAna_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			if (m_sLogOrAnaFileName != null && m_sLogOrAnaFileName.Length > 0)
			{
				dlg.InitialDirectory = Path.GetDirectoryName(m_sLogOrAnaFileName);
				dlg.FileName = m_sLogOrAnaFileName;
			}
			dlg.Filter =
				"Log file (*.log)|*.log|" + "Analysis file (*.an*)|*.an*|" + "All Files (*.*)|*.*";
			;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				m_sLogOrAnaFileName = dlg.FileName;
				miViewRefresh.Enabled = true;
				tbbtnRefresh.Enabled = true;
				LoadAnaFile();
			}
		}

		public void LoadAnaFile(String anaFile)
		{
			m_sLogOrAnaFileName = anaFile;
			LoadAnaFile();
		}

		private void LoadAnaFile()
		{
			Cursor.Current = Cursors.WaitCursor;
			string sGrammarFile;
			m_doc = new PcPatrDocument(m_sLogOrAnaFileName, out sGrammarFile);
			PropertiesChosen = new String[m_doc.NumberOfSentences];
			ParsesChosen = new int[m_doc.NumberOfSentences];
			if (sGrammarFile != null)
			{
				m_sGrammarFileName = sGrammarFile;
				LoadGrammarFile();
			}
			else
			{
				richTextBox1.Text = m_ksGrammarMessage;
			}

			PcPatrSentence sent = m_doc.CurrentSentence;
			if (sent != null)
			{
				ShowParseTree(sent, sent.FirstParse);
			}
			else
			{
				m_tree.Root = null;
				m_tree.Invalidate();
				wbFeatureStructure.Navigate(m_sInitFeatureMessagePath);
				sbpnlSentence.Text = m_ksNoSentences;
				sbpnlParse.Text = m_ksNoParses;
			}
			ShowInterlinear(sent);
			Cursor.Current = Cursors.Arrow;
			SetTitle();
		}

		private void FileOpenGrammar_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Grammar rule file (*.grm)|*.grm|" + "All Files (*.*)|*.*";
			;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				m_sGrammarFileName = dlg.FileName;
				LoadGrammarFile();
			}
		}

		private void LoadGrammarFile()
		{
			StreamReader sr = new StreamReader(m_sGrammarFileName);
			richTextBox1.Text = sr.ReadToEnd();
			sr.Close();
		}

		private void ShowInterlinear(PcPatrSentence sentence)
		{
			if (sentence == null)
			{
				ReportNoInterlinear();
				return;
			}
			XmlNode node = sentence.Node;
			XmlNode interlinear = node.SelectSingleNode("//Input");
			if (interlinear == null)
			{
				ReportNoInterlinear();
				return;
			}

			XmlDocument doc1 = new XmlDocument();
			doc1.LoadXml(interlinear.OuterXml);
			XmlNode format = doc1.SelectSingleNode("//Format");
			if (format != null)
			{
				string sBefore = format.InnerText;
				if (sBefore != null)
				{
					string sAfter = sBefore.Replace("\\n", "\n");
					format.InnerText = sAfter;
				}
			}
			XPathNavigator nav = doc1.CreateNavigator();

			XmlTextWriter writer = new XmlTextWriter(m_sInterFile, null);
			XsltArgumentList argList = new XsltArgumentList();
			argList.AddParam(
				"sTextMessage",
				"",
				"Sentence "
					+ m_doc.CurrentSentenceNumber.ToString()
					+ " of "
					+ m_doc.NumberOfSentences.ToString()
			);
			argList.AddParam(
				"bParseAccepted",
				"",
				ParsesChosen[m_doc.CurrentSentenceNumber - 1] == 0 ? "N" : "Y"
			);
			argList.AddParam("bRightToLeft", "", miViewRightToLeft.Checked ? "Y" : "N");
			argList.AddParam("sDecompSeparationCharacter", "", m_language.DecompChar);
			argList.AddParam("sCategoryFont", "", m_language.NTFontFace);
			argList.AddParam("sDecompFont", "", m_language.LexFontFace);
			argList.AddParam("sFeatDescFont", "", m_language.NTFontFace);
			argList.AddParam("sFormatFont", "", m_language.NTFontFace);
			argList.AddParam("sGlossFont", "", m_language.GlossFontFace);
			argList.AddParam("sOrigWordFont", "", m_language.LexFontFace);
			argList.AddParam("sUnderFormFont", "", m_language.LexFontFace);
			m_interlinearTransform.Transform(nav, argList, writer, null);
			writer.Close();

			this.wbInterlinear.Navigate(m_sInterFile);
		}

		private void ReportNoInterlinear()
		{
			string sNoInterlinearMessagePath = Path.Combine(
				m_sStartUpPath,
				@"Transforms\NoInterlinear.htm"
			);
			wbInterlinear.Navigate(sNoInterlinearMessagePath);
		}

		private void ShowParseTree(PcPatrSentence sentence, PcPatrParse parse)
		{
			m_parse = parse;
			if (sentence.Parses.Length > 0 && parse != null)
			{
				XmlNode node = parse.Node;

				if (node != null)
				{
					XmlDocument treeDoc = MakeTreeDocFromNode(node);
					XmlNode xmlTree = treeDoc.SelectSingleNode("//TreeDescription/node");
					ShowTree(treeDoc, xmlTree, sentence);
				}
			}
			else
			{
				m_tree.Root = null;
				m_tree.Invalidate();
				pnlTree.Invalidate();
			}
			EnableDisableItems();
			UpdateStatusBar(sentence);
			wbFeatureStructure.Navigate(m_sInitFeatureMessagePath);
		}

		private void UpdateStatusBar(PcPatrSentence sentence)
		{
			if (m_doc != null)
			{
				sbpnlSentence.Text =
					"Sentence "
					+ m_doc.CurrentSentenceNumber.ToString()
					+ " of "
					+ m_doc.NumberOfSentences.ToString();
				if (sentence != null)
				{
					if (sentence.CurrentParse == null)
						sbpnlParse.Text = m_ksNoParses;
					else
						sbpnlParse.Text =
							"Parse "
							+ sentence.CurrentParseNumber.ToString()
							+ " of "
							+ sentence.NumberOfParses.ToString();
				}
			}
		}

		private XmlDocument MakeTreeDocFromNode(XmlNode node)
		{
			XPathDocument xpath = MakeXPathDocFromNode(node);

			string sOutput = Path.GetTempFileName();
			StreamWriter result = new StreamWriter(sOutput);
			XsltArgumentList argList = new XsltArgumentList();
			argList.AddParam("sGlossFont", "", m_language.GlossFontFace);
			argList.AddParam("sGlossFontSize", "", m_language.GlossFontSize.ToString());
			argList.AddParam("sGlossFontColor", "", m_language.GlossInfo.Color.Name);
			argList.AddParam("sNTFont", "", m_language.NTFontFace);
			argList.AddParam("sNTFontSize", "", m_language.NTFontSize.ToString());
			argList.AddParam("sNTFontColor", "", m_language.NTColorName);
			argList.AddParam("sLexFont", "", m_language.LexFontFace);
			argList.AddParam("sLexFontSize", "", m_language.LexFontSize.ToString());
			argList.AddParam("sLexFontColor", "", m_language.LexColorName);
			m_treeTransform.Transform(xpath, argList, result);
			result.Close();

			XmlDocument treeDoc = new XmlDocument();
			treeDoc.Load(sOutput);
			File.Delete(sOutput);
			return treeDoc;
		}

		private XPathDocument MakeXPathDocFromNode(XmlNode node)
		{
			string sInput = Path.GetTempFileName();
			StreamWriter input = new StreamWriter(sInput);
			input.Write(node.OuterXml);
			input.Close();

			XPathDocument xpath = new XPathDocument(sInput);
			File.Delete(sInput);
			return xpath;
		}

		private void ShowTree(XmlDocument treeDoc, XmlNode xmlTree, PcPatrSentence sentence)
		{
			m_tree.Visible = true;
			m_tree.Enabled = true;
			m_tree.UseRightToLeft = miViewRightToLeft.Checked;
			m_tree.SetTreeParameters(treeDoc);
			m_tree.ParseXmlTreeDescription(xmlTree);
			m_tree.MessageText =
				"Parse "
				+ sentence.CurrentParseNumber.ToString()
				+ " of "
				+ sentence.NumberOfParses.ToString();
			if (ParsesChosen[m_doc.CurrentSentenceNumber - 1] == sentence.CurrentParseNumber)
			{
				m_tree.BackgroundColor = Color.Yellow;
			}
			Graphics grfx = CreateGraphics();
			m_tree.CalculateCoordinates(grfx);
			m_tree.Draw(grfx, Color.Black);

			m_tree.Invalidate();
			pnlTree.Invalidate();
		}

		void TreePanelOnPaint(object obj, PaintEventArgs pea)
		{
			if (this.m_parse != null)
			{
				m_tree.OnPaint(obj, pea);
			}
			else
			{
				//if (m_sLogOrAnaFileName != null && m_sLogOrAnaFileName.Length > 1)
				if (m_doc != null)
				{
					string outOfTime = "";
					var sentence = m_doc.CurrentSentence;
					if (sentence != null)
						if (sentence.OutOfTimeFailure)
							outOfTime = " The time limit was exceeded.";
					Graphics grfx = pea.Graphics;
					grfx.DrawString(
						"There is no parse for this sentence." + outOfTime,
						new Font("Times New Roman", 12.0f),
						new SolidBrush(Color.Black),
						10.0f,
						10.0f
					);
				}
			}
		}

		private void OnNodeClicked(object sender, LingTreeNodeClickedEventArgs ltne)
		{
			string sId = ltne.Node.Id;
			ShowFSInfo(sId);
			ShowGrammarInfo(sId);
		}

		private void ShowGrammarInfo(string sId)
		{
			XmlNode node = m_parse.Node.SelectSingleNode("//Node[@id='" + sId + "']");
			if (node != null)
			{
				XmlNode rule = node.SelectSingleNode("@rule");
				if (rule != null)
				{
					string sRule = rule.InnerText;
					int iLoc = richTextBox1.Find(sRule);
					if (iLoc > 0)
					{
						richTextBox1.SelectionStart = iLoc;
						richTextBox1.Focus();
						richTextBox1.ScrollToCaret();
					}
				}
			}
		}

		private void ShowFSInfo(string sId)
		{
			XmlNode featStruct = GetFeatureStructureAtNode(sId);
			if (featStruct == null)
				return; // avoid crash

			XmlDocument doc1 = new XmlDocument();
			doc1.LoadXml(featStruct.OuterXml);
			XPathNavigator nav = doc1.CreateNavigator();

			XmlTextWriter writer = new XmlTextWriter(m_sFSFile, null);
			m_fsTransform.Transform(nav, null, writer, null);
			writer.Close();

			this.wbFeatureStructure.Navigate(m_sFSFile);
		}

		/// <summary>
		/// Gete Fs element based on a node's id value
		/// </summary>
		/// <param name="sNodeId"></param>
		/// <returns></returns>
		public XmlNode GetFeatureStructureAtNode(string sNodeId)
		{
			string sId = sNodeId;
			string sFeatureStructureElement = "Fs";
			if (sNodeId.EndsWith("gloss"))
			{
				sId = sNodeId.Substring(0, sNodeId.Length - 5);
				sFeatureStructureElement = "Lexfs";
			}
			if (sNodeId.EndsWith("lex"))
			{
				sId = sNodeId.Substring(0, sNodeId.Length - 3);
				sFeatureStructureElement = "Lexfs";
			}
			string sAtId = "[@id='" + sId + "']/" + sFeatureStructureElement;
			string sChosenNodeXPath = "//Node" + sAtId + " | //Leaf" + sAtId;
			XmlNode featStructBasic = m_parse.Node.SelectSingleNode(sChosenNodeXPath);
			XmlNode featStruct = FleshOutFVals(featStructBasic);
			return featStruct;
		}

		/// <summary>
		/// Gete Fs element based on a node's id value
		/// </summary>
		/// <param name="sFsId"></param>
		/// <returns></returns>
		private XmlNode GetFeatureStructureAtFs(string sFsId)
		{
			string sFs = "//Fs[@id='" + sFsId + "']";
			if (m_parse.Node == null)
				return null; // avoid crash
			XmlNode featStructBasic = m_parse.Node.SelectSingleNode(sFs);
			XmlNode featStruct = FleshOutFVals(featStructBasic);
			return featStruct;
		}

		private XmlNode FleshOutFVals(XmlNode basic)
		{
			if (basic == null)
				return null;
			XmlNodeList nodes = basic.SelectNodes("descendant::F/@fVal");
			foreach (XmlNode node in nodes)
			{
				string sFVal = node.InnerText;
				XmlNode fleshed = GetFeatureStructureAtFs(sFVal);
				if (fleshed != null)
				{
					// append fleshed to basic at right spot
					XmlNode f = node.SelectSingleNode("..");
					f.InnerXml = fleshed.OuterXml;
				}
			}
			return basic;
		}

		private void miExit_Click(object sender, System.EventArgs e)
		{
			this.OnClosed(e);
			if (!IsDisposed)
			{
				this.Dispose();
			}
		}

		private void miSentenceFirst_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.FirstSentence;
			ShowParseTree(sent, sent.CurrentParse);
			ShowInterlinear(sent);
		}

		private void miSentenceLast_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.LastSentence;
			ShowParseTree(sent, sent.CurrentParse);
			ShowInterlinear(sent);
		}

		private void miSentenceNext_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.NextSentence;
			ShowParseTree(sent, sent.CurrentParse);
			ShowInterlinear(sent);
		}

		private void miSentencePrevious_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.PreviousSentence;
			ShowParseTree(sent, sent.CurrentParse);
			ShowInterlinear(sent);
		}

		private void miParseFirst_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.CurrentSentence;
			ShowParseTree(sent, sent.FirstParse);
		}

		private void miParsePrevious_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.CurrentSentence;
			ShowParseTree(sent, sent.PreviousParse);
		}

		private void miParseNext_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.CurrentSentence;
			ShowParseTree(sent, sent.NextParse);
		}

		private void miParseLast_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.CurrentSentence;
			ShowParseTree(sent, sent.LastParse);
		}

		private void miUseThisParse_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.CurrentSentence;
			PcPatrParse parse = sent.CurrentParse;
			if (parse == null)
				return;
			int i = sent.CurrentParseNumber;
			// build and save result value
			var guids = parse.Node.SelectNodes(
				"//Parse[" + i + "]//Lexfs/F[@name='properties']/Str"
			);
			var sb = new StringBuilder();
			foreach (XmlNode node in guids)
			{
				sb.Append(node.InnerText + "\n");
			}
			PropertiesChosen[m_doc.CurrentSentenceNumber - 1] = sb.ToString();
			ParsesChosen[m_doc.CurrentSentenceNumber - 1] = sent.CurrentParseNumber;
			ShowInterlinear(sent);
			ShowParseTree(sent, parse);
		}

		private void miUseThisParseNextSentence_Click(object sender, EventArgs e)
		{
			miUseThisParse_Click(sender, e);
			miSentenceNext_Click(sender, e);
		}

		private void miCancelUsingThisParse_Click(object sender, System.EventArgs e)
		{
			PcPatrSentence sent = m_doc.CurrentSentence;
			PcPatrParse parse = sent.CurrentParse;
			if (parse == null)
				return;
			PropertiesChosen[m_doc.CurrentSentenceNumber - 1] = "";
			ParsesChosen[m_doc.CurrentSentenceNumber - 1] = 0;
			ShowInterlinear(sent);
			ShowParseTree(sent, parse);
		}

		private void miViewStatusBar_Click(object sender, System.EventArgs e)
		{
			if (miViewStatusBar.Checked)
			{
				sbStatusBar.Visible = false;
				miViewStatusBar.Checked = false;
			}
			else
			{
				sbStatusBar.Visible = true;
				miViewStatusBar.Checked = true;
			}
		}

		private void miViewFeatStruct_Click(object sender, System.EventArgs e)
		{
			if (miViewFeatStruct.Checked)
			{
				pnlFeatureStructure.Visible = false;
				miViewFeatStruct.Checked = false;
			}
			else
			{
				pnlFeatureStructure.Visible = true;
				miViewFeatStruct.Checked = true;
			}
		}

		private void miViewRuleFile_Click(object sender, System.EventArgs e)
		{
			if (miViewRuleFile.Checked)
			{
				pnlRule.Visible = false;
				miViewRuleFile.Checked = false;
			}
			else
			{
				pnlRule.Visible = true;
				miViewRuleFile.Checked = true;
			}
		}

		private void tbMain_ButtonClick(
			object sender,
			System.Windows.Forms.ToolBarButtonClickEventArgs e
		)
		{
			if (e != null)
			{
				MenuItem mi = (MenuItem)e.Button.Tag;
				mi.PerformClick();
			}
		}

		private void miViewToolBar_Click(object sender, System.EventArgs e)
		{
			if (miViewToolBar.Checked)
			{
				tbMain.Visible = false;
				miViewToolBar.Checked = false;
			}
			else
			{
				tbMain.Visible = true;
				miViewToolBar.Checked = true;
			}
		}

		private void miSentenceGoTo_Click(object sender, System.EventArgs e)
		{
			dlgGoTo dlg = new dlgGoTo();
			dlg.Text = "Go To Sentence";
			dlg.GoToPrompt = "&Go to sentence:";
			dlg.ShowDialog();
			if (dlg.DialogResult == DialogResult.OK)
			{
				int iNum = dlg.Number;
				PcPatrSentence sent = m_doc.GoToSentence(iNum);
				ShowParseTree(sent, sent.CurrentParse);
				ShowInterlinear(sent);
			}
		}

		private void miParserGoTo_Click(object sender, System.EventArgs e)
		{
			dlgGoTo dlg = new dlgGoTo();
			dlg.Text = "Go To Parse";
			dlg.GoToPrompt = "&Go to parse:";
			dlg.ShowDialog();
			if (dlg.DialogResult == DialogResult.OK)
			{
				int iNum = dlg.Number;
				PcPatrSentence sent = m_doc.CurrentSentence;
				ShowParseTree(sent, sent.GoToParse(iNum));
			}
		}

		private void miViewInterlinear_Click(object sender, System.EventArgs e)
		{
			if (miViewInterlinear.Checked)
			{
				pnlInterlinear.Visible = false;
				miViewInterlinear.Checked = false;
			}
			else
			{
				pnlInterlinear.Visible = true;
				miViewInterlinear.Checked = true;
			}
		}

		private void miViewRightToLeft_Click(object sender, System.EventArgs e)
		{
			if (miViewRightToLeft.Checked)
			{
				m_tree.UseRightToLeft = false;
				miViewRightToLeft.Checked = false;
			}
			else
			{
				m_tree.UseRightToLeft = true;
				miViewRightToLeft.Checked = true;
			}
			// keep view and language info in sync
			m_language.UseRTL = miViewRightToLeft.Checked;
			m_tree.Invalidate();
			if (m_doc != null)
				ShowInterlinear(m_doc.CurrentSentence);
		}

		private void miLanguage_Click(object sender, System.EventArgs e)
		{
			DlgLanguage dlg = new DlgLanguage(m_language);

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				m_language.LanguageName = dlg.LanguageName;
				m_language.NTInfo = dlg.NTInfo;
				m_language.LexInfo = dlg.LexInfo;
				m_language.GlossInfo = dlg.GlossInfo;
				m_language.UseRTL = dlg.UseRTL;
				// make sure view is set to same value as in language info file
				miViewRightToLeft.Checked = m_language.UseRTL;
				m_language.DecompChar = dlg.DecompChar;
				m_fNeedToSaveLanguageInfo = true;
				SetTitle();
				if (m_doc != null && m_doc.CurrentSentence != null)
				{
					ShowParseTree(m_doc.CurrentSentence, m_doc.CurrentSentence.CurrentParse);
					ShowInterlinear(m_doc.CurrentSentence);
				}
			}
		}

		private void SetTitle()
		{
			this.Text =
				"PC-PATR Browser - "
				+ m_sLogOrAnaFileName
				+ " ("
				+ m_language.LanguageName
				+ ")"
				+ (m_fNeedToSaveLanguageInfo ? "*" : "");
		}

		private void miFileSaveLanguage_Click(object sender, System.EventArgs e)
		{
			if ((m_sLanguageFileName == null) || (!File.Exists(m_sLanguageFileName)))
			{
				SaveFileDialog saveDlg = new SaveFileDialog();
				saveDlg.AddExtension = true;
				saveDlg.Title = "PC-PATR Browser Language Info File";
				saveDlg.Filter = m_strLanguageFileFilter;
				// following needed, otherwise it always returns "Cancel"
				saveDlg.OverwritePrompt = false;
				if (m_sLanguageFileName != null && m_sLanguageFileName != "")
				{
					saveDlg.InitialDirectory = Path.GetDirectoryName(m_sLanguageFileName);
					saveDlg.FileName = Path.GetFileNameWithoutExtension(m_sLanguageFileName);
				}
				if (saveDlg.ShowDialog() == DialogResult.OK)
				{
					if (Path.GetExtension(saveDlg.FileName) == "")
						saveDlg.FileName += ".pbl";
					m_sLanguageFileName = saveDlg.FileName;
				}
				else
					return;
			}
			SaveLanguageInfo();
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Is public for unit testing</remarks>
		public void SaveLanguageInfo()
		{
			try
			{
				m_language.SaveInfo(m_sLanguageFileName);
				m_fNeedToSaveLanguageInfo = false;
				SetTitle();
			}
			catch (Exception exc)
			{
				MessageBox.Show(
					"An error occurred while trying to save the language info file: "
						+ exc.Message
						+ " "
						+ exc.InnerException,
					"Save error!",
					MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation
				);
			}
		}

		private void miFileOpenLanguage_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = m_strLanguageFileFilter;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				m_sLanguageFileName = dlg.FileName;
			}
			else
				return;

			LoadLanguageInfo();
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>is public for unit testing</remarks>
		public void LoadLanguageInfo()
		{
			try
			{
				m_language.LoadInfo(m_sLanguageFileName);
				// make sure view is set to same value as in language info file
				miViewRightToLeft.Checked = m_language.UseRTL;
			}
			catch (Exception exc)
			{
				MessageBox.Show(
					"An error occurred while trying to load the language information file: "
						+ exc.Message,
					"Load error!",
					MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation
				);
			}
		}

		/// <summary>
		/// Get/set current Parse
		/// </summary>
		public PcPatrParse Parse
		{
			get { return m_parse; }
			set { m_parse = value; }
		}

		/// <summary>
		/// Get/set language info
		/// </summary>
		/// <remarks>setting is for unit testing</remarks>
		public LanguageInfo LanguageInfo
		{
			get { return m_language; }
			set { m_language = value; }
		}

		/// <summary>
		/// Get/set language file name
		/// </summary>
		/// <remarks>used for unit testing</remarks>
		public string LanguageFileName
		{
			get { return m_sLanguageFileName; }
			set { m_sLanguageFileName = value; }
		}

		protected override void OnClosed(EventArgs e)
		{
			if (IsDisposed)
				// if user uses Exit menu item, we dispose and this gets called a second time
				return;
			SaveRegistryInfo();
			if (miSaveForTreeTran.Checked)
			{
				SaveForTreeTran();
			}
			if (m_fNeedToSaveLanguageInfo)
				miFileSaveLangAs_Click(null, null);
			if (File.Exists(m_sFSFile))
				File.Delete(m_sFSFile);
			if (File.Exists(m_sInterFile))
				File.Delete(m_sInterFile);
			base.OnClosed(e);
		}

		private void SaveForTreeTran()
		{
			TreeTranSaver saver = new TreeTranSaver(m_doc);
			string ana = saver.CreateANAFromParsesChosen(ParsesChosen);
			if (!string.IsNullOrEmpty(ana))
			{
				string outputFileName = m_sLogOrAnaFileName;
				if (!outputFileName.EndsWith(".and"))
				{
					outputFileName = outputFileName + ".xml";
				}
				else
				{
					outputFileName = outputFileName.Substring(0, outputFileName.Length - 3) + "xml";
				}
				File.WriteAllText(outputFileName, ana, Encoding.UTF8);
			}
		}

		void RetrieveRegistryInfo()
		{
			try
			{
				RegistryKey regkey = Registry.CurrentUser.OpenSubKey(m_sRegKey);
				if (regkey != null)
				{
					Cursor.Current = Cursors.WaitCursor;
					Application.DoEvents();

					// View settings
					miViewFeatStruct.Checked = Convert.ToBoolean(
						(string)regkey.GetValue(m_ksViewFeatStruct)
					);
					miViewInterlinear.Checked = Convert.ToBoolean(
						(string)regkey.GetValue(m_ksViewInterlinear)
					);
					miViewRuleFile.Checked = Convert.ToBoolean(
						(string)regkey.GetValue(m_ksViewRule)
					);
					miViewStatusBar.Checked = Convert.ToBoolean(
						(string)regkey.GetValue(m_ksViewStatusBar)
					);
					miViewToolBar.Checked = Convert.ToBoolean(
						(string)regkey.GetValue(m_ksViewToolBar)
					);
					miViewRightToLeft.Checked = Convert.ToBoolean(
						(string)regkey.GetValue(m_ksViewRightToLeft)
					);
					miSaveForTreeTran.Checked = Convert.ToBoolean(
						(string)regkey.GetValue(m_ksSaveForTreeTran)
					);
					m_sLanguageFileName = (string)regkey.GetValue(m_ksLastLanguageFile);
					if (m_sLanguageFileName != null && m_sLanguageFileName != "")
						LoadLanguageInfo();

					// Window location
					int iX = (int)regkey.GetValue(m_ksLocationX, 100);
					int iY = (int)regkey.GetValue(m_ksLocationY, 100);
					int iWidth = (int)regkey.GetValue(m_ksSizeWidth, 592);
					int iHeight = (int)regkey.GetValue(m_ksSizeHeight, 569);
					m_RectNormal = new Rectangle(iX, iY, iWidth, iHeight);
					// Set form properties
					DesktopBounds = m_RectNormal;
					WindowState = (FormWindowState)regkey.GetValue(m_ksWindowState, 0);
					StartPosition = FormStartPosition.Manual;
					// For some reason .Net now forces the SplitPosition to be no more than
					// MinSize.  We set both if needed.
					int iSize = (int)regkey.GetValue(m_ksSplitterBetweenInterAndTreeFeat, 100);
					if (splitterBetweenInterlinearAndTreeFeat.MinSize < iSize)
					{
						splitterBetweenInterlinearAndTreeFeat.MinSize = iSize;
						splitterBetweenInterlinearAndTreeFeat.SplitPosition = iSize;
						splitterBetweenInterlinearAndTreeFeat.MinSize = 25;
					}
					else
						splitterBetweenInterlinearAndTreeFeat.SplitPosition = iSize;
					iSize = (int)regkey.GetValue(m_ksSplitterBetweenTreeAndFeat, 192);
					if (splitterBetweenTreeAndFeat.MinSize < iSize)
					{
						splitterBetweenTreeAndFeat.MinSize = iSize;
						splitterBetweenTreeAndFeat.SplitPosition = iSize;
						splitterBetweenTreeAndFeat.MinSize = 25;
					}
					else
						splitterBetweenTreeAndFeat.SplitPosition = iSize;
					iSize = (int)regkey.GetValue(m_ksSplitterBetweenTreeFeatAndRule, 100);
					if (splitterBetweenTreeFeatAndRule.MinSize < iSize)
					{
						splitterBetweenTreeFeatAndRule.MinSize = iSize;
						splitterBetweenTreeFeatAndRule.SplitPosition = iSize;
						splitterBetweenTreeFeatAndRule.MinSize = 25;
					}
					else
						splitterBetweenTreeFeatAndRule.SplitPosition = iSize;

					m_sLogOrAnaFileName = (string)regkey.GetValue(m_ksLastLogFile);
					Cursor.Current = Cursors.Default;
					regkey.Close();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.InnerException);
				Console.WriteLine(e.StackTrace);
			}
		}

		void SaveRegistryInfo()
		{
			RegistryKey regkey = Registry.CurrentUser.OpenSubKey(m_sRegKey, true);
			if (regkey == null)
			{
				regkey = Registry.CurrentUser.CreateSubKey(m_sRegKey);
			}
			if (m_sLogOrAnaFileName != null)
				regkey.SetValue(m_ksLastLogFile, m_sLogOrAnaFileName);
			else
				regkey.SetValue(m_ksLastLogFile, "");
			if (m_sLanguageFileName != null)
				regkey.SetValue(m_ksLastLanguageFile, m_sLanguageFileName);
			else
				regkey.SetValue(m_ksLastLanguageFile, "");
			// View settings
			regkey.SetValue(m_ksViewFeatStruct, miViewFeatStruct.Checked);
			regkey.SetValue(m_ksViewInterlinear, miViewInterlinear.Checked);
			regkey.SetValue(m_ksViewRule, miViewRuleFile.Checked);
			regkey.SetValue(m_ksViewStatusBar, miViewStatusBar.Checked);
			regkey.SetValue(m_ksViewToolBar, miViewToolBar.Checked);
			regkey.SetValue(m_ksViewRightToLeft, miViewRightToLeft.Checked);
			regkey.SetValue(m_ksSaveForTreeTran, miSaveForTreeTran.Checked);
			// Window position and location
			regkey.SetValue(m_ksWindowState, (int)WindowState);
			regkey.SetValue(m_ksLocationX, this.m_RectNormal.X);
			regkey.SetValue(m_ksLocationY, this.m_RectNormal.Y);
			regkey.SetValue(m_ksSizeWidth, this.m_RectNormal.Width);
			regkey.SetValue(m_ksSizeHeight, this.m_RectNormal.Height - 20); // not sure why it's 20 off...
			regkey.SetValue(
				m_ksSplitterBetweenInterAndTreeFeat,
				splitterBetweenInterlinearAndTreeFeat.SplitPosition
			);
			regkey.SetValue(
				m_ksSplitterBetweenTreeAndFeat,
				splitterBetweenTreeAndFeat.SplitPosition
			);
			regkey.SetValue(
				m_ksSplitterBetweenTreeFeatAndRule,
				splitterBetweenTreeFeatAndRule.SplitPosition
			);

			regkey.Close();
		}

		private void miFileSaveLangAs_Click(object sender, System.EventArgs e)
		{
			SaveFileDialog saveDlg = new SaveFileDialog();
			saveDlg.AddExtension = true;
			saveDlg.Title = "PC-PATR Browser Language Info File";
			saveDlg.Filter = m_strLanguageFileFilter;
			// following needed, otherwise it always returns "Cancel"
			saveDlg.OverwritePrompt = false;
			if (m_sLanguageFileName != null && m_sLanguageFileName != "")
			{
				saveDlg.InitialDirectory = Path.GetDirectoryName(m_sLanguageFileName);
				saveDlg.FileName = Path.GetFileNameWithoutExtension(m_sLanguageFileName);
			}
			if (saveDlg.ShowDialog() == DialogResult.OK)
			{
				if (Path.GetExtension(saveDlg.FileName) == "")
					saveDlg.FileName += ".pbl";
				m_sLanguageFileName = saveDlg.FileName;
				SaveLanguageInfo();
			}
		}

		protected override void OnMove(EventArgs ea)
		{
			base.OnMove(ea);

			if (WindowState == FormWindowState.Normal)
				m_RectNormal = DesktopBounds;
		}

		protected override void OnResize(EventArgs ea)
		{
			base.OnResize(ea);

			if (WindowState == FormWindowState.Normal)
				m_RectNormal = DesktopBounds;
		}

		private void miViewRefresh_Click(object sender, System.EventArgs e)
		{
			if (File.Exists(m_sLogOrAnaFileName))
				LoadAnaFile();
		}

		private void miSaveForTreeTran_Click(object sender, EventArgs e)
		{
			miSaveForTreeTran.Checked = !miSaveForTreeTran.Checked;
			tbbtnSaveForTreeTran.Pushed = miSaveForTreeTran.Checked;
			;
		}
	}
}
