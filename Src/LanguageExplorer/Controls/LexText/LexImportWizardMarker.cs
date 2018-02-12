// Copyright (c) 2005-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using Gecko;
using Sfm2Xml;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Xml;
using TreeView = System.Windows.Forms.TreeView;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary />
	public class LexImportWizardMarker : Form
	{
		private Label lblMarker;
		private Label m_lblMarker;
		private Panel panel1;
		private CheckBox chkbxExclude;
		private Label m_lblInstances;
		private Label lblDestinationInFlex;
		private TreeView tvDestination;
		private TreeNode m_StoredTreeNode = null;
		private Label blbLangDesc;
		private FwOverrideComboBox cbLangDesc;
		private Button btnAddLangDesc;
		private Button btnAddCustomField;
		private Button btnOK;
		private Button btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private Hashtable m_uiLangs;
		private RadioButton rbAbbrName;
		private RadioButton rbAbbrAbbr;
		private Label lblAbbr;
		private Panel panel5;
		private FwOverrideComboBox cbFunction;
		private Label lblFunction;
		private CheckBox chkbxAutoField;
		private LcmCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private string m_refFuncString;
		private string m_refFuncStringOrig;
		private Button buttonHelp;	// initial value
		private Hashtable m_htNameToAbbr = new Hashtable();
		private struct NameWSandAbbr	// objects that are stuffed in the m_htNameToAbbr hashtable
		{
			public string Name;
			public string NameWS;
			public string Abbr;
		};
		private const string s_helpTopic = "khtpImportSFMModifyMapping";
		private Panel panelBottom;
		private Button btnShowInfo;
		private GeckoWebBrowser m_browser;
		private XslCompiledTransform m_xslShowInfoTransform;
		private XmlDocument m_xmlShowInfoDoc;
		private string m_sHelpHtm = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Import", "Help.htm");
		private int m_panelBottomHeight;
		private HelpProvider _helpProvider;

		private void EnableLangDesc(bool enable)
		{
			blbLangDesc.Enabled = cbLangDesc.Enabled = btnAddLangDesc.Enabled = enable;
		}

		public void Init(ContentMapping currentMarker, Hashtable uiLangsHT, LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app)
		{
			CheckDisposed();

			m_uiLangs = uiLangsHT;
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			_helpProvider.HelpNamespace = helpTopicProvider.HelpFile;
			_helpProvider.SetHelpKeyword(this, helpTopicProvider.GetHelpString(s_helpTopic));
			_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

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
			bool customFieldsChanged;
			CustomFields = LexImportWizard.Wizard().ReadCustomFieldsFromDB(out customFieldsChanged);

			// Init will only be called the first time, so here we don't have to remove an nodes

			tvDestination.BeginUpdate();
			foreach (TreeNode classNameNode in tvDestination.Nodes)
			{
				var className = classNameNode.Text.Trim('(', ')');
				if (CustomFields.FieldsForClass(className) == null)
				{
					continue;
				}

				foreach (LexImportField field in CustomFields.FieldsForClass(className))
				{
					var cnode = new TreeNode(field.UIName + " (Custom Field)")
					{
						Tag = field
					};
					classNameNode.Nodes.Add(cnode);
				}
			}
			tvDestination.EndUpdate();
			// end of CFS processing

			// set the correct marker and number of times it is used
			m_lblMarker.Text = currentMarker.Marker;
			m_lblInstances.Text = string.Format(LexTextControls.ksXInstances, currentMarker.Count);

			if (currentMarker.AutoImport)
			{
				chkbxAutoField.Checked = true;
				tvDestination.Enabled = false;
			}

			// chkbxExclude.Checked = false;
			// tvDestination.Enabled = true;
			// find the node that has the tag.meaningid value the same as the currentmarker
			var found = false;
			foreach (TreeNode classNode in tvDestination.Nodes)
			{
				if (currentMarker.ClsFieldDescription is ClsCustomFieldDescription &&
					currentMarker.DestinationClass != classNode.Text.Trim('(', ')'))
				{
					continue;
				}

				foreach (TreeNode fieldNode in classNode.Nodes)
				{
					if ((fieldNode.Tag as LexImportField).ID == currentMarker.FwId)
					{
						tvDestination.SelectedNode = fieldNode;
						found = true;
						break;
					}
				}

				if (found)
				{
					break;
				}
			}

			if (!found && tvDestination.Nodes.Count > 0) // make first entry topmost and visible
			{
				tvDestination.Nodes[0].EnsureVisible();
			}

			// set the writing system combo box
			foreach (DictionaryEntry lang in m_uiLangs)
			{
				var langInfo = lang.Value as LanguageInfoUI;
				// make sure there is only one entry for each writing system (especially 'ignore')
				if (cbLangDesc.FindStringExact(langInfo.ToString()) < 0)
				{
					cbLangDesc.Items.Add(langInfo);
					if (langInfo.FwName == currentMarker.WritingSystem)
					{
						cbLangDesc.SelectedItem = langInfo;
					}
				}
			}
			if (cbLangDesc.SelectedIndex < 0)
			{
				// default to ignore if it's in the list
				var ignorePos = cbLangDesc.FindStringExact(ContentMapping.Ignore());
				cbLangDesc.SelectedIndex = ignorePos >= 0 ? ignorePos : 0;
			}

			// add the func if it's present
			m_refFuncString = string.Empty;
			if (currentMarker.IsRefField)
			{
				m_refFuncString = currentMarker.RefField;

				cbFunction.Enabled = true;

				var node = tvDestination.SelectedNode;
				var field = node?.Tag as LexImportField;
				if (field != null)
				{
					FillLexicalRefTypesCombo(field);
					// walk the name to abbr list and select the name
					var name = m_refFuncString;
					foreach (DictionaryEntry de in m_htNameToAbbr)
					{
						var nwsa = (NameWSandAbbr)de.Value;
						if (nwsa.Abbr == name)
						{
							name = nwsa.Name;
							break;
						}
					}
					cbFunction.Text = name;
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
				{
					rbAbbrAbbr.Checked = true;
				}
			}
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

				var langinfo = cbLangDesc.SelectedItem as LanguageInfoUI;
				return langinfo.FwName;
			}
		}
		public string LangDesc
		{
			get
			{
				CheckDisposed();

				var langinfo = cbLangDesc.SelectedItem as LanguageInfoUI;
				return langinfo.Key;
			}
		}

		public string FWDestID
		{
			get
			{
				CheckDisposed();
				var node = m_StoredTreeNode ?? tvDestination.SelectedNode;
				return node == null ? string.Empty : (node.Tag as LexImportField)?.ID;
			}
		}

		public string FWDestinationClass
		{
			get
			{
				CheckDisposed();
				var node = m_StoredTreeNode ?? tvDestination.SelectedNode;
				return node?.Parent.Text.Trim('(', ')') ?? string.Empty;
			}
		}


		public bool IsCustomField
		{
			get
			{
				CheckDisposed();
				var node = m_StoredTreeNode ?? tvDestination.SelectedNode;
				return node?.Tag is LexImportCustomField;//.IsCustomField;
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
		public bool IsFuncField => cbFunction.Enabled;

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
						var nwsa = (NameWSandAbbr)m_htNameToAbbr[cbFunction.Text];
						return nwsa.Name;
					}
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
						var nwsa = (NameWSandAbbr)m_htNameToAbbr[cbFunction.Text];
						return nwsa.NameWS;
					}
					return "en";
				}
				return "en";	// default to initial value if not found
			}
		}

		public ILexImportFields CustomFields { get; private set; } = new LexImportFields();

		public LexImportWizardMarker(ILexImportFields fwFields)
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
				var tnode = new TreeNode($"({className})");
				foreach (LexImportField field in fwFields.FieldsForClass(className))
				{
					var cnode = new TreeNode(field.UIName)
					{
						Tag = field
					};
					tnode.Nodes.Add(cnode);
				}
				tvDestination.Nodes.Add(tnode);
			}
			tvDestination.ExpandAll();
			tvDestination.EndUpdate();

			_helpProvider = new HelpProvider();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				components?.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
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
			this.m_browser = new Gecko.GeckoWebBrowser();
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
			this.tvDestination.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvDestination_BeforeSelect);
			this.tvDestination.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvDestination_AfterSelect);
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
			this.panelBottom.Controls.Add(this.m_browser);
			resources.ApplyResources(this.panelBottom, "panelBottom");
			this.panelBottom.Name = "panelBottom";
			//
			// m_browser
			//
			resources.ApplyResources(this.m_browser, "m_browser");
			this.m_browser.Name = "m_browser";
			this.m_browser.NoDefaultContextMenu = true;
			this.m_browser.UseHttpActivityObserver = false;
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
			this.panelBottom.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void tvDestination_BeforeSelect(object sender, TreeViewCancelEventArgs e)
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
				var parent = tvDestination.SelectedNode.Parent;
				if (parent.Index < e.Node.Index) // going down
				{
					tvDestination.SelectedNode = parent.NextNode.FirstNode;
				}
				else if (e.Node.Index > 0) // going up
				{
					tvDestination.SelectedNode = parent.PrevNode.LastNode;
				}
				else                        // at to Top
				{
					e.Node.EnsureVisible();	// show the class for the selected item
				}
			}
			else
			{
				if (e.Node == e.Node.Parent.FirstNode)
				{
					e.Node.Parent.EnsureVisible();	// make sure class is visible if first
				}
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
			using (var dlg = new LexImportWizardLanguage(m_cache, m_uiLangs, m_helpTopicProvider, m_app))
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
						var langInfo = new LanguageInfoUI(langDesc, ws, ec, wsId);
						if (cbLangDesc.FindStringExact(langInfo.ToString()) < 0)
						{
							cbLangDesc.Items.Add(langInfo);
						}
						cbLangDesc.SelectedItem = langInfo;
					}
				}
			}
		}

		private void EnableControlsFromField(LexImportField field)
		{
			// see if the abbr controls should be enabled or not
			var enable = false;
			if (field != null)
			{
				enable = field.IsAbbrField;
			}
			lblAbbr.Enabled = enable;
			rbAbbrName.Enabled = enable;
			rbAbbrAbbr.Enabled = enable;
			// see if the function controls should be enabled
			if (field != null)
			{
				enable = field.IsRef;
			}
			lblFunction.Enabled = enable;
			cbFunction.Enabled = enable;
			if (lblFunction.Enabled == false)
			{
				lblFunction.Text = "Not An Active Field :";
			}
		}

		LexImportField m_LastSelectedField;

		private void tvDestination_AfterSelect(object sender, TreeViewEventArgs e)
		{
			UpdateOKButtonState();
			var field = e.Node.Tag as LexImportField;
			if (field == null)
			{
				return;
			}

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
				tssAnal = name.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
				var sname = tssAnal.Text;
				var snameWS = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);

				cbFunction.Items.Add(sname);

				if (!m_htNameToAbbr.ContainsKey(sname))
				{
					string sabbr;
					if (abbr == null)
					{
						sabbr = sname;	// use both for the map key
					}
					else
					{
						tssAnal = abbr.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
						sabbr = tssAnal.Text;
					}
					var nwsa = new NameWSandAbbr
					{
						Name = sname,
						NameWS = snameWS,
						Abbr = sabbr
					};
					m_htNameToAbbr.Add(sname, nwsa);
				}
			}
			if (reverseName != null)
			{
				tssAnal = reverseName.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
				var srname = tssAnal.Text;
				var srnameWS = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);

				cbFunction.Items.Add(srname);

				if (!m_htNameToAbbr.ContainsKey(srname))
				{
					string srabbr;
					if (reverseAbbr == null)
					{
						srabbr = srname;	// use both for the map key
					}
					else
					{
						tssAnal = reverseAbbr.GetAlternativeOrBestTss(m_cache.DefaultAnalWs, out wsActual);
						srabbr = tssAnal.Text;
					}
					var nwsa = new NameWSandAbbr
					{
						Name = srname,
						NameWS = srnameWS,
						Abbr = srabbr
					};
					m_htNameToAbbr.Add(srname, nwsa);
				}
			}
		}

		private void FillLexicalRefTypesCombo(LexImportField field)
		{
			if (m_LastSelectedField == field)
			{
				return;		// don't change
			}
			m_LastSelectedField = field;

			// fill the combo box with current values in the DB
			cbFunction.Items.Clear();
			m_htNameToAbbr.Clear();
			if (field.IsRef)
			{
				lblFunction.Text = LexTextControls.ksLexicalRelationType;
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
							case (int)MappingTypes.kmtEntryUnidirectional:
							case (int)MappingTypes.kmtEntryOrSenseCollection:
							case (int)MappingTypes.kmtEntryOrSensePair:
							case (int)MappingTypes.kmtEntryOrSenseSequence:
							case (int)MappingTypes.kmtEntryOrSenseUnidirectional:
								AddAbbrAndNameInfo(lrt.Abbreviation, lrt.Name, null, null);
								break;
							case (int)MappingTypes.kmtEntryAsymmetricPair:
							case (int)MappingTypes.kmtEntryTree:
							case (int)MappingTypes.kmtEntryOrSenseAsymmetricPair:
							case (int)MappingTypes.kmtEntryOrSenseTree:
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
							case (int)MappingTypes.kmtSenseUnidirectional:
							case (int)MappingTypes.kmtEntryOrSenseCollection:
							case (int)MappingTypes.kmtEntryOrSensePair:
							case (int)MappingTypes.kmtEntryOrSenseSequence:
							case (int)MappingTypes.kmtEntryOrSenseUnidirectional:
								AddAbbrAndNameInfo(lrt.Abbreviation, lrt.Name, null, null);
								break;
							case (int)MappingTypes.kmtSenseAsymmetricPair:
							case (int)MappingTypes.kmtSenseTree:
							case (int)MappingTypes.kmtEntryOrSenseAsymmetricPair:
							case (int)MappingTypes.kmtEntryOrSenseTree:
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
					}
				}
				else if (field.ID == "sub")
				{
					lblFunction.Text = LexTextControls.ksComplexFormType;
					// fill the comboBox with the names of the Complex Form objects
					foreach (var let in m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities)
					{
						AddAbbrAndNameInfo(let.Abbreviation, let.Name, null, null);
					}
				}

				// now select the one with the correct abbreviation
				var pos = -1;
				if (m_refFuncString.Length > 0)
				{
					pos = cbFunction.FindString(m_refFuncString);
				}

				cbFunction.SelectedIndex = pos >= 0 ? pos : 0;
				cbFunction.Text = cbFunction.SelectedItem as string;
			}
			// The radio buttons for abbr and Name are set when initialized - so don't reset them
		}

		private void chkbxAutoField_CheckedChanged(object sender, EventArgs e)
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
			// default to not enabled
			var enable = chkbxExclude.Checked || chkbxAutoField.Checked || tvDestination.SelectedNode != null;

			if (btnOK.Enabled != enable)
			{
				btnOK.Enabled = enable;
			}
		}

		private void cbFunction_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cbFunction.SelectedIndex >= 0)
			{
				m_refFuncString = cbFunction.SelectedItem as string;
			}
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
		private void InitBottomPanel()
		{

			if (m_browser.Handle != IntPtr.Zero)
			{
				var uri = new Uri(m_sHelpHtm);
				m_browser.Navigate(uri.AbsoluteUri);
			}

			// init transform used in help panel
			m_xslShowInfoTransform = new XslCompiledTransform();
			var sXsltFile = Path.Combine(FwDirectoryFinder.CodeDirectory, @"Language Explorer/Import/ImportFieldsHelpToHtml.xsl");
			m_xslShowInfoTransform.Load(sXsltFile);
			// init XmlDoc, too
			m_xmlShowInfoDoc = new XmlDocument();
		}
		private void ShowInfo(LexImportField field)
		{
			var element = field.Element;
			var tempfile = Path.Combine(Path.GetTempPath(), "temphelp.htm");
			using (var w = new StreamWriter(tempfile, false))
			using (var tw = new XmlTextWriter(w))
			{
				m_xmlShowInfoDoc.LoadXml(element.GetOuterXml()); // N.B. LoadXml requires UTF-16 or UCS-2 encodings
				m_xslShowInfoTransform.Transform(m_xmlShowInfoDoc, tw);
			}
			var uri = new Uri(tempfile);
			m_browser.Navigate(uri.AbsoluteUri);
		}

		private void btnShowInfo_Click(object sender, EventArgs e)
		{
			var sz = new Size(ClientSize.Width, ClientSize.Height);
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
			IPublisher publisher = null;
			var wiz = LexImportWizard.Wizard();
			if (wiz != null)
			{
				publisher = wiz.Publisher;
			}
			if (publisher == null)
			{
				// See LT-9100 and LT-9266.  Apparently this condition can happen.
				MessageBox.Show(LexTextControls.ksCannotSoTryAgain, LexTextControls.ksInternalProblem, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			publisher.Publish("AddCustomField", null);

			// The above call can cause the Mediator to 'go away', so check it and
			// restore the member variable for everyone else who may be surprised
			// that it is gone ... or not

			// At this point there could be custom fields added or removed, so we have to
			// recalculate the customfields and populate the TVcontrol based on the currently
			// defined custom fields.

			bool customFieldsChanged;
			CustomFields = wiz.ReadCustomFieldsFromDB(out customFieldsChanged);

			// if the custom fields have changed any, then update the display with the changes
			if (customFieldsChanged)
			{
				AddCustomFieldsToPossibleFields();
			}
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
				var leaves = new TreeNode[classNameNode.Nodes.Count];
				classNameNode.Nodes.CopyTo(leaves, 0);
				foreach (var leafNode in leaves)
				{
					if (leafNode.Tag is LexImportCustomField)
					{
						classNameNode.Nodes.Remove(leafNode);
					}
				}

				// Now add any custom fields for this class
				var className = classNameNode.Text.Trim('(', ')');
				if (CustomFields.FieldsForClass(className) == null)
				{
					continue;
				}

				foreach (LexImportField field in CustomFields.FieldsForClass(className))
				{
					var cnode = new TreeNode(field.UIName + " (Custom Field)")
					{
						Tag = field
					};
					classNameNode.Nodes.Add(cnode);
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
				var funcText = cbFunction.Text;
				// add case to handle when the user hasn't entered text or selected any items in the drop down list.
				if (funcText.Length == 0)
				{
					var labelText = lblFunction.Text.TrimEnd(' ', ':');
					MessageBox.Show("You must select a '"+labelText+"' item or enter a new one to continue.", "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Stop);
					cbFunction.Focus();
					return;
				}
				if (!cbFunction.Items.Contains(funcText))
				{
					// found case where the user has entered their own text and want to add it to the proper list
					var tn = tvDestination.SelectedNode;
					var field = tn.Tag as LexImportField;
					if (field != null && field.IsRef)
					{
						var entryTypefactory = m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
						ICmPossibility newType;
						ILcmOwningSequence<ICmPossibility> owningSeq;
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
								var userWs = m_cache.WritingSystemFactory.UserWs;
								newType.Name.set_String(userWs, TsStringUtils.MakeString(funcText, userWs));
								newType.Description.set_String(userWs, TsStringUtils.MakeString(description, userWs));
							});
					}
				}
			}

			// Allows accessing FWDestID, FWDestinationClass, IsCustomField properties reliably
			// after dialog is closed.
			m_StoredTreeNode = tvDestination.SelectedNode;
			DialogResult = DialogResult.OK;
		}
	}
}
