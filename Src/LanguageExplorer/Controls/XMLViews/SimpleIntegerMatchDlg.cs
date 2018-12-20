// Copyright (c) 2004-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class SimpleIntegerMatchDlg : Form
	{
		private FwOverrideComboBox m_comboMatchType;
		private IHelpTopicProvider m_helpTopicProvider;
		private System.Windows.Forms.Label m_labelAnd;
		private System.Windows.Forms.Button m_cancelButton;
		private System.Windows.Forms.Button m_okButton;
		private NumericUpDown m_nudVal1;
		private NumericUpDown m_nudVal2;
		private Button m_helpButton;
		private HelpProvider helpProvider1;
		private const string s_helpTopic = "khtpFilterRestrict";

		// All the current indexes of the text in the drop down list, if the order changes in
		// the dlg than those changes should be reflected here.
		private const int GreaterThan = 0;
		private const int LessThan = 1;
		private const int EqualTo = 2;
		private const int NotEqualTo = 3;
		private const int LessThanOREqualTo = 4;
		private const int GreaterThanOREqualTo = 5;
		private const int Between = 6;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary />
		private SimpleIntegerMatchDlg()
		{
			InitializeComponent();
			m_comboMatchType.SelectedIndex = 0;
			// Set the max and min values for the NumericUpDown controls
			m_nudVal1.Maximum = m_nudVal2.Maximum = int.MaxValue;
			m_nudVal1.Minimum = m_nudVal2.Minimum = int.MinValue;
		}

		/// <summary />
		public SimpleIntegerMatchDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
			helpProvider1.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider1.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider1.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		/// <summary>
		/// Gets the resulting matcher.
		/// </summary>
		public IMatcher ResultingMatcher
		{
			get
			{
				var val = (int)m_nudVal1.Value;	// validating handled in the use of the numeric up down control
				var index = m_comboMatchType.SelectedIndex;
				switch (index)
				{
					case EqualTo: //	equal to
						return new RangeIntMatcher(val, val);
					case NotEqualTo: //	not equal to
						return new NotEqualIntMatcher(val);
					case LessThan: //	less than
						return new RangeIntMatcher(int.MinValue, val - 1);
					case GreaterThan: //	greater than
						return new RangeIntMatcher(val + 1, int.MaxValue);
					case LessThanOREqualTo: //	less than or equal to
						return new RangeIntMatcher(int.MinValue, val);
					case GreaterThanOREqualTo: //	greater than or equal to
						return new RangeIntMatcher(val, int.MaxValue);
					case Between: //	between
						var val2 = (int)m_nudVal2.Value;
						// Swap the values if val is > val2 : the UI allows ranges
						// to be entered in any order small to large and large to small,
						// but the processing expects val to be smaller than val2.
						if (val > val2)
						{
							var temp = val;
							val = val2;
							val2 = temp;
						}
						return new RangeIntMatcher(val, val2);
					default:
						throw new Exception("internal error...bad combo index in SimpleIntegerMatchDlg.ResultingMatcher");
				}
			}
		}
		/// <summary>
		/// A representation of the condition.
		/// </summary>
		public string Pattern
		{
			get
			{
				var index = m_comboMatchType.SelectedIndex;
				var sval = m_nudVal1.Value.ToString();
				switch (index)
				{
					case EqualTo: //	equal to
						return sval;
					case NotEqualTo: //	not equal to
						return string.Format(XMLViewsStrings.ksNotX, sval);
					case LessThan: //	less than
						return string.Format(XMLViewsStrings.ksLessX, sval);
					case GreaterThan://	greater than
						return string.Format(XMLViewsStrings.ksGreaterX, sval);
					case LessThanOREqualTo://	less than or equal to
						return string.Format(XMLViewsStrings.ksLessEqX, sval);
					case GreaterThanOREqualTo://	greater than or equal to
						return string.Format(XMLViewsStrings.ksGreaterEqX, sval);
					case Between: //	between
						// Swap the values if val is > val2 : the UI allows ranges
						// to be entered in any order small to large and large to small,
						// but the processing expects val to be smaller than val2.
						var sval2 = m_nudVal2.Value.ToString();
						if (m_nudVal1.Value > m_nudVal2.Value)
						{
							sval = m_nudVal2.Value.ToString();
							sval2 = m_nudVal1.Value.ToString();
						}
						return string.Format(XMLViewsStrings.ksRangeXY, sval, sval2);
					default:
						throw new Exception("internal error...bad combo index in SimpleIntegerMatchDlg.Pattern");
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				components?.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimpleIntegerMatchDlg));
			this.m_comboMatchType = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_labelAnd = new System.Windows.Forms.Label();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_nudVal1 = new System.Windows.Forms.NumericUpDown();
			this.m_nudVal2 = new System.Windows.Forms.NumericUpDown();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.helpProvider1 = new HelpProvider();
			((System.ComponentModel.ISupportInitialize)(this.m_nudVal1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_nudVal2)).BeginInit();
			this.SuspendLayout();
			//
			// m_comboMatchType
			//
			this.m_comboMatchType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_comboMatchType.Items.AddRange(new object[] {
			resources.GetString("m_comboMatchType.Items"),
			resources.GetString("m_comboMatchType.Items1"),
			resources.GetString("m_comboMatchType.Items2"),
			resources.GetString("m_comboMatchType.Items3"),
			resources.GetString("m_comboMatchType.Items4"),
			resources.GetString("m_comboMatchType.Items5"),
			resources.GetString("m_comboMatchType.Items6")});
			resources.ApplyResources(this.m_comboMatchType, "m_comboMatchType");
			this.m_comboMatchType.Name = "m_comboMatchType";
			this.m_comboMatchType.SelectedIndexChanged += new System.EventHandler(this.m_comboMatchType_SelectedIndexChanged);
			//
			// m_labelAnd
			//
			resources.ApplyResources(this.m_labelAnd, "m_labelAnd");
			this.m_labelAnd.Name = "m_labelAnd";
			//
			// m_cancelButton
			//
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.Name = "m_cancelButton";
			//
			// m_okButton
			//
			this.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.Name = "m_okButton";
			//
			// m_nudVal1
			//
			resources.ApplyResources(this.m_nudVal1, "m_nudVal1");
			this.m_nudVal1.Name = "m_nudVal1";
			//
			// m_nudVal2
			//
			resources.ApplyResources(this.m_nudVal2, "m_nudVal2");
			this.m_nudVal2.Name = "m_nudVal2";
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.UseVisualStyleBackColor = true;
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// SimpleIntegerMatchDlg
			//
			this.AcceptButton = this.m_okButton;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_cancelButton;
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_nudVal2);
			this.Controls.Add(this.m_nudVal1);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_okButton);
			this.Controls.Add(this.m_labelAnd);
			this.Controls.Add(this.m_comboMatchType);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SimpleIntegerMatchDlg";
			((System.ComponentModel.ISupportInitialize)(this.m_nudVal1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_nudVal2)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Display the second text box and label only for the 'between' case.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_comboMatchType_SelectedIndexChanged(object sender, EventArgs e)
		{
			var fBetween = m_comboMatchType.SelectedIndex == m_comboMatchType.Items.Count - 1;
			m_labelAnd.Visible = fBetween;
			m_nudVal2.Visible = fBetween;
		}

		internal void SetDlgValues(IMatcher matcher)
		{
			long val;
			if (matcher is RangeIntMatcher)
			{
				var rm = (RangeIntMatcher)matcher;
				if (rm.Min == int.MinValue)
				{
					m_comboMatchType.SelectedIndex = LessThan; // less than
					val = rm.Max + 1;
				}
				else if (rm.Max == rm.Min)
				{
					m_comboMatchType.SelectedIndex = EqualTo; // equal to
					val = rm.Min;
				}
				else if (rm.Max == int.MaxValue)
				{
					m_comboMatchType.SelectedIndex = GreaterThan; // greater than
					val = rm.Min - 1;
				}
				else
				{
					m_comboMatchType.SelectedIndex = Between; // between
					val = rm.Min;
					m_nudVal2.Value = rm.Max;
				}
				// Enhance JohnT: it would be nice if there was some way to tell whether
				// the user entered >= 3 versus > 2, but at present we can't.
			}
			else if (matcher is NotEqualIntMatcher)
			{
				m_comboMatchType.SelectedIndex = NotEqualTo; // not equal
				val = ((NotEqualIntMatcher)matcher).NotEqualValue;
			}
			else
				return; // old matcher is for some other combo item, use defaults.
			m_nudVal1.Value = val;
		}

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
	}
}
