using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// NonEmptyTargetControl is a small reusable piece currently used in the BulkEditBar.
	/// It allows three options to be chosen between for dealing with non-empty target fields
	/// in bulk copy and transduce. Additionally a separator can be chosen for the append option.
	/// </summary>
	public class NonEmptyTargetControl : UserControl, IFWDisposable
	{
		private System.Windows.Forms.GroupBox NonBlankTargetGroup;
		private System.Windows.Forms.RadioButton appendRadio;
		private System.Windows.Forms.RadioButton overwriteRadio;
		private System.Windows.Forms.RadioButton doNothingRadio;
		private SIL.FieldWorks.Common.Widgets.FwTextBox sepBox;
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
			this.sepBox.WritingSystemCode = 0;
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
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// This is the main point of the class: to find out how the user wants to handle non-empty
		/// target fields.
		/// </summary>
		public NonEmptyTargetOptions NonEmptyMode
		{
			get
			{
				CheckDisposed();

				if (this.overwriteRadio.Checked)
					return NonEmptyTargetOptions.Overwrite;
				else if (this.appendRadio.Checked)
					return NonEmptyTargetOptions.Append;
				else
					return NonEmptyTargetOptions.DoNothing;
			}

			set
			{
				RadioButton checkedButton = null;
				switch (value)
				{
					case NonEmptyTargetOptions.Append:
						checkedButton = this.appendRadio;
						break;
					case NonEmptyTargetOptions.Overwrite:
						checkedButton = this.overwriteRadio;
						break;
					case NonEmptyTargetOptions.DoNothing:
					default:
						checkedButton = this.doNothingRadio;
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
				CheckDisposed();
				return sepBox.WritingSystemFactory;
			}
			set
			{
				CheckDisposed();
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
				CheckDisposed();
				return sepBox.WritingSystemCode;
			}
			set
			{
				CheckDisposed();
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
				CheckDisposed();
				return sepBox.Tss;
			}
			set
			{
				CheckDisposed();
				sepBox.Tss = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the separator.
		/// </summary>
		/// <value>The separator.</value>
		/// ------------------------------------------------------------------------------------
		public string Separator
		{
			get
			{
				CheckDisposed();
				return sepBox.Text;
			}
			set
			{
				CheckDisposed();
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
			this.sepBox.AllowMultipleLines = false;
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

	/// <summary>
	/// Options for dealing with non-empty targets.
	/// </summary>
	public enum NonEmptyTargetOptions
	{
		/// <summary>
		/// Leave the non-empty value alone.
		/// </summary>
		DoNothing,
		/// <summary>
		/// Overwrite the non-empty target with the computed/copied value
		/// </summary>
		Overwrite,
		/// <summary>
		/// Append the computed/copied value to the non-empty target.
		/// </summary>
		Append
	}
}
