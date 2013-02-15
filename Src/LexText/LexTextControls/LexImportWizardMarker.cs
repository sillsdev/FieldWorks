using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
#if __MonoCS__
using Skybound.Gecko;
#endif
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary></summary>
	public class LexImportWizardMarker : Form, IFWDisposable
	{
		private System.Windows.Forms.Label lblMarker;
		private System.Windows.Forms.Label m_lblMarker;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.CheckBox chkbxExclude;
		private System.Windows.Forms.Label m_lblInstances;
		private System.Windows.Forms.Label lblDestinationInFlex;
		private System.Windows.Forms.TreeView tvDestination;
		private System.Windows.Forms.TreeNode m_StoredTreeNode = null;
		private System.Windows.Forms.Label blbLangDesc;
		private FwOverrideComboBox cbLangDesc;
		private System.Windows.Forms.Button btnAddLangDesc;
		private System.Windows.Forms.Button btnAddCustomField;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private Hashtable m_uiLangs;
		private System.Windows.Forms.RadioButton rbAbbrName;
		private System.Windows.Forms.RadioButton rbAbbrAbbr;
		private System.Windows.Forms.Label lblAbbr;
		private System.Windows.Forms.Panel panel5;
		private FwOverrideComboBox cbFunction;
		private System.Windows.Forms.Label lblFunction;
		private System.Windows.Forms.CheckBox chkbxAutoField;
		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private IVwStylesheet m_stylesheet;
		private string m_refFuncString;
		private string m_refFuncStringOrig;
		private System.Windows.Forms.Button buttonHelp;	// initial value
		private Hashtable m_htNameToAbbr = new Hashtable();
		struct NameWSandAbbr	// objects that are stuffed in the m_htNameToAbbr hashtable
		{
			public string name;
			public string nameWS;
			public string abbr;
			public string abbrWS;
		};
		private const string s_helpTopic = "khtpImportSFMModifyMapping";
		private Panel panelBottom;
		private Button btnShowInfo;
#if !__MonoCS__
		private WebBrowser webBrowserInfo;
#else // use geckofx on Linux
		private GeckoWebBrowser m_browser;
#endif
		private XslCompiledTransform m_xslShowInfoTransform;
		private XmlDocument m_xmlShowInfoDoc;
		private string m_sHelpHtm = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer/Import/Help.htm");
		private int m_panelBottomHeight = 0;
		private HelpProvider helpProvider;

		private void EnableLangDesc(bool enable)
		{
			blbLangDesc.Enabled = cbLangDesc.Enabled = btnAddLangDesc.Enabled = enable;
		}

		public void Init(MarkerPresenter.ContentMapping currentMarker, Hashtable uiLangsHT, FdoCache cache,
			IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet)
		{
			CheckDisposed();

			m_uiLangs = uiLangsHT;
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_stylesheet = stylesheet;
			helpProvider.HelpNamespace = helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

			// The following call is needed to 'correct' the current behavior of the FwOverrideComboBox control.
			// As the control is currently, it will not allow the space character to be passed to the base
			// control: which means it doesn't get into the edit box portion of the control. We want to allow
			// space characters in our data, so we're enabling it here. This seems like a bug in the control
			// design, but not knowing the history and it's use, we'll just make it work as we think it should
			// be for us here.
			cbFunction.AllowSpaceInEditBox = true;

			// handle processing for the custom fields (CFS)
			// - read the db and get the CFS
			// - remove CFS that are in the TV and not in the CFS list
			// - add CFS that are in the list and not in the TV
			// - handle the case where the current marker is a CF and it's no longer in the list (just throw for now)

			//ReadCustomFieldsFromDB(cache);
			bool customFieldsChanged = false;
			m_customFields = LexImportWizard.Wizard().ReadCustomFieldsFromDB(out customFieldsChanged);

			// Init will only be called the first time, so here we don't have to remove an nodes

			tvDestination.BeginUpdate();
			foreach (TreeNode classNameNode in tvDestination.Nodes)
			{
				string className = classNameNode.Text.Trim(new char[] {'(', ')'});
				if (m_customFields.FieldsForClass(className) == null)
					continue;

				foreach (Sfm2Xml.LexImportField field in m_customFields.FieldsForClass(className))
				{
					TreeNode cnode = new TreeNode(field.UIName + " (Custom Field)");
					cnode.Tag = field;
					classNameNode.Nodes.Add(cnode);
				}
				//tvDestination.Nodes.Add(tnode);
			}
			//tvDestination.ExpandAll();
			tvDestination.EndUpdate();

			// end of CFS processing

			// set the correct marker and number of times it is used
			m_lblMarker.Text = currentMarker.Marker;
			m_lblInstances.Text = String.Format(LexTextControls.ksXInstances, currentMarker.Count);

			if (currentMarker.AutoImport)
			{
				chkbxAutoField.Checked = true;
				tvDestination.Enabled = false;
			}

			// chkbxExclude.Checked = false;
			// tvDestination.Enabled = true;
			// find the node that has the tag.meaningid value the same as the currentmarker
			bool found = false;
			foreach (TreeNode classNode in tvDestination.Nodes)
			{
				if (currentMarker.ClsFieldDescription is Sfm2Xml.ClsCustomFieldDescription &&
					currentMarker.DestinationClass != classNode.Text.Trim(new char[] {'(', ')'}))
					continue;

				foreach (TreeNode fieldNode in classNode.Nodes)
				{
					if ((fieldNode.Tag as Sfm2Xml.LexImportField).ID == currentMarker.FwId)
					{
						tvDestination.SelectedNode = fieldNode;
						found = true;
						break;
					}
				}
				if (found)
					break;
			}
			if (!found && tvDestination.Nodes.Count > 0)	// make first entry topmost and visible
				tvDestination.Nodes[0].EnsureVisible();

			// set the writing system combo box
			foreach (DictionaryEntry lang in m_uiLangs)
			{
				Sfm2Xml.LanguageInfoUI langInfo = lang.Value as Sfm2Xml.LanguageInfoUI;
				// make sure there is only one entry for each writing system (especially 'ignore')
				if (cbLangDesc.FindStringExact(langInfo.ToString()) < 0)
				{
					cbLangDesc.Items.Add(langInfo);
					if (langInfo.FwName == currentMarker.WritingSystem)
						cbLangDesc.SelectedItem = langInfo;
				}
			}
			if (cbLangDesc.SelectedIndex < 0)
			{
				// default to ignore if it's in the list
				int ignorePos = cbLangDesc.FindStringExact(MarkerPresenter.ContentMapping.Ignore());
				if (ignorePos >= 0)
					cbLangDesc.SelectedIndex = ignorePos;
				else
					cbLangDesc.SelectedIndex = 0;	// first item in list as fail safe
			}

			// add the func if it's present
			m_refFuncString = string.Empty;
			if (currentMarker.IsRefField)
			{
				m_refFuncString = currentMarker.RefField;

				cbFunction.Enabled = true;

				TreeNode node = tvDestination.SelectedNode;
				if (node != null)
				{
					Sfm2Xml.LexImportField field = node.Tag as Sfm2Xml.LexImportField;
					if (field != null)
					{
						FillLexicalRefTypesCombo(field);
						// walk the name to abbr list and select the name
						string name = m_refFuncString;
						foreach (DictionaryEntry de in m_htNameToAbbr)
						{
							NameWSandAbbr nwsa = (NameWSandAbbr)de.Value;
							//if (de.Value as string == name)
							if (nwsa.abbr == name)
							{
								//name = de.Key as string;
								name = nwsa.name;	//.abbr;
								break;
							}
						}
						cbFunction.Text = name;
					}
				}
			}
			m_refFuncStringOrig = m_refFuncString;	// save the initial value

			// set the exclude chkbox and select item from FW Destination
			if (currentMarker.Exclude)// currentMarker.IsLangIgnore || // currentMarker.DestinationField == MarkerPresenter.ContentMapping.Ignore() ||
			{
				chkbxExclude.Checked = true;
				chkbxAutoField.Enabled = false;
				tvDestination.Enabled = false;
				EnableLangDesc(false);
			}

			rbAbbrName.Checked = true;
			if (currentMarker.IsAbbrvField)
			{
				// set the proper radio button
				if (currentMarker.ClsFieldDescription.IsAbbr)
					rbAbbrAbbr.Checked = true;
			}

			// LT-4722
			// btnAddCustomField.Visible = false;
		}

		// result methods to be used if the dialog result is OK
		public bool ExcludeFromImport
		{
			get
			{
				CheckDisposed();
				return chkbxExclude.Checked;
			}
		}
		public bool AutoImport
		{
			get
			{
				CheckDisposed();
				return chkbxAutoField.Checked;
			}
		}

		public string WritingSystem
		{
			get
			{
				CheckDisposed();

				Sfm2Xml.LanguageInfoUI langinfo = cbLangDesc.SelectedItem as Sfm2Xml.LanguageInfoUI;
				return langinfo.FwName;
//				return cbLangDesc.SelectedItem.ToString();
			}
		}
		public string LangDesc
		{
			get
			{
				CheckDisposed();

				Sfm2Xml.LanguageInfoUI langinfo = cbLangDesc.SelectedItem as Sfm2Xml.LanguageInfoUI;
				return langinfo.Key;
			}
		}

		public string FWDestID
		{
			get
			{
				CheckDisposed();
				TreeNode node = m_StoredTreeNode ?? tvDestination.SelectedNode;
				if (node == null)
					return "";
				return (node.Tag as Sfm2Xml.LexImportField).ID;
			}
		}

		public string FWDestinationClass
		{
			get
			{
				CheckDisposed();
				TreeNode node = m_StoredTreeNode ?? tvDestination.SelectedNode;
				if (node == null)
					return "";
				return node.Parent.Text.Trim(new char[] {'(',')'});
			}
		}


		public bool IsCustomField
		{
			get
			{
				CheckDisposed();
				TreeNode node = m_StoredTreeNode ?? tvDestination.SelectedNode;
				if (node == null)
					return false;
				return (node.Tag is Sfm2Xml.LexImportCustomField);//.IsCustomField;
			}
		}

		public bool IsAbbrNotName
		{
			get
			{
				CheckDisposed();
				return rbAbbrAbbr.Checked;
			}
		}
		public bool IsFuncField
		{
			get { return cbFunction.Enabled; }
		}
		public string FuncField
		{
			get
			{
				CheckDisposed();

				if (cbFunction.Enabled && cbFunction.Text.Length > 0)
				{
					if (m_htNameToAbbr.ContainsKey(cbFunction.Text))
					{
						//	return m_htNameToAbbr[cbFunction.Text] as string;
						NameWSandAbbr nwsa = (NameWSandAbbr)m_htNameToAbbr[cbFunction.Text];
						return nwsa.name;	// abbr;
					}
					else
						return cbFunction.Text;
				}
				return m_refFuncStringOrig;	// default to initial value if not found
			}
		}

		public string FuncFieldWS
		{
			get
			{
				CheckDisposed();

				if (cbFunction.Enabled && cbFunction.Text.Length > 0)
				{
					if (m_htNameToAbbr.ContainsKey(cbFunction.Text))
					{
						NameWSandAbbr nwsa = (NameWSandAbbr)m_htNameToAbbr[cbFunction.Text];
						return nwsa.nameWS;
					}
					else
						return "en";
				}
				return "en";	// default to initial value if not found
			}
		}


//		private Dictionary<Sfm2Xml.ILexImportField, TreeNode> m_customFieldNodes = new Dictionary<Sfm2Xml.ILexImportField, TreeNode>();
		private Sfm2Xml.ILexImportFields m_customFields = new Sfm2Xml.LexImportFields();
		public Sfm2Xml.ILexImportFields CustomFields { get { return m_customFields; } }

		public LexImportWizardMarker(Sfm2Xml.ILexImportFields fwFields)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			InitBottomPanel();

			tvDestination.BeginUpdate();
			tvDestination.Nodes.Clear();

			foreach(string className in fwFields.Classes)
			{
				TreeNode tnode = new TreeNode( String.Format("({0})", className));
//				tnode.Tag = className;
				foreach (Sfm2Xml.LexImportField field in fwFields.FieldsForClass(className))
				{
					TreeNode cnode = new TreeNode(field.UIName);
					cnode.Tag = field;
					tnode.Nodes.Add(cnode);
				}
				tvDestination.Nodes.Add(tnode);
			}
			tvDestination.ExpandAll();
			tvDestination.EndUpdate();

			helpProvider = new HelpProvider();
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
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "Code in question is only compiled on Windows")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexImportWizardMarker));
			this.lblMarker = new System.Windows.Forms.Label();
			this.m_lblMarker = new System.Windows.Forms.Label();
			this.m_lblInstances = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.chkbxExclude = new System.Windows.Forms.CheckBox();
			this.lblDestinationInFlex = new System.Windows.Forms.Label();
			this.tvDestination = new System.Windows.Forms.TreeView();
			this.blbLangDesc = new System.Windows.Forms.Label();
			this.cbLangDesc = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.btnAddLangDesc = new System.Windows.Forms.Button();
			this.btnAddCustomField = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.rbAbbrName = new System.Windows.Forms.RadioButton();
			this.rbAbbrAbbr = new System.Windows.Forms.RadioButton();
			this.lblAbbr = new System.Windows.Forms.Label();
			this.panel5 = new System.Windows.Forms.Panel();
			this.cbFunction = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblFunction = new System.Windows.Forms.Label();
			this.chkbxAutoField = new System.Windows.Forms.CheckBox();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.panelBottom = new System.Windows.Forms.Panel();
