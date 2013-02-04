using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcMorphDlg : Form
	{
		const string s_helpTopic = "khtpNoHelpTopic";

		private Button m_btnHelp;
		private Button m_btnCancel;
		private Button m_btnOK;
		private HelpProvider m_helpProvider;
		private GroupBox groupBox1;
		private ComboBox m_formWsComboBox;
		private FwTextBox m_formTextBox;
		private GroupBox groupBox2;
		private ComboBox m_glossWsComboBox;
		private FwTextBox m_glossTextBox;
		private GroupBox groupBox3;
		private ComboBox m_entryWsComboBox;
		private FwTextBox m_entryTextBox;
		private GroupBox groupBox4;
		private TreeCombo m_categoryComboBox;
		private GroupBox groupBox5;
		private TreeViewAdv m_inflFeatsTreeView;
		private TreeColumn m_featureColumn;
		private TreeColumn m_valueColumn;
		private TreeColumn m_notColumn;
		private NodeTextBox m_featureTextBox;
		private NodeComboBox m_valueComboBox;
		private NodeCheckBox m_inflNotCheckBox;
		private NodeIcon m_featureIcon;
		private ImageList m_imageList;
		private CheckBox m_categoryNotCheckBox;

		private System.ComponentModel.IContainer components;

		private FdoCache m_cache;
		private Mediator m_mediator;
		private IHelpTopicProvider m_helpTopicProvider;
		private ComplexConcMorphNode m_node;
		private PossibilityComboController m_catPopupTreeManager;
		private InflFeatureTreeModel m_inflModel;

		public ComplexConcMorphDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		public void SetDlgInfo(FdoCache cache, Mediator mediator, ComplexConcMorphNode node)
		{
			m_cache = cache;
			m_mediator = mediator;
			m_node = node;

			m_formTextBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_formTextBox.AdjustForStyleSheet(FontHeightAdjuster.StyleSheetFromMediator(mediator));

			m_glossTextBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_glossTextBox.AdjustForStyleSheet(FontHeightAdjuster.StyleSheetFromMediator(mediator));

			m_entryTextBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_entryTextBox.AdjustForStyleSheet(FontHeightAdjuster.StyleSheetFromMediator(mediator));

			m_categoryComboBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;

			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				m_formWsComboBox.Items.Add(ws);
				m_entryWsComboBox.Items.Add(ws);
			}

			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
				m_glossWsComboBox.Items.Add(ws);

			m_inflModel = new InflFeatureTreeModel(m_cache.LangProject.MsFeatureSystemOA, m_node.InflFeatures, m_imageList.Images[0], m_imageList.Images[1]);
			m_inflFeatsTreeView.Model = m_inflModel;
			m_inflFeatsTreeView.ExpandAll();

			SetTextBoxValue(m_node.Form, m_formTextBox, m_formWsComboBox, true);
			SetTextBoxValue(m_node.Entry, m_entryTextBox, m_entryWsComboBox, true);
			SetTextBoxValue(m_node.Gloss, m_glossTextBox, m_glossWsComboBox, false);

			m_catPopupTreeManager = new PossibilityComboController(m_categoryComboBox,
									m_cache,
									m_cache.LanguageProject.PartsOfSpeechOA,
									m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle,
									false,
									m_mediator,
									(Form) m_mediator.PropertyTable.GetValue("window"));

			if (m_node.Category != null)
			{
				m_categoryNotCheckBox.Checked = m_node.NegateCategory;
				m_catPopupTreeManager.LoadPopupTree(m_node.Category.Hvo);
			}
			else
			{
				m_catPopupTreeManager.LoadPopupTree(0);
			}

			m_helpTopicProvider = m_mediator.HelpTopicProvider;

			m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		private void SetTextBoxValue(ITsString tss, FwTextBox textBox, ComboBox comboBox, bool vern)
		{
			if (tss != null)
			{
				int ws = tss.get_WritingSystemAt(0);
				comboBox.SelectedItem = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
				textBox.Tss = tss;
			}
			else
			{
				comboBox.SelectedItem = vern ? m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem
					: m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			m_node.Form = string.IsNullOrEmpty(m_formTextBox.Text) ? null : m_formTextBox.Tss;
			m_node.Gloss = string.IsNullOrEmpty(m_glossTextBox.Text) ? null : m_glossTextBox.Tss;
			m_node.Entry = string.IsNullOrEmpty(m_entryTextBox.Text) ? null : m_entryTextBox.Tss;

			m_inflModel.AddInflFeatures(m_node.InflFeatures);

			var node = (HvoTreeNode) m_categoryComboBox.SelectedNode;
			if (node.Hvo != 0)
			{
				m_node.Category = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(node.Hvo);
				m_node.NegateCategory = m_categoryNotCheckBox.Checked;
			}
			else
			{
				m_node.Category = null;
				m_node.NegateCategory = false;
			}

			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		private void m_formWsComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateTextBoxWs(m_formWsComboBox, m_formTextBox);
		}

		private void m_glossWsComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateTextBoxWs(m_glossWsComboBox, m_glossTextBox);
		}

		private void m_entryWsComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateTextBoxWs(m_entryWsComboBox, m_entryTextBox);
		}

		private void UpdateTextBoxWs(ComboBox wsComboBox, FwTextBox textBox)
		{
			var ws = wsComboBox.SelectedItem as IWritingSystem;
			if (ws == null)
			{
				Debug.Assert(wsComboBox.SelectedIndex == -1);
				return;
			}
			textBox.WritingSystemCode = ws.Handle;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			textBox.Tss = tsf.MakeString(textBox.Text.Trim(), ws.Handle);
		}

		private void m_valueComboBox_CreatingEditor(object sender, EditEventArgs e)
		{
			var comboBox = (ComboBox) e.Control;
			var closedFeatNode = (ClosedFeatureNode) e.Node.Tag;
			comboBox.Items.Add(new SymbolicValue(null));
			comboBox.Items.AddRange(closedFeatNode.Feature.ValuesOC.OrderBy(v => v.Abbreviation.BestAnalysisAlternative.Text).Select(v => new SymbolicValue(v)).Cast<object>().ToArray());
			comboBox.SelectedItem = closedFeatNode.Value;
		}

		private void m_inflNotCheckBox_IsVisibleValueNeeded(object sender, NodeControlValueEventArgs e)
		{
			e.Value = e.Node.Tag is ClosedFeatureNode;
		}

		private void m_valueComboBox_IsEditEnabledValueNeeded(object sender, NodeControlValueEventArgs e)
		{
			e.Value = e.Node.Tag is ClosedFeatureNode;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposing && (components != null))
			{
				components.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ComplexConcMorphDlg));
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.m_glossWsComboBox = new System.Windows.Forms.ComboBox();
			this.m_glossTextBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.m_entryWsComboBox = new System.Windows.Forms.ComboBox();
			this.m_entryTextBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_formWsComboBox = new System.Windows.Forms.ComboBox();
			this.m_formTextBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.m_categoryNotCheckBox = new System.Windows.Forms.CheckBox();
			this.m_categoryComboBox = new SIL.FieldWorks.Common.Widgets.TreeCombo();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.m_inflFeatsTreeView = new Aga.Controls.Tree.TreeViewAdv();
			this.m_featureColumn = new Aga.Controls.Tree.TreeColumn();
			this.m_notColumn = new Aga.Controls.Tree.TreeColumn();
			this.m_valueColumn = new Aga.Controls.Tree.TreeColumn();
			this.m_featureIcon = new Aga.Controls.Tree.NodeControls.NodeIcon();
			this.m_featureTextBox = new Aga.Controls.Tree.NodeControls.NodeTextBox();
			this.m_valueComboBox = new Aga.Controls.Tree.NodeControls.NodeComboBox();
			this.m_inflNotCheckBox = new Aga.Controls.Tree.NodeControls.NodeCheckBox();
			this.m_imageList = new System.Windows.Forms.ImageList(this.components);
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_glossTextBox)).BeginInit();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_entryTextBox)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_formTextBox)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// groupBox2
			//
			this.groupBox2.Controls.Add(this.m_glossWsComboBox);
			this.groupBox2.Controls.Add(this.m_glossTextBox);
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Name = "groupBox2";
			this.m_helpProvider.SetShowHelp(this.groupBox2, ((bool)(resources.GetObject("groupBox2.ShowHelp"))));
			this.groupBox2.TabStop = false;
			//
			// m_glossWsComboBox
			//
			this.m_glossWsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_glossWsComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_glossWsComboBox, "m_glossWsComboBox");
			this.m_glossWsComboBox.Name = "m_glossWsComboBox";
			this.m_helpProvider.SetShowHelp(this.m_glossWsComboBox, ((bool)(resources.GetObject("m_glossWsComboBox.ShowHelp"))));
			this.m_glossWsComboBox.SelectedIndexChanged += new System.EventHandler(this.m_glossWsComboBox_SelectedIndexChanged);
			//
			// m_glossTextBox
			//
			this.m_glossTextBox.AcceptsReturn = false;
			this.m_glossTextBox.AdjustStringHeight = true;
			this.m_glossTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_glossTextBox.controlID = null;
			resources.ApplyResources(this.m_glossTextBox, "m_glossTextBox");
			this.m_glossTextBox.HasBorder = true;
			this.m_glossTextBox.Name = "m_glossTextBox";
			this.m_helpProvider.SetShowHelp(this.m_glossTextBox, ((bool)(resources.GetObject("m_glossTextBox.ShowHelp"))));
			this.m_glossTextBox.SuppressEnter = true;
			this.m_glossTextBox.WordWrap = false;
			//
			// groupBox3
			//
			this.groupBox3.Controls.Add(this.m_entryWsComboBox);
			this.groupBox3.Controls.Add(this.m_entryTextBox);
			resources.ApplyResources(this.groupBox3, "groupBox3");
			this.groupBox3.Name = "groupBox3";
			this.m_helpProvider.SetShowHelp(this.groupBox3, ((bool)(resources.GetObject("groupBox3.ShowHelp"))));
			this.groupBox3.TabStop = false;
			//
			// m_entryWsComboBox
			//
			this.m_entryWsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_entryWsComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_entryWsComboBox, "m_entryWsComboBox");
			this.m_entryWsComboBox.Name = "m_entryWsComboBox";
			this.m_helpProvider.SetShowHelp(this.m_entryWsComboBox, ((bool)(resources.GetObject("m_entryWsComboBox.ShowHelp"))));
			this.m_entryWsComboBox.SelectedIndexChanged += new System.EventHandler(this.m_entryWsComboBox_SelectedIndexChanged);
			//
			// m_entryTextBox
			//
			this.m_entryTextBox.AcceptsReturn = false;
			this.m_entryTextBox.AdjustStringHeight = true;
			this.m_entryTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_entryTextBox.controlID = null;
			resources.ApplyResources(this.m_entryTextBox, "m_entryTextBox");
			this.m_entryTextBox.HasBorder = true;
			this.m_entryTextBox.Name = "m_entryTextBox";
			this.m_helpProvider.SetShowHelp(this.m_entryTextBox, ((bool)(resources.GetObject("m_entryTextBox.ShowHelp"))));
			this.m_entryTextBox.SuppressEnter = true;
			this.m_entryTextBox.WordWrap = false;
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.m_formWsComboBox);
			this.groupBox1.Controls.Add(this.m_formTextBox);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// m_formWsComboBox
			//
			this.m_formWsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_formWsComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_formWsComboBox, "m_formWsComboBox");
			this.m_formWsComboBox.Name = "m_formWsComboBox";
			this.m_formWsComboBox.SelectedIndexChanged += new System.EventHandler(this.m_formWsComboBox_SelectedIndexChanged);
			//
			// m_formTextBox
			//
			this.m_formTextBox.AcceptsReturn = false;
			this.m_formTextBox.AdjustStringHeight = true;
			this.m_formTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_formTextBox.controlID = null;
			resources.ApplyResources(this.m_formTextBox, "m_formTextBox");
			this.m_formTextBox.HasBorder = true;
			this.m_formTextBox.Name = "m_formTextBox";
			this.m_formTextBox.SuppressEnter = true;
			this.m_formTextBox.WordWrap = false;
			//
			// groupBox4
			//
			this.groupBox4.Controls.Add(this.m_categoryNotCheckBox);
			this.groupBox4.Controls.Add(this.m_categoryComboBox);
			resources.ApplyResources(this.groupBox4, "groupBox4");
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.TabStop = false;
			//
			// m_categoryNotCheckBox
			//
			resources.ApplyResources(this.m_categoryNotCheckBox, "m_categoryNotCheckBox");
			this.m_categoryNotCheckBox.Name = "m_categoryNotCheckBox";
			this.m_categoryNotCheckBox.UseVisualStyleBackColor = true;
			//
			// m_categoryComboBox
			//
			this.m_categoryComboBox.AdjustStringHeight = true;
			this.m_categoryComboBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_categoryComboBox.DropDownWidth = 120;
			this.m_categoryComboBox.DroppedDown = false;
			this.m_categoryComboBox.HasBorder = true;
			resources.ApplyResources(this.m_categoryComboBox, "m_categoryComboBox");
			this.m_categoryComboBox.Name = "m_categoryComboBox";
			this.m_categoryComboBox.UseVisualStyleBackColor = true;
			//
			// groupBox5
			//
			this.groupBox5.Controls.Add(this.m_inflFeatsTreeView);
			resources.ApplyResources(this.groupBox5, "groupBox5");
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.TabStop = false;
			//
			// m_inflFeatsTreeView
			//
			this.m_inflFeatsTreeView.BackColor = System.Drawing.SystemColors.Window;
			this.m_inflFeatsTreeView.Columns.Add(this.m_featureColumn);
			this.m_inflFeatsTreeView.Columns.Add(this.m_notColumn);
			this.m_inflFeatsTreeView.Columns.Add(this.m_valueColumn);
			this.m_inflFeatsTreeView.DefaultToolTipProvider = null;
			this.m_inflFeatsTreeView.DragDropMarkColor = System.Drawing.Color.Black;
			this.m_inflFeatsTreeView.FullRowSelect = true;
			this.m_inflFeatsTreeView.LineColor = System.Drawing.SystemColors.ControlDark;
			resources.ApplyResources(this.m_inflFeatsTreeView, "m_inflFeatsTreeView");
			this.m_inflFeatsTreeView.Model = null;
			this.m_inflFeatsTreeView.Name = "m_inflFeatsTreeView";
			this.m_inflFeatsTreeView.NodeControls.Add(this.m_featureIcon);
			this.m_inflFeatsTreeView.NodeControls.Add(this.m_featureTextBox);
			this.m_inflFeatsTreeView.NodeControls.Add(this.m_valueComboBox);
			this.m_inflFeatsTreeView.NodeControls.Add(this.m_inflNotCheckBox);
			this.m_inflFeatsTreeView.SelectedNode = null;
			this.m_inflFeatsTreeView.UseColumns = true;
			//
			// m_featureColumn
			//
			resources.ApplyResources(this.m_featureColumn, "m_featureColumn");
			this.m_featureColumn.SortOrder = System.Windows.Forms.SortOrder.None;
			//
			// m_notColumn
			//
			resources.ApplyResources(this.m_notColumn, "m_notColumn");
			this.m_notColumn.SortOrder = System.Windows.Forms.SortOrder.None;
			//
			// m_valueColumn
			//
			resources.ApplyResources(this.m_valueColumn, "m_valueColumn");
			this.m_valueColumn.SortOrder = System.Windows.Forms.SortOrder.None;
			//
			// m_featureIcon
			//
			this.m_featureIcon.DataPropertyName = "Image";
			this.m_featureIcon.LeftMargin = 1;
			this.m_featureIcon.ParentColumn = this.m_featureColumn;
			this.m_featureIcon.ScaleMode = Aga.Controls.Tree.ImageScaleMode.Clip;
			//
			// m_featureTextBox
			//
			this.m_featureTextBox.DataPropertyName = "Text";
			this.m_featureTextBox.IncrementalSearchEnabled = true;
			this.m_featureTextBox.LeftMargin = 1;
			this.m_featureTextBox.ParentColumn = this.m_featureColumn;
			//
			// m_valueComboBox
			//
			this.m_valueComboBox.DataPropertyName = "Value";
			this.m_valueComboBox.EditEnabled = true;
			this.m_valueComboBox.EditOnClick = true;
			this.m_valueComboBox.IncrementalSearchEnabled = true;
			this.m_valueComboBox.LeftMargin = 1;
			this.m_valueComboBox.ParentColumn = this.m_valueColumn;
			this.m_valueComboBox.CreatingEditor += new System.EventHandler<Aga.Controls.Tree.NodeControls.EditEventArgs>(this.m_valueComboBox_CreatingEditor);
			this.m_valueComboBox.IsEditEnabledValueNeeded += new System.EventHandler<Aga.Controls.Tree.NodeControls.NodeControlValueEventArgs>(this.m_valueComboBox_IsEditEnabledValueNeeded);
			//
			// m_inflNotCheckBox
			//
			this.m_inflNotCheckBox.DataPropertyName = "IsChecked";
			this.m_inflNotCheckBox.EditEnabled = true;
			this.m_inflNotCheckBox.LeftMargin = 1;
			this.m_inflNotCheckBox.ParentColumn = this.m_notColumn;
			this.m_inflNotCheckBox.IsVisibleValueNeeded += new System.EventHandler<Aga.Controls.Tree.NodeControls.NodeControlValueEventArgs>(this.m_inflNotCheckBox_IsVisibleValueNeeded);
			//
			// m_imageList
			//
			this.m_imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList.ImageStream")));
			this.m_imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.m_imageList.Images.SetKeyName(0, "");
			this.m_imageList.Images.SetKeyName(1, "");
			//
			// ComplexConcMorphDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ComplexConcMorphDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.groupBox2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_glossTextBox)).EndInit();
			this.groupBox3.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_entryTextBox)).EndInit();
			this.groupBox1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_formTextBox)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox5.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
