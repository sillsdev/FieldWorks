using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;
using System.Resources;
using System.Reflection; // to get Assembly for opening resource manager.
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// Summary description for RegionVariantControl.
	/// </summary>
	public class RegionVariantControl : UserControl, IFWDisposable
	{
		private System.Windows.Forms.TextBox m_RegionAbbrev;
		private System.Windows.Forms.Label lblVariantName;
		// Note: this currently has a max length set to 30. This is to ensure that any
		// combination of language (max 11), country (max 3) and variant with two
		// underscores will be under the 49-char limit for the overall length of a
		// locale ID. We even gave ourselves a margin of a couple of characters,
		// since 30 is a lot and it may be useful to have a couple left in case ICU
		// reduces the limit or we want to add a version number or something like that.
		private System.Windows.Forms.TextBox m_VariantAbbrev;
		private System.Windows.Forms.Label lblRegionAbbr;
		private System.Windows.Forms.Label lblVariantAbbrev;
		private System.Windows.Forms.Label lblRegionName;
		private FwOverrideComboBox m_RegionName;
		private FwOverrideComboBox m_VariantName;
		private FwOverrideComboBox m_ScriptName;
		private Label lblScriptName;
		private Label lblScriptAbbreviation;
		private TextBox m_ScriptAbbrev;
		private SIL.FieldWorks.Common.FwUtils.LanguageDefinition m_langDef;
//		private int m_wsUi; // Writing system for UI.
//		private string m_localeUi; // Name of locale corresponding to m_wsUser
		private ResourceManager m_res =
			new System.Resources.ResourceManager("SIL.FieldWorks.FwCoreDlgControls.FwCoreDlgControls",
			Assembly.GetExecutingAssembly());
		// Used in Writing System Properties dlg as opposed to New Writing System dialog.
		private bool m_PropDlg;
		private System.Windows.Forms.HelpProvider helpProvider1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// We need an event to let the parents control know when comboBox values have changed
		/// </summary>
		public event EventHandler OnRegionVariantNameChanged;


		/// <summary>
		/// Constructor.
		/// </summary>
		public RegionVariantControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			m_PropDlg = false;
			this.m_RegionName.Name = "m_RegionName";
			this.m_RegionName.Sorted = true;
			this.m_VariantName.Name = "m_VariantName";
			this.m_VariantName.Sorted = true;
			this.m_VariantName.Text = "";
			this.m_ScriptName.Name = "m_ScriptName";
			this.m_ScriptName.Sorted = true;
			this.m_ScriptAbbrev.Text = "";
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
				LoadControlsFromLangDef();
			}
		}

		/// <summary>
		/// The larger component using this control must supply a LanguageDefinition
		/// which this control will help to edit.
		/// </summary>
		public bool PropDlg
		{
			get
			{
				CheckDisposed();
				return m_PropDlg;
			}
			set
			{
				CheckDisposed();
				m_PropDlg = value;
			}
		}

		/// <summary>
		/// Check that the contents of the control are valid. If not, report the error
		/// to the user and return false. This should prevent the user from closing the
		/// containing form using OK, but not from cancelling.
		/// </summary>
		public bool CheckValid()
		{
			CheckDisposed();

			string caption = m_res.GetString("kstidError");
			string name = m_RegionName.Text.Trim().ToLowerInvariant();
			string abbr = m_RegionAbbrev.Text.Trim().ToUpperInvariant();
			// Can't allow a country name without an abbreviation.
			if (name != "" && abbr == "")
			{
				MessageBox.Show(m_res.GetString("kstidMissingRgnAbbr"), caption);
				return false;
			}
			// We need to validate the country code is not being modified unintentionally by
			// ICU. For example, if we ask for KEN, ICU automatically changes this to KE
			// because it thinks we mean Kenya, and KEN/KE are both used for Kenya. So
			// we need to make this change so that InstallLanguage will work.
			string icuCountryAbbr;
			Icu.UErrorCode err;
			StringUtils.InitIcuDataDir();	// initialize ICU data dir
			Icu.GetCountryCode("_" + abbr, out icuCountryAbbr, out err);
			string icuCountryName = "";
			// See if this abbreviation (or name) has already been used somewhere in the combo.
			bool fDefinedInIcu = false;
			foreach (NameAbbrComboItem item in m_RegionName.Items)
			{
				if (item.Abbr == icuCountryAbbr)
				{
					fDefinedInIcu = true;
					icuCountryName = item.Name;
					break;
				}
			}
			// But if we are trying to change the country name for a given abbreviation, it's OK as
			// long as the country name is one we defined.
			bool fIcuSwitchesToIso2 = abbr != icuCountryAbbr.ToUpperInvariant();
			bool fIcuCountryNameMatchesEditBox = icuCountryName.ToLowerInvariant() == name;
			if (fDefinedInIcu && (fIcuSwitchesToIso2 || !fIcuCountryNameMatchesEditBox))
			{
				bool fError = true;
				if (m_PropDlg)
				{
					if (!fIcuSwitchesToIso2)
					{
						// Need to check that the country name is not a custom name. If so,
						// it's fine to modify it.
						ILgIcuResourceBundle rbCustom = m_langDef.RootRb.get_GetSubsection("Custom");
						if (rbCustom != null)
						{
							ILgIcuResourceBundle rbCountriesAdded = rbCustom.get_GetSubsection("CountriesAdded");
							System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCustom);
							if (rbCountriesAdded != null)
							{
								while (fError && rbCountriesAdded.HasNext)
								{
									ILgIcuResourceBundle rbItem = rbCountriesAdded.Next;
									if (abbr == rbItem.Key)
									{
										// We have a custom abbreviation, but it's not clear whether
										// the name currently displayed is really the one being
										// changed, or whether the user is creating a new country
										// and accidentally tried to assign it to this abbreviation.
										string msg = string.Format(m_res.GetString("kstidRegAbbrQuestion"),
											abbr, icuCountryName, m_RegionName.Text.Trim());
										DialogResult res = MessageBox.Show(msg, caption,
											MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
											MessageBoxDefaultButton.Button2);
										//System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
										//System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCountriesAdded);
										//return DialogResult.OK == res;
										fError = false;
									}
									System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
								}
								System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCountriesAdded);
							}
						}
					}
				}
				if (fError)
				{
					string msg = string.Format(m_res.GetString("kstidRegAbbrReserved"),
						abbr, icuCountryName);
					MessageBox.Show(msg, caption);
					return false;
				}
			}
			// See if this name has already been used for a different abbreviation.
			bool fNameDefinedInIcu = false;
			foreach (NameAbbrComboItem item in m_RegionName.Items)
			{
				if (item.Name.ToLowerInvariant() == name && item.Abbr != icuCountryAbbr)
				{
					fNameDefinedInIcu = true;
					icuCountryName = item.Name;
					icuCountryAbbr = item.Abbr;
					break;
				}
			}
			if (fNameDefinedInIcu)
			{
				string msg = string.Format(m_res.GetString("kstidRegNameUsed"),
					icuCountryName, icuCountryAbbr);
				MessageBox.Show(msg, caption);
				return false;
			}

			// Can't allow a variant name without an abbreviation.
			name = m_VariantName.Text.Trim().ToLowerInvariant();
			abbr = m_VariantAbbrev.Text.Trim().ToUpperInvariant();
			if (name != "" && abbr == "")
			{
				MessageBox.Show(m_res.GetString("kstidMissingVarAbbr"), caption);
				return false;
			}

			// See if this abbreviation has already been used somewhere in the combo.
			fDefinedInIcu = false;
			string icuVariantName = "";
			foreach (NameAbbrComboItem item in m_VariantName.Items)
			{
				if (item.Abbr == abbr)
				{
					fDefinedInIcu = true;
					icuVariantName = item.Name;
					break;
				}
			}

			bool fIcuVariantNameMatchesEditBox = icuVariantName.ToLowerInvariant() == name;
			if (fDefinedInIcu && !fIcuVariantNameMatchesEditBox)
			{
				bool fError = true;
				if (m_PropDlg)
				{
					ILgIcuResourceBundle rbCustom = m_langDef.RootRb.get_GetSubsection("Custom");
					if (rbCustom != null)
					{
						// Need to check that the variant name is not a custom name. If so,
						// it's fine to modify it. But we can never modify it in the new
						// writing system wizard.
						ILgIcuResourceBundle rbVariantsAdded = rbCustom.get_GetSubsection("VariantsAdded");
						System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCustom);
						if (rbVariantsAdded != null)
						{
							while (fError && rbVariantsAdded.HasNext)
							{
								ILgIcuResourceBundle rbItem = rbVariantsAdded.Next;
								if (abbr == rbItem.Key)
								{
									// We have a custom abbreviation, but it's not clear whether
									// the name currently displayed is really the one being
									// changed, or whether the user is creating a new variant
									// and accidentally tried to assign it to this abbreviation.
									string msg = string.Format(m_res.GetString("kstidVarAbbrQuestion"),
										abbr, icuVariantName, m_VariantName.Text.Trim());
									DialogResult res = MessageBox.Show(msg, caption,
										MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
										MessageBoxDefaultButton.Button2);
									//System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
									//System.Runtime.InteropServices.Marshal.ReleaseComObject(rbVariantsAdded);
									//return DialogResult.OK == res;
									fError = false;
								}
								System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
							}
							System.Runtime.InteropServices.Marshal.ReleaseComObject(rbVariantsAdded);
						}
					}
				}
				if (fError)
				{
					string msg = string.Format(m_res.GetString("kstidVarAbbrReserved"),
						abbr, icuVariantName);
					MessageBox.Show(msg, caption);
					return false;
				}
			}
			// See if this name has already been used for a different abbreviation.
			fNameDefinedInIcu = false;
			string icuVariantAbbr = "";
			foreach (NameAbbrComboItem item in m_VariantName.Items)
			{
				if (item.Name.ToLowerInvariant() == name && item.Abbr != abbr)
				{
					fNameDefinedInIcu = true;
					icuVariantName = item.Name;
					icuVariantAbbr = item.Abbr;
					break;
				}
			}
			// This restriction was removed for 6.0 because numerous users may have had
			// phonetic and phonemic defined with an older abbreviation. Since we now have
			// a X_ETIC / X_EMIC default, this test would make it impossible to do anything
			// with these writing systems because there will be two different phonetic
			// items in the list. The slight problems observed with having duplicates in the
			// list are far less than the being blocked from making changes. See LT-10109.
			//if (fNameDefinedInIcu)
			//{
			//    string msg = string.Format(m_res.GetString("kstidVarNameUsed"),
			//        icuVariantName, icuVariantAbbr);
			//    MessageBox.Show(msg, caption);
			//    return false;
			//}

			// We don't have to do anything special to add new regions and variants to the
			// resource bundle. The code that serializes and persists the language definition
			// handles that.
			return true; // If no problems all is well.
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegionVariantControl));
			this.m_RegionAbbrev = new System.Windows.Forms.TextBox();
			this.lblVariantName = new System.Windows.Forms.Label();
			this.m_VariantAbbrev = new System.Windows.Forms.TextBox();
			this.lblRegionAbbr = new System.Windows.Forms.Label();
			this.lblVariantAbbrev = new System.Windows.Forms.Label();
			this.lblRegionName = new System.Windows.Forms.Label();
			this.m_RegionName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_VariantName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.lblScriptName = new System.Windows.Forms.Label();
			this.lblScriptAbbreviation = new System.Windows.Forms.Label();
			this.m_ScriptAbbrev = new System.Windows.Forms.TextBox();
			this.m_ScriptName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.SuspendLayout();
			//
			// m_RegionAbbrev
			//
			this.helpProvider1.SetHelpString(this.m_RegionAbbrev, resources.GetString("m_RegionAbbrev.HelpString"));
			resources.ApplyResources(this.m_RegionAbbrev, "m_RegionAbbrev");
			this.m_RegionAbbrev.Name = "m_RegionAbbrev";
			this.helpProvider1.SetShowHelp(this.m_RegionAbbrev, ((bool)(resources.GetObject("m_RegionAbbrev.ShowHelp"))));
			this.m_RegionAbbrev.TextChanged += new System.EventHandler(this.m_RegionAbbrev_TextChanged);
			this.m_RegionAbbrev.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_RegionAbbrev_KeyPress);
			//
			// lblVariantName
			//
			resources.ApplyResources(this.lblVariantName, "lblVariantName");
			this.lblVariantName.BackColor = System.Drawing.Color.Transparent;
			this.lblVariantName.Name = "lblVariantName";
			this.helpProvider1.SetShowHelp(this.lblVariantName, ((bool)(resources.GetObject("lblVariantName.ShowHelp"))));
			//
			// m_VariantAbbrev
			//
			this.helpProvider1.SetHelpString(this.m_VariantAbbrev, resources.GetString("m_VariantAbbrev.HelpString"));
			resources.ApplyResources(this.m_VariantAbbrev, "m_VariantAbbrev");
			this.m_VariantAbbrev.Name = "m_VariantAbbrev";
			this.helpProvider1.SetShowHelp(this.m_VariantAbbrev, ((bool)(resources.GetObject("m_VariantAbbrev.ShowHelp"))));
			this.m_VariantAbbrev.TextChanged += new System.EventHandler(this.m_VariantAbbrev_TextChanged);
			this.m_VariantAbbrev.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_VariantAbbrev_KeyPress);
			//
			// lblRegionAbbr
			//
			resources.ApplyResources(this.lblRegionAbbr, "lblRegionAbbr");
			this.lblRegionAbbr.BackColor = System.Drawing.Color.Transparent;
			this.lblRegionAbbr.Name = "lblRegionAbbr";
			this.helpProvider1.SetShowHelp(this.lblRegionAbbr, ((bool)(resources.GetObject("lblRegionAbbr.ShowHelp"))));
			//
			// lblVariantAbbrev
			//
			resources.ApplyResources(this.lblVariantAbbrev, "lblVariantAbbrev");
			this.lblVariantAbbrev.BackColor = System.Drawing.Color.Transparent;
			this.lblVariantAbbrev.Name = "lblVariantAbbrev";
			this.helpProvider1.SetShowHelp(this.lblVariantAbbrev, ((bool)(resources.GetObject("lblVariantAbbrev.ShowHelp"))));
			//
			// lblRegionName
			//
			resources.ApplyResources(this.lblRegionName, "lblRegionName");
			this.lblRegionName.BackColor = System.Drawing.Color.Transparent;
			this.lblRegionName.Name = "lblRegionName";
			this.helpProvider1.SetShowHelp(this.lblRegionName, ((bool)(resources.GetObject("lblRegionName.ShowHelp"))));
			//
			// m_RegionName
			//
			this.m_RegionName.AllowSpaceInEditBox = false;
			this.helpProvider1.SetHelpString(this.m_RegionName, resources.GetString("m_RegionName.HelpString"));
			resources.ApplyResources(this.m_RegionName, "m_RegionName");
			this.m_RegionName.Name = "m_RegionName";
			this.helpProvider1.SetShowHelp(this.m_RegionName, ((bool)(resources.GetObject("m_RegionName.ShowHelp"))));
			this.m_RegionName.SelectedIndexChanged += new System.EventHandler(this.m_RegionName_SelectedIndexChanged);
			this.m_RegionName.Leave += new System.EventHandler(this.m_RegionName_Leave);
			this.m_RegionName.TextChanged += new System.EventHandler(this.m_RegionName_TextChanged);
			//
			// m_VariantName
			//
			this.m_VariantName.AllowSpaceInEditBox = false;
			this.helpProvider1.SetHelpString(this.m_VariantName, resources.GetString("m_VariantName.HelpString"));
			resources.ApplyResources(this.m_VariantName, "m_VariantName");
			this.m_VariantName.Name = "m_VariantName";
			this.helpProvider1.SetShowHelp(this.m_VariantName, ((bool)(resources.GetObject("m_VariantName.ShowHelp"))));
			this.m_VariantName.SelectedIndexChanged += new System.EventHandler(this.m_VariantName_SelectedIndexChanged);
			this.m_VariantName.Leave += new System.EventHandler(this.m_VariantName_Leave);
			this.m_VariantName.TextChanged += new System.EventHandler(this.m_VariantName_TextChanged);
			//
			// lblScriptName
			//
			resources.ApplyResources(this.lblScriptName, "lblScriptName");
			this.lblScriptName.Name = "lblScriptName";
			//
			// lblScriptAbbreviation
			//
			resources.ApplyResources(this.lblScriptAbbreviation, "lblScriptAbbreviation");
			this.lblScriptAbbreviation.Name = "lblScriptAbbreviation";
			//
			// m_ScriptAbbrev
			//
			resources.ApplyResources(this.m_ScriptAbbrev, "m_ScriptAbbrev");
			this.m_ScriptAbbrev.Name = "m_ScriptAbbrev";
			this.m_ScriptAbbrev.TextChanged += new System.EventHandler(this.m_ScriptAbbrev_TextChanged);
			this.m_ScriptAbbrev.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_ScriptAbbrev_KeyPress);
			//
			// m_ScriptName
			//
			this.m_ScriptName.AllowSpaceInEditBox = false;
			this.m_ScriptName.FormattingEnabled = true;
			resources.ApplyResources(this.m_ScriptName, "m_ScriptName");
			this.m_ScriptName.Name = "m_ScriptName";
			this.m_ScriptName.SelectedIndexChanged += new System.EventHandler(this.m_ScriptName_SelectedIndexChanged);
			this.m_ScriptName.Leave += new System.EventHandler(this.m_ScriptName_Leave);
			this.m_ScriptName.TextChanged += new System.EventHandler(this.m_ScriptName_TextChanged);
			//
			// RegionVariantControl
			//
			this.BackColor = System.Drawing.SystemColors.Control;
			this.Controls.Add(this.m_ScriptName);
			this.Controls.Add(this.m_ScriptAbbrev);
			this.Controls.Add(this.lblScriptAbbreviation);
			this.Controls.Add(this.lblScriptName);
			this.Controls.Add(this.m_VariantName);
			this.Controls.Add(this.m_RegionName);
			this.Controls.Add(this.m_RegionAbbrev);
			this.Controls.Add(this.lblVariantName);
			this.Controls.Add(this.m_VariantAbbrev);
			this.Controls.Add(this.lblRegionAbbr);
			this.Controls.Add(this.lblVariantAbbrev);
			this.Controls.Add(this.lblRegionName);
			this.Name = "RegionVariantControl";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.Leave += new System.EventHandler(this.RegionVariantControl_Leave);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// Returns <c>true</c> if a region or variant or script name changes without the corresponding
		/// abbreviation changing.
		/// </summary>
		public bool DisplayNameChanged
		{
			get
			{
				CheckDisposed();

				if (m_RegionAbbrev.Text == m_sRegionAbbrOrig &&
					m_RegionName.Text != m_sRegionNameOrig)
				{
					return true;
				}
				else if (m_VariantAbbrev.Text == m_sVariantAbbrOrig &&
					m_VariantName.Text != m_sVariantNameOrig)
				{
					return true;
				}
				else if (m_ScriptAbbrev.Text == m_sScriptAbbrOrig &&
					m_ScriptName.Text != m_sScriptNameOrig)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Indicates whether the Region or Variant or Script name has changed in the control since initialization.
		/// </summary>
		public bool RegionOrVariantOrScriptChanged
		{
			get
			{
				if (m_RegionName.Text != m_sRegionNameOrig ||
					m_VariantName.Text != m_sVariantNameOrig ||
					m_ScriptName.Text != m_sScriptNameOrig)
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// This is also called when the selection changes. If the user types the exact name
		/// of an existing item, we may get both notifications, and the order we get them
		/// is not certain (In fact, it appears that it is somewhat unpredictable(!) whether
		/// the index changed happens at all!).
		/// However, whatever the order, we make the behavior depend only on whether what's
		/// in the text matches one of the items.
		/// We do this continuously, not just when the user leaves the control, because
		/// the natural place to go when leaving is the abbreviation, but that might be
		/// disabled when the user starts editing this box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_RegionName_TextChanged(object sender, EventArgs e)
		{
			string rgnTrim = m_RegionName.Text.Trim();
			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			m_langDef.LocaleCountry = m_RegionName.Text;
			int selIndex = m_RegionName.FindStringExact(rgnTrim);
			if (selIndex >= 0)
			{
				m_RegionAbbrev.Text = ((NameAbbrComboItem)(m_RegionName.Items[selIndex])).Abbr;
				EnableCustomAbbrev(m_RegionAbbrev, "CountriesAdded");
			}
			else
			{
				m_RegionAbbrev.Text = MakeAsciiAbbrUppercase(rgnTrim, 3, false);
				m_RegionAbbrev.Enabled = rgnTrim.Length > 0;
			}
			if (OnRegionVariantNameChanged != null)
			{
				OnRegionVariantNameChanged(this, null);
			}
		}

		private void EnableCustomAbbrev(TextBox abbrBox, string subsectionName)
		{
			abbrBox.Enabled = false;
			ILgIcuResourceBundle rbCustom = m_langDef.RootRb.get_GetSubsection("Custom");
			if (rbCustom == null)
				return; // No Custom locales.
			ILgIcuResourceBundle rbSubsection = rbCustom.get_GetSubsection(subsectionName);
			System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCustom);
			if (rbSubsection == null)
				return; // If there is a Custom resource, this should also be there, but...
			while (rbSubsection.HasNext)
			{
				ILgIcuResourceBundle rbItem = rbSubsection.Next;
				if (abbrBox.Text == rbItem.Key)
				{
					// This is a custom name/abbreviation, so it's ok to edit it here.
					abbrBox.Enabled = true;
					System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
					System.Runtime.InteropServices.Marshal.ReleaseComObject(rbSubsection);
					return;
				}
				System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
			}
			System.Runtime.InteropServices.Marshal.ReleaseComObject(rbSubsection);
		}

		/// <summary>
		/// If the selection changes, update the abbr even though the user hasn't left the
		/// control.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_RegionName_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_RegionName_TextChanged(sender, e);
		}
		/// <summary>
		/// This is also called when the selection changes. If the user types the exact name
		/// of an existing item, we may get both notifications, and the order we get them
		/// is not certain. However, we expect that whatever the order, the last notification
		/// will have a correct SelectedIndex and so should produce the right effects.
		/// We do this continuously, not just when the user leaves the control, because
		/// the natural place to go when leaving is the abbreviation, but that might be
		/// disabled when the user starts editing this box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_VariantName_TextChanged(object sender, EventArgs e)
		{
			string varTrim = m_VariantName.Text.Trim();
			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			m_langDef.LocaleVariant = m_VariantName.Text;
			int idx = m_VariantName.FindStringExact(varTrim);
			if (idx >= 0)
			{
				m_VariantAbbrev.Text = ((NameAbbrComboItem)(m_VariantName.Items[idx])).Abbr;
				EnableCustomAbbrev(m_VariantAbbrev, "VariantsAdded");
			}
			else
			{
				m_VariantAbbrev.Text = MakeAsciiAbbrUppercase(varTrim, 30, true);
				m_VariantAbbrev.Enabled = varTrim.Length > 0;
			}
			if (OnRegionVariantNameChanged != null)
			{
				OnRegionVariantNameChanged(this, null);
			}
		}

		/// <summary>
		/// This is also called when the selection changes. If the user types the exact name
		/// of an existing item, we may get both notifications, and the order we get them
		/// is not certain (In fact, it appears that it is somewhat unpredictable(!) whether
		/// the index changed happens at all!).
		/// However, whatever the order, we make the behavior depend only on whether what's
		/// in the text matches one of the items.
		/// We do this continuously, not just when the user leaves the control, because
		/// the natural place to go when leaving is the abbreviation, but that might be
		/// disabled when the user starts editing this box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_ScriptName_TextChanged(object sender, EventArgs e)
		{
			//NOTE:  ScriptName is required to be exactly 4 characters long. Make sure
			//this method enforces that.

			string scrTrim = m_ScriptName.Text.Trim();
			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			int selIndex = m_ScriptName.FindStringExact(scrTrim);
			m_langDef.LocaleScript = m_ScriptName.Text;

			if (selIndex >= 0)
			{
				m_ScriptAbbrev.Text = ((NameAbbrComboItem)(m_ScriptName.Items[selIndex])).Abbr;
				EnableCustomAbbrev(m_ScriptAbbrev, "ScriptsAdded");
			}
			else
			{
				// Ensure only alphanumeric chars are passed to the abbreviation.  See LT-10360.
				string sAbbr = MakeAsciiAbbrUppercase(scrTrim, 4, false);
				m_ScriptAbbrev.Text = LanguageDefinition.FormatScriptAbbr(sAbbr);
				m_ScriptAbbrev.Enabled = scrTrim.Length > 0;
			}
			if (OnRegionVariantNameChanged != null)
			{
				OnRegionVariantNameChanged(this, null);
			}

		}

		/// <summary>
		/// Take the input name and convert it into an uppercase ASCII alphanumeric
		/// abbreviation with a maximum number of characters.
		/// </summary>
		/// <param name="name">Starting string</param>
		/// <param name="cchMax">Maximum number of characters in abbreviation</param>
		/// <param name="fUnderlineOk">Can underlines be copied?</param>
		private string MakeAsciiAbbrUppercase(string name, int cchMax, bool fUnderlineOk)
		{
			string abbr = "";
			int ichAbbr = 0;
			for (int ich = 0; ich < name.Length; ++ich)
			{
				char ch = char.ToUpperInvariant(name[ich]);
				if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') ||
					(fUnderlineOk && ch == '_'))
				{
					abbr += ch;
					if (++ichAbbr >= cchMax)
						break;
				}
			}
			return abbr;
		}

		/// <summary>
		/// If the selection changes, update the abbr even though the user hasn't left the
		/// control.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_VariantName_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			m_VariantName_TextChanged(sender, e);
		}

		private void m_ScriptName_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_ScriptName_TextChanged(sender, e);
		}

		/// <summary>
		/// resourceSectionName's are top-level items in the resource bundle whose keys start with %%,
		/// except that there is a special top-level item called %%UCARULES that serves
		/// a quite different purpose and must be omitted.
		/// We make a new resource bundle rather than using the one already in m_langDef
		/// because get_Next changes its state, and we don't want to do that to the
		/// shared one.
		/// </summary>
		/// <param name="cb">ComboBox</param>
		/// <param name="resourceSectionName"></param>
		private void PopulateCombo(ComboBox cb, String resourceSectionName)
		{
			if (m_langDef == null)
				return; // Probably in design mode; can't populate.
			cb.Items.Clear(); // Clear out any previous items.
			try
			{
				ILgIcuResourceBundle resBundle = m_langDef.RootRb.get_GetSubsection(resourceSectionName);
				if (resBundle == null)
					return; // Missing Section ??
				while (resBundle.HasNext)
				{
					ILgIcuResourceBundle rbItem = resBundle.Next;
					cb.Items.Add(new NameAbbrComboItem(rbItem.String, rbItem.Key));
					System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
				}
				System.Runtime.InteropServices.Marshal.ReleaseComObject(resBundle);
			}
			finally
			{
				m_langDef.ReleaseRootRb();
			}
		}

		/// <summary>
		/// Given a combo box whose items are (name, abbr) pairs, find an item
		/// whose abbr matches, and select it. If there is no match, leave the
		/// combo blank.
		/// </summary>
		/// <param name="combo"></param>
		/// <param name="abbr"></param>
		/// <returns></returns>
		private void SelectNameFromAbbr(ComboBox combo, string abbr)
		{
			if (abbr == "")
			{
				combo.Text = "";
				return;
			}
			combo.SelectedIndex = IndexOfAbbr(combo, abbr);
			// Review: What if not found? assert? Or what?
			// The current code will just make the name empty.
		}

		private int IndexOfAbbr(ComboBox combo, string abbr)
		{
			for (int i = 0; i < combo.Items.Count; ++i)
			{
				NameAbbrComboItem item = (NameAbbrComboItem)(combo.Items[i]);
				// Exact equality should be OK...all abbrs are required to be upper case.
				if (item.Abbr == abbr)
				{
					return i;
				}
			}
			return -1;
		}


		/// <summary>
		/// Gets the IcuLocale of this control based on the Language Name
		/// and the Variant, Region and Script abbreviations. It returns an
		/// empty string if LangDef is null.
		/// </summary>
		public String ConstructIcuLocaleFromAbbreviations()
		{
				if (LangDef == null)
					return "";
				StringBuilder strBldr = new StringBuilder(m_langDef.LocaleAbbr);

				//now add the ScriptCode if it exists
				if (!String.IsNullOrEmpty(m_ScriptAbbrev.Text))
					strBldr.AppendFormat("_{0}", m_ScriptAbbrev.Text);

				//now add the CountryCode if it exists
				if (!String.IsNullOrEmpty(m_RegionAbbrev.Text))
					strBldr.AppendFormat("_{0}", m_RegionAbbrev.Text);

				// if variantCode is notNullofEmpty then add it
				// and if CountryCode is empty add two underscores instead of one.
				if (!String.IsNullOrEmpty(m_VariantAbbrev.Text))
				{
					if (String.IsNullOrEmpty(m_RegionAbbrev.Text))
						strBldr.AppendFormat("__{0}", m_VariantAbbrev.Text);
					else
						strBldr.AppendFormat("_{0}", m_VariantAbbrev.Text);
				}
				return strBldr.ToString();
		}

		/// <summary>
		/// The variant name. Just used for Tests
		/// </summary>
		public string VariantName
		{
			get { return m_VariantName.Text; }
			internal set { m_VariantName.Text = value; }
		}

		/// <summary>
		/// The variant name. Just used for Tests
		/// </summary>
		public string ScriptName
		{
			get { return m_ScriptName.Text; }
			internal set { m_ScriptName.Text = value; }
		}

		private string m_sRegionNameOrig;
		private string m_sRegionAbbrOrig;
		private string m_sVariantNameOrig;
		private string m_sVariantAbbrOrig;
		private string m_sScriptNameOrig;
		private string m_sScriptAbbrOrig;

		/// <summary>
		/// Load the controls from  m_LangDef, if it is not null. If it is null, clear all controls.
		/// If the combo boxes are not populated, do nothing...the method will get called again
		/// when the form loads.
		/// </summary>
		private void LoadControlsFromLangDef()
		{
			if (m_langDef == null)
				return; // Probably in design mode; can't populate.
			// Set up the combo box items.
			PopulateCombo(m_RegionName, "Countries");
			PopulateCombo(m_VariantName, "Variants");
			PopulateCombo(m_ScriptName, "Scripts");

			if (m_RegionName.Items.Count == 0)
				return;
			if (m_langDef == null)
			{
				m_RegionAbbrev.Text = "";
				m_RegionName.Text = "";
				m_VariantAbbrev.Text = "";
				m_VariantName.Text = "";
				m_sRegionNameOrig = "";
				m_sRegionAbbrOrig = "";
				m_sVariantNameOrig = "";
				m_sVariantAbbrOrig = "";
				m_sScriptNameOrig = "";
				m_sScriptAbbrOrig = "";
				return;
			}
			m_sRegionAbbrOrig = m_langDef.CountryAbbr;
			m_sVariantAbbrOrig = m_langDef.VariantAbbr;
			m_sScriptAbbrOrig = m_langDef.ScriptAbbr;
			m_RegionAbbrev.Text = m_sRegionAbbrOrig;
			m_VariantAbbrev.Text = m_sVariantAbbrOrig;
			m_ScriptAbbrev.Text = m_sScriptAbbrOrig;
			SelectNameFromAbbr(m_RegionName, m_sRegionAbbrOrig);
			SelectNameFromAbbr(m_VariantName, m_sVariantAbbrOrig);
			SelectNameFromAbbr(m_ScriptName, m_sScriptAbbrOrig);
			m_sRegionNameOrig = m_RegionName.Text;
			m_sVariantNameOrig = m_VariantName.Text;
			m_sScriptNameOrig = m_ScriptName.Text;
			// Either it's a known item, or it's blank. In either case, the abbreviation
			// can't be edited until something changes.
			EnableCustomAbbrev(m_ScriptAbbrev, "ScriptsAdded");
			EnableCustomAbbrev(m_RegionAbbrev, "CountriesAdded");
			EnableCustomAbbrev(m_VariantAbbrev, "VariantsAdded");
			if (m_langDef.LocaleScript == null)
				m_langDef.LocaleScript = "";

			if (m_RegionName.Text != m_langDef.LocaleCountry)
				m_RegionName.Text = m_langDef.LocaleCountry;
			if (m_VariantName.Text != m_langDef.LocaleVariant)
				m_VariantName.Text = m_langDef.LocaleVariant;
			if (m_ScriptName.Text != m_langDef.LocaleScript)
				m_ScriptName.Text = m_langDef.LocaleScript;
			if (m_RegionAbbrev.Text != m_langDef.CountryAbbr)
				m_RegionAbbrev.Text = m_langDef.CountryAbbr;
			if (m_VariantAbbrev.Text != m_langDef.VariantAbbr)
				m_VariantAbbrev.Text = m_langDef.VariantAbbr;
			if (m_ScriptAbbrev.Text != m_langDef.ScriptAbbr)
				m_ScriptAbbrev.Text = m_langDef.ScriptAbbr;
		}

		/// <summary>
		/// Identify characters allowed in the abbreviation fields: upper case or numeric
		/// plus Underline. Strictly ASCII characters are used to name locales.
		/// Review: do we need to allow backspace, del, arrow keys?
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		private bool IsUpperAlphaNumericOrUnderline(char ch)
		{
			return (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_';
		}

		private bool isBackSpace(char c)
		{
			return (int) c == (int)Keys.Back;
		}

		private bool isReturn(char c)
		{
			return (int) c == (int)Keys.Return;
		}

		/// <summary>
		/// Suppress entering invalid characters. Note that, for incomprehensible reasons,
		/// Backspace and returns come through this validator, while Delete and arrow keys don't,
		/// so we have to allow Backspace/return explicitly but can ignore the others.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_RegionAbbrev_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		private void HandleKeyPress(KeyPressEventArgs e)
		{
			if (!isBackSpace(e.KeyChar) && !isReturn(e.KeyChar) &&
				!IsUpperAlphaNumericOrUnderline(Char.ToUpperInvariant(e.KeyChar)))
			{
				// Stop the character from being entered into the control since it is not valid.
				e.Handled = true;
				MiscUtils.ErrorBeep();
			}
		}

		/// <summary>
		/// Suppress entering invalid characters. Note that, for incomprehensible reasons,
		/// Backspace and returns come through this validator, while Delete and arrow keys don't,
		/// so we have to allow Backspace/return explicitly but can ignore the others.
		/// </summary>
		private void m_ScriptAbbrev_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		/// <summary>
		/// Suppress entering invalid characters.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_VariantAbbrev_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		/// <summary>
		/// Is set to true when handling TextChange in the Abbreviation Textboxes
		/// to prevent infinite looping when adjusting the text.
		/// </summary>
		private bool fInTextChanged = false;

		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_RegionAbbrev_TextChanged(object sender, EventArgs e)
		{
			if (fInTextChanged)
				return;
			AdjustToUpperInvariant(m_RegionAbbrev);
			if (OnRegionVariantNameChanged != null)
			{
				OnRegionVariantNameChanged(this, null);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_VariantAbbrev_TextChanged(object sender, EventArgs e)
		{
			if (fInTextChanged)
				return;
			AdjustToUpperInvariant(m_VariantAbbrev);
			if (OnRegionVariantNameChanged != null)
			{
				OnRegionVariantNameChanged(this, null);
			}
		}

		private void AdjustToUpperInvariant(TextBox tb)
		{
			try
			{
				fInTextChanged = true;
				//Adjusting needs to position the cursor at the end of the text. Therefore
				//Paste() is a way to do this.
				String tmp = tb.Text.Trim().ToUpperInvariant();
				tb.Clear();
				tb.Paste(tmp);
			}
			finally
			{
				fInTextChanged = false;
			}
		}

		private void m_ScriptAbbrev_TextChanged(object sender, EventArgs e)
		{
			if (fInTextChanged)
				return;
			try
			{
				fInTextChanged = true;
				//Adjusting needs to position the cursor at the end of the text. Therefore
				//Paste() is a way to do this.
				String tmp = LanguageDefinition.FormatScriptAbbr(m_ScriptAbbrev.Text.Trim());
				m_ScriptAbbrev.Clear();
				m_ScriptAbbrev.Paste(tmp);
			}
			finally
			{
				fInTextChanged = false;
			}

			if (OnRegionVariantNameChanged != null)
			{
				OnRegionVariantNameChanged(this, null);
			}
		}


		private void m_VariantName_Leave(object sender, System.EventArgs e)
		{
			m_VariantName.Text = m_langDef.LocaleVariant != null ?
				m_langDef.LocaleVariant.Trim() : "";
		}

		private void m_RegionName_Leave(object sender, System.EventArgs e)
		{
			m_RegionName.Text = m_langDef.LocaleCountry != null ?
				m_langDef.LocaleCountry.Trim() : "";
		}

		private void m_ScriptName_Leave(object sender, EventArgs e)
		{
			m_ScriptName.Text = m_langDef.LocaleScript != null ?
				m_langDef.LocaleScript.Trim() : "";
		}

		internal void RegionVariantControl_Leave(object sender, EventArgs e)
		{
			if ( (!String.IsNullOrEmpty(m_ScriptName.Text) && m_ScriptAbbrev.Text.Length != 4) ||
				(m_ScriptAbbrev.Text.Length > 0 && m_ScriptAbbrev.Text.Length != 4) )
			{
				MessageBox.Show(FwCoreDlgControls.ksScriptAbbreviationLength);
				m_ScriptAbbrev.Focus();
				return;
			}

			if (!String.IsNullOrEmpty(m_RegionName.Text) &&
				(m_RegionAbbrev.Text.Length < 2 || m_RegionAbbrev.Text.Length >3))
			{
				MessageBox.Show(FwCoreDlgControls.ksRegionAbbreviationLength);
				m_RegionAbbrev.Focus();
				return;
			}

			//Ensure any changes get copied to the language definition.
			m_ScriptAbbrev.Text = LanguageDefinition.FormatScriptAbbr(m_ScriptAbbrev.Text);
			m_langDef.ScriptAbbr = m_ScriptAbbrev.Text;

			//ICU requires CountryAbbr and VariantAbbr are uppercase.
			m_langDef.CountryAbbr = m_RegionAbbrev.Text.Trim().ToUpperInvariant();
			m_langDef.VariantAbbr = m_VariantAbbrev.Text.Trim().ToUpperInvariant();
		}

	}

	/// <summary>
	/// NameAbbrComboItem stores a name and abbreviation, with the name as its ToString.
	/// It is used as a combo item in menus where we need to retrieve both bits of information
	/// from an item.
	/// </summary>
	public class NameAbbrComboItem
	{
		string m_name;
		string m_abbr;

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="abbr"></param>
		public NameAbbrComboItem(string name, string abbr)
		{
			m_name = name;
			m_abbr = abbr;
		}

		/// <summary>
		///
		/// </summary>
		public string Name
		{
			get {return m_name;}
		}

		/// <summary>
		///
		/// </summary>
		public string Abbr
		{
			get {return m_abbr;}
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return m_name;
		}
	}
}