#if !__MonoCS__
			this.webBrowserInfo = new System.Windows.Forms.WebBrowser();
#else
			this.m_browser = new GeckoWebBrowser();
#endif
			this.btnShowInfo = new System.Windows.Forms.Button();
			this.panelBottom.SuspendLayout();
			this.SuspendLayout();
			//
			// lblMarker
			//
			resources.ApplyResources(this.lblMarker, "lblMarker");
			this.lblMarker.Name = "lblMarker";
			//
			// m_lblMarker
			//
			resources.ApplyResources(this.m_lblMarker, "m_lblMarker");
			this.m_lblMarker.Name = "m_lblMarker";
			//
			// m_lblInstances
			//
			resources.ApplyResources(this.m_lblInstances, "m_lblInstances");
			this.m_lblInstances.Name = "m_lblInstances";
			//
			// panel1
			//
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// chkbxExclude
			//
			resources.ApplyResources(this.chkbxExclude, "chkbxExclude");
			this.chkbxExclude.Name = "chkbxExclude";
			this.chkbxExclude.CheckedChanged += new System.EventHandler(this.chkbxExclude_CheckedChanged);
			//
			// lblDestinationInFlex
			//
			resources.ApplyResources(this.lblDestinationInFlex, "lblDestinationInFlex");
			this.lblDestinationInFlex.Name = "lblDestinationInFlex";
			//
			// tvDestination
			//
			this.tvDestination.FullRowSelect = true;
			this.tvDestination.HideSelection = false;
			resources.ApplyResources(this.tvDestination, "tvDestination");
			this.tvDestination.Name = "tvDestination";
			this.tvDestination.ShowLines = false;
			this.tvDestination.ShowPlusMinus = false;
			this.tvDestination.ShowRootLines = false;
			this.tvDestination.Sorted = true;
			this.tvDestination.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvDestination_AfterSelect);
			this.tvDestination.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvDestination_BeforeSelect);
			//
			// blbLangDesc
			//
			resources.ApplyResources(this.blbLangDesc, "blbLangDesc");
			this.blbLangDesc.Name = "blbLangDesc";
			//
			// cbLangDesc
			//
			this.cbLangDesc.AllowSpaceInEditBox = false;
			this.cbLangDesc.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbLangDesc, "cbLangDesc");
			this.cbLangDesc.Name = "cbLangDesc";
			//
			// btnAddLangDesc
			//
			resources.ApplyResources(this.btnAddLangDesc, "btnAddLangDesc");
			this.btnAddLangDesc.Name = "btnAddLangDesc";
			this.btnAddLangDesc.Click += new System.EventHandler(this.btnAddLangDesc_Click);
			//
			// btnAddCustomField
			//
			resources.ApplyResources(this.btnAddCustomField, "btnAddCustomField");
			this.btnAddCustomField.Name = "btnAddCustomField";
			this.btnAddCustomField.Click += new System.EventHandler(this.btnAddCustomField_Click);
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			//
			// rbAbbrName
			//
			resources.ApplyResources(this.rbAbbrName, "rbAbbrName");
			this.rbAbbrName.Name = "rbAbbrName";
			//
			// rbAbbrAbbr
			//
			resources.ApplyResources(this.rbAbbrAbbr, "rbAbbrAbbr");
			this.rbAbbrAbbr.Name = "rbAbbrAbbr";
			//
			// lblAbbr
			//
			resources.ApplyResources(this.lblAbbr, "lblAbbr");
			this.lblAbbr.Name = "lblAbbr";
			//
			// panel5
			//
			this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.panel5, "panel5");
			this.panel5.Name = "panel5";
			//
			// cbFunction
			//
			this.cbFunction.AllowSpaceInEditBox = false;
			resources.ApplyResources(this.cbFunction, "cbFunction");
			this.cbFunction.Name = "cbFunction";
			this.cbFunction.SelectedIndexChanged += new System.EventHandler(this.cbFunction_SelectedIndexChanged);
			//
			// lblFunction
			//
			resources.ApplyResources(this.lblFunction, "lblFunction");
			this.lblFunction.Name = "lblFunction";
			//
			// chkbxAutoField
			//
			resources.ApplyResources(this.chkbxAutoField, "chkbxAutoField");
			this.chkbxAutoField.Name = "chkbxAutoField";
			this.chkbxAutoField.CheckedChanged += new System.EventHandler(this.chkbxAutoField_CheckedChanged);
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// panelBottom
			//
