using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Keyboarding;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary></summary>
	public class KeyboardControl : UserControl, IFWDisposable
	{
		private FwOverrideComboBox m_keyboardComboBox;
		private FwOverrideComboBox m_langIdComboBox;
		private bool m_fKeymanInitErrorReported;
		private IWritingSystem m_ws;
		private HelpProvider m_helpProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyboardControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyboardControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			m_keyboardComboBox.DropDown += new EventHandler(m_keyboardComboBox_DropDown);
			m_langIdComboBox.DropDown += new EventHandler(m_langIdComboBox_DropDown);
		}

		void m_langIdComboBox_DropDown(object sender, EventArgs e)
		{
			InitLanguageCombo();
		}

		void m_keyboardComboBox_DropDown(object sender, EventArgs e)
		{
			InitKeymanCombo();
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
		/// The larger component using this control must supply a writing system
		/// which this control will help to edit.
		/// </summary>
		public IWritingSystem WritingSystem
		{
			get
			{
				CheckDisposed();
				return m_ws;
			}
			set
			{
				CheckDisposed();
				m_ws = value;
				Reset();
			}
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
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeyboardControl));
			System.Windows.Forms.Label m_langIdLabel;
			System.Windows.Forms.Label m_keyboardLabel;
			this.m_keyboardComboBox = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_langIdComboBox = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			m_langIdLabel = new System.Windows.Forms.Label();
			m_keyboardLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_keyboardComboBox
			//
			this.m_keyboardComboBox.AllowSpaceInEditBox = false;
			this.m_keyboardComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_helpProvider.SetHelpString(this.m_keyboardComboBox, resources.GetString("m_keyboardComboBox.HelpString"));
			resources.ApplyResources(this.m_keyboardComboBox, "m_keyboardComboBox");
			this.m_keyboardComboBox.Name = "m_keyboardComboBox";
			this.m_helpProvider.SetShowHelp(this.m_keyboardComboBox, ((bool)(resources.GetObject("m_keyboardComboBox.ShowHelp"))));
			this.m_keyboardComboBox.Sorted = true;
			this.m_keyboardComboBox.SelectedIndexChanged += new System.EventHandler(this.m_cboKeyboard_SelectedIndexChanged);
			//
			// m_langIdComboBox
			//
			this.m_langIdComboBox.AllowSpaceInEditBox = false;
			this.m_langIdComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_helpProvider.SetHelpString(this.m_langIdComboBox, resources.GetString("m_langIdComboBox.HelpString"));
			resources.ApplyResources(this.m_langIdComboBox, "m_langIdComboBox");
			this.m_langIdComboBox.Name = "m_langIdComboBox";
			this.m_helpProvider.SetShowHelp(this.m_langIdComboBox, ((bool)(resources.GetObject("m_langIdComboBox.ShowHelp"))));
			this.m_langIdComboBox.Sorted = true;
			this.m_langIdComboBox.SelectedIndexChanged += new System.EventHandler(this.m_cbLangId_SelectedIndexChanged);
			//
			// m_langIdLabel
			//
			resources.ApplyResources(m_langIdLabel, "m_langIdLabel");
			m_langIdLabel.BackColor = System.Drawing.Color.Transparent;
			m_langIdLabel.Name = "m_langIdLabel";
			//
			// m_keyboardLabel
			//
			resources.ApplyResources(m_keyboardLabel, "m_keyboardLabel");
			m_keyboardLabel.BackColor = System.Drawing.Color.Transparent;
			m_keyboardLabel.Name = "m_keyboardLabel";
			//
			// KeyboardControl
			//
			this.Controls.Add(this.m_keyboardComboBox);
			this.Controls.Add(this.m_langIdComboBox);
			this.Controls.Add(m_keyboardLabel);
			this.Controls.Add(m_langIdLabel);
			this.Name = "KeyboardControl";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// Resets this instance.
		/// </summary>
		public void Reset()
		{
			if (m_ws == null)
				return;

			InitKeymanCombo();
			InitLanguageCombo();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the keyman combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitKeymanCombo()
		{
			CheckDisposed();

			m_keyboardComboBox.Items.Clear(); // Clear out any old items from combobox list
			var strKbdName = string.IsNullOrEmpty(m_ws.Keyboard) ? FwCoreDlgControls.kstid_None :
				m_ws.Keyboard;
			m_keyboardComboBox.Items.Add(FwCoreDlgControls.kstid_None);

			var badLocales = KeyboardController.ErrorKeyboards.Where(
				keyboard => keyboard.Type == KeyboardType.OtherIm).ToList();
			if (badLocales.Count > 0 && !m_fKeymanInitErrorReported)
			{
				m_fKeymanInitErrorReported = true;
				string caption = FwCoreDlgControls.kstidKeymanInitFailed;
				var exception = badLocales[0].Details as Exception;
				string message = exception != null ? exception.Message : null;
				if (string.IsNullOrEmpty(message))
					message = caption;
				MessageBoxUtils.Show(ParentForm, message, caption,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			foreach (var item in KeyboardController.InstalledKeyboards.Where(
				keyboard => keyboard.Type == KeyboardType.OtherIm))
			{
				m_keyboardComboBox.Items.Add(item.Name);
			}

			m_keyboardComboBox.SelectedItem = strKbdName;
		}

		// Since InitLanguageCombo gets called from an OnGetFocus, and the message box causes a
		// change in focus, we need to avoid an endless loop of error messages.
		private static bool errorMessage1Out;
		private static bool errorMessage2Out;

		/// <summary>
		/// Resets the error messages. This is needed for unit tests.
		/// </summary>
		public static void ResetErrorMessages()
		{
			errorMessage1Out = false;
			errorMessage2Out = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the language combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitLanguageCombo()
		{
			CheckDisposed();

			m_langIdComboBox.Items.Clear(); // Clear out any old items.
			string selectedName = null;
			int selectedId = m_ws.LCID;
			foreach (var item in KeyboardController.InstalledKeyboards.Where(
				keyboard => keyboard.Type == KeyboardType.System))
			{
				try
				{
					m_langIdComboBox.Items.Add(item);
					// The 'if' below should make a 'fr-CAN' language choose a french keyboard, if installed.
					if (item.Id == selectedId)
						selectedName = item.Name;
				}
				catch
				{
					// Problem adding a language to the combo box. Notify user and continue.
					if (errorMessage1Out == false)
					{
						errorMessage1Out = true;
						MessageBoxUtils.Show(ParentForm, FwCoreDlgControls.kstidBadLanguageName,
								FwCoreDlgControls.kstidError, MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					break;
				}
			}

			var badLocales = KeyboardController.ErrorKeyboards.Where(
				keyboard => keyboard.Type == KeyboardType.System).ToList();
			if (badLocales.Count > 0 && errorMessage2Out == false)
			{
				errorMessage2Out = true;
				string strBadLocales = badLocales.Aggregate("", (current, loc) => current + (loc.Details + ", "));
				strBadLocales = strBadLocales.Substring(0, strBadLocales.Length - 2);
				string caption = FwCoreDlgControls.kstidError;
				MessageBoxUtils.Show(ParentForm, String.Format(FwCoreDlgControls.kstidBadLocales,
					strBadLocales), caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			if (selectedName == null)
			{
				try
				{
					// Try selecting the default language
					selectedName = InputLanguage.DefaultInputLanguage.Culture.DisplayName;
				}
				catch
				{
					selectedName = FwCoreDlgControls.kstidInvalidKeyboard;
				}
				finally
				{
					// The DefaultInputLanguage should already be in the control
					if (selectedName == FwCoreDlgControls.kstidInvalidKeyboard)
					{
						m_langIdComboBox.Items.Add(new KeyboardDescription(selectedId, selectedName, null));
					}
				}
			}
			int idx = m_langIdComboBox.FindStringExact(selectedName, -1);
			m_langIdComboBox.SelectedIndex = idx;
		}

		private void m_cbLangId_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_langIdComboBox.SelectedItem == null)
				return;
			m_ws.LCID = ((IKeyboardDescription)m_langIdComboBox.SelectedItem).Id;
		}

		private void m_cboKeyboard_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_keyboardComboBox.SelectedIndex >= 0)
			{
				string str = m_keyboardComboBox.Text;
				if (str == FwCoreDlgControls.kstid_None)
					str = null;
				m_ws.Keyboard = str;
			}
		}
	}
}
