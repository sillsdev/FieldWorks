// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// NonEmptyTargetControl is a small reusable piece currently used in the BulkEditBar.
	/// It allows three options to be chosen between for dealing with non-empty target fields
	/// in bulk copy and transduce. Additionally a separator can be chosen for the append option.
	/// </summary>
	public class NonEmptyTargetControl : UserControl
	{
		private GroupBox NonBlankTargetGroup;
		private RadioButton appendRadio;
		private RadioButton overwriteRadio;
		private RadioButton doNothingRadio;
		private FwTextBox sepBox;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Make one.
		/// </summary>
		public NonEmptyTargetControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// Prevent problems caused by assigning a bogus value of 1 to the
			// writing system code in InitializeComponent() -- a bug inserted
			// for free by the Windows.Forms Form Designer!
			sepBox.WritingSystemCode = 0;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// This is the main point of the class: to find out how the user wants to handle non-empty
		/// target fields.
		/// </summary>
		public NonEmptyTargetOptions NonEmptyMode
		{
			get
			{
				if (overwriteRadio.Checked)
				{
					return NonEmptyTargetOptions.Overwrite;
				}

				return appendRadio.Checked ? NonEmptyTargetOptions.Append : NonEmptyTargetOptions.DoNothing;
			}

			set
			{
				RadioButton checkedButton;
				switch (value)
				{
					case NonEmptyTargetOptions.Append:
						checkedButton = appendRadio;
						break;
					case NonEmptyTargetOptions.Overwrite:
						checkedButton = this.overwriteRadio;
						break;
					case NonEmptyTargetOptions.DoNothing:
					default:
						checkedButton = doNothingRadio;
						break;
				}
				checkedButton.Checked = true;
			}
		}

		/// <summary>
		/// Set the writing system factory so the separator text box can work properly.
		/// Note that setting this will clear the string in the separator box.
		/// </summary>
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return sepBox.WritingSystemFactory;
			}
			set
			{
				sepBox.WritingSystemFactory = value;
			}
		}

		/// <summary>
		/// The writing system that is actually used in the separator text box.
		/// </summary>
		public int WritingSystemCode
		{
			get
			{
				return sepBox.WritingSystemCode;
			}
			set
			{
				sepBox.WritingSystemCode = value;
			}
		}

		/// <summary>
		/// Set the string in the separator box.
		/// </summary>
		public ITsString TssSeparator
		{
			get
			{
				return sepBox.Tss;
			}
			set
			{
				sepBox.Tss = value;
			}
		}

		/// <summary>
		/// Gets or sets the separator.
		/// </summary>
		public string Separator
		{
			get
			{
				return sepBox.Text;
			}
			set
			{
				sepBox.Text = value;
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NonEmptyTargetControl));
			this.NonBlankTargetGroup = new System.Windows.Forms.GroupBox();
			this.sepBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.appendRadio = new System.Windows.Forms.RadioButton();
			this.overwriteRadio = new System.Windows.Forms.RadioButton();
			this.doNothingRadio = new System.Windows.Forms.RadioButton();
			this.NonBlankTargetGroup.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.sepBox)).BeginInit();
			this.SuspendLayout();
			//
			// NonBlankTargetGroup
			//
			resources.ApplyResources(this.NonBlankTargetGroup, "NonBlankTargetGroup");
			this.NonBlankTargetGroup.Controls.Add(this.sepBox);
			this.NonBlankTargetGroup.Controls.Add(this.appendRadio);
			this.NonBlankTargetGroup.Controls.Add(this.overwriteRadio);
			this.NonBlankTargetGroup.Controls.Add(this.doNothingRadio);
			this.NonBlankTargetGroup.Name = "NonBlankTargetGroup";
			this.NonBlankTargetGroup.TabStop = false;
			//
			// sepBox
			//
			this.sepBox.AdjustStringHeight = true;
			this.sepBox.BackColor = System.Drawing.SystemColors.Window;
			this.sepBox.controlID = null;
			resources.ApplyResources(this.sepBox, "sepBox");
			this.sepBox.Name = "sepBox";
			this.sepBox.SelectionLength = 0;
			this.sepBox.SelectionStart = 0;
			//
			// appendRadio
			//
			resources.ApplyResources(this.appendRadio, "appendRadio");
			this.appendRadio.Name = "appendRadio";
			//
			// overwriteRadio
			//
			resources.ApplyResources(this.overwriteRadio, "overwriteRadio");
			this.overwriteRadio.Name = "overwriteRadio";
			//
			// doNothingRadio
			//
			this.doNothingRadio.Checked = true;
			resources.ApplyResources(this.doNothingRadio, "doNothingRadio");
			this.doNothingRadio.Name = "doNothingRadio";
			this.doNothingRadio.TabStop = true;
			//
			// NonEmptyTargetControl
			//
			this.Controls.Add(this.NonBlankTargetGroup);
			this.Name = "NonEmptyTargetControl";
			resources.ApplyResources(this, "$this");
			this.NonBlankTargetGroup.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.sepBox)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
