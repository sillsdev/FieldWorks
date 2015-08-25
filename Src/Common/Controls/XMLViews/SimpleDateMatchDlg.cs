using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework;	// various help routines
using SIL.FieldWorks.Common.FwUtils;

// ShowHelp

namespace SIL.FieldWorks.Common.Controls
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class SimpleDateMatchDlg : Form, IFWDisposable
	{
		private const string s_helpTopic = "khtpFilterRestrict";
		private IHelpTopicProvider m_helpTopicProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SimpleDateMatchDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
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
				int xWidth = m_chkStartBC.Location.X;
				m_chkStartBC.Visible = false;
				this.Width = xWidth;
				int yDelta = m_cancelButton.Location.Y - m_chkUnspecific.Location.Y;
				m_chkUnspecific.Visible = false;
				this.Height = this.Height - yDelta;
			}
			m_chkEndBC.Visible = false;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the selection start.
		/// </summary>
		/// <value>The selection start.</value>
		/// ------------------------------------------------------------------------------------
		public DateTime SelectionStart
		{
			get
			{
				CheckDisposed();

				if (ShowingTimes)
					return m_startPicker.Value;
				else
					return m_startPicker.Value.Date;
			}
			set
			{
				CheckDisposed();
				m_startPicker.Value = value;
			}
		}

		bool ShowingTimes
		{
			get { return m_typeCombo.SelectedIndex == 4; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the selection end.
		/// </summary>
		/// <value>The selection end.</value>
		/// ------------------------------------------------------------------------------------
		public DateTime SelectionEnd
		{
			get
			{
				CheckDisposed();

				if (ShowingTimes)
					return m_endPicker.Value;
				// If not showing times we want the range to extend to the very end of the day.
				// Also, currently, not showing times corresponds to not showing the second
				// control, so we take the end value as well as the beginning from the START
				// control.
				DateTime end = m_startPicker.Value.Date; // YES, YES, really, truly the start picker!!
				end = end.AddHours(23);
				end = end.AddSeconds(3599.999);
				return end;
			}
			set
			{
				CheckDisposed();
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
					m_chkEndBC.Visible = true;
				//System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.FullDateTimePattern
				// = dddd, MMMM dd, yyyy h:mm:ss tt
				string dateTimeFormat = "ddd, MMM dd, yyyy h:mm:ss tt";
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resulting matcher.
		/// </summary>
		/// <value>The resulting matcher.</value>
		/// ------------------------------------------------------------------------------------
		public IMatcher ResultingMatcher
		{
			get
			{
				CheckDisposed();

				DateTimeMatcher val = new DateTimeMatcher(SelectionStart, SelectionEnd, CompareType);
				val.HandleGenDate = HandleGenDate;
				if (HandleGenDate)
				{
					val.IsStartAD = !m_chkStartBC.Checked;
					val.IsEndAD = ShowingTimes ? !m_chkEndBC.Checked : !m_chkStartBC.Checked;
					val.UnspecificMatching = m_chkUnspecific.Checked;
				}
				return val;
			}
		}

		DateTimeMatcher.DateMatchType CompareType
		{
			get
			{
				switch (m_typeCombo.SelectedIndex)
				{
					default:
					case 0: // on
						return DateTimeMatcher.DateMatchType.On;
					case 1: // not on
						return DateTimeMatcher.DateMatchType.NotRange;
					case 2: //on or before
						return DateTimeMatcher.DateMatchType.Before;
					case 3: // on or after
						return DateTimeMatcher.DateMatchType.After;
					case 4: // between...and...
						return DateTimeMatcher.DateMatchType.Range;
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
				CheckDisposed();

				switch (CompareType)
				{
					case DateTimeMatcher.DateMatchType.On:
						return SelectionStart.ToShortDateString();
					case DateTimeMatcher.DateMatchType.NotRange:
						return String.Format(XMLViewsStrings.ksNotX, SelectionStart.ToShortDateString());
					case DateTimeMatcher.DateMatchType.Before:
						return String.Format(XMLViewsStrings.ksLessEqX, SelectionStart.ToShortDateString());
					case DateTimeMatcher.DateMatchType.After:
						return String.Format(XMLViewsStrings.ksGreaterEqX, SelectionEnd.ToShortDateString());
					case DateTimeMatcher.DateMatchType.Range:
						return String.Format(XMLViewsStrings.ksRangeXY,
							SelectionStart.ToString("g"), SelectionEnd.ToString("g"));
				}
				return "";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the dialog, based on the old matcher, if any, and if recognized.
		/// </summary>
		/// <param name="matcher1">The matcher1.</param>
		/// ------------------------------------------------------------------------------------
		public void SetDlgValues(IMatcher matcher1)
		{
			CheckDisposed();

			DateTimeMatcher matcher = matcher1 as DateTimeMatcher;
			if (matcher == null)
				return;
			switch (matcher.MatchType)
			{
				case DateTimeMatcher.DateMatchType.On:
					m_typeCombo.SelectedIndex = 0;
					break;
				case DateTimeMatcher.DateMatchType.NotRange:
					m_typeCombo.SelectedIndex = 1;
					break;
				case DateTimeMatcher.DateMatchType.Before:
					m_typeCombo.SelectedIndex = 2;
					break;
				case DateTimeMatcher.DateMatchType.After:
					m_typeCombo.SelectedIndex = 3;
					break;
				case DateTimeMatcher.DateMatchType.Range:
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

		void m_typeCombo_SelectedIndexChanged(object sender, System.EventArgs e)
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

		void m_startPicker_ValueChanged(object sender, System.EventArgs e)
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