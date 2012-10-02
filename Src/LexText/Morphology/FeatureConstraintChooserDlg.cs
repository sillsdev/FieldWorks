// This really needs to be refactored with MasterCategoryListDlg.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
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
		private FwLinkArgs m_link = null;
		private FeatureConstraintPublisher m_sda;

		private Button m_btnOK;
		private Button m_btnCancel;
		private Button m_bnHelp;
		private PictureBox pictureBox1;
		private LinkLabel linkLabel1;
		private Label labelPrompt;
		private Panel m_listPanel;

		private FwComboBox m_valuesCombo;

		private const string s_helpTopic = "khtpChoose-FeatConstr";
		private BrowseViewer m_bvList;
		private HelpProvider m_helpProvider;

		public FeatureConstraintChooserDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			m_valuesCombo = new FwComboBox();
			m_valuesCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			m_valuesCombo.AdjustStringHeight = false;
			m_valuesCombo.Padding = new Padding(0, 1, 0, 0);
			m_valuesCombo.BackColor = SystemColors.Window;
			m_valuesCombo.SelectedIndexChanged += new EventHandler(m_valuesCombo_SelectedIndexChanged);
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_helpProvider != null)
					m_helpProvider.Dispose();
			}
			m_cache = null;
			m_ctxt = null;
			m_mediator = null;
			m_bvList = null;
			m_valuesCombo = null;
			m_sda = null;

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
		Mediator = mediator;
			m_cache = cache;

			m_valuesCombo.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_valuesCombo.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_valuesCombo.WritingSystemCode = m_cache.DefaultUserWs;
			m_valuesCombo.Items.Add(MEStrings.ksFeatConstrAgree);
			m_valuesCombo.Items.Add(MEStrings.ksFeatConstrDisagree);
			m_valuesCombo.Items.Add(MEStrings.ks_DontCare_);

			var feats = new HashSet<IFsFeatDefn>();
			var natClass = m_ctxt.FeatureStructureRA as IPhNCFeatures;
			foreach (var feat in m_cache.LangProject.PhFeatureSystemOA.FeaturesOC)
			{
				if (natClass.FeaturesOA == null || natClass.FeaturesOA.GetValue(feat as IFsClosedFeature) == null)
					feats.Add(feat);
			}
			LoadPhonFeats(feats);
			BuildInitialBrowseView(mediator, feats);
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

			string str = m_sda.get_UnicodeProp(hvoSel, FeatureConstraintPublisher.PolarityFlid);
			for (int i = 0; i < m_valuesCombo.Items.Count; i++)
			{
				string comboStr = m_valuesCombo.Items[i] as string;
				if (str == comboStr || (String.IsNullOrEmpty(str) && comboStr == MEStrings.ks_DontCare_))
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

			m_sda.SetUnicode(hvoSel, FeatureConstraintPublisher.PolarityFlid, str, str.Length);
		}

		private void BuildInitialBrowseView(XCore.Mediator mediator, IEnumerable<IFsFeatDefn> features)
		{
			XmlNode configurationParameters = (XmlNode)mediator.PropertyTable.GetValue("WindowConfiguration");
			XmlNode toolNode = configurationParameters.SelectSingleNode("controls/parameters/guicontrol[@id='FeatureConstraintFlatList']/parameters");

			m_listPanel.SuspendLayout();
			var hvos = (from feat in features
						select feat.Hvo).ToArray();
			m_sda.CacheVecProp(m_cache.LangProject.Hvo, hvos);
			m_bvList = new BrowseViewer(toolNode, m_cache.LangProject.Hvo, FeatureConstraintPublisher.ListFlid, m_cache, mediator, null, m_sda);
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

		private XCore.Mediator Mediator
		{
			set
			{
				m_mediator = value;
				if (m_mediator != null)
				{
					// Reset window location.
					// Get location to the stored values, if any.
					object locWnd = m_mediator.PropertyTable.GetValue("featConstrListDlgLocation");
					object szWnd = m_mediator.PropertyTable.GetValue("featConstrListDlgSize");
					if (locWnd != null && szWnd != null)
					{
						Rectangle rect = new Rectangle((Point) locWnd, (Size) szWnd);
						ScreenUtils.EnsureVisibleRect(ref rect);
						DesktopBounds = rect;
						StartPosition = FormStartPosition.Manual;
					}

					if (m_mediator.HelpTopicProvider != null) // Will be null when running tests
					{
						m_helpProvider.HelpNamespace = m_mediator.HelpTopicProvider.HelpFile;
						m_helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(s_helpTopic));
						m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
					}
				}
			}
		}

		/// <summary>
		/// Load the tree items if the starting point is a simple context.
		/// </summary>
		/// <param name="fs"></param>
		private void LoadPhonFeats(IEnumerable<IFsFeatDefn> features)
		{
			m_sda = new FeatureConstraintPublisher(m_cache.DomainDataByFlid as ISilDataAccessManaged);
			foreach (var feat in features)
			{
				string str = null;
				if (ContainsFeature(m_ctxt.PlusConstrRS, feat))
					str = MEStrings.ksFeatConstrAgree;
				else if (ContainsFeature(m_ctxt.MinusConstrRS, feat))
					str = MEStrings.ksFeatConstrDisagree;
				else
					str = "";
				m_sda.SetUnicode(feat.Hvo, FeatureConstraintPublisher.PolarityFlid, str, str.Length);
			}
		}

		bool ContainsFeature(IEnumerable<IPhFeatureConstraint> vars, IFsFeatDefn feat)
		{
			foreach (IPhFeatureConstraint var in vars)
			{
				if (var.FeatureRA == feat)
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
			this.m_helpProvider = new HelpProvider();
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
				var feat = m_cache.ServiceLocator.GetInstance<IFsFeatDefnRepository>().GetObject(hvoClosedFeature);
				string str = m_sda.get_UnicodeProp(feat.Hvo, FeatureConstraintPublisher.PolarityFlid);
				if (String.IsNullOrEmpty(str))
				{
					IPhFeatureConstraint removedConstr = RemoveFeatureConstraint(m_ctxt.PlusConstrRS, feat);
					if (removedConstr == null)
						removedConstr = RemoveFeatureConstraint(m_ctxt.MinusConstrRS, feat);

					if (removedConstr != null)
					{
						if (!m_rule.FeatureConstraints.Contains(removedConstr))
							m_cache.LangProject.PhonologicalDataOA.FeatConstraintsOS.Remove(removedConstr);
					}
				}
				else
				{
					IPhFeatureConstraint var = GetFeatureConstraint(m_rule.FeatureConstraints, feat);
					if (var == null)
					{
						var = m_cache.ServiceLocator.GetInstance<IPhFeatureConstraintFactory>().Create();
						m_cache.LangProject.PhonologicalDataOA.FeatConstraintsOS.Add(var);
						var.FeatureRA = feat;
					}

					if (str == MEStrings.ksFeatConstrAgree)
					{
						if (!m_ctxt.PlusConstrRS.Contains(var))
						{
							m_ctxt.PlusConstrRS.Add(var);
							RemoveFeatureConstraint(m_ctxt.MinusConstrRS, feat);
						}
					}
					else if (str == MEStrings.ksFeatConstrDisagree)
					{
						if (!m_ctxt.MinusConstrRS.Contains(var))
						{
							m_ctxt.MinusConstrRS.Add(var);
							RemoveFeatureConstraint(m_ctxt.PlusConstrRS, feat);
						}
					}
				}
			}
		}

		IPhFeatureConstraint RemoveFeatureConstraint(IFdoReferenceSequence<IPhFeatureConstraint> featConstrs, IFsFeatDefn feat)
		{
			var constrToRemove = GetFeatureConstraint(featConstrs, feat);
			if (constrToRemove != null)
				featConstrs.Remove(constrToRemove);
			return constrToRemove;
		}

		IPhFeatureConstraint GetFeatureConstraint(IEnumerable<IPhFeatureConstraint> featConstrs, IFsFeatDefn feat)
		{
			foreach (var curConstr in featConstrs)
			{
				if (curConstr.FeatureRA == feat)
					return curConstr;
			}
			return null;
		}

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			Guid guid = m_cache.LangProject.PhFeatureSystemOA.Guid;
			m_link = new FwLinkArgs("phonologicalFeaturesAdvancedEdit", guid);
			m_btnCancel.PerformClick();
			DialogResult = DialogResult.Ignore;
		}

		private void m_bnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, s_helpTopic);
		}

		class FeatureConstraintPublisher : ObjectListPublisher
		{
			public const int ListFlid = 89999988;
			public const int PolarityFlid = 89999977;

			Dictionary<int, string> m_unicodeProps;

			public FeatureConstraintPublisher(ISilDataAccessManaged domainDataByFlid)
				: base(domainDataByFlid, ListFlid)
			{
				m_unicodeProps = new Dictionary<int, string>();
				SetOverrideMdc(new FeatureConstraintMdc(MetaDataCache as IFwMetaDataCacheManaged));
			}

			public override string get_UnicodeProp(int hvo, int tag)
			{
				if (tag == PolarityFlid)
				{
					string str;
					if (!m_unicodeProps.TryGetValue(hvo, out str))
						str = "";
					return str;
				}
				else
				{
					return base.get_UnicodeProp(hvo, tag);
				}
			}

			public override void SetUnicode(int hvo, int tag, string _rgch, int cch)
			{
				if (tag == PolarityFlid)
					m_unicodeProps[hvo] = _rgch.Substring(0, cch);
				else
					base.SetUnicode(hvo, tag, _rgch, cch);
			}

			class FeatureConstraintMdc : FdoMetaDataCacheDecoratorBase
			{
				public FeatureConstraintMdc(IFwMetaDataCacheManaged mdc)
					: base(mdc)
				{
				}

				public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
				{
					throw new NotImplementedException();
				}

				public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
				{
					if (bstrClassName == "FsClosedFeature" && bstrFieldName == "DummyPolarity")
						return PolarityFlid;
					else
						return base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
				}

				public override int GetFieldType(int luFlid)
				{
					return luFlid == PolarityFlid ? (int)CellarPropertyType.Unicode : base.GetFieldType(luFlid);
				}
			}
		}
	}
}
