using System;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.TE
{
	partial class ScrTextListSelectionForm : IDisposable
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScrTextListSelectionForm));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.uiLeftList = new System.Windows.Forms.ListBox();
			this.uiLeftLabel = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.uiAdd = new System.Windows.Forms.Button();
			this.uiRemove = new System.Windows.Forms.Button();
			this.uiDescription = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();
			this.uiRightLabel = new System.Windows.Forms.Label();
			this.uiRightList = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.panel4 = new System.Windows.Forms.Panel();
			this.uiMoveUp = new System.Windows.Forms.Button();
			this.uiMoveDown = new System.Windows.Forms.Button();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.uiLoadSelections = new System.Windows.Forms.LinkLabel();
			this.uiSaveSelections = new System.Windows.Forms.LinkLabel();
			this.uiDeleteSelections = new System.Windows.Forms.LinkLabel();
			this.uiCancel = new System.Windows.Forms.Button();
			this.uiOk = new System.Windows.Forms.Button();
			this.uiLeftListMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.uiLeftListAdd = new System.Windows.Forms.ToolStripMenuItem();
			this.uiRightListMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.uiRightListRemove = new System.Windows.Forms.ToolStripMenuItem();
			this.uiToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.uiSavedSelectionsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.tableLayoutPanel1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.panel3.SuspendLayout();
			this.panel4.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.uiLeftListMenu.SuspendLayout();
			this.uiRightListMenu.SuspendLayout();
			this.SuspendLayout();
			//
			// tableLayoutPanel1
			//
			resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
			this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.uiDescription, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.panel3, 2, 1);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label2, 2, 2);
			this.tableLayoutPanel1.Controls.Add(this.panel4, 3, 1);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 4);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			//
			// panel1
			//
			this.panel1.Controls.Add(this.uiLeftList);
			this.panel1.Controls.Add(this.uiLeftLabel);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// uiLeftList
			//
			resources.ApplyResources(this.uiLeftList, "uiLeftList");
			this.uiLeftList.Name = "uiLeftList";
			this.uiLeftList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.uiToolTip.SetToolTip(this.uiLeftList, resources.GetString("uiLeftList.ToolTip"));
			this.uiLeftList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.uiLeftList_MouseDoubleClick);
			this.uiLeftList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.uiLeftList_MouseDown);
			//
			// uiLeftLabel
			//
			resources.ApplyResources(this.uiLeftLabel, "uiLeftLabel");
			this.uiLeftLabel.Name = "uiLeftLabel";
			//
			// panel2
			//
			resources.ApplyResources(this.panel2, "panel2");
			this.panel2.Controls.Add(this.uiAdd);
			this.panel2.Controls.Add(this.uiRemove);
			this.panel2.Name = "panel2";
			//
			// uiAdd
			//
			resources.ApplyResources(this.uiAdd, "uiAdd");
			this.uiAdd.Name = "uiAdd";
			this.uiAdd.Click += new System.EventHandler(this.uiAdd_Click);
			//
			// uiRemove
			//
			resources.ApplyResources(this.uiRemove, "uiRemove");
			this.uiRemove.Name = "uiRemove";
			this.uiRemove.Click += new System.EventHandler(this.uiRemove_Click);
			//
			// uiDescription
			//
			resources.ApplyResources(this.uiDescription, "uiDescription");
			this.tableLayoutPanel1.SetColumnSpan(this.uiDescription, 3);
			this.uiDescription.Name = "uiDescription";
			//
			// panel3
			//
			this.panel3.Controls.Add(this.uiRightLabel);
			this.panel3.Controls.Add(this.uiRightList);
			resources.ApplyResources(this.panel3, "panel3");
			this.panel3.Name = "panel3";
			//
			// uiRightLabel
			//
			resources.ApplyResources(this.uiRightLabel, "uiRightLabel");
			this.uiRightLabel.Name = "uiRightLabel";
			//
			// uiRightList
			//
			resources.ApplyResources(this.uiRightList, "uiRightList");
			this.uiRightList.Name = "uiRightList";
			this.uiRightList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.uiToolTip.SetToolTip(this.uiRightList, resources.GetString("uiRightList.ToolTip"));
			this.uiRightList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.uiRightList_MouseDoubleClick);
			this.uiRightList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.uiRightList_MouseDown);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// panel4
			//
			resources.ApplyResources(this.panel4, "panel4");
			this.panel4.Controls.Add(this.uiMoveUp);
			this.panel4.Controls.Add(this.uiMoveDown);
			this.panel4.Name = "panel4";
			//
			// uiMoveUp
			//
			resources.ApplyResources(this.uiMoveUp, "uiMoveUp");
			this.uiMoveUp.Name = "uiMoveUp";
			this.uiMoveUp.Click += new System.EventHandler(this.uiMoveUp_Click);
			//
			// uiMoveDown
			//
			resources.ApplyResources(this.uiMoveDown, "uiMoveDown");
			this.uiMoveDown.Name = "uiMoveDown";
			this.uiMoveDown.Click += new System.EventHandler(this.uiMoveDown_Click);
			//
			// flowLayoutPanel1
			//
			resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
			this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 4);
			this.flowLayoutPanel1.Controls.Add(this.uiLoadSelections);
			this.flowLayoutPanel1.Controls.Add(this.uiSaveSelections);
			this.flowLayoutPanel1.Controls.Add(this.uiDeleteSelections);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			//
			// uiLoadSelections
			//
			resources.ApplyResources(this.uiLoadSelections, "uiLoadSelections");
			this.uiLoadSelections.Name = "uiLoadSelections";
			this.uiLoadSelections.TabStop = true;
			this.uiLoadSelections.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.uiLoadSelections_LinkClicked);
			//
			// uiSaveSelections
			//
			resources.ApplyResources(this.uiSaveSelections, "uiSaveSelections");
			this.uiSaveSelections.Name = "uiSaveSelections";
			this.uiSaveSelections.TabStop = true;
			this.uiSaveSelections.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.uiSaveSelections_LinkClicked);
			//
			// uiDeleteSelections
			//
			resources.ApplyResources(this.uiDeleteSelections, "uiDeleteSelections");
			this.uiDeleteSelections.Name = "uiDeleteSelections";
			this.uiDeleteSelections.TabStop = true;
			this.uiDeleteSelections.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.uiDeleteSelections_LinkClicked);
			//
			// uiCancel
			//
			resources.ApplyResources(this.uiCancel, "uiCancel");
			this.uiCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.uiCancel.Name = "uiCancel";
			//
			// uiOk
			//
			resources.ApplyResources(this.uiOk, "uiOk");
			this.uiOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.uiOk.Name = "uiOk";
			//
			// uiLeftListMenu
			//
			this.uiLeftListMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.uiLeftListAdd});
			this.uiLeftListMenu.Name = "uiLeftListMenu";
			resources.ApplyResources(this.uiLeftListMenu, "uiLeftListMenu");
			//
			// uiLeftListAdd
			//
			this.uiLeftListAdd.Name = "uiLeftListAdd";
			resources.ApplyResources(this.uiLeftListAdd, "uiLeftListAdd");
			this.uiLeftListAdd.Click += new System.EventHandler(this.uiLeftListAdd_Click);
			//
			// uiRightListMenu
			//
			this.uiRightListMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.uiRightListRemove});
			this.uiRightListMenu.Name = "uiRightListMenu";
			resources.ApplyResources(this.uiRightListMenu, "uiRightListMenu");
			//
			// uiRightListRemove
			//
			this.uiRightListRemove.Name = "uiRightListRemove";
			resources.ApplyResources(this.uiRightListRemove, "uiRightListRemove");
			this.uiRightListRemove.Click += new System.EventHandler(this.uiRightListRemove_Click);
			//
			// uiSavedSelectionsMenu
			//
			this.uiSavedSelectionsMenu.Name = "uiSavedSelections";
			resources.ApplyResources(this.uiSavedSelectionsMenu, "uiSavedSelectionsMenu");
			//
			// ScrTextListSelectionForm
			//
			this.AcceptButton = this.uiOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.uiCancel;
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.uiCancel);
			this.Controls.Add(this.uiOk);
			this.MinimizeBox = false;
			this.Name = "ScrTextListSelectionForm";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.panel4.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.uiLeftListMenu.ResumeLayout(false);
			this.uiRightListMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ListBox uiLeftList;
		private System.Windows.Forms.Label uiLeftLabel;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Button uiAdd;
		private System.Windows.Forms.Button uiRemove;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label uiRightLabel;
		private System.Windows.Forms.ListBox uiRightList;
		private System.Windows.Forms.Button uiCancel;
		private System.Windows.Forms.Button uiOk;
		private System.Windows.Forms.ContextMenuStrip uiLeftListMenu;
		private System.Windows.Forms.ToolStripMenuItem uiLeftListAdd;
		private System.Windows.Forms.ContextMenuStrip uiRightListMenu;
		private System.Windows.Forms.ToolStripMenuItem uiRightListRemove;
		private System.Windows.Forms.Label uiDescription;
		private System.Windows.Forms.ToolTip uiToolTip;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Button uiMoveUp;
		private System.Windows.Forms.Button uiMoveDown;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.LinkLabel uiLoadSelections;
		private System.Windows.Forms.LinkLabel uiSaveSelections;
		private System.Windows.Forms.LinkLabel uiDeleteSelections;
		private System.Windows.Forms.ContextMenuStrip uiSavedSelectionsMenu;
	}
}