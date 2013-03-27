using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A chooser dialog for generic dates.
	/// </summary>
	public class GenDateChooserDlg : Form, IFWDisposable
	{
		private const int PRECISION_NO_DATE = 4;
		private const int MONTH_UNKNOWN = 12;
		private const int ERA_AD = 0;
		private const int ERA_BC = 1;
		private const int LEAP_YEAR = 2008;
		private int DAY_UNKNOWN
		{
			get
			{
				return m_dayComboBox.Items.Count - 1;
			}
		}

		private ComboBox m_precisionComboBox;
		private ComboBox m_monthComboBox;
		private Button m_okButton;
		private Button m_cancelButton;
		private Button m_helpButton;
		private IHelpTopicProvider m_helpTopicProvider;
		private NumericUpDown m_yearUpDown;
		private MonthCalendar m_calendar;
		private ComboBox m_eraComboBox;
		private GroupBox m_emptyCalendar;
		private Label label1;
		private HelpProvider m_helpProvider;
		private ComboBox m_dayComboBox;
		private bool m_changingDate = false;

		private string m_helpTopic = "khtpGenDateChooserDlg";

		/// <summary>
		/// Initializes a new instance of the <see cref="GenDateChooserDlg"/> class.
		/// </summary>
		private GenDateChooserDlg()
		{
			InitializeComponent();

#if __MonoCS__
			// FWNX-817:
			// center calendar on Form. This is necessary because it has a different size
			// on Linux
			m_calendar.Left = (ClientRectangle.Width - m_calendar.Width) / 2;
			// make the empty calender box the same size as calendar
			var oriEmptyCalendarSize = m_emptyCalendar.Size;
			m_emptyCalendar.Size = m_calendar.Size;
			m_emptyCalendar.Location = m_calendar.Location;
			// resize "No Calendar Available" label.
			label1.Size = m_emptyCalendar.Size - (oriEmptyCalendarSize - label1.Size);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="GenDateChooserDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public GenDateChooserDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
			if (m_helpTopicProvider != null)
			{
				m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_helpProvider != null)
					m_helpProvider.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets or sets the help topic.
		/// </summary>
		/// <value>The help topic.</value>
		public string HelpTopic
		{
			get
			{
				CheckDisposed();
				return m_helpTopic;
			}

			set
			{
				CheckDisposed();
				m_helpTopic = value;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(value));
			}
		}

		/// <summary>
		/// Gets the number of days in the currently selected month. If the year is unknown, this will return
		/// the number of days in the currently selected month for a leap year.
		/// </summary>
		/// <value>The days in month.</value>
		private int DaysInMonth
		{
			get
			{
				var year = (int)m_yearUpDown.Value;
				return DateTime.DaysInMonth(year == 0 ? LEAP_YEAR : year, m_monthComboBox.SelectedIndex + 1);
			}
		}

		/// <summary>
		/// Determines if the currently selected date is in the available range for the calendar control.
		/// </summary>
		private bool IsDateInCalendarRange
		{
			get
			{
				var year = (int)m_yearUpDown.Value;
				return year >= m_calendar.MinDate.Year && year <= m_calendar.MaxDate.Year;
			}
		}

		/// <summary>
		/// Gets or sets the generic date.
		/// </summary>
		/// <value>The generic date.</value>
		public GenDate GenericDate
		{
			get
			{
				CheckDisposed();

				if (m_precisionComboBox.SelectedIndex == PRECISION_NO_DATE)
					return new GenDate();

				var precision = (GenDate.PrecisionType)m_precisionComboBox.SelectedIndex;
				var month = m_monthComboBox.SelectedIndex == -1 ? GenDate.UnknownMonth : m_monthComboBox.SelectedIndex + 1;
				var day = m_dayComboBox.SelectedIndex == -1 ? GenDate.UnknownDay : m_dayComboBox.SelectedIndex + 1;
				var year = (int)m_yearUpDown.Value;
				var ad = m_eraComboBox.SelectedIndex == ERA_AD;
				return new GenDate(precision, month, day, year, ad);
			}

			set
			{
				CheckDisposed();

				if (value.IsEmpty)
				{
					m_precisionComboBox.SelectedIndex = PRECISION_NO_DATE;
					m_eraComboBox.SelectedIndex = ERA_AD;
				}
				else
				{
					m_precisionComboBox.SelectedIndex = (int)value.Precision;
					m_eraComboBox.SelectedIndex = value.IsAD ? ERA_AD : ERA_BC;
					m_yearUpDown.Value = value.Year;
					m_monthComboBox.SelectedIndex = value.Month == GenDate.UnknownMonth ? MONTH_UNKNOWN : value.Month - 1;
					m_dayComboBox.SelectedIndex = value.Day == GenDate.UnknownDay ? DAY_UNKNOWN : value.Day - 1;
				}
			}
		}

		/// <summary>
		/// Updates the calendar control to match the currently selected date.
		/// </summary>
		private void UpdateCalendar()
		{
			if (m_calendar.Visible)
			{
				var year = (int)m_yearUpDown.Value;
				var month = m_monthComboBox.SelectedIndex == MONTH_UNKNOWN || m_monthComboBox.SelectedIndex == -1
					? 1 : m_monthComboBox.SelectedIndex + 1;
				var day = m_dayComboBox.SelectedIndex == DAY_UNKNOWN || m_dayComboBox.SelectedIndex == -1
					? 1 : m_dayComboBox.SelectedIndex + 1;
				m_calendar.SetDate(new DateTime(year, month, day));
			}
		}

		/// <summary>
		/// Populates the day combo box with the correct number of days for the currently selected month.
		/// </summary>
		private void PopulateDayComboBox()
		{
			var numDays = DaysInMonth;
			var selectedIndex = m_dayComboBox.SelectedIndex;
			m_dayComboBox.BeginUpdate();
			m_dayComboBox.Items.Clear();
			for (int i = 1; i <= numDays; i++)
				m_dayComboBox.Items.Add(i);
			m_dayComboBox.Items.Add(DetailControlsStrings.ksGenDateUnknown);
			m_dayComboBox.EndUpdate();
			m_dayComboBox.SelectedIndex = selectedIndex < numDays ? selectedIndex : -1;
		}

		private void m_monthComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_monthComboBox.SelectedIndex == -1 || m_changingDate)
				return;

			try
			{
				m_changingDate = true;
				if (m_monthComboBox.SelectedIndex == MONTH_UNKNOWN)
				{
					m_monthComboBox.SelectedIndex = -1;
					m_dayComboBox.SelectedIndex = -1;
					m_dayComboBox.Enabled = false;
					if (m_yearUpDown.Value == 0)
						m_precisionComboBox.SelectedIndex = PRECISION_NO_DATE;
				}
				else
				{
					m_dayComboBox.Enabled = true;
					PopulateDayComboBox();
					if (m_precisionComboBox.SelectedIndex == PRECISION_NO_DATE)
						m_precisionComboBox.SelectedIndex = (int)GenDate.PrecisionType.Exact;
				}
				UpdateCalendar();
			}
			finally
			{
				m_changingDate = false;
			}
		}

		private void m_yearUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (m_changingDate)
				return;

			try
			{
				m_changingDate = true;
				var year = (int)m_yearUpDown.Value;
				// hide the calendar control if the current date is out of the supported range
				m_calendar.Visible = m_eraComboBox.SelectedIndex == ERA_AD && IsDateInCalendarRange;

				if (m_monthComboBox.SelectedIndex != -1 && DaysInMonth != m_dayComboBox.Items.Count - 1)
					// only repopulate the day combo if the number of days in the month has changed
					PopulateDayComboBox();

				if (year == 0 && m_monthComboBox.SelectedIndex == -1)
					m_precisionComboBox.SelectedIndex = PRECISION_NO_DATE;
				else if (m_precisionComboBox.SelectedIndex == PRECISION_NO_DATE)
					m_precisionComboBox.SelectedIndex = (int)GenDate.PrecisionType.Exact;

				UpdateCalendar();
			}
			finally
			{
				m_changingDate = false;
			}
		}

		private void m_eraComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_eraComboBox.SelectedIndex == ERA_BC)
			{
				// GenDate does not support exact month and day for BC dates
				m_monthComboBox.SelectedIndex = -1;
				m_monthComboBox.Enabled = false;
				m_dayComboBox.SelectedIndex = -1;
				m_dayComboBox.Enabled = false;
				// calendar control cannot handle BC dates
				m_calendar.Visible = false;
			}
			else
			{
				m_monthComboBox.Enabled = true;
				var year = (int)m_yearUpDown.Value;
				m_calendar.Visible = IsDateInCalendarRange;
			}
		}

		private void m_dayComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_dayComboBox.SelectedIndex == -1 || m_changingDate)
				return;

			try
			{
				m_changingDate = true;
				if (m_dayComboBox.SelectedIndex == DAY_UNKNOWN)
					m_dayComboBox.SelectedIndex = -1;

				UpdateCalendar();
			}
			finally
			{
				m_changingDate = false;
			}
		}

		private void m_precisionComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_precisionComboBox.SelectedIndex == PRECISION_NO_DATE)
			{
				m_monthComboBox.SelectedIndex = -1;
				m_dayComboBox.SelectedIndex = -1;
				m_dayComboBox.Enabled = false;
				m_yearUpDown.Value = 0;
			}
			else if (m_monthComboBox.SelectedIndex == -1 && m_yearUpDown.Value == 0)
			{
				m_calendar.SetDate(DateTime.Now);
			}
		}

		private void m_calendar_DateChanged(object sender, DateRangeEventArgs e)
		{
			if (m_changingDate)
				return;

			var date = m_calendar.SelectionStart;
			if (m_precisionComboBox.SelectedIndex == PRECISION_NO_DATE)
				m_precisionComboBox.SelectedIndex = (int)GenDate.PrecisionType.Exact;
			m_yearUpDown.Value = date.Year;
			m_monthComboBox.SelectedIndex = date.Month - 1;
			m_dayComboBox.SelectedIndex = date.Day - 1;
		}

		private void m_okButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenDateChooserDlg));
			this.m_precisionComboBox = new System.Windows.Forms.ComboBox();
			this.m_monthComboBox = new System.Windows.Forms.ComboBox();
			this.m_dayComboBox = new System.Windows.Forms.ComboBox();
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_yearUpDown = new System.Windows.Forms.NumericUpDown();
			this.m_calendar = new System.Windows.Forms.MonthCalendar();
			this.m_eraComboBox = new System.Windows.Forms.ComboBox();
			this.m_emptyCalendar = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			((System.ComponentModel.ISupportInitialize)(this.m_yearUpDown)).BeginInit();
			this.m_emptyCalendar.SuspendLayout();
			this.SuspendLayout();
			//
			// m_precisionComboBox
			//
			this.m_precisionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_precisionComboBox.FormattingEnabled = true;
			this.m_precisionComboBox.Items.AddRange(new object[] {
			resources.GetString("m_precisionComboBox.Items"),
			resources.GetString("m_precisionComboBox.Items1"),
			resources.GetString("m_precisionComboBox.Items2"),
			resources.GetString("m_precisionComboBox.Items3"),
			resources.GetString("m_precisionComboBox.Items4")});
			resources.ApplyResources(this.m_precisionComboBox, "m_precisionComboBox");
			this.m_precisionComboBox.Name = "m_precisionComboBox";
			this.m_precisionComboBox.SelectedIndexChanged += new System.EventHandler(this.m_precisionComboBox_SelectedIndexChanged);
			//
			// m_monthComboBox
			//
			this.m_monthComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_monthComboBox.FormattingEnabled = true;
			this.m_monthComboBox.Items.AddRange(new object[] {
			resources.GetString("m_monthComboBox.Items"),
			resources.GetString("m_monthComboBox.Items1"),
			resources.GetString("m_monthComboBox.Items2"),
			resources.GetString("m_monthComboBox.Items3"),
			resources.GetString("m_monthComboBox.Items4"),
			resources.GetString("m_monthComboBox.Items5"),
			resources.GetString("m_monthComboBox.Items6"),
			resources.GetString("m_monthComboBox.Items7"),
			resources.GetString("m_monthComboBox.Items8"),
			resources.GetString("m_monthComboBox.Items9"),
			resources.GetString("m_monthComboBox.Items10"),
			resources.GetString("m_monthComboBox.Items11"),
			resources.GetString("m_monthComboBox.Items12")});
			resources.ApplyResources(this.m_monthComboBox, "m_monthComboBox");
			this.m_monthComboBox.Name = "m_monthComboBox";
			this.m_monthComboBox.SelectedIndexChanged += new System.EventHandler(this.m_monthComboBox_SelectedIndexChanged);
			//
			// m_dayComboBox
			//
			this.m_dayComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_dayComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_dayComboBox, "m_dayComboBox");
			this.m_dayComboBox.Name = "m_dayComboBox";
			this.m_dayComboBox.SelectedIndexChanged += new System.EventHandler(this.m_dayComboBox_SelectedIndexChanged);
			//
			// m_okButton
			//
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.UseVisualStyleBackColor = true;
			this.m_okButton.Click += new System.EventHandler(this.m_okButton_Click);
			//
			// m_cancelButton
			//
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_cancelButton.Name = "m_cancelButton";
			this.m_cancelButton.UseVisualStyleBackColor = true;
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.UseVisualStyleBackColor = true;
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// m_yearUpDown
			//
			resources.ApplyResources(this.m_yearUpDown, "m_yearUpDown");
			this.m_yearUpDown.Maximum = new decimal(new int[] {
			9999,
			0,
			0,
			0});
			this.m_yearUpDown.Name = "m_yearUpDown";
			this.m_yearUpDown.ValueChanged += new System.EventHandler(this.m_yearUpDown_ValueChanged);
			//
			// m_calendar
			//
			resources.ApplyResources(this.m_calendar, "m_calendar");
			this.m_calendar.MaxSelectionCount = 1;
			this.m_calendar.Name = "m_calendar";
			this.m_calendar.DateChanged += new System.Windows.Forms.DateRangeEventHandler(this.m_calendar_DateChanged);
			//
			// m_eraComboBox
			//
			this.m_eraComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_eraComboBox.FormattingEnabled = true;
			this.m_eraComboBox.Items.AddRange(new object[] {
			resources.GetString("m_eraComboBox.Items"),
			resources.GetString("m_eraComboBox.Items1")});
			resources.ApplyResources(this.m_eraComboBox, "m_eraComboBox");
			this.m_eraComboBox.Name = "m_eraComboBox";
			this.m_helpProvider.SetShowHelp(this.m_eraComboBox, ((bool)(resources.GetObject("m_eraComboBox.ShowHelp"))));
			this.m_eraComboBox.SelectedIndexChanged += new System.EventHandler(this.m_eraComboBox_SelectedIndexChanged);
			//
			// m_emptyCalendar
			//
			this.m_emptyCalendar.Controls.Add(this.label1);
			resources.ApplyResources(this.m_emptyCalendar, "m_emptyCalendar");
			this.m_emptyCalendar.Name = "m_emptyCalendar";
			this.m_emptyCalendar.TabStop = false;
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// GenDateChooserDlg
			//
			this.AutoScaleMode = AutoScaleMode.Font;
			this.AcceptButton = this.m_okButton;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_cancelButton;
			this.Controls.Add(this.m_eraComboBox);
			this.Controls.Add(this.m_calendar);
			this.Controls.Add(this.m_yearUpDown);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_okButton);
			this.Controls.Add(this.m_dayComboBox);
			this.Controls.Add(this.m_monthComboBox);
			this.Controls.Add(this.m_precisionComboBox);
			this.Controls.Add(this.m_emptyCalendar);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GenDateChooserDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.m_yearUpDown)).EndInit();
			this.m_emptyCalendar.ResumeLayout(false);
			this.m_emptyCalendar.PerformLayout();
			this.ResumeLayout(false);

		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			//Here we want to make sure the size of the dialog is reasonable so that the buttons are not hidden
			//by the calendar when the dialog is loaded in 120 dpi
			if (m_calendar.Bottom > m_okButton.Top)
			{
				this.Height = 337;
				m_calendar.Left = 43;
			}
		}
	}
}
