using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// Summary description for RegionVariantControl.
	/// </summary>
	public class RegionVariantControl : UserControl, IFWDisposable
	{
		private TextBox m_regionCode;
		private Label m_variantNameLabel;
		// Note: this currently has a max length set to 30. This is to ensure that any
		// combination of language (max 11), country (max 3) and variant with two
		// underscores will be under the 49-char limit for the overall length of a
		// locale ID. We even gave ourselves a margin of a couple of characters,
		// since 30 is a lot and it may be useful to have a couple left in case ICU
		// reduces the limit or we want to add a version number or something like that.
		private TextBox m_variantCode;
		private Label m_regionCodeLabel;
		private Label m_variantCodeLabel;
		private Label m_regionNameLabel;
		private FwOverrideComboBox m_regionName;
		private FwOverrideComboBox m_variantName;
		private FwOverrideComboBox m_scriptName;
		private Label m_scriptNameLabel;
		private Label m_scriptCodeLabel;
		private TextBox m_scriptCode;
		private HelpProvider m_helpProvider;

		private IWritingSystem m_ws;
		private ScriptSubtag m_origScriptSubtag;
		private RegionSubtag m_origRegionSubtag;
		private VariantSubtag m_origVariantSubtag;

		/// <summary>
		/// We need an event to let the parents control know when comboBox values have changed
		/// </summary>
		public event EventHandler ScriptRegionVariantChanged;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RegionVariantControl()
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
				LoadControlsFromWritingSystem();
			}
		}

		/// <summary>
		/// Indicates whether the Region or Variant or Script name has changed in the control since initialization.
		/// </summary>
		public bool RegionOrVariantOrScriptChanged
		{
			get
			{
				if (m_ws.RegionSubtag != m_origRegionSubtag ||
					m_ws.VariantSubtag != m_origVariantSubtag ||
					m_ws.ScriptSubtag != m_origScriptSubtag)
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets or sets the script subtag.
		/// </summary>
		/// <value>The script subtag.</value>
		public ScriptSubtag ScriptSubtag
		{
			get
			{
				CheckDisposed();

				ScriptSubtag subtag = null;
				if (m_scriptCode.Enabled)
				{
					string code = m_scriptCode.Text.Trim();
					if (!string.IsNullOrEmpty(code))
						subtag = new ScriptSubtag(code, m_scriptName.Text.Trim(), true);
				}
				else
				{
					subtag = (ScriptSubtag) m_scriptName.SelectedItem;
				}
				return subtag;
			}

			set
			{
				CheckDisposed();

				if (value == null)
				{
					m_scriptCode.Text = "";
					m_scriptCode.Enabled = false;
					m_scriptName.Text = "";
				}
				else
				{
					m_scriptName.SelectedItem = value;
					if (m_scriptName.SelectedItem == null)
					{
						m_scriptName.Items.Add(value);
						m_scriptName.SelectedItem = value;
					}
					m_scriptCode.Enabled = value.IsPrivateUse;
					m_scriptCode.Text = value.Code;
				}
			}
		}

		/// <summary>
		/// Gets or sets the region subtag.
		/// </summary>
		/// <value>The region subtag.</value>
		public RegionSubtag RegionSubtag
		{
			get
			{
				CheckDisposed();

				RegionSubtag subtag = null;
				if (m_regionCode.Enabled)
				{
					string code = m_regionCode.Text.Trim();
					if (!string.IsNullOrEmpty(code))
						subtag = new RegionSubtag(code, m_regionName.Text.Trim(), true);
				}
				else
				{
					subtag = (RegionSubtag) m_regionName.SelectedItem;
				}
				return subtag;
			}

			set
			{
				CheckDisposed();

				if (value == null)
				{
					m_regionCode.Text = "";
					m_regionCode.Enabled = false;
					m_regionName.Text = "";
				}
				else
				{
					m_regionName.SelectedItem = value;
					if (m_regionName.SelectedItem == null)
					{
						m_regionName.Items.Add(value);
						m_regionName.SelectedItem = value;
					}
					m_regionCode.Enabled = value.IsPrivateUse;
					m_regionCode.Text = value.Code;
				}
			}
		}

		/// <summary>
		/// Gets or sets the variant subtag.
		/// </summary>
		/// <value>The variant subtag.</value>
		public VariantSubtag VariantSubtag
		{
			get
			{
				CheckDisposed();

				VariantSubtag subtag = null;
				if (m_variantCode.Enabled)
				{
					string code = m_variantCode.Text.Trim();
					if (!string.IsNullOrEmpty(code))
						subtag = new VariantSubtag(code, m_variantName.Text.Trim(), true, null);
				}
				else
				{
					subtag = (VariantSubtag) m_variantName.SelectedItem;
				}
				return subtag;
			}

			set
			{
				CheckDisposed();

				if (value == null)
				{
					m_variantName.Text = "";
					m_variantCode.Enabled = false;
					m_variantCode.Text = "";
				}
				else
				{
					m_variantName.SelectedItem = value;
					// There are initially items in our combo that are VariantSubtags with codes starting with x-,
					// in particular x-py and x-pyn. However when these are used in actual writing systems the
					// writing system library figures out that the x- is just a private-use marker and does not make
					// it part of the code. So the incoming VariantSubtag has a code of just py or pyn.
					// To take advantage of the one already in the menu and see its name, and avoid adding a duplicate,
					// we have to search for the existing one starting with "x-".
					if (m_variantName.SelectedItem == null && !value.Code.StartsWith("x-"))
					{
						// Resharper thinks this code is redundant because it thinks m_variantName.SelectedItem == null
						// can't be true since we just checked that value is not null and set SelectedItem to value.
						// However, setting SelectedItem on a combo box has no effect if the desired item is not in the list.
						var altTag = new VariantSubtag("x-" + value.Code, value.Name, value.IsPrivateUse, value.Prefixes);
						m_variantName.SelectedItem = altTag;
					}
					if (m_variantName.SelectedItem == null)
					{
						// See Reshaper comment above.
						m_variantName.Items.Add(value);
						m_variantName.SelectedItem = value;
					}
					m_variantCode.Enabled = value.IsPrivateUse;
					m_variantCode.Text = value.Code;
				}
			}
		}

		/// <summary>
		/// Gets or sets the name of the script.
		/// </summary>
		/// <value>The name of the script.</value>
		public string ScriptName
		{
			get
			{
				CheckDisposed();
				return m_scriptName.Text;
			}

			set
			{
				CheckDisposed();
				m_scriptName.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the region.
		/// </summary>
		/// <value>The name of the region.</value>
		public string RegionName
		{
			get
			{
				CheckDisposed();
				return m_regionName.Text;
			}

			set
			{
				CheckDisposed();
				m_regionName.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the variant.
		/// </summary>
		/// <value>The name of the variant.</value>
		public string VariantName
		{
			get
			{
				CheckDisposed();
				return m_variantName.Text;
			}

			set
			{
				CheckDisposed();

				m_variantName.Text = value;
			}
		}

		/// <summary>
		/// Activates a child control.
		/// </summary>
		/// <param name="directed">true to specify the direction of the control to select; otherwise, false.</param>
		/// <param name="forward">true to move forward in the tab order; false to move backward in the tab order.</param>
		protected override void Select(bool directed, bool forward)
		{
			base.Select(directed, forward);
			if (!directed)
				SelectNextControl(null, forward, true, true, false);
		}

		/// <summary>
		/// Load the controls from the writing system, if it is not null. If it is null, clear all controls.
		/// If the combo boxes are not populated, do nothing...the method will get called again
		/// when the form loads.
		/// </summary>
		private void LoadControlsFromWritingSystem()
		{
			if (m_ws == null)
				return; // Probably in design mode; can't populate.

			m_origScriptSubtag = m_ws.ScriptSubtag;
			m_origRegionSubtag = m_ws.RegionSubtag;
			m_origVariantSubtag = m_ws.VariantSubtag;

			m_scriptName.Items.Clear();
			m_scriptName.Items.AddRange(LangTagUtils.ScriptSubtags.ToArray());
			ScriptSubtag = m_origScriptSubtag;

			m_regionName.Items.Clear();
			m_regionName.Items.AddRange(LangTagUtils.RegionSubtags.ToArray());
			RegionSubtag = m_origRegionSubtag;

			PopulateVariantCombo(false);
			VariantSubtag = m_origVariantSubtag;
		}

		private void PopulateVariantCombo(bool fPreserve)
		{
			m_variantName.BeginUpdate();
			VariantSubtag orig = VariantSubtag;
			m_variantName.Items.Clear();
			m_variantName.Items.AddRange((from subtag in LangTagUtils.VariantSubtags
										  where subtag.IsVariantOf(m_ws.Id)
										  select subtag).ToArray());
			if (orig != null && fPreserve)
				VariantSubtag = orig;
			m_variantName.EndUpdate();
		}

		/// <summary>
		/// Check that the contents of the control are valid. If not, report the error
		/// to the user and return false. This should prevent the user from closing the
		/// containing form using OK, but not from cancelling.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		public bool CheckValid()
		{
			CheckDisposed();

			string caption = FwCoreDlgControls.kstidError;

			ScriptSubtag scriptSubtag = ScriptSubtag;
			// Can't allow a script name without an abbreviation.
			if (scriptSubtag == null && !string.IsNullOrEmpty(m_scriptName.Text.Trim()))
			{
				MessageBox.Show(FindForm(), FwCoreDlgControls.kstidMissingScrAbbr, caption);
				return false;
			}
			if (scriptSubtag != null && scriptSubtag.IsPrivateUse)
			{
				if (!LangTagUtils.GetScriptSubtag(scriptSubtag.Code).IsPrivateUse)
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidDupScrAbbr, caption);
					return false;
				}
				if (!scriptSubtag.IsValid)
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidInvalidScrAbbr, caption);
					return false;
				}
			}

			RegionSubtag regionSubtag = RegionSubtag;
			// Can't allow a country name without an abbreviation.
			if (regionSubtag == null && !string.IsNullOrEmpty(m_regionName.Text.Trim()))
			{
				MessageBox.Show(FindForm(), FwCoreDlgControls.kstidMissingRgnAbbr, caption);
				return false;
			}
			if (regionSubtag != null && regionSubtag.IsPrivateUse)
			{
				if (!LangTagUtils.GetRegionSubtag(regionSubtag.Code).IsPrivateUse)
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidDupRgnAbbr, caption);
					return false;
				}
				if (!regionSubtag.IsValid)
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidInvalidRgnAbbr, caption);
					return false;
				}
			}

			VariantSubtag variantSubtag = VariantSubtag;
			// Can't allow a variant name without an abbreviation.
			if (variantSubtag == null && !string.IsNullOrEmpty(m_variantName.Text.Trim()))
			{
				MessageBox.Show(FindForm(), FwCoreDlgControls.kstidMissingVarAbbr, caption);
				return false;
			}
			if (variantSubtag != null && variantSubtag.IsPrivateUse)
			{
				if (!LangTagUtils.GetVariantSubtag(variantSubtag.Code).IsPrivateUse)
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidDupVarAbbr, caption);
					return false;
				}
				if (!variantSubtag.IsValid)
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidInvalidVarAbbr, caption);
					return false;
				}
			}

			return true;
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegionVariantControl));
			this.m_regionCode = new System.Windows.Forms.TextBox();
			this.m_variantNameLabel = new System.Windows.Forms.Label();
			this.m_variantCode = new System.Windows.Forms.TextBox();
			this.m_regionCodeLabel = new System.Windows.Forms.Label();
			this.m_variantCodeLabel = new System.Windows.Forms.Label();
			this.m_regionNameLabel = new System.Windows.Forms.Label();
			this.m_regionName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_variantName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_scriptNameLabel = new System.Windows.Forms.Label();
			this.m_scriptCodeLabel = new System.Windows.Forms.Label();
			this.m_scriptCode = new System.Windows.Forms.TextBox();
			this.m_scriptName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.SuspendLayout();
			//
			// m_regionCode
			//
			this.m_helpProvider.SetHelpString(this.m_regionCode, resources.GetString("m_regionCode.HelpString"));
			resources.ApplyResources(this.m_regionCode, "m_regionCode");
			this.m_regionCode.Name = "m_regionCode";
			this.m_helpProvider.SetShowHelp(this.m_regionCode, ((bool)(resources.GetObject("m_regionCode.ShowHelp"))));
			this.m_regionCode.TextChanged += new System.EventHandler(this.m_regionCode_TextChanged);
			this.m_regionCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_regionCode_KeyPress);
			//
			// m_variantNameLabel
			//
			resources.ApplyResources(this.m_variantNameLabel, "m_variantNameLabel");
			this.m_variantNameLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_variantNameLabel.Name = "m_variantNameLabel";
			this.m_helpProvider.SetShowHelp(this.m_variantNameLabel, ((bool)(resources.GetObject("m_variantNameLabel.ShowHelp"))));
			//
			// m_variantCode
			//
			this.m_helpProvider.SetHelpString(this.m_variantCode, resources.GetString("m_variantCode.HelpString"));
			resources.ApplyResources(this.m_variantCode, "m_variantCode");
			this.m_variantCode.Name = "m_variantCode";
			this.m_helpProvider.SetShowHelp(this.m_variantCode, ((bool)(resources.GetObject("m_variantCode.ShowHelp"))));
			this.m_variantCode.TextChanged += new System.EventHandler(this.m_variantCode_TextChanged);
			this.m_variantCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_variantCode_KeyPress);
			//
			// m_regionCodeLabel
			//
			resources.ApplyResources(this.m_regionCodeLabel, "m_regionCodeLabel");
			this.m_regionCodeLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_regionCodeLabel.Name = "m_regionCodeLabel";
			this.m_helpProvider.SetShowHelp(this.m_regionCodeLabel, ((bool)(resources.GetObject("m_regionCodeLabel.ShowHelp"))));
			//
			// m_variantCodeLabel
			//
			resources.ApplyResources(this.m_variantCodeLabel, "m_variantCodeLabel");
			this.m_variantCodeLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_variantCodeLabel.Name = "m_variantCodeLabel";
			this.m_helpProvider.SetShowHelp(this.m_variantCodeLabel, ((bool)(resources.GetObject("m_variantCodeLabel.ShowHelp"))));
			//
			// m_regionNameLabel
			//
			resources.ApplyResources(this.m_regionNameLabel, "m_regionNameLabel");
			this.m_regionNameLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_regionNameLabel.Name = "m_regionNameLabel";
			this.m_helpProvider.SetShowHelp(this.m_regionNameLabel, ((bool)(resources.GetObject("m_regionNameLabel.ShowHelp"))));
			//
			// m_regionName
			//
			this.m_regionName.AllowSpaceInEditBox = true;
			this.m_helpProvider.SetHelpString(this.m_regionName, resources.GetString("m_regionName.HelpString"));
			resources.ApplyResources(this.m_regionName, "m_regionName");
			this.m_regionName.Name = "m_regionName";
			this.m_helpProvider.SetShowHelp(this.m_regionName, ((bool)(resources.GetObject("m_regionName.ShowHelp"))));
			this.m_regionName.Sorted = true;
			this.m_regionName.SelectedIndexChanged += new System.EventHandler(this.m_regionName_SelectedIndexChanged);
			this.m_regionName.TextChanged += new System.EventHandler(this.m_regionName_TextChanged);
			//
			// m_variantName
			//
			this.m_variantName.AllowSpaceInEditBox = true;
			this.m_helpProvider.SetHelpString(this.m_variantName, resources.GetString("m_variantName.HelpString"));
			resources.ApplyResources(this.m_variantName, "m_variantName");
			this.m_variantName.Name = "m_variantName";
			this.m_helpProvider.SetShowHelp(this.m_variantName, ((bool)(resources.GetObject("m_variantName.ShowHelp"))));
			this.m_variantName.Sorted = true;
			this.m_variantName.SelectedIndexChanged += new System.EventHandler(this.m_variantName_SelectedIndexChanged);
			this.m_variantName.TextChanged += new System.EventHandler(this.m_variantName_TextChanged);
			//
			// m_scriptNameLabel
			//
			resources.ApplyResources(this.m_scriptNameLabel, "m_scriptNameLabel");
			this.m_scriptNameLabel.Name = "m_scriptNameLabel";
			//
			// m_scriptCodeLabel
			//
			resources.ApplyResources(this.m_scriptCodeLabel, "m_scriptCodeLabel");
			this.m_scriptCodeLabel.Name = "m_scriptCodeLabel";
			//
			// m_scriptCode
			//
			resources.ApplyResources(this.m_scriptCode, "m_scriptCode");
			this.m_scriptCode.Name = "m_scriptCode";
			this.m_scriptCode.TextChanged += new System.EventHandler(this.m_scriptCode_TextChanged);
			this.m_scriptCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_scriptCode_KeyPress);
			//
			// m_scriptName
			//
			this.m_scriptName.AllowSpaceInEditBox = true;
			resources.ApplyResources(this.m_scriptName, "m_scriptName");
			this.m_scriptName.Name = "m_scriptName";
			this.m_scriptName.Sorted = true;
			this.m_scriptName.SelectedIndexChanged += new System.EventHandler(this.m_scriptName_SelectedIndexChanged);
			this.m_scriptName.TextChanged += new System.EventHandler(this.m_scriptName_TextChanged);
			//
			// RegionVariantControl
			//
			this.BackColor = System.Drawing.SystemColors.Control;
			this.Controls.Add(this.m_scriptName);
			this.Controls.Add(this.m_scriptCode);
			this.Controls.Add(this.m_scriptCodeLabel);
			this.Controls.Add(this.m_scriptNameLabel);
			this.Controls.Add(this.m_variantName);
			this.Controls.Add(this.m_regionName);
			this.Controls.Add(this.m_regionCode);
			this.Controls.Add(this.m_variantNameLabel);
			this.Controls.Add(this.m_variantCode);
			this.Controls.Add(this.m_regionCodeLabel);
			this.Controls.Add(this.m_variantCodeLabel);
			this.Controls.Add(this.m_regionNameLabel);
			this.Name = "RegionVariantControl";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

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
		private void m_scriptName_TextChanged(object sender, EventArgs e)
		{
			string scriptName = m_scriptName.Text.Trim();
			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			int selIndex = m_scriptName.FindStringExact(scriptName);
			if (selIndex >= 0)
			{
				m_scriptName.SelectedIndex = selIndex;
				return;
			}

			if (string.IsNullOrEmpty(scriptName))
			{
				m_scriptCode.Text = "";
				m_scriptCode.Enabled = false;
			}
			else
			{
				m_scriptCode.Text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(GetValidAbbr(scriptName, 4));
				m_scriptCode.Enabled = true;
			}

			m_ws.ScriptSubtag = ScriptSubtag;

			PopulateVariantCombo(true);
			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		private void m_scriptName_SelectedIndexChanged(object sender, EventArgs e)
		{
			var subtag = (ScriptSubtag) m_scriptName.SelectedItem;
			m_scriptCode.Text = subtag.Code;
			m_scriptCode.Enabled = false;

			m_ws.ScriptSubtag = ScriptSubtag;

			PopulateVariantCombo(true);
			OnScriptRegionVariantChanged(EventArgs.Empty);
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
		private void m_regionName_TextChanged(object sender, EventArgs e)
		{
			string regionName = m_regionName.Text.Trim();

			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			int selIndex = m_regionName.FindStringExact(regionName);
			if (selIndex >= 0)
			{
				m_regionName.SelectedIndex = selIndex;
				return;
			}

			if (string.IsNullOrEmpty(regionName))
			{
				m_regionCode.Text = "";
				m_regionCode.Enabled = false;
			}
			else
			{
				m_regionCode.Text = GetValidAbbr(regionName, 2).ToUpperInvariant();
				m_regionCode.Enabled = true;
			}

			m_ws.RegionSubtag = RegionSubtag;

			PopulateVariantCombo(true);
			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		/// <summary>
		/// If the selection changes, update the abbr even though the user hasn't left the
		/// control.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_regionName_SelectedIndexChanged(object sender, EventArgs e)
		{
			var subtag = (RegionSubtag) m_regionName.SelectedItem;
			m_regionCode.Text = subtag.Code;
			m_regionCode.Enabled = false;

			m_ws.RegionSubtag = RegionSubtag;

			PopulateVariantCombo(true);
			OnScriptRegionVariantChanged(EventArgs.Empty);
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
		private void m_variantName_TextChanged(object sender, EventArgs e)
		{
			string variantName = m_variantName.Text.Trim();
			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			int selIndex = m_variantName.FindStringExact(variantName);
			if (selIndex >= 0)
			{
				m_variantName.SelectedIndex = selIndex;
				return;
			}

			if (string.IsNullOrEmpty(variantName))
			{
				m_variantCode.Enabled = false;
				m_variantCode.Text = "";
			}
			else
			{
				m_variantCode.Enabled = true;
				m_variantCode.Text = GetValidAbbr(variantName, 8).ToLowerInvariant();
			}

			m_ws.VariantSubtag = VariantSubtag;

			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		/// <summary>
		/// If the selection changes, update the abbr even though the user hasn't left the
		/// control.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_variantName_SelectedIndexChanged(object sender, EventArgs e)
		{
			var subtag = (VariantSubtag) m_variantName.SelectedItem;
			m_variantCode.Enabled = subtag.IsPrivateUse;
			m_variantCode.Text = subtag.Code;

			m_ws.VariantSubtag = VariantSubtag;

			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Suppress entering invalid characters. Note that, for incomprehensible reasons,
		/// Backspace and returns come through this validator, while Delete and arrow keys don't,
		/// so we have to allow Backspace/return explicitly but can ignore the others.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_regionCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		/// <summary>
		/// Suppress entering invalid characters. Note that, for incomprehensible reasons,
		/// Backspace and returns come through this validator, while Delete and arrow keys don't,
		/// so we have to allow Backspace/return explicitly but can ignore the others.
		/// </summary>
		private void m_scriptCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		/// <summary>
		/// Suppress entering invalid characters.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_variantCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		private static void HandleKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar != (int)Keys.Back && e.KeyChar != (int)Keys.Return && e.KeyChar != (int)Keys.Delete
				&& !IsValidAbbrChar(e.KeyChar))
			{
				// Stop the character from being entered into the control since it is not valid.
				e.Handled = true;
				MiscUtils.ErrorBeep();
			}
		}

		private static string GetValidAbbr(string name, int maxLen)
		{
			var sb = new StringBuilder();
			foreach (char c in name)
			{
				if (IsValidAbbrChar(c))
				{
					sb.Append(c);
					if (sb.Length == maxLen)
						break;
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Identify characters allowed in the abbreviation fields: upper case or numeric
		/// plus hyphen. Strictly ASCII characters are used to name locales.
		/// Review: do we need to allow backspace, del, arrow keys?
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		private static bool IsValidAbbrChar(char ch)
		{
			return (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || (ch >= 'a' && ch <= 'z');
		}

		private void m_scriptCode_TextChanged(object sender, EventArgs e)
		{
			m_ws.ScriptSubtag = ScriptSubtag;

			PopulateVariantCombo(true);
			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		private void m_regionCode_TextChanged(object sender, EventArgs e)
		{
			m_ws.RegionSubtag = RegionSubtag;

			PopulateVariantCombo(true);
			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		private void m_variantCode_TextChanged(object sender, EventArgs e)
		{
			m_ws.VariantSubtag = VariantSubtag;
			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Raises the <see cref="T:ScriptRegionVariantChanged"/> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnScriptRegionVariantChanged(EventArgs e)
		{
			if (ScriptRegionVariantChanged != null)
				ScriptRegionVariantChanged(this, e);
		}
	}
}
