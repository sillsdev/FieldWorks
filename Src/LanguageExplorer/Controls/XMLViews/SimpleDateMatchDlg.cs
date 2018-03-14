// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public partial class SimpleDateMatchDlg : Form
	{
		private const string s_helpTopic = "khtpFilterRestrict";
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SimpleDateMatchDlg"/> class.
		/// </summary>
		public SimpleDateMatchDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_typeCombo.SelectedIndex = 0;

			m_helpTopicProvider = helpTopicProvider;
			helpProvider1.HelpNamespace = helpTopicProvider.HelpFile;
			helpProvider1.SetHelpKeyword(this, helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider1.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		/// <summary>
		/// Flag whether we're dealing with GenDate instead of DateTime data.
		/// </summary>
		public bool HandleGenDate { get; set; }

		/// <summary>
		/// Hide unwanted controls.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (!HandleGenDate)
			{
				var xWidth = m_chkStartBC.Location.X;
				m_chkStartBC.Visible = false;
				Width = xWidth;
				var yDelta = m_cancelButton.Location.Y - m_chkUnspecific.Location.Y;
				m_chkUnspecific.Visible = false;
				Height = Height - yDelta;
			}
			m_chkEndBC.Visible = false;
		}

		/// <summary>
		/// Gets or sets the selection start.
		/// </summary>
		public DateTime SelectionStart
		{
			get
			{
				return ShowingTimes ? m_startPicker.Value : m_startPicker.Value.Date;
			}
			set
			{
				m_startPicker.Value = value;
			}
		}

		private bool ShowingTimes => m_typeCombo.SelectedIndex == 4;

		/// <summary>
		/// Gets or sets the selection end.
		/// </summary>
		public DateTime SelectionEnd
		{
			get
			{
				if (ShowingTimes)
				{
					return m_endPicker.Value;
				}
				// If not showing times we want the range to extend to the very end of the day.
				// Also, currently, not showing times corresponds to not showing the second
				// control, so we take the end value as well as the beginning from the START
				// control.
				var end = m_startPicker.Value.Date; // YES, YES, really, truly the start picker!!
				end = end.AddHours(23);
				end = end.AddSeconds(3599.999);
				return end;
			}
			set
			{
				m_endPicker.Value = value;
			}
		}

		private void UpdateCalendarOptions()
		{
			if (m_typeCombo.SelectedIndex == 4)
			{
				m_andLabel.Visible = true;
				m_endPicker.Visible = true;
				if (HandleGenDate)
				{
					m_chkEndBC.Visible = true;
				}
				//System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.FullDateTimePattern
				// = dddd, MMMM dd, yyyy h:mm:ss tt
				const string dateTimeFormat = "ddd, MMM dd, yyyy h:mm:ss tt";
				m_startPicker.Format = DateTimePickerFormat.Custom;
				m_startPicker.CustomFormat = dateTimeFormat;
				m_endPicker.Format = DateTimePickerFormat.Custom;
				m_endPicker.CustomFormat = dateTimeFormat;
			}
			else
			{
				m_andLabel.Visible = false;
				m_endPicker.Visible = false;
				m_chkEndBC.Visible = false;
				m_startPicker.Format = DateTimePickerFormat.Long;
			}
		}

		/// <summary>
		/// Gets the resulting matcher.
		/// </summary>
		public IMatcher ResultingMatcher
		{
			get
			{
				var val = new DateTimeMatcher(SelectionStart, SelectionEnd, CompareType) {HandleGenDate = HandleGenDate};
				if (!HandleGenDate)
				{
					return val;
				}
				val.IsStartAD = !m_chkStartBC.Checked;
				val.IsEndAD = ShowingTimes ? !m_chkEndBC.Checked : !m_chkStartBC.Checked;
				val.UnspecificMatching = m_chkUnspecific.Checked;
				return val;
			}
		}

		DateMatchType CompareType
		{
			get
			{
				switch (m_typeCombo.SelectedIndex)
				{
					default:
					case 0: // on
						return DateMatchType.On;
					case 1: // not on
						return DateMatchType.NotRange;
					case 2: //on or before
						return DateMatchType.Before;
					case 3: // on or after
						return DateMatchType.After;
					case 4: // between...and...
						return DateMatchType.Range;
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
				switch (CompareType)
				{
					case DateMatchType.On:
						return SelectionStart.ToShortDateString();
					case DateMatchType.NotRange:
						return string.Format(XMLViewsStrings.ksNotX, SelectionStart.ToShortDateString());
					case DateMatchType.Before:
						return string.Format(XMLViewsStrings.ksLessEqX, SelectionStart.ToShortDateString());
					case DateMatchType.After:
						return string.Format(XMLViewsStrings.ksGreaterEqX, SelectionEnd.ToShortDateString());
					case DateMatchType.Range:
						return string.Format(XMLViewsStrings.ksRangeXY, SelectionStart.ToString("g"), SelectionEnd.ToString("g"));
				}
				return string.Empty;
			}
		}

		/// <summary>
		/// Initialize the dialog, based on the old matcher, if any, and if recognized.
		/// </summary>
		public void SetDlgValues(IMatcher matcher1)
		{
			var matcher = matcher1 as DateTimeMatcher;
			if (matcher == null)
			{
				return;
			}
			switch (matcher.MatchType)
			{
				case DateMatchType.On:
					m_typeCombo.SelectedIndex = 0;
					break;
				case DateMatchType.NotRange:
					m_typeCombo.SelectedIndex = 1;
					break;
				case DateMatchType.Before:
					m_typeCombo.SelectedIndex = 2;
					break;
				case DateMatchType.After:
					m_typeCombo.SelectedIndex = 3;
					break;
				case DateMatchType.Range:
					m_typeCombo.SelectedIndex = 4;
					break;
			}

			SelectionStart = matcher.Start;
			SelectionEnd = matcher.End;

			HandleGenDate = matcher.HandleGenDate;
			m_chkStartBC.Checked = !matcher.IsStartAD;
			m_chkEndBC.Checked = !matcher.IsEndAD;
			m_chkUnspecific.Checked = matcher.UnspecificMatching;
		}

		private void m_typeCombo_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			UpdateCalendarOptions();
		}

		void m_endPicker_ValueChanged(object sender, System.EventArgs e)
		{
			if (m_typeCombo.SelectedIndex != 4)
				return; // side effect change
			if (SelectionEnd < SelectionStart)
				SelectionStart = SelectionEnd;
		}

		private void m_startPicker_ValueChanged(object sender, System.EventArgs e)
		{
			if (m_typeCombo.SelectedIndex != 4)
			{
				// Only one showing, get the effect we want by making the other match
				SelectionEnd = SelectionStart;
			}
			else
			{
				// Both visible, keep in order
				if (SelectionEnd < SelectionStart)
					SelectionEnd = SelectionStart;
			}
		}

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
	}
}