// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigurationTreeControl
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
				foreach(IDisposable menuItem in m_CtrlRightClickMenu.Items)
					menuItem.Dispose();
				m_CtrlRightClickMenu.Dispose();
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DictionaryConfigurationTreeControl));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.tree = new System.Windows.Forms.TreeView();
			this.moveUp = new System.Windows.Forms.Button();
			this.moveDown = new System.Windows.Forms.Button();
			this.duplicate = new System.Windows.Forms.Button();
			this.remove = new System.Windows.Forms.Button();
			this.rename = new System.Windows.Forms.Button();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
			this.tableLayoutPanel.Controls.Add(this.tree, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.moveUp, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.moveDown, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.duplicate, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.remove, 1, 3);
			this.tableLayoutPanel.Controls.Add(this.rename, 1, 4);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			// 
			// tree
			// 
			this.tree.CheckBoxes = true;
			resources.ApplyResources(this.tree, "tree");
			this.tree.HideSelection = false;
			this.tree.Name = "tree";
			this.tableLayoutPanel.SetRowSpan(this.tree, 5);
			this.tree.Click += new System.EventHandler(this.TreeClick);
			// 
			// moveUp
			// 
			resources.ApplyResources(this.moveUp, "moveUp");
			this.moveUp.Name = "moveUp";
			this.moveUp.UseVisualStyleBackColor = true;
			// 
			// moveDown
			// 
			resources.ApplyResources(this.moveDown, "moveDown");
			this.moveDown.Name = "moveDown";
			this.moveDown.UseVisualStyleBackColor = true;
			// 
			// duplicate
			// 
			resources.ApplyResources(this.duplicate, "duplicate");
			this.duplicate.Name = "duplicate";
			this.duplicate.UseVisualStyleBackColor = true;
			// 
			// remove
			// 
			resources.ApplyResources(this.remove, "remove");
			this.remove.Name = "remove";
			this.remove.UseVisualStyleBackColor = true;
			// 
			// rename
			// 
			resources.ApplyResources(this.rename, "rename");
			this.rename.Name = "rename";
			this.rename.UseVisualStyleBackColor = true;
			// 
			// DictionaryConfigurationTreeControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel);
			this.Name = "DictionaryConfigurationTreeControl";
			this.tableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.TreeView tree;
		private System.Windows.Forms.Button moveDown;
		private System.Windows.Forms.Button remove;
		private System.Windows.Forms.Button duplicate;
		private System.Windows.Forms.Button moveUp;
		private System.Windows.Forms.Button rename;
	}
}
