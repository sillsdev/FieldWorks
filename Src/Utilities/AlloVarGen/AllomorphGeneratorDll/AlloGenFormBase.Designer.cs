using System;
using System.Windows.Forms;

namespace SIL.AllomorphGenerator
{
    partial class AlloGenFormBase
    {

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
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
        protected void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlloGenFormBase));
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabEditOps = new System.Windows.Forms.TabPage();
			this.lbApplyTo = new System.Windows.Forms.Label();
			this.cbApplyTo = new System.Windows.Forms.ComboBox();
			this.lbCountOps = new System.Windows.Forms.Label();
			this.btnSaveChanges = new System.Windows.Forms.Button();
			this.lbOpRightClickToEdit = new System.Windows.Forms.Label();
			this.lbOperations = new System.Windows.Forms.Label();
			this.lbPattern = new System.Windows.Forms.Label();
			this.lBoxOperations = new System.Windows.Forms.ListBox();
			this.lbName = new System.Windows.Forms.Label();
			this.lbDescription = new System.Windows.Forms.Label();
			this.tbName = new System.Windows.Forms.TextBox();
			this.tbDescription = new System.Windows.Forms.TextBox();
			this.lbAction = new System.Windows.Forms.Label();
			this.plPattern = new System.Windows.Forms.Panel();
			this.btnMatch = new System.Windows.Forms.Button();
			this.btnMorphTypes = new System.Windows.Forms.Button();
			this.btnCategory = new System.Windows.Forms.Button();
			this.tbCategory = new System.Windows.Forms.TextBox();
			this.lbCategory = new System.Windows.Forms.Label();
			this.lBoxMorphTypes = new System.Windows.Forms.ListBox();
			this.lbMorphTypes = new System.Windows.Forms.Label();
			this.tbMatch = new System.Windows.Forms.TextBox();
			this.lbMatch = new System.Windows.Forms.Label();
			this.plActions = new System.Windows.Forms.Panel();
			this.lbReplaceRightClickToEdit = new System.Windows.Forms.Label();
			this.btnEnvironments = new System.Windows.Forms.Button();
			this.lbReplaceOps = new System.Windows.Forms.Label();
			this.btnStemName = new System.Windows.Forms.Button();
			this.tbStemName = new System.Windows.Forms.TextBox();
			this.lbStemName = new System.Windows.Forms.Label();
			this.lBoxReplaceOps = new System.Windows.Forms.ListBox();
			this.lBoxEnvironments = new System.Windows.Forms.ListBox();
			this.lbEnvironments = new System.Windows.Forms.Label();
			this.tabRunOps = new System.Windows.Forms.TabPage();
			this.btnSaveChanges2 = new System.Windows.Forms.Button();
			this.lvOperations = new System.Windows.Forms.ListView();
			this.lbCountRunOps = new System.Windows.Forms.Label();
			this.lvPreview = new System.Windows.Forms.ListView();
			this.lbPreview = new System.Windows.Forms.Label();
			this.btnApplyOperations = new System.Windows.Forms.Button();
			this.lbOperationsToApply = new System.Windows.Forms.Label();
			this.tabEditReplaceOps = new System.Windows.Forms.TabPage();
			this.lbCountReplaceOps = new System.Windows.Forms.Label();
			this.btnSaveChanges3 = new System.Windows.Forms.Button();
			this.btnDeleteReplaceOp = new System.Windows.Forms.Button();
			this.btnEditReplaceOp = new System.Windows.Forms.Button();
			this.btnAddNewReplaceOp = new System.Windows.Forms.Button();
			this.lvEditReplaceOps = new System.Windows.Forms.ListView();
			this.lbAlloGenFile = new System.Windows.Forms.Label();
			this.tbFile = new System.Windows.Forms.TextBox();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnNewFile = new System.Windows.Forms.Button();
			this.ilPreview = new System.Windows.Forms.ImageList(this.components);
			this.tabControl.SuspendLayout();
			this.tabEditOps.SuspendLayout();
			this.plPattern.SuspendLayout();
			this.plActions.SuspendLayout();
			this.tabRunOps.SuspendLayout();
			this.tabEditReplaceOps.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabEditOps);
			this.tabControl.Controls.Add(this.tabRunOps);
			this.tabControl.Controls.Add(this.tabEditReplaceOps);
			this.tabControl.Location = new System.Drawing.Point(0, 68);
			this.tabControl.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(859, 586);
			this.tabControl.TabIndex = 5;
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			// 
			// tabEditOps
			// 
			this.tabEditOps.Controls.Add(this.lbApplyTo);
			this.tabEditOps.Controls.Add(this.cbApplyTo);
			this.tabEditOps.Controls.Add(this.lbCountOps);
			this.tabEditOps.Controls.Add(this.btnSaveChanges);
			this.tabEditOps.Controls.Add(this.lbOpRightClickToEdit);
			this.tabEditOps.Controls.Add(this.lbOperations);
			this.tabEditOps.Controls.Add(this.lbPattern);
			this.tabEditOps.Controls.Add(this.lBoxOperations);
			this.tabEditOps.Controls.Add(this.lbName);
			this.tabEditOps.Controls.Add(this.lbDescription);
			this.tabEditOps.Controls.Add(this.tbName);
			this.tabEditOps.Controls.Add(this.tbDescription);
			this.tabEditOps.Controls.Add(this.lbAction);
			this.tabEditOps.Controls.Add(this.plPattern);
			this.tabEditOps.Controls.Add(this.plActions);
			this.tabEditOps.Location = new System.Drawing.Point(4, 22);
			this.tabEditOps.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tabEditOps.Name = "tabEditOps";
			this.tabEditOps.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tabEditOps.Size = new System.Drawing.Size(851, 560);
			this.tabEditOps.TabIndex = 0;
			this.tabEditOps.Text = "Edit Operations";
			this.tabEditOps.UseVisualStyleBackColor = true;
			// 
			// lbApplyTo
			// 
			this.lbApplyTo.AutoSize = true;
			this.lbApplyTo.Location = new System.Drawing.Point(5, 517);
			this.lbApplyTo.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbApplyTo.Name = "lbApplyTo";
			this.lbApplyTo.Size = new System.Drawing.Size(100, 13);
			this.lbApplyTo.TabIndex = 31;
			this.lbApplyTo.Text = "Apply operations to:";
			// 
			// cbApplyTo
			// 
			this.cbApplyTo.FormattingEnabled = true;
			this.cbApplyTo.Location = new System.Drawing.Point(134, 515);
			this.cbApplyTo.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.cbApplyTo.Name = "cbApplyTo";
			this.cbApplyTo.Size = new System.Drawing.Size(106, 21);
			this.cbApplyTo.TabIndex = 6;
			this.cbApplyTo.SelectedIndexChanged += new System.EventHandler(this.cbApplyTo_SelectedIndexChanged);
			// 
			// lbCountOps
			// 
			this.lbCountOps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbCountOps.AutoSize = true;
			this.lbCountOps.Location = new System.Drawing.Point(773, 517);
			this.lbCountOps.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbCountOps.Name = "lbCountOps";
			this.lbCountOps.Size = new System.Drawing.Size(42, 13);
			this.lbCountOps.TabIndex = 30;
			this.lbCountOps.Text = "1 / 160";
			// 
			// btnSaveChanges
			// 
			this.btnSaveChanges.Location = new System.Drawing.Point(338, 517);
			this.btnSaveChanges.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnSaveChanges.Name = "btnSaveChanges";
			this.btnSaveChanges.Size = new System.Drawing.Size(87, 22);
			this.btnSaveChanges.TabIndex = 9;
			this.btnSaveChanges.Text = "&Save Changes";
			this.btnSaveChanges.UseVisualStyleBackColor = true;
			this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
			// 
			// lbOpRightClickToEdit
			// 
			this.lbOpRightClickToEdit.AutoSize = true;
			this.lbOpRightClickToEdit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbOpRightClickToEdit.Location = new System.Drawing.Point(73, 7);
			this.lbOpRightClickToEdit.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbOpRightClickToEdit.Name = "lbOpRightClickToEdit";
			this.lbOpRightClickToEdit.Size = new System.Drawing.Size(95, 13);
			this.lbOpRightClickToEdit.TabIndex = 29;
			this.lbOpRightClickToEdit.Text = "(Right-click to edit)";
			// 
			// lbOperations
			// 
			this.lbOperations.AutoSize = true;
			this.lbOperations.Location = new System.Drawing.Point(5, 8);
			this.lbOperations.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbOperations.Name = "lbOperations";
			this.lbOperations.Size = new System.Drawing.Size(58, 13);
			this.lbOperations.TabIndex = 28;
			this.lbOperations.Text = "Operations";
			// 
			// lbPattern
			// 
			this.lbPattern.AutoSize = true;
			this.lbPattern.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbPattern.Location = new System.Drawing.Point(251, 65);
			this.lbPattern.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbPattern.Name = "lbPattern";
			this.lbPattern.Size = new System.Drawing.Size(48, 13);
			this.lbPattern.TabIndex = 6;
			this.lbPattern.Text = "Pattern";
			// 
			// lBoxOperations
			// 
			this.lBoxOperations.FormattingEnabled = true;
			this.lBoxOperations.Location = new System.Drawing.Point(2, 27);
			this.lBoxOperations.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.lBoxOperations.Name = "lBoxOperations";
			this.lBoxOperations.Size = new System.Drawing.Size(238, 472);
			this.lBoxOperations.TabIndex = 0;
			this.lBoxOperations.SelectedIndexChanged += new System.EventHandler(this.lBoxOperations_SelectedIndexChanged);
			this.lBoxOperations.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lBoxOperations_MouseUp);
			// 
			// lbName
			// 
			this.lbName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbName.Location = new System.Drawing.Point(243, 4);
			this.lbName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbName.Name = "lbName";
			this.lbName.Size = new System.Drawing.Size(59, 17);
			this.lbName.TabIndex = 1;
			this.lbName.Text = "Name";
			// 
			// lbDescription
			// 
			this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbDescription.Location = new System.Drawing.Point(243, 31);
			this.lbDescription.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbDescription.Name = "lbDescription";
			this.lbDescription.Size = new System.Drawing.Size(77, 13);
			this.lbDescription.TabIndex = 3;
			this.lbDescription.Text = "Description";
			// 
			// tbName
			// 
			this.tbName.Location = new System.Drawing.Point(331, 2);
			this.tbName.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tbName.Name = "tbName";
			this.tbName.Size = new System.Drawing.Size(433, 20);
			this.tbName.TabIndex = 2;
			this.tbName.Text = "My name";
			this.tbName.TextChanged += new System.EventHandler(this.tbName_TextChanged);
			// 
			// tbDescription
			// 
			this.tbDescription.Location = new System.Drawing.Point(331, 27);
			this.tbDescription.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tbDescription.Name = "tbDescription";
			this.tbDescription.Size = new System.Drawing.Size(433, 20);
			this.tbDescription.TabIndex = 4;
			this.tbDescription.Text = "My description";
			this.tbDescription.TextChanged += new System.EventHandler(this.tbDescription_TextChanged);
			// 
			// lbAction
			// 
			this.lbAction.AutoSize = true;
			this.lbAction.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbAction.Location = new System.Drawing.Point(253, 248);
			this.lbAction.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbAction.Name = "lbAction";
			this.lbAction.Size = new System.Drawing.Size(49, 13);
			this.lbAction.TabIndex = 8;
			this.lbAction.Text = "Actions";
			// 
			// plPattern
			// 
			this.plPattern.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.plPattern.Controls.Add(this.btnMatch);
			this.plPattern.Controls.Add(this.btnMorphTypes);
			this.plPattern.Controls.Add(this.btnCategory);
			this.plPattern.Controls.Add(this.tbCategory);
			this.plPattern.Controls.Add(this.lbCategory);
			this.plPattern.Controls.Add(this.lBoxMorphTypes);
			this.plPattern.Controls.Add(this.lbMorphTypes);
			this.plPattern.Controls.Add(this.tbMatch);
			this.plPattern.Controls.Add(this.lbMatch);
			this.plPattern.Location = new System.Drawing.Point(247, 57);
			this.plPattern.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.plPattern.Name = "plPattern";
			this.plPattern.Size = new System.Drawing.Size(518, 166);
			this.plPattern.TabIndex = 5;
			// 
			// btnMatch
			// 
			this.btnMatch.Location = new System.Drawing.Point(337, 9);
			this.btnMatch.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnMatch.Name = "btnMatch";
			this.btnMatch.Size = new System.Drawing.Size(96, 21);
			this.btnMatch.TabIndex = 2;
			this.btnMatch.Text = "&Define match";
			this.btnMatch.UseVisualStyleBackColor = true;
			this.btnMatch.Click += new System.EventHandler(this.btnMatch_Click);
			// 
			// btnMorphTypes
			// 
			this.btnMorphTypes.Location = new System.Drawing.Point(356, 42);
			this.btnMorphTypes.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnMorphTypes.Name = "btnMorphTypes";
			this.btnMorphTypes.Size = new System.Drawing.Size(39, 18);
			this.btnMorphTypes.TabIndex = 5;
			this.btnMorphTypes.Text = "&Edit";
			this.btnMorphTypes.UseVisualStyleBackColor = true;
			this.btnMorphTypes.Click += new System.EventHandler(this.btnMorphTypes_Click);
			// 
			// btnCategory
			// 
			this.btnCategory.Location = new System.Drawing.Point(474, 133);
			this.btnCategory.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnCategory.Name = "btnCategory";
			this.btnCategory.Size = new System.Drawing.Size(20, 18);
			this.btnCategory.TabIndex = 8;
			this.btnCategory.Text = "...";
			this.btnCategory.UseVisualStyleBackColor = true;
			this.btnCategory.Click += new System.EventHandler(this.btnCategory_Click);
			// 
			// tbCategory
			// 
			this.tbCategory.Location = new System.Drawing.Point(165, 135);
			this.tbCategory.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tbCategory.Multiline = true;
			this.tbCategory.Name = "tbCategory";
			this.tbCategory.ReadOnly = true;
			this.tbCategory.Size = new System.Drawing.Size(285, 18);
			this.tbCategory.TabIndex = 7;
			// 
			// lbCategory
			// 
			this.lbCategory.AutoSize = true;
			this.lbCategory.Location = new System.Drawing.Point(85, 135);
			this.lbCategory.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbCategory.Name = "lbCategory";
			this.lbCategory.Size = new System.Drawing.Size(49, 13);
			this.lbCategory.TabIndex = 6;
			this.lbCategory.Text = "Category";
			// 
			// lBoxMorphTypes
			// 
			this.lBoxMorphTypes.Enabled = false;
			this.lBoxMorphTypes.FormattingEnabled = true;
			this.lBoxMorphTypes.Location = new System.Drawing.Point(165, 42);
			this.lBoxMorphTypes.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.lBoxMorphTypes.Name = "lBoxMorphTypes";
			this.lBoxMorphTypes.Size = new System.Drawing.Size(177, 82);
			this.lBoxMorphTypes.Sorted = true;
			this.lBoxMorphTypes.TabIndex = 4;
			// 
			// lbMorphTypes
			// 
			this.lbMorphTypes.AutoSize = true;
			this.lbMorphTypes.Location = new System.Drawing.Point(85, 42);
			this.lbMorphTypes.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbMorphTypes.Name = "lbMorphTypes";
			this.lbMorphTypes.Size = new System.Drawing.Size(69, 13);
			this.lbMorphTypes.TabIndex = 3;
			this.lbMorphTypes.Text = "Morph Types";
			// 
			// tbMatch
			// 
			this.tbMatch.Enabled = false;
			this.tbMatch.Location = new System.Drawing.Point(165, 9);
			this.tbMatch.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tbMatch.Name = "tbMatch";
			this.tbMatch.Size = new System.Drawing.Size(137, 20);
			this.tbMatch.TabIndex = 1;
			this.tbMatch.Text = "My match";
			// 
			// lbMatch
			// 
			this.lbMatch.AutoSize = true;
			this.lbMatch.Location = new System.Drawing.Point(85, 8);
			this.lbMatch.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbMatch.Name = "lbMatch";
			this.lbMatch.Size = new System.Drawing.Size(37, 13);
			this.lbMatch.TabIndex = 0;
			this.lbMatch.Text = "Match";
			// 
			// plActions
			// 
			this.plActions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.plActions.Controls.Add(this.lbReplaceRightClickToEdit);
			this.plActions.Controls.Add(this.btnEnvironments);
			this.plActions.Controls.Add(this.lbReplaceOps);
			this.plActions.Controls.Add(this.btnStemName);
			this.plActions.Controls.Add(this.tbStemName);
			this.plActions.Controls.Add(this.lbStemName);
			this.plActions.Controls.Add(this.lBoxReplaceOps);
			this.plActions.Controls.Add(this.lBoxEnvironments);
			this.plActions.Controls.Add(this.lbEnvironments);
			this.plActions.Location = new System.Drawing.Point(248, 240);
			this.plActions.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.plActions.Name = "plActions";
			this.plActions.Size = new System.Drawing.Size(516, 259);
			this.plActions.TabIndex = 7;
			// 
			// lbReplaceRightClickToEdit
			// 
			this.lbReplaceRightClickToEdit.AutoSize = true;
			this.lbReplaceRightClickToEdit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lbReplaceRightClickToEdit.Location = new System.Drawing.Point(203, 3);
			this.lbReplaceRightClickToEdit.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbReplaceRightClickToEdit.Name = "lbReplaceRightClickToEdit";
			this.lbReplaceRightClickToEdit.Size = new System.Drawing.Size(95, 13);
			this.lbReplaceRightClickToEdit.TabIndex = 1;
			this.lbReplaceRightClickToEdit.Text = "(Right-click to edit)";
			// 
			// btnEnvironments
			// 
			this.btnEnvironments.Location = new System.Drawing.Point(399, 119);
			this.btnEnvironments.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnEnvironments.Name = "btnEnvironments";
			this.btnEnvironments.Size = new System.Drawing.Size(33, 20);
			this.btnEnvironments.TabIndex = 5;
			this.btnEnvironments.Text = "Ed&it";
			this.btnEnvironments.UseVisualStyleBackColor = true;
			this.btnEnvironments.Click += new System.EventHandler(this.btnEnvironments_Click);
			// 
			// lbReplaceOps
			// 
			this.lbReplaceOps.AutoSize = true;
			this.lbReplaceOps.Location = new System.Drawing.Point(83, 4);
			this.lbReplaceOps.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbReplaceOps.Name = "lbReplaceOps";
			this.lbReplaceOps.Size = new System.Drawing.Size(99, 13);
			this.lbReplaceOps.TabIndex = 0;
			this.lbReplaceOps.Text = "Replace operations";
			// 
			// btnStemName
			// 
			this.btnStemName.Location = new System.Drawing.Point(480, 214);
			this.btnStemName.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnStemName.Name = "btnStemName";
			this.btnStemName.Size = new System.Drawing.Size(20, 18);
			this.btnStemName.TabIndex = 8;
			this.btnStemName.Text = "...";
			this.btnStemName.UseVisualStyleBackColor = true;
			this.btnStemName.Click += new System.EventHandler(this.btnStemName_Click);
			// 
			// tbStemName
			// 
			this.tbStemName.Location = new System.Drawing.Point(192, 214);
			this.tbStemName.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tbStemName.Name = "tbStemName";
			this.tbStemName.ReadOnly = true;
			this.tbStemName.Size = new System.Drawing.Size(285, 20);
			this.tbStemName.TabIndex = 7;
			// 
			// lbStemName
			// 
			this.lbStemName.AutoSize = true;
			this.lbStemName.Location = new System.Drawing.Point(81, 214);
			this.lbStemName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbStemName.Name = "lbStemName";
			this.lbStemName.Size = new System.Drawing.Size(60, 13);
			this.lbStemName.TabIndex = 6;
			this.lbStemName.Text = "Stem name";
			// 
			// lBoxReplaceOps
			// 
			this.lBoxReplaceOps.FormattingEnabled = true;
			this.lBoxReplaceOps.Location = new System.Drawing.Point(83, 21);
			this.lBoxReplaceOps.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.lBoxReplaceOps.Name = "lBoxReplaceOps";
			this.lBoxReplaceOps.Size = new System.Drawing.Size(418, 82);
			this.lBoxReplaceOps.TabIndex = 2;
			this.lBoxReplaceOps.DoubleClick += new System.EventHandler(this.lBoxReplaceOps_DoubleClick);
			this.lBoxReplaceOps.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lBoxReplaceOps_MouseUp);
			// 
			// lBoxEnvironments
			// 
			this.lBoxEnvironments.Enabled = false;
			this.lBoxEnvironments.FormattingEnabled = true;
			this.lBoxEnvironments.Location = new System.Drawing.Point(177, 119);
			this.lBoxEnvironments.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.lBoxEnvironments.Name = "lBoxEnvironments";
			this.lBoxEnvironments.Size = new System.Drawing.Size(207, 82);
			this.lBoxEnvironments.TabIndex = 4;
			// 
			// lbEnvironments
			// 
			this.lbEnvironments.AutoSize = true;
			this.lbEnvironments.Location = new System.Drawing.Point(83, 119);
			this.lbEnvironments.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbEnvironments.Name = "lbEnvironments";
			this.lbEnvironments.Size = new System.Drawing.Size(71, 13);
			this.lbEnvironments.TabIndex = 3;
			this.lbEnvironments.Text = "Environments";
			// 
			// tabRunOps
			// 
			this.tabRunOps.Controls.Add(this.btnSaveChanges2);
			this.tabRunOps.Controls.Add(this.lvOperations);
			this.tabRunOps.Controls.Add(this.lbCountRunOps);
			this.tabRunOps.Controls.Add(this.lvPreview);
			this.tabRunOps.Controls.Add(this.lbPreview);
			this.tabRunOps.Controls.Add(this.btnApplyOperations);
			this.tabRunOps.Controls.Add(this.lbOperationsToApply);
			this.tabRunOps.Location = new System.Drawing.Point(4, 22);
			this.tabRunOps.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tabRunOps.Name = "tabRunOps";
			this.tabRunOps.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tabRunOps.Size = new System.Drawing.Size(851, 560);
			this.tabRunOps.TabIndex = 1;
			this.tabRunOps.Text = "Run Operations";
			this.tabRunOps.UseVisualStyleBackColor = true;
			// 
			// btnSaveChanges2
			// 
			this.btnSaveChanges2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnSaveChanges2.Location = new System.Drawing.Point(302, 502);
			this.btnSaveChanges2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnSaveChanges2.Name = "btnSaveChanges2";
			this.btnSaveChanges2.Size = new System.Drawing.Size(87, 22);
			this.btnSaveChanges2.TabIndex = 5;
			this.btnSaveChanges2.Text = "&Save Changes";
			this.btnSaveChanges2.UseVisualStyleBackColor = true;
			this.btnSaveChanges2.Click += new System.EventHandler(this.btnSaveChanges2_Click);
			// 
			// lvOperations
			// 
			this.lvOperations.CheckBoxes = true;
			this.lvOperations.FullRowSelect = true;
			this.lvOperations.GridLines = true;
			this.lvOperations.HideSelection = false;
			this.lvOperations.Location = new System.Drawing.Point(15, 32);
			this.lvOperations.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.lvOperations.MultiSelect = false;
			this.lvOperations.Name = "lvOperations";
			this.lvOperations.Size = new System.Drawing.Size(237, 436);
			this.lvOperations.TabIndex = 1;
			this.lvOperations.UseCompatibleStateImageBehavior = false;
			this.lvOperations.View = System.Windows.Forms.View.Details;
			this.lvOperations.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvOperations_ColumnClick);
			this.lvOperations.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvOperations_ItemChecked);
			this.lvOperations.SelectedIndexChanged += new System.EventHandler(this.lvOperations_SelectedIndexChanged);
			// 
			// lbCountRunOps
			// 
			this.lbCountRunOps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.lbCountRunOps.AutoSize = true;
			this.lbCountRunOps.Location = new System.Drawing.Point(803, 499);
			this.lbCountRunOps.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbCountRunOps.Name = "lbCountRunOps";
			this.lbCountRunOps.Size = new System.Drawing.Size(42, 13);
			this.lbCountRunOps.TabIndex = 6;
			this.lbCountRunOps.Text = "1 / 160";
			// 
			// lvPreview
			// 
			this.lvPreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lvPreview.CheckBoxes = true;
			this.lvPreview.FullRowSelect = true;
			this.lvPreview.GridLines = true;
			this.lvPreview.HideSelection = false;
			this.lvPreview.Location = new System.Drawing.Point(255, 32);
			this.lvPreview.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.lvPreview.Name = "lvPreview";
			this.lvPreview.Size = new System.Drawing.Size(583, 436);
			this.lvPreview.TabIndex = 3;
			this.lvPreview.UseCompatibleStateImageBehavior = false;
			this.lvPreview.View = System.Windows.Forms.View.Details;
			this.lvPreview.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvPreview_ColumnClick);
			// 
			// lbPreview
			// 
			this.lbPreview.AutoSize = true;
			this.lbPreview.Location = new System.Drawing.Point(260, 14);
			this.lbPreview.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbPreview.Name = "lbPreview";
			this.lbPreview.Size = new System.Drawing.Size(147, 13);
			this.lbPreview.TabIndex = 2;
			this.lbPreview.Text = "Preview of selected operation";
			// 
			// btnApplyOperations
			// 
			this.btnApplyOperations.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnApplyOperations.Location = new System.Drawing.Point(15, 495);
			this.btnApplyOperations.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnApplyOperations.Name = "btnApplyOperations";
			this.btnApplyOperations.Size = new System.Drawing.Size(225, 38);
			this.btnApplyOperations.TabIndex = 4;
			this.btnApplyOperations.Text = "Apply Checked Operations";
			this.btnApplyOperations.UseVisualStyleBackColor = true;
			this.btnApplyOperations.Click += new System.EventHandler(this.btnApplyOperations_Click);
			// 
			// lbOperationsToApply
			// 
			this.lbOperationsToApply.AutoSize = true;
			this.lbOperationsToApply.Location = new System.Drawing.Point(13, 14);
			this.lbOperationsToApply.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbOperationsToApply.Name = "lbOperationsToApply";
			this.lbOperationsToApply.Size = new System.Drawing.Size(98, 13);
			this.lbOperationsToApply.TabIndex = 0;
			this.lbOperationsToApply.Text = "Operations to apply";
			// 
			// tabEditReplaceOps
			// 
			this.tabEditReplaceOps.Controls.Add(this.lbCountReplaceOps);
			this.tabEditReplaceOps.Controls.Add(this.btnSaveChanges3);
			this.tabEditReplaceOps.Controls.Add(this.btnDeleteReplaceOp);
			this.tabEditReplaceOps.Controls.Add(this.btnEditReplaceOp);
			this.tabEditReplaceOps.Controls.Add(this.btnAddNewReplaceOp);
			this.tabEditReplaceOps.Controls.Add(this.lvEditReplaceOps);
			this.tabEditReplaceOps.Location = new System.Drawing.Point(4, 22);
			this.tabEditReplaceOps.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tabEditReplaceOps.Name = "tabEditReplaceOps";
			this.tabEditReplaceOps.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tabEditReplaceOps.Size = new System.Drawing.Size(851, 560);
			this.tabEditReplaceOps.TabIndex = 2;
			this.tabEditReplaceOps.Text = "Edit Replace Operations";
			this.tabEditReplaceOps.UseVisualStyleBackColor = true;
			// 
			// lbCountReplaceOps
			// 
			this.lbCountReplaceOps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.lbCountReplaceOps.AutoSize = true;
			this.lbCountReplaceOps.Location = new System.Drawing.Point(799, 512);
			this.lbCountReplaceOps.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbCountReplaceOps.Name = "lbCountReplaceOps";
			this.lbCountReplaceOps.Size = new System.Drawing.Size(42, 13);
			this.lbCountReplaceOps.TabIndex = 7;
			this.lbCountReplaceOps.Text = "1 / 160";
			// 
			// btnSaveChanges3
			// 
			this.btnSaveChanges3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnSaveChanges3.Location = new System.Drawing.Point(444, 511);
			this.btnSaveChanges3.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnSaveChanges3.Name = "btnSaveChanges3";
			this.btnSaveChanges3.Size = new System.Drawing.Size(87, 22);
			this.btnSaveChanges3.TabIndex = 6;
			this.btnSaveChanges3.Text = "&Save Changes";
			this.btnSaveChanges3.UseVisualStyleBackColor = true;
			this.btnSaveChanges3.Click += new System.EventHandler(this.btnSaveChanges3_Click);
			// 
			// btnDeleteReplaceOp
			// 
			this.btnDeleteReplaceOp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnDeleteReplaceOp.Location = new System.Drawing.Point(291, 505);
			this.btnDeleteReplaceOp.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnDeleteReplaceOp.Name = "btnDeleteReplaceOp";
			this.btnDeleteReplaceOp.Size = new System.Drawing.Size(95, 28);
			this.btnDeleteReplaceOp.TabIndex = 3;
			this.btnDeleteReplaceOp.Text = "&Delete";
			this.btnDeleteReplaceOp.UseVisualStyleBackColor = true;
			this.btnDeleteReplaceOp.Click += new System.EventHandler(this.btnDeleteReplaceOp_Click);
			// 
			// btnEditReplaceOp
			// 
			this.btnEditReplaceOp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnEditReplaceOp.Location = new System.Drawing.Point(4, 505);
			this.btnEditReplaceOp.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnEditReplaceOp.Name = "btnEditReplaceOp";
			this.btnEditReplaceOp.Size = new System.Drawing.Size(95, 28);
			this.btnEditReplaceOp.TabIndex = 2;
			this.btnEditReplaceOp.Text = "&Edit";
			this.btnEditReplaceOp.UseVisualStyleBackColor = true;
			this.btnEditReplaceOp.Click += new System.EventHandler(this.btnEditReplaceOp_Click);
			// 
			// btnAddNewReplaceOp
			// 
			this.btnAddNewReplaceOp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnAddNewReplaceOp.Location = new System.Drawing.Point(147, 505);
			this.btnAddNewReplaceOp.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnAddNewReplaceOp.Name = "btnAddNewReplaceOp";
			this.btnAddNewReplaceOp.Size = new System.Drawing.Size(95, 28);
			this.btnAddNewReplaceOp.TabIndex = 1;
			this.btnAddNewReplaceOp.Text = "&Add new";
			this.btnAddNewReplaceOp.UseVisualStyleBackColor = true;
			this.btnAddNewReplaceOp.Click += new System.EventHandler(this.btnAddNewReplaceOp_Click);
			// 
			// lvEditReplaceOps
			// 
			this.lvEditReplaceOps.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lvEditReplaceOps.FullRowSelect = true;
			this.lvEditReplaceOps.GridLines = true;
			this.lvEditReplaceOps.HideSelection = false;
			this.lvEditReplaceOps.Location = new System.Drawing.Point(4, 10);
			this.lvEditReplaceOps.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.lvEditReplaceOps.MultiSelect = false;
			this.lvEditReplaceOps.Name = "lvEditReplaceOps";
			this.lvEditReplaceOps.Size = new System.Drawing.Size(846, 468);
			this.lvEditReplaceOps.TabIndex = 0;
			this.lvEditReplaceOps.UseCompatibleStateImageBehavior = false;
			this.lvEditReplaceOps.View = System.Windows.Forms.View.Details;
			this.lvEditReplaceOps.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvEditReplaceOps_ColumnClick);
			this.lvEditReplaceOps.SelectedIndexChanged += new System.EventHandler(this.lvEditReplaceOps_SelectedIndexChanged);
			this.lvEditReplaceOps.DoubleClick += new System.EventHandler(this.lvEditReplaceOps_DoubleClick);
			this.lvEditReplaceOps.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvEditReplaceOps_MouseUp);
			// 
			// lbAlloGenFile
			// 
			this.lbAlloGenFile.AutoSize = true;
			this.lbAlloGenFile.Location = new System.Drawing.Point(15, 8);
			this.lbAlloGenFile.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.lbAlloGenFile.Name = "lbAlloGenFile";
			this.lbAlloGenFile.Size = new System.Drawing.Size(26, 13);
			this.lbAlloGenFile.TabIndex = 0;
			this.lbAlloGenFile.Text = "File:";
			// 
			// tbFile
			// 
			this.tbFile.Location = new System.Drawing.Point(67, 8);
			this.tbFile.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.tbFile.Name = "tbFile";
			this.tbFile.Size = new System.Drawing.Size(665, 20);
			this.tbFile.TabIndex = 1;
			this.tbFile.Text = "XML file location";
			// 
			// btnBrowse
			// 
			this.btnBrowse.Location = new System.Drawing.Point(757, 10);
			this.btnBrowse.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new System.Drawing.Size(57, 25);
			this.btnBrowse.TabIndex = 2;
			this.btnBrowse.Text = "&Browse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			// 
			// btnHelp
			// 
			this.btnHelp.Location = new System.Drawing.Point(757, 40);
			this.btnHelp.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Size = new System.Drawing.Size(57, 25);
			this.btnHelp.TabIndex = 4;
			this.btnHelp.Text = "&Help";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// btnNewFile
			// 
			this.btnNewFile.Location = new System.Drawing.Point(18, 40);
			this.btnNewFile.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.btnNewFile.Name = "btnNewFile";
			this.btnNewFile.Size = new System.Drawing.Size(99, 19);
			this.btnNewFile.TabIndex = 3;
			this.btnNewFile.Text = "&Create New File";
			this.btnNewFile.UseVisualStyleBackColor = true;
			this.btnNewFile.Click += new System.EventHandler(this.btnNewFile_Click);
			// 
			// ilPreview
			// 
			this.ilPreview.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilPreview.ImageStream")));
			this.ilPreview.TransparentColor = System.Drawing.Color.Transparent;
			this.ilPreview.Images.SetKeyName(0, "CheckedCheckbox.bmp");
			// 
			// AlloGenFormBase
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(859, 647);
			this.Controls.Add(this.btnNewFile);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnBrowse);
			this.Controls.Add(this.tbFile);
			this.Controls.Add(this.lbAlloGenFile);
			this.Controls.Add(this.tabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.Name = "AlloGenFormBase";
			this.tabControl.ResumeLayout(false);
			this.tabEditOps.ResumeLayout(false);
			this.tabEditOps.PerformLayout();
			this.plPattern.ResumeLayout(false);
			this.plPattern.PerformLayout();
			this.plActions.ResumeLayout(false);
			this.plActions.PerformLayout();
			this.tabRunOps.ResumeLayout(false);
			this.tabRunOps.PerformLayout();
			this.tabEditReplaceOps.ResumeLayout(false);
			this.tabEditReplaceOps.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        protected System.Windows.Forms.TabControl tabControl;
        protected System.Windows.Forms.TabPage tabEditOps;
        protected System.Windows.Forms.TabPage tabRunOps;
        protected System.Windows.Forms.ListBox lBoxOperations;
        protected System.Windows.Forms.Label lbAlloGenFile;
        protected System.Windows.Forms.TextBox tbFile;
        protected System.Windows.Forms.Button btnBrowse;
        protected System.Windows.Forms.Button btnHelp;
        protected System.Windows.Forms.TextBox tbName;
        protected System.Windows.Forms.TextBox tbDescription;
        protected System.Windows.Forms.Label lbPattern;
        protected System.Windows.Forms.Label lbAction;
        protected System.Windows.Forms.Label lbName;
        protected System.Windows.Forms.Label lbDescription;
        protected System.Windows.Forms.ListBox lBoxEnvironments;
        protected System.Windows.Forms.ListBox lBoxMorphTypes;
        protected System.Windows.Forms.Label lbEnvironments;
        protected System.Windows.Forms.Label lbMorphTypes;
        protected System.Windows.Forms.TextBox tbMatch;
        protected System.Windows.Forms.Label lbMatch;
        protected System.Windows.Forms.ListBox lBoxReplaceOps;
        protected System.Windows.Forms.Button btnCategory;
        protected System.Windows.Forms.TextBox tbCategory;
        protected System.Windows.Forms.Label lbCategory;
        protected System.Windows.Forms.Button btnStemName;
        protected System.Windows.Forms.TextBox tbStemName;
        protected System.Windows.Forms.Label lbStemName;
        protected System.Windows.Forms.Label lbReplaceOps;
        protected System.Windows.Forms.Button btnEnvironments;
        protected System.Windows.Forms.Button btnMorphTypes;
        protected System.Windows.Forms.Label lbReplaceRightClickToEdit;
        protected System.Windows.Forms.Label lbOpRightClickToEdit;
        protected System.Windows.Forms.Label lbOperations;
        protected System.Windows.Forms.Button btnSaveChanges;
        protected System.Windows.Forms.Panel plPattern;
        protected System.Windows.Forms.Panel plActions;
        protected System.Windows.Forms.Button btnNewFile;
        protected System.Windows.Forms.Button btnMatch;
        protected System.Windows.Forms.Label lbOperationsToApply;
        protected System.Windows.Forms.Button btnApplyOperations;
        protected System.Windows.Forms.Label lbPreview;
        protected System.Windows.Forms.ListView lvPreview;
        protected System.Windows.Forms.Label lbCountRunOps;
        protected ImageList ilPreview;
        protected ListView lvOperations;
        protected Button btnSaveChanges2;
        protected TabPage tabEditReplaceOps;
        protected Button btnEditReplaceOp;
        protected Button btnAddNewReplaceOp;
        protected ListView lvEditReplaceOps;
        protected Button btnDeleteReplaceOp;
        protected Button btnSaveChanges3;
        protected Label lbCountReplaceOps;
        protected Label lbCountOps;
        protected Label lbApplyTo;
        protected ComboBox cbApplyTo;
		private System.ComponentModel.IContainer components;
	}
}

