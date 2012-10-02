using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;

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
		private Button m_btnOk;
		private Button m_btnCancel;
		private Common.Widgets.FwTextBox m_txtMorphs;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container m_components;
		private Label m_lblWord;
		private GroupBox m_groupBox2BreakCharacters;
		private GroupBox m_groupBox1Examples;
		private Label m_lblHelp2Example1;
		private Label m_lblHelp2Example2;
		private Label m_lblBreakPrefixExample;
		private Label m_lblBreakSuffixExample;
		private Label m_lblBreakInfixExample;
		private Label m_lblBreakSimulfixExample;
		private Label m_lblBreakEncliticExample;
		private Label m_lblBreakProcliticExample;
		private Label m_lblBreakSuprafixExample;
		private Label m_lblBreakInfixLabel;
		private Label m_lblBreakSuffixLabel;
		private Label m_lblBreakPrefixLabel;
		private Label m_lblBreakSimulfixLabel;
		private Label m_lblBreakEncliticLabel;
		private Label m_lblBreakProcliticLabel;
		private Label m_lblBreakSuprafixtLabel;
		private Label m_lblBreakStemExample;
		private Label m_lblBreakBoundStemExample;
		private Label m_lblBreakStemLabel;
		private Label m_lblBreakBoundStemLabel;
		private Label m_label1;
		private Button m_buttonHelp;

		private string m_sMorphs;

		private const string ksHelpTopic = "khtpEditMorphBreaks";
		private Button m_morphBreakHelper;
		private readonly HelpProvider m_helpProvider;

		private MorphBreakHelperMenu m_morphBreakContextMenu;
		private readonly IHelpTopicProvider m_helpTopicProvider;

		public EditMorphBreaksDlg(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);
			AccessibleName = GetType().Name;

			if (!Application.RenderWithVisualStyles)
				m_txtMorphs.BorderStyle = BorderStyle.FixedSingle;

			m_helpProvider = new HelpProvider {HelpNamespace = helpTopicProvider.HelpFile};
			m_helpProvider.SetHelpKeyword(this, helpTopicProvider.GetHelpString(ksHelpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		/// <summary>
		/// This sets the original wordform and morph-broken word into the dialog.
		/// </summary>
		public void Initialize(ITsString tssWord, string sMorphs, ILgWritingSystemFactory wsf,
			FdoCache cache, StringTable stringTable, IVwStylesheet stylesheet)
		{
			CheckDisposed();

			Debug.Assert(tssWord != null);
			Debug.Assert(wsf != null);
			ITsTextProps ttp = tssWord.get_Properties(0);
			Debug.Assert(ttp != null);
			int var;
			int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Debug.Assert(ws != 0);
			ILgWritingSystem wsVern = wsf.get_EngineOrNull(ws);
			Debug.Assert(wsVern != null);

			m_txtMorphs.WritingSystemFactory = wsf;
			m_txtMorphs.WritingSystemCode = ws;
			m_txtMorphs.Text = sMorphs;
			m_sMorphs = sMorphs;

			// Fix the help strings to use the actual MorphType markers.
			IMoMorphType mmtStem;
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			IMoMorphType mmtBoundStem;
			IMoMorphType mmtProclitic;
			IMoMorphType mmtEnclitic;
			IMoMorphType mmtSimulfix;
			IMoMorphType mmtSuprafix ;
			var morphTypeRepo = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			morphTypeRepo.GetMajorMorphTypes(out mmtStem, out mmtPrefix, out mmtSuffix, out mmtInfix,
				out mmtBoundStem, out mmtProclitic, out mmtEnclitic, out mmtSimulfix, out mmtSuprafix);
			// Format the labels according to the MoMorphType Prefix/Postfix values.
			string sExample1 = stringTable.GetString("EditMorphBreaks-Example1", "DialogStrings");
			string sExample2 = stringTable.GetString("EditMorphBreaks-Example2", "DialogStrings");
			string sStemExample = stringTable.GetString("EditMorphBreaks-stemExample", "DialogStrings");
			string sAffixExample = stringTable.GetString("EditMorphBreaks-affixExample", "DialogStrings");
			m_lblHelp2Example1.Text = String.Format(sExample1, mmtStem.Prefix ?? "", mmtStem.Postfix ?? "");
			m_lblHelp2Example2.Text = String.Format(sExample2, mmtSuffix.Prefix ?? "", mmtSuffix.Postfix ?? "");
			m_lblBreakStemExample.Text = String.Format(sStemExample, mmtStem.Prefix ?? "", mmtStem.Postfix ?? "");
			m_lblBreakBoundStemExample.Text = String.Format(sStemExample, mmtBoundStem.Prefix ?? "", mmtBoundStem.Postfix ?? "");
			m_lblBreakPrefixExample.Text = String.Format(sAffixExample,
				mmtPrefix.Prefix == null ? "" : " " + mmtPrefix.Prefix,
				mmtPrefix.Postfix == null ? "" : mmtPrefix.Postfix + " ");
			m_lblBreakSuffixExample.Text = String.Format(sAffixExample,
				mmtSuffix.Prefix == null ? "" : " " + mmtSuffix.Prefix,
				mmtSuffix.Postfix == null ? "" : mmtSuffix.Postfix + " ");
			m_lblBreakInfixExample.Text = String.Format(sAffixExample,
				mmtInfix.Prefix == null ? "" : " " + mmtInfix.Prefix,
				mmtInfix.Postfix == null ? "" : mmtInfix.Postfix + " ");
			m_lblBreakProcliticExample.Text = String.Format(sAffixExample,
				mmtProclitic.Prefix == null ? "" : " " + mmtProclitic.Prefix,
				mmtProclitic.Postfix == null ? "" : mmtProclitic.Postfix + " ");
			m_lblBreakEncliticExample.Text = String.Format(sAffixExample,
				mmtEnclitic.Prefix == null ? "" : " " + mmtEnclitic.Prefix,
				mmtEnclitic.Postfix == null ? "" : mmtEnclitic.Postfix + " ");
			m_lblBreakSimulfixExample.Text = String.Format(sAffixExample,
				mmtSimulfix.Prefix == null ? "" : " " + mmtSimulfix.Prefix,
				mmtSimulfix.Postfix == null ? "" : mmtSimulfix.Postfix + " ");
			m_lblBreakSuprafixExample.Text = String.Format(sAffixExample,
				mmtSuprafix.Prefix == null ? "" : " " + mmtSuprafix.Prefix,
				mmtSuprafix.Postfix == null ? "" : mmtSuprafix.Postfix + " ");

			m_morphBreakContextMenu = new MorphBreakHelperMenu(m_txtMorphs, m_helpTopicProvider, cache, stringTable);
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(m_components != null)
				{
					m_components.Dispose();
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
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_txtMorphs = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_lblWord = new System.Windows.Forms.Label();
			this.m_groupBox2BreakCharacters = new System.Windows.Forms.GroupBox();
			this.m_lblBreakSuprafixtLabel = new System.Windows.Forms.Label();
			this.m_lblBreakSimulfixLabel = new System.Windows.Forms.Label();
			this.m_lblBreakBoundStemLabel = new System.Windows.Forms.Label();
			this.m_lblBreakEncliticLabel = new System.Windows.Forms.Label();
			this.m_lblBreakProcliticLabel = new System.Windows.Forms.Label();
			this.m_lblBreakInfixLabel = new System.Windows.Forms.Label();
			this.m_lblBreakSuprafixExample = new System.Windows.Forms.Label();
			this.m_lblBreakSimulfixExample = new System.Windows.Forms.Label();
			this.m_lblBreakEncliticExample = new System.Windows.Forms.Label();
			this.m_lblBreakProcliticExample = new System.Windows.Forms.Label();
			this.m_lblBreakBoundStemExample = new System.Windows.Forms.Label();
			this.m_lblBreakInfixExample = new System.Windows.Forms.Label();
			this.m_lblBreakSuffixExample = new System.Windows.Forms.Label();
			this.m_lblBreakPrefixExample = new System.Windows.Forms.Label();
			this.m_lblBreakStemExample = new System.Windows.Forms.Label();
			this.m_lblBreakStemLabel = new System.Windows.Forms.Label();
			this.m_lblBreakSuffixLabel = new System.Windows.Forms.Label();
			this.m_lblBreakPrefixLabel = new System.Windows.Forms.Label();
			this.m_groupBox1Examples = new System.Windows.Forms.GroupBox();
			this.m_lblHelp2Example1 = new System.Windows.Forms.Label();
			this.m_lblHelp2Example2 = new System.Windows.Forms.Label();
			this.m_label1 = new System.Windows.Forms.Label();
			this.m_buttonHelp = new System.Windows.Forms.Button();
			this.m_morphBreakHelper = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_txtMorphs)).BeginInit();
			this.m_groupBox2BreakCharacters.SuspendLayout();
			this.m_groupBox1Examples.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.Click += new System.EventHandler(this.MBtnOkClick);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.Click += new System.EventHandler(this.MBtnCancelClick);
			//
			// m_txtMorphs
			//
			this.m_txtMorphs.AdjustStringHeight = true;
			resources.ApplyResources(this.m_txtMorphs, "m_txtMorphs");
			this.m_txtMorphs.BackColor = System.Drawing.SystemColors.Window;
			this.m_txtMorphs.controlID = null;
			this.m_txtMorphs.Name = "m_txtMorphs";
			this.m_txtMorphs.SelectionLength = 0;
			this.m_txtMorphs.SelectionStart = 0;
			//
			// lblWord
			//
			resources.ApplyResources(this.m_lblWord, "m_lblWord");
			this.m_lblWord.Name = "m_lblWord";
			//
			// groupBox2_BreakCharacters
			//
			resources.ApplyResources(this.m_groupBox2BreakCharacters, "m_groupBox2BreakCharacters");
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakSuprafixtLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakSimulfixLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakBoundStemLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakEncliticLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakProcliticLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakInfixLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakSuprafixExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakSimulfixExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakEncliticExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakProcliticExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakBoundStemExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakInfixExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakSuffixExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakPrefixExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakStemExample);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakStemLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakSuffixLabel);
			this.m_groupBox2BreakCharacters.Controls.Add(this.m_lblBreakPrefixLabel);
			this.m_groupBox2BreakCharacters.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_groupBox2BreakCharacters.Name = "m_groupBox2BreakCharacters";
			this.m_groupBox2BreakCharacters.TabStop = false;
			//
			// lblBreak_suprafixtLabel
			//
			resources.ApplyResources(this.m_lblBreakSuprafixtLabel, "m_lblBreakSuprafixtLabel");
			this.m_lblBreakSuprafixtLabel.Name = "m_lblBreakSuprafixtLabel";
			//
			// lblBreak_simulfixLabel
			//
			resources.ApplyResources(this.m_lblBreakSimulfixLabel, "m_lblBreakSimulfixLabel");
			this.m_lblBreakSimulfixLabel.Name = "m_lblBreakSimulfixLabel";
			//
			// lblBreak_boundStemLabel
			//
			resources.ApplyResources(this.m_lblBreakBoundStemLabel, "m_lblBreakBoundStemLabel");
			this.m_lblBreakBoundStemLabel.Name = "m_lblBreakBoundStemLabel";
			//
			// lblBreak_encliticLabel
			//
			resources.ApplyResources(this.m_lblBreakEncliticLabel, "m_lblBreakEncliticLabel");
			this.m_lblBreakEncliticLabel.Name = "m_lblBreakEncliticLabel";
			//
			// lblBreak_procliticLabel
			//
			resources.ApplyResources(this.m_lblBreakProcliticLabel, "m_lblBreakProcliticLabel");
			this.m_lblBreakProcliticLabel.Name = "m_lblBreakProcliticLabel";
			//
			// lblBreak_infixLabel
			//
			resources.ApplyResources(this.m_lblBreakInfixLabel, "m_lblBreakInfixLabel");
			this.m_lblBreakInfixLabel.Name = "m_lblBreakInfixLabel";
			//
			// lblBreak_suprafixExample
			//
			resources.ApplyResources(this.m_lblBreakSuprafixExample, "m_lblBreakSuprafixExample");
			this.m_lblBreakSuprafixExample.Name = "m_lblBreakSuprafixExample";
			//
			// lblBreak_simulfixExample
			//
			resources.ApplyResources(this.m_lblBreakSimulfixExample, "m_lblBreakSimulfixExample");
			this.m_lblBreakSimulfixExample.Name = "m_lblBreakSimulfixExample";
			//
			// lblBreak_encliticExample
			//
			resources.ApplyResources(this.m_lblBreakEncliticExample, "m_lblBreakEncliticExample");
			this.m_lblBreakEncliticExample.Name = "m_lblBreakEncliticExample";
			//
			// lblBreak_procliticExample
			//
			resources.ApplyResources(this.m_lblBreakProcliticExample, "m_lblBreakProcliticExample");
			this.m_lblBreakProcliticExample.Name = "m_lblBreakProcliticExample";
			//
			// lblBreak_boundStemExample
			//
			resources.ApplyResources(this.m_lblBreakBoundStemExample, "m_lblBreakBoundStemExample");
			this.m_lblBreakBoundStemExample.Name = "m_lblBreakBoundStemExample";
			//
			// lblBreak_infixExample
			//
			resources.ApplyResources(this.m_lblBreakInfixExample, "m_lblBreakInfixExample");
			this.m_lblBreakInfixExample.Name = "m_lblBreakInfixExample";
			//
			// lblBreak_suffixExample
			//
			resources.ApplyResources(this.m_lblBreakSuffixExample, "m_lblBreakSuffixExample");
			this.m_lblBreakSuffixExample.Name = "m_lblBreakSuffixExample";
			//
			// lblBreak_prefixExample
			//
			resources.ApplyResources(this.m_lblBreakPrefixExample, "m_lblBreakPrefixExample");
			this.m_lblBreakPrefixExample.Name = "m_lblBreakPrefixExample";
			//
			// lblBreak_stemExample
			//
			resources.ApplyResources(this.m_lblBreakStemExample, "m_lblBreakStemExample");
			this.m_lblBreakStemExample.Name = "m_lblBreakStemExample";
			//
			// lblBreak_stemLabel
			//
			resources.ApplyResources(this.m_lblBreakStemLabel, "m_lblBreakStemLabel");
			this.m_lblBreakStemLabel.Name = "m_lblBreakStemLabel";
			//
			// lblBreak_suffixLabel
			//
			resources.ApplyResources(this.m_lblBreakSuffixLabel, "m_lblBreakSuffixLabel");
			this.m_lblBreakSuffixLabel.Name = "m_lblBreakSuffixLabel";
			//
			// lblBreak_prefixLabel
			//
			resources.ApplyResources(this.m_lblBreakPrefixLabel, "m_lblBreakPrefixLabel");
			this.m_lblBreakPrefixLabel.Name = "m_lblBreakPrefixLabel";
			//
			// groupBox1_Examples
			//
			this.m_groupBox1Examples.Controls.Add(this.m_lblHelp2Example1);
			this.m_groupBox1Examples.Controls.Add(this.m_lblHelp2Example2);
			resources.ApplyResources(this.m_groupBox1Examples, "m_groupBox1Examples");
			this.m_groupBox1Examples.Name = "m_groupBox1Examples";
			this.m_groupBox1Examples.TabStop = false;
			//
			// lblHelp2_Example1
			//
			resources.ApplyResources(this.m_lblHelp2Example1, "m_lblHelp2Example1");
			this.m_lblHelp2Example1.Name = "m_lblHelp2Example1";
			//
			// lblHelp2_Example2
			//
			resources.ApplyResources(this.m_lblHelp2Example2, "m_lblHelp2Example2");
			this.m_lblHelp2Example2.Name = "m_lblHelp2Example2";
			//
			// label1
			//
			resources.ApplyResources(this.m_label1, "m_label1");
			this.m_label1.Name = "m_label1";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.m_buttonHelp, "m_buttonHelp");
			this.m_buttonHelp.Name = "m_buttonHelp";
			this.m_buttonHelp.Click += new System.EventHandler(this.ButtonHelpClick);
			//
			// morphBreakHelper
			//
			resources.ApplyResources(this.m_morphBreakHelper, "m_morphBreakHelper");
			this.m_morphBreakHelper.Name = "m_morphBreakHelper";
			this.m_morphBreakHelper.Click += new System.EventHandler(this.MorphBreakHelperClick);
			//
			// EditMorphBreaksDlg
			//
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_morphBreakHelper);
			this.Controls.Add(this.m_buttonHelp);
			this.Controls.Add(this.m_label1);
			this.Controls.Add(this.m_groupBox1Examples);
			this.Controls.Add(this.m_groupBox2BreakCharacters);
			this.Controls.Add(this.m_lblWord);
			this.Controls.Add(this.m_txtMorphs);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditMorphBreaksDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(this.m_txtMorphs)).EndInit();
			this.m_groupBox2BreakCharacters.ResumeLayout(false);
			this.m_groupBox1Examples.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		void MorphBreakHelperClick(object sender, EventArgs e)
		{
			m_morphBreakContextMenu.Show(m_morphBreakHelper, new System.Drawing.Point(m_morphBreakHelper.Width,0));
		}

		private void MBtnOkClick(object sender, EventArgs e)
		{
			DialogResult =  DialogResult.OK;
			m_sMorphs = m_txtMorphs.Text;
			Close();
		}

		private void MBtnCancelClick(object sender, EventArgs e)
		{
			DialogResult =  DialogResult.Cancel;
			Close();
		}

		private void ButtonHelpClick(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, ksHelpTopic);
		}
	}
}
