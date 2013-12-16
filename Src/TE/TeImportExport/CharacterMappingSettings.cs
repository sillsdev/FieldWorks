// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CharacterMappingSettings.cs
// Responsibility: TE Team

using System;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// CharacterMappingSettings is a dialog box with fields to edit an ECMapping for a
	/// character style. It is used in the Import Wizard to edit additional mappings.
	/// </summary>
	public class CharacterMappingSettings : Form, IFWDisposable
	{
		#region Custom events
		/// <summary>Handler for allowing caller to check if proposed mapping is a duplicate.</summary>
		public delegate bool IsDuplicateMappingHandler(string beginMarker);

		/// <summary>Event for allowing caller to check if proposed mapping is a duplicate.</summary>
		public event IsDuplicateMappingHandler IsDuplicateMapping;
		#endregion

		#region Member varibles
		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private FwStyleSheet m_styleSheet;
		/// <summary>Mapping being modified or added</summary>
		protected ImportMappingInfo m_mapping;
		private ResourceManager m_resources;
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.Container components = null;
		/// <summary>exposed to dummy</summary>
		protected System.Windows.Forms.TextBox txtBeginningMarker;
		/// <summary>exposed to dummy</summary>
		protected System.Windows.Forms.TextBox txtEndingMarker;
		private Button btnOk;
		private Button btnHelp;
		/// <summary>exposed to dummy</summary>
		protected MappingDetailsCtrl mappingDetailsCtrl;
		#endregion

		#region Construction, etc
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CharacterMappingSettings"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public CharacterMappingSettings()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for runtime.
		/// </summary>
		/// <param name="mapping">Provides intial values displayed in dialog.</param>
		/// <param name="styleSheet">Provides the character styles user can pick from.</param>
		/// <param name="cache">The DB cache</param>
		/// <param name="fIsAnnotation">if set to <c>true</c> the current tab is for
		/// annotations.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The application</param>
		/// ------------------------------------------------------------------------------------
		public CharacterMappingSettings(ImportMappingInfo mapping, FwStyleSheet styleSheet,
			FdoCache cache, bool fIsAnnotation, IHelpTopicProvider helpTopicProvider, IApp app) :
			this()
		{
			m_resources = new ResourceManager("SIL.FieldWorks.TE.ScrImportComponents",
				Assembly.GetExecutingAssembly());

			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_styleSheet = styleSheet;

			InitializeControls(mapping, fIsAnnotation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Can be called on an existing dialog to modify or create a new mapping.
		/// </summary>
		/// <param name="mapping">Provides initial values displayed in dialog.</param>
		/// <param name="forceAnnotation">true to only allow annotation domain mappings</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeControls(ImportMappingInfo mapping, bool forceAnnotation)
		{
			CheckDisposed();

			Debug.Assert(mapping != null);
			// If we are modifying an existing mapping then load the information about it.
			txtBeginningMarker.Text = mapping.BeginMarker;
			txtEndingMarker.Text = mapping.EndMarker;
			txtBeginningMarker.Focus();
			m_mapping = mapping;

			// Include all character styles
			mappingDetailsCtrl.m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
			// Include footnote paragraph styles because they behave like character mappings.
			mappingDetailsCtrl.m_styleListHelper.UnionIncludeAndTypeFilter = true;

			mappingDetailsCtrl.Initialize(false, m_mapping, m_styleSheet, m_cache, forceAnnotation, false);
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
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

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Label lblBegMarker;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CharacterMappingSettings));
			System.Windows.Forms.Label lblEndMarker;
			System.Windows.Forms.Button btnCancel;
			this.txtBeginningMarker = new System.Windows.Forms.TextBox();
			this.txtEndingMarker = new System.Windows.Forms.TextBox();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.mappingDetailsCtrl = new MappingDetailsCtrl();
			lblBegMarker = new System.Windows.Forms.Label();
			lblEndMarker = new System.Windows.Forms.Label();
			btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lblBegMarker
			//
			resources.ApplyResources(lblBegMarker, "lblBegMarker");
			lblBegMarker.Name = "lblBegMarker";
			//
			// lblEndMarker
			//
			resources.ApplyResources(lblEndMarker, "lblEndMarker");
			lblEndMarker.Name = "lblEndMarker";
			//
			// txtBeginningMarker
			//
			resources.ApplyResources(this.txtBeginningMarker, "txtBeginningMarker");
			this.txtBeginningMarker.Name = "txtBeginningMarker";
			//
			// txtEndingMarker
			//
			resources.ApplyResources(this.txtEndingMarker, "txtEndingMarker");
			this.txtEndingMarker.Name = "txtEndingMarker";
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.Name = "btnOk";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// mappingDetailsCtrl
			//
			resources.ApplyResources(this.mappingDetailsCtrl, "mappingDetailsCtrl");
			this.mappingDetailsCtrl.Name = "mappingDetailsCtrl";
			this.mappingDetailsCtrl.ValidStateChanged += new MappingDetailsCtrl.ValidStateChangedHandler(this.mappingDetailsCtrl1_ValidStateChanged);
			//
			// CharacterMappingSettings
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(this.mappingDetailsCtrl);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.txtEndingMarker);
			this.Controls.Add(lblBegMarker);
			this.Controls.Add(this.txtBeginningMarker);
			this.Controls.Add(lblEndMarker);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CharacterMappingSettings";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Event handlers
		///-------------------------------------------------------------------------------
		/// <summary>
		/// Draws two etched lines on the dialog and places them relative to some
		/// of the other controls on the form.
		/// </summary>
		/// <param name="e"></param>
		///-------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the line between the markers and the exclude check box.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle,
				(mappingDetailsCtrl.Top + txtBeginningMarker.Bottom) / 2);

			// Draw the line separating the buttons from the rest of the form.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle, btnHelp.Bounds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the mapping properties
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void btnOk_Click(object sender, System.EventArgs e)
		{
			CheckDisposed();

			if (!ValidateMarkerCharacters(txtBeginningMarker.Text, true))
				return;

			if (!ValidateMarkerCharacters(txtEndingMarker.Text, false))
				return;

			if (IsDuplicateMapping != null && IsDuplicateMapping(txtBeginningMarker.Text))
			{
				string s = string.Format(m_resources.GetString("kstidImportMappingsDuplicateWarning"),
					txtBeginningMarker.Text);
				DisplayInvalidMappingWarning(s);
				return;
			}

			m_mapping.BeginMarker = txtBeginningMarker.Text;
			m_mapping.EndMarker = txtEndingMarker.Text;

			DialogResult = DialogResult.OK;
			this.Hide();

			SaveMapping();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the mapping details to the mapping stored in m_mapping.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void SaveMapping()
		{
			mappingDetailsCtrl.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpSFMWizardStep4Map-SingleAdditionalMapping");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables or disables the OK button based on the valid state of the MappingDetailsCtrl
		/// </summary>
		/// <param name="sender">mappingDetailsCtrl</param>
		/// <param name="valid">Whether or not the mapping details are in a valid state or not
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void mappingDetailsCtrl1_ValidStateChanged(object sender, bool valid)
		{
			btnOk.Enabled = valid;
		}
		#endregion

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates begin/end marker for additional mapping. Beginning and trailing spaces are
		/// allowed, but not internal spaces.
		/// </summary>
		/// <param name="marker">Marker to be checked</param>
		/// <param name="isBeginMarker">true if begin marker is being checked</param>
		/// <returns>true if marker is valid</returns>
		/// ------------------------------------------------------------------------------------
		private bool ValidateMarkerCharacters(string marker, bool isBeginMarker)
		{
			string trimmedMarker = marker.Trim();
			string msg;

			//check for missing markers
			if (marker.Equals(string.Empty))
			{
				DisplayInvalidMappingWarning(m_resources.GetString(isBeginMarker ?
					"kstidImportMappingsBeginMarkerReqd" : "kstidImportMappingsEndMarkerReqd"));
				return false;
			}

			// Check for spaces within begin marker
			foreach(char ch in trimmedMarker)
			{
				if (ch == ' ')
				{
					msg = string.Format(m_resources.GetString("kstidImportMappingsNoSpace"),
						marker);
					DisplayInvalidMappingWarning(msg);
					return false;
				}
			}

			// Check for a char with a USV greater then U+007F within begin marker
			foreach (char ch in trimmedMarker)
			{
				if (ch > (char)0x007F)
				{
					DisplayInvalidMappingWarning(m_resources.GetString(isBeginMarker ?
						"kstidImportBeginMarkerLowerANSI" :
						"kstidImportEndMarkerLowerANSI"));
					return false;
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called if the user attempts to make a mapping that has a problem.
		/// This is virtual so that test code can override it.
		/// </summary>
		/// <param name="msg">warning to display</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayInvalidMappingWarning(string msg)
		{
			MessageBox.Show(this, msg, m_app.ApplicationName, MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}
		#endregion
	}
}
