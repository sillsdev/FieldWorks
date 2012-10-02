// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DefaultFontControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32; // For registry stuff.

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// Summary description for DefaultFontsControl.
	/// </summary>
	public class DefaultFontsControl : UserControl, IFWDisposable
	{
		#region Member variables
		private FwOverrideComboBox cbDefaultNormalFont;
		private System.Windows.Forms.Label lblDefaultNormalFont;
		private System.Windows.Forms.Label lblDefaultHeadingFont;
		private FwOverrideComboBox cbDefaultHeadingFont;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private SIL.FieldWorks.FwCoreDlgControls.FontFeaturesButton m_defaultFontFeaturesBtn;
		private SIL.FieldWorks.FwCoreDlgControls.FontFeaturesButton m_headingFontFeaturesBtn;
		private SIL.FieldWorks.FwCoreDlgControls.FontFeaturesButton m_pubFontFeaturesBtn;
		private System.Windows.Forms.HelpProvider helpProvider1;

		private LanguageDefinition m_langDef;
		private FwOverrideComboBox cbDefaultPubFont;
		private Label lblDefaultPubFont;
		private bool m_fNewRendering = false;	// the writing system rendering has changed.
		#endregion

		#region Constructor/destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DefaultFontsControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DefaultFontsControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Fill in the font selection combo boxes.
			InstalledFontCollection installedFontCollection = new InstalledFontCollection();
			FontFamily[] fontFamilies = installedFontCollection.Families;
			int count = fontFamilies.Length;
			for (int i = 0; i < count; ++i)
			{
				// The .NET framework is unforgiving of using a font that doesn't support the
				// "regular"  style.  So we won't allow the user to even see them...
				if (fontFamilies[i].IsStyleAvailable(FontStyle.Regular))
				{
					string familyName = fontFamilies[i].Name;
					cbDefaultNormalFont.Items.Add(familyName);
					cbDefaultHeadingFont.Items.Add(familyName);
					cbDefaultPubFont.Items.Add(familyName);
					fontFamilies[i].Dispose();
				}
			}
			installedFontCollection.Dispose();

			// Add our event handlers.
			cbDefaultNormalFont.SelectedIndexChanged +=
				new EventHandler(cbDefaultNormalFont_SelectedIndexChanged);
			m_defaultFontFeaturesBtn.FontFeatureSelected +=
				new EventHandler(m_defaultFontFeatures_FontFeatureSelected);
			cbDefaultHeadingFont.SelectedIndexChanged +=
				new EventHandler(cbDefaultHeadingFont_SelectedIndexChanged);
			m_headingFontFeaturesBtn.FontFeatureSelected +=
				new EventHandler(m_headingFontFeatures_FontFeatureSelected);
			cbDefaultPubFont.SelectedIndexChanged +=
				new EventHandler(cbDefaultPubFont_SelectedIndexChanged);
			m_pubFontFeaturesBtn.FontFeatureSelected +=
				new EventHandler(m_bodyFontFeaturesBtn_FontFeatureSelected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The language definition object that the controls will modify and
		/// from which they will be initialized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LanguageDefinition LangDef
		{
			get
			{
				CheckDisposed();
				return m_langDef;
			}
			set
			{
				CheckDisposed();

				m_langDef = value;
				SetSelectedFonts();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Access the Default Normal Font property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultNormalFont
		{
			get
			{
				CheckDisposed();
				return cbDefaultNormalFont.Text;
			}
			set
			{
				CheckDisposed();
				cbDefaultNormalFont.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Default Heading Font property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultHeadingFont
		{
			get
			{
				CheckDisposed();
				return cbDefaultHeadingFont.Text;
			}
			set
			{
				CheckDisposed();
				cbDefaultHeadingFont.Text = value;
				// If the specified font is not available, use the default font.
				if (cbDefaultHeadingFont.Text != value)
					cbDefaultHeadingFont.Text = cbDefaultNormalFont.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default publication font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultPublicationFont
		{
			get
			{
				CheckDisposed();
				return cbDefaultPubFont.Text;
			}
			set
			{
				CheckDisposed();
				cbDefaultPubFont.Text = value;
				// If the specified font is not available, use the default font.
				if (cbDefaultPubFont.Text != value)
					cbDefaultPubFont.Text = cbDefaultNormalFont.Text;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if a font or a font property changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NewRenderingNeeded
		{
			get
			{
				CheckDisposed();
				return m_fNewRendering;
			}
		}
		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DefaultFontsControl));
			this.lblDefaultNormalFont = new System.Windows.Forms.Label();
			this.lblDefaultHeadingFont = new System.Windows.Forms.Label();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.lblDefaultPubFont = new System.Windows.Forms.Label();
			this.cbDefaultPubFont = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.cbDefaultNormalFont = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.cbDefaultHeadingFont = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_pubFontFeaturesBtn = new SIL.FieldWorks.FwCoreDlgControls.FontFeaturesButton();
			this.m_headingFontFeaturesBtn = new SIL.FieldWorks.FwCoreDlgControls.FontFeaturesButton();
			this.m_defaultFontFeaturesBtn = new SIL.FieldWorks.FwCoreDlgControls.FontFeaturesButton();
			this.SuspendLayout();
			//
			// lblDefaultNormalFont
			//
			resources.ApplyResources(this.lblDefaultNormalFont, "lblDefaultNormalFont");
			this.lblDefaultNormalFont.BackColor = System.Drawing.Color.Transparent;
			this.lblDefaultNormalFont.Name = "lblDefaultNormalFont";
			this.helpProvider1.SetShowHelp(this.lblDefaultNormalFont, ((bool)(resources.GetObject("lblDefaultNormalFont.ShowHelp"))));
			//
			// lblDefaultHeadingFont
			//
			resources.ApplyResources(this.lblDefaultHeadingFont, "lblDefaultHeadingFont");
			this.lblDefaultHeadingFont.BackColor = System.Drawing.Color.Transparent;
			this.lblDefaultHeadingFont.Name = "lblDefaultHeadingFont";
			this.helpProvider1.SetShowHelp(this.lblDefaultHeadingFont, ((bool)(resources.GetObject("lblDefaultHeadingFont.ShowHelp"))));
			//
			// lblDefaultPubFont
			//
			resources.ApplyResources(this.lblDefaultPubFont, "lblDefaultPubFont");
			this.lblDefaultPubFont.BackColor = System.Drawing.Color.Transparent;
			this.lblDefaultPubFont.Name = "lblDefaultPubFont";
			this.helpProvider1.SetShowHelp(this.lblDefaultPubFont, ((bool)(resources.GetObject("lblDefaultPubFont.ShowHelp"))));
			//
			// cbDefaultPubFont
			//
			this.cbDefaultPubFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.cbDefaultPubFont, resources.GetString("cbDefaultPubFont.HelpString"));
			resources.ApplyResources(this.cbDefaultPubFont, "cbDefaultPubFont");
			this.cbDefaultPubFont.Name = "cbDefaultPubFont";
			this.helpProvider1.SetShowHelp(this.cbDefaultPubFont, ((bool)(resources.GetObject("cbDefaultPubFont.ShowHelp"))));
			this.cbDefaultPubFont.SelectedIndexChanged += new System.EventHandler(this.cbDefaultPubFont_SelectedIndexChanged);
			//
			// cbDefaultNormalFont
			//
			this.cbDefaultNormalFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.cbDefaultNormalFont, resources.GetString("cbDefaultNormalFont.HelpString"));
			resources.ApplyResources(this.cbDefaultNormalFont, "cbDefaultNormalFont");
			this.cbDefaultNormalFont.Name = "cbDefaultNormalFont";
			this.helpProvider1.SetShowHelp(this.cbDefaultNormalFont, ((bool)(resources.GetObject("cbDefaultNormalFont.ShowHelp"))));
			//
			// cbDefaultHeadingFont
			//
			this.cbDefaultHeadingFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.cbDefaultHeadingFont, resources.GetString("cbDefaultHeadingFont.HelpString"));
			resources.ApplyResources(this.cbDefaultHeadingFont, "cbDefaultHeadingFont");
			this.cbDefaultHeadingFont.Name = "cbDefaultHeadingFont";
			this.helpProvider1.SetShowHelp(this.cbDefaultHeadingFont, ((bool)(resources.GetObject("cbDefaultHeadingFont.ShowHelp"))));
			//
			// m_pubFontFeaturesBtn
			//
			resources.ApplyResources(this.m_pubFontFeaturesBtn, "m_pubFontFeaturesBtn");
			this.m_pubFontFeaturesBtn.FontFeatures = null;
			this.m_pubFontFeaturesBtn.FontName = null;
			this.helpProvider1.SetHelpString(this.m_pubFontFeaturesBtn, resources.GetString("m_pubFontFeaturesBtn.HelpString"));
			this.m_pubFontFeaturesBtn.Name = "m_pubFontFeaturesBtn";
			this.helpProvider1.SetShowHelp(this.m_pubFontFeaturesBtn, ((bool)(resources.GetObject("m_pubFontFeaturesBtn.ShowHelp"))));
			this.m_pubFontFeaturesBtn.WritingSystemFactory = null;
			this.m_pubFontFeaturesBtn.FontFeatureSelected += new System.EventHandler(this.m_bodyFontFeaturesBtn_FontFeatureSelected);
			//
			// m_headingFontFeaturesBtn
			//
			resources.ApplyResources(this.m_headingFontFeaturesBtn, "m_headingFontFeaturesBtn");
			this.m_headingFontFeaturesBtn.FontFeatures = null;
			this.m_headingFontFeaturesBtn.FontName = null;
			this.helpProvider1.SetHelpString(this.m_headingFontFeaturesBtn, resources.GetString("m_headingFontFeaturesBtn.HelpString"));
			this.m_headingFontFeaturesBtn.Name = "m_headingFontFeaturesBtn";
			this.helpProvider1.SetShowHelp(this.m_headingFontFeaturesBtn, ((bool)(resources.GetObject("m_headingFontFeaturesBtn.ShowHelp"))));
			this.m_headingFontFeaturesBtn.WritingSystemFactory = null;
			//
			// m_defaultFontFeaturesBtn
			//
			resources.ApplyResources(this.m_defaultFontFeaturesBtn, "m_defaultFontFeaturesBtn");
			this.m_defaultFontFeaturesBtn.FontFeatures = null;
			this.m_defaultFontFeaturesBtn.FontName = null;
			this.helpProvider1.SetHelpString(this.m_defaultFontFeaturesBtn, resources.GetString("m_defaultFontFeaturesBtn.HelpString"));
			this.m_defaultFontFeaturesBtn.Name = "m_defaultFontFeaturesBtn";
			this.helpProvider1.SetShowHelp(this.m_defaultFontFeaturesBtn, ((bool)(resources.GetObject("m_defaultFontFeaturesBtn.ShowHelp"))));
			this.m_defaultFontFeaturesBtn.WritingSystemFactory = null;
			//
			// DefaultFontsControl
			//
			this.Controls.Add(this.m_pubFontFeaturesBtn);
			this.Controls.Add(this.lblDefaultPubFont);
			this.Controls.Add(this.cbDefaultPubFont);
			this.Controls.Add(this.m_headingFontFeaturesBtn);
			this.Controls.Add(this.m_defaultFontFeaturesBtn);
			this.Controls.Add(this.cbDefaultNormalFont);
			this.Controls.Add(this.lblDefaultNormalFont);
			this.Controls.Add(this.lblDefaultHeadingFont);
			this.Controls.Add(this.cbDefaultHeadingFont);
			this.Name = "DefaultFontsControl";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Select the Fonts in the Font comboboxes, and set any features into the controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetSelectedFonts()
		{
			if (m_langDef == null)
				return;		// can't do anything useful.

			// setup controls for default font
			SetFontInCombo(cbDefaultNormalFont, m_langDef.WritingSystem.DefaultSerif);
			m_defaultFontFeaturesBtn.WritingSystemFactory =
				m_langDef.WritingSystem.WritingSystemFactory;
			m_defaultFontFeaturesBtn.FontName = cbDefaultNormalFont.Text;
			m_defaultFontFeaturesBtn.FontFeatures = m_langDef.WritingSystem.FontVariation;

			// setup controls for default body (publiation) font
			SetFontInCombo(cbDefaultPubFont, m_langDef.WritingSystem.DefaultBodyFont);
			m_pubFontFeaturesBtn.WritingSystemFactory =
				m_langDef.WritingSystem.WritingSystemFactory;
			m_pubFontFeaturesBtn.FontName = cbDefaultPubFont.Text;
			m_pubFontFeaturesBtn.FontFeatures = m_langDef.WritingSystem.BodyFontFeatures;

			// setup controls for default heading font
			SetFontInCombo(cbDefaultHeadingFont, m_langDef.WritingSystem.DefaultSansSerif);
			m_headingFontFeaturesBtn.WritingSystemFactory =
				m_langDef.WritingSystem.WritingSystemFactory;
			m_headingFontFeaturesBtn.FontName = cbDefaultHeadingFont.Text;
			m_headingFontFeaturesBtn.FontFeatures =
				m_langDef.WritingSystem.SansFontVariation;
			m_fNewRendering = false;
		}

		/// <summary>
		/// Set the font in the ComboBox.  If the selection ends up null, mark the font as
		/// missing and display it as such.  See LT-8750.
		/// </summary>
		/// <param name="fwcb"></param>
		/// <param name="sFont"></param>
		private static void SetFontInCombo(FwOverrideComboBox fwcb, string sFont)
		{
			fwcb.SelectedItem = sFont;
			if (fwcb.SelectedItem == null)
			{
				string sMissingFmt = FwCoreDlgControls.kstidMissingFontFmt;
				string sMissing = String.Format(sMissingFmt, sFont);
				fwcb.SelectedItem = sMissing;
				if (fwcb.SelectedItem == null)
				{
					fwcb.Items.Add(sMissing);
					fwcb.SelectedItem = sMissing;
				}
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cbDefaultNormalFont control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void cbDefaultNormalFont_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (m_langDef == null)
				return;
			string oldFont = m_langDef.WritingSystem.DefaultSerif;
			if (oldFont != cbDefaultNormalFont.Text)
			{
				m_langDef.WritingSystem.DefaultSerif = cbDefaultNormalFont.Text;
				m_langDef.WritingSystem.FontVariation = "";
				m_defaultFontFeaturesBtn.FontName = cbDefaultNormalFont.Text;
				m_defaultFontFeaturesBtn.FontFeatures = "";
				m_fNewRendering = true;		// the writing system rendering has changed.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cbDefaultHeadingFont control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void cbDefaultHeadingFont_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (m_langDef == null)
				return;
			string oldFont = m_langDef.WritingSystem.DefaultSansSerif;
			if (oldFont != cbDefaultHeadingFont.Text)
			{
				m_langDef.WritingSystem.DefaultSansSerif = cbDefaultHeadingFont.Text;
				m_langDef.WritingSystem.SansFontVariation = "";
				m_headingFontFeaturesBtn.FontName = cbDefaultHeadingFont.Text;
				m_headingFontFeaturesBtn.FontFeatures = "";
				m_fNewRendering = true;		// the writing system rendering has changed.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cbDefaultPubFont control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cbDefaultPubFont_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_langDef == null)
				return;
			string oldFont = m_langDef.WritingSystem.DefaultBodyFont;
			if (oldFont != cbDefaultPubFont.Text)
			{
				m_langDef.WritingSystem.DefaultBodyFont = cbDefaultPubFont.Text;
				m_langDef.WritingSystem.BodyFontFeatures = "";
				m_pubFontFeaturesBtn.FontName = cbDefaultPubFont.Text;
				m_pubFontFeaturesBtn.FontFeatures = "";
				m_fNewRendering = true;		// the writing system rendering has changed.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the FontFeatureSelected event of the m_defaultFontFeatures control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_defaultFontFeatures_FontFeatureSelected(object sender, EventArgs e)
		{
			if (m_langDef == null)
				return;
			m_langDef.WritingSystem.FontVariation = m_defaultFontFeaturesBtn.FontFeatures;
			m_fNewRendering = true;		// the writing system rendering has changed.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the FontFeatureSelected event of the m_headingFontFeatures control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_headingFontFeatures_FontFeatureSelected(object sender, EventArgs e)
		{
			if (m_langDef == null)
				return;
			m_langDef.WritingSystem.SansFontVariation = m_headingFontFeaturesBtn.FontFeatures;
			m_fNewRendering = true;		// the writing system rendering has changed.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the FontFeatureSelected event of the m_bodyFontFeaturesBtn control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_bodyFontFeaturesBtn_FontFeatureSelected(object sender, EventArgs e)
		{
			if (m_langDef == null)
				return;
			m_langDef.WritingSystem.BodyFontFeatures = m_pubFontFeaturesBtn.FontFeatures;
			m_fNewRendering = true;		// the writing system rendering has changed.
		}
		#endregion
	}
}
