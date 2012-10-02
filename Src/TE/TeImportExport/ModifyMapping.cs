// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ModifyMapping.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Resources;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ModifyMapping.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class ModifyMapping : Form, IFWDisposable
	{
		#region Data members
		/// <summary></summary>
		protected IVwStylesheet m_StyleSheet;

		private IHelpTopicProvider m_helpTopicProvider;
		private Button btnHelp;
		private Button btnOk;
		private Label markerLabel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;
		private Label endMarkerLabel;
		/// <summary></summary>
		public MappingDetailsCtrl mappingDetailsCtrl;
		#endregion

		#region Constructors/Init/Destructors
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ModifyMapping"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ModifyMapping()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this dialog
		/// </summary>
		/// <param name="fParatextMapping"><c>true</c> if a Paratext mapping is being modified;
		/// <c>false</c> otherwise</param>
		/// <param name="mapping">Mapping object being modified</param>
		/// <param name="styleSheet">Stylesheet containing styles that will appear in the list</param>
		/// <param name="cache">The cache representing the DB connection</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="isAnnotationMapping">If <c>true</c>, forces this mapping to be in the
		/// Annotation domain.</param>
		/// <param name="fBackTransDomainLocked">If <c>true</c>, won't allow the user to
		/// check or clear the BT checkbox. If the incoming mapping is for the back translation
		/// and has a domain of either Scripture or Footnote, these two domains remain
		/// enabled so the user can switch between them, but the Notes domain will be
		/// disabled. If the incoming mapping is not for the back translation, then
		/// this only affects the BT checkbox, not the domain options.</param>
		/// <remarks>We separated this from the constructor so that we can create a mock object
		/// for testing purposes.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void Initialize(bool fParatextMapping, ImportMappingInfo mapping,
			FwStyleSheet styleSheet, FdoCache cache, IHelpTopicProvider helpTopicProvider,
			bool isAnnotationMapping, bool fBackTransDomainLocked)
		{
			CheckDisposed();
			m_helpTopicProvider = helpTopicProvider;
			this.markerLabel.Text = mapping.BeginMarker;
			this.endMarkerLabel.Text = mapping.EndMarker;
			this.mappingDetailsCtrl.Initialize(fParatextMapping, mapping, styleSheet, cache,
				isAnnotationMapping, fBackTransDomainLocked);
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
		/// 		/// -----------------------------------------------------------------------------------
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModifyMapping));
			System.Windows.Forms.Button btnCancel;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label3;
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.markerLabel = new System.Windows.Forms.Label();
			this.mappingDetailsCtrl = new MappingDetailsCtrl();
			this.endMarkerLabel = new System.Windows.Forms.Label();
			btnCancel = new System.Windows.Forms.Button();
			label1 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnCancel
			//
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.Name = "btnCancel";
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.Name = "btnOk";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// markerLabel
			//
			resources.ApplyResources(this.markerLabel, "markerLabel");
			this.markerLabel.Name = "markerLabel";
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// mappingDetailsCtrl
			//
			resources.ApplyResources(this.mappingDetailsCtrl, "mappingDetailsCtrl");
			this.mappingDetailsCtrl.Name = "mappingDetailsCtrl";
			this.mappingDetailsCtrl.ValidStateChanged += new MappingDetailsCtrl.ValidStateChangedHandler(this.mappingDetailsCtrl_ValidStateChanged);
			//
			// endMarkerLabel
			//
			resources.ApplyResources(this.endMarkerLabel, "endMarkerLabel");
			this.endMarkerLabel.Name = "endMarkerLabel";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// ModifyMapping
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(this.endMarkerLabel);
			this.Controls.Add(label3);
			this.Controls.Add(this.mappingDetailsCtrl);
			this.Controls.Add(this.markerLabel);
			this.Controls.Add(label1);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ModifyMapping";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwOverrideComboBox WritingCombobox
		{
			get
			{
				CheckDisposed();
				return this.mappingDetailsCtrl.cboWritingSys;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RadioButton Scripture
		{
			get
			{
				CheckDisposed();
				return this.mappingDetailsCtrl.rbtnScripture;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckBox BackTranslation
		{
			get
			{
				CheckDisposed();
				return this.mappingDetailsCtrl.chkBackTranslation;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RadioButton Footnotes
		{
			get
			{
				CheckDisposed();
				return this.mappingDetailsCtrl.rbtnFootnotes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RadioButton Notes
		{
			get
			{
				CheckDisposed();
				return this.mappingDetailsCtrl.rbtnNotes;
			}
		}
		#endregion

		#region Other Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dialog result. We added this property so that we can mock the dialog.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual DialogResult GetDialogResult
		{
			get
			{
				CheckDisposed();
				return DialogResult;
			}
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will draw the the etched line that separates the dialog's buttons at the
		/// bottom from the rest of the dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the line between the marker name and the exclude check box.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle,
				(mappingDetailsCtrl.Top + markerLabel.Bottom) / 2);

			// Draw the line separating the buttons from the rest of the form.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle, btnHelp.Bounds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, System.EventArgs e)
		{
			mappingDetailsCtrl.Save();
			DialogResult = DialogResult.OK;
			Hide();
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
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpSFMWizardStep4Map-Modify");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="valid"></param>
		/// ------------------------------------------------------------------------------------
		private void mappingDetailsCtrl_ValidStateChanged(object sender, bool valid)
		{
			btnOk.Enabled = valid;
		}
		#endregion
	}
}
