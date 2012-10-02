// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2005' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FootnotePropertiesSelector.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices; // needed for Marshal
using System.Windows.Forms.VisualStyles;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Summary description for FootnotePropertiesSelector.
	/// </summary>
	public class FootnotePropertiesSelector : UserControl, IFWDisposable
	{
		#region Member Variables
		private const int kMaxMarkerLength = 3;
		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IVwStylesheet m_styleSheet;
		private FootnotePropertiesSelector m_sibling;
		/// <summary>Used when the general and cross reference footnote types are combined
		/// </summary>
		private bool m_fCombined;

		private System.Windows.Forms.GroupBox grpMarker;
		private System.Windows.Forms.RadioButton optNone;
		private System.Windows.Forms.TextBox txtMarker;
		private System.Windows.Forms.Button btnOptions;
		private System.Windows.Forms.Button btnChooseSymbol;
		private System.Windows.Forms.RadioButton optSymbol;
		private System.Windows.Forms.RadioButton optAlpha;
		private System.Windows.Forms.CheckBox chkShowRef;
		// This must be disposed of properly as a COM object.
		private ILgCharacterPropertyEngine m_cpe = null;
		private bool m_fRestartSequence;
		private CheckBox chkShowCustomSymbol;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		#endregion

		#region Constructors/Destructors/Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// default constructor to make the design mode work
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FootnotePropertiesSelector()
		{
			InitializeComponent();
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				Debug.Assert(m_cpe == null);
				return;
			}

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_cpe = null;
			base.Dispose( disposing );
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the dialog values
		/// </summary>
		/// <param name="cache">The cache</param>
		/// <param name="styleSheet">The style sheet</param>
		/// <param name="footnoteMarkerType">type of footnote marker</param>
		/// <param name="footnoteMarkerSymbol">The symbolic footnote marker. This is only used
		/// when the marker type is "custom symbol" (but a value should be specified regardless
		/// in order to fillin the text box in the UI).</param>
		/// <param name="displayReference">flag whether to display the footnote reference and
		/// check associated check box.</param>
		/// <param name="displayCusSymbol">flag whether to display the custom symbol and
		/// check associated check box.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(FdoCache cache, IVwStylesheet styleSheet,
			FootnoteMarkerTypes footnoteMarkerType, string footnoteMarkerSymbol,
			bool displayReference, bool displayCusSymbol, IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_styleSheet = styleSheet;
			m_fRestartSequence = cache.LangProject.TranslatedScriptureOA.RestartFootnoteSequence;

			if (m_styleSheet is FwStyleSheet)
			{
				string fontFace = ((FwStyleSheet)m_styleSheet).GetFaceNameFromStyle(
					ScrStyleNames.FootnoteMarker,
					cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle,
					cache.LanguageWritingSystemFactoryAccessor);

				txtMarker.Font = new Font(fontFace, 10);
			}
			txtMarker.MaxLength = kMaxMarkerLength;

			switch (footnoteMarkerType)
			{
				case FootnoteMarkerTypes.AutoFootnoteMarker:
					optAlpha.Checked = true;
					chkShowCustomSymbol.Checked = true;
					break;
				case FootnoteMarkerTypes.NoFootnoteMarker:
					optNone.Checked = true;
					chkShowCustomSymbol.Checked = false;
					break;
				case FootnoteMarkerTypes.SymbolicFootnoteMarker:
					optSymbol.Checked = true;
					txtMarker.Text = footnoteMarkerSymbol;
					chkShowCustomSymbol.Checked = displayCusSymbol;
					break;
			}

			chkShowRef.Checked = displayReference;
		}
		#endregion

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FootnotePropertiesSelector));
			this.grpMarker = new System.Windows.Forms.GroupBox();
			this.optNone = new System.Windows.Forms.RadioButton();
			this.txtMarker = new System.Windows.Forms.TextBox();
			this.btnOptions = new System.Windows.Forms.Button();
			this.btnChooseSymbol = new System.Windows.Forms.Button();
			this.optSymbol = new System.Windows.Forms.RadioButton();
			this.optAlpha = new System.Windows.Forms.RadioButton();
			this.chkShowRef = new System.Windows.Forms.CheckBox();
			this.chkShowCustomSymbol = new System.Windows.Forms.CheckBox();
			this.grpMarker.SuspendLayout();
			this.SuspendLayout();
			//
			// grpMarker
			//
			resources.ApplyResources(this.grpMarker, "grpMarker");
			this.grpMarker.Controls.Add(this.optNone);
			this.grpMarker.Controls.Add(this.txtMarker);
			this.grpMarker.Controls.Add(this.btnOptions);
			this.grpMarker.Controls.Add(this.btnChooseSymbol);
			this.grpMarker.Controls.Add(this.optSymbol);
			this.grpMarker.Controls.Add(this.optAlpha);
			this.grpMarker.Name = "grpMarker";
			this.grpMarker.TabStop = false;
			//
			// optNone
			//
			resources.ApplyResources(this.optNone, "optNone");
			this.optNone.Name = "optNone";
			this.optNone.CheckedChanged += new System.EventHandler(this.FormatOptionsCheckedChanged);
			//
			// txtMarker
			//
			resources.ApplyResources(this.txtMarker, "txtMarker");
			this.txtMarker.HideSelection = false;
			this.txtMarker.Name = "txtMarker";
			this.txtMarker.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtMarker_KeyUp);
			this.txtMarker.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMarker_KeyPress);
			this.txtMarker.MouseUp += new System.Windows.Forms.MouseEventHandler(this.txtMarker_MouseUp);
			this.txtMarker.TextChanged += new System.EventHandler(this.txtMarker_TextChanged);
			//
			// btnOptions
			//
			resources.ApplyResources(this.btnOptions, "btnOptions");
			this.btnOptions.Name = "btnOptions";
			this.btnOptions.Click += new System.EventHandler(this.btnOptions_Click);
			//
			// btnChooseSymbol
			//
			resources.ApplyResources(this.btnChooseSymbol, "btnChooseSymbol");
			this.btnChooseSymbol.Name = "btnChooseSymbol";
			this.btnChooseSymbol.Click += new System.EventHandler(this.btnChooseSymbol_Click);
			//
			// optSymbol
			//
			resources.ApplyResources(this.optSymbol, "optSymbol");
			this.optSymbol.Name = "optSymbol";
			this.optSymbol.CheckedChanged += new System.EventHandler(this.FormatOptionsCheckedChanged);
			//
			// optAlpha
			//
			resources.ApplyResources(this.optAlpha, "optAlpha");
			this.optAlpha.Name = "optAlpha";
			this.optAlpha.CheckedChanged += new System.EventHandler(this.FormatOptionsCheckedChanged);
			//
			// chkShowRef
			//
			resources.ApplyResources(this.chkShowRef, "chkShowRef");
			this.chkShowRef.Name = "chkShowRef";
			//
			// chkShowCustomSymbol
			//
			resources.ApplyResources(this.chkShowCustomSymbol, "chkShowCustomSymbol");
			this.chkShowCustomSymbol.Name = "chkShowCustomSymbol";
			//
			// FootnotePropertiesSelector
			//
			this.Controls.Add(this.chkShowCustomSymbol);
			this.Controls.Add(this.chkShowRef);
			this.Controls.Add(this.grpMarker);
			this.Name = "FootnotePropertiesSelector";
			resources.ApplyResources(this, "$this");
			this.grpMarker.ResumeLayout(false);
			this.grpMarker.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether [combined footnotes].
		/// </summary>
		/// <value><c>true</c> if [combined footnotes]; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool CombinedFootnotes
		{
			set
			{
				CheckDisposed();
				m_fCombined = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the sibling properties selector.
		/// </summary>
		/// <value>The sibling properties selector.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public FootnotePropertiesSelector SiblingPropertiesSelector
		{
			set
			{
				CheckDisposed();
				m_sibling = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether or not the sequence button is showing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[Browsable(true)]
		[DefaultValue(true)]
		public bool ShowSequenceButton
		{
			get
			{
				CheckDisposed();
				return btnOptions.Visible;
			}
			set
			{
				CheckDisposed();
				btnOptions.Visible = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the user wants the scripture reference to
		/// appear with their inserted footnote in the footnote pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool ShowScriptureReference
		{
			get
			{
				CheckDisposed();
				return chkShowRef.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the user wants the custom symbol to
		/// appear with their inserted footnote in the footnote pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool ShowCustomSymbol
		{
			get
			{
				CheckDisposed();
				return chkShowCustomSymbol.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the user wants to restart the footnote
		/// sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool RestartFootnoteSequence
		{
			get
			{
				CheckDisposed();
				return m_fRestartSequence;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the symbolic footnote marker. This is only used when the marker
		/// type is "custom symbol".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public string FootnoteMarkerSymbol
		{
			get
			{
				CheckDisposed();
				return txtMarker.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether [enable sequence option].
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool EnableSequenceOption
		{
			set
			{
				CheckDisposed();
				optAlpha.Enabled = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of footnote marker specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public FootnoteMarkerTypes FootnoteMarkerType
		{
			get
			{
				CheckDisposed();

				if (optAlpha.Checked)
					return FootnoteMarkerTypes.AutoFootnoteMarker;

				if (optNone.Checked)
					return FootnoteMarkerTypes.NoFootnoteMarker;

				return FootnoteMarkerTypes.SymbolicFootnoteMarker;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Unicode character properties engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		private ILgCharacterPropertyEngine UnicodeCharProps
		{
			get
			{
				if (m_cpe == null)
					m_cpe = m_cache.ServiceLocator.UnicodeCharProps;
				return m_cpe;
			}
		}
		#endregion

		#region Event Handler Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled states.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateEnabledStates()
		{
			CheckDisposed();

			RadioButton button = null;
			if (optAlpha.Checked)
				button = optAlpha;
			else if (optSymbol.Checked)
				button = optSymbol;
			else
				button = optNone;

			Debug.Assert(m_sibling != null, "need to set a sibling selector");

			btnChooseSymbol.Enabled = (button == optSymbol);
			txtMarker.Enabled = (button == optSymbol);
			if (ShowSequenceButton)
				btnOptions.Enabled = (button == optAlpha);

			if (button == optAlpha)
			{
				chkShowRef.Enabled = true;
				chkShowCustomSymbol.Enabled = false;
				chkShowCustomSymbol.Checked = true;
			}
			else
			{
				m_sibling.optAlpha.Enabled = true;
				chkShowRef.Enabled = false;
				chkShowRef.Checked = true;
				if (button == optNone)
				{
					chkShowCustomSymbol.Enabled = false;
					chkShowCustomSymbol.Checked = false;
				}
				else // button is custom symbol
					chkShowCustomSymbol.Enabled = true;
			}

			// We need to update the states if the combined state changed
			if (m_fCombined)
				optAlpha.Enabled = true;
			else if (button == optAlpha)
			{
				m_sibling.optAlpha.Enabled = false;
				if (m_sibling.optAlpha.Checked)
					m_sibling.optNone.Checked = true;
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void FormatOptionsCheckedChanged(object sender, EventArgs e)
		{
			UpdateEnabledStates();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Choose a symbol for the footnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnChooseSymbol_Click(object sender, System.EventArgs e)
		{
			using (Font fnt = new Font(txtMarker.Font.Name, 20))
			using (SymbolChooserDlg dlg = new SymbolChooserDlg(fnt, UnicodeCharProps, m_helpTopicProvider))
			{
				// Make the initial character in the symbol dialog the last character in the
				// marker string.
				if (txtMarker.Text != string.Empty)
					dlg.ChosenSymbol = txtMarker.Text.Substring(txtMarker.Text.Length - 1, 1);
				else
				{
					dlg.ChosenSymbol = "*";
					txtMarker.SelectionLength = 1;
					txtMarker.SelectionStart = 0;
				}

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					int prevSelStart = txtMarker.SelectionStart;

					// If any text is selected then remove it first.
					if (txtMarker.SelectionLength > 0)
					{
						txtMarker.Text =
							txtMarker.Text.Remove(txtMarker.SelectionStart,
							txtMarker.SelectionLength);
					}

					txtMarker.Text =
						txtMarker.Text.Insert(prevSelStart, dlg.ChosenSymbol);

					txtMarker.SelectionLength = 0;
					txtMarker.SelectionStart =
						(prevSelStart < kMaxMarkerLength ? prevSelStart + 1 : kMaxMarkerLength);
					txtMarker.Focus();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnOptions_Click(object sender, System.EventArgs e)
		{
			using (SequenceOptionsDlg dlg = new SequenceOptionsDlg(m_fRestartSequence, m_helpTopicProvider))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
					m_fRestartSequence = dlg.RestartFootnoteSequence;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtMarker_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			LogicalFont logfont = new LogicalFont(txtMarker.Font);
			bool fSymbolFont = (logfont.lfCharSet == (byte)TextMetricsCharacterSet.Symbol);
			if (UnicodeCharProps.get_IsSeparator(e.KeyChar) || (!fSymbolFont &&
				(UnicodeCharProps.get_IsLetter(e.KeyChar) ||
				UnicodeCharProps.get_IsNumber(e.KeyChar))))
			{
				e.Handled = true;
				MiscUtils.ErrorBeep();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtMarker_TextChanged(object sender, System.EventArgs e)
		{
			CheckChooseSymbolButtonEnablability();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtMarker_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			CheckChooseSymbolButtonEnablability();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtMarker_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			CheckChooseSymbolButtonEnablability();
		}
		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckChooseSymbolButtonEnablability()
		{
			btnChooseSymbol.Enabled = (txtMarker.Text.Length < kMaxMarkerLength ||
				txtMarker.SelectionLength > 0);
		}
		#endregion
	}
}