#if !__MonoCS__
			this.panelBottom.Controls.Add(this.webBrowserInfo);
#else
			this.panelBottom.Controls.Add(this.m_browser);
#endif
			resources.ApplyResources(this.panelBottom, "panelBottom");
			this.panelBottom.Name = "panelBottom";
#if !__MonoCS__
			//
			// webBrowserInfo
			//
			resources.ApplyResources(this.webBrowserInfo, "webBrowserInfo");
			this.webBrowserInfo.IsWebBrowserContextMenuEnabled = false;
			this.webBrowserInfo.MinimumSize = new System.Drawing.Size(20, 20);
			this.webBrowserInfo.Name = "webBrowserInfo";
			this.webBrowserInfo.WebBrowserShortcutsEnabled = false;
#else
			this.m_browser.NoDefaultContextMenu = true;
			this.m_browser.MinimumSize = new System.Drawing.Size(20, 20);
			this.m_browser.Dock = System.Windows.Forms.DockStyle.Fill;
#endif
			//
			// btnShowInfo
			//
			resources.ApplyResources(this.btnShowInfo, "btnShowInfo");
			this.btnShowInfo.Name = "btnShowInfo";
			this.btnShowInfo.UseVisualStyleBackColor = true;
			this.btnShowInfo.Click += new System.EventHandler(this.btnShowInfo_Click);
			//
			// LexImportWizardMarker
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.btnShowInfo);
			this.Controls.Add(this.panelBottom);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.chkbxAutoField);
			this.Controls.Add(this.lblFunction);
			this.Controls.Add(this.cbFunction);
			this.Controls.Add(this.panel5);
			this.Controls.Add(this.lblAbbr);
			this.Controls.Add(this.rbAbbrAbbr);
			this.Controls.Add(this.rbAbbrName);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnAddCustomField);
			this.Controls.Add(this.btnAddLangDesc);
			this.Controls.Add(this.cbLangDesc);
			this.Controls.Add(this.blbLangDesc);
			this.Controls.Add(this.tvDestination);
			this.Controls.Add(this.lblDestinationInFlex);
			this.Controls.Add(this.chkbxExclude);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.m_lblInstances);
			this.Controls.Add(this.m_lblMarker);
			this.Controls.Add(this.lblMarker);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LexImportWizardMarker";
			this.Load += new System.EventHandler(this.LexImportWizardMarker_Load);
			this.panelBottom.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void LexImportWizardMarker_Load(object sender, System.EventArgs e)
		{

		}

		private void tvDestination_BeforeSelect(object sender, System.Windows.Forms.TreeViewCancelEventArgs e)
		{
			// don't allow the class nodes to be selected
			if (e.Node.Tag == null)
			{
				e.Cancel = true;	// cancel the current selection change
				if (tvDestination.SelectedNode == null)	// no previous node selected
				{
					tvDestination.SelectedNode = e.Node.FirstNode;
					return;
				}
				TreeNode parent = tvDestination.SelectedNode.Parent;
				if (parent.Index < e.Node.Index)	// going down
					tvDestination.SelectedNode = parent.NextNode.FirstNode;
				else if (e.Node.Index > 0)			// going up
					tvDestination.SelectedNode = parent.PrevNode.LastNode;
				else						// at to Top
					e.Node.EnsureVisible();	// show the class for the selected item
			}
			else
			{
				if (e.Node == e.Node.Parent.FirstNode)
					e.Node.Parent.EnsureVisible();	// make sure class is visible if first
			}
		}

		private void chkbxExclude_CheckedChanged(object sender, EventArgs e)
		{
			tvDestination.Enabled = !chkbxExclude.Checked && !chkbxAutoField.Checked;
			EnableLangDesc(!chkbxExclude.Checked);
			chkbxAutoField.Enabled = !chkbxExclude.Checked;
			UpdateOKButtonState();
		}

		private void btnAddLangDesc_Click(object sender, EventArgs e)
		{
			using (var dlg = new LexImportWizardLanguage(m_cache, m_uiLangs, m_helpTopicProvider, m_app, m_stylesheet))
			{
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				string langDesc, ws, ec, wsId;
				// retrieve the new WS information from the dlg
				dlg.GetCurrentLangInfo(out langDesc, out ws, out ec, out wsId);

				// now put the lang info into the language list view
				if (LexImportWizard.Wizard().AddLanguage(langDesc, ws, ec, wsId))
				{
					// this was added to the list of languages, so add it to the dlg and select it
					var langInfo = new Sfm2Xml.LanguageInfoUI(langDesc, ws, ec, wsId);
					if (cbLangDesc.FindStringExact(langInfo.ToString()) < 0)
					{
						cbLangDesc.Items.Add(langInfo);
					}
					cbLangDesc.SelectedItem = langInfo;
					}
				}
			}
		}

		private void EnableControlsFromField(Sfm2Xml.LexImportField field)
		{
			// see if the abbr controls should be enabled or not
			bool enable = false;
			if (field != null)
				enable = field.IsAbbrField;
			lblAbbr.Enabled = enable;
			rbAbbrName.Enabled = enable;
			rbAbbrAbbr.Enabled = enable;
			// see if the function controls should be enabled
			if (field != null)
				enable = field.IsRef;
			lblFunction.Enabled = enable;
			cbFunction.Enabled = enable;
			if (lblFunction.Enabled == false)
				lblFunction.Text = "Not An Active Field :";
		}

		Sfm2Xml.LexImportField m_LastSelectedField;

		private void tvDestination_AfterSelect(object sender, TreeViewEventArgs e)
		{
			UpdateOKButtonState();
			Sfm2Xml.LexImportField field = e.Node.Tag as Sfm2Xml.LexImportField;
			if (field == null)
				return;

			EnableControlsFromField(field);
			FillLexicalRefTypesCombo(field);
			ShowInfo(field);
		}

		private void AddAbbrAndNameInfo(IMultiUnicode abbr, IMultiUnicode name, IMultiUnicode reverseAbbr, IMultiUnicode reverseName)
		{
			int wsActual;
			ITsString tssAnal;

			if (name != null)
			{
				string sname, snameWS, sabbr, sabbrWS;
				tssAnal = name.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
				sname = tssAnal.Text;
				snameWS = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);

				cbFunction.Items.Add(sname);

				if (!m_htNameToAbbr.ContainsKey(sname))
				{
					if (abbr == null)
					{
						sabbr = sname;	// use both for the map key
						sabbrWS = snameWS;
					}
					else
					{
						tssAnal = abbr.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
						sabbr = tssAnal.Text;
						sabbrWS = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);
					}
					NameWSandAbbr nwsa = new NameWSandAbbr();
					nwsa.name = sname;
					nwsa.nameWS = snameWS;
					nwsa.abbr = sabbr;
					nwsa.abbrWS = sabbrWS;
					m_htNameToAbbr.Add(sname, nwsa);
				}
			}
			if (reverseName != null)
			{
				string srname, srnameWS, srabbr, srabbrWS;
				tssAnal = reverseName.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
				srname = tssAnal.Text;
				srnameWS = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);

				cbFunction.Items.Add(srname);

				if (!m_htNameToAbbr.ContainsKey(srname))
				{
					if (reverseAbbr == null)
					{
						srabbr = srname;	// use both for the map key
						srabbrWS = srnameWS;
					}
					else
					{
						tssAnal = reverseAbbr.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
						srabbr = tssAnal.Text;
						srabbrWS = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);
					}
					NameWSandAbbr nwsa = new NameWSandAbbr();
					nwsa.name = srname;
					nwsa.nameWS = srnameWS;
					nwsa.abbr = srabbr;
					nwsa.abbrWS = srabbrWS;
					m_htNameToAbbr.Add(srname, nwsa);
				}
			}
		}

		private void FillLexicalRefTypesCombo(Sfm2Xml.LexImportField field)
		{
			if (m_LastSelectedField == field)
				return;		// don't change
			m_LastSelectedField = field;

			// fill the combo box with current values in the DB
			cbFunction.Items.Clear();
			m_htNameToAbbr.Clear();
			if (field.IsRef)
			{
				lblFunction.Text = LexTextControls.ksLexicalRelationType;
				int pos = -1;
				//string abbr, name, reverseAbbr, reverseName;
				rbAbbrAbbr.Checked = true;
				rbAbbrName.Checked = false;
				if (field.ID == "cref" || field.ID == "scref")
				{
					// Fill the comboBox with the names (and where appropriate, reverse names) of the
					// LexRefType objects which map to entries.
					foreach (ILexRefType lrt in m_cache.LanguageProject.LexDbOA.ReferencesOA.PossibilitiesOS)
					{
						switch (lrt.MappingType)
						{
							case (int)MappingTypes.kmtEntryCollection:
							case (int)MappingTypes.kmtEntryPair:
							case (int)MappingTypes.kmtEntrySequence:
							case (int)MappingTypes.kmtEntryOrSenseCollection:
							case (int)MappingTypes.kmtEntryOrSensePair:
							case (int)MappingTypes.kmtEntryOrSenseSequence:
								//abbr = lrt.Abbreviation.AnalysisDefaultWritingSystem.Text;
								//name = lrt.Name.AnalysisDefaultWritingSystem.Text;
								//AddAbbrAndNameInfo(abbr, name, "en", null, null, null);
								AddAbbrAndNameInfo(lrt.Abbreviation, lrt.Name, null, null);
								break;
							case (int)MappingTypes.kmtEntryAsymmetricPair:
							case (int)MappingTypes.kmtEntryTree:
							case (int)MappingTypes.kmtEntryOrSenseAsymmetricPair:
							case (int)MappingTypes.kmtEntryOrSenseTree:
								//abbr = lrt.Abbreviation.AnalysisDefaultWritingSystem.Text;
								//name = lrt.Name.AnalysisDefaultWritingSystem.Text;
								//reverseAbbr = lrt.ReverseAbbreviation.AnalysisDefaultWritingSystem.Text;
								//reverseName = lrt.ReverseName.AnalysisDefaultWritingSystem.Text;
								//AddAbbrAndNameInfo(abbr, name, "en", reverseAbbr, reverseName, "en");
								AddAbbrAndNameInfo(lrt.Abbreviation, lrt.Name, lrt.ReverseAbbreviation, lrt.ReverseName);
								break;
						}
					}
				}
				else if (field.ID == "lxrel")
				{
					// Fill the comboBox with the names (and where appropriate, reverse names) of the
					// LexRefType objects which map to senses.
					foreach (ILexRefType lrt in m_cache.LanguageProject.LexDbOA.ReferencesOA.PossibilitiesOS)
					{
						switch (lrt.MappingType)
						{
							case (int)MappingTypes.kmtSenseCollection:
							case (int)MappingTypes.kmtSensePair:
							case (int)MappingTypes.kmtSenseSequence:
							case (int)MappingTypes.kmtEntryOrSenseCollection:
							case (int)MappingTypes.kmtEntryOrSensePair:
							case (int)MappingTypes.kmtEntryOrSenseSequence:
								//abbr = lrt.Abbreviation.AnalysisDefaultWritingSystem.Text;
								//name = lrt.Name.AnalysisDefaultWritingSystem.Text;
								//AddAbbrAndNameInfo(abbr, name, "en", null, null, null);
								AddAbbrAndNameInfo(lrt.Abbreviation, lrt.Name, null, null);
								break;
							case (int)MappingTypes.kmtSenseAsymmetricPair:
							case (int)MappingTypes.kmtSenseTree:
							case (int)MappingTypes.kmtEntryOrSenseAsymmetricPair:
							case (int)MappingTypes.kmtEntryOrSenseTree:
								//abbr = lrt.Abbreviation.AnalysisDefaultWritingSystem.Text;
								//name = lrt.Name.AnalysisDefaultWritingSystem.Text;
								//reverseAbbr = lrt.ReverseAbbreviation.AnalysisDefaultWritingSystem.Text;
								//reverseName = lrt.ReverseName.AnalysisDefaultWritingSystem.Text;
								//AddAbbrAndNameInfo(abbr, name, "en", reverseAbbr, reverseName, "en");
								AddAbbrAndNameInfo(lrt.Abbreviation, lrt.Name, lrt.ReverseAbbreviation, lrt.ReverseName);
								break;
						}
					}
				}
				else if (field.ID == "var")
				{
					lblFunction.Text = LexTextControls.ksVariantType;
					// fill the comboBox with the names of the Variant objects
					foreach (var let in m_cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities)
					{
						AddAbbrAndNameInfo(let.Abbreviation, let.Name, null, null);
						//int wsActual;
						//ITsString tssAnal = let.Name. GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
						//name = tssAnal.Text;
						//string ws = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);
						//AddAbbrAndNameInfo(null, name, ws, null, null, null);
					}
				}
				else if (field.ID == "sub")
				{
					lblFunction.Text = LexTextControls.ksComplexFormType;
					// fill the comboBox with the names of the Variant objects
					foreach (var let in m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities)
					{
						AddAbbrAndNameInfo(let.Abbreviation, let.Name, null, null);
						//int wsActual;
						//ITsString tssAnal = let.Name.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
						//name = tssAnal.Text;
						//string ws = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);
						//AddAbbrAndNameInfo(null, name, ws, null, null, null);
					}
				}


				// now select the one with the correct abbreviation
				pos = -1;
				if (m_refFuncString.Length > 0)
					pos = cbFunction.FindString(m_refFuncString);
				if (pos >= 0)
				{
					cbFunction.SelectedIndex = pos;
					cbFunction.Text = cbFunction.SelectedItem as string;
				}
			}
			// The radio buttons for abbr and Name are set when initialized - so don't reset them
		}

		private void chkbxAutoField_CheckedChanged(object sender, System.EventArgs e)
		{
			tvDestination.Enabled = !chkbxExclude.Checked && !chkbxAutoField.Checked;
			UpdateOKButtonState();
		}

		/// <summary>
		/// It only makes sense for the user to press ok if they have selected a destination field OR
		/// the field is an Auto field or it's to be excluded.
		/// </summary>
		private void UpdateOKButtonState()
		{
			bool enable = false;	// default to not enabled
			if (chkbxExclude.Checked || chkbxAutoField.Checked ||
				tvDestination.SelectedNode != null)	// something is selected
				enable = true;

			if (btnOK.Enabled != enable)
			{
				if (enable)
				{
//					btnOK.StyleChanged
				}
				btnOK.Enabled = enable;
			}
		}

		private void cbFunction_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (cbFunction.SelectedIndex >= 0)
				m_refFuncString = cbFunction.SelectedItem as string;
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
		private void InitBottomPanel()
		{

#if !__MonoCS__
			webBrowserInfo.Navigate(m_sHelpHtm);
#else
			if (m_browser.Handle != IntPtr.Zero)
				m_browser.Navigate(m_sHelpHtm);
#endif

			// init transform used in help panel
			m_xslShowInfoTransform = new XslCompiledTransform();
			string sXsltFile = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer/Import/ImportFieldsHelpToHtml.xsl");
			m_xslShowInfoTransform.Load(sXsltFile);
			// init XmlDoc, too
			m_xmlShowInfoDoc = new XmlDocument();
		}
		private void ShowInfo(Sfm2Xml.LexImportField field)
		{
			XmlNode node = field.Node;
			////if (node == null)
			////{
			////    // custom fields didn't come from the ImportFields.xml file so the node doesn't exist
			////    webBrowserInfo.DocumentText = "";	// just make it empty for now
			////    return;
			////}
#if __MonoCS__
			var tempfile = Path.Combine(Path.GetTempPath(), "temphelp.htm");
			using (StreamWriter w = new StreamWriter(tempfile, false))
#else
			using (StringWriter w = new StringWriter())
#endif
			using (XmlTextWriter tw = new XmlTextWriter(w))
			{
				m_xmlShowInfoDoc.LoadXml(node.OuterXml); // N.B. LoadXml requires UTF-16 or UCS-2 encodings
				m_xslShowInfoTransform.Transform(m_xmlShowInfoDoc, tw);
#if !__MonoCS__
				webBrowserInfo.DocumentText = w.GetStringBuilder().ToString();
#endif
			}
#if __MonoCS__
			m_browser.Navigate(tempfile);
#endif
		}

		private void btnShowInfo_Click(object sender, EventArgs e)
		{
			Size sz = new Size(ClientSize.Width, ClientSize.Height);
			if (panelBottom.Visible)
			{
				panelBottom.Visible = false;
				btnShowInfo.Text = LexTextControls.ksS_howInfo;
				m_panelBottomHeight = panelBottom.Size.Height;
				sz.Height -= panelBottom.Size.Height;
				ClientSize = sz;
			}
			else
			{
				panelBottom.Visible = true;
				btnShowInfo.Text = LexTextControls.ks_HideInfo;
				sz.Height += m_panelBottomHeight;
				ClientSize = sz;
			}
		}

		private void btnAddCustomField_Click(object sender, EventArgs e)
		{
			// Mediator accessor in the LexImportWizard now checks to make sure that
			// it's not disposed - if it is it creates a new one.
			XCore.Mediator med = null;
			LexImportWizard wiz = LexImportWizard.Wizard();
			if (wiz != null)
				med = wiz.Mediator;
			if (med == null)
			{
				// See LT-9100 and LT-9266.  Apparently this condition can happen.
				MessageBox.Show(LexTextControls.ksCannotSoTryAgain, LexTextControls.ksInternalProblem,
								MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			med.SendMessage("AddCustomField", null);

			// The above call can cause the Mediator to 'go away', so check it and
			// restore the member variable for everyone else who may be surprised
			// that it is gone ... or not

			// At this point there could be custom fields added or removed, so we have to
			// recalculate the customfields and populate the TVcontrol based on the currently
			// defined custom fields.

			bool customFieldsChanged = false;
			m_customFields = wiz.ReadCustomFieldsFromDB(out customFieldsChanged);

			// if the custom fields have changed any, then update the display with the changes
			if (customFieldsChanged)
				AddCustomFieldsToPossibleFields();
		}

		private void AddCustomFieldsToPossibleFields()
		{
			// The first pass is not a 'smart' approach, but a 'get it done' approach
			// first remove all custom fields from the list
			// now add all current custom fields to the list

			tvDestination.BeginUpdate();
			foreach (TreeNode classNameNode in tvDestination.Nodes)
			{
				// Remove any existing custom entries from the list (add them back due to possible changes in name etc...)
				TreeNode[] leaves = new TreeNode[classNameNode.Nodes.Count];
				classNameNode.Nodes.CopyTo(leaves, 0);
				foreach (TreeNode leafNode in leaves)
				{
					if (leafNode.Tag is Sfm2Xml.LexImportCustomField)
						classNameNode.Nodes.Remove(leafNode);
				}

				// Now add any custom fields for this class
				string className = classNameNode.Text.Trim(new char[] { '(', ')' });
				if (m_customFields.FieldsForClass(className) == null)
					continue;

				foreach (Sfm2Xml.LexImportField field in m_customFields.FieldsForClass(className))
				{
					TreeNode cnode = new TreeNode(field.UIName + " (Custom Field)");
					cnode.Tag = field;
					classNameNode.Nodes.Add(cnode);
//					m_customFieldNodes.Add(field, cnode);
				}

			}

			tvDestination.EndUpdate();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			// See if the 'function' value is a user entered one and is the current selection, if
			// so then add it to the correct list in the DB.

			if (lblFunction.Enabled)
			{
				string funcText = cbFunction.Text;
				// add case to handle when the user hasn't entered text or selected any items in the drop down list.
				if (funcText.Length == 0)
				{
					string labelText = lblFunction.Text.TrimEnd(new[]{' ', ':'});
					MessageBox.Show("You must select a '"+labelText+"' item or enter a new one to continue.", "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					cbFunction.Focus();
					return;
				}
				if (!cbFunction.Items.Contains(funcText))
				{
					// found case where the user has entered their own text and want to add it to the proper list
					TreeNode tn = tvDestination.SelectedNode;
					var field = tn.Tag as Sfm2Xml.LexImportField;
					if (field != null && field.IsRef)
					{
						var entryTypefactory = m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
						ICmPossibility newType;
						IFdoOwningSequence<ICmPossibility> owningSeq;
						string description;
						switch (field.ID)
						{
							default:
								throw new ArgumentException("Unrecognized type.");
							case "var":
								newType = entryTypefactory.Create();
								owningSeq = m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS;
								description = "A \"var\" type entered by the user during the import process.";
								break;
							case "sub":
								newType = entryTypefactory.Create();
								owningSeq = m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS;
								description = "A \"sub\" type entered by the user during the import process.";
								break;
							case "lxrel":
								newType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
								owningSeq = m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS;
								description = "A \"lxrel\" type entered by the user during the import process.";
								break;
						}
						NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
							{
								owningSeq.Add(newType);
								var strFact = m_cache.TsStrFactory;
								var userWs = m_cache.WritingSystemFactory.UserWs;
								newType.Name.set_String(userWs, strFact.MakeString(funcText, userWs));
								newType.Description.set_String(userWs, strFact.MakeString(description, userWs));
							});
					}
				}
			}

			// Allows accessing FWDestID, FWDestinationClass, IsCustomField properties reliably
			// after dialog is closed.
			m_StoredTreeNode = tvDestination.SelectedNode;
			DialogResult = DialogResult.OK;
		}

		public MarkerPresenter.ContentMapping GetContentMapping()
		{
			//MarkerPresenter.ContentMapping cm = new MarkerPresenter.ContentMapping("xx", "xxx",
			//            public ContentMapping(string marker, string desc, string className, string fwDest,
			//    string ws, string langDescriptor, int count, int order, Sfm2Xml.ClsFieldDescription fdesc, bool isCustom)
			return null;
		}
	}
}
