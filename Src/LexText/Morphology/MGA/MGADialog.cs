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
// File: MGA.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;

namespace  SIL.FieldWorks.LexText.Controls.MGA
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class MGADialog : Form, IFWDisposable
	{
		private int m_panelBottomHeight = 0;
		private FdoCache m_cache;
		private XCore.Mediator m_mediator;
		private XslCompiledTransform m_xslShowInfoTransform;
		private XmlDocument m_xmlShowInfoDoc;
		private string m_sHelpHtm = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\MGA\Help.htm");
		private WebBrowser webBrowserInfo;
		private System.Windows.Forms.Button buttonInsert;
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.Button buttonAcceptGloss;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonHelp;
		private System.Windows.Forms.GroupBox groupBoxSelectedGloss;
		private System.Windows.Forms.Button buttonMoveUp;
		private System.Windows.Forms.Button buttonMoveDown;
		private System.Windows.Forms.Button buttonModify;
		private System.Windows.Forms.Button buttonInfo;
		private System.Windows.Forms.GroupBox groupBoxGlossComponents;
		private SIL.FieldWorks.LexText.Controls.MGA.GlossListTreeView treeViewGlossListItem;
		private System.Windows.Forms.CheckBox checkBoxShowUsed;
		private FwOverrideComboBox comboGlossListItem;
		private System.Windows.Forms.Label labelGlossListItem;
		private System.Windows.Forms.TextBox textBoxResult;
		private System.Windows.Forms.Label labelAllomorph;
		private System.Windows.Forms.Label labelConstructedGlossForPrompt;

		private const string s_helpTopic = "khtpMGA";
		private System.Windows.Forms.HelpProvider helpProvider;
		private SplitContainer splitContainerHorizontal;
		private GlossListBox glossListBoxGloss;
		private SplitContainer splitContainerVertical;
		private IContainer components;
		#region event handlers
		public event GlossListEventHandler InsertMGAGlossListItem;
		protected virtual void OnInsertMGAGlossListItem(GlossListEventArgs glea)
		{
			if (InsertMGAGlossListItem != null)
				InsertMGAGlossListItem(this, glea);
		}

		public event EventHandler RemoveMGAGlossListItem;
		protected virtual void OnRemoveMGAGlossListItem(EventArgs e)
		{
			if (RemoveMGAGlossListItem != null)
				RemoveMGAGlossListItem(this, e);
		}

		public event EventHandler MoveDownMGAGlossListItem;
		protected virtual void OnMoveDownMGAGlossListItem(EventArgs e)
		{
			if (MoveDownMGAGlossListItem != null)
				MoveDownMGAGlossListItem(this, e);
		}

		public event EventHandler MoveUpMGAGlossListItem;
		protected virtual void OnMoveUpMGAGlossListItem(EventArgs e)
		{
			if (MoveUpMGAGlossListItem != null)
				MoveUpMGAGlossListItem(this, e);
		}

		#endregion
		public MGADialog(FdoCache cache, Mediator mediator, string sMorphemeForm)
		{
			m_mediator = mediator;
			m_cache = cache;
			InitForm();
			labelAllomorph.Text = sMorphemeForm;

			helpProvider = new System.Windows.Forms.HelpProvider();
			helpProvider.HelpNamespace = FwApp.App.HelpFile;
			helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
			helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
		}

		private void InitForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			SuspendLayout();

			glossListBoxGloss.MGADialog = this;

			webBrowserInfo.Navigate(m_sHelpHtm);

			// init transform used in help panel
			m_xslShowInfoTransform = new XslCompiledTransform();
			string sXsltFile = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\MGA\MGAShowInfo.xsl");
			m_xslShowInfoTransform.Load(sXsltFile);
			// init XmlDoc, too
			m_xmlShowInfoDoc = new XmlDocument();

			CancelButton = buttonCancel;
			// add the Info button
			buttonInfo.Text = MGAStrings.ksHideInfo;
			string sXmlFile = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\MGA\GlossLists\EticGlossList.xml");
			using (new WaitCursor(this))
			{
				treeViewGlossListItem.LoadGlossListTreeFromXml(sXmlFile,
					m_cache.LangProject.DefaultAnalysisWritingSystemICULocale);
			}

			float fRatio = CreateGraphics().DpiX / 96.0f; // try to adjust for screen resolution
			// on start-up, ensure the selected gloss panel is wide enough for all the buttons
			splitContainerVertical.SplitterDistance =
				splitContainerVertical.Width - (buttonAcceptGloss.Width
				+ buttonCancel.Width + buttonHelp.Width + (int)(16 * fRatio));

			ILangProject lp = m_cache.LangProject;
			this.Text += " " + lp.Name.AnalysisDefaultWritingSystem;

			ResumeLayout();
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
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		void OnAcceptGlossButtonClick(object obj, EventArgs ea)
		{
			Close();
		}
		void OnCancelButtonClick(object obj, EventArgs ea)
		{
			Close();
		}
		void OnHelpButtonClick(object obj, EventArgs ea)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}
		void OnInfoButtonClick(object obj, EventArgs ea)
		{
			Size sz = new Size(ClientSize.Width, ClientSize.Height);
			if (splitContainerHorizontal.Panel2Collapsed)
			{
				// Show the info pane
				int splitterDistance = splitContainerHorizontal.Panel1.Height;
				splitContainerHorizontal.Panel2Collapsed = false;
				sz.Height += m_panelBottomHeight;
				ClientSize = sz;
				splitContainerHorizontal.SplitterDistance = splitterDistance;
				buttonInfo.Text = MGAStrings.ksHideInfo;
			}
			else
			{
				// hide the info pane
				int firstPanelHeight = splitContainerHorizontal.Panel1.Height;
				m_panelBottomHeight = splitContainerHorizontal.Panel2.Height;
				splitContainerHorizontal.Panel2Collapsed = true;
				sz.Height = splitContainerHorizontal.Location.Y + firstPanelHeight;
				ClientSize = sz;
				buttonInfo.Text = MGAStrings.ksShowInfo;
			}
		}
		public void OnInsertButtonClick(object obj, EventArgs ea)
		{
			CheckDisposed();

			MasterInflectionFeature mif = (MasterInflectionFeature)treeViewGlossListItem.SelectedNode.Tag;
			if (mif == null)
				return; // just to be safe
			GlossListBoxItem glbiNew = new GlossListBoxItem(mif.Node,
				treeViewGlossListItem.AfterSeparator, treeViewGlossListItem.ComplexNameSeparator,
				treeViewGlossListItem.ComplexNameFirst);
			GlossListBoxItem glbiConflict;
			if (glossListBoxGloss.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict))
			{
				const string ksPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='MGA']/";
				string sMsg1 = m_mediator.StringTbl.GetStringWithXPath("ItemConflictDlgMessage", ksPath);
				string sMsg = String.Format(sMsg1, glbiConflict.ToString());
				string sCaption = m_mediator.StringTbl.GetStringWithXPath("ItemConflictDlgCaption", ksPath);
				MessageBox.Show(sMsg, sCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			// raise event
			GlossListEventArgs glea = new GlossListEventArgs(glbiNew);
			OnInsertMGAGlossListItem(glea);
			buttonAcceptGloss.Enabled = true;
			EnableMoveUpDownButtons();
			ShowGloss();
		}
		void OnModifyButtonClick(object obj, EventArgs ea)
		{
			MessageBox.Show(MGAStrings.ksNoModifyButtonYet);
		}
		void OnMoveDownButtonClick(object obj, EventArgs ea)
		{
			// raise event
			OnMoveDownMGAGlossListItem(EventArgs.Empty);
			ShowGloss();
		}
		void OnMoveUpButtonClick(object obj, EventArgs ea)
		{
			// raise event
			OnMoveUpMGAGlossListItem(EventArgs.Empty);
			ShowGloss();
		}
		void OnRemoveButtonClick(object obj, EventArgs ea)
		{
			// raise event
			OnRemoveMGAGlossListItem(EventArgs.Empty);
			// determine which buttons to enable
			int cCount = glossListBoxGloss.Items.Count;
			if (cCount <= 0)
			{
				buttonRemove.Enabled = false;
				buttonModify.Enabled = false;
				buttonAcceptGloss.Enabled = false;
			}
			else
			{
				int iIndex = glossListBoxGloss.SelectedIndex;
				iIndex = Math.Min(iIndex, cCount - 1);
				glossListBoxGloss.SelectedIndex = iIndex;
			}
			EnableMoveUpDownButtons();
			ShowGloss();
		}
		void OnGlossListTreeSelect(object obj, TreeViewEventArgs tvea)
		{
			buttonInsert.Enabled = true;
			TreeNode tn = tvea.Node;
			// do we need a try block here to catch a problem?
			MasterInflectionFeature mif = (MasterInflectionFeature)tn.Tag;
			XmlNode node = mif.Node;
			ShowInfo(node);
		}
		void OnGlossListBoxSelectedIndexChanged(object obj, EventArgs ea)
		{
			buttonRemove.Enabled = true;
#if ModifyImplemented
			buttonModify.Enabled = true;
#endif
			EnableMoveUpDownButtons();
		}
		void EnableMoveUpDownButtons()
		{
			int iSelectedIndex = glossListBoxGloss.SelectedIndex;
			int cCount = glossListBoxGloss.Items.Count;
			if (cCount < 2 || iSelectedIndex < 0)
			{
				buttonMoveDown.Enabled = false;
				buttonMoveUp.Enabled = false;
			}
			else
			{
				if (iSelectedIndex == (cCount - 1))
					buttonMoveDown.Enabled = false;
				else
					buttonMoveDown.Enabled = true;
				if (iSelectedIndex == 0)
					buttonMoveUp.Enabled = false;
				else
					buttonMoveUp.Enabled = true;
			}
		}
		void ShowGloss()
		{
			StringBuilder sb = new StringBuilder();
			int i = 0;
			int iMax = glossListBoxGloss.Items.Count;
			foreach (GlossListBoxItem xn in glossListBoxGloss.Items)
			{
				sb.Append(xn.Abbrev);
				i++;
				if (i != iMax)
				{
					if (xn.IsComplex)
						sb.Append(xn.ComplexNameSeparator);
					else
						sb.Append(xn.AfterSeparator);
				}
			}
			textBoxResult.Text = sb.ToString();
		}
		void ShowInfo(XmlNode node)
		{
			StringWriter w = new StringWriter();
			XmlTextWriter tw = new XmlTextWriter(w);

			m_xmlShowInfoDoc.LoadXml(node.OuterXml); // N.B. LoadXml requires UTF-16 or UCS-2 encodings

			XsltArgumentList args = new XsltArgumentList();
			args.AddParam("sHelpFile", "", m_sHelpHtm);
			m_xslShowInfoTransform.Transform(m_xmlShowInfoDoc, args, tw);

			webBrowserInfo.DocumentText = w.GetStringBuilder().ToString();
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MGADialog));
			System.Windows.Forms.TableLayoutPanel panelGlossComponents;
			System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
			System.Windows.Forms.Panel panel1;
			System.Windows.Forms.Panel panelInsertRemove;
			System.Windows.Forms.TableLayoutPanel panelSelectedGloss;
			System.Windows.Forms.TableLayoutPanel tableLayoutPanelSelectedGloss;
			System.Windows.Forms.Panel panel;
			System.Windows.Forms.Panel panel2;
			System.Windows.Forms.Panel panelTop;
			System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
			this.splitContainerVertical = new System.Windows.Forms.SplitContainer();
			this.groupBoxGlossComponents = new System.Windows.Forms.GroupBox();
			this.comboGlossListItem = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.labelGlossListItem = new System.Windows.Forms.Label();
			this.checkBoxShowUsed = new System.Windows.Forms.CheckBox();
			this.buttonInfo = new System.Windows.Forms.Button();
			this.buttonInsert = new System.Windows.Forms.Button();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.groupBoxSelectedGloss = new System.Windows.Forms.GroupBox();
			this.buttonMoveUp = new System.Windows.Forms.Button();
			this.buttonMoveDown = new System.Windows.Forms.Button();
			this.buttonModify = new System.Windows.Forms.Button();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.buttonAcceptGloss = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.labelConstructedGlossForPrompt = new System.Windows.Forms.Label();
			this.labelAllomorph = new System.Windows.Forms.Label();
			this.textBoxResult = new System.Windows.Forms.TextBox();
			this.webBrowserInfo = new System.Windows.Forms.WebBrowser();
			this.splitContainerHorizontal = new System.Windows.Forms.SplitContainer();
			this.treeViewGlossListItem = new SIL.FieldWorks.LexText.Controls.MGA.GlossListTreeView();
			this.glossListBoxGloss = new SIL.FieldWorks.LexText.Controls.MGA.GlossListBox();
			panelGlossComponents = new System.Windows.Forms.TableLayoutPanel();
			tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			panel1 = new System.Windows.Forms.Panel();
			panelInsertRemove = new System.Windows.Forms.Panel();
			panelSelectedGloss = new System.Windows.Forms.TableLayoutPanel();
			tableLayoutPanelSelectedGloss = new System.Windows.Forms.TableLayoutPanel();
			panel = new System.Windows.Forms.Panel();
			panel2 = new System.Windows.Forms.Panel();
			panelTop = new System.Windows.Forms.Panel();
			tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.splitContainerVertical.Panel1.SuspendLayout();
			this.splitContainerVertical.Panel2.SuspendLayout();
			this.splitContainerVertical.SuspendLayout();
			panelGlossComponents.SuspendLayout();
			this.groupBoxGlossComponents.SuspendLayout();
			tableLayoutPanel1.SuspendLayout();
			panel1.SuspendLayout();
			panelInsertRemove.SuspendLayout();
			panelSelectedGloss.SuspendLayout();
			this.groupBoxSelectedGloss.SuspendLayout();
			tableLayoutPanelSelectedGloss.SuspendLayout();
			panel.SuspendLayout();
			panel2.SuspendLayout();
			panelTop.SuspendLayout();
			tableLayoutPanel2.SuspendLayout();
			this.splitContainerHorizontal.Panel1.SuspendLayout();
			this.splitContainerHorizontal.Panel2.SuspendLayout();
			this.splitContainerHorizontal.SuspendLayout();
			this.SuspendLayout();
			//
			// splitContainerVertical
			//
			resources.ApplyResources(this.splitContainerVertical, "splitContainerVertical");
			this.splitContainerVertical.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainerVertical.Name = "splitContainerVertical";
			//
			// splitContainerVertical.Panel1
			//
			this.splitContainerVertical.Panel1.Controls.Add(panelGlossComponents);
			this.splitContainerVertical.Panel1.Controls.Add(panelInsertRemove);
			//
			// splitContainerVertical.Panel2
			//
			this.splitContainerVertical.Panel2.Controls.Add(panelSelectedGloss);
			resources.ApplyResources(this.splitContainerVertical.Panel2, "splitContainerVertical.Panel2");
			//
			// panelGlossComponents
			//
			resources.ApplyResources(panelGlossComponents, "panelGlossComponents");
			panelGlossComponents.Controls.Add(this.groupBoxGlossComponents, 0, 0);
			panelGlossComponents.Controls.Add(this.buttonInfo, 0, 1);
			panelGlossComponents.Name = "panelGlossComponents";
			//
			// groupBoxGlossComponents
			//
			this.groupBoxGlossComponents.Controls.Add(tableLayoutPanel1);
			resources.ApplyResources(this.groupBoxGlossComponents, "groupBoxGlossComponents");
			this.groupBoxGlossComponents.Name = "groupBoxGlossComponents";
			this.groupBoxGlossComponents.TabStop = false;
			//
			// tableLayoutPanel1
			//
			resources.ApplyResources(tableLayoutPanel1, "tableLayoutPanel1");
			tableLayoutPanel1.Controls.Add(panel1, 0, 0);
			tableLayoutPanel1.Controls.Add(this.checkBoxShowUsed, 0, 2);
			tableLayoutPanel1.Controls.Add(this.treeViewGlossListItem, 0, 1);
			tableLayoutPanel1.Name = "tableLayoutPanel1";
			//
			// panel1
			//
			panel1.Controls.Add(this.comboGlossListItem);
			panel1.Controls.Add(this.labelGlossListItem);
			resources.ApplyResources(panel1, "panel1");
			panel1.Name = "panel1";
			//
			// comboGlossListItem
			//
			resources.ApplyResources(this.comboGlossListItem, "comboGlossListItem");
			this.comboGlossListItem.Name = "comboGlossListItem";
			//
			// labelGlossListItem
			//
			resources.ApplyResources(this.labelGlossListItem, "labelGlossListItem");
			this.labelGlossListItem.Name = "labelGlossListItem";
			//
			// checkBoxShowUsed
			//
			resources.ApplyResources(this.checkBoxShowUsed, "checkBoxShowUsed");
			this.checkBoxShowUsed.Name = "checkBoxShowUsed";
			//
			// buttonInfo
			//
			resources.ApplyResources(this.buttonInfo, "buttonInfo");
			this.buttonInfo.Name = "buttonInfo";
			this.buttonInfo.Click += new System.EventHandler(this.OnInfoButtonClick);
			//
			// panelInsertRemove
			//
			panelInsertRemove.Controls.Add(this.buttonInsert);
			panelInsertRemove.Controls.Add(this.buttonRemove);
			resources.ApplyResources(panelInsertRemove, "panelInsertRemove");
			panelInsertRemove.Name = "panelInsertRemove";
			panelInsertRemove.TabStop = true;
			//
			// buttonInsert
			//
			resources.ApplyResources(this.buttonInsert, "buttonInsert");
			this.buttonInsert.Name = "buttonInsert";
			this.buttonInsert.Click += new System.EventHandler(this.OnInsertButtonClick);
			//
			// buttonRemove
			//
			resources.ApplyResources(this.buttonRemove, "buttonRemove");
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.Click += new System.EventHandler(this.OnRemoveButtonClick);
			//
			// panelSelectedGloss
			//
			resources.ApplyResources(panelSelectedGloss, "panelSelectedGloss");
			panelSelectedGloss.Controls.Add(this.groupBoxSelectedGloss, 0, 0);
			panelSelectedGloss.Controls.Add(panel2, 0, 1);
			panelSelectedGloss.Name = "panelSelectedGloss";
			//
			// groupBoxSelectedGloss
			//
			this.groupBoxSelectedGloss.Controls.Add(tableLayoutPanelSelectedGloss);
			resources.ApplyResources(this.groupBoxSelectedGloss, "groupBoxSelectedGloss");
			this.groupBoxSelectedGloss.Name = "groupBoxSelectedGloss";
			this.groupBoxSelectedGloss.TabStop = false;
			//
			// tableLayoutPanelSelectedGloss
			//
			resources.ApplyResources(tableLayoutPanelSelectedGloss, "tableLayoutPanelSelectedGloss");
			tableLayoutPanelSelectedGloss.Controls.Add(this.glossListBoxGloss, 0, 0);
			tableLayoutPanelSelectedGloss.Controls.Add(panel, 1, 0);
			tableLayoutPanelSelectedGloss.Name = "tableLayoutPanelSelectedGloss";
			//
			// panel
			//
			panel.Controls.Add(this.buttonMoveUp);
			panel.Controls.Add(this.buttonMoveDown);
			panel.Controls.Add(this.buttonModify);
			resources.ApplyResources(panel, "panel");
			panel.Name = "panel";
			//
			// buttonMoveUp
			//
			resources.ApplyResources(this.buttonMoveUp, "buttonMoveUp");
			this.buttonMoveUp.Name = "buttonMoveUp";
			this.buttonMoveUp.Click += new System.EventHandler(this.OnMoveUpButtonClick);
			//
			// buttonMoveDown
			//
			resources.ApplyResources(this.buttonMoveDown, "buttonMoveDown");
			this.buttonMoveDown.Name = "buttonMoveDown";
			this.buttonMoveDown.Click += new System.EventHandler(this.OnMoveDownButtonClick);
			//
			// buttonModify
			//
			resources.ApplyResources(this.buttonModify, "buttonModify");
			this.buttonModify.Name = "buttonModify";
			this.buttonModify.Click += new System.EventHandler(this.OnModifyButtonClick);
			//
			// panel2
			//
			panel2.Controls.Add(this.buttonHelp);
			panel2.Controls.Add(this.buttonAcceptGloss);
			panel2.Controls.Add(this.buttonCancel);
			resources.ApplyResources(panel2, "panel2");
			panel2.Name = "panel2";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.OnHelpButtonClick);
			//
			// buttonAcceptGloss
			//
			this.buttonAcceptGloss.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.buttonAcceptGloss, "buttonAcceptGloss");
			this.buttonAcceptGloss.Name = "buttonAcceptGloss";
			this.buttonAcceptGloss.Click += new System.EventHandler(this.OnAcceptGlossButtonClick);
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.buttonCancel, "buttonCancel");
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Click += new System.EventHandler(this.OnCancelButtonClick);
			//
			// panelTop
			//
			panelTop.Controls.Add(tableLayoutPanel2);
			panelTop.Controls.Add(this.textBoxResult);
			resources.ApplyResources(panelTop, "panelTop");
			panelTop.Name = "panelTop";
			panelTop.TabStop = true;
			//
			// tableLayoutPanel2
			//
			resources.ApplyResources(tableLayoutPanel2, "tableLayoutPanel2");
			tableLayoutPanel2.Controls.Add(this.labelConstructedGlossForPrompt, 0, 0);
			tableLayoutPanel2.Controls.Add(this.labelAllomorph, 0, 1);
			tableLayoutPanel2.Name = "tableLayoutPanel2";
			//
			// labelConstructedGlossForPrompt
			//
			resources.ApplyResources(this.labelConstructedGlossForPrompt, "labelConstructedGlossForPrompt");
			this.labelConstructedGlossForPrompt.Name = "labelConstructedGlossForPrompt";
			//
			// labelAllomorph
			//
			resources.ApplyResources(this.labelAllomorph, "labelAllomorph");
			this.labelAllomorph.Name = "labelAllomorph";
			//
			// textBoxResult
			//
			resources.ApplyResources(this.textBoxResult, "textBoxResult");
			this.textBoxResult.Name = "textBoxResult";
			//
			// webBrowserInfo
			//
			resources.ApplyResources(this.webBrowserInfo, "webBrowserInfo");
			this.webBrowserInfo.IsWebBrowserContextMenuEnabled = false;
			this.webBrowserInfo.MinimumSize = new System.Drawing.Size(20, 20);
			this.webBrowserInfo.Name = "webBrowserInfo";
			this.webBrowserInfo.WebBrowserShortcutsEnabled = false;
			//
			// splitContainerHorizontal
			//
			resources.ApplyResources(this.splitContainerHorizontal, "splitContainerHorizontal");
			this.splitContainerHorizontal.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainerHorizontal.Name = "splitContainerHorizontal";
			//
			// splitContainerHorizontal.Panel1
			//
			this.splitContainerHorizontal.Panel1.Controls.Add(this.splitContainerVertical);
			//
			// splitContainerHorizontal.Panel2
			//
			this.splitContainerHorizontal.Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
			this.splitContainerHorizontal.Panel2.Controls.Add(this.webBrowserInfo);
			//
			// treeViewGlossListItem
			//
			resources.ApplyResources(this.treeViewGlossListItem, "treeViewGlossListItem");
			this.treeViewGlossListItem.HideSelection = false;
			this.treeViewGlossListItem.ItemHeight = 16;
			this.treeViewGlossListItem.Name = "treeViewGlossListItem";
			this.treeViewGlossListItem.TerminalsUseCheckBoxes = false;
			this.treeViewGlossListItem.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnGlossListTreeSelect);
			//
			// glossListBoxGloss
			//
			resources.ApplyResources(this.glossListBoxGloss, "glossListBoxGloss");
			this.glossListBoxGloss.FormattingEnabled = true;
			this.glossListBoxGloss.MGADialog = null;
			this.glossListBoxGloss.Name = "glossListBoxGloss";
			this.glossListBoxGloss.SelectedIndexChanged += new System.EventHandler(this.OnGlossListBoxSelectedIndexChanged);
			//
			// MGADialog
			//
			this.AcceptButton = this.buttonAcceptGloss;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.splitContainerHorizontal);
			this.Controls.Add(panelTop);
			this.HelpButton = true;
			this.Name = "MGADialog";
			this.splitContainerVertical.Panel1.ResumeLayout(false);
			this.splitContainerVertical.Panel2.ResumeLayout(false);
			this.splitContainerVertical.ResumeLayout(false);
			panelGlossComponents.ResumeLayout(false);
			this.groupBoxGlossComponents.ResumeLayout(false);
			tableLayoutPanel1.ResumeLayout(false);
			panel1.ResumeLayout(false);
			panelInsertRemove.ResumeLayout(false);
			panelSelectedGloss.ResumeLayout(false);
			this.groupBoxSelectedGloss.ResumeLayout(false);
			tableLayoutPanelSelectedGloss.ResumeLayout(false);
			panel.ResumeLayout(false);
			panel2.ResumeLayout(false);
			panelTop.ResumeLayout(false);
			panelTop.PerformLayout();
			tableLayoutPanel2.ResumeLayout(false);
			this.splitContainerHorizontal.Panel1.ResumeLayout(false);
			this.splitContainerHorizontal.Panel2.ResumeLayout(false);
			this.splitContainerHorizontal.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
		#region Properties
		/// <summary>
		/// Get gloss string result
		/// </summary>
		public string Result
		{
			get
			{
				CheckDisposed();

				return textBoxResult.Text;
			}
		}
		/// <summary>
		/// Get items in the gloss list
		/// </summary>
		public ListBox.ObjectCollection Items
		{
			get
			{
				CheckDisposed();

				return glossListBoxGloss.Items;
			}
		}
		#endregion
	}
}
