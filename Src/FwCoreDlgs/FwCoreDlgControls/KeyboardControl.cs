using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Resources;
using System.Reflection; // to get Assembly for opening resource manager.

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for KeyboardControl.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyboardControl : UserControl, IFWDisposable
	{
		private FwOverrideComboBox m_cboKeyboard;
		private FwOverrideComboBox m_cbLangId;
		private bool m_fKeymanInitErrorReported = false;
		private SIL.FieldWorks.Common.FwUtils.LanguageDefinition m_langDef;
		private System.Windows.Forms.HelpProvider helpProvider1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyboardControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyboardControl()
		{
			// This call is required by the Windows.Forms Form Designer.
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

		/// <summary>
		/// The larger component using this control must supply a LanguageDefinition
		/// which this control will help to edit.
		/// </summary>
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeyboardControl));
			System.Windows.Forms.Label label2;
			this.m_cboKeyboard = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_cbLangId = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.BackColor = System.Drawing.Color.Transparent;
			label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.BackColor = System.Drawing.Color.Transparent;
			label2.Name = "label2";
			//
			// m_cboKeyboard
			//
			this.m_cboKeyboard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.m_cboKeyboard, resources.GetString("m_cboKeyboard.HelpString"));
			resources.ApplyResources(this.m_cboKeyboard, "m_cboKeyboard");
			this.m_cboKeyboard.Name = "m_cboKeyboard";
			this.helpProvider1.SetShowHelp(this.m_cboKeyboard, ((bool)(resources.GetObject("m_cboKeyboard.ShowHelp"))));
			this.m_cboKeyboard.Sorted = true;
			this.m_cboKeyboard.SelectedIndexChanged += new System.EventHandler(this.m_cboKeyboard_SelectedIndexChanged);
			//
			// m_cbLangId
			//
			this.m_cbLangId.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.m_cbLangId, resources.GetString("m_cbLangId.HelpString"));
			resources.ApplyResources(this.m_cbLangId, "m_cbLangId");
			this.m_cbLangId.Name = "m_cbLangId";
			this.helpProvider1.SetShowHelp(this.m_cbLangId, ((bool)(resources.GetObject("m_cbLangId.ShowHelp"))));
			this.m_cbLangId.Sorted = true;
			this.m_cbLangId.SelectedIndexChanged += new System.EventHandler(this.m_cbLangId_SelectedIndexChanged);
			//
			// KeyboardControl
			//
			this.Controls.Add(this.m_cboKeyboard);
			this.Controls.Add(this.m_cbLangId);
			this.Controls.Add(label2);
			this.Controls.Add(label1);
			this.Name = "KeyboardControl";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the keyman combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitKeymanCombo()
		{
			CheckDisposed();

			ILgKeymanHandler keymanHandler = LgKeymanHandlerClass.Create();
			m_cboKeyboard.Items.Clear(); // Clear out any old items from combobox list
			try
			{
				keymanHandler.Init(true); // Update handler with any new/removed keyman keyboards
			}
			catch (Exception e)
			{
				if (!m_fKeymanInitErrorReported)
				{
					m_fKeymanInitErrorReported = true;
					string caption = FwCoreDlgControls.kstidKeymanInitFailed;
					string message = e.Message;
					if (message == null || message == string.Empty)
						message = caption;
					MessageBox.Show(this.ParentForm, message, caption,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return;
			}
			int clayout = keymanHandler.NLayout;
			string strKbdName = m_langDef.WritingSystem.KeymanKbdName;
			if(strKbdName == null)
				strKbdName = FwCoreDlgControls.kstid_None;
			m_cboKeyboard.Items.Add(FwCoreDlgControls.kstid_None);
			for (int i = 0; i < clayout; ++i )
				m_cboKeyboard.Items.Add(keymanHandler.get_Name(i));
			m_cboKeyboard.SelectedItem = strKbdName;
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
		public void InitLanguageCombo()
		{
			CheckDisposed();

			ILgLanguageEnumerator lenum = LgLanguageEnumeratorClass.Create();
			m_cbLangId.Items.Clear(); // Clear out any old items.
			lenum.Init();
			int id = 0;
			string name;
			string selectedName = null;
			int selectedId = m_langDef.WritingSystem.Locale;
			ArrayList badLocales = new ArrayList();
			try
			{
				for (; ; )
				{
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
						continue;
					}
					if (id == 0)
						break;
					try
					{
						m_cbLangId.Items.Add(new LangIdComboItem(id, name));
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
							string message = FwCoreDlgControls.kstidBadLanguageName;
							MessageBox.Show(this.ParentForm, FwCoreDlgControls.kstidBadLanguageName,
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
				System.Runtime.InteropServices.Marshal.FinalReleaseComObject(lenum);
			}

			if (badLocales.Count > 0 && errorMessage2Out == false)
			{
				errorMessage2Out = true;
				string strBadLocales = "";
				foreach (int loc in badLocales)
				{
					strBadLocales += loc + ", ";
				}
				strBadLocales = strBadLocales.Substring(0, strBadLocales.Length - 2);
				string caption = FwCoreDlgControls.kstidError;
				MessageBox.Show(this.ParentForm, String.Format(FwCoreDlgControls.kstidBadLocales,
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
						m_cbLangId.Items.Add(new LangIdComboItem(selectedId, selectedName));
					}
				}
			}
			int idx = m_cbLangId.FindStringExact(selectedName, -1);
			m_cbLangId.SelectedIndex = idx;
		}

		private void m_cbLangId_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (m_cbLangId.SelectedItem == null)
				return;
			m_langDef.WritingSystem.Locale = ((LangIdComboItem)m_cbLangId.SelectedItem).id;
		}

		private void m_cboKeyboard_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (m_cboKeyboard.SelectedIndex >= 0)
			{
				string str = m_cboKeyboard.Text;
				if (str == FwCoreDlgControls.kstid_None)
					str = null;
				m_langDef.WritingSystem.KeymanKbdName = str;
			}
		}
	}

	/// <summary>
	/// Summary description for LangIdComboClass.
	/// </summary>
	public class LangIdComboItem
	{
		string m_itemName;
		int m_id;

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
