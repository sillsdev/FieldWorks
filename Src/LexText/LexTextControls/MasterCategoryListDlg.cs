using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.CoreImpl;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MasterCategoryListDlg.
	/// </summary>
	public class MasterCategoryListDlg : Form, IFWDisposable
	{
		private ICmPossibilityList m_posList;
		private bool m_launchedFromInsertMenu = false;
		private Mediator m_mediator;
		private IHelpTopicProvider m_helpTopicProvider;
		private FdoCache m_cache;
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
			m_mediator = null;
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

		/// <summary>
		///
		/// </summary>
		/// <param name="posList"></param>
		/// <param name="mediator"></param>
		/// <param name="launchedFromInsertMenu"></param>
		/// <param name="subItemOwner"></param>
		public void SetDlginfo(ICmPossibilityList posList, Mediator mediator, bool launchedFromInsertMenu, IPartOfSpeech subItemOwner)
		{
			CheckDisposed();

			m_subItemOwner = subItemOwner; // May be null, which is fine.
			m_posList = posList;
			m_launchedFromInsertMenu = launchedFromInsertMenu;
			m_mediator = mediator;
			if (mediator != null)
			{
				// Reset window location.
				// Get location to the stored values, if any.
				object locWnd = m_mediator.PropertyTable.GetValue("masterCatListDlgLocation");
				object szWnd = m_mediator.PropertyTable.GetValue("masterCatListDlgSize");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
				m_helpTopicProvider = m_mediator.HelpTopicProvider;
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
			var posSet = new Set<IPartOfSpeech>();
			foreach (IPartOfSpeech pos in posList.ReallyReallyAllPossibilities)
				posSet.Add(pos);
			LoadMasterCategories(posSet);
		}

		private void LoadMasterCategories(Set<IPartOfSpeech> posSet)
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

		private void AddNodes(Set<IPartOfSpeech> posSet, XmlNodeList nodeList, TreeNodeCollection treeNodes, FdoCache cache)
		{
			foreach (XmlNode node in nodeList)
				AddNode(posSet, node, treeNodes, cache);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void AddNode(Set<IPartOfSpeech> posSet, XmlNode node, TreeNodeCollection treeNodes, FdoCache cache)
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
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

			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("masterCatListDlgLocation", Location);
				m_mediator.PropertyTable.SetProperty("masterCatListDlgSize", Size);
			}
		}

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			if (!m_launchedFromInsertMenu)
				XCore.XMessageBoxExManager.Trigger("CreateNewFromGrammaticalCategoryCatalog");
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

		#region My classes

		internal class MasterCategory
		{
			private bool m_isGroup = false;
			private string m_id;
			private string m_abbrev;
			private string m_abbrevWs;
			private string m_term;
			private string m_termWs;
			private string m_def;
			private string m_defWs;
			private List<MasterCategoryCitation> m_citations;
			private IPartOfSpeech m_pos;
			private XmlNode m_node; // need to remember the node so can put info for *all* writing systems into databas

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			public static MasterCategory Create(Set<IPartOfSpeech> posSet, XmlNode node, FdoCache cache)
			{
				/*
				<item type="category" id="Adjective" guid="30d07580-5052-4d91-bc24-469b8b2d7df9">
					<abbrev ws="en">adj</abbrev>
					<term ws="en">adjective</term>
					<def ws="en">An adjective is a part of speech whose members modify nouns. An adjective specifies the attributes of a noun referent. Note: this is one case among many. Adjectives are a class of modifiers.</def>
					<citation ws="en">Crystal 1997:8</citation>
					<citation ws="en">Mish et al. 1990:56</citation>
					<citation ws="en">Payne 1997:63</citation>
				</item>
				*/

				MasterCategory mc = new MasterCategory();
				mc.m_isGroup = node.SelectNodes("item") != null;
				mc.m_id = XmlUtils.GetManditoryAttributeValue(node, "id");

				foreach (var pos in posSet)
				{
					if (pos.CatalogSourceId == mc.m_id)
					{
						mc.m_pos = pos;
						break;
					}
				}

				mc.m_node = node; // remember node, too, so can put info for all WSes in database

				string sDefaultWS = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
				string sContent;
				mc.m_abbrevWs = GetBestWritingSystemForNamedNode(node, "abbrev", sDefaultWS, cache, out sContent);
				mc.m_abbrev = sContent;

				mc.m_termWs = GetBestWritingSystemForNamedNode(node, "term", sDefaultWS, cache, out sContent);
				mc.m_term = NameFixer(sContent);

				mc.m_defWs = GetBestWritingSystemForNamedNode(node, "def", sDefaultWS, cache, out sContent);
				mc.m_def = sContent;

				foreach (XmlNode citNode in node.SelectNodes("citation"))
					mc.m_citations.Add(new MasterCategoryCitation(XmlUtils.GetManditoryAttributeValue(citNode, "ws"), citNode.InnerText));
				return mc;
			}
			private static string GetBestWritingSystemForNamedNode(XmlNode node, string sNodeName, string sDefaultWS, FdoCache cache, out string sNodeContent)
			{
				string sWS;
				XmlNode nd = node.SelectSingleNode(sNodeName + "[@ws='" + sDefaultWS + "']");
				if (nd == null || nd.InnerText.Length == 0)
				{
					foreach (WritingSystem ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						sWS = ws.Id;
						if (sWS == sDefaultWS)
							continue;
						nd = node.SelectSingleNode(sNodeName + "[@ws='" + sWS + "']");
						if (nd != null && nd.InnerText.Length > 0)
							break;
					}
				}
				if (nd == null)
				{
					sNodeContent = "";
					sWS = sDefaultWS;
				}
				else
				{
					sNodeContent = nd.InnerText;
					sWS = XmlUtils.GetManditoryAttributeValue(nd, "ws");
				}
				return sWS;
			}

			public void AddToDatabase(FdoCache cache, ICmPossibilityList posList, MasterCategory parent, IPartOfSpeech subItemOwner)
			{
				if (m_pos != null)
					return; // It's already in the database, so nothing more can be done.

				UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateCategory, LexTextControls.ksRedoCreateCategory,
					cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					int newOwningFlid;
					int insertLocation;
					int newOwner =
						DeterminePOSLocationInfo(cache, subItemOwner, parent, posList, out newOwningFlid, out insertLocation);
					ILgWritingSystemFactory wsf = cache.WritingSystemFactory;
					Debug.Assert(m_pos != null);

					int termWs = wsf.GetWsFromStr(m_termWs);
					int abbrevWs = wsf.GetWsFromStr(m_abbrevWs);
					int defWs = wsf.GetWsFromStr(m_defWs);
					ITsStrFactory tsf = cache.TsStrFactory;
					if (m_node == null)
					{ // should not happen, but just in case... we still get something useful
						m_pos.Name.set_String(termWs, tsf.MakeString(m_term, termWs));
						m_pos.Abbreviation.set_String(abbrevWs, tsf.MakeString(m_abbrev, abbrevWs));
						m_pos.Description.set_String(defWs, tsf.MakeString(m_def, defWs));
					}
					else
					{
						SetContentFromNode(cache, "abbrev", false, m_pos.Abbreviation);
						SetContentFromNode(cache, "term", true, m_pos.Name);
						SetContentFromNode(cache, "def", false, m_pos.Description);
					}

					m_pos.CatalogSourceId = m_id;
				});
			}

			private void SetContentFromNode(FdoCache cache, string sNodeName, bool fFixName, ITsMultiString item)
			{
				ILgWritingSystemFactory wsf = cache.WritingSystemFactory;
				ITsStrFactory tsf = cache.TsStrFactory;
				int iWS;
				XmlNode nd;
				bool fContentFound = false; // be pessimistic
				foreach (WritingSystem ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
				{
					string sWS = ws.Id;
					nd = m_node.SelectSingleNode(sNodeName + "[@ws='" + sWS + "']");
					if (nd == null || nd.InnerText.Length == 0)
						continue;
					fContentFound = true;
					string sNodeContent;
					if (fFixName)
						sNodeContent = NameFixer(nd.InnerText);
					else
						sNodeContent = nd.InnerText;
					iWS = wsf.GetWsFromStr(sWS);
					item.set_String(iWS, (tsf.MakeString(sNodeContent, iWS)));
				}
				if (!fContentFound)
				{
					iWS = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
					item.set_String(iWS, tsf.MakeString("", iWS));
				}
			}

			private int DeterminePOSLocationInfo(FdoCache cache, IPartOfSpeech subItemOwner, MasterCategory parent, ICmPossibilityList posList,
				out int newOwningFlid, out int insertLocation)
			{
				int newOwner;
				// The XML node is from a file shipped with FieldWorks. It is quite likely multiple users
				// of a project could independently add the same items, so we create them with fixed guids
				// so merge will recognize them as the same objects.
				//// LT-14511 However, if the partOfSpeech is being added to a reversal index, a different guid needs to be used
				//// than the ones in the file shipped with FieldWorks. In this case if two users add the same POS to the
				//// reversal index at the same time and then do a Send/Receive operation, then a merge conflict report
				//// will probably be created for this. This scenario is not likely to occur very often at all so having
				//// a conflict report created for when this happens is something we can live with.
				Guid guid;
				if (posList.Owner is IReversalIndex)
					guid = new Guid();
				else
					guid = new Guid(XmlUtils.GetAttributeValue(m_node, "guid"));
				var posFactory = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
				if (subItemOwner != null)
				{
					newOwner = subItemOwner.Hvo;
					newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
					insertLocation = subItemOwner.SubPossibilitiesOS.Count;
					m_pos = posFactory.Create(guid, subItemOwner);
				}
				else if (parent != null && parent.m_pos != null)
				{
					newOwner = parent.m_pos.Hvo;
					newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
					insertLocation = parent.m_pos.SubPossibilitiesOS.Count;
					m_pos = posFactory.Create(guid, parent.m_pos);
				}
				else
				{
					newOwner = posList.Hvo;
					newOwningFlid = CmPossibilityListTags.kflidPossibilities;
					insertLocation = posList.PossibilitiesOS.Count;
					m_pos = posFactory.Create(guid,posList); // automatically adds to parent.
				}
				return newOwner;
			}

			/// <summary>
			/// Ensures the first letter is uppercase, and that uppercase letters after the first letter, start a new word.
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			private static string NameFixer(string name)
			{
				if (name == null || name.Length == 0)
					return name;

				char c = name[0];
				if (char.IsLetter(c) && char.IsLower(c))
					name = char.ToUpper(c).ToString() + name.Substring(1, name.Length - 1);

				// Add space before each upper case letter, after the first one.
				for (int i = name.Length - 1; i > 0; --i)
				{
					c = name[i];
					if (char.IsLetter(c) && char.IsUpper(c))
						name = name.Insert(i, " ");
				}

				return name;
			}

			public MasterCategory()
			{
				m_citations = new List<MasterCategoryCitation>();
			}

			public bool IsGroup
			{
				get
				{
					return m_isGroup;
				}
			}

			public IPartOfSpeech POS
			{
				get
				{
					return m_pos;
				}
			}

			public bool InDatabase
			{
				get
				{
					return m_pos != null;
				}
			}

			public void ResetDescription(RichTextBox rtbDescription)
			{

				rtbDescription.Clear();

				var doubleNewLine = Environment.NewLine + Environment.NewLine;
				Font original = rtbDescription.SelectionFont;
				Font fntBold = new Font(original.FontFamily, original.Size, FontStyle.Bold);
				Font fntItalic = new Font(original.FontFamily, original.Size, FontStyle.Italic);
				rtbDescription.SelectionFont = fntBold;
				rtbDescription.AppendText(m_term);
				rtbDescription.AppendText(doubleNewLine);

				rtbDescription.SelectionFont = (m_def == null || m_def == String.Empty) ? fntItalic : original;
				rtbDescription.AppendText((m_def == null || m_def == String.Empty) ? LexTextControls.ksUndefinedItem : m_def);
				rtbDescription.AppendText(doubleNewLine);

				if (m_citations.Count > 0)
				{
					rtbDescription.SelectionFont = fntItalic;
					rtbDescription.AppendText(LexTextControls.ksReferences);
					rtbDescription.AppendText(doubleNewLine);

					rtbDescription.SelectionFont = original;
					foreach (MasterCategoryCitation mcc in m_citations)
						mcc.ResetDescription(rtbDescription);
				}
#if __MonoCS__
				// Ensure that the top of the description is showing (FWNX-521).
				rtbDescription.Select(0,0);
				rtbDescription.ScrollToCaret();
#endif
			}

			public override string ToString()
			{
				if (InDatabase)
					return String.Format(LexTextControls.ksXInFwProject, m_term);
				else
					return m_term;
			}
		}


		internal class MasterCategoryCitation
		{
			private string m_ws;
			private string m_citation;

			public string WS
			{
				get { return m_ws; }
			}

			public string Citation
			{
				get { return m_citation; }
			}

			public MasterCategoryCitation(string ws, string citation)
			{
				m_ws = ws;
				m_citation = citation;
			}

			public void ResetDescription(RichTextBox rtbDescription)
			{
				rtbDescription.AppendText( String.Format(LexTextControls.ksBullettedItem,
					m_citation, System.Environment.NewLine));
			}
		}


		#endregion My classes
	}
}
