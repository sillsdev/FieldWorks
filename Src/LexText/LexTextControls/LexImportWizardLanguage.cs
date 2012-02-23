using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs;
using SilEncConverters40;	// for the encoding converters
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using XCore;


namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for LexImportWizardLanguage.
	/// </summary>
	public class LexImportWizardLanguage : Form, IFWDisposable
	{
		private Label lblComment;
		private Label lblLangDesc;
		private TextBox tbLangDesc;
		private FwOverrideComboBox cbEC;
		private Label lblEC;
		private FwOverrideComboBox cbWS;
		private Label lblWS;
		private Button btnAddEC;
		private Button btnOK;
		private Button btnCancel;
		private GroupBox groupBox1;
		private IContainer components;

		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private IVwStylesheet m_stylesheet;
		// class to contain 'ws' information to be put in combo boxes
		class WsInfo
		{
			private readonly string m_name;
			private readonly string m_id;
			private readonly string m_map;

			public WsInfo()
			{
				m_name = LexTextControls.ksIgnore;
			}

			public WsInfo(string name, string id, string map)
			{
				m_name = name;
				m_id = id;
				m_map = map;
			}

			public string Name
			{
				get { return m_name; }
			}

			public string Id
			{
				get { return m_id; }
			}

			public string KEY
			{
				get { return Id; }
			}

			public string Map
			{
				get { return m_map; }
			}

			public override string ToString()
			{
				return Name;
			}
		}

		private readonly Dictionary<string, WsInfo> m_wsInfo;	// hash of wsInfo
		private string m_blankEC = Sfm2Xml.STATICS.AlreadyInUnicode;
//		private WsInfo m_wsiDefault;

		private string m_LangDesc;
		private string m_wsName;
		private string m_encConverter;
		private bool m_AddUsage;
		private bool m_LinguaLinksImport;
		private Button buttonHelp; // (Bev) marks when a LL import is in progress
		private Hashtable m_existingLangDescriptors;

		private string m_helpTopic;
		private AddWritingSystemButton btnAddWS;
		private HelpProvider helpProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexImportWizardLanguage"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LexImportWizardLanguage()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			m_wsInfo = new Dictionary<string, WsInfo>();
			m_LangDesc = m_wsName = m_encConverter = "";
			m_AddUsage = true;	// this is an "Add" use of the dlg by default
			btnOK.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexImportWizardLanguage"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="existingLangDesc">The existing lang descriptors with currently
		/// defined values.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public LexImportWizardLanguage(FdoCache cache, Hashtable existingLangDesc,
			IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet) : this()
		{
			m_existingLangDescriptors = existingLangDesc; //
			m_cache = cache;
			m_app = app;
			m_stylesheet = stylesheet;
			m_LinguaLinksImport = false; // (Bev) this is an SFM import
			setupHelp(helpTopicProvider);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexImportWizardLanguage"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public LexImportWizardLanguage(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IApp app, IVwStylesheet stylesheet) : this(cache, new Hashtable(), helpTopicProvider, app, stylesheet)
		{
			m_LinguaLinksImport = true;
			tbLangDesc.ReadOnly = true; // don't let them change the language name
			tbLangDesc.Enabled = false;
		}

		private void setupHelp(IHelpTopicProvider helpTopicProvider)
		{
			switch(m_LinguaLinksImport)
			{
				case true:
					m_helpTopic = "khtpLinguaLinksImportLanguageMapping";
					break;
				case false:
					m_helpTopic = "khtpImportSFMLanguageMapping";
					break;
			}

			m_helpTopicProvider = helpTopicProvider;
			if (m_helpTopic != null && m_helpTopicProvider != null) // FwApp.App could be null during tests
			{
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		public void LangToModify(string langDescriptor, string wsName, string encConverter)
		{
			CheckDisposed();

			m_LangDesc = langDescriptor;
			m_wsName = wsName;
			m_encConverter = encConverter;
			m_AddUsage = false;	// modify case
		}

		public void GetCurrentLangInfo(out string langDescriptor, out string wsName,
			out string encConverter, out string wsId)
		{
			CheckDisposed();

			langDescriptor = tbLangDesc.Text;
			wsName = cbWS.SelectedItem.ToString();
			encConverter = cbEC.SelectedItem.ToString();
			wsId = ((WsInfo) cbWS.SelectedItem).Id;
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
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexImportWizardLanguage));
			this.lblComment = new System.Windows.Forms.Label();
			this.lblLangDesc = new System.Windows.Forms.Label();
			this.tbLangDesc = new System.Windows.Forms.TextBox();
			this.cbEC = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblEC = new System.Windows.Forms.Label();
			this.cbWS = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblWS = new System.Windows.Forms.Label();
			this.btnAddEC = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.btnAddWS = new SIL.FieldWorks.LexText.Controls.AddWritingSystemButton(this.components);
			this.SuspendLayout();
			//
			// lblComment
			//
			resources.ApplyResources(this.lblComment, "lblComment");
			this.lblComment.Name = "lblComment";
			//
			// lblLangDesc
			//
			resources.ApplyResources(this.lblLangDesc, "lblLangDesc");
			this.lblLangDesc.Name = "lblLangDesc";
			//
			// tbLangDesc
			//
			resources.ApplyResources(this.tbLangDesc, "tbLangDesc");
			this.tbLangDesc.Name = "tbLangDesc";
			this.tbLangDesc.TextChanged += new System.EventHandler(this.tbLangDesc_TextChanged);
			//
			// cbEC
			//
			this.cbEC.AllowSpaceInEditBox = false;
			this.cbEC.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbEC, "cbEC");
			this.cbEC.Name = "cbEC";
			this.cbEC.Sorted = true;
			//
			// lblEC
			//
			resources.ApplyResources(this.lblEC, "lblEC");
			this.lblEC.Name = "lblEC";
			//
			// cbWS
			//
			this.cbWS.AllowSpaceInEditBox = false;
			this.cbWS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbWS, "cbWS");
			this.cbWS.Name = "cbWS";
			this.cbWS.Sorted = true;
			this.cbWS.SelectedValueChanged += new System.EventHandler(this.cbWS_SelectedValueChanged);
			//
			// lblWS
			//
			resources.ApplyResources(this.lblWS, "lblWS");
			this.lblWS.Name = "lblWS";
			//
			// btnAddEC
			//
			resources.ApplyResources(this.btnAddEC, "btnAddEC");
			this.btnAddEC.Name = "btnAddEC";
			this.btnAddEC.Click += new System.EventHandler(this.btnAddEC_Click);
			//
			// btnOK
			//
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			//
			// groupBox1
			//
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// btnAddWS
			//
			resources.ApplyResources(this.btnAddWS, "btnAddWS");
			this.btnAddWS.Name = "btnAddWS";
			this.btnAddWS.UseVisualStyleBackColor = true;
			this.btnAddWS.WritingSystemAdded += new System.EventHandler(this.btnAddWS_WritingSystemAdded);
			//
			// LexImportWizardLanguage
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.btnAddWS);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnAddEC);
			this.Controls.Add(this.cbEC);
			this.Controls.Add(this.lblEC);
			this.Controls.Add(this.cbWS);
			this.Controls.Add(this.lblWS);
			this.Controls.Add(this.tbLangDesc);
			this.Controls.Add(this.lblLangDesc);
			this.Controls.Add(this.lblComment);
			this.Controls.Add(this.groupBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LexImportWizardLanguage";
			this.Load += new System.EventHandler(this.LexImportWizardLanguage_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void LexImportWizardLanguage_Load(object sender, EventArgs e)
		{
			// (Bev) modify a few labels
			if (m_LinguaLinksImport)
			{
				Text = LexTextControls.ksSpecifyFwWs;
				lblComment.Text = LexTextControls.ksSpecifyFwWsDescription;
				lblLangDesc.Text = LexTextControls.ksLanguageDefinition;
			}
			else
			{
				if (m_AddUsage)
					Text = LexTextControls.ksAddLangMapping;
				else
					Text = LexTextControls.ksModifyLangMapping;
			}

			tbLangDesc.Text = m_LangDesc;

			// initialize the 'ws' combo box and the AddWs button with the data from the DB
			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystemManager.LocalWritingSystems)
			{
				var wsi = new WsInfo(ws.DisplayLabel, ws.Id, ws.LegacyMapping);
				m_wsInfo.Add(wsi.KEY, wsi);
				cbWS.Items.Add(wsi);
			}

			cbWS.Sorted = false;
			var wsiIgnore = new WsInfo();
			cbWS.Items.Add(wsiIgnore);
			btnAddWS.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_cache.ServiceLocator.WritingSystemManager.LocalWritingSystems);

			// select the proper index if there is a valid writing system
			int index = 0;
			if (!string.IsNullOrEmpty(m_wsName))
			{
				index = cbWS.FindStringExact(m_wsName);
				if (index < 0)
					index = 0;
			}
			cbWS.SelectedIndex = index;

			LoadEncodingConverters();

			index = 0;
			if (!string.IsNullOrEmpty(m_encConverter))
			{
				index = cbEC.FindStringExact(m_encConverter);
				if (index < 0)
					index = 0;
			}
			cbEC.SelectedIndex = index;
		}

		/// <summary>
		/// update the 'encoding converters' combo box with the current values
		/// </summary>
		private void LoadEncodingConverters()
		{
			/// Added to make the list of encoding converters match the list that is given when
			/// the add new converter option is selected. (LT-2955)
			EncConverters encConv = new EncConverters();
			System.Collections.IDictionaryEnumerator de = encConv.GetEnumerator();
			cbEC.BeginUpdate();
			cbEC.Items.Clear();
			cbEC.Sorted = true;
			while (de.MoveNext())
			{
				string name = de.Key as string;
				if (name != null)
					cbEC.Items.Add(name);
			}
			cbEC.Sorted = false;
			cbEC.Items.Insert(0, m_blankEC);
			cbEC.EndUpdate();
		}

		private void btnAddWS_WritingSystemAdded(object sender, EventArgs e)
		{
			IWritingSystem ws = btnAddWS.NewWritingSystem;
			if (ws != null)
			{
				string mapName = ws.LegacyMapping;
				var wsi = new WsInfo(ws.DisplayLabel, ws.Id, mapName);
				m_wsInfo.Add(wsi.KEY, wsi);

				// now select it for the ws combo box
				int index = cbWS.Items.Add(wsi);
				cbWS.SelectedIndex = index;

				// now if there's an encoding converter for the ws, select it
				if (String.IsNullOrEmpty(mapName))
					index = cbEC.FindStringExact(m_blankEC);
				else
					index = cbEC.Items.Add(mapName);
				cbEC.SelectedIndex = index;
			}
		}

		private void btnAddEC_Click(object sender, EventArgs e)
		{
			try
			{
				string prevEC = cbEC.Text;
				using (AddCnvtrDlg dlg = new AddCnvtrDlg(m_helpTopicProvider, m_app, null,
					cbEC.Text, null, false))
				{
					dlg.ShowDialog();

					// Reload the converter list in the combo to reflect the changes.
					LoadEncodingConverters();

					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !String.IsNullOrEmpty(dlg.SelectedConverter))
						cbEC.SelectedItem = dlg.SelectedConverter;
					else if (cbEC.Items.Count > 0)
						cbEC.SelectedItem = prevEC; // preserve selection if possible
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format(LexTextControls.ksErrorAcessingEncodingConverters, ex.Message));
				return;
			}
		}

		private void tbLangDesc_TextChanged(object sender, System.EventArgs e)
		{
			bool enableOK = false;	// default to false
			string currentDesc = tbLangDesc.Text;
			if (currentDesc.Length > 0)
			{
				if (!m_existingLangDescriptors.ContainsKey(currentDesc))
					enableOK = true;
				else if (m_AddUsage == false)	// modify case
				{
					if (currentDesc == m_LangDesc)	// can be original value
						enableOK = true;
				}
			}
			btnOK.Enabled = enableOK;
		}

		private void cbWS_SelectedValueChanged(object sender, System.EventArgs e)
		{
			// (Bev) when the user changes the FW writing system, select a new encoding converter
			if (m_wsInfo != null)
			{
				// (Bev) pick up the current writing system
				WsInfo wsi = cbWS.SelectedItem as WsInfo;
				if (wsi != null)
				{
					if (wsi.Map != null)
					{
						// (Bev) this writing system has a default encoding converter
						cbEC.Text = wsi.Map;
					}
					else
					{
						// (Bev) defaults if the writing system is not associated with an encoding converter
						// REVIEW: SHOULD THIS NAME BE LOCALIZED?
						cbEC.Text = (m_LinguaLinksImport == true) ? "Windows1252<>Unicode" : m_blankEC;
					}
				}
			}
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}
	}
}
