// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.FwUtils.MessageBoxEx;
using SIL.LCModel;
using SIL.Windows.Forms;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Summary description for MasterCategoryListDlg.
	/// </summary>
	public class MasterCategoryListDlg : Form
	{
		private ICmPossibilityList m_posList;
		private bool m_launchedFromInsertMenu = false;
		private IPropertyTable m_propertyTable;
		private IHelpTopicProvider m_helpTopicProvider;
		private LcmCache m_cache;
		private List<TreeNode> m_nodes = new List<TreeNode>();
		private IPartOfSpeech m_selPOS;
		private bool m_skipEvents = false;
		private IPartOfSpeech m_subItemOwner;

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TreeView m_tvMasterList;
		private System.Windows.Forms.RichTextBox m_rtbDescription;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_bnHelp;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.ImageList m_imageList;
		private System.Windows.Forms.ImageList m_imageListPictures;
		private System.ComponentModel.IContainer components;

		private const string s_helpTopic = "khtpAddFromCatalog";
		private System.Windows.Forms.HelpProvider helpProvider;

		public MasterCategoryListDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			string sCat = LexTextControls.kscategory;
			linkLabel1.Text = String.Format(LexTextControls.ksLinkText, sCat, sCat);

			pictureBox1.Image = m_imageListPictures.Images[0];
			m_btnOK.Enabled = false; // Disable until we are able to support interaction with the DB list of POSes.
			m_rtbDescription.ReadOnly = true;  // Don't allow any editing
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
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
				if (m_nodes != null)
					m_nodes.Clear();
			}
			m_posList = null;
			m_cache = null;
			m_selPOS = null;
			m_nodes = null;

			base.Dispose( disposing );
		}

		public IPartOfSpeech SelectedPOS
		{
			get
			{
				CheckDisposed();
				return m_selPOS;
			}
		}

		///  <summary>
		///
		///  </summary>
		///  <param name="posList"></param>
		/// <param name="propertyTable"></param>
		/// <param name="launchedFromInsertMenu"></param>
		///  <param name="subItemOwner"></param>
		public void SetDlginfo(ICmPossibilityList posList, IPropertyTable propertyTable, bool launchedFromInsertMenu, IPartOfSpeech subItemOwner)
		{
			CheckDisposed();

			m_subItemOwner = subItemOwner; // May be null, which is fine.
			m_posList = posList;
			m_launchedFromInsertMenu = launchedFromInsertMenu;
			m_propertyTable = propertyTable;
			if (m_propertyTable != null)
			{
				// Reset window location.
				// Get location to the stored values, if any.
				Point dlgLocation;
				Size dlgSize;
				if (m_propertyTable.TryGetValue("masterCatListDlgLocation", out dlgLocation) &&
					m_propertyTable.TryGetValue("masterCatListDlgSize", out dlgSize))
				{
					Rectangle rect = new Rectangle(dlgLocation, dlgSize);
					ScreenHelper.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
				m_helpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
				if (m_helpTopicProvider != null)
				{
					helpProvider = new HelpProvider();
					helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
					helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
					helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
				}
			}
			m_bnHelp.Enabled = (m_helpTopicProvider != null);

			Debug.Assert(posList != null);
			m_cache = posList.Cache;
			var posSet = new HashSet<IPartOfSpeech>();
			foreach (IPartOfSpeech pos in posList.ReallyReallyAllPossibilities)
				posSet.Add(pos);
			LoadMasterCategories(posSet);
		}

		private void LoadMasterCategories(HashSet<IPartOfSpeech> posSet)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(Path.Combine(FwDirectoryFinder.TemplateDirectory, "GOLDEtic.xml"));
			XmlElement root = doc.DocumentElement;
			AddNodes(posSet, root.SelectNodes("/eticPOSList/item"), m_tvMasterList.Nodes, m_cache);

			// The expand/collapse cycle is to ensure that all the folder icons get set
			m_tvMasterList.ExpandAll();
			m_tvMasterList.CollapseAll();

			// Select the first node in the list
			TreeNode node = m_tvMasterList.Nodes[0];
			m_tvMasterList.SelectedNode = node;
			// Then try to find a root-level node that is not yet installed and select it if we
			// can, without moving the scrollbar (see LT-7441).
			do
			{
				if (!(node.Tag is MasterCategory))
					continue;
				if (!(node.Tag as MasterCategory).InDatabase)
					break;
				// DownArrow moves the selection without affecting the scroll position (unless
				// the selection was at the bottom).
				Win32.SendMessage(m_tvMasterList.Handle, Win32.WinMsgs.WM_KEYDOWN,
					(int)Keys.Down, 0);
			} while ((node = node.NextNode) != null);
		}

		private void AddNodes(HashSet<IPartOfSpeech> posSet, XmlNodeList nodeList, TreeNodeCollection treeNodes, LcmCache cache)
		{
			foreach (XmlNode node in nodeList)
				AddNode(posSet, node, treeNodes, cache);
		}

		private void AddNode(HashSet<IPartOfSpeech> posSet, XmlNode node, TreeNodeCollection treeNodes, LcmCache cache)
		{
			if (node.Attributes["id"].InnerText == "PartOfSpeechValue")
			{
				AddNodes(posSet, node.SelectNodes("item"), treeNodes, cache);
				return; // Skip the top level node.
			}
			MasterCategory mc = MasterCategory.Create(posSet, node, cache);
			TreeNode tn = new TreeNode();
			tn.Tag = mc;
			tn.Text = TsStringUtils.NormalizeToNFC(mc.ToString());
			if (mc.InDatabase)
			{
				try
				{
					m_skipEvents = true;
					tn.Checked = true;
					tn.ForeColor = Color.Gray;
				}
				finally
				{
					m_skipEvents = false;
				}
			}

			treeNodes.Add(tn);
			m_nodes.Add(tn);
			XmlNodeList list = node.SelectNodes("item");
			if (list.Count > 0)
				AddNodes(posSet, list, tn.Nodes, cache);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MasterCategoryListDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_tvMasterList = new System.Windows.Forms.TreeView();
			this.m_imageList = new System.Windows.Forms.ImageList(this.components);
			this.m_rtbDescription = new System.Windows.Forms.RichTextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_bnHelp = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.m_imageListPictures = new System.Windows.Forms.ImageList(this.components);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_tvMasterList
			//
			resources.ApplyResources(this.m_tvMasterList, "m_tvMasterList");
			this.m_tvMasterList.FullRowSelect = true;
			this.m_tvMasterList.HideSelection = false;
			this.m_tvMasterList.ImageList = this.m_imageList;
			this.m_tvMasterList.Name = "m_tvMasterList";
			this.m_tvMasterList.Sorted = true;
			this.m_tvMasterList.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.m_tvMasterList_AfterCheck);
			this.m_tvMasterList.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.m_tvMasterList_AfterCollapse);
			this.m_tvMasterList.DoubleClick += new System.EventHandler(this.m_tvMasterList_DoubleClick);
			this.m_tvMasterList.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_tvMasterList_AfterSelect);
			this.m_tvMasterList.BeforeCheck += new System.Windows.Forms.TreeViewCancelEventHandler(this.m_tvMasterList_BeforeCheck);
			this.m_tvMasterList.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.m_tvMasterList_AfterExpand);
			//
			// m_imageList
			//
			this.m_imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList.ImageStream")));
			this.m_imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.m_imageList.Images.SetKeyName(0, "");
			this.m_imageList.Images.SetKeyName(1, "");
			this.m_imageList.Images.SetKeyName(2, "");
			//
			// m_rtbDescription
			//
			resources.ApplyResources(this.m_rtbDescription, "m_rtbDescription");
			this.m_rtbDescription.Name = "m_rtbDescription";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
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
			// m_bnHelp
			//
			resources.ApplyResources(this.m_bnHelp, "m_bnHelp");
			this.m_bnHelp.Name = "m_bnHelp";
			this.m_bnHelp.Click += new System.EventHandler(this.m_bnHelp_Click);
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// linkLabel1
			//
			resources.ApplyResources(this.linkLabel1, "linkLabel1");
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.TabStop = true;
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			//
			// m_imageListPictures
			//
			this.m_imageListPictures.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageListPictures.ImageStream")));
			this.m_imageListPictures.TransparentColor = System.Drawing.Color.Magenta;
			this.m_imageListPictures.Images.SetKeyName(0, "");
			//
			// MasterCategoryListDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.m_bnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.m_rtbDescription);
			this.Controls.Add(this.m_tvMasterList);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MasterCategoryListDlg";
			this.ShowInTaskbar = false;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.MasterCategoryListDlg_Closing);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

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

		private void m_tvMasterList_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			MasterCategory mc = e.Node.Tag as MasterCategory;
			mc.ResetDescription(m_rtbDescription);
			ResetOKBtnEnable();
		}

		private void m_tvMasterList_DoubleClick(object sender, System.EventArgs e)
		{
			TreeNode tn = m_tvMasterList.GetNodeAt(m_tvMasterList.PointToClient(Cursor.Position));
			m_tvMasterList.SelectedNode = tn;
			MasterCategory mc = tn.Tag as MasterCategory;
			if (!mc.InDatabase)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void m_tvMasterList_AfterExpand(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			MasterCategory selMC = e.Node.Tag as MasterCategory;
			if (selMC.IsGroup)
			{
				e.Node.ImageIndex = 1;
				e.Node.SelectedImageIndex = 1;
			}
		}

		private void m_tvMasterList_AfterCollapse(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			MasterCategory selMC = e.Node.Tag as MasterCategory;
			if (selMC.IsGroup)
			{
				e.Node.ImageIndex = 0;
				e.Node.SelectedImageIndex = 0;
			}
		}

		/// <summary>
		/// Cancel, if it is already in the database.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_tvMasterList_BeforeCheck(object sender, System.Windows.Forms.TreeViewCancelEventArgs e)
		{
			if (m_skipEvents)
				return;

			MasterCategory selMC = e.Node.Tag as MasterCategory;
			e.Cancel = selMC.InDatabase;
		}

		private void m_tvMasterList_AfterCheck(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			if (m_skipEvents)
				return;

			ResetOKBtnEnable();
		}

		private void ResetOKBtnEnable()
		{
			bool haveCheckeditems = false;
			foreach (TreeNode node in m_nodes)
			{
				if (node.Checked)
				{
					haveCheckeditems = true;
					break;
				}
			}
			TreeNode selNode = m_tvMasterList.SelectedNode;
			m_btnOK.Enabled = haveCheckeditems
				|| (selNode != null
						&& !(selNode.Tag as MasterCategory).InDatabase);
		}

		/// <summary>
		/// If OK, then add relevant POSes to DB.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MasterCategoryListDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			switch (DialogResult)
			{
				default:
					m_selPOS = null;
					break;
				case DialogResult.OK:
				{
					// Closing with normal selection(s).
					foreach (TreeNode tn in m_nodes)
					{
						MasterCategory mc = tn.Tag as MasterCategory;
						Debug.Assert(mc != null);
						if ((tn.Checked || (tn == m_tvMasterList.SelectedNode))
							&& !mc.InDatabase)
						{
							// if this.m_subItemOwner != null, it indicates where to put the newly chosed POS
							mc.AddToDatabase(m_cache,
								m_posList,
								(tn.Parent == null) ? null : tn.Parent.Tag as MasterCategory,
								m_subItemOwner);
						}
					}
					MasterCategory mc2 = m_tvMasterList.SelectedNode.Tag as MasterCategory;
					Debug.Assert(mc2 != null);
					m_selPOS = mc2.POS;
					Debug.Assert(m_selPOS != null);
					break;
				}
				case DialogResult.Yes:
				{
					// Closing via the hotlink.
					// Do nothing special, except avoid setting m_selPOS to null, as in the default case.
					break;
				}
			}

			if (m_propertyTable != null)
			{
				m_propertyTable.SetProperty("masterCatListDlgLocation", Location, true, true);
				m_propertyTable.SetProperty("masterCatListDlgSize", Size, true, true);
			}
		}

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			if (!m_launchedFromInsertMenu)
				MessageBoxExManager.Trigger("CreateNewFromGrammaticalCategoryCatalog");
			m_cache.DomainDataByFlid.BeginUndoTask(LexTextControls.ksUndoInsertCategory,
				LexTextControls.ksRedoInsertCategory);
			var posFactory = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			if (m_subItemOwner != null)
			{
				m_selPOS = posFactory.Create();
				m_subItemOwner.SubPossibilitiesOS.Add(m_selPOS);
			}
			else
			{
				m_selPOS = posFactory.Create();
				m_posList.PossibilitiesOS.Add(m_selPOS);
			}
			m_cache.DomainDataByFlid.EndUndoTask();
			DialogResult = DialogResult.Yes;
			Close();
		}

		private void m_bnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
	}
}