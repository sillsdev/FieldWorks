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
			resources.ApplyResources(this.tabControl, "tabControl");
			this.tabControl.Controls.Add(this.tabEditOps);
			this.tabControl.Controls.Add(this.tabRunOps);
			this.tabControl.Controls.Add(this.tabEditReplaceOps);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
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
			resources.ApplyResources(this.tabEditOps, "tabEditOps");
			this.tabEditOps.Name = "tabEditOps";
			this.tabEditOps.UseVisualStyleBackColor = true;
			// 
			// lbApplyTo
			// 
			resources.ApplyResources(this.lbApplyTo, "lbApplyTo");
			this.lbApplyTo.Name = "lbApplyTo";
			// 
			// cbApplyTo
			// 
			this.cbApplyTo.FormattingEnabled = true;
			resources.ApplyResources(this.cbApplyTo, "cbApplyTo");
			this.cbApplyTo.Name = "cbApplyTo";
			this.cbApplyTo.SelectedIndexChanged += new System.EventHandler(this.cbApplyTo_SelectedIndexChanged);
			// 
			// lbCountOps
			// 
			resources.ApplyResources(this.lbCountOps, "lbCountOps");
			this.lbCountOps.Name = "lbCountOps";
			// 
			// btnSaveChanges
			// 
			resources.ApplyResources(this.btnSaveChanges, "btnSaveChanges");
			this.btnSaveChanges.Name = "btnSaveChanges";
			this.btnSaveChanges.UseVisualStyleBackColor = true;
			this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
			// 
			// lbOpRightClickToEdit
			// 
			resources.ApplyResources(this.lbOpRightClickToEdit, "lbOpRightClickToEdit");
			this.lbOpRightClickToEdit.Name = "lbOpRightClickToEdit";
			// 
			// lbOperations
			// 
			resources.ApplyResources(this.lbOperations, "lbOperations");
			this.lbOperations.Name = "lbOperations";
			// 
			// lbPattern
			// 
			resources.ApplyResources(this.lbPattern, "lbPattern");
			this.lbPattern.Name = "lbPattern";
			// 
			// lBoxOperations
			// 
			this.lBoxOperations.FormattingEnabled = true;
			resources.ApplyResources(this.lBoxOperations, "lBoxOperations");
			this.lBoxOperations.Name = "lBoxOperations";
			this.lBoxOperations.SelectedIndexChanged += new System.EventHandler(this.lBoxOperations_SelectedIndexChanged);
			this.lBoxOperations.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lBoxOperations_MouseUp);
			// 
			// lbName
			// 
			resources.ApplyResources(this.lbName, "lbName");
			this.lbName.Name = "lbName";
			// 
			// lbDescription
			// 
			resources.ApplyResources(this.lbDescription, "lbDescription");
			this.lbDescription.Name = "lbDescription";
			// 
			// tbName
			// 
			resources.ApplyResources(this.tbName, "tbName");
			this.tbName.Name = "tbName";
			this.tbName.TextChanged += new System.EventHandler(this.tbName_TextChanged);
			// 
			// tbDescription
			// 
			resources.ApplyResources(this.tbDescription, "tbDescription");
			this.tbDescription.Name = "tbDescription";
			this.tbDescription.TextChanged += new System.EventHandler(this.tbDescription_TextChanged);
			// 
			// lbAction
			// 
			resources.ApplyResources(this.lbAction, "lbAction");
			this.lbAction.Name = "lbAction";
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
			resources.ApplyResources(this.plPattern, "plPattern");
			this.plPattern.Name = "plPattern";
			// 
			// btnMatch
			// 
			resources.ApplyResources(this.btnMatch, "btnMatch");
			this.btnMatch.Name = "btnMatch";
			this.btnMatch.UseVisualStyleBackColor = true;
			this.btnMatch.Click += new System.EventHandler(this.btnMatch_Click);
			// 
			// btnMorphTypes
			// 
			resources.ApplyResources(this.btnMorphTypes, "btnMorphTypes");
			this.btnMorphTypes.Name = "btnMorphTypes";
			this.btnMorphTypes.UseVisualStyleBackColor = true;
			this.btnMorphTypes.Click += new System.EventHandler(this.btnMorphTypes_Click);
			// 
			// btnCategory
			// 
			resources.ApplyResources(this.btnCategory, "btnCategory");
			this.btnCategory.Name = "btnCategory";
			this.btnCategory.UseVisualStyleBackColor = true;
			this.btnCategory.Click += new System.EventHandler(this.btnCategory_Click);
			// 
			// tbCategory
			// 
			resources.ApplyResources(this.tbCategory, "tbCategory");
			this.tbCategory.Name = "tbCategory";
			this.tbCategory.ReadOnly = true;
			// 
			// lbCategory
			// 
			resources.ApplyResources(this.lbCategory, "lbCategory");
			this.lbCategory.Name = "lbCategory";
			// 
			// lBoxMorphTypes
			// 
			resources.ApplyResources(this.lBoxMorphTypes, "lBoxMorphTypes");
			this.lBoxMorphTypes.FormattingEnabled = true;
			this.lBoxMorphTypes.Name = "lBoxMorphTypes";
			this.lBoxMorphTypes.Sorted = true;
			// 
			// lbMorphTypes
			// 
			resources.ApplyResources(this.lbMorphTypes, "lbMorphTypes");
			this.lbMorphTypes.Name = "lbMorphTypes";
			// 
			// tbMatch
			// 
			resources.ApplyResources(this.tbMatch, "tbMatch");
			this.tbMatch.Name = "tbMatch";
			// 
			// lbMatch
			// 
			resources.ApplyResources(this.lbMatch, "lbMatch");
			this.lbMatch.Name = "lbMatch";
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
			resources.ApplyResources(this.plActions, "plActions");
			this.plActions.Name = "plActions";
			// 
			// lbReplaceRightClickToEdit
			// 
			resources.ApplyResources(this.lbReplaceRightClickToEdit, "lbReplaceRightClickToEdit");
			this.lbReplaceRightClickToEdit.Name = "lbReplaceRightClickToEdit";
			// 
			// btnEnvironments
			// 
			resources.ApplyResources(this.btnEnvironments, "btnEnvironments");
			this.btnEnvironments.Name = "btnEnvironments";
			this.btnEnvironments.UseVisualStyleBackColor = true;
			this.btnEnvironments.Click += new System.EventHandler(this.btnEnvironments_Click);
			// 
			// lbReplaceOps
			// 
			resources.ApplyResources(this.lbReplaceOps, "lbReplaceOps");
			this.lbReplaceOps.Name = "lbReplaceOps";
			// 
			// btnStemName
			// 
			resources.ApplyResources(this.btnStemName, "btnStemName");
			this.btnStemName.Name = "btnStemName";
			this.btnStemName.UseVisualStyleBackColor = true;
			this.btnStemName.Click += new System.EventHandler(this.btnStemName_Click);
			// 
			// tbStemName
			// 
			resources.ApplyResources(this.tbStemName, "tbStemName");
			this.tbStemName.Name = "tbStemName";
			this.tbStemName.ReadOnly = true;
			// 
			// lbStemName
			// 
			resources.ApplyResources(this.lbStemName, "lbStemName");
			this.lbStemName.Name = "lbStemName";
			// 
			// lBoxReplaceOps
			// 
			this.lBoxReplaceOps.FormattingEnabled = true;
			resources.ApplyResources(this.lBoxReplaceOps, "lBoxReplaceOps");
			this.lBoxReplaceOps.Name = "lBoxReplaceOps";
			this.lBoxReplaceOps.DoubleClick += new System.EventHandler(this.lBoxReplaceOps_DoubleClick);
			this.lBoxReplaceOps.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lBoxReplaceOps_MouseUp);
			// 
			// lBoxEnvironments
			// 
			resources.ApplyResources(this.lBoxEnvironments, "lBoxEnvironments");
			this.lBoxEnvironments.FormattingEnabled = true;
			this.lBoxEnvironments.Name = "lBoxEnvironments";
			// 
			// lbEnvironments
			// 
			resources.ApplyResources(this.lbEnvironments, "lbEnvironments");
			this.lbEnvironments.Name = "lbEnvironments";
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
			resources.ApplyResources(this.tabRunOps, "tabRunOps");
			this.tabRunOps.Name = "tabRunOps";
			this.tabRunOps.UseVisualStyleBackColor = true;
			// 
			// btnSaveChanges2
			// 
			resources.ApplyResources(this.btnSaveChanges2, "btnSaveChanges2");
			this.btnSaveChanges2.Name = "btnSaveChanges2";
			this.btnSaveChanges2.UseVisualStyleBackColor = true;
			this.btnSaveChanges2.Click += new System.EventHandler(this.btnSaveChanges2_Click);
			// 
			// lvOperations
			// 
			this.lvOperations.CheckBoxes = true;
			this.lvOperations.FullRowSelect = true;
			this.lvOperations.GridLines = true;
			this.lvOperations.HideSelection = false;
			resources.ApplyResources(this.lvOperations, "lvOperations");
			this.lvOperations.MultiSelect = false;
			this.lvOperations.Name = "lvOperations";
			this.lvOperations.UseCompatibleStateImageBehavior = false;
			this.lvOperations.View = System.Windows.Forms.View.Details;
			this.lvOperations.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvOperations_ColumnClick);
			this.lvOperations.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvOperations_ItemChecked);
			this.lvOperations.SelectedIndexChanged += new System.EventHandler(this.lvOperations_SelectedIndexChanged);
			// 
			// lbCountRunOps
			// 
			resources.ApplyResources(this.lbCountRunOps, "lbCountRunOps");
			this.lbCountRunOps.Name = "lbCountRunOps";
			// 
			// lvPreview
			// 
			resources.ApplyResources(this.lvPreview, "lvPreview");
			this.lvPreview.CheckBoxes = true;
			this.lvPreview.FullRowSelect = true;
			this.lvPreview.GridLines = true;
			this.lvPreview.HideSelection = false;
			this.lvPreview.Name = "lvPreview";
			this.lvPreview.UseCompatibleStateImageBehavior = false;
			this.lvPreview.View = System.Windows.Forms.View.Details;
			this.lvPreview.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvPreview_ColumnClick);
			// 
			// lbPreview
			// 
			resources.ApplyResources(this.lbPreview, "lbPreview");
			this.lbPreview.Name = "lbPreview";
			// 
			// btnApplyOperations
			// 
			resources.ApplyResources(this.btnApplyOperations, "btnApplyOperations");
			this.btnApplyOperations.Name = "btnApplyOperations";
			this.btnApplyOperations.UseVisualStyleBackColor = true;
			this.btnApplyOperations.Click += new System.EventHandler(this.btnApplyOperations_Click);
			// 
			// lbOperationsToApply
			// 
			resources.ApplyResources(this.lbOperationsToApply, "lbOperationsToApply");
			this.lbOperationsToApply.Name = "lbOperationsToApply";
			// 
			// tabEditReplaceOps
			// 
			this.tabEditReplaceOps.Controls.Add(this.lbCountReplaceOps);
			this.tabEditReplaceOps.Controls.Add(this.btnSaveChanges3);
			this.tabEditReplaceOps.Controls.Add(this.btnDeleteReplaceOp);
			this.tabEditReplaceOps.Controls.Add(this.btnEditReplaceOp);
			this.tabEditReplaceOps.Controls.Add(this.btnAddNewReplaceOp);
			this.tabEditReplaceOps.Controls.Add(this.lvEditReplaceOps);
			resources.ApplyResources(this.tabEditReplaceOps, "tabEditReplaceOps");
			this.tabEditReplaceOps.Name = "tabEditReplaceOps";
			this.tabEditReplaceOps.UseVisualStyleBackColor = true;
			// 
			// lbCountReplaceOps
			// 
			resources.ApplyResources(this.lbCountReplaceOps, "lbCountReplaceOps");
			this.lbCountReplaceOps.Name = "lbCountReplaceOps";
			// 
			// btnSaveChanges3
			// 
			resources.ApplyResources(this.btnSaveChanges3, "btnSaveChanges3");
			this.btnSaveChanges3.Name = "btnSaveChanges3";
			this.btnSaveChanges3.UseVisualStyleBackColor = true;
			this.btnSaveChanges3.Click += new System.EventHandler(this.btnSaveChanges3_Click);
			// 
			// btnDeleteReplaceOp
			// 
			resources.ApplyResources(this.btnDeleteReplaceOp, "btnDeleteReplaceOp");
			this.btnDeleteReplaceOp.Name = "btnDeleteReplaceOp";
			this.btnDeleteReplaceOp.UseVisualStyleBackColor = true;
			this.btnDeleteReplaceOp.Click += new System.EventHandler(this.btnDeleteReplaceOp_Click);
			// 
			// btnEditReplaceOp
			// 
			resources.ApplyResources(this.btnEditReplaceOp, "btnEditReplaceOp");
			this.btnEditReplaceOp.Name = "btnEditReplaceOp";
			this.btnEditReplaceOp.UseVisualStyleBackColor = true;
			this.btnEditReplaceOp.Click += new System.EventHandler(this.btnEditReplaceOp_Click);
			// 
			// btnAddNewReplaceOp
			// 
			resources.ApplyResources(this.btnAddNewReplaceOp, "btnAddNewReplaceOp");
			this.btnAddNewReplaceOp.Name = "btnAddNewReplaceOp";
			this.btnAddNewReplaceOp.UseVisualStyleBackColor = true;
			this.btnAddNewReplaceOp.Click += new System.EventHandler(this.btnAddNewReplaceOp_Click);
			// 
			// lvEditReplaceOps
			// 
			resources.ApplyResources(this.lvEditReplaceOps, "lvEditReplaceOps");
			this.lvEditReplaceOps.FullRowSelect = true;
			this.lvEditReplaceOps.GridLines = true;
			this.lvEditReplaceOps.HideSelection = false;
			this.lvEditReplaceOps.MultiSelect = false;
			this.lvEditReplaceOps.Name = "lvEditReplaceOps";
			this.lvEditReplaceOps.UseCompatibleStateImageBehavior = false;
			this.lvEditReplaceOps.View = System.Windows.Forms.View.Details;
			this.lvEditReplaceOps.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvEditReplaceOps_ColumnClick);
			this.lvEditReplaceOps.SelectedIndexChanged += new System.EventHandler(this.lvEditReplaceOps_SelectedIndexChanged);
			this.lvEditReplaceOps.DoubleClick += new System.EventHandler(this.lvEditReplaceOps_DoubleClick);
			this.lvEditReplaceOps.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvEditReplaceOps_MouseUp);
			// 
			// lbAlloGenFile
			// 
			resources.ApplyResources(this.lbAlloGenFile, "lbAlloGenFile");
			this.lbAlloGenFile.Name = "lbAlloGenFile";
			// 
			// tbFile
			// 
			resources.ApplyResources(this.tbFile, "tbFile");
			this.tbFile.Name = "tbFile";
			// 
			// btnBrowse
			// 
			resources.ApplyResources(this.btnBrowse, "btnBrowse");
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			// 
			// btnHelp
			// 
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// btnNewFile
			// 
			resources.ApplyResources(this.btnNewFile, "btnNewFile");
			this.btnNewFile.Name = "btnNewFile";
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
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.btnNewFile);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnBrowse);
			this.Controls.Add(this.tbFile);
			this.Controls.Add(this.lbAlloGenFile);
			this.Controls.Add(this.tabControl);
			this.KeyPreview = true;
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

