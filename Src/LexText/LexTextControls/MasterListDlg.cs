// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MasterListDlg.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.LexText.Controls.MGA;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MasterListDlg.
	/// </summary>
	public class MasterListDlg : Form, IFWDisposable
	{
		protected IFdoOwningCollection<IFsFeatDefn> m_featureList;
		protected bool m_launchedFromInsertMenu = false;
		protected Mediator m_mediator;
		protected FdoCache m_cache;
		protected IHelpTopicProvider m_helpTopicProvider;
		protected IFsFeatDefn m_selFeatDefn;
		protected IFsFeatureSystem m_featureSystem;
		protected bool m_skipEvents = false;
		protected string m_sClassName;
		protected int iCheckedCount;
		protected string m_sWindowKeyLocation;
		protected string m_sWindowKeySize;

		protected Label label1;
		protected Label label2;
		protected GlossListTreeView m_tvMasterList;
		protected RichTextBox m_rtbDescription;
		protected Label label3;
		protected Button m_btnOK;
		protected Button m_btnCancel;
		protected Button m_bnHelp;
		protected PictureBox pictureBox1;
		protected LinkLabel linkLabel1;
		protected ImageList m_imageList;
		protected ImageList m_imageListPictures;
		protected System.ComponentModel.IContainer components;

		protected string s_helpTopic = "khtpInsertInflectionFeature";
		protected System.Windows.Forms.HelpProvider helpProvider;

		public MasterListDlg()
		{
			GlossListTreeView treeView = new GlossListTreeView();
			InitDlg("FsClosedFeature", treeView);
		}
		public MasterListDlg(string className, GlossListTreeView treeView)
		{
			InitDlg(className, treeView);
		}

		private void InitDlg(string className, GlossListTreeView treeView)
		{
			m_sClassName = className;
			m_tvMasterList = treeView;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_tvMasterList.TerminalsUseCheckBoxes = true;
			iCheckedCount = 0;
			pictureBox1.Image = m_imageListPictures.Images[0];
			m_btnOK.Enabled = false; // Disable until we are able to support interaction with the DB list of POSes.
			m_rtbDescription.ReadOnly = true;  // Don't allow any editing
			DoExtraInit();
		}

		protected virtual void DoExtraInit()
		{
			// needs to be overriden
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_cache = null;
			m_selFeatDefn = null;
			m_featureList = null;
			m_mediator = null;

			base.Dispose( disposing );
		}

		public IFsFeatDefn SelectedFeatDefn
		{
			get
			{
				CheckDisposed();
				return m_selFeatDefn;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="featSys"></param>
		/// <param name="mediator"></param>
		/// <param name="launchedFromInsertMenu"></param>
		public void SetDlginfo(IFsFeatureSystem featSys, Mediator mediator, bool launchedFromInsertMenu)
		{
			// default to inflection features
			string sXmlFile = Path.Combine(DirectoryFinder.FWCodeDirectory, String.Format("Language Explorer{0}MGA{0}GlossLists{0}EticGlossList.xml", Path.DirectorySeparatorChar));
			SetDlginfo(featSys, mediator, launchedFromInsertMenu, "masterInflFeatListDlg", sXmlFile);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="featSys"></param>
		/// <param name="mediator"></param>
		/// <param name="launchedFromInsertMenu"></param>
		/// <param name="sWindowKey">used to store location and size of dialog window</param>
		/// <param name="sXmlFile">file containing the XML form of the gloss list</param>
		public void SetDlginfo(IFsFeatureSystem featSys, Mediator mediator, bool launchedFromInsertMenu, string sWindowKey, string sXmlFile)
		{
			CheckDisposed();

			m_featureSystem = featSys;
			m_featureList = featSys.FeaturesOC;
			m_launchedFromInsertMenu = launchedFromInsertMenu;
			m_mediator = mediator;
			if (mediator != null)
			{
				m_sWindowKeyLocation = sWindowKey + "Location";
				m_sWindowKeySize = sWindowKey + "Size";

				ResetWindowLocationAndSize();

				m_helpTopicProvider = m_mediator.HelpTopicProvider;
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
			m_cache = featSys.Cache;
			LoadMasterFeatures(sXmlFile);
			m_tvMasterList.Cache = m_cache;
		}

		private void ResetWindowLocationAndSize()
		{
			// Get location to the stored values, if any.
			object locWnd = m_mediator.PropertyTable.GetValue(m_sWindowKeyLocation);
			object szWnd = m_mediator.PropertyTable.GetValue(m_sWindowKeySize);
			if (locWnd != null && szWnd != null)
			{
				Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
				ScreenUtils.EnsureVisibleRect(ref rect);
				DesktopBounds = rect;
				StartPosition = FormStartPosition.Manual;
			}
		}

		private void LoadMasterFeatures(string sXmlFile)
		{
			m_tvMasterList.LoadGlossListTreeFromXml(sXmlFile, "en");
			// walk tree and set InDatabase info and use ToString() and change color, etc.
			AdjustNodes(m_tvMasterList.Nodes);
		}

		private void AdjustNodes(TreeNodeCollection treeNodes)
		{
			foreach (TreeNode node in treeNodes)
				AdjustNode(node);
		}

		private void AdjustNode(TreeNode treeNode)
		{
			MasterItem mi = (MasterItem)treeNode.Tag;
			mi.DetermineInDatabase(m_cache);
			treeNode.Text = mi.ToString();
			if (mi.InDatabase && treeNode.Nodes.Count == 0)
			{
				try
				{
					m_skipEvents = true;
					treeNode.Checked = true;
					treeNode.ImageIndex = (int)GlossListTreeView.ImageKind.checkedBox;
					treeNode.SelectedImageIndex = treeNode.ImageIndex;
					treeNode.ForeColor = Color.Gray;
				}
				finally
				{
					m_skipEvents = false;
				}
			}
			TreeNodeCollection list = treeNode.Nodes;
			if (list.Count > 0)
			{
				if (!mi.KindCanBeInDatabase() || mi.InDatabase)
				{
					AdjustNodes(treeNode.Nodes);
				}
				DoFinalAdjustment(treeNode);
			}
		}
		protected virtual void DoFinalAdjustment(TreeNode treeNode)
		{
			// default is to do nothing
		}
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MasterListDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			// seems to be crucial that the following is commented off
			//this.m_tvMasterList = new SIL.FieldWorks.LexText.Controls.MGA.GlossListTreeView();
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
			this.m_tvMasterList.Name = "m_tvMasterList";
			this.m_tvMasterList.TerminalsUseCheckBoxes = false;
			this.m_tvMasterList.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.m_tvMasterList_AfterCheck);
			this.m_tvMasterList.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_tvMasterList_AfterSelect);
			this.m_tvMasterList.BeforeCheck += new System.Windows.Forms.TreeViewCancelEventHandler(this.m_tvMasterList_BeforeCheck);
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
			this.linkLabel1.Text = LexTextControls.ksLinkText;
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			//
			// m_imageListPictures
			//
			this.m_imageListPictures.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageListPictures.ImageStream")));
			this.m_imageListPictures.TransparentColor = System.Drawing.Color.Magenta;
			this.m_imageListPictures.Images.SetKeyName(0, "");
			//
			// MasterListDlg
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
			this.Name = "MasterListDlg";
			this.ShowInTaskbar = false;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.MasterListDlg_Closing);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		void m_bnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", s_helpTopic);
		}

		protected void m_tvMasterList_AfterSelect(object sender, TreeViewEventArgs e)
		{
			MasterItem mi = e.Node.Tag as MasterItem;
			mi.ResetDescription(m_rtbDescription);
			ResetOKBtnEnable();
		}
		/// <summary>
		/// Cancel, if it is already in the database.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_tvMasterList_BeforeCheck(object sender, System.Windows.Forms.TreeViewCancelEventArgs e)
		{
			if (m_skipEvents)
				return;

			MasterItem selMC = e.Node.Tag as MasterItem;
			e.Cancel = selMC.InDatabase;
			if (!selMC.InDatabase && selMC.KindCanBeInDatabase())
			{
				if (e.Node.Checked)
					iCheckedCount--;
				else
					iCheckedCount++;
			}
		}

		protected void m_tvMasterList_AfterCheck(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			if (m_skipEvents)
				return;

			ResetOKBtnEnable();
		}

		private void ResetOKBtnEnable()
		{
			if (m_tvMasterList.TerminalsUseCheckBoxes)
			{
				if (iCheckedCount == 0)
					m_btnOK.Enabled = false;
				else
					m_btnOK.Enabled = true;
			}
			else
			{
				TreeNode selNode = m_tvMasterList.SelectedNode;
				if (selNode != null)
				{
					if (HasChosenItemNotInDatabase(selNode))
						m_btnOK.Enabled = true;
					else
						m_btnOK.Enabled = false;
				}
				else
				{
					m_btnOK.Enabled = false; //FoundChosenItemNotInDatabase(m_tvMasterList.Nodes);
				}
			}
		}

		private bool FoundChosenItemNotInDatabase(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (HasChosenItemNotInDatabase(node))
					return true;
				if (FoundChosenItemNotInDatabase(node.Nodes))
					return true;
			}
			return false;
		}
		private bool HasChosenItemNotInDatabase(TreeNode node)
		{
			if (!node.Checked)
				return false;
			MasterItem mi = node.Tag as MasterItem;
			if (mi != null && !mi.InDatabase)
				return true;
			return false;
		}

		/// <summary>
		/// If OK, then add relevant inflection features to DB.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MasterListDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			switch (DialogResult)
			{
				default:
					m_selFeatDefn = null;
					break;
				case DialogResult.OK:
				{
					Cursor = Cursors.WaitCursor;
					if (m_tvMasterList.TerminalsUseCheckBoxes)
					{
						UpdateAllCheckedItems(m_tvMasterList.Nodes);
					}
					else
					{
						MasterItem mi = m_tvMasterList.SelectedNode.Tag as MasterItem;
						if (mi != null)
						{
							mi.AddToDatabase(m_cache);
							m_selFeatDefn = mi.FeatureDefn;
						}
					}
					Cursor = Cursors.Default;
					break;
				}
				case DialogResult.Yes:
				{
					// Closing via the hotlink.
					// Do nothing special, except avoid setting m_selFeatDefn to null, as in the default case.
					break;
				}
			}

			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty(m_sWindowKeyLocation, Location);
				m_mediator.PropertyTable.SetProperty(m_sWindowKeySize, Size);
			}
		}

		private void UpdateAllCheckedItems(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (node.Nodes != null && node.Nodes.Count > 0)
					UpdateAllCheckedItems(node.Nodes);
				else
				{
					if (node.Checked)
					{
						MasterItem mi = node.Tag as MasterItem;
						if (!mi.InDatabase)
						{
							mi.AddToDatabase(m_cache);
							m_selFeatDefn = mi.FeatureDefn;
						}
					}
				}
			}
		}
		protected virtual void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			// should be overriden, but just in case...
			DialogResult = DialogResult.Yes;
			Close();
		}
	}
}
