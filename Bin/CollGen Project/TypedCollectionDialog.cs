namespace TypedCollectionBuilder
{
	using Microsoft.Win32;
	using System;
	using System.IO;
	using System.Drawing;
	using System.Collections;
	using System.ComponentModel;
	using System.Windows.Forms;
	using System.Data;
	using System.Diagnostics;
	using System.Text.RegularExpressions;

	/// <summary>
	///    Summary description for Form1.
	/// </summary>
	public class FormMain : System.Windows.Forms.Form
	{

		private enum Effect
		{
			CodeGenPathField = 0,
			FileNameField = 1,
			LanguageField = 2,
			NestedEnumField = 3,
			ValidationField = 4,
			CollectionNamespaceField = 5,
			CollectionTypeField = 6,
			CollectionTypeNamespaceField = 7,
			AuthorNameField = 8,
			DisposeField = 9,
		};

		private string originalDir;
		private string fileExt;
		private Hashtable htEffects;
		private SaveFileDialog sfd;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.ToolTip toolTipMain;
		private System.Windows.Forms.ErrorProvider errorProviderMain;
		private System.Windows.Forms.Button buttonClear;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.Button buttonGenerate;
		private System.Windows.Forms.TextBox textCollectionName;
		private System.Windows.Forms.TextBox textCodeGenPath;
		private System.Windows.Forms.TextBox textTypedClassNamespace;
		private System.Windows.Forms.TextBox textCollectionNS;
		private System.Windows.Forms.TextBox textFileName;
		private System.Windows.Forms.TextBox textPreview;
		private System.Windows.Forms.CheckBox checkNestedEnum;
		private System.Windows.Forms.CheckBox checkAddValidation;
		private System.Windows.Forms.Label labelFileName;
		private System.Windows.Forms.Label labelPath;
		private System.Windows.Forms.Label labelTypeDefinition;
		private System.Windows.Forms.Label labelCollectionSpace;
		private System.Windows.Forms.Label labelPreview;
		private System.Windows.Forms.Label labelType;
		private System.Windows.Forms.GroupBox groupLanguage;
		private System.Windows.Forms.RadioButton optionJScript;
		private System.Windows.Forms.RadioButton optionVB;
		private System.Windows.Forms.RadioButton optionCS;
		private System.Windows.Forms.Button buttonLocation;
		private System.Windows.Forms.StatusBar statusMain;
		private System.Windows.Forms.StatusBarPanel panelMessage;
		private System.Windows.Forms.Timer timerMain;
		private System.Windows.Forms.LinkLabel helpMain;
		private System.Windows.Forms.TextBox textEffect;
		private System.Windows.Forms.Label labelExplanation;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox authorName;
		private System.Windows.Forms.CheckBox checkAddDispose;
		private System.Windows.Forms.Button buttonExample;

		public FormMain()
		{
			//
			// Required for Win Form Designer support
			//
			InitializeComponent();
			originalDir = Environment.CurrentDirectory;
			SetupFileDialog();

			//initialize the array of effect entries
			htEffects = new Hashtable();

			htEffects.Add(Effect.CodeGenPathField,
				"The Code Generation Path field determines the location in which your file is saved, once you click the 'Generate' button. If left empty, the file is saved in the current location (if not specified, this field will be automaticaly filled when all critical fields are completed).");
			htEffects.Add(Effect.FileNameField,
				"The Filename field determines the name of the file that is created, once the 'Generate' button is clicked. Note that an extension is automatically added to the name type in this box, based on the language selected from the language group below. Note, this name does not impact the name of the collection itself");
			htEffects.Add(Effect.CollectionTypeField,
				"The Type field determines the Type stored in your collection. The collection itself is named by concatenating the text in this field plus the word 'Collection'. The Type name specified here is also used throughout the source code generated");
			htEffects.Add(Effect.CollectionTypeNamespaceField,
				"The Type's Namespace (package in JScript) field indicates the Namespace in which the Type is declared. It's effect on the generated code is to include a 'using' (C#), 'Imports' (Visual Basic), or 'import' (JScript) statement near the top of the generated code");
			htEffects.Add(Effect.AuthorNameField,
				"The Author's Name field specifies the name of the person responsible for the created file.");
			htEffects.Add(Effect.CollectionNamespaceField,
				"The Collection's Namespace field indicates the Namespace (package in JScript) in which your collection is itself defined. It's effect on the generated code is to determine the Namespace declared for your collection, near the top of the code, as well as the namespace references included in the comments");
			htEffects.Add(Effect.LanguageField,
				"Using the language radio buttons, you can change the language in which the source code is generated. You can change this at any time");
			htEffects.Add(Effect.NestedEnumField,
				"Selecting the Nested Enumerator checkbox means that the Enumerator included with your collection is nested within the collection itself, rather than have it declared outside your collection. The enumerator is declared at the bottom of the generated code, and is named base on the specified Type");
			htEffects.Add(Effect.ValidationField,
				"Selecting the Add Validation checkbox means that Validation methods are added into the generated source code (they are added near the bottom of the code, but above the Enumerator declaration). Note that the validation methods are empty once added, but you can easily add your own validation code");
			htEffects.Add(Effect.DisposeField,
				"Selecting the Add Dispose checkbox means that the IDisposable interface and a Dispose() method are added into the generated code. The Dispose() method calls Dispose() for all items in the collection.");
			this.MinimumSize = new Size(640,480);
		}

		/// <summary>
		///    Clean up any resources being used
		/// </summary>
		public new void Dispose()
		{
			base.Dispose();
			components.Dispose();
		}

		/// <summary>
		///    Required method for Designer support - do not modify
		///    the contents of this method with the code editor
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.sfd = new System.Windows.Forms.SaveFileDialog();
			this.buttonGenerate = new System.Windows.Forms.Button();
			this.errorProviderMain = new System.Windows.Forms.ErrorProvider();
			this.textCollectionName = new System.Windows.Forms.TextBox();
			this.textCodeGenPath = new System.Windows.Forms.TextBox();
			this.textTypedClassNamespace = new System.Windows.Forms.TextBox();
			this.textCollectionNS = new System.Windows.Forms.TextBox();
			this.textFileName = new System.Windows.Forms.TextBox();
			this.authorName = new System.Windows.Forms.TextBox();
			this.textPreview = new System.Windows.Forms.TextBox();
			this.toolTipMain = new System.Windows.Forms.ToolTip(this.components);
			this.checkNestedEnum = new System.Windows.Forms.CheckBox();
			this.checkAddValidation = new System.Windows.Forms.CheckBox();
			this.buttonClear = new System.Windows.Forms.Button();
			this.buttonExample = new System.Windows.Forms.Button();
			this.optionJScript = new System.Windows.Forms.RadioButton();
			this.optionVB = new System.Windows.Forms.RadioButton();
			this.optionCS = new System.Windows.Forms.RadioButton();
			this.buttonClose = new System.Windows.Forms.Button();
			this.labelFileName = new System.Windows.Forms.Label();
			this.labelPath = new System.Windows.Forms.Label();
			this.labelTypeDefinition = new System.Windows.Forms.Label();
			this.labelCollectionSpace = new System.Windows.Forms.Label();
			this.labelPreview = new System.Windows.Forms.Label();
			this.labelType = new System.Windows.Forms.Label();
			this.groupLanguage = new System.Windows.Forms.GroupBox();
			this.buttonLocation = new System.Windows.Forms.Button();
			this.statusMain = new System.Windows.Forms.StatusBar();
			this.panelMessage = new System.Windows.Forms.StatusBarPanel();
			this.timerMain = new System.Windows.Forms.Timer(this.components);
			this.helpMain = new System.Windows.Forms.LinkLabel();
			this.textEffect = new System.Windows.Forms.TextBox();
			this.labelExplanation = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.checkAddDispose = new System.Windows.Forms.CheckBox();
			this.groupLanguage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.panelMessage)).BeginInit();
			this.SuspendLayout();
			//
			// buttonGenerate
			//
			this.buttonGenerate.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.buttonGenerate.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonGenerate.Enabled = false;
			this.buttonGenerate.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonGenerate.ForeColor = System.Drawing.SystemColors.ControlText;
			this.buttonGenerate.Location = new System.Drawing.Point(384, 520);
			this.buttonGenerate.Name = "buttonGenerate";
			this.buttonGenerate.TabIndex = 24;
			this.buttonGenerate.Text = "Generate";
			this.toolTipMain.SetToolTip(this.buttonGenerate, "Generates the actual source file for your collection");
			this.buttonGenerate.Click += new System.EventHandler(this.buttonGenerate_Click);
			//
			// errorProviderMain
			//
			this.errorProviderMain.DataMember = null;
			//
			// textCollectionName
			//
			this.textCollectionName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textCollectionName.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.errorProviderMain.SetIconAlignment(this.textCollectionName, System.Windows.Forms.ErrorIconAlignment.MiddleLeft);
			this.errorProviderMain.SetIconPadding(this.textCollectionName, 2);
			this.textCollectionName.Location = new System.Drawing.Point(16, 288);
			this.textCollectionName.Name = "textCollectionName";
			this.textCollectionName.Size = new System.Drawing.Size(248, 22);
			this.textCollectionName.TabIndex = 14;
			this.textCollectionName.Text = "";
			this.toolTipMain.SetToolTip(this.textCollectionName, "Type the name of the object type which may be stored in this collection (E.g. Cus" +
				"tomer)");
			this.textCollectionName.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.textCollectionName.TextChanged += new System.EventHandler(this.textCollectionName_TextChanged);
			this.textCollectionName.Enter += new System.EventHandler(this.textCollectionName_Enter);
			//
			// textCodeGenPath
			//
			this.textCodeGenPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textCodeGenPath.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.errorProviderMain.SetIconAlignment(this.textCodeGenPath, System.Windows.Forms.ErrorIconAlignment.MiddleLeft);
			this.errorProviderMain.SetIconPadding(this.textCodeGenPath, 2);
			this.textCodeGenPath.Location = new System.Drawing.Point(16, 32);
			this.textCodeGenPath.Name = "textCodeGenPath";
			this.textCodeGenPath.Size = new System.Drawing.Size(216, 22);
			this.textCodeGenPath.TabIndex = 1;
			this.textCodeGenPath.Text = "";
			this.toolTipMain.SetToolTip(this.textCodeGenPath, "Type the location in which you want to save the file (E.g. c:\\temp)");
			this.textCodeGenPath.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.textCodeGenPath.TextChanged += new System.EventHandler(this.textCodeGenPath_TextChanged);
			this.textCodeGenPath.Enter += new System.EventHandler(this.textCodeGenPath_Enter);
			//
			// textTypedClassNamespace
			//
			this.textTypedClassNamespace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textTypedClassNamespace.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.errorProviderMain.SetIconAlignment(this.textTypedClassNamespace, System.Windows.Forms.ErrorIconAlignment.MiddleLeft);
			this.errorProviderMain.SetIconPadding(this.textTypedClassNamespace, 2);
			this.textTypedClassNamespace.Location = new System.Drawing.Point(16, 336);
			this.textTypedClassNamespace.Name = "textTypedClassNamespace";
			this.textTypedClassNamespace.Size = new System.Drawing.Size(248, 22);
			this.textTypedClassNamespace.TabIndex = 16;
			this.textTypedClassNamespace.Text = "";
			this.toolTipMain.SetToolTip(this.textTypedClassNamespace, "If the object you are storing in this collection is defined in a different namesp" +
				"ace, type that namespace here (E.g. YourSpace)");
			this.textTypedClassNamespace.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.textTypedClassNamespace.TextChanged += new System.EventHandler(this.textTypedClassNamespace_TextChanged);
			this.textTypedClassNamespace.Enter += new System.EventHandler(this.textTypedClassNamespace_Enter);
			//
			// textCollectionNS
			//
			this.textCollectionNS.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textCollectionNS.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.errorProviderMain.SetIconAlignment(this.textCollectionNS, System.Windows.Forms.ErrorIconAlignment.MiddleLeft);
			this.errorProviderMain.SetIconPadding(this.textCollectionNS, 2);
			this.textCollectionNS.Location = new System.Drawing.Point(16, 240);
			this.textCollectionNS.Name = "textCollectionNS";
			this.textCollectionNS.Size = new System.Drawing.Size(248, 22);
			this.textCollectionNS.TabIndex = 12;
			this.textCollectionNS.Text = "";
			this.toolTipMain.SetToolTip(this.textCollectionNS, "Type the namespace in which this collection is defined (E.g. MySpace)");
			this.textCollectionNS.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.textCollectionNS.TextChanged += new System.EventHandler(this.textCollectionNS_TextChanged);
			this.textCollectionNS.Enter += new System.EventHandler(this.textCollectionNS_Enter);
			//
			// textFileName
			//
			this.textFileName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textFileName.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.errorProviderMain.SetIconAlignment(this.textFileName, System.Windows.Forms.ErrorIconAlignment.MiddleLeft);
			this.errorProviderMain.SetIconPadding(this.textFileName, 2);
			this.textFileName.Location = new System.Drawing.Point(16, 88);
			this.textFileName.Name = "textFileName";
			this.textFileName.Size = new System.Drawing.Size(248, 22);
			this.textFileName.TabIndex = 3;
			this.textFileName.Text = "";
			this.toolTipMain.SetToolTip(this.textFileName, "Type the file name you want to give this collection (E.g. CustomerCollection)");
			this.textFileName.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.textFileName.TextChanged += new System.EventHandler(this.textFileName_TextChanged);
			this.textFileName.Enter += new System.EventHandler(this.textFileName_Enter);
			//
			// authorName
			//
			this.authorName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.authorName.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.errorProviderMain.SetIconAlignment(this.authorName, System.Windows.Forms.ErrorIconAlignment.MiddleLeft);
			this.errorProviderMain.SetIconPadding(this.authorName, 2);
			this.authorName.Location = new System.Drawing.Point(16, 384);
			this.authorName.Name = "authorName";
			this.authorName.Size = new System.Drawing.Size(248, 22);
			this.authorName.TabIndex = 18;
			this.authorName.Text = "";
			this.toolTipMain.SetToolTip(this.authorName, "Your name (First name, first letter of last name (e.g. EberhardB)");
			this.authorName.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.authorName.TextChanged += new System.EventHandler(this.authorName_TextChanged);
			this.authorName.Enter += new System.EventHandler(this.authorName_Enter);
			//
			// textPreview
			//
			this.textPreview.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.textPreview.BackColor = System.Drawing.Color.White;
			this.textPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textPreview.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.textPreview.ForeColor = System.Drawing.Color.Black;
			this.textPreview.Location = new System.Drawing.Point(272, 32);
			this.textPreview.Multiline = true;
			this.textPreview.Name = "textPreview";
			this.textPreview.ReadOnly = true;
			this.textPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textPreview.Size = new System.Drawing.Size(512, 480);
			this.textPreview.TabIndex = 22;
			this.textPreview.Text = "";
			this.textPreview.WordWrap = false;
			//
			// checkNestedEnum
			//
			this.checkNestedEnum.Checked = true;
			this.checkNestedEnum.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkNestedEnum.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.checkNestedEnum.ForeColor = System.Drawing.SystemColors.ControlText;
			this.checkNestedEnum.Location = new System.Drawing.Point(136, 128);
			this.checkNestedEnum.Name = "checkNestedEnum";
			this.checkNestedEnum.Size = new System.Drawing.Size(144, 26);
			this.checkNestedEnum.TabIndex = 8;
			this.checkNestedEnum.Text = "Nested Enumerator";
			this.toolTipMain.SetToolTip(this.checkNestedEnum, "Generates a read only collection");
			this.checkNestedEnum.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.checkNestedEnum.Enter += new System.EventHandler(this.checkNestedEnum_Enter);
			this.checkNestedEnum.CheckedChanged += new System.EventHandler(this.OnControlValidating2);
			//
			// checkAddValidation
			//
			this.checkAddValidation.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.checkAddValidation.ForeColor = System.Drawing.SystemColors.ControlText;
			this.checkAddValidation.Location = new System.Drawing.Point(136, 152);
			this.checkAddValidation.Name = "checkAddValidation";
			this.checkAddValidation.Size = new System.Drawing.Size(112, 24);
			this.checkAddValidation.TabIndex = 9;
			this.checkAddValidation.Text = "Add Validation";
			this.toolTipMain.SetToolTip(this.checkAddValidation, "Adds stubs for item validation and customization");
			this.checkAddValidation.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.checkAddValidation.Enter += new System.EventHandler(this.checkAddValidation_Enter);
			this.checkAddValidation.CheckedChanged += new System.EventHandler(this.OnControlValidating2);
			//
			// buttonClear
			//
			this.buttonClear.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.buttonClear.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClear.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonClear.ForeColor = System.Drawing.SystemColors.ControlText;
			this.buttonClear.Location = new System.Drawing.Point(464, 520);
			this.buttonClear.Name = "buttonClear";
			this.buttonClear.TabIndex = 25;
			this.buttonClear.Text = "Clear";
			this.toolTipMain.SetToolTip(this.buttonClear, "Clears the fields, and resets the Build Process");
			this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
			//
			// buttonExample
			//
			this.buttonExample.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
			this.buttonExample.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonExample.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonExample.ForeColor = System.Drawing.SystemColors.ControlText;
			this.buttonExample.Location = new System.Drawing.Point(272, 520);
			this.buttonExample.Name = "buttonExample";
			this.buttonExample.Size = new System.Drawing.Size(104, 23);
			this.buttonExample.TabIndex = 23;
			this.buttonExample.Text = "Show Example";
			this.toolTipMain.SetToolTip(this.buttonExample, "Fills the Fields with example information");
			this.buttonExample.Click += new System.EventHandler(this.buttonExample_Click);
			//
			// optionJScript
			//
			this.optionJScript.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.optionJScript.ForeColor = System.Drawing.SystemColors.ControlText;
			this.optionJScript.Location = new System.Drawing.Point(8, 64);
			this.optionJScript.Name = "optionJScript";
			this.optionJScript.Size = new System.Drawing.Size(100, 23);
			this.optionJScript.TabIndex = 7;
			this.optionJScript.Text = "JScript";
			this.toolTipMain.SetToolTip(this.optionJScript, "Language to Generate");
			this.optionJScript.CheckedChanged += new System.EventHandler(this.optionJScript_CheckedChanged);
			//
			// optionVB
			//
			this.optionVB.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.optionVB.ForeColor = System.Drawing.SystemColors.ControlText;
			this.optionVB.Location = new System.Drawing.Point(8, 40);
			this.optionVB.Name = "optionVB";
			this.optionVB.Size = new System.Drawing.Size(100, 23);
			this.optionVB.TabIndex = 6;
			this.optionVB.Text = "Visual Basic";
			this.toolTipMain.SetToolTip(this.optionVB, "Language to Generate");
			this.optionVB.CheckedChanged += new System.EventHandler(this.optionVB_CheckedChanged);
			//
			// optionCS
			//
			this.optionCS.Checked = true;
			this.optionCS.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.optionCS.ForeColor = System.Drawing.SystemColors.ControlText;
			this.optionCS.Location = new System.Drawing.Point(8, 16);
			this.optionCS.Name = "optionCS";
			this.optionCS.Size = new System.Drawing.Size(100, 23);
			this.optionCS.TabIndex = 5;
			this.optionCS.TabStop = true;
			this.optionCS.Text = "C#";
			this.toolTipMain.SetToolTip(this.optionCS, "Language to Generate");
			this.optionCS.CheckedChanged += new System.EventHandler(this.optionCS_CheckedChanged);
			//
			// buttonClose
			//
			this.buttonClose.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.buttonClose.CausesValidation = false;
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.buttonClose.ForeColor = System.Drawing.SystemColors.ControlText;
			this.buttonClose.Location = new System.Drawing.Point(712, 520);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.TabIndex = 26;
			this.buttonClose.Text = "Close";
			this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
			//
			// labelFileName
			//
			this.labelFileName.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelFileName.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelFileName.Location = new System.Drawing.Point(16, 72);
			this.labelFileName.Name = "labelFileName";
			this.labelFileName.Size = new System.Drawing.Size(240, 24);
			this.labelFileName.TabIndex = 2;
			this.labelFileName.Text = "File Name:  (don\'t include extension)";
			//
			// labelPath
			//
			this.labelPath.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelPath.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelPath.Location = new System.Drawing.Point(13, 13);
			this.labelPath.Name = "labelPath";
			this.labelPath.Size = new System.Drawing.Size(235, 13);
			this.labelPath.TabIndex = 0;
			this.labelPath.Text = "Code Generation Path: ";
			//
			// labelTypeDefinition
			//
			this.labelTypeDefinition.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelTypeDefinition.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelTypeDefinition.Location = new System.Drawing.Point(16, 320);
			this.labelTypeDefinition.Name = "labelTypeDefinition";
			this.labelTypeDefinition.Size = new System.Drawing.Size(240, 26);
			this.labelTypeDefinition.TabIndex = 15;
			this.labelTypeDefinition.Text = "Type\'s Namespace: (optional)";
			//
			// labelCollectionSpace
			//
			this.labelCollectionSpace.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelCollectionSpace.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelCollectionSpace.Location = new System.Drawing.Point(16, 224);
			this.labelCollectionSpace.Name = "labelCollectionSpace";
			this.labelCollectionSpace.Size = new System.Drawing.Size(211, 26);
			this.labelCollectionSpace.TabIndex = 11;
			this.labelCollectionSpace.Text = "Collection\'s Namespace:";
			//
			// labelPreview
			//
			this.labelPreview.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelPreview.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelPreview.Location = new System.Drawing.Point(272, 15);
			this.labelPreview.Name = "labelPreview";
			this.labelPreview.Size = new System.Drawing.Size(179, 13);
			this.labelPreview.TabIndex = 21;
			this.labelPreview.Text = "Preview:";
			//
			// labelType
			//
			this.labelType.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelType.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelType.Location = new System.Drawing.Point(16, 272);
			this.labelType.Name = "labelType";
			this.labelType.Size = new System.Drawing.Size(211, 26);
			this.labelType.TabIndex = 13;
			this.labelType.Text = "Type Stored:";
			//
			// groupLanguage
			//
			this.groupLanguage.Controls.AddRange(new System.Windows.Forms.Control[] {
																						this.optionJScript,
																						this.optionVB,
																						this.optionCS});
			this.groupLanguage.Location = new System.Drawing.Point(16, 120);
			this.groupLanguage.Name = "groupLanguage";
			this.groupLanguage.Size = new System.Drawing.Size(112, 96);
			this.groupLanguage.TabIndex = 4;
			this.groupLanguage.TabStop = false;
			this.groupLanguage.Text = "Language";
			this.groupLanguage.Enter += new System.EventHandler(this.groupLanguage_Enter);
			//
			// buttonLocation
			//
			this.buttonLocation.Location = new System.Drawing.Point(240, 32);
			this.buttonLocation.Name = "buttonLocation";
			this.buttonLocation.Size = new System.Drawing.Size(24, 24);
			this.buttonLocation.TabIndex = 6;
			this.buttonLocation.Text = "...";
			this.buttonLocation.Click += new System.EventHandler(this.buttonLocation_Click);
			//
			// statusMain
			//
			this.statusMain.Anchor = ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.statusMain.Dock = System.Windows.Forms.DockStyle.None;
			this.statusMain.Location = new System.Drawing.Point(272, 549);
			this.statusMain.Name = "statusMain";
			this.statusMain.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
																						  this.panelMessage});
			this.statusMain.ShowPanels = true;
			this.statusMain.Size = new System.Drawing.Size(520, 24);
			this.statusMain.TabIndex = 0;
			//
			// panelMessage
			//
			this.panelMessage.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
			this.panelMessage.Width = 504;
			//
			// timerMain
			//
			this.timerMain.Interval = 6000;
			this.timerMain.Tick += new System.EventHandler(this.timerMain_Tick);
			//
			// helpMain
			//
			this.helpMain.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.helpMain.Location = new System.Drawing.Point(704, 8);
			this.helpMain.Name = "helpMain";
			this.helpMain.Size = new System.Drawing.Size(72, 24);
			this.helpMain.TabIndex = 5;
			this.helpMain.TabStop = true;
			this.helpMain.Text = "Show Help";
			this.helpMain.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.helpMain_LinkClicked);
			//
			// textEffect
			//
			this.textEffect.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left);
			this.textEffect.BackColor = System.Drawing.Color.White;
			this.textEffect.ForeColor = System.Drawing.Color.Navy;
			this.textEffect.Location = new System.Drawing.Point(16, 434);
			this.textEffect.Multiline = true;
			this.textEffect.Name = "textEffect";
			this.textEffect.ReadOnly = true;
			this.textEffect.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textEffect.Size = new System.Drawing.Size(248, 126);
			this.textEffect.TabIndex = 20;
			this.textEffect.Text = "";
			//
			// labelExplanation
			//
			this.labelExplanation.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelExplanation.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelExplanation.Location = new System.Drawing.Point(16, 418);
			this.labelExplanation.Name = "labelExplanation";
			this.labelExplanation.Size = new System.Drawing.Size(240, 26);
			this.labelExplanation.TabIndex = 19;
			this.labelExplanation.Text = "Explanation of Effect";
			//
			// label1
			//
			this.label1.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
			this.label1.Location = new System.Drawing.Point(16, 368);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(240, 26);
			this.label1.TabIndex = 17;
			this.label1.Text = "Your name: (optional)";
			//
			// checkAddDispose
			//
			this.checkAddDispose.Location = new System.Drawing.Point(136, 176);
			this.checkAddDispose.Name = "checkAddDispose";
			this.checkAddDispose.Size = new System.Drawing.Size(120, 24);
			this.checkAddDispose.TabIndex = 10;
			this.checkAddDispose.Text = "Add Dispose()";
			this.toolTipMain.SetToolTip(this.checkAddDispose, "Adds the IDisposable interface and a Dispose() method");
			this.checkAddDispose.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.checkAddDispose.Enter += new System.EventHandler(this.checkAddDispose_Enter);
			this.checkAddDispose.CheckedChanged += new System.EventHandler(this.OnControlValidating2);
			//
			// FormMain
			//
			this.AcceptButton = this.buttonGenerate;
			this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
			this.CancelButton = this.buttonClose;
			this.ClientSize = new System.Drawing.Size(792, 573);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.checkAddDispose,
																		  this.authorName,
																		  this.label1,
																		  this.textEffect,
																		  this.helpMain,
																		  this.statusMain,
																		  this.buttonLocation,
																		  this.groupLanguage,
																		  this.buttonExample,
																		  this.buttonClear,
																		  this.checkAddValidation,
																		  this.labelPreview,
																		  this.textPreview,
																		  this.checkNestedEnum,
																		  this.textFileName,
																		  this.labelFileName,
																		  this.buttonClose,
																		  this.buttonGenerate,
																		  this.textTypedClassNamespace,
																		  this.labelTypeDefinition,
																		  this.textCollectionName,
																		  this.labelType,
																		  this.textCollectionNS,
																		  this.labelCollectionSpace,
																		  this.textCodeGenPath,
																		  this.labelPath,
																		  this.labelExplanation});
			this.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(640, 480);
			this.Name = "FormMain";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Typed Collection Builder";
			this.Load += new System.EventHandler(this.FormMain_Load);
			this.Validating += new System.ComponentModel.CancelEventHandler(this.OnControlValidating);
			this.Closed += new System.EventHandler(this.FormMain_Closed);
			this.groupLanguage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.panelMessage)).EndInit();
			this.ResumeLayout(false);

		}


		private void SetupFileDialog()
		{
			sfd.InitialDirectory = Environment.CurrentDirectory;
			sfd.Filter = "All Files (*.*)|*.*|Source Files (*.cs, *.vb, *.js)|*.cs;*.vb;*.js|" +
				"C# Source Files (*.cs)|*.cs|VB Source Files (*.vb)|*.vb|JScript Source Files (*.js)|*.js";
			sfd.FilterIndex = 2;

		}

		protected void OnControlValidating2 (System.Object sender, System.EventArgs e)
		{
			if (ValidateEntries())
			{
				Generate(false);
			}
			else
			{
				textPreview.Text = "Invalid Selection";
			}
		}

		protected void OnControlValidating (System.Object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (ValidateEntries())
			{
				Generate(false);
			}
			else
			{
				textPreview.Text = "Invalid Selection";
			}
		}

		protected void buttonClose_Click(object sender, System.EventArgs e)
		{
			this.Close();

		}



		private TypedCollectionGenerator RetrieveValues()
		{
			TypedCollectionGenerator x = new TypedCollectionGenerator();
			x.FileName = this.textFileName.Text ;
			x.CollectionTypeName = this.textCollectionName.Text ;
			x.NameSpace = this.textCollectionNS.Text;
			x.CollectionTypeNameSpace = this.textTypedClassNamespace.Text;
			x.AuthorName = this.authorName.Text;
			x.AddValidation = this.checkAddValidation.Checked;
			x.AddDispose = this.checkAddDispose.Checked;
			x.GenerateEnumAsNested = this.checkNestedEnum.Checked;

			string lang = null;

			if(optionCS.Checked)
				lang = "CS";
			else if (optionVB.Checked)
				lang = "VB";
			else
				lang = "JSCRIPT";

			x.Language = lang;

			return x;
		}

		protected void buttonGenerate_Click(object sender, System.EventArgs e)
		{
			if (!ValidateEntries())
			{
				return;
			}

			TypedCollectionGenerator x = RetrieveValues();

			if (this.textCodeGenPath.Text.Length == 0)
				this.textCodeGenPath.Text = Environment.CurrentDirectory + "\\";

			try
			{
				x.Generate(this.textCodeGenPath.Text, fileExt);
				statusMain.Panels[0].Text = "File was successfully saved: " +
					this.textCodeGenPath.Text + "\\" + fileExt;
				timerMain.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show("An error occurred generating the output file:" +
					Environment.NewLine +
					ex.ToString(),"Generation Error", MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation);
			}
		}

		private void Generate(bool toFile)
		{
			if (!ValidateEntries())
			{
				return;
			}

			TypedCollectionGenerator x = RetrieveValues();

			if (this.textCodeGenPath.Text.Length == 0)
			{
				this.textCodeGenPath.Text = Environment.CurrentDirectory + "\\";
			}

			if (toFile)
			{
				x.Generate(this.textCodeGenPath.Text, fileExt);
			}
			else
			{
				textPreview.Text = x.GenerateToString();
			}
		}

		private bool ValidateEntries()
		{
			bool valid = true;
			/*
			if (includePath && textCodeGenPath.Text.Trim() == "")
			{
			//	errorProviderMain.SetError(textFileName, "Please provide a location to save the file");
				valid = false;
			}
			else
			{
				errorProviderMain.SetError(textFileName, "");
			}
			*/

			if (textFileName.Text.Trim() == "")
			{
				errorProviderMain.SetError(textFileName, "Please specify a name for the file");
				valid = false;
			}
			else
			{
				errorProviderMain.SetError(textFileName, "");
			}

			if (textCollectionName.Text.Trim() == "")
			{
				errorProviderMain.SetError(textCollectionName, "Please specify the name of the object being stored in this collection");
				valid = false;
			}
			else
			{
				errorProviderMain.SetError(textCollectionName, "");
			}

			if (textCollectionNS.Text.Trim() == "")
			{
				errorProviderMain.SetError(textCollectionNS, "Please specify the namespace in which this collection is defined");
				valid = false;
			}
			else
			{
				errorProviderMain.SetError(textCollectionNS, "");
			}

			buttonGenerate.Enabled = valid;

			return valid;
		}

		/*
		 * The main entry point for the application.
		 *
		 */
		[STAThread]
		public static void Main(string[] args)
		{
			Application.Run(new FormMain());
		}

		private void buttonExample_Click(object sender, System.EventArgs e)
		{
			if (textCodeGenPath.Text.Trim() != "" ||
				textCollectionName.Text.Trim() != "" ||
				textCollectionNS.Text.Trim() != "" ||
				textFileName.Text.Trim() != "" ||
				textTypedClassNamespace.Text.Trim() != "" ||
				authorName.Text.Trim() != "")
			{
				if (MessageBox.Show("This action will replace all existing entries with the example entries." + Environment.NewLine +
					"Are you sure you wish to continue?", "Populate Example Entries",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
				{
					return;
				}
			}

			textCodeGenPath.Text = originalDir;
			textFileName.Text = "CustomerCollection";
			textCollectionName.Text = "Customer";
			textCollectionNS.Text = "MySpace";
			textTypedClassNamespace.Text = "YourSpace";
			authorName.Text = "YourN";
			textCollectionNS.Focus();
			textTypedClassNamespace.Focus();
		}

		private void FormMain_Load(object sender, System.EventArgs e)
		{
			ValidateEntries();
			GetRegEntries();
		}

		private void textCodeGenPath_TextChanged(object sender, System.EventArgs e)
		{
			ValidateEntries();
		}

		private void textFileName_TextChanged(object sender, System.EventArgs e)
		{
			ValidateEntries();
		}

		private void textCollectionNS_TextChanged(object sender, System.EventArgs e)
		{
			OnControlValidating2(sender, e);
		}

		private void textCollectionName_TextChanged(object sender, System.EventArgs e)
		{
			OnControlValidating2(sender, e);
		}

		private void textCodeGenPath_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.CodeGenPathField];
		}

		private void textFileName_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.FileNameField];
		}

		private void textCollectionNS_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.CollectionNamespaceField];
		}

		private void textCollectionName_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.CollectionTypeField];
		}

		private void textTypedClassNamespace_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.CollectionTypeNamespaceField];
		}

		private void checkNestedEnum_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.NestedEnumField];
		}

		private void checkAddValidation_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.ValidationField];
		}

		private void checkAddDispose_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.DisposeField];
		}

		private void groupLanguage_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.LanguageField];
		}

		private void optionCS_CheckedChanged(object sender, System.EventArgs e)
		{
			OnControlValidating2(sender, e);
			fileExt = "";
		}

		private void optionVB_CheckedChanged(object sender, System.EventArgs e)
		{
			OnControlValidating2(sender, e);
			fileExt = "";
		}

		private void optionJScript_CheckedChanged(object sender, System.EventArgs e)
		{
			OnControlValidating2(sender, e);
			fileExt = "";
		}

		private void textTypedClassNamespace_TextChanged(object sender, System.EventArgs e)
		{
			OnControlValidating2(sender, e);
		}

		private void buttonLocation_Click(object sender, System.EventArgs e)
		{
			if (sfd.ShowDialog() == DialogResult.OK)
			{
				textCodeGenPath.Text = Path.GetDirectoryName(sfd.FileName);
				string fileTemp = Path.GetFileName(sfd.FileName);
				int extension = fileTemp.LastIndexOf(".");
				if (extension >= 0)
				{
					string ext = fileTemp.Substring(extension + 1);
					fileTemp = fileTemp.Substring(0,extension);
					fileExt = "";
					if (ext.ToLower() == "vb")
						optionVB.Select();
					else if (ext.ToLower() == "cs")
						optionCS.Select();
					else if (ext.ToLower() == "js")
						optionJScript.Select();
					else
						fileExt = ext;
				}

				textFileName.Text = fileTemp;
				textCollectionNS.Select();
			}
		}

		private void timerMain_Tick(object sender, System.EventArgs e)
		{
			statusMain.Panels[0].Text = "";
			timerMain.Stop();
		}

		private void helpMain_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			string[] strings = Environment.GetCommandLineArgs();
			System.Text.RegularExpressions.Regex r = new Regex(@"\\[^\\]+$", RegexOptions.Compiled);
			Process.Start("Explorer.exe", r.Replace(strings[0], "") + "\\Help\\Help.htm");
		}

		private void buttonClear_Click(object sender, System.EventArgs e)
		{
			textCodeGenPath.Text = "";
			textCollectionName.Text = "";
			textCollectionNS.Text = "";
			textEffect.Text = "";
			textFileName.Text = "";
			textPreview.Text = "";
			textTypedClassNamespace.Text = "";
			authorName.Text = "";
			fileExt = "";
			textCodeGenPath.Select();
		}

		private void FormMain_Closed(object sender, System.EventArgs e)
		{
			SaveRegEntries();
		}

		private void GetRegEntries()
		{
			RegistryKey rk = Registry.CurrentUser;
			RegistryKey collGenKey = rk.OpenSubKey(@"Software\CollGen");

			if (collGenKey != null)
			{
				string lang = Convert.ToString(collGenKey.GetValue("language"));

				switch (lang)
				{
					case "CS":
						//take no action, this is the default
						break;
					case "VB":
						optionVB.Select();
						break;
					case "JS":
						optionJScript.Select();
						break;
				}

				checkNestedEnum.Checked = Convert.ToBoolean(collGenKey.GetValue("nestedEnum"));
				checkAddValidation.Checked = Convert.ToBoolean(collGenKey.GetValue("AddValid"));
				checkAddDispose.Checked = Convert.ToBoolean(collGenKey.GetValue("AddDispose"));
			}
		}

		private void SaveRegEntries()
		{
			RegistryKey rk = Registry.CurrentUser;
			RegistryKey collGenKey = rk.CreateSubKey(@"Software\CollGen");

			string lang = (optionCS.Checked == true ? "CS" : optionVB.Checked == true ? "VB" : "JS");

			collGenKey.SetValue("language", lang);
			collGenKey.SetValue("nestedEnum", checkNestedEnum.Checked);
			collGenKey.SetValue("AddValid", checkAddValidation.Checked);
			collGenKey.SetValue("AddDispose", checkAddDispose.Checked);
		}

		private void authorName_TextChanged(object sender, System.EventArgs e)
		{
			OnControlValidating2(sender, e);
		}

		private void authorName_Enter(object sender, System.EventArgs e)
		{
			textEffect.Text = (String)htEffects[Effect.AuthorNameField];
		}

	}
}
