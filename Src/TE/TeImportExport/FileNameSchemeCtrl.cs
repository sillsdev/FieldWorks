// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2004' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportUsfmDialog.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using System.Collections.Generic;
using System.Media;

namespace SIL.FieldWorks.TE
{
	#region class FileNameSchemeCtrl
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FileNameSchemeCtrl.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FileNameSchemeCtrl : UserControl, IFWDisposable
	{
		#region Member variables
		private string m_exampleFormat;
		private MarkupType m_markupType = MarkupType.Paratext;
		private bool m_fUserModifiedNamingScheme = false;
		private bool m_fUserModifiedSuffix = false;
		private List<char> m_invalidPathChars;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.GroupBox grpOptions;
		private GroupBox grpTemplate;
		private TextBox txtSuffix;
		private TextBox txtExtension;
		private TextBox txtPrefix;
		/// <summary></summary>
		public FwOverrideComboBox cboScheme;
		private Label lblExample;
		#endregion

		private class TextBoxInfo
		{
			public string text = string.Empty;
			public int selStart = 0;
			public int selLength = 0;
		}

		#region Constructors/Destructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for the <see cref="FileNameSchemeCtrl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileNameSchemeCtrl()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Save the example text (which is a format string) because the {0} will
			// get clobbered the first time we load it with text.
			m_exampleFormat = lblExample.Text;

			m_invalidPathChars = new List<char>(Path.GetInvalidFileNameChars());

			txtExtension.Tag = new TextBoxInfo();
			txtPrefix.Tag = new TextBoxInfo();
			txtSuffix.Tag = new TextBoxInfo();
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Label lblScheme;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileNameSchemeCtrl));
			System.Windows.Forms.Label lblPrefix;
			System.Windows.Forms.Label lblSuffix;
			System.Windows.Forms.Label lblExtension;
			this.grpOptions = new System.Windows.Forms.GroupBox();
			this.grpTemplate = new System.Windows.Forms.GroupBox();
			this.txtSuffix = new System.Windows.Forms.TextBox();
			this.txtExtension = new System.Windows.Forms.TextBox();
			this.txtPrefix = new System.Windows.Forms.TextBox();
			this.cboScheme = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblExample = new System.Windows.Forms.Label();
			lblScheme = new System.Windows.Forms.Label();
			lblPrefix = new System.Windows.Forms.Label();
			lblSuffix = new System.Windows.Forms.Label();
			lblExtension = new System.Windows.Forms.Label();
			this.grpTemplate.SuspendLayout();
			this.SuspendLayout();
			//
			// lblScheme
			//
			resources.ApplyResources(lblScheme, "lblScheme");
			lblScheme.Name = "lblScheme";
			//
			// lblPrefix
			//
			resources.ApplyResources(lblPrefix, "lblPrefix");
			lblPrefix.Name = "lblPrefix";
			//
			// lblSuffix
			//
			resources.ApplyResources(lblSuffix, "lblSuffix");
			lblSuffix.Name = "lblSuffix";
			//
			// lblExtension
			//
			resources.ApplyResources(lblExtension, "lblExtension");
			lblExtension.Name = "lblExtension";
			//
			// grpOptions
			//
			resources.ApplyResources(this.grpOptions, "grpOptions");
			this.grpOptions.Name = "grpOptions";
			this.grpOptions.TabStop = false;
			//
			// grpTemplate
			//
			this.grpTemplate.Controls.Add(lblExtension);
			this.grpTemplate.Controls.Add(lblSuffix);
			this.grpTemplate.Controls.Add(this.txtSuffix);
			this.grpTemplate.Controls.Add(this.txtExtension);
			this.grpTemplate.Controls.Add(lblPrefix);
			this.grpTemplate.Controls.Add(lblScheme);
			this.grpTemplate.Controls.Add(this.txtPrefix);
			this.grpTemplate.Controls.Add(this.cboScheme);
			this.grpTemplate.Controls.Add(this.lblExample);
			resources.ApplyResources(this.grpTemplate, "grpTemplate");
			this.grpTemplate.Name = "grpTemplate";
			this.grpTemplate.TabStop = false;
			//
			// txtSuffix
			//
			resources.ApplyResources(this.txtSuffix, "txtSuffix");
			this.txtSuffix.Name = "txtSuffix";
			this.txtSuffix.TextChanged += new System.EventHandler(this.txtSuffix_TextChanged);
			this.txtSuffix.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSuffix_KeyPress);
			//
			// txtExtension
			//
			resources.ApplyResources(this.txtExtension, "txtExtension");
			this.txtExtension.Name = "txtExtension";
			this.txtExtension.TextChanged += new System.EventHandler(this.txtExtension_TextChanged);
			this.txtExtension.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtExtension_KeyPress);
			//
			// txtPrefix
			//
			resources.ApplyResources(this.txtPrefix, "txtPrefix");
			this.txtPrefix.Name = "txtPrefix";
			this.txtPrefix.TextChanged += new System.EventHandler(this.txtPrefix_TextChanged);
			this.txtPrefix.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtPrefix_KeyPress);
			//
			// cboScheme
			//
			this.cboScheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cboScheme, "cboScheme");
			this.cboScheme.Name = "cboScheme";
			this.cboScheme.SelectedIndexChanged += new System.EventHandler(this.cboScheme_SelectedIndexChanged);
			//
			// lblExample
			//
			resources.ApplyResources(this.lblExample, "lblExample");
			this.lblExample.Name = "lblExample";
			//
			// FileNameSchemeCtrl
			//
			this.Controls.Add(this.grpTemplate);
			this.Name = "FileNameSchemeCtrl";
			resources.ApplyResources(this, "$this");
			this.grpTemplate.ResumeLayout(false);
			this.grpTemplate.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore settings from the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (!DesignMode)
				InitSchemeComboBx();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the combobox for the file naming schemes.
		/// </summary>
		/// <remarks>Public so that we can intialize from tests without showing the dialog.</remarks>
		/// ------------------------------------------------------------------------------------
		public void InitSchemeComboBx()
		{
			CheckDisposed();

			cboScheme.Items.Clear();
			cboScheme.Items.Add(FileNameFormat.GetUiSchemeFormat(m_markupType, FileNameFormat.SchemeFormat.NNBBB));
			cboScheme.Items.Add(FileNameFormat.GetUiSchemeFormat(m_markupType, FileNameFormat.SchemeFormat.BBB));
			cboScheme.Items.Add(FileNameFormat.GetUiSchemeFormat(m_markupType, FileNameFormat.SchemeFormat.NN));

			UpdateFileNameExample();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the example for the file name template.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateFileNameExample()
		{
			lblExample.Text = string.Format(m_exampleFormat,
				txtPrefix.Text.TrimStart(" ".ToCharArray()) +
				cboScheme.Text + txtSuffix.Text + "." + txtExtension.Text.Trim());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle when the scheme for the file name changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void cboScheme_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			UpdateFileNameExample();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Normally, the check for invalid characters can be handled in the delegates for the
		/// key press events for the naming scheme text boxes. However, the user may include
		/// invalid path characters in pasted text, thereby circumventing the check performed
		/// in the key press delegates. This method will make sure that cannot happen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleNamingSchemeTextChanged(TextBox txt)
		{
			if (txt == null)
				return;

			// Replace invalid path characters to underscores.
			StringBuilder bldr = new StringBuilder();
			foreach (char c in txt.Text)
				bldr.Append(m_invalidPathChars.Contains(c) ? '_' : c);

			ChangeTextInTextBox(txt, bldr.ToString());
			UpdateFileNameExample();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the text in the specified text without losing the selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ChangeTextInTextBox(TextBox txt, string newText)
		{
			if (txt == null || txt.Text == newText)
				return;

			// Make sure to save the selection start and length before changing
			// the text. After replacing the text, the selection start and length
			// are restored.
			int selstart = txt.SelectionStart;
			int sellength = txt.SelectionLength;
			txt.Text = newText;
			txt.SelectionStart = selstart;
			txt.SelectionLength = sellength;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle changes to the file naming scheme extension.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtExtension_TextChanged(object sender, System.EventArgs e)
		{
			// Trim all spaces and remove all periods from the extension.
			ChangeTextInTextBox(txtExtension, txtExtension.Text.Trim());
			ChangeTextInTextBox(txtExtension, txtExtension.Text.Replace(".", string.Empty));
			HandleNamingSchemeTextChanged(txtExtension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle changes to the file naming scheme prefix.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtPrefix_TextChanged(object sender, EventArgs e)
		{
			// Trim leading spaces from the prefix.
			ChangeTextInTextBox(txtPrefix, txtPrefix.Text.TrimStart(" ".ToCharArray()));
			HandleNamingSchemeTextChanged(txtPrefix);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle changes to the file naming scheme suffix.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtSuffix_TextChanged(object sender, EventArgs e)
		{
			HandleNamingSchemeTextChanged(txtSuffix);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles events in which the user has manually modified some aspect of the naming
		/// scheme.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleNamingSchemeKeyPress(KeyPressEventArgs e)
		{
			// Don't allow invalid path characters to be entered, but allow
			// the backspace, Ctrl+C, Ctrl+V and Ctrl+X to pass through.
			if (m_invalidPathChars.Contains(e.KeyChar) && e.KeyChar != '\b' &&
				(int)e.KeyChar != 23 && (int)e.KeyChar != 22 && (int)e.KeyChar != 3)
			{
				SystemSounds.Beep.Play();
				e.KeyChar = '\0';
				e.Handled = true;
				return;
			}

			m_fUserModifiedNamingScheme = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the KeyPress event of the suffix text box control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtSuffix_KeyPress(object sender, KeyPressEventArgs e)
		{
			m_fUserModifiedSuffix = true;
			HandleNamingSchemeKeyPress(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the KeyPress event of the prefix text box control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtPrefix_KeyPress(object sender, KeyPressEventArgs e)
		{
			// Don't allow spaces in at the beginning of the prefix.
			if (txtPrefix.SelectionStart == 0 && e.KeyChar == ' ')
			{
				SystemSounds.Beep.Play();
				e.KeyChar = '\0';
				e.Handled = true;
				return;
			}

			HandleNamingSchemeKeyPress(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the KeyPress event of the extension text box control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtExtension_KeyPress(object sender, KeyPressEventArgs e)
		{
			// Don't allow spaces or periods in the extension.
			if (e.KeyChar == ' ' || e.KeyChar == '.')
			{
				SystemSounds.Beep.Play();
				e.KeyChar = '\0';
				e.Handled = true;
				return;
			}

			HandleNamingSchemeKeyPress(e);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the file name format.
		/// </summary>
		/// <value>The file name format.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FileNameFormat FileNameFormat
		{
			get
			{
				CheckDisposed();

				return new FileNameFormat(txtPrefix.Text,
					(FileNameFormat.SchemeFormat)cboScheme.SelectedIndex,
					txtSuffix.Text, txtExtension.Text);
			}
			set
			{
				CheckDisposed();

				txtPrefix.Text = value.m_filePrefix;
				cboScheme.Text = FileNameFormat.GetUiSchemeFormat(m_markupType,
					value.m_schemeFormat);
				txtSuffix.Text = value.m_fileSuffix;
				txtExtension.Text = value.m_fileExtension;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The type of markup format for export: Paratext OR Toolbox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[Browsable(true)]
		[DefaultValue(MarkupType.Paratext)]
		[Category("Behavior")]
		[Description("The type of markup format for export: Paratext or Toolbox.")]
		[Localizable(false)]
		public MarkupType Markup
		{
			get
			{
				CheckDisposed();
				return m_markupType;
			}
			set
			{
				CheckDisposed();

				m_markupType = value;
				if (cboScheme != null)
				{
					cboScheme.Items.Clear();
					cboScheme.Items.Add(FileNameFormat.GetUiSchemeFormat(m_markupType, FileNameFormat.SchemeFormat.NNBBB));
					cboScheme.Items.Add(FileNameFormat.GetUiSchemeFormat(m_markupType, FileNameFormat.SchemeFormat.BBB));
					cboScheme.Items.Add(FileNameFormat.GetUiSchemeFormat(m_markupType, FileNameFormat.SchemeFormat.NN));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The filename prefix
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The filename prefix")]
		[Localizable(false)]
		public string Prefix
		{
			get
			{
				CheckDisposed();
				return txtPrefix.Text;
			}
			set
			{
				CheckDisposed();

				if (!m_fUserModifiedNamingScheme)
				{
					txtPrefix.Text = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The filename suffix
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The filename suffix")]
		[Localizable(false)]
		public string Suffix
		{
			get
			{
				CheckDisposed();
				return txtSuffix.Text;
			}
			set
			{
				CheckDisposed();

				// If the user has manually set the suffix, don't allow programmatic changes.
				if (!m_fUserModifiedSuffix)
					txtSuffix.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The filename scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[Browsable(false)]
		[Description("The filename scheme")]
		[Localizable(false)]
		public string Scheme
		{
			get
			{
				CheckDisposed();
				return cboScheme.Text;
			}
			set
			{
				CheckDisposed();

				if (!m_fUserModifiedNamingScheme)
				{
					if (value == null)
						cboScheme.SelectedIndex = 0;
					else
						cboScheme.Text = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The filename extension
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[Browsable(true)]
		[DefaultValue("sfm")]
		[Category("Appearance")]
		[Description("The filename extension")]
		[Localizable(false)]
		public string Extension
		{
			get
			{
				CheckDisposed();
				return txtExtension.Text;
			}
			set
			{
				CheckDisposed();

				if (!m_fUserModifiedNamingScheme)
				{
					if (value == null)
					{
						// By default use the markup type to determine what the default extension should be.
						txtExtension.Text = m_markupType == MarkupType.Paratext ? "sfm" : "sf";
					}
					else
						txtExtension.Text = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the user modified the suffix.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool UserModifiedSuffix
		{
			get
			{
				CheckDisposed();
				return m_fUserModifiedSuffix;
			}
			set
			{
				CheckDisposed();
				m_fUserModifiedSuffix = value;
			}
		}

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the user modified name scheme so that naming scheme fields can be set again.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearUserModifiedNameScheme()
		{
			m_fUserModifiedNamingScheme = false;
		}
		#endregion
	}

	#endregion

	#region class FileNameFormat
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to specify the format of a file name for USFM export.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FileNameFormat
	{
		/// <summary>File naming scheme format for outputting books in separate files</summary>
		public enum SchemeFormat
		{
			/// <summary>Book number + book code</summary>
			NNBBB,
			/// <summary>Book code only</summary>
			BBB,
			/// <summary>Book number only</summary>
			NN,
		}
		#region Member variables
		/// <summary></summary>
		public string m_filePrefix;
		/// <summary></summary>
		public SchemeFormat m_schemeFormat;
		/// <summary></summary>
		public string m_fileSuffix;
		/// <summary></summary>
		public string m_fileExtension;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FileNameFormat"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileNameFormat()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the format of a file name for USFM export.
		/// </summary>
		/// <param name="filePrefix">The file prefix.</param>
		/// <param name="fileScheme">The file scheme format.</param>
		/// <param name="fileSuffix">The file suffix.</param>
		/// <param name="fileExtension">The file extension.</param>
		/// ------------------------------------------------------------------------------------
		public FileNameFormat(string filePrefix, SchemeFormat fileScheme, string fileSuffix,
			string fileExtension)
		{
			m_filePrefix = filePrefix;
			m_schemeFormat = fileScheme;
			m_fileSuffix = fileSuffix;
			m_fileExtension = fileExtension;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file scheme in the format that Paratext uses internally.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParatextFileScheme
		{
			get { return GetUiSchemeFormat(MarkupType.Paratext, m_schemeFormat); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the scheme format in a format that can be presented in the User Interface (this
		/// is also the format Paratext uses internally).
		/// </summary>
		/// <param name="markup">The markup.</param>
		/// <param name="scheme">The scheme.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetUiSchemeFormat(MarkupType markup, SchemeFormat scheme)
		{
			string mattBookNum = (markup == MarkupType.Paratext) ? "41" : "40";
			switch (scheme)
			{
				default:
				case SchemeFormat.NNBBB:
					return mattBookNum + "MAT";
				case SchemeFormat.BBB:
					return "MAT";
				case SchemeFormat.NN:
					return mattBookNum;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the scheme format based on the format Paratext uses internally.
		/// </summary>
		/// <param name="scheme">The Paratext internal representation of the naming scheme.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public static SchemeFormat GetSchemeFormatFromParatextForm(string scheme)
		{
			switch (scheme)
			{
				default:
				case "41MAT":
					return SchemeFormat.NNBBB;
				case "MAT":
					return SchemeFormat.BBB;
				case "41":
					return SchemeFormat.NN;
			}
		}
	#endregion
	}
}
