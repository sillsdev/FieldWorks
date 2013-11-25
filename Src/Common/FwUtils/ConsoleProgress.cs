// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ConsoleProgress.cs
// Responsibility: mcconnel

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Utility class to provide progress reporting via console output instead of a dialog box.
	/// This may be useful for command line utility programs that access DLLs that use IProgress
	/// arguments for progress reporting.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ConsoleProgress : IProgress
	{
		int m_min;
		int m_max = 100;
		string m_message;
		int m_pos;
		int m_dots;
		int m_grain = 1;

		/// <summary>
		/// Let the caller know whether we've written any dots out for progress reporting.
		/// </summary>
		public bool DotsWritten
		{
			get { return m_dots > 0; }
		}

		#region IProgress Members

		/// <summary>
		/// Gets the form displaying the progress (used for message box owners, etc). If the progress
		/// is not associated with a visible Form, then this returns its owning form, if any.
		/// </summary>
		public System.Windows.Forms.Form Form
		{
			get { return null; }
		}

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		/// <value>The maximum.</value>
		public int Maximum
		{
			get { return m_max; }
			set
			{
				m_max = value;
				ComputeGranularity();
			}
		}

		private void ComputeGranularity()
		{
			m_grain = ((m_max - m_min) + 79) / 80;
			if (m_grain <= 0)
				m_grain = 1;
		}

		/// <summary>
		/// Get the message within the progress display window.
		/// </summary>
		/// <value>The message.</value>
		public string Message
		{
			get { return m_message; }
			set
			{
				m_message = value;
				if (DotsWritten)
					Console.WriteLine();
				Console.WriteLine(m_message);
				m_dots = 0;
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>The minimum.</value>
		public int Minimum
		{
			get { return m_min; }
			set
			{
				m_min = value;
				ComputeGranularity();
			}
		}

		/// <summary>
		/// Gets or sets the current position of the progress bar. This should be within the limits set by
		/// SetRange, or returned by GetRange.
		/// </summary>
		/// <value>The position.</value>
		public int Position
		{
			get { return m_pos; }
			set
			{
				m_pos = value;
			}
		}

		/// <summary>
		/// Cause the progress indicator to advance by the specified amount.
		/// </summary>
		/// <param name="amount">Amount of progress.</param>
		public void Step(int amount)
		{
			++m_pos;
			if ((m_pos % m_grain) == 0)
			{
				Console.Write('.');
				++m_dots;
			}
			if (Canceling != null)
			{
				// don't do anything -- this just shuts up the compiler about the
				// event handler never being used.
			}
		}

		/// <summary>
		/// Gets or sets the size of the step increment used by Step.
		/// </summary>
		/// <value>The size of the step.</value>
		public int StepSize
		{
			get { return 1; }
			set { }
		}

		/// <summary>
		/// Get the title of the progress display window.
		/// </summary>
		/// <value>The title.</value>
		public string Title
		{
			get { return String.Empty; }
			set { }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the opertation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		public bool AllowCancel
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// Event handler for listening to whether or the cancel button is pressed.
		/// </summary>
		public event System.ComponentModel.CancelEventHandler Canceling;

		/// <summary>
		/// Gets or sets the progress bar style.
		/// </summary>
		public System.Windows.Forms.ProgressBarStyle ProgressBarStyle
		{
			get { return System.Windows.Forms.ProgressBarStyle.Continuous; }
			set { }
		}

		#endregion
	}
}
