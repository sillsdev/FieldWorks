using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.COMInterfaces;
using ECInterfaces;
using SilEncConverters31;	// for the encoding converters
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;


namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for LexImportWizardLanguage.
	/// </summary>
	public class LexImportWizardLanguage : Form, IFWDisposable
	{
		private System.Windows.Forms.Label lblComment;
		private System.Windows.Forms.Label lblLangDesc;
		private System.Windows.Forms.TextBox tbLangDesc;
		private FwOverrideComboBox cbEC;
		private System.Windows.Forms.Label lblEC;
		private FwOverrideComboBox cbWS;
		private System.Windows.Forms.Label lblWS;
		private System.Windows.Forms.Button btnAddWS;
		private System.Windows.Forms.Button btnAddEC;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.GroupBox groupBox1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private FDO.FdoCache m_cache;
		// class to contain 'ws' information to be put in combo boxes
		class WsInfo
		{
			private int m_ws;
			private string m_name;
			private string m_locale;
			private string m_map;

			public WsInfo()
			{
				m_name = LexTextControls.ksIgnore;
			}

			public WsInfo(int ws, string name, string locale, string map)
			{
				m_ws = ws;
				m_name = name;
				m_locale = locale;
				m_map = map;
			}

			public string Name
			{
				get { return m_name; }
			}

			public string Locale
			{
				get { return m_locale; }
			}

			public string KEY
			{
				get { return Locale; }
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

		private Hashtable m_wsInfo;	// hash of wsInfo
		private string m_blankEC = Sfm2Xml.STATICS.AlreadyInUnicode;
		private WsInfo m_wsiDefault;

		private string m_LangDesc;
		private string m_wsName;
		private string m_encConverter;
		private bool m_AddUsage;
		private bool m_LinguaLinksImport;
		private System.Windows.Forms.Button buttonHelp; // (Bev) marks when a LL import is in progress
		private Hashtable m_existingLangDescriptors;

		private string m_helpTopic;
		private System.Windows.Forms.HelpProvider helpProvider;

		public LexImportWizardLanguage(FdoCache cache, Hashtable existingLangDesc)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_existingLangDescriptors = existingLangDesc;	// currently defined values
			m_wsInfo = new Hashtable();
			m_cache = cache;
			m_LangDesc = m_wsName = m_encConverter = "";
			m_AddUsage = true;	// this is an "Add" use of the dlg by default
			m_LinguaLinksImport = false; // (Bev) this is an SFM import
			btnOK.Enabled = false;
			setupHelp();
		}

		public LexImportWizardLanguage(FdoCache cache)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_existingLangDescriptors = new Hashtable();	// not necessary for LL import
			m_wsInfo = new Hashtable();
			m_cache = cache;
			m_LangDesc = m_wsName = m_encConverter = "";
			m_AddUsage = true;	// this is an "Add" use of the dlg by default
			m_LinguaLinksImport = true; // (Bev) this is a LL import
			btnOK.Enabled = false;
			tbLangDesc.ReadOnly = true; // don't let them change the language name
			tbLangDesc.Enabled = false;
			setupHelp();
		}

		private void setupHelp()
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

			if(m_helpTopic != null && FwApp.App != null) // FwApp.App could be null during tests
			{
				this.helpProvider = new System.Windows.Forms.HelpProvider();
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
				this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(m_helpTopic, 0));
				this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
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
			out string encConverter, out string icu)
		{
			CheckDisposed();

			langDescriptor = tbLangDesc.Text;
			wsName = cbWS.SelectedItem.ToString();
			encConverter = cbEC.SelectedItem.ToString();
			icu = (cbWS.SelectedItem as WsInfo).Locale;
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
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexImportWizardLanguage));
			this.lblComment = new System.Windows.Forms.Label();
			this.lblLangDesc = new System.Windows.Forms.Label();
			this.tbLangDesc = new System.Windows.Forms.TextBox();
			this.cbEC = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblEC = new System.Windows.Forms.Label();
			this.cbWS = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblWS = new System.Windows.Forms.Label();
			this.btnAddWS = new System.Windows.Forms.Button();
			this.btnAddEC = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.buttonHelp = new System.Windows.Forms.Button();
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
			// btnAddWS
			//
			resources.ApplyResources(this.btnAddWS, "btnAddWS");
			this.btnAddWS.Name = "btnAddWS";
			this.btnAddWS.Paint += new System.Windows.Forms.PaintEventHandler(this.btnAddWS_Paint);
			this.btnAddWS.Click += new System.EventHandler(this.btnAddWS_Click);
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
			// LexImportWizardLanguage
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnAddEC);
			this.Controls.Add(this.btnAddWS);
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

		private ArrayList GetOtherNamedWritingSystems()
		{
			ArrayList result = new ArrayList();
			try
			{
				// Convert from Set to List, since the Set can't sort.
				List<NamedWritingSystem> al = new List<NamedWritingSystem>(m_cache.LangProject.GetAllNamedWritingSystems().ToArray());
				al.Sort();
				foreach (NamedWritingSystem namedWs in al)
				{
					// Make sure we only add language names (actually ICULocales in case any
					// language names happen to be identical) that aren't already in the list box.
					bool fFound = false;
					foreach (DictionaryEntry entry in m_wsInfo)
					{
						WsInfo wsi = entry.Value as WsInfo;
						if (wsi.Locale == namedWs.IcuLocale)
						{
							fFound = true;
							break;
						}
					}
					if (!fFound)
					{
						result.Add(namedWs);
					}
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}

			result.Sort();
			return result;
		}

		private void LexImportWizardLanguage_Load(object sender, System.EventArgs e)
		{
			// (Bev) modify a few labels
			if (m_LinguaLinksImport)
			{
				this.Text = LexTextControls.ksSpecifyFwWs;
				lblComment.Text = LexTextControls.ksSpecifyFwWsDescription;
				lblLangDesc.Text = LexTextControls.ksLanguageDefinition;
			}
			else
			{
				if (m_AddUsage)
					this.Text = LexTextControls.ksAddLangMapping;
				else
					this.Text = LexTextControls.ksModifyLangMapping;
			}

			tbLangDesc.Text = m_LangDesc;

			//getting name for a writing system given the ICU code.
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			int wsUser = wsf.UserWs;
			int wsVern = m_cache.DefaultVernWs;
			IWritingSystem ws = wsf.get_EngineOrNull(wsVern);
			m_wsiDefault = new WsInfo(wsVern, ws.get_UiName(wsVern), ws.IcuLocale, ws.LegacyMapping);

			// getting list of writing systems to populate a combo.
			int cws = wsf.NumberOfWs;
			using (ArrayPtr ptr = MarshalEx.ArrayToNative(cws, typeof(int)))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vws = (int[])MarshalEx.NativeToArray(ptr, cws, typeof(int));
				for (int iws = 0; iws < cws; iws++)
				{
					if (vws[iws] == 0)
						continue;
					ws = wsf.get_EngineOrNull(vws[iws]);
					if (ws == null)
						continue;
					string name = ws.get_UiName(wsUser);
					string icuLocal = ws.IcuLocale;
					string mapName = ws.LegacyMapping;
					WsInfo wsi = new WsInfo(vws[iws], name, icuLocal, mapName);
					m_wsInfo.Add(wsi.KEY, wsi);
				}
			}

			// initialize the 'ws' combo box with the data from the DB
			foreach (DictionaryEntry entry in m_wsInfo)
			{
				WsInfo wsi = entry.Value as WsInfo;
				cbWS.Items.Add(wsi);
			}
			cbWS.Sorted = false;
			WsInfo wsiIgnore = new WsInfo();
			cbWS.Items.Add(wsiIgnore);

			// select the proper index if there is a valid writhing system
			int index = 0;
			if (m_wsName != null && m_wsName != "")
			{
				index = cbWS.FindStringExact(m_wsName);
				if (index < 0)
					index = 0;
			}
			cbWS.SelectedIndex = index;

			LoadEncodingConverters();

			index = 0;
			if (m_encConverter != null && m_encConverter != "")
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

		private void btnAddWS_Vern(object sender, System.EventArgs e)
		{
			CommonAddWS(false, (sender as MenuItem));
		}
		private void btnAddWS_Anal(object sender, System.EventArgs e)
		{
			CommonAddWS(true, (sender as MenuItem));
		}

		private void CommonAddWS(bool isAnalysis, MenuItem selectedMI)
		{
			string icuLocal = "", mapName = "", wsName = "";
			bool addWs = true;

			if (selectedMI.Text == LexTextControls.ks_DefineNew_)
			{
				using (WritingSystemWizard wiz = new WritingSystemWizard())
				{
					wiz.Init(m_cache.LanguageWritingSystemFactoryAccessor, FwApp.App);
					DialogResult dr = (DialogResult)wiz.ShowDialog();
					if (dr == DialogResult.OK)
					{
						// The engine from the wizard isn't the real one, so it doesn't have an id.
						IWritingSystem wsEngine = wiz.WritingSystem();
						icuLocal = wsEngine.IcuLocale;
						mapName = wsEngine.LegacyMapping;
						wsName = wsEngine.LanguageName;
					}
					else
						addWs = false;
				}
			}
			else
			{
				NamedWritingSystem namedWs = selectedMI.Tag as NamedWritingSystem;
				ILgWritingSystem wsEngine = namedWs.GetLgWritingSystem(m_cache);
				icuLocal = wsEngine.ICULocale;
				mapName = wsEngine.LegacyMapping;
				wsName = wsEngine.Name.BestAnalysisAlternative.Text;
			}

			if (addWs)
			{
				int ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(icuLocal);
				// string wsName = ws.get_UiName(wsUser);
				WsInfo wsi = new WsInfo(ws, wsName, icuLocal, mapName);
				m_wsInfo.Add(wsi.KEY, wsi);

				// now select it for the ws combo box
				int index = cbWS.Items.Add(wsi);
				cbWS.SelectedIndex = index;

				// now if there's an encoding converter for the ws, select it
				if (mapName != null && mapName.Length > 0)
				{
					index = cbEC.Items.Add(mapName);
					cbEC.SelectedIndex = index;
				}
				else
				{
					index = cbEC.FindStringExact(m_blankEC);
					cbEC.SelectedIndex = index;
				}

				// now add the ws to the FDO list for it
				if (isAnalysis)
				{
					m_cache.LangProject.AnalysisWssRC.Add(ws);
					m_cache.LangProject.CurAnalysisWssRS.Append(ws);
				}
				else
				{
					m_cache.LangProject.VernWssRC.Add(ws);
					m_cache.LangProject.CurVernWssRS.Append(ws);
				}
			}
		}

		private void btnAddWS_Click(object sender, System.EventArgs e)
		{
			// show the menu to select which type of writing system to create
			ContextMenu addWs = new ContextMenu();

			// look like the "Add" button on the WS properties dlg
			ArrayList xmlWs = GetOtherNamedWritingSystems();
			MenuItem[] xmlWsV = new MenuItem[xmlWs.Count + 2];	// one for Vernacular
			MenuItem[] xmlWsA = new MenuItem[xmlWs.Count + 2];	// one for Analysis
			for (int i = 0; i < xmlWs.Count; i++)
			{
				NamedWritingSystem nws = xmlWs[i] as NamedWritingSystem;
				xmlWsV[i] = new MenuItem(nws.Name, new EventHandler(btnAddWS_Vern));
				xmlWsA[i] = new MenuItem(nws.Name, new EventHandler(btnAddWS_Anal));
				xmlWsV[i].Tag = nws;
				xmlWsA[i].Tag = nws;
			}
			xmlWsV[xmlWs.Count] = new MenuItem("-");
			xmlWsV[xmlWs.Count + 1] = new MenuItem(LexTextControls.ks_DefineNew_, new EventHandler(btnAddWS_Vern));
			xmlWsA[xmlWs.Count] = new MenuItem("-");
			xmlWsA[xmlWs.Count + 1] = new MenuItem(LexTextControls.ks_DefineNew_, new EventHandler(btnAddWS_Anal));

			// have to have seperate lists
			addWs.MenuItems.Add(LexTextControls.ks_VernacularWS, xmlWsV);
			addWs.MenuItems.Add(LexTextControls.ks_AnalysisWS, xmlWsA);

			addWs.Show(btnAddWS, new Point(0, btnAddWS.Height));
		}

		private void btnAddEC_Click(object sender, System.EventArgs e)
		{
			try
			{
				string prevEC = cbEC.Text;
				using (AddCnvtrDlg dlg = new AddCnvtrDlg(FwApp.App, null,
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

		private void btnAddWS_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			// add the 'drop down' arrow to the text
			Pen p = new Pen(System.Drawing.SystemColors.ControlText, 1);
			int x = btnAddWS.Width - 14;	// 7 wide at top, and 7 in from right boundry
			int y = btnAddWS.Height/2 - 2;	// up 2 past the mid point
			// 4 lines: len 7, len 5, len 3 and len 1
			e.Graphics.DrawLine(p, x  , y  , x+7  , y  );
			e.Graphics.DrawLine(p, x+1, y+1, x+1+5, y+1);
			e.Graphics.DrawLine(p, x+2, y+2, x+2+3, y+2);
			e.Graphics.DrawLine(p, x+3, y+3, x+3+1, y+3);
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
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopic);
		}
	}
}
