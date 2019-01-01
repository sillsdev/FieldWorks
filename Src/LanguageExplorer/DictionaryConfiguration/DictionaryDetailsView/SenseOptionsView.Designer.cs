// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Resources;

namespace LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView
{
	partial class SenseOptionsView
	{
		// these two variables are used to get shared labels
		private static ResourceManager s_resourceMan;
		private static System.Globalization.CultureInfo s_resourceCulture;

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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#region SharedLabels
		/// <summary>Returns the cached ResourceManager instance used by this class.</summary>
		[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				return s_resourceMan ?? (s_resourceMan =
					new ResourceManager("LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView.SenseOptionsView", typeof(SenseOptionsView).Assembly));
			}
		}

		/// <summary>
		/// Overrides the current thread's CurrentUICulture property for all resource lookups using this strongly typed resource class.
		/// </summary>
		[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
		internal static System.Globalization.CultureInfo Culture
		{
			get { return s_resourceCulture; }
			set { s_resourceCulture = value; }
		}

		/// <summary>Looks up a localized string similar to If all senses share the grammatical information, show it first.</summary>
		internal static string ksShowGrammarFirst
		{
			get { return ResourceManager.GetString("checkBoxShowGrammarFirst.Text", s_resourceCulture); }
		}
		#endregion SharedLabels

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SenseOptionsView));
			this.checkBoxSenseInPara = new System.Windows.Forms.CheckBox();
			this.checkBoxShowGrammarFirst = new System.Windows.Forms.CheckBox();
			this.checkBoxFirstSenseInline = new System.Windows.Forms.CheckBox();
			this.labelStyle = new System.Windows.Forms.Label();
			this.labelAfter = new System.Windows.Forms.Label();
			this.labelNumberingStyle = new System.Windows.Forms.Label();
			this.labelBefore = new System.Windows.Forms.Label();
			this.dropDownNumberingStyle = new System.Windows.Forms.ComboBox();
			this.textBoxAfter = new System.Windows.Forms.TextBox();
			this.textBoxBefore = new System.Windows.Forms.TextBox();
			this.checkBoxNumberSingleSense = new System.Windows.Forms.CheckBox();
			this.dropDownStyle = new System.Windows.Forms.ComboBox();
			this.buttonStyles = new System.Windows.Forms.Button();
			this.groupBoxSenseNumber = new System.Windows.Forms.GroupBox();
			this.labelParentSenseNumberStyle = new System.Windows.Forms.Label();
			this.dropDownParentSenseNumberStyle = new System.Windows.Forms.ComboBox();
			this.senseStructureVerticalFlow = new System.Windows.Forms.FlowLayoutPanel();
			this.groupBoxSenseNumber.SuspendLayout();
			this.senseStructureVerticalFlow.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkBoxSenseInPara
			// 
			resources.ApplyResources(this.checkBoxSenseInPara, "checkBoxSenseInPara");
			this.checkBoxSenseInPara.Name = "checkBoxSenseInPara";
			this.checkBoxSenseInPara.UseVisualStyleBackColor = true;
			// 
			// checkBoxShowGrammarFirst
			// 
			resources.ApplyResources(this.checkBoxShowGrammarFirst, "checkBoxShowGrammarFirst");
			this.checkBoxShowGrammarFirst.Name = "checkBoxShowGrammarFirst";
			this.checkBoxShowGrammarFirst.UseVisualStyleBackColor = true;
			// 
			// checkBoxFirstSenseInline
			// 
			resources.ApplyResources(this.checkBoxFirstSenseInline, "checkBoxFirstSenseInline");
			this.checkBoxFirstSenseInline.Name = "checkBoxFirstSenseInline";
			this.checkBoxFirstSenseInline.UseVisualStyleBackColor = true;
			// 
			// labelStyle
			// 
			resources.ApplyResources(this.labelStyle, "labelStyle");
			this.labelStyle.Name = "labelStyle";
			// 
			// labelAfter
			// 
			resources.ApplyResources(this.labelAfter, "labelAfter");
			this.labelAfter.Name = "labelAfter";
			// 
			// labelNumberingStyle
			// 
			resources.ApplyResources(this.labelNumberingStyle, "labelNumberingStyle");
			this.labelNumberingStyle.Name = "labelNumberingStyle";
			// 
			// labelBefore
			// 
			resources.ApplyResources(this.labelBefore, "labelBefore");
			this.labelBefore.Name = "labelBefore";
			// 
			// dropDownNumberingStyle
			// 
			resources.ApplyResources(this.dropDownNumberingStyle, "dropDownNumberingStyle");
			this.dropDownNumberingStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownNumberingStyle.FormattingEnabled = true;
			this.dropDownNumberingStyle.Name = "dropDownNumberingStyle";
			// 
			// textBoxAfter
			// 
			resources.ApplyResources(this.textBoxAfter, "textBoxAfter");
			this.textBoxAfter.Name = "textBoxAfter";
			// 
			// textBoxBefore
			// 
			resources.ApplyResources(this.textBoxBefore, "textBoxBefore");
			this.textBoxBefore.Name = "textBoxBefore";
			// 
			// checkBoxNumberSingleSense
			// 
			resources.ApplyResources(this.checkBoxNumberSingleSense, "checkBoxNumberSingleSense");
			this.checkBoxNumberSingleSense.Name = "checkBoxNumberSingleSense";
			this.checkBoxNumberSingleSense.UseVisualStyleBackColor = false;
			// 
			// dropDownStyle
			// 
			resources.ApplyResources(this.dropDownStyle, "dropDownStyle");
			this.dropDownStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownStyle.FormattingEnabled = true;
			this.dropDownStyle.Name = "dropDownStyle";
			// 
			// buttonStyles
			// 
			resources.ApplyResources(this.buttonStyles, "buttonStyles");
			this.buttonStyles.Name = "buttonStyles";
			this.buttonStyles.UseVisualStyleBackColor = true;
			// 
			// groupBoxSenseNumber
			// 
			this.groupBoxSenseNumber.Controls.Add(this.labelParentSenseNumberStyle);
			this.groupBoxSenseNumber.Controls.Add(this.dropDownParentSenseNumberStyle);
			this.groupBoxSenseNumber.Controls.Add(this.buttonStyles);
			this.groupBoxSenseNumber.Controls.Add(this.dropDownStyle);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxNumberSingleSense);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxBefore);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxAfter);
			this.groupBoxSenseNumber.Controls.Add(this.dropDownNumberingStyle);
			this.groupBoxSenseNumber.Controls.Add(this.labelBefore);
			this.groupBoxSenseNumber.Controls.Add(this.labelNumberingStyle);
			this.groupBoxSenseNumber.Controls.Add(this.labelAfter);
			this.groupBoxSenseNumber.Controls.Add(this.labelStyle);
			resources.ApplyResources(this.groupBoxSenseNumber, "groupBoxSenseNumber");
			this.groupBoxSenseNumber.Name = "groupBoxSenseNumber";
			this.groupBoxSenseNumber.TabStop = false;
			// 
			// labelParentSenseNumberStyle
			// 
			resources.ApplyResources(this.labelParentSenseNumberStyle, "labelParentSenseNumberStyle");
			this.labelParentSenseNumberStyle.Name = "labelParentSenseNumberStyle";
			// 
			// dropDownParentSenseNumberStyle
			// 
			this.dropDownParentSenseNumberStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownParentSenseNumberStyle.FormattingEnabled = true;
			resources.ApplyResources(this.dropDownParentSenseNumberStyle, "dropDownParentSenseNumberStyle");
			this.dropDownParentSenseNumberStyle.Name = "dropDownParentSenseNumberStyle";
			// 
			// senseOrderingAndFormattingOptionsVerticalFlow
			// 
			resources.ApplyResources(this.senseStructureVerticalFlow, "senseStructureVerticalFlow");
			this.senseStructureVerticalFlow.Controls.Add(this.checkBoxShowGrammarFirst);
			this.senseStructureVerticalFlow.Controls.Add(this.checkBoxSenseInPara);
			this.senseStructureVerticalFlow.Controls.Add(this.checkBoxFirstSenseInline);
			this.senseStructureVerticalFlow.Name = "senseStructureVerticalFlow";
			// 
			// SenseOptionsView
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBoxSenseNumber);
			this.Controls.Add(this.senseStructureVerticalFlow);
			this.MaximumSize = new System.Drawing.Size(0, 193);
			this.MinimumSize = new System.Drawing.Size(305, 220);
			this.Name = "SenseOptionsView";
			this.groupBoxSenseNumber.ResumeLayout(false);
			this.groupBoxSenseNumber.PerformLayout();
			this.senseStructureVerticalFlow.ResumeLayout(false);
			this.senseStructureVerticalFlow.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxSenseInPara;
		private System.Windows.Forms.CheckBox checkBoxShowGrammarFirst;
		private System.Windows.Forms.CheckBox checkBoxFirstSenseInline;
		private System.Windows.Forms.Label labelStyle;
		private System.Windows.Forms.Label labelAfter;
		private System.Windows.Forms.Label labelNumberingStyle;
		private System.Windows.Forms.Label labelBefore;
		private System.Windows.Forms.ComboBox dropDownNumberingStyle;
		private System.Windows.Forms.TextBox textBoxAfter;
		private System.Windows.Forms.TextBox textBoxBefore;
		private System.Windows.Forms.CheckBox checkBoxNumberSingleSense;
		private System.Windows.Forms.ComboBox dropDownStyle;
		private System.Windows.Forms.Button buttonStyles;
		private System.Windows.Forms.GroupBox groupBoxSenseNumber;
		private System.Windows.Forms.Label labelParentSenseNumberStyle;
		private System.Windows.Forms.ComboBox dropDownParentSenseNumberStyle;
		private System.Windows.Forms.FlowLayoutPanel senseStructureVerticalFlow;
	}
}
