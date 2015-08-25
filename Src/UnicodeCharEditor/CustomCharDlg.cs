// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CustomCharDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// This is a mutation of the old PUACharacterDlg.cs
// </remarks>

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary>
	/// Dialog for editing the properties of a Unicode character.
	/// </summary>
	public class CustomCharDlg : Form, IFWDisposable
	{
		# region member variables

		private System.ComponentModel.IContainer components;
		/// <summary></summary>
		protected TextBox m_txtUpperEquiv;
		/// <summary></summary>
		protected TextBox m_txtTitleEquiv;
		/// <summary></summary>
		protected TextBox m_txtLowerEquiv;
		/// <summary></summary>
		protected TextBox m_txtNumericValue;
		/// <summary></summary>
		protected TextBox m_txtCodepoint;
		/// <summary></summary>
		protected TextBox m_txtName;
		/// <summary></summary>
		protected TextBox m_txtDecomposition;
		/// <summary></summary>
		protected FwOverrideComboBox m_cbGeneralCategory;
		/// <summary></summary>
		protected FwOverrideComboBox m_cbCanonicalCombClass;
		/// <summary></summary>
		protected FwOverrideComboBox m_cbBidiClass;
		/// <summary>
		/// A working copy of the private use area character that this dialog box is modifying.
		/// </summary>
		protected PUACharacter m_puaChar;
		/// <summary>
		/// The private use area character that this dialog box is modifying.
		/// Don't edit the object that this refers to,
		/// merely redirect this to refer to the working copy when the user chooses to apply his changes
		/// </summary>
		private PUACharacter m_storedPuaChar;
		private Label m_lblUpperDisplay;
		private Label m_lblLowerDisplay;
		private Label m_lblTitleDisplay;
		private Label m_lblPUADisplay;
		private CheckBox m_chBidiMirrored;
		/// <summary></summary>
		protected FwOverrideComboBox m_cbCompatabilityDecomposition;
		private ToolTip m_toolTip;
		/// <summary></summary>
		protected FwOverrideComboBox m_cbNumericType;
		private Label m_lblMessageBottom;
		private Label m_lblMessageMiddle;
		private Label m_lblMessageTop;

		/// <summary>
		/// Error handling scheme
		/// </summary>
		private readonly ErrorMessageHandler m_errorMessageHandler;
		private Label m_lblDecompostionDisplay;

		private CharEditorWindow m_parentDialog;
		private HelpProvider m_helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;

		private string m_sHelpTopic;
		private Button m_btnHelp;
		private Button m_btnOK;
		private Label m_lblWarning;
		private readonly HelpProvider m_helpProvider2;
		private bool m_modifyMode;

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

		#region attributes

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the PUACharacter that this dialog is changing.
		/// </summary>
		/// <value>The PUA char.</value>
		/// ------------------------------------------------------------------------------------
		public PUACharacter PUAChar
		{
			get
			{
				CheckDisposed();
				return m_storedPuaChar;
			}
			set
			{
				CheckDisposed();

				m_storedPuaChar = value;
				m_puaChar = new PUACharacter(value);
			}
		}

		/// <summary>
		/// If <c>true</c> the dialog is being used to modify and existing PUACharacter.
		/// </summary>
		public bool Modify
		{
			get
			{
				CheckDisposed();

				return m_modifyMode;
			}
			set
			{
				CheckDisposed();

				m_modifyMode = value;
				if( value )
				{
					SetEnabledAll(m_txtCodepoint, true);
					SetEnableBasedOnGeneralCategory();
					m_txtCodepoint.Enabled = false;
					Text = Properties.Resources.kstidModifyPuaTitle;
				}
				else
					Text = Properties.Resources.kstidAddPuaTitle;
			}
		}

		/// <summary>
		/// Allows us to access information stored in the Writing System Properties Dialog
		/// </summary>
		public CharEditorWindow ParentDialog
		{
			get
			{
				CheckDisposed();

				return m_parentDialog;
			}

			set
			{
				CheckDisposed();

				m_parentDialog = value;
			}
		}
		#endregion

		#region construction/destruction

		/// <summary>
		/// Creates a new empty PUACharacterDlg
		/// </summary>
		public CustomCharDlg()
		{
			// Required for Windows Form Designer support
			InitializeComponent();

			m_helpProvider2 = new HelpProvider();

			//Initialize our label Association dictionary
			var labelAssociations = new Dictionary<TextBox, Label>
			{
				{m_txtCodepoint, m_lblMessageTop},
				{m_txtName, m_lblMessageTop},
				{m_txtUpperEquiv, m_lblMessageMiddle},
				{m_txtLowerEquiv, m_lblMessageMiddle},
				{m_txtTitleEquiv, m_lblMessageMiddle},
				{m_txtNumericValue, m_lblMessageMiddle},
				{m_txtDecomposition, m_lblMessageBottom}
			};

			// Add any constructor code after InitializeComponent call
			m_errorMessageHandler = new ErrorMessageHandler(labelAssociations, m_btnOK);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the dialog properties object for dialogs that are created.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void SetDialogProperties(IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_helpTopicProvider = helpTopicProvider;

			if (Text == Properties.Resources.kstidAddPuaTitle)
				m_sHelpTopic = "khtpWsAddPUAChar";
			else if (Text == Properties.Resources.kstidModifyPuaTitle)
				m_sHelpTopic = "khtpWsModifyPUAChar";
			else
				Debug.Assert(false, "Dialog must be set to Add or Modify (using the Modify property) before SetDialogProperties is called");

			if (m_helpTopicProvider != null)
			{
				m_helpProvider2.HelpNamespace = m_helpTopicProvider.HelpFile;
				m_helpProvider2.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_sHelpTopic));
				m_helpProvider2.SetHelpNavigator(this, HelpNavigator.Topic);
			}
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

		#endregion

		/// <summary>
		/// Fills the input fields in the form with the data in the PUACharacter.
		/// </summary>
		public void FillFormFromPUACharacter(bool replaceCodepointToo)
		{
			CheckDisposed();

			// If the character doesn't have all of its properites yet, fill them in
			if(m_puaChar.Empty)
				m_puaChar.RefreshFromIcu(true);

			// Plain text fields
			if(replaceCodepointToo)
				m_txtCodepoint.Text = m_puaChar.CodePoint;
			m_txtName.Text = m_puaChar.Name;
			m_txtUpperEquiv.Text = m_puaChar.Upper;
			m_txtLowerEquiv.Text = m_puaChar.Lower;
			m_txtTitleEquiv.Text = m_puaChar.Title;
			m_txtNumericValue.Text = m_puaChar.NumericValue;
			m_txtDecomposition.Text = m_puaChar.Decomposition;

			// Combo boxes - values
			m_cbGeneralCategory.SelectedItem = m_puaChar.GeneralCategory;
			m_cbCanonicalCombClass.SelectedItem = m_puaChar.CanonicalCombiningClass;
			m_cbBidiClass.SelectedItem = m_puaChar.BidiClass;
			m_cbCompatabilityDecomposition.SelectedItem = m_puaChar.CompatabilityDecomposition;
			m_cbNumericType.SelectedItem = m_puaChar.NumericType;

			// Check box
			m_chBidiMirrored.Checked = m_puaChar.BidiMirrored;
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomCharDlg));
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label m_cmbBoxPuaCanonCombin;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label7;
			System.Windows.Forms.Label label8;
			System.Windows.Forms.Label label9;
			System.Windows.Forms.Label label10;
			System.Windows.Forms.Label label12;
			System.Windows.Forms.Label label13;
			System.Windows.Forms.GroupBox groupBox1;
			System.Windows.Forms.GroupBox groupBox2;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.GroupBox groupBox3;
			System.Windows.Forms.Label label11;
			System.Windows.Forms.Button m_btnCancel;
			this.m_txtName = new System.Windows.Forms.TextBox();
			this.m_txtCodepoint = new System.Windows.Forms.TextBox();
			this.m_cbGeneralCategory = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_lblPUADisplay = new System.Windows.Forms.Label();
			this.m_lblMessageTop = new System.Windows.Forms.Label();
			this.m_txtNumericValue = new System.Windows.Forms.TextBox();
			this.m_txtTitleEquiv = new System.Windows.Forms.TextBox();
			this.m_txtLowerEquiv = new System.Windows.Forms.TextBox();
			this.m_txtUpperEquiv = new System.Windows.Forms.TextBox();
			this.m_lblUpperDisplay = new System.Windows.Forms.Label();
			this.m_lblLowerDisplay = new System.Windows.Forms.Label();
			this.m_lblTitleDisplay = new System.Windows.Forms.Label();
			this.m_cbNumericType = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_lblMessageMiddle = new System.Windows.Forms.Label();
			this.m_lblMessageBottom = new System.Windows.Forms.Label();
			this.m_chBidiMirrored = new System.Windows.Forms.CheckBox();
			this.m_cbCanonicalCombClass = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_cbBidiClass = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_txtDecomposition = new System.Windows.Forms.TextBox();
			this.m_cbCompatabilityDecomposition = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_lblDecompostionDisplay = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_lblWarning = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			m_cmbBoxPuaCanonCombin = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label7 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			label9 = new System.Windows.Forms.Label();
			label10 = new System.Windows.Forms.Label();
			label12 = new System.Windows.Forms.Label();
			label13 = new System.Windows.Forms.Label();
			groupBox1 = new System.Windows.Forms.GroupBox();
			groupBox2 = new System.Windows.Forms.GroupBox();
			label4 = new System.Windows.Forms.Label();
			groupBox3 = new System.Windows.Forms.GroupBox();
			label11 = new System.Windows.Forms.Label();
			m_btnCancel = new System.Windows.Forms.Button();
			groupBox1.SuspendLayout();
			groupBox2.SuspendLayout();
			groupBox3.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// m_cmbBoxPuaCanonCombin
			//
			resources.ApplyResources(m_cmbBoxPuaCanonCombin, "m_cmbBoxPuaCanonCombin");
			m_cmbBoxPuaCanonCombin.Name = "m_cmbBoxPuaCanonCombin";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// label7
			//
			resources.ApplyResources(label7, "label7");
			label7.Name = "label7";
			//
			// label8
			//
			resources.ApplyResources(label8, "label8");
			label8.Name = "label8";
			//
			// label9
			//
			resources.ApplyResources(label9, "label9");
			label9.Name = "label9";
			//
			// label10
			//
			resources.ApplyResources(label10, "label10");
			label10.Name = "label10";
			//
			// label12
			//
			resources.ApplyResources(label12, "label12");
			label12.Name = "label12";
			//
			// label13
			//
			resources.ApplyResources(label13, "label13");
			label13.Name = "label13";
			//
			// groupBox1
			//
			groupBox1.Controls.Add(this.m_txtName);
			groupBox1.Controls.Add(this.m_txtCodepoint);
			groupBox1.Controls.Add(label10);
			groupBox1.Controls.Add(label1);
			groupBox1.Controls.Add(this.m_cbGeneralCategory);
			groupBox1.Controls.Add(label2);
			groupBox1.Controls.Add(this.m_lblPUADisplay);
			groupBox1.Controls.Add(this.m_lblMessageTop);
			groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			resources.ApplyResources(groupBox1, "groupBox1");
			groupBox1.Name = "groupBox1";
			groupBox1.TabStop = false;
			//
			// m_txtName
			//
			resources.ApplyResources(this.m_txtName, "m_txtName");
			this.m_txtName.Name = "m_txtName";
			this.m_txtName.TextChanged += new System.EventHandler(this.m_txtName_TextChanged);
			this.m_txtName.Leave += new System.EventHandler(this.m_txtName_Leave);
			//
			// m_txtCodepoint
			//
			resources.ApplyResources(this.m_txtCodepoint, "m_txtCodepoint");
			this.m_txtCodepoint.Name = "m_txtCodepoint";
			this.m_txtCodepoint.TextChanged += new System.EventHandler(this.m_txtCodepoint_TextChanged);
			this.m_txtCodepoint.Leave += new System.EventHandler(this.m_txtCodepoint_Leave);
			//
			// m_cbGeneralCategory
			//
			this.m_cbGeneralCategory.AllowSpaceInEditBox = false;
			this.m_cbGeneralCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbGeneralCategory, "m_cbGeneralCategory");
			this.m_cbGeneralCategory.Name = "m_cbGeneralCategory";
			this.m_cbGeneralCategory.SelectedIndexChanged += new System.EventHandler(this.m_cbGeneralCategory_SelectedIndexChanged);
			//
			// m_lblPUADisplay
			//
			resources.ApplyResources(this.m_lblPUADisplay, "m_lblPUADisplay");
			this.m_lblPUADisplay.Name = "m_lblPUADisplay";
			//
			// m_lblMessageTop
			//
			this.m_lblMessageTop.ForeColor = System.Drawing.SystemColors.ControlText;
			resources.ApplyResources(this.m_lblMessageTop, "m_lblMessageTop");
			this.m_lblMessageTop.Name = "m_lblMessageTop";
			//
			// groupBox2
			//
			groupBox2.Controls.Add(label5);
			groupBox2.Controls.Add(this.m_txtNumericValue);
			groupBox2.Controls.Add(this.m_txtTitleEquiv);
			groupBox2.Controls.Add(this.m_txtLowerEquiv);
			groupBox2.Controls.Add(label9);
			groupBox2.Controls.Add(label7);
			groupBox2.Controls.Add(label8);
			groupBox2.Controls.Add(this.m_txtUpperEquiv);
			groupBox2.Controls.Add(this.m_lblUpperDisplay);
			groupBox2.Controls.Add(this.m_lblLowerDisplay);
			groupBox2.Controls.Add(this.m_lblTitleDisplay);
			groupBox2.Controls.Add(this.m_cbNumericType);
			groupBox2.Controls.Add(label4);
			groupBox2.Controls.Add(this.m_lblMessageMiddle);
			groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			resources.ApplyResources(groupBox2, "groupBox2");
			groupBox2.Name = "groupBox2";
			groupBox2.TabStop = false;
			//
			// m_txtNumericValue
			//
			resources.ApplyResources(this.m_txtNumericValue, "m_txtNumericValue");
			this.m_txtNumericValue.Name = "m_txtNumericValue";
			this.m_txtNumericValue.TextChanged += new System.EventHandler(this.m_txtNumericValue_TextChanged);
			this.m_txtNumericValue.Leave += new System.EventHandler(this.m_txtNumericValue_Leave);
			//
			// m_txtTitleEquiv
			//
			resources.ApplyResources(this.m_txtTitleEquiv, "m_txtTitleEquiv");
			this.m_txtTitleEquiv.Name = "m_txtTitleEquiv";
			this.m_txtTitleEquiv.TextChanged += new System.EventHandler(this.m_txtTitleEquiv_TextChanged);
			this.m_txtTitleEquiv.Leave += new System.EventHandler(this.m_txtTitleEquiv_Leave);
			//
			// m_txtLowerEquiv
			//
			resources.ApplyResources(this.m_txtLowerEquiv, "m_txtLowerEquiv");
			this.m_txtLowerEquiv.Name = "m_txtLowerEquiv";
			this.m_txtLowerEquiv.TextChanged += new System.EventHandler(this.m_txtLowerEquiv_TextChanged);
			this.m_txtLowerEquiv.Leave += new System.EventHandler(this.m_txtLowerEquiv_Leave);
			//
			// m_txtUpperEquiv
			//
			resources.ApplyResources(this.m_txtUpperEquiv, "m_txtUpperEquiv");
			this.m_txtUpperEquiv.Name = "m_txtUpperEquiv";
			this.m_txtUpperEquiv.TextChanged += new System.EventHandler(this.m_txtUpperEquiv_TextChanged);
			this.m_txtUpperEquiv.Leave += new System.EventHandler(this.m_txtUpperEquiv_Leave);
			//
			// m_lblUpperDisplay
			//
			resources.ApplyResources(this.m_lblUpperDisplay, "m_lblUpperDisplay");
			this.m_lblUpperDisplay.Name = "m_lblUpperDisplay";
			//
			// m_lblLowerDisplay
			//
			resources.ApplyResources(this.m_lblLowerDisplay, "m_lblLowerDisplay");
			this.m_lblLowerDisplay.Name = "m_lblLowerDisplay";
			//
			// m_lblTitleDisplay
			//
			resources.ApplyResources(this.m_lblTitleDisplay, "m_lblTitleDisplay");
			this.m_lblTitleDisplay.Name = "m_lblTitleDisplay";
			//
			// m_cbNumericType
			//
			this.m_cbNumericType.AllowSpaceInEditBox = false;
			this.m_cbNumericType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbNumericType, "m_cbNumericType");
			this.m_cbNumericType.Name = "m_cbNumericType";
			this.m_cbNumericType.SelectedIndexChanged += new System.EventHandler(this.m_cbNumericType_SelectedIndexChanged);
			this.m_cbNumericType.Leave += new System.EventHandler(this.m_cbNumericType_Leave);
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// m_lblMessageMiddle
			//
			this.m_lblMessageMiddle.ForeColor = System.Drawing.SystemColors.ControlText;
			resources.ApplyResources(this.m_lblMessageMiddle, "m_lblMessageMiddle");
			this.m_lblMessageMiddle.Name = "m_lblMessageMiddle";
			//
			// groupBox3
			//
			groupBox3.Controls.Add(this.m_lblMessageBottom);
			groupBox3.Controls.Add(this.m_chBidiMirrored);
			groupBox3.Controls.Add(this.m_cbCanonicalCombClass);
			groupBox3.Controls.Add(m_cmbBoxPuaCanonCombin);
			groupBox3.Controls.Add(label13);
			groupBox3.Controls.Add(label12);
			groupBox3.Controls.Add(this.m_cbBidiClass);
			groupBox3.Controls.Add(this.m_txtDecomposition);
			groupBox3.Controls.Add(this.m_cbCompatabilityDecomposition);
			groupBox3.Controls.Add(label11);
			groupBox3.Controls.Add(this.m_lblDecompostionDisplay);
			groupBox3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			resources.ApplyResources(groupBox3, "groupBox3");
			groupBox3.Name = "groupBox3";
			groupBox3.TabStop = false;
			//
			// m_lblMessageBottom
			//
			this.m_lblMessageBottom.ForeColor = System.Drawing.Color.Black;
			resources.ApplyResources(this.m_lblMessageBottom, "m_lblMessageBottom");
			this.m_lblMessageBottom.Name = "m_lblMessageBottom";
			//
			// m_chBidiMirrored
			//
			resources.ApplyResources(this.m_chBidiMirrored, "m_chBidiMirrored");
			this.m_chBidiMirrored.Name = "m_chBidiMirrored";
			this.m_toolTip.SetToolTip(this.m_chBidiMirrored, resources.GetString("m_chBidiMirrored.ToolTip"));
			this.m_chBidiMirrored.Leave += new System.EventHandler(this.m_chBidiMirrored_Leave);
			this.m_chBidiMirrored.CheckedChanged += new System.EventHandler(this.m_chBidiMirrored_CheckedChanged);
			//
			// m_cbCanonicalCombClass
			//
			this.m_cbCanonicalCombClass.AllowSpaceInEditBox = false;
			this.m_cbCanonicalCombClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbCanonicalCombClass, "m_cbCanonicalCombClass");
			this.m_cbCanonicalCombClass.Name = "m_cbCanonicalCombClass";
			this.m_cbCanonicalCombClass.Leave += new System.EventHandler(this.m_cbCanonicalCombClass_Leave);
			//
			// m_cbBidiClass
			//
			this.m_cbBidiClass.AllowSpaceInEditBox = false;
			this.m_cbBidiClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbBidiClass, "m_cbBidiClass");
			this.m_cbBidiClass.Name = "m_cbBidiClass";
			this.m_cbBidiClass.Leave += new System.EventHandler(this.m_cbBidiClass_Leave);
			//
			// m_txtDecomposition
			//
			resources.ApplyResources(this.m_txtDecomposition, "m_txtDecomposition");
			this.m_txtDecomposition.Name = "m_txtDecomposition";
			this.m_txtDecomposition.TextChanged += new System.EventHandler(this.m_txtDecomposition_TextChanged);
			this.m_txtDecomposition.Leave += new System.EventHandler(this.m_txtDecomposition_Leave);
			//
			// m_cbCompatabilityDecomposition
			//
			this.m_cbCompatabilityDecomposition.AllowSpaceInEditBox = false;
			this.m_cbCompatabilityDecomposition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbCompatabilityDecomposition, "m_cbCompatabilityDecomposition");
			this.m_cbCompatabilityDecomposition.Name = "m_cbCompatabilityDecomposition";
			this.m_cbCompatabilityDecomposition.SelectedIndexChanged += new System.EventHandler(this.m_cbCompatabilityDecomposition_SelectedIndexChanged);
			this.m_cbCompatabilityDecomposition.Leave += new System.EventHandler(this.m_cbCompatabilityDecomposition_Leave);
			//
			// label11
			//
			resources.ApplyResources(label11, "label11");
			label11.Name = "label11";
			//
			// m_lblDecompostionDisplay
			//
			resources.ApplyResources(this.m_lblDecompostionDisplay, "m_lblDecompostionDisplay");
			this.m_lblDecompostionDisplay.Name = "m_lblDecompostionDisplay";
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_helpProvider.SetHelpString(this.m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_helpProvider.SetShowHelp(this.m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_lblWarning
			//
			this.m_lblWarning.ForeColor = System.Drawing.Color.Red;
			resources.ApplyResources(this.m_lblWarning, "m_lblWarning");
			this.m_lblWarning.Name = "m_lblWarning";
			this.m_helpProvider.SetShowHelp(this.m_lblWarning, ((bool)(resources.GetObject("m_lblWarning.ShowHelp"))));
			//
			// CustomCharDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_lblWarning);
			this.Controls.Add(groupBox2);
			this.Controls.Add(groupBox1);
			this.Controls.Add(groupBox3);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(m_btnCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CustomCharDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			groupBox3.ResumeLayout(false);
			groupBox3.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region event handler helpers

		/// <summary>
		/// Sets all the "siblings" of the given baseControl to be the enabled value, but leaves baseControl enabled.
		/// </summary>
		/// <param name="baseControl"></param>
		/// <param name="enabled"></param>
		private static void SetEnabledAll(Control baseControl, bool enabled)
		{
			Type groupBoxType = typeof(GroupBox);
			Type textBoxType = typeof(TextBox);
			Type comboBoxType = typeof(ComboBox);
			Type checkBoxType = typeof(CheckBox);

			foreach( Control groupBox in baseControl.Parent.Parent.Controls)
				if(groupBox.GetType() == groupBoxType)
					foreach(Control control in groupBox.Controls)
					{
						// Skip the baseControl
						if(control == baseControl)
							continue;
						// Only for the controls that the user can actually set, disable them.
						if(control.GetType() == textBoxType ||
							control.GetType() == comboBoxType ||
							control.GetType() == checkBoxType)
								control.Enabled = enabled;
					}
		}

		/// <summary>
		/// Set the fields based on the general category value
		/// </summary>
		private void SetEnableBasedOnGeneralCategory()
		{
			var ucdEnumSelected = (UcdProperty)m_cbGeneralCategory.SelectedItem;

			// If the ucdEnum hasn't been selected yet, don't bother trying to use it.
			if(ucdEnumSelected == null)
				return;

			m_cbNumericType.Enabled = false;
			m_txtNumericValue.Enabled = false;
			m_txtUpperEquiv.Enabled = false;
			m_txtLowerEquiv.Enabled = false;
			m_txtTitleEquiv.Enabled = false;
			m_cbCanonicalCombClass.Enabled = false;

			m_chBidiMirrored.Enabled = true;

			char majorClass = ucdEnumSelected.UcdRepresentation[0];
			char subClass = ucdEnumSelected.UcdRepresentation[1];

			switch (majorClass)
			{
				case 'N':
					m_cbNumericType.Enabled = true;
					m_txtNumericValue.Enabled = true;
					m_chBidiMirrored.Enabled = false;
					break;
				case 'L':
					m_txtUpperEquiv.Enabled = true;
					m_txtLowerEquiv.Enabled = true;
					m_txtTitleEquiv.Enabled = true;
					m_chBidiMirrored.Enabled = false;
					break;
				case 'C':
					m_cbNumericType.Enabled = true;
					m_txtNumericValue.Enabled = true;
					m_txtUpperEquiv.Enabled = true;
					m_txtLowerEquiv.Enabled = true;
					m_txtTitleEquiv.Enabled = true;
					m_cbCanonicalCombClass.Enabled = true;
					break;
				case 'M':
					if( subClass == 'e' )
						break;
					m_cbCanonicalCombClass.Enabled = true;
					break;
				case 'Z':
					m_chBidiMirrored.Enabled = false;
					break;
			}

			DontDisplayHiddenData(m_txtUpperEquiv, m_puaChar.Upper, "");
			DontDisplayHiddenData(m_txtLowerEquiv, m_puaChar.Lower, "");
			DontDisplayHiddenData(m_txtTitleEquiv, m_puaChar.Title, "");
			DontDisplayHiddenData(m_txtNumericValue, m_puaChar.NumericValue, "");
			DontDisplayHiddenData(m_txtDecomposition, m_puaChar.Decomposition, "");

			DontDisplayHiddenData(m_chBidiMirrored, m_puaChar.BidiMirrored, false);

			DontDisplayHiddenData(m_cbNumericType, m_puaChar.NumericType,
				UcdProperty.GetInstance(Icu.UNumericType.U_NT_NONE));

			DontDisplayHiddenData(m_cbCanonicalCombClass, m_puaChar.CanonicalCombiningClass ,
				UcdProperty.GetInstance(0));

			switch (majorClass)
			{
				case 'N':
					if( subClass == 'd' )
					{
						m_cbNumericType.SelectedItem = UcdProperty.GetInstance(Icu.UNumericType.U_NT_DECIMAL);
						m_cbNumericType.Enabled = false;
					}
					break;
			}
		}

		/// <summary>
		/// Parses the given decompostion string and returns any errors if it can't succeed.
		/// </summary>
		/// <param name="decompostion">The hexadecimal values separated by spaces.</param>
		/// <param name="parsedDecomposition">The decomposition as it is represented in actual unicode codepoints.</param>
		/// <returns>A set of m_errorMessageHandler.ErrorMessages or <c>null</c> if it parses correctly.</returns>
		private static Set<ErrorMessageHandler.ErrorMessage> ParseDecomposition(string decompostion, out string parsedDecomposition)
		{
			var errorMessages = new Set<ErrorMessageHandler.ErrorMessage>();
			string[] codepoints = decompostion.Split(new[]{' '});
			parsedDecomposition = "";
			foreach(string codepoint in codepoints)
			{
				// Check to make sure the codepoint is valid
				ErrorMessageHandler.ErrorMessage errorMessage = ValidCodepoint(codepoint, true);
				if (errorMessage != ErrorMessageHandler.ErrorMessage.none)
					errorMessages.Add(errorMessage);
				// If there are no error yet, add the new character
				if(errorMessages.Count == 0)
					parsedDecomposition += PUACharacter.CodepointAsString(codepoint);
				else
					parsedDecomposition = "";
			}
			return errorMessages;
		}

		/// <summary>
		/// Grays the given text box if its value matches the PUA Characters codepoint.
		/// The user can still type, it merely makes the value less obtrusive.
		/// </summary>
		/// <param name="codePointTextBox">The textbox to change to gray</param>
		private void GrayMatch(TextBox codePointTextBox)
		{
			codePointTextBox.ForeColor = CodePointMatches(codePointTextBox) ?
				SystemColors.GrayText : SystemColors.ControlText;
		}

		/// <summary><c>true</c> if codePointTextBox's value matches the PUA Characters codepoint.</summary>
		/// <returns><c>true</c> if codePointTextBox's value matches the PUA Characters codepoint.</returns>
		private bool CodePointMatches(TextBox codePointTextBox)
		{
			return codePointTextBox.Text.TrimEnd(new[]{' '}) == m_puaChar.CodePoint ||
				codePointTextBox.Text.Trim().Length == 0;
		}

		/// <summary>
		/// Forces <c>textBox</c> to have valid text by removing invalid data:
		/// When a single character is incorrect, deletes it
		/// When more are incorrect it removes the entire string.
		/// </summary>
		/// <param name="textBox"></param>
		/// <param name="unicodePropertyType"></param>
		private static void ForceCharacterSet(TextBox textBox, UnicodePropertyType unicodePropertyType)
		{
			// Find and save the curret caret position
			int selectionStart = textBox.SelectionStart;

			// Remove the character right before the selection if it is wrong
			if( selectionStart > 0 && !IsValid(textBox.Text[selectionStart - 1], unicodePropertyType))
			{
				MiscUtils.ErrorBeep();
				RemoveSingleChar(textBox, selectionStart - 1);
				// Set the cursor back where it was
				textBox.SelectionStart = selectionStart - 1;
				return;
			}

			// Number of incorrect characters found so far
			int incorrectCount = 0;
			// Search all the characters in the text box
			foreach(char character in textBox.Text)
			{
				if(!IsValid(character, unicodePropertyType))
					incorrectCount++;
			}
			if(incorrectCount > 0)
				textBox.Text = "";

			// Set the characters to be uppercase and replace the cursor to where it began.
			if(incorrectCount <= 1)
			{
				// Set the text to be upper case
				textBox.Text = textBox.Text.ToUpperInvariant();
				// Don't allow the selection start to be negative (for tests which don't actually open the dialogbox)
				if(selectionStart < 0)
					selectionStart = 0;
				// Set the cursor back where it was
				textBox.SelectionStart = selectionStart;
			}
		}
		/// <summary>
		/// Forces <c>textBox</c> to have valid text by removing invalid data:
		/// When a single character is incorrect, deletes it
		/// When more are incorrect it removes the entire string.
		/// </summary>
		/// <param name="textBox"></param>
		private static void ForceValidNumeric(TextBox textBox)
		{
			// Find and save the curret caret position
			int selectionStart = textBox.SelectionStart;

			char characterJustTyped;
			if(selectionStart > 0)
				characterJustTyped = textBox.Text[selectionStart - 1];
			else
				return;

			int nonNumericCharacterCount = 0;

			if(!char.IsNumber(characterJustTyped))
			{
				if(characterJustTyped == '-')
				{
					// the '-' must appear at the beginning of the field (if at all)
					if( selectionStart != 1 )
					{
						MiscUtils.ErrorBeep();
						RemoveSingleChar(textBox, selectionStart - 1);
						// Set the cursor back where it was
						textBox.SelectionStart = selectionStart - 1;
						return;
					}
				}
				else
				{
					foreach(char character in textBox.Text)
						if(!char.IsNumber(character) && character != '-')
							nonNumericCharacterCount++;
					// Remove the '/' or '.' if there is already one of them.
					// (Having both '/' and '.' is not allowed)
					if(nonNumericCharacterCount > 1)
					{
						MiscUtils.ErrorBeep();
						RemoveSingleChar(textBox, selectionStart - 1);
						// Set the cursor back where it was
						textBox.SelectionStart = selectionStart - 1;
						return;
					}
				}
			}
		}

		private static void RemoveSingleChar(TextBox textBox, int incorrectCharIndex)
		{
			// Take out the invalid character
			textBox.Text = textBox.Text.Substring(0,incorrectCharIndex) +
				textBox.Text.Substring(incorrectCharIndex + 1);
		}

		private enum UnicodePropertyType
		{
			name,
			codepoint,
			decomposition,
			numeric
		}

		/// <summary>
		/// Returns if the character is valid.  The given <c>UnicodePropertyType</c> defines which characters are allowed.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="unicodePropertyType">The enumeration that defines what kind of property is being checked
		///		(e.g. codepoint, numeric, ...).</param>
		/// <returns></returns>
		private static bool IsValid(char character, UnicodePropertyType unicodePropertyType)
		{
			switch ( unicodePropertyType )
			{
				case UnicodePropertyType.name:
					return char.IsLetter(character) || character == ' ' || character == '-';
				case UnicodePropertyType.codepoint:
					return ('A' <= char.ToUpper(character) &&  char.ToUpper(character) <= 'F') || char.IsNumber(character);
				case UnicodePropertyType.decomposition:
					return IsValid(character, UnicodePropertyType.codepoint) || character == ' ';
				case UnicodePropertyType.numeric:
					return char.IsNumber(character) || character == '/' || character == '.' || character == '-';
			}
			return false;
		}


		private void UpperLowerTitleTextChanged(TextBox textBox, Label characterPreviewLabel)
		{
			m_errorMessageHandler.RemoveMessage(textBox);
			ForceCharacterSet(textBox,UnicodePropertyType.codepoint);
			GrayMatch(textBox);
			ErrorMessageHandler.ErrorMessage errorMessage =
				ValidCodepoint(textBox.Text, true);
			if(errorMessage == ErrorMessageHandler.ErrorMessage.none)
			{
				m_errorMessageHandler.RemoveStar(textBox);
				characterPreviewLabel.Text = PUACharacter.CodepointAsString(textBox.Text);
			}
			else
			{
				m_errorMessageHandler.AddStar(textBox);
				characterPreviewLabel.Text = "";
			}
		}

		private void UpperLowerTitleLeave(TextBox textBox)
		{
			ErrorMessageHandler.ErrorMessage errorMessage =
				ValidCodepoint(textBox.Text, true);
			if(errorMessage != ErrorMessageHandler.ErrorMessage.none)
				m_errorMessageHandler.AddMessage(textBox, errorMessage);
		}


		/// <summary>
		/// Check the numeric fields when either are changed
		/// </summary>
		private void CheckNumericTextChanged()
		{
			m_errorMessageHandler.RemoveMessage(m_txtNumericValue);
			bool digit = true;
			if(m_cbNumericType.SelectedItem == UcdProperty.GetInstance(Icu.UNumericType.U_NT_NONE) ||
				m_cbNumericType.SelectedItem == UcdProperty.GetInstance(Icu.UNumericType.U_NT_NUMERIC))
				digit = false;
			if(ValidNumeric(m_txtNumericValue.Text, digit).Count != 0)
				m_errorMessageHandler.AddStar(m_txtNumericValue);
			else
				m_errorMessageHandler.RemoveStar(m_txtNumericValue);
		}
		/// <summary>
		/// Check the numeric fields when either are left
		/// </summary>
		private void CheckNumericLeave()
		{
			bool digit = true;
			if(m_puaChar.NumericType == UcdProperty.GetInstance(Icu.UNumericType.U_NT_NONE) ||
				m_puaChar.NumericType == UcdProperty.GetInstance(Icu.UNumericType.U_NT_NUMERIC))
					digit = false;
				Set<ErrorMessageHandler.ErrorMessage> errorMessages = ValidNumeric(m_txtNumericValue.Text, digit);
			if( errorMessages.Count != 0)
				m_errorMessageHandler.AddMessage(m_txtNumericValue, errorMessages);
		}

		#region Validation Helpers
		/// <summary>
		/// Checks the validity of the the codepoint string.
		/// </summary>
		/// <param name="codepoint">The codepoint as a string to check</param>
		/// <param name="lessStrict">we use less strict rules for Upper/Lower/Title,
		/// we want this parameter to be false when the topmost codepoint is being passed</param>
		/// <returns></returns>
		private static ErrorMessageHandler.ErrorMessage ValidCodepoint(string codepoint, bool lessStrict)
		{
			if( codepoint.Length == 0 && lessStrict )
				return ErrorMessageHandler.ErrorMessage.none;
			if( codepoint.Length < 4)
				return ErrorMessageHandler.ErrorMessage.shortCodepoint;
			if( codepoint.Length > 6)
				return ErrorMessageHandler.ErrorMessage.longCodepoint;
			if (codepoint.Length > 3 && Convert.ToInt32(codepoint, 16) == 0)
				return ErrorMessageHandler.ErrorMessage.zeroCodepoint;
			if (Icu.IsSurrogate(codepoint))
				return ErrorMessageHandler.ErrorMessage.inSurrogateRange;
			return ErrorMessageHandler.ErrorMessage.none;
		}

		/// <summary>
		/// Checks the given numeric string to see if it is a valid numeric
		/// </summary>
		/// <param name="numeric"></param>
		/// <param name="isDigit">If the character is a digit or decimal digit.</param>
		/// <returns></returns>
		private static Set<ErrorMessageHandler.ErrorMessage> ValidNumeric(string numeric, bool isDigit)
		{
			var errorMessages = new Set<ErrorMessageHandler.ErrorMessage>();
			if(isDigit)
			{
				// Don't allow any non-numerics in digit numeric values.
				if(numeric.IndexOf('.') != -1)
					errorMessages.Add(ErrorMessageHandler.ErrorMessage.numericDotDigit);
				if(numeric.IndexOf('/') != -1)
					errorMessages.Add(ErrorMessageHandler.ErrorMessage.numericSlashDigit);
				if(numeric.IndexOf('-') != -1)
					errorMessages.Add(ErrorMessageHandler.ErrorMessage.numericDashDigit);
			}
			else
			{
				int slashIndex = numeric.IndexOf('/');
				// If there is a slash
				if(slashIndex != -1)
				{
					// Don't allow any fractions that won't parse (e.g. "2/0" "-/"  "1/")
					try
					{
						int.Parse(numeric.Substring(0,slashIndex));
						int denominator = int.Parse(numeric.Substring(slashIndex + 1));
						if (denominator == 0)
							errorMessages.Add(ErrorMessageHandler.ErrorMessage.numericMalformedFraction);
					}
					catch
					{
						errorMessages.Add(ErrorMessageHandler.ErrorMessage.numericMalformedFraction);
					}
				}
			}
			return errorMessages;
		}

		/// <summary>
		/// Check to make sure that the decomposition field is not empty unless the
		/// DecompositionType selected is "none"
		/// </summary>
		/// <returns></returns>
		private ErrorMessageHandler.ErrorMessage CheckEmptyDecomposition()
		{
			if( m_cbCompatabilityDecomposition.SelectedItem !=
				UcdProperty.GetInstance(Icu.UDecompositionType.U_DT_NONE) &&
				CodePointMatches(m_txtDecomposition)
				)
			{
				return ErrorMessageHandler.ErrorMessage.mustEnterDecomp;
			}
			return ErrorMessageHandler.ErrorMessage.none;
		}

		/// <summary>
		/// Add the error star if decompostion and decomposition type do not match.
		/// </summary>
		private void DecompostionControlsTextChanged()
		{
			// Remove the message the moment they begin talking
			m_errorMessageHandler.RemoveMessage(m_txtDecomposition);
			// Check to make sure that the decomposition field is not empty unless the
			// DecompositionType selected is "none"
			ErrorMessageHandler.ErrorMessage errorMessage = CheckEmptyDecomposition();
			// Display the star
			if(errorMessage != ErrorMessageHandler.ErrorMessage.none)
				m_errorMessageHandler.AddStar(m_txtDecomposition);
			else
				m_errorMessageHandler.RemoveStar(m_txtDecomposition);
		}

		/// <summary>
		/// Add the error message text if decomposition and decompostion type do not match.
		/// </summary>
		private void DecompostionControlsLeave()
		{
			// Check to make sure that the decomposition field is not empty unless the
			// DecompositionType selected is "none"
			ErrorMessageHandler.ErrorMessage errorMessage = CheckEmptyDecomposition();
			// Display the star
			if(errorMessage != ErrorMessageHandler.ErrorMessage.none)
				m_errorMessageHandler.AddMessage(m_txtDecomposition, errorMessage);
		}
		#endregion
		#endregion

		#region event_handlers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			m_cbGeneralCategory.Sorted = true;
			m_cbCanonicalCombClass.Sorted = true;
			m_cbBidiClass.Sorted = true;
			m_cbCompatabilityDecomposition.Sorted = true;
			m_cbNumericType.Sorted = true;

			m_lblMessageTop.Text = Properties.Resources.kstidMessageIntro;

			// Combo boxes - options

			m_cbGeneralCategory.Items.Clear();
			ICollection enumerations = UcdProperty.GetUCDProperty(UcdProperty.UcdCategories.generalCategory);
			foreach( UcdProperty ucdEnum in enumerations)
				m_cbGeneralCategory.Items.Add(ucdEnum);

			enumerations =
				UcdProperty.GetUCDProperty(UcdProperty.UcdCategories.canonicalCombiningClass);
			foreach( UcdProperty ucdEnum in enumerations)
				m_cbCanonicalCombClass.Items.Add(ucdEnum);

			enumerations =
				UcdProperty.GetUCDProperty(UcdProperty.UcdCategories.bidiClass);
			foreach( UcdProperty ucdEnum in enumerations)
				m_cbBidiClass.Items.Add(ucdEnum);

			enumerations =
				UcdProperty.GetUCDProperty
				(UcdProperty.UcdCategories.compatabilityDecompositionType);
			foreach( UcdProperty ucdEnum in enumerations)
				m_cbCompatabilityDecomposition.Items.Add(ucdEnum);

			enumerations =
				UcdProperty.GetUCDProperty(UcdProperty.UcdCategories.numericType);
			foreach( UcdProperty ucdEnum in enumerations)
				m_cbNumericType.Items.Add(ucdEnum);

			FillFormFromPUACharacter(true);

			if(!Modify)
			{
				SetEnabledAll(m_txtCodepoint, false);
			}
		}

		private void m_cbBidiClass_Leave(object sender, EventArgs e)
		{
			m_puaChar.BidiClass = (UcdProperty)m_cbBidiClass.SelectedItem;
		}

		/// <summary>
		/// Performs data validation on the decomposion text as the user types.
		/// </summary>
		/// <remarks>
		/// Performs two kind of data validation:
		/// 1) Forces the characters to only be upper hexadecimal and spaces as the user types.
		/// 2) Displays a an error star next to the text box if the there is an error,
		/// such as the codepoint is too short.
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_txtDecomposition_TextChanged(object sender, EventArgs e)
		{
			// Remove any errors related to this textbox the moment the user changes a character
			m_errorMessageHandler.RemoveMessage(m_txtDecomposition);

			// Force the characters to be uppercase hexadecimals codes with spaces
			ForceCharacterSet(m_txtDecomposition, UnicodePropertyType.decomposition);
			// Make the characters appear gray if they match the codepoint of the character
			// (since this technically isn't a decompostion, but the codepoint itself)
			GrayMatch(m_txtDecomposition);

			//Parse the display string so that the user can see what codepoints they have entered.
			// Display an error star if any errors are encountered in the process.
			string parsedDecomposition;
			Set<ErrorMessageHandler.ErrorMessage> errorMessages = ParseDecomposition(m_txtDecomposition.Text,
				out parsedDecomposition);
			if (errorMessages.Count == 0)
			{
				m_lblDecompostionDisplay.Text = parsedDecomposition;
				m_errorMessageHandler.RemoveStar(m_txtDecomposition);
			}
			else
			{
				m_lblDecompostionDisplay.Text = "";
				m_errorMessageHandler.AddStar(m_txtDecomposition);
			}

			DecompostionControlsTextChanged();
		}

		/// <summary>
		/// When the user leaves, display any error messages and save the underlying data.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_txtDecomposition_Leave(object sender, EventArgs e)
		{
			// Set the underlying data
			m_puaChar.Decomposition = m_txtDecomposition.Text;
			// Display text discribing any errors in the decomposition string syntax.
			string parsedDecomposition;
			Set<ErrorMessageHandler.ErrorMessage> errorMessages = ParseDecomposition(m_txtDecomposition.Text,
				out parsedDecomposition);
			m_errorMessageHandler.AddMessage(m_txtDecomposition, errorMessages);

			DecompostionControlsLeave();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnOK control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnOK_Click(object sender, EventArgs e)
		{
			// Assign all fields to the values that are currently being displayed.
			m_puaChar.CodePoint = m_txtCodepoint.Text;
			m_puaChar.Name = m_txtName.Text;
			m_puaChar.GeneralCategory = (UcdProperty)m_cbGeneralCategory.SelectedItem;
			m_puaChar.CanonicalCombiningClass = (UcdProperty)m_cbCanonicalCombClass.SelectedItem;
			m_puaChar.BidiClass = (UcdProperty)m_cbBidiClass.SelectedItem;
			m_puaChar.CompatabilityDecomposition =
				(UcdProperty)m_cbCompatabilityDecomposition.SelectedItem;
			m_puaChar.Decomposition = m_txtDecomposition.Text;
			m_puaChar.NumericType = (UcdProperty)m_cbNumericType.SelectedItem;
			m_puaChar.NumericValue = m_txtNumericValue.Text;
			m_puaChar.BidiMirrored = m_chBidiMirrored.Checked;
			m_puaChar.Upper = m_txtUpperEquiv.Text;
			m_puaChar.Lower = m_txtLowerEquiv.Text;
			m_puaChar.Title = m_txtTitleEquiv.Text;

			// Clear matching codepoints
			if(CodePointMatches(m_txtUpperEquiv))
				m_puaChar.Upper = "";
			if(CodePointMatches(m_txtLowerEquiv))
				m_puaChar.Lower = "";
			if(CodePointMatches(m_txtTitleEquiv))
				m_puaChar.Title = "";
			if(CodePointMatches(m_txtDecomposition))
				m_puaChar.Decomposition = "";

			// Redirect the value that the user will request using the PuaChar property
			// This commits the user's changes
			m_storedPuaChar.Copy(m_puaChar);
		}

		/// <summary>
		/// Exits from the Dialog without making any changes to the PUA Character.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_btnCancel_Click(object sender, EventArgs e)
		{
		}

		private void m_txtCodepoint_TextChanged(object sender, EventArgs e)
		{
			// Display the character in the display label
			m_lblPUADisplay.Text = PUACharacter.CodepointAsString(m_txtCodepoint.Text);
			m_lblWarning.Text = "";

			// Don't bother decoding the text if the text box was disabled, becuase then the
			// user didn't type it.
			if(m_txtCodepoint.Enabled == false)
				return;

			// Remove any related messages instantly if the user types in this box.
			m_errorMessageHandler.RemoveMessage(m_txtCodepoint);
			m_errorMessageHandler.RemoveStar(m_txtName);
			// Force the user to only type valid codepoint characters.
			ForceCharacterSet(m_txtCodepoint, UnicodePropertyType.codepoint);
			Modify = false;
			// Check to seee if the codepoint is valid
			ErrorMessageHandler.ErrorMessage errorMessage = ValidCodepoint(m_txtCodepoint.Text, false);

			// Only load if the codepoint is valid
			if(errorMessage == ErrorMessageHandler.ErrorMessage.none)
			{
				// Automatically load the values from the cache
				PUACharacter cachedCharacter = m_parentDialog.FindCachedIcuEntry(m_txtCodepoint.Text);
				// If the cache actually gives us a character, load it into the fields
				if (cachedCharacter == null)
				{
					m_puaChar = PUACharacter.UnicodeDefault;
					FillFormFromPUACharacter(false);
					m_lblWarning.Text = "";
					Text = Properties.Resources.kstidAddPuaTitle;
				}
				else
				{
					m_puaChar = cachedCharacter;
					FillFormFromPUACharacter(false);
					if (m_parentDialog.IsCustomChar(cachedCharacter.CodePoint))
					{
						m_lblWarning.Text = Properties.Resources.kstidOverwriteUserCode;
						Text = Properties.Resources.kstidModifyPuaTitle;
					}
					else
					{
						if (!string.IsNullOrEmpty(m_puaChar.Name))
							m_lblWarning.Text = Properties.Resources.kstidOverwriteUnicode;
						Text = Properties.Resources.kstidAddPuaTitle;
					}
				}
			}
			// Show the error star and block all fields if the character is not a valid, unused
			// character.
			// This must be done last so that nothing else will enable or disable any of the boxes.
			if( errorMessage == ErrorMessageHandler.ErrorMessage.none)
			{
				// The character is valid, load it
				m_errorMessageHandler.RemoveStar(m_txtCodepoint);
				// Open all the disabled fields
				SetEnabledAll(m_txtCodepoint, true);
				// Force the name to validate.
				m_txtName_TextChanged(null, null);
				// Make general category disable appropriate boxes.
				SetEnableBasedOnGeneralCategory();
			}
			else
			{
				m_errorMessageHandler.AddStar(m_txtCodepoint);
				m_errorMessageHandler.AddMessage(m_txtCodepoint, errorMessage);
				SetEnabledAll(m_txtCodepoint, false);
				m_puaChar = PUACharacter.UnicodeDefault;
				m_lblPUADisplay.Text = "";
				FillFormFromPUACharacter(false);
			}
		}

		private void m_txtCodepoint_Leave(object sender, EventArgs e)
		{
			m_puaChar.CodePoint = m_txtCodepoint.Text;
		}

		private void m_txtName_TextChanged(object sender, EventArgs e)
		{
			m_errorMessageHandler.RemoveMessage(m_txtName);
			ForceCharacterSet(m_txtName,UnicodePropertyType.name);
			if( m_txtName.Text.Length <= 0 )
				m_errorMessageHandler.AddStar(m_txtName);
			else
				m_errorMessageHandler.RemoveStar(m_txtName);
		}

		private void m_txtName_Leave(object sender, EventArgs e)
		{
			// Set the underlying data
			m_puaChar.Name = m_txtName.Text;

			if( m_txtName.Text.Length <= 0 )
				m_errorMessageHandler.AddMessage(m_txtName,
					ErrorMessageHandler.ErrorMessage.emptyName);
		}

		private void m_txtUpperEquiv_TextChanged(object sender, EventArgs e)
		{
			UpperLowerTitleTextChanged(m_txtUpperEquiv, m_lblUpperDisplay);
		}

		private void m_txtUpperEquiv_Leave(object sender, EventArgs e)
		{
			UpperLowerTitleLeave(m_txtUpperEquiv);
			m_puaChar.Upper = m_txtUpperEquiv.Text;
		}

		private void m_txtLowerEquiv_TextChanged(object sender, EventArgs e)
		{
			UpperLowerTitleTextChanged(m_txtLowerEquiv, m_lblLowerDisplay);
		}
		private void m_txtLowerEquiv_Leave(object sender, EventArgs e)
		{
			UpperLowerTitleLeave(m_txtLowerEquiv);
			m_puaChar.Lower = m_txtLowerEquiv.Text;
		}

		private void m_txtTitleEquiv_TextChanged(object sender, EventArgs e)
		{
			UpperLowerTitleTextChanged(m_txtTitleEquiv, m_lblTitleDisplay);
		}

		private void m_txtTitleEquiv_Leave(object sender, EventArgs e)
		{
			UpperLowerTitleLeave(m_txtTitleEquiv);
			m_puaChar.Title = m_txtTitleEquiv.Text;
		}

		private void m_txtNumericValue_TextChanged(object sender, EventArgs e)
		{
			ForceCharacterSet(m_txtNumericValue, UnicodePropertyType.numeric);
			ForceValidNumeric(m_txtNumericValue);
			CheckNumericTextChanged();
		}

		private void m_txtNumericValue_Leave(object sender, EventArgs e)
		{
			m_puaChar.NumericValue = m_txtNumericValue.Text;
			CheckNumericLeave();
		}

		private void m_cbGeneralCategory_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_puaChar.GeneralCategory = (UcdProperty)m_cbGeneralCategory.SelectedItem;
			SetEnableBasedOnGeneralCategory();
		}

		/// <summary>
		/// Checks the given GUI item to see if it disabled.
		/// If it is disabled, sets it to a disabled value
		/// If it is enabled it restores the value to be <c>savedString</c>
		/// </summary>
		/// <param name="textBox">The GUI item to check.</param>
		/// <param name="savedString">The value to restore the feild with if it is no
		/// longer saved.</param>
		/// <param name="disabledDisplayValue">The value to display if the <c>textBox</c> is
		/// disabled</param>
		private static void DontDisplayHiddenData(TextBox textBox, string savedString,
			string disabledDisplayValue)
		{
			textBox.Text = !textBox.Enabled ? disabledDisplayValue : savedString;
		}

		/// <summary>
		/// Checks the given GUI item to see if it disabled.
		/// If it is disabled, sets it to a disabled value
		/// If it is enabled it restores the value to be <c>savedValue</c>
		/// </summary>
		/// <param name="checkBox">The GUI item to check.</param>
		/// <param name="savedValue">The value to restore the feild with if it is no
		/// longer saved.</param>
		/// <param name="disabledDisplayValue">The value to display if the <c>textBox</c>
		/// is disabled</param>
		private static void DontDisplayHiddenData(CheckBox checkBox, bool savedValue, bool disabledDisplayValue)
		{
			checkBox.Checked = !checkBox.Enabled ? disabledDisplayValue : savedValue;
		}

		/// <summary>
		/// Checks the given GUI item to see if it disabled.
		/// If it is disabled, sets it to the disabled display value.
		/// If it is enabled it restores the value to be <c>savedString</c>
		/// </summary>
		/// <param name="comboBox">The GUI item to check.</param>
		/// <param name="savedEnumeration">The value to restore the field with if it is no
		/// longer saved.</param>
		/// <param name="disabledDisplayEnumeration">The value to display if
		/// the <c>textBox</c> is disabled</param>
		private static void DontDisplayHiddenData(ComboBox comboBox, UcdProperty savedEnumeration,
			UcdProperty disabledDisplayEnumeration)
		{
			comboBox.SelectedItem = !comboBox.Enabled ? disabledDisplayEnumeration : savedEnumeration;
		}

		private void m_cbCanonicalCombClass_Leave(object sender, EventArgs e)
		{
			m_puaChar.CanonicalCombiningClass = (UcdProperty)m_cbCanonicalCombClass.SelectedItem;
		}

		private void m_chBidiMirrored_CheckedChanged(object sender, EventArgs e)
		{
		}

		private void m_chBidiMirrored_Leave(object sender, EventArgs e)
		{
			m_puaChar.BidiMirrored = m_chBidiMirrored.Checked;
		}

		private void m_cbNumericType_SelectedIndexChanged(object sender, EventArgs e)
		{
			CheckNumericTextChanged();
		}
		private void m_cbNumericType_Leave(object sender, EventArgs e)
		{
			m_puaChar.NumericType = (UcdProperty)m_cbNumericType.SelectedItem;
			CheckNumericLeave();
		}

		private void m_cbCompatabilityDecomposition_Leave(object sender, EventArgs e)
		{
			m_puaChar.CompatabilityDecomposition =
				(UcdProperty)m_cbCompatabilityDecomposition.SelectedItem;
			DecompostionControlsLeave();
		}
		#endregion

		// This method merely irritates users (see LT-5845).  I think it's leftover debugging code.
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Raises the <see cref="E:System.Windows.Forms.Form.Activated"></see> event.
		///// </summary>
		///// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		///// ------------------------------------------------------------------------------------
		//protected override void OnActivated(EventArgs e)
		//{
		//    base.OnActivated(e);
		//    // For new PUACharacters start with the focus in the codepoint.
		//    if(!Modify && !m_txtCodepoint.Focus())
		//        MessageBox.Show(FwCoreDlgs.ksCannotSetFocus);
		//}

		private void m_cbCompatabilityDecomposition_SelectedIndexChanged(object sender, EventArgs e)
		{
			DecompostionControlsTextChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_sHelpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the ShowDialog method (so that we can prevent showing the dialog in the tests)
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual DialogResult CallShowDialog()
		{
			CheckDisposed();

			return ShowDialog();
		}
	}
}
