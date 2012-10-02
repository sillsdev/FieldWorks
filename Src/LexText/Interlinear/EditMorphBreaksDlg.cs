using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// The EditMorphBreaks dialog allows the user to edit the morph breaks in a word.  It is
	/// better for this purpose than just using the combobox's edit box because it has a much
	/// bigger edit box, and it displays some helpful (?) information to assist in marking the
	/// morpheme types.
	/// </summary>
	public class EditMorphBreaksDlg : Form, IFWDisposable
	{
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		// TODO: use Graphite-enabled fancy edit box for the morphs.
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_txtMorphs;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label lblWord;
		private System.Windows.Forms.GroupBox groupBox2_BreakCharacters;
		private System.Windows.Forms.GroupBox groupBox1_Examples;
		private System.Windows.Forms.Label lblHelp2_Example1;
		private System.Windows.Forms.Label lblHelp2_Example2;
		private System.Windows.Forms.Label lblBreak_prefixExample;
		private System.Windows.Forms.Label lblBreak_suffixExample;
		private System.Windows.Forms.Label lblBreak_infixExample;
		private System.Windows.Forms.Label lblBreak_simulfixExample;
		private System.Windows.Forms.Label lblBreak_encliticExample;
		private System.Windows.Forms.Label lblBreak_procliticExample;
		private System.Windows.Forms.Label lblBreak_suprafixExample;
		private System.Windows.Forms.Label lblBreak_infixLabel;
		private System.Windows.Forms.Label lblBreak_suffixLabel;
		private System.Windows.Forms.Label lblBreak_prefixLabel;
		private System.Windows.Forms.Label lblBreak_simulfixLabel;
		private System.Windows.Forms.Label lblBreak_encliticLabel;
		private System.Windows.Forms.Label lblBreak_procliticLabel;
		private System.Windows.Forms.Label lblBreak_suprafixtLabel;
		private System.Windows.Forms.Label lblBreak_stemExample;
		private System.Windows.Forms.Label lblBreak_boundStemExample;
		private System.Windows.Forms.Label lblBreak_stemLabel;
		private System.Windows.Forms.Label lblBreak_boundStemLabel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonHelp;

		private string m_sMorphs;

		private const string s_helpTopic = "khtpEditMorphBreaks";
		private Button morphBreakHelper;
		private System.Windows.Forms.HelpProvider helpProvider;

		private MorphBreakHelperMenu morphBreakContextMenu;

		public EditMorphBreaksDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			if (!Application.RenderWithVisualStyles)
				m_txtMorphs.BorderStyle = BorderStyle.FixedSingle;

			helpProvider = new System.Windows.Forms.HelpProvider();
			helpProvider.HelpNamespace = FwApp.App.HelpFile;
			helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
			helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
		}

		/// <summary>
		/// This sets the original wordform and morph-broken word into the dialog.
		/// </summary>
		/// <param name="sWord"></param>
		/// <param name="sMorphs"></param>
		public void Initialize(ITsString tssWord, string sMorphs, ILgWritingSystemFactory wsf,
			FdoCache cache, SIL.Utils.StringTable stringTable, IVwStylesheet stylesheet)
		{
			CheckDisposed();

			Debug.Assert(tssWord != null);
			Debug.Assert(wsf != null);
			ITsTextProps ttp = tssWord.get_Properties(0);
			Debug.Assert(ttp != null);
			int var;
			int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Debug.Assert(ws != 0);
			IWritingSystem wsVern = wsf.get_EngineOrNull(ws);
			Debug.Assert(wsVern != null);
			// The following is needed for Graphite fonts.
			string sFontVar = wsVern.FontVariation;
			if (sFontVar == null)
				sFontVar = "";

			this.m_txtMorphs.WritingSystemFactory = wsf;
			this.m_txtMorphs.WritingSystemCode = ws;
			this.m_txtMorphs.Text = sMorphs;
			m_sMorphs = sMorphs;

			// Fix the help strings to use the actual MorphType markers.
			IMoMorphType mmtStem = null;
			IMoMorphType mmtPrefix = null;
			IMoMorphType mmtSuffix = null;
			IMoMorphType mmtInfix = null;
			IMoMorphType mmtBoundStem = null;
			IMoMorphType mmtProclitic = null;
			IMoMorphType mmtEnclitic = null;
			IMoMorphType mmtSimulfix = null;
			IMoMorphType mmtSuprafix = null;
			MoMorphType.GetMajorMorphTypes(cache, out mmtStem, out mmtPrefix, out mmtSuffix, out mmtInfix,
				out mmtBoundStem, out mmtProclitic, out mmtEnclitic, out mmtSimulfix, out mmtSuprafix);
			// Format the labels according to the MoMorphType Prefix/Postfix values.
			string sExample1 = stringTable.GetString("EditMorphBreaks-Example1", "DialogStrings");
			string sExample2 = stringTable.GetString("EditMorphBreaks-Example2", "DialogStrings");
			string sStemExample = stringTable.GetString("EditMorphBreaks-stemExample", "DialogStrings");
			string sAffixExample = stringTable.GetString("EditMorphBreaks-affixExample", "DialogStrings");
			lblHelp2_Example1.Text = String.Format(sExample1,
				mmtStem.Prefix == null ? "" : mmtStem.Prefix,
				mmtStem.Postfix == null ? "" : mmtStem.Postfix);
			lblHelp2_Example2.Text = String.Format(sExample2,
				mmtSuffix.Prefix == null ? "" : mmtSuffix.Prefix,
				mmtSuffix.Postfix == null ? "" : mmtSuffix.Postfix);
			lblBreak_stemExample.Text = String.Format(sStemExample,
				mmtStem.Prefix == null ? "" : mmtStem.Prefix,
				mmtStem.Postfix == null ? "" : mmtStem.Postfix);
			lblBreak_boundStemExample.Text = String.Format(sStemExample,
				mmtBoundStem.Prefix == null ? "" : mmtBoundStem.Prefix,
				mmtBoundStem.Postfix == null ? "" : mmtBoundStem.Postfix);
			lblBreak_prefixExample.Text = String.Format(sAffixExample,
				mmtPrefix.Prefix == null ? "" : " " + mmtPrefix.Prefix,
				mmtPrefix.Postfix == null ? "" : mmtPrefix.Postfix + " ");
			lblBreak_suffixExample.Text = String.Format(sAffixExample,
				mmtSuffix.Prefix == null ? "" : " " + mmtSuffix.Prefix,
				mmtSuffix.Postfix == null ? "" : mmtSuffix.Postfix + " ");
			lblBreak_infixExample.Text = String.Format(sAffixExample,
				mmtInfix.Prefix == null ? "" : " " + mmtInfix.Prefix,
				mmtInfix.Postfix == null ? "" : mmtInfix.Postfix + " ");
			lblBreak_procliticExample.Text = String.Format(sAffixExample,
				mmtProclitic.Prefix == null ? "" : " " + mmtProclitic.Prefix,
				mmtProclitic.Postfix == null ? "" : mmtProclitic.Postfix + " ");
			lblBreak_encliticExample.Text = String.Format(sAffixExample,
				mmtEnclitic.Prefix == null ? "" : " " + mmtEnclitic.Prefix,
				mmtEnclitic.Postfix == null ? "" : mmtEnclitic.Postfix + " ");
			lblBreak_simulfixExample.Text = String.Format(sAffixExample,
				mmtSimulfix.Prefix == null ? "" : " " + mmtSimulfix.Prefix,
				mmtSimulfix.Postfix == null ? "" : mmtSimulfix.Postfix + " ");
			lblBreak_suprafixExample.Text = String.Format(sAffixExample,
				mmtSuprafix.Prefix == null ? "" : " " + mmtSuprafix.Prefix,
				mmtSuprafix.Postfix == null ? "" : mmtSuprafix.Postfix + " ");

			morphBreakContextMenu = new MorphBreakHelperMenu(m_txtMorphs, FwApp.App, cache, stringTable);
			m_txtMorphs.AdjustForStyleSheet(this, null, stylesheet);
		}

		/// <summary>
		/// Retrieve the morph-broken word.
		/// </summary>
		/// <returns>string containing the morph-broken word</returns>
		public string GetMorphs()
		{
			CheckDisposed();

			return m_sMorphs;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditMorphBreaksDlg));
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_txtMorphs = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.lblWord = new System.Windows.Forms.Label();
			this.groupBox2_BreakCharacters = new System.Windows.Forms.GroupBox();
			this.lblBreak_suprafixtLabel = new System.Windows.Forms.Label();
			this.lblBreak_simulfixLabel = new System.Windows.Forms.Label();
			this.lblBreak_boundStemLabel = new System.Windows.Forms.Label();
			this.lblBreak_encliticLabel = new System.Windows.Forms.Label();
			this.lblBreak_procliticLabel = new System.Windows.Forms.Label();
			this.lblBreak_infixLabel = new System.Windows.Forms.Label();
			this.lblBreak_suprafixExample = new System.Windows.Forms.Label();
			this.lblBreak_simulfixExample = new System.Windows.Forms.Label();
			this.lblBreak_encliticExample = new System.Windows.Forms.Label();
			this.lblBreak_procliticExample = new System.Windows.Forms.Label();
			this.lblBreak_boundStemExample = new System.Windows.Forms.Label();
			this.lblBreak_infixExample = new System.Windows.Forms.Label();
			this.lblBreak_suffixExample = new System.Windows.Forms.Label();
			this.lblBreak_prefixExample = new System.Windows.Forms.Label();
			this.lblBreak_stemExample = new System.Windows.Forms.Label();
			this.lblBreak_stemLabel = new System.Windows.Forms.Label();
			this.lblBreak_suffixLabel = new System.Windows.Forms.Label();
			this.lblBreak_prefixLabel = new System.Windows.Forms.Label();
			this.groupBox1_Examples = new System.Windows.Forms.GroupBox();
			this.lblHelp2_Example1 = new System.Windows.Forms.Label();
			this.lblHelp2_Example2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.morphBreakHelper = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_txtMorphs)).BeginInit();
			this.groupBox2_BreakCharacters.SuspendLayout();
			this.groupBox1_Examples.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_txtMorphs
			//
			this.m_txtMorphs.AdjustStringHeight = true;
			this.m_txtMorphs.AllowMultipleLines = false;
			resources.ApplyResources(this.m_txtMorphs, "m_txtMorphs");
			this.m_txtMorphs.BackColor = System.Drawing.SystemColors.Window;
			this.m_txtMorphs.controlID = null;
			this.m_txtMorphs.Name = "m_txtMorphs";
			this.m_txtMorphs.SelectionLength = 0;
			this.m_txtMorphs.SelectionStart = 0;
			this.m_txtMorphs.WritingSystemCode = 1;
			//
			// lblWord
			//
			resources.ApplyResources(this.lblWord, "lblWord");
			this.lblWord.Name = "lblWord";
			//
			// groupBox2_BreakCharacters
			//
			resources.ApplyResources(this.groupBox2_BreakCharacters, "groupBox2_BreakCharacters");
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_suprafixtLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_simulfixLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_boundStemLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_encliticLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_procliticLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_infixLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_suprafixExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_simulfixExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_encliticExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_procliticExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_boundStemExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_infixExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_suffixExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_prefixExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_stemExample);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_stemLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_suffixLabel);
			this.groupBox2_BreakCharacters.Controls.Add(this.lblBreak_prefixLabel);
			this.groupBox2_BreakCharacters.ForeColor = System.Drawing.SystemColors.ControlText;
			this.groupBox2_BreakCharacters.Name = "groupBox2_BreakCharacters";
			this.groupBox2_BreakCharacters.TabStop = false;
			//
			// lblBreak_suprafixtLabel
			//
			resources.ApplyResources(this.lblBreak_suprafixtLabel, "lblBreak_suprafixtLabel");
			this.lblBreak_suprafixtLabel.Name = "lblBreak_suprafixtLabel";
			//
			// lblBreak_simulfixLabel
			//
			resources.ApplyResources(this.lblBreak_simulfixLabel, "lblBreak_simulfixLabel");
			this.lblBreak_simulfixLabel.Name = "lblBreak_simulfixLabel";
			//
			// lblBreak_boundStemLabel
			//
			resources.ApplyResources(this.lblBreak_boundStemLabel, "lblBreak_boundStemLabel");
			this.lblBreak_boundStemLabel.Name = "lblBreak_boundStemLabel";
			//
			// lblBreak_encliticLabel
			//
			resources.ApplyResources(this.lblBreak_encliticLabel, "lblBreak_encliticLabel");
			this.lblBreak_encliticLabel.Name = "lblBreak_encliticLabel";
			//
			// lblBreak_procliticLabel
			//
			resources.ApplyResources(this.lblBreak_procliticLabel, "lblBreak_procliticLabel");
			this.lblBreak_procliticLabel.Name = "lblBreak_procliticLabel";
			//
			// lblBreak_infixLabel
			//
			resources.ApplyResources(this.lblBreak_infixLabel, "lblBreak_infixLabel");
			this.lblBreak_infixLabel.Name = "lblBreak_infixLabel";
			//
			// lblBreak_suprafixExample
			//
			resources.ApplyResources(this.lblBreak_suprafixExample, "lblBreak_suprafixExample");
			this.lblBreak_suprafixExample.Name = "lblBreak_suprafixExample";
			//
			// lblBreak_simulfixExample
			//
			resources.ApplyResources(this.lblBreak_simulfixExample, "lblBreak_simulfixExample");
			this.lblBreak_simulfixExample.Name = "lblBreak_simulfixExample";
			//
			// lblBreak_encliticExample
			//
			resources.ApplyResources(this.lblBreak_encliticExample, "lblBreak_encliticExample");
			this.lblBreak_encliticExample.Name = "lblBreak_encliticExample";
			//
			// lblBreak_procliticExample
			//
			resources.ApplyResources(this.lblBreak_procliticExample, "lblBreak_procliticExample");
			this.lblBreak_procliticExample.Name = "lblBreak_procliticExample";
			//
			// lblBreak_boundStemExample
			//
			resources.ApplyResources(this.lblBreak_boundStemExample, "lblBreak_boundStemExample");
			this.lblBreak_boundStemExample.Name = "lblBreak_boundStemExample";
			//
			// lblBreak_infixExample
			//
			resources.ApplyResources(this.lblBreak_infixExample, "lblBreak_infixExample");
			this.lblBreak_infixExample.Name = "lblBreak_infixExample";
			//
			// lblBreak_suffixExample
			//
			resources.ApplyResources(this.lblBreak_suffixExample, "lblBreak_suffixExample");
			this.lblBreak_suffixExample.Name = "lblBreak_suffixExample";
			//
			// lblBreak_prefixExample
			//
			resources.ApplyResources(this.lblBreak_prefixExample, "lblBreak_prefixExample");
			this.lblBreak_prefixExample.Name = "lblBreak_prefixExample";
			//
			// lblBreak_stemExample
			//
			resources.ApplyResources(this.lblBreak_stemExample, "lblBreak_stemExample");
			this.lblBreak_stemExample.Name = "lblBreak_stemExample";
			//
			// lblBreak_stemLabel
			//
			resources.ApplyResources(this.lblBreak_stemLabel, "lblBreak_stemLabel");
			this.lblBreak_stemLabel.Name = "lblBreak_stemLabel";
			//
			// lblBreak_suffixLabel
			//
			resources.ApplyResources(this.lblBreak_suffixLabel, "lblBreak_suffixLabel");
			this.lblBreak_suffixLabel.Name = "lblBreak_suffixLabel";
			//
			// lblBreak_prefixLabel
			//
			resources.ApplyResources(this.lblBreak_prefixLabel, "lblBreak_prefixLabel");
			this.lblBreak_prefixLabel.Name = "lblBreak_prefixLabel";
			//
			// groupBox1_Examples
			//
			this.groupBox1_Examples.Controls.Add(this.lblHelp2_Example1);
			this.groupBox1_Examples.Controls.Add(this.lblHelp2_Example2);
			resources.ApplyResources(this.groupBox1_Examples, "groupBox1_Examples");
			this.groupBox1_Examples.Name = "groupBox1_Examples";
			this.groupBox1_Examples.TabStop = false;
			//
			// lblHelp2_Example1
			//
			resources.ApplyResources(this.lblHelp2_Example1, "lblHelp2_Example1");
			this.lblHelp2_Example1.Name = "lblHelp2_Example1";
			//
			// lblHelp2_Example2
			//
			resources.ApplyResources(this.lblHelp2_Example2, "lblHelp2_Example2");
			this.lblHelp2_Example2.Name = "lblHelp2_Example2";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// morphBreakHelper
			//
			resources.ApplyResources(this.morphBreakHelper, "morphBreakHelper");
			this.morphBreakHelper.Name = "morphBreakHelper";
			this.morphBreakHelper.Click += new System.EventHandler(this.morphBreakHelper_Click);
			//
			// EditMorphBreaksDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.morphBreakHelper);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.groupBox1_Examples);
			this.Controls.Add(this.groupBox2_BreakCharacters);
			this.Controls.Add(this.lblWord);
			this.Controls.Add(this.m_txtMorphs);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditMorphBreaksDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(this.m_txtMorphs)).EndInit();
			this.groupBox2_BreakCharacters.ResumeLayout(false);
			this.groupBox1_Examples.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		void morphBreakHelper_Click(object sender, EventArgs e)
		{
			morphBreakContextMenu.Show(morphBreakHelper, new System.Drawing.Point(morphBreakHelper.Width,0));
		}

		private void m_btnOK_Click(object sender, System.EventArgs e)
		{
			this.DialogResult =  System.Windows.Forms.DialogResult.OK;
			m_sMorphs = this.m_txtMorphs.Text;
			this.Close();
		}

		private void m_btnCancel_Click(object sender, System.EventArgs e)
		{
			this.DialogResult =  System.Windows.Forms.DialogResult.Cancel;
			this.Close();
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}
	}
}
