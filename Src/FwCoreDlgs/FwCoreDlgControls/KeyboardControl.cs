using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary></summary>
	public class KeyboardControl : UserControl, IFWDisposable
	{
		private System.Windows.Forms.Label m_langIdLabel;
		private System.Windows.Forms.Label m_keyboardLabel;
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

			// FWNX-498 Different UI for IBus in Linux
			// Just one keyboard combo box
			if (MiscUtils.IsUnix)
			{
				m_keyboardLabel.Text = FwCoreDlgControls.kstidKeyboard;
				// Move Keyboard combo box up
				if (m_langIdLabel.Top < m_keyboardLabel.Top)
				{
					m_keyboardComboBox.Top -= m_langIdComboBox.Height + m_langIdLabel.Height;
					m_keyboardLabel.Top -= m_langIdComboBox.Height + m_langIdLabel.Height;
				}
				Controls.Remove(m_langIdComboBox);
				Controls.Remove(m_langIdLabel);
			}
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
			this.m_keyboardComboBox = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_langIdComboBox = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_langIdLabel = new System.Windows.Forms.Label();
			this.m_keyboardLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_langIdLabel
			//
			resources.ApplyResources(this.m_langIdLabel, "m_langIdLabel");
			this.m_langIdLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_langIdLabel.Name = "m_langIdLabel";
			//
			// m_keyboardLabel
			//
			resources.ApplyResources(this.m_keyboardLabel, "m_keyboardLabel");
			this.m_keyboardLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_keyboardLabel.Name = "m_keyboardLabel";
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
			// KeyboardControl
			//
			this.Controls.Add(this.m_keyboardComboBox);
			this.Controls.Add(this.m_langIdComboBox);
			this.Controls.Add(this.m_keyboardLabel);
			this.Controls.Add(this.m_langIdLabel);
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
			if (!MiscUtils.IsUnix)
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
			string strKbdName = m_ws.Keyboard ?? FwCoreDlgControls.kstid_None;
			m_keyboardComboBox.Items.Add(FwCoreDlgControls.kstid_None);

			var keyboards = GetAvailableKeyboards(exception => {
				if (!m_fKeymanInitErrorReported)
				{
					m_fKeymanInitErrorReported = true;
					string caption = FwCoreDlgControls.kstidKeymanInitFailed;
					string message = exception.Message;
					if (string.IsNullOrEmpty(message))
						message = caption;
					MessageBoxUtils.Show(ParentForm, message, caption,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				});

			foreach (var keyboard in keyboards)
				m_keyboardComboBox.Items.Add(keyboard);

			m_keyboardComboBox.SelectedItem = strKbdName;
			}

		/// <summary>
		/// Get available IBus (Linux) or Keyman (Windows) keyboards.
		/// </summary>
		/// <param name="doIfError">
		/// Delegate to run if KeymanHandler.Init throws an exception. Takes the exception
		/// as an argument.
		/// </param>
		private static IEnumerable<string> GetAvailableKeyboards(Action<Exception> doIfError)
		{
			ILgKeymanHandler keymanHandler = LgKeymanHandlerClass.Create();
			try
			{
			var keyboards = new List<string>();

			try
			{
				// Update handler with any new/removed keyman keyboards
				keymanHandler.Init(true);
			}
			catch (Exception e)
			{
				if (doIfError != null)
					doIfError(e);
				return keyboards;
			}
			int clayout = keymanHandler.NLayout;

			for (int i = 0; i < clayout; ++i)
			{
				var item = keymanHandler.get_Name(i);
				// JohnT: haven't been able to reproduce FWR-1935, but apparently there's some bizarre
				// circumstance where one of the names comes back null. If so, leave it out.
				if (item != null)
					keyboards.Add(item);
			}
				return keyboards;
			}
			finally
			{
				keymanHandler.Close();
			Marshal.ReleaseComObject(keymanHandler);
		}
		}

		// Since InitLanguageCombo gets called from an OnGetFocus, and the message box causes a
		// change in focus, we need to avoid an endless loop of error messages.
		static bool errorMessage1Out;
		static bool errorMessage2Out;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the language combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitLanguageCombo()
		{
			CheckDisposed();

			ILgLanguageEnumerator lenum = LgLanguageEnumeratorClass.Create();
			m_langIdComboBox.Items.Clear(); // Clear out any old items.
			lenum.Init();
			int id = 0;
			string selectedName = null;
			int selectedId = m_ws.LCID;
			var badLocales = new List<int>();
			try
			{
				for (; ; )
				{
					string name;
					try
					{
						lenum.Next(out id, out name);
					}
					catch (OutOfMemoryException)
					{
						throw;
					}
					catch
					{ // if we fail to get a language, skip this one, but display once in error message.
						badLocales.Add(id);
						// Under certain conditions it can happen that lenum.Next() returns
						// E_UNEXPECTED right away. We're then stuck in an inifinite loop.
						if (badLocales.Count > 1000 || id == 0)
							break;
						continue;
					}
					if (id == 0)
						break;
					try
					{
						m_langIdComboBox.Items.Add(new LangIdComboItem(id, name));
						// The 'if' below should make a 'fr-CAN' language choose a french keyboard, if installed.
						if (id == selectedId)
							selectedName = name;
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
			}
			finally
			{
				// LT-8465 when Windows and Language Options changes are made lenum does not always get
				// updated correctly so we are ensuring the memory for this ComObject gets released.
				Marshal.FinalReleaseComObject(lenum);
			}

			if (badLocales.Count > 0 && errorMessage2Out == false)
			{
				errorMessage2Out = true;
				string strBadLocales = badLocales.Aggregate("", (current, loc) => current + (loc + ", "));
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
						m_langIdComboBox.Items.Add(new LangIdComboItem(selectedId, selectedName));
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
			m_ws.LCID = ((LangIdComboItem)m_langIdComboBox.SelectedItem).id;
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

	/// <summary></summary>
	public class LangIdComboItem
	{
		private readonly string m_itemName;
		private readonly int m_id;

		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		public LangIdComboItem(int id, string name)
		{
			m_itemName = name;
			m_id = id;
		}

		/// <summary>
		/// Human-readable name of the language.
		/// </summary>
		public string Name
		{
			get {return m_itemName;}
		}

		/// <summary>
		/// Langid, computationally identifies the language.
		/// </summary>
		public int id
		{
			get {return m_id;}
		}

		/// <summary>
		/// Display name to show in the combo.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return m_itemName;
		}
	}
}
