// This really needs to be refactored with MasterCategoryListDlg.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for PhonologicalFeatureChooserDlg.
	/// </summary>
	public class PhonologicalFeatureChooserDlg : Form, IFWDisposable
	{
		private XCore.Mediator m_mediator;
		private FdoCache m_cache;
		private FwLink m_link;
		// The dialog can be initialized with an existing feature structure,
		// or just with an owning object and flid in which to create one.
		private IFsFeatStruc m_fs;
		// Where to put a new feature structure if needed. Owning flid may be atomic
		// or collection. Used only if m_fs is initially null.
		int m_hvoOwner;
		int m_owningFlid;
		private System.Windows.Forms.Panel m_listPanel;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_bnHelp;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.Label labelPrompt;
		private System.ComponentModel.IContainer components = null;

		private FwComboBox m_valuesCombo;

		private string m_helpTopic = "khtpChoose-PhonFeats";
		private BrowseViewer m_bvList;
		private int m_fakeFlid;
		private int m_flidDummyValue;
		private System.Windows.Forms.HelpProvider helpProvider;

		public PhonologicalFeatureChooserDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_valuesCombo = new FwComboBox();
			m_valuesCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			m_valuesCombo.AdjustStringHeight = false;
			m_valuesCombo.BackColor = SystemColors.Window;
			m_valuesCombo.Padding = new Padding(0, 1, 0, 0);
			m_valuesCombo.SelectedIndexChanged += new EventHandler(m_valuesCombo_SelectedIndexChanged);

			if(FwApp.App != null) // Will be null when running tests
			{
				this.helpProvider = new System.Windows.Forms.HelpProvider();
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
				this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(m_helpTopic, 0));
				this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			}
			Resize += new EventHandler(PhonologicalFeatureChooserDlg_Resize);
		}

		void PhonologicalFeatureChooserDlg_Resize(object sender, EventArgs e)
		{
			PositionValuesCombo();
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
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
			m_fs = null;
			m_mediator = null;
			m_cache = null;
			m_bvList = null;
			m_valuesCombo = null;

			base.Dispose( disposing );
		}

		/// <summary>
		/// Init the dialog with an existing FS.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="fs"></param>
		public void SetDlgInfo(FdoCache cache, XCore.Mediator mediator, IFsFeatStruc fs)
		{
			CheckDisposed();

			m_fs = fs;
			m_owningFlid = fs.OwningFlid;
			m_hvoOwner = fs.OwnerHVO;
			RestoreWindowPosition(mediator);
			m_cache = cache;
			m_valuesCombo.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_valuesCombo.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			LoadPhonFeats(fs);
			BuildInitialBrowseView(mediator);
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			CheckDisposed();

			m_helpTopic = helpTopic;
		}

		/// <summary>
		/// Init the dialog with a PhPhoneme (or PhNCFeatures) and flid that does not yet contain a feature structure.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="cobj"></param>
		/// <param name="owningFlid"></param>
		public void SetDlgInfo(FdoCache cache, XCore.Mediator mediator, ICmObject cobj, int owningFlid)
		{
			CheckDisposed();

			m_fs = null;
			m_owningFlid = owningFlid;
			m_hvoOwner = cobj.Hvo;
			RestoreWindowPosition(mediator);
			m_cache = cache;
			m_valuesCombo.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_valuesCombo.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			LoadPhonFeats(null);
			BuildInitialBrowseView(mediator);
		}

		public void SetDlgInfo(FdoCache cache, XCore.Mediator mediator)
		{
			CheckDisposed();

			m_fs = null;
			RestoreWindowPosition(mediator);
			m_cache = cache;
			m_valuesCombo.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_valuesCombo.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			LoadPhonFeats(null);
			BuildInitialBrowseView(mediator);
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
			Rectangle valueLocation = m_bvList.LocationOfCellInSelectedRow("Value");
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
			int[] values = m_cache.GetVectorProperty(hvoSel, (int)FsClosedFeature.FsClosedFeatureTags.kflidValues, false);
			m_valuesCombo.Items.Clear();
			int comboSelectedIndex = -1;
			for (int i = 0; i < values.Length; i++)
			{
				int hvoValue = values[i];
				IFsSymFeatVal val = FsSymFeatVal.CreateFromDBObject(m_cache, hvoValue, true) as IFsSymFeatVal;
				//int hvoSymfeatVal = m_cache.GetObjProperty(hvoValue, (int) FsClosedValue.FsClosedValueTags.kflidValue);
				if (val != null)
				{
					m_valuesCombo.Items.Add(val.Abbreviation.BestAnalysisAlternative);
					// try to set the selected item
					int hvoDummy = m_cache.GetObjProperty(hvoSel, m_flidDummyValue);
					if (hvoDummy == hvoValue)
					{
						comboSelectedIndex = m_valuesCombo.SelectedIndex = i;
					}
				}
			}
			m_valuesCombo.Items.Add(m_cache.MakeUserTss(MEStrings.ks_DontCare_));
			if (comboSelectedIndex == -1)
				m_valuesCombo.SelectedIndex = values.Length;
		}

		void m_valuesCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// make sure the dummy value reflects the selected value
			int selectedRowIndex = m_bvList.SelectedIndex;
			int hvoSel = m_bvList.AllItems[selectedRowIndex];
			int[] values = m_cache.GetVectorProperty(hvoSel, (int)FsClosedFeature.FsClosedFeatureTags.kflidValues, false);
			int selectedValueIndex = m_valuesCombo.SelectedIndex;
			// values[selectedValueIndex] is the selected value
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			if (selectedValueIndex >= values.Length)
			{ // is unspecified
				cda.CacheObjProp(hvoSel, m_flidDummyValue, 0);
			}
			else
			{
				int hvoValue = values[selectedValueIndex];
				cda.CacheObjProp(hvoSel, m_flidDummyValue, hvoValue);
			}
		}

		private void BuildInitialBrowseView(XCore.Mediator mediator)
		{

			XmlNode configurationParameters =
				(XmlNode)mediator.PropertyTable.GetValue("WindowConfiguration");
			XmlNode toolNode = configurationParameters.SelectSingleNode(
				"controls/parameters/guicontrol[@id='PhonologicalFeaturesFlatList']/parameters");
			m_listPanel.SuspendLayout();
			m_fakeFlid = FdoCache.DummyFlid;
			IVwCacheDa cda = m_cache.MainCacheAccessor as IVwCacheDa;
			int[] featureHvos = m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.HvoArray;
			cda.CacheVecProp(m_cache.LangProject.Hvo, m_fakeFlid, featureHvos, featureHvos.Length);
			m_bvList = new BrowseViewer(toolNode, m_cache.LangProject.Hvo, m_fakeFlid, m_cache, mediator, null);
			m_bvList.SelectionChanged += new FwSelectionChangedEventHandler(m_bvList_SelectionChanged);
			m_bvList.ScrollBar.ValueChanged += new EventHandler(ScrollBar_ValueChanged);
			m_bvList.Scroller.Scroll += new ScrollEventHandler(ScrollBar_Scroll);
			m_bvList.ColumnsChanged += new EventHandler(BrowseViewer_ColumnsChanged);
			m_bvList.Resize += new EventHandler(m_bvList_Resize);
			m_bvList.TabStop = true;
			m_bvList.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_bvList.Dock = DockStyle.Fill;
			m_bvList.BackColor = SystemColors.Window;
			m_listPanel.Controls.Add(m_bvList);
			m_listPanel.ResumeLayout(false);
		}

		void ScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			PositionValuesCombo();
		}

		void BrowseViewer_ColumnsChanged(object sender, EventArgs e)
		{
			PositionValuesCombo();
		}

		void ScrollBar_ValueChanged(object sender, EventArgs e)
		{
			PositionValuesCombo();
		}

		void m_bvList_Resize(object sender, EventArgs e)
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
				object locWnd = m_mediator.PropertyTable.GetValue("phonFeatListDlgLocation");
				object szWnd = m_mediator.PropertyTable.GetValue("phonFeatListDlgSize");
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
		/// Load the tree items if the starting point is a feature structure.
		/// </summary>
		/// <param name="fs"></param>
		private void LoadPhonFeats(IFsFeatStruc fs)
		{
			m_flidDummyValue = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor, "FsClosedFeature", "DummyValue",
																	(int)CellarModuleDefns.kcptReferenceAtom).Tag;
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			foreach (int hvoClosedFeature in m_cache.LangProject.PhFeatureSystemOA.FeaturesOC.HvoArray)
			{
				if (fs != null)
				{
					IFsClosedValue closedValue = fs.FindClosedValue(hvoClosedFeature);
					if (closedValue != null)
					{
						cda.CacheObjProp(hvoClosedFeature, m_flidDummyValue, closedValue.ValueRAHvo);
					}
					else
					{  // set the value to zero so nothing shows
						cda.CacheObjProp(hvoClosedFeature, m_flidDummyValue, 0);
					}
				}
				else
				{
					// set the value to zero so nothing shows
					cda.CacheObjProp(hvoClosedFeature, m_flidDummyValue, 0);
				}
			}
		}

		/// <summary>
		/// Get Feature Structure resulting from dialog operation
		/// </summary>
		public IFsFeatStruc FS
		{
			get
			{
				CheckDisposed();

				return m_fs;
			}

			set
			{
				CheckDisposed();

				m_fs = value;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PhonologicalFeatureChooserDlg));
			this.labelPrompt = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_bnHelp = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.m_listPanel = new System.Windows.Forms.Panel();
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
			// PhonologicalFeatureChooserDlg
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
			this.Name = "PhonologicalFeatureChooserDlg";
			this.ShowInTaskbar = false;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.PhonologicalFeatureChooserDlg_Closing);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// If OK, then make FS have the selected feature value(s).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PhonologicalFeatureChooserDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (DialogResult == DialogResult.OK)
			{
				Cursor = Cursors.WaitCursor;
				if (m_fs == null)
				{
					// Didn't have one to begin with. See whether we want to create one.
					if (m_hvoOwner != 0 && CheckFeatureStructure())
					{
						// The last argument is meaningless since we expect this property to be owning
						// or collection.
						int hvoFs = m_cache.CreateObject(FsFeatStruc.kClassId, m_hvoOwner, m_owningFlid, 0);
						m_fs = (IFsFeatStruc)FsFeatStruc.CreateFromDBObject(m_cache, hvoFs, false);
					}
				}

				if (m_fs != null)
				{
					// clean out any extant features in the feature structure
					foreach (IFsFeatureSpecification spec in m_fs.FeatureSpecsOC)
						m_fs.FeatureSpecsOC.Remove(spec);
					UpdateFeatureStructure();
				}
			}

			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("phonFeatListDlgLocation", Location);
				m_mediator.PropertyTable.SetProperty("phonFeatListDlgSize", Size);
			}
			Cursor = Cursors.Default;
		}

		/// <summary>
		/// Answer true if there is some feature with a real value.
		/// </summary>
		/// <returns></returns>
		private bool CheckFeatureStructure()
		{
			List<int> featureHvos = m_bvList.AllItems;
			foreach (int hvoClosedFeature in featureHvos)
			{
				int hvoDummy = m_cache.GetObjProperty(hvoClosedFeature, m_flidDummyValue);
				if (hvoDummy > 0)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Makes the feature structure reflect the values chosen
		/// </summary>
		/// <remarks>Is public for Unit Testing</remarks>
		public void UpdateFeatureStructure()
		{
			CheckDisposed();

			List<int> featureHvos = m_bvList.AllItems;
			foreach (int hvoClosedFeature in featureHvos)
			{
				int hvoDummy = m_cache.GetObjProperty(hvoClosedFeature, m_flidDummyValue);
				if (hvoDummy > 0)
				{
					IFsFeatureSpecification val = null;
					BuildFeatureStructure(hvoClosedFeature, hvoDummy, ref val);
				}
			}
		}
		/// <summary>
		/// Builds the feature structure based on the items chosen
		/// </summary>
		/// <returns></returns>
		private void BuildFeatureStructure(int hvoClosedFeature, int hvoValue, ref IFsFeatureSpecification val)
		{
			val = (IFsClosedValue)m_fs.FindOrCreateClosedValue(hvoClosedFeature);
			val.FeatureRAHvo = hvoClosedFeature;
			IFsClosedValue closed = val as IFsClosedValue;
			if (closed != null)
			{
				closed.ValueRAHvo = hvoValue;
			}
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
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopic);
		}
	}
}
