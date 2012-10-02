// This really needs to be refactored with MasterCategoryListDlg.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FdoUi;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for FeatureConstraintChooserDlg.
	/// </summary>
	public class FeatureConstraintChooserDlg : Form, IFWDisposable
	{
		private XCore.Mediator m_mediator;
		private FdoCache m_cache;
		private IPhRegularRule m_rule;
		private IPhSimpleContextNC m_ctxt;
		private FwLink m_link;

		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_bnHelp;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.Label labelPrompt;
		private System.Windows.Forms.Panel m_listPanel;

		private FwComboBox m_valuesCombo;

		private const string s_helpTopic = "khtpChoose-FeatConstr";
		private BrowseViewer m_bvList;
		private int m_fakeFlid;
		private HelpProvider m_helpProvider;
		private int m_dummyPolarityFlid;

		public FeatureConstraintChooserDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_valuesCombo = new FwComboBox();
			m_valuesCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			m_valuesCombo.AdjustStringHeight = false;
			m_valuesCombo.Padding = new Padding(0, 1, 0, 0);
			m_valuesCombo.BackColor = SystemColors.Window;
			m_valuesCombo.SelectedIndexChanged += new EventHandler(m_valuesCombo_SelectedIndexChanged);

			if (FwApp.App != null) // Will be null when running tests
			{
				m_helpProvider = new System.Windows.Forms.HelpProvider();
				m_helpProvider.HelpNamespace = FwApp.App.HelpFile;
				m_helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
				m_helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			}
		}

		#region OnLoad
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
			base.OnLoad(e);
			if (this.Size != size)
				this.Size = size;
			PopulateValuesCombo();
			PositionValuesCombo();
		}

		#endregion

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
				if (m_helpProvider != null)
					m_helpProvider.Dispose();
			}
			if (m_cache != null)
			{
				IVwCacheDa cda = m_cache.MainCacheAccessor as IVwCacheDa;
				cda.CacheVecProp(m_cache.LangProject.Hvo, m_fakeFlid, null, 0);
				cda = null;
				m_cache = null;
			}
			m_ctxt = null;
			m_mediator = null;
			m_bvList = null;
			m_valuesCombo = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Init the dialog with a simple context.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="fs"></param>
		public void SetDlgInfo(FdoCache cache, XCore.Mediator mediator, IPhRegularRule rule, IPhSimpleContextNC ctxt)
		{
			CheckDisposed();

			m_rule = rule;
			m_ctxt = ctxt;
			RestoreWindowPosition(mediator);
			m_cache = cache;

			m_valuesCombo.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_valuesCombo.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_valuesCombo.WritingSystemCode = m_cache.DefaultUserWs;
			m_valuesCombo.Items.Add(MEStrings.ksFeatConstrAgree);
			m_valuesCombo.Items.Add(MEStrings.ksFeatConstrDisagree);
			m_valuesCombo.Items.Add(MEStrings.ks_DontCare_);

			List<int> hvos = new List<int>();
			IPhNCFeatures natClass = m_ctxt.FeatureStructureRA as IPhNCFeatures;
			foreach (int hvo in m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.HvoArray)
			{
				if (natClass.FeaturesOAHvo == 0 || natClass.FeaturesOA.FindClosedValue(hvo) == null)
					hvos.Add(hvo);
			}
			LoadPhonFeats(hvos);
			BuildInitialBrowseView(mediator, hvos);
		}

		public void HandleJump()
		{
			if (m_link != null)
				m_mediator.PostMessage("FollowLink", m_link);
		}

		void m_bvList_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			PopulateValuesCombo();
			PositionValuesCombo();
		}

		private void PositionValuesCombo()
		{
			if (m_bvList == null)
				return;
			HScrollProperties hprops = m_bvList.Scroller.HorizontalScroll;
			int iValueLocationHorizontalOffset = hprops.Value;
			Rectangle valueLocation = m_bvList.LocationOfCellInSelectedRow("Polarity");
			m_valuesCombo.Location = new Point(valueLocation.Left + m_listPanel.Left + 2 - iValueLocationHorizontalOffset,
											   valueLocation.Top + m_listPanel.Top - 3);
			m_valuesCombo.Size = new Size(valueLocation.Width + 1, valueLocation.Height + 4);
			if (!Controls.Contains(m_valuesCombo))
				Controls.Add(m_valuesCombo);
			if (IsValuesComboBoxVisible(hprops))
			{
				m_valuesCombo.Visible = true;
				m_valuesCombo.BringToFront();
			}
			else
			{
				m_valuesCombo.Visible = false;
				m_valuesCombo.SendToBack();
			}
		}

		private bool IsValuesComboBoxVisible(HScrollProperties hprops)
		{
			int iVerticalScrollBarWidth = (m_bvList.ScrollBar.Visible) ? SystemInformation.VerticalScrollBarWidth : 0;
			int iHorizontalScrollBarHeight = (hprops.Visible) ? SystemInformation.HorizontalScrollBarHeight : 0;

			if (m_valuesCombo.Top < (m_listPanel.Top + m_bvList.BrowseView.Top))
				return false;  // too high
			if (m_valuesCombo.Bottom > (m_listPanel.Bottom - iHorizontalScrollBarHeight))
				return false; // too low
			if (m_valuesCombo.Right > (m_listPanel.Right - iVerticalScrollBarWidth + 1))
				return false; // too far to the right
			if (m_valuesCombo.Left < m_listPanel.Left)
				return false; // too far to the left
			return true;
		}

		private void PopulateValuesCombo()
		{
			int selIndex = m_bvList.SelectedIndex;
			if (selIndex < 0)
			{
				if (Controls.Contains(m_valuesCombo))
					Controls.Remove(m_valuesCombo);
				return;
			}
			int hvoSel = m_bvList.AllItems[selIndex];

			string str = m_cache.GetUnicodeProperty(hvoSel, m_dummyPolarityFlid);
			for (int i = 0; i < m_valuesCombo.Items.Count; i++)
			{
				string comboStr = m_valuesCombo.Items[i] as string;
				if (str == comboStr || (str == null && comboStr == MEStrings.ks_DontCare_))
				{
					m_valuesCombo.SelectedIndex = i;
					break;
				}
			}
		}

		void m_valuesCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// make sure the dummy value reflects the selected value
			int selectedRowIndex = m_bvList.SelectedIndex;
			int hvoSel = m_bvList.AllItems[selectedRowIndex];

			string str = m_valuesCombo.SelectedItem as string;
			if (str == MEStrings.ks_DontCare_)
				str = "";

			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			cda.CacheUnicodeProp(hvoSel, m_dummyPolarityFlid, str, str.Length);
		}

		private void BuildInitialBrowseView(XCore.Mediator mediator, List<int> featureHvos)
		{
			XmlNode configurationParameters = (XmlNode)mediator.PropertyTable.GetValue("WindowConfiguration");
			XmlNode toolNode = configurationParameters.SelectSingleNode("controls/parameters/guicontrol[@id='FeatureConstraintFlatList']/parameters");

			m_listPanel.SuspendLayout();
			m_fakeFlid = FdoCache.DummyFlid;
			IVwCacheDa cda = m_cache.MainCacheAccessor as IVwCacheDa;
			cda.CacheVecProp(m_cache.LangProject.Hvo, m_fakeFlid, featureHvos.ToArray(), featureHvos.Count);
			m_bvList = new BrowseViewer(toolNode, m_cache.LangProject.Hvo, m_fakeFlid, m_cache, mediator, null);
			m_bvList.SelectionChanged += new FwSelectionChangedEventHandler(m_bvList_SelectionChanged);
			m_bvList.ScrollBar.ValueChanged += new EventHandler(ScrollBar_ValueChanged);
			m_bvList.Scroller.Scroll += new ScrollEventHandler(Scroller_Scroll);
			m_bvList.ColumnsChanged += new EventHandler(m_bvList_ColumnsChanged);
			m_bvList.Resize += new EventHandler(m_bvList_Resize);
			m_bvList.TabStop = true;
			m_bvList.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_bvList.Dock = DockStyle.Fill;
			m_bvList.BackColor = SystemColors.Window;
			m_listPanel.Controls.Add(m_bvList);
			m_listPanel.ResumeLayout(false);
		}

		void m_bvList_ColumnsChanged(object sender, EventArgs e)
		{
			PositionValuesCombo();
		}

		void Scroller_Scroll(object sender, ScrollEventArgs e)
		{
			PositionValuesCombo();
		}

		void m_bvList_Resize(object sender, EventArgs e)
		{
			PositionValuesCombo();
		}

		void ScrollBar_ValueChanged(object sender, EventArgs e)
		{
			PositionValuesCombo();
		}

		private void RestoreWindowPosition(XCore.Mediator mediator)
		{
			m_mediator = mediator;
			if (mediator != null)
			{
				// Reset window location.
				// Get location to the stored values, if any.
				object locWnd = m_mediator.PropertyTable.GetValue("featConstrListDlgLocation");
				object szWnd = m_mediator.PropertyTable.GetValue("featConstrListDlgSize");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}
		}

		/// <summary>
		/// Load the tree items if the starting point is a simple context.
		/// </summary>
		/// <param name="fs"></param>
		private void LoadPhonFeats(List<int> featureHvos)
		{
			m_dummyPolarityFlid = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor, "FsClosedFeature", "DummyPolarity",
																	(int)CellarModuleDefns.kcptUnicode).Tag;
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			foreach (int hvoClosedFeature in featureHvos)
			{
				string str = null;
				if (ContainsFeature(m_ctxt.PlusConstrRS, hvoClosedFeature))
					str = MEStrings.ksFeatConstrAgree;
				else if (ContainsFeature(m_ctxt.MinusConstrRS, hvoClosedFeature))
					str = MEStrings.ksFeatConstrDisagree;
				else
					str = "";
				cda.CacheUnicodeProp(hvoClosedFeature, m_dummyPolarityFlid, str, str.Length);
			}
		}

		bool ContainsFeature(FdoSequence<IPhFeatureConstraint> vars, int hvoClosedFeature)
		{
			foreach (IPhFeatureConstraint var in vars)
			{
				if (var.FeatureRAHvo == hvoClosedFeature)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Get the simple context.
		/// </summary>
		public IPhSimpleContextNC Context
		{
			get
			{
				CheckDisposed();

				return m_ctxt;
			}
		}
		/// <summary>
		/// Get/Set prompt text
		/// </summary>
		public string Prompt
		{
			get
			{
				CheckDisposed();

				return labelPrompt.Text;
			}
			set
			{
				CheckDisposed();

				string s1 = value ?? MEStrings.ksPhonologicalFeatures;
				labelPrompt.Text = s1;
			}
		}
		/// <summary>
		/// Get/Set dialog title text
		/// </summary>
		public string Title
		{
			get
			{
				CheckDisposed();

				return Text;
			}
			set
			{
				CheckDisposed();

				Text = value;
			}
		}
		/// <summary>
		/// Get/Set link text
		/// </summary>
		public string LinkText
		{
			get
			{
				CheckDisposed();

				return linkLabel1.Text;
			}
			set
			{
				CheckDisposed();

				string s1 = value ?? MEStrings.ksPhonologicalFeaturesAdd;
				linkLabel1.Text = s1;
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FeatureConstraintChooserDlg));
			this.labelPrompt = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_bnHelp = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.m_listPanel = new System.Windows.Forms.Panel();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// labelPrompt
			//
			resources.ApplyResources(this.labelPrompt, "labelPrompt");
			this.labelPrompt.Name = "labelPrompt";
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
			// m_listPanel
			//
			resources.ApplyResources(this.m_listPanel, "m_listPanel");
			this.m_listPanel.Name = "m_listPanel";
			//
			// FeatureConstraintChooserDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_listPanel);
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.m_bnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.labelPrompt);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FeatureConstraintChooserDlg";
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (DialogResult == DialogResult.OK)
				UpdateFeatureConstraints();

			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("featConstrListDlgLocation", Location);
				m_mediator.PropertyTable.SetProperty("featConstrListDlgSize", Size);
			}

			base.OnClosing(e);
		}

		/// <summary>
		/// Updates the feature constraints in the context.
		/// </summary>
		void UpdateFeatureConstraints()
		{
			CheckDisposed();

			List<int> featureHvos = m_bvList.AllItems;
			foreach (int hvoClosedFeature in featureHvos)
			{
				string str = m_cache.GetUnicodeProperty(hvoClosedFeature, m_dummyPolarityFlid);
				if (str == null)
				{
					IPhFeatureConstraint removedConstr = RemoveFeatureConstraint(m_ctxt.PlusConstrRS, hvoClosedFeature);
					if (removedConstr == null)
						removedConstr = RemoveFeatureConstraint(m_ctxt.MinusConstrRS, hvoClosedFeature);

					if (removedConstr != null)
					{
						bool found = false;
						foreach (int hvo in m_rule.FeatureConstraints)
						{
							if (removedConstr.Hvo == hvo)
							{
								found = true;
								break;
							}
						}
						if (!found)
							m_cache.LangProject.PhonologicalDataOA.FeatConstraintsOS.Remove(removedConstr);
					}
				}
				else
				{
					IPhFeatureConstraint var = GetFeatureConstraint(m_rule.FeatureConstraints, hvoClosedFeature);
					if (var == null)
					{
						var = new PhFeatureConstraint();
						m_cache.LangProject.PhonologicalDataOA.FeatConstraintsOS.Append(var);
						var.FeatureRAHvo = hvoClosedFeature;
						var.NotifyNew();
					}

					if (str == MEStrings.ksFeatConstrAgree)
					{
						if (!m_ctxt.PlusConstrRS.Contains(var))
						{
							m_ctxt.PlusConstrRS.Append(var);
							RemoveFeatureConstraint(m_ctxt.MinusConstrRS, hvoClosedFeature);
						}
					}
					else if (str == MEStrings.ksFeatConstrDisagree)
					{
						if (!m_ctxt.MinusConstrRS.Contains(var))
						{
							m_ctxt.MinusConstrRS.Append(var);
							RemoveFeatureConstraint(m_ctxt.PlusConstrRS, hvoClosedFeature);
						}
					}
				}
			}
		}

		IPhFeatureConstraint RemoveFeatureConstraint(FdoReferenceSequence<IPhFeatureConstraint> featConstrs, int hvoClosedFeature)
		{
			IPhFeatureConstraint constrToRemove = GetFeatureConstraint(featConstrs.HvoArray, hvoClosedFeature);
			if (constrToRemove != null)
				featConstrs.Remove(constrToRemove);
			return constrToRemove;
		}

		IPhFeatureConstraint GetFeatureConstraint(IEnumerable<int> featConstrs, int hvoClosedFeature)
		{
			IPhFeatureConstraint result = null;
			foreach (int hvo in featConstrs)
			{
				IPhFeatureConstraint curConstr = new PhFeatureConstraint(m_cache, hvo);
				if (curConstr.FeatureRAHvo == hvoClosedFeature)
				{
					result = curConstr;
					break;
				}
			}
			return result;
		}

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			m_link = FwLink.Create("phonologicalFeaturesAdvancedEdit", m_cache.LangProject.PhFeatureSystemOA.Guid,
				m_cache.ServerName, m_cache.DatabaseName);
			m_btnCancel.PerformClick();
			DialogResult = DialogResult.Ignore;
		}

		private void m_bnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}
	}
}
